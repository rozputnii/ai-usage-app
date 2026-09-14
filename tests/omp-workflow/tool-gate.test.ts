import { afterEach, expect, mock, test } from "bun:test";
import { mkdtemp, mkdir, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import type { ExtensionAPI } from "@oh-my-pi/pi-coding-agent";
import type { Authorization } from "../../.omp/lib/work";
import * as work from "../../.omp/lib/work";

type Context = {
  cwd: string; hasUI: boolean;
  sessionManager: { getSessionId(): string; getEntries(): { type: string; agent: string; isolated: boolean }[]; getSessionFile(): string; getHeader(): { parentSession?: string } };
  abort(): void;
  ui: { notify(message: string, type: string): void; setStatus(): void; confirm(): Promise<boolean>; select(title: string, labels: string[]): Promise<string>; input(): Promise<string> };
  isIdle(): boolean; getAsyncJobSnapshot(): { running: never[] }; newSession(options: { parentSession?: string }): Promise<{ cancelled: boolean }>;
};
type Handler = (event: { toolName?: string; input?: unknown }, ctx: Context) => Promise<{ block: boolean; reason: string } | undefined>;
type Command = (args: string, ctx: Context) => Promise<void>;
type Checkpoint = { name: string; execute(id: string, params: { operation: string }, signal: undefined, update: undefined, ctx: Context): Promise<unknown> };

// Module mocks stay in the child process, never in the workflow suite's module cache.
if (process.env.AIU_GATE_CHILD !== "1") {
  test("actual extension tool gate and bounded session lifecycle", async () => {
    const child = Bun.spawn([process.execPath, "test", import.meta.path], {
      env: { ...process.env, AIU_GATE_CHILD: "1" }, stdout: "pipe", stderr: "pipe",
    });
    const [out, err, code] = await Promise.all([new Response(child.stdout).text(), new Response(child.stderr).text(), child.exited]);
    expect({ code, diagnostics: code ? out + err : "" }).toEqual({ code: 0, diagnostics: "" });
    const executed = /Ran (\d+) tests? across/.exec(out + err);
    expect(Number(executed?.[1] ?? 0)).toBeGreaterThanOrEqual(12);
  }, 60000);
} else {
  mock.module("@mariozechner/pi-coding-agent", () => ({ SettingsManager: { create() { throw new Error("Unexpected settings access"); } } }));
  mock.module("../../.omp/lib/validate", () => ({ validate: async () => {} }));
  // This module-loading boundary must run after child-local registration mocks.
  const { default: register } = await import("../../.omp/extensions/ai-usage");
  const roots: string[] = [];
  afterEach(async () => { await Promise.all(roots.splice(0).map(root => rm(root, { recursive: true, force: true }))); });
  async function fixture(limit = 12) {
    const root = await mkdtemp(join(tmpdir(), "aiu-gate-")); roots.push(root);
    await mkdir(join(root, "docs/product"), { recursive: true });
    await mkdir(join(root, "docs/specs/AIU-001-first"), { recursive: true });
    const original = "# Goals\n\n## G-001 - Local workflow\n- status: in-progress\n- scope: AIU-001, AIU-002\n\n## Owner notes\nPreserve this text.\n";
    const goals = join(root, "docs/product/goals.md");
    await writeFile(goals, original);
    await writeFile(join(root, "docs/backlog.md"), "# Backlog\n\n## AIU-001 - First\n- goal: G-001\n- status: in-progress\n- depends_on: []\n- trigger: now\n\n## AIU-002 - Second\n- goal: G-001\n- status: ready\n- depends_on: [AIU-001]\n- trigger: next\n");
    await writeFile(join(root, "docs/specs/AIU-001-first/tasks.md"), "# Tasks\n\n### T-01 - Execute\n- status: in-progress\n- depends_on: []\n- evidence: not-run\n");
    async function git(...args: string[]) {
      const p = Bun.spawn(["git", "-C", root, ...args], { stdout: "pipe", stderr: "pipe" });
      const [out, err, code] = await Promise.all([new Response(p.stdout).text(), new Response(p.stderr).text(), p.exited]);
      if (code) throw new Error(err); return out.trim();
    }
    await git("init", "-b", "feature/AIU-001-gate");
    await git("-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid", "commit", "--allow-empty", "-m", "fixture");
    const auth: Authorization = { version: 1, revision: 0, goalId: "G-001", allowedItems: ["AIU-001", "AIU-002"], baseRef: `feature/AIU-001-gate@${await git("rev-parse", "HEAD")}`, capabilities: ["implementation"], budget: { limit, used: 0, unit: "steps" }, currentItem: null, completedItems: [], sessionIds: [], status: "active", stopReason: null };
    const handlers: Record<string, Handler> = {}; const tools: Record<string, Checkpoint> = {};
    let command: Command = async () => { throw new Error("Work command not registered"); }; let activeTools: string[] = []; const messages: string[] = []; const errors: string[] = [];
    const schema = (..._args: unknown[]) => ({});
    // Registration-only fake implements the factory's exercised surface, not native runtime internals.
    const api = { on: (name: string, fn: Handler) => { handlers[name] = fn; }, registerCommand: (_name: string, value: { handler: Command }) => { command = value.handler; }, registerTool: (value: Checkpoint) => { tools[value.name] = value; }, typebox: { Type: { Object: schema, Union: schema, Literal: schema, Optional: schema, String: schema } }, getActiveTools: () => activeTools, setActiveTools: (names: string[]) => { activeTools = names; }, sendUserMessage: (message: string) => messages.push(message) } as unknown as ExtensionAPI;
    register(api);
    let signal = new AbortController(); let id = "ordinary"; let entries: { type: string; agent: string; isolated: boolean }[] = []; let confirm = true; let choice = true; let cancelHandoff = false;
    let handoffFault: "throw" | "headless" | "wrong-parent" | undefined;
    let parentSession: string | undefined;
    const ctx: Context = { cwd: root, hasUI: true, sessionManager: { getSessionId: () => id, getEntries: () => entries, getSessionFile: () => `${id}.jsonl`, getHeader: () => ({ parentSession }) }, abort: () => signal.abort(), ui: { notify: (message: string, type: string) => { if (type === "error") errors.push(message); }, setStatus() {}, confirm: async () => confirm, select: async (_title: string, labels: string[]) => choice ? labels[0]! : "Cancel", input: async () => "12" }, isIdle: () => true, getAsyncJobSnapshot: () => ({ running: [] }), newSession: async options => {
      if (cancelHandoff) return { cancelled: true };
      if (handoffFault === "throw") throw new Error("Native session creation failed");
      if (handoffFault === "headless") { ctx.hasUI = false; await handlers.session_start!({}, ctx); ctx.hasUI = true; }
      id = "transferred";
      parentSession = handoffFault === "wrong-parent" ? "unrelated.jsonl" : options.parentSession;
      await handlers.session_start!({}, ctx);
      return { cancelled: false };
    } };
    const call = (toolName = "bash", input: unknown = { command: "fnm --version" }) => handlers.tool_call({ toolName, input }, ctx);
    await handlers.session_start({}, ctx);
    return { root, goals, original, auth, ctx, call, tools, messages, errors, result: () => handlers.tool_result({}, ctx), command: (args: string) => command(args, ctx), aborted: () => signal.signal.aborted, turn: () => { signal = new AbortController(); }, seed: () => work.select(root, "AIU-001", auth), saved: () => work.loadAuthorization(root), text: () => readFile(goals, "utf8"), confirm: (value: boolean) => { confirm = value; }, choice: (value: boolean) => { choice = value; }, worker: (agent: string, isolated: boolean) => { id = `${agent}-${isolated}`; entries = [{ type: "session_init", agent, isolated }]; }, switch: async (value: string) => { id = value; await handlers.session_switch({}, ctx); }, cancelHandoff: () => { cancelHandoff = true; }, fault: (value: typeof handoffFault) => { handoffFault = value; } };
  }
  test("ordinary tools and siblings reach native permissions without durable changes", async () => {
    const f = await fixture();
    for (const [name, input] of [["bash", { command: "fnm --version" }], ["eval", { code: "1 + 1" }], ["write", { path: "local://gate-plan.md", content: "diagnostic" }], ["write", { path: "xd://propose" }], ["task", { tasks: [] }]] as const) expect(await f.call(name, input)).toBeUndefined();
    expect(await Promise.all([f.call(), f.call("read")])).toEqual([undefined, undefined]);
    expect(f.aborted()).toBe(false); expect(await f.text()).toBe(f.original);
    await expect(f.tools.work_checkpoint.execute("id", { operation: "finish" }, undefined, undefined, f.ctx)).rejects.toThrow("No authorized primary");
  });
  test("headless denial is local and only exact isolated worker metadata exempts", async () => {
    const f = await fixture(); f.ctx.hasUI = false;
    for (const name of ["bash", "write", "task"]) expect((await f.call(name)).block).toBe(true);
    expect(await f.call("read")).toBeUndefined(); expect(f.aborted()).toBe(false); expect(await f.text()).toBe(f.original);
    for (const [agent, isolated] of [["work-worker", false], ["other", true]] as const) { f.worker(agent, isolated); expect((await f.call()).block).toBe(true); }
    f.worker("work-worker", true); expect(await f.call()).toBeUndefined();
  });
  test("headless lifecycle does not silently revoke a live interactive primary", async () => {
    const f = await fixture(); await f.seed(); await f.command("resume");
    f.ctx.hasUI = false; await f.switch("ordinary"); f.ctx.hasUI = true;
    const before = (await f.saved())!.budget.used;
    expect(await f.call()).toBeUndefined(); expect((await f.saved())!.budget.used).toBe(before + 1);
    expect(f.aborted()).toBe(false);
  });
  test("cancelled selection and declined resume do not opt in", async () => {
    const f = await fixture(); f.choice(false); await f.command("select G-001"); expect(await f.text()).toBe(f.original); expect(await f.call()).toBeUndefined();
    await f.seed(); const before = await f.text(); f.confirm(false); await f.command("resume"); expect(await f.text()).toBe(before); expect(await f.call()).toBeUndefined(); expect(f.aborted()).toBe(false);
  });
  for (const entry of ["select", "resume"]) test(`${entry} charges once and pause latches across session switches`, async () => {
    const f = await fixture(); if (entry === "resume") await f.seed();
    await f.command(entry === "select" ? "select G-001" : "resume"); expect(f.errors).toEqual([]);
    const before = (await f.saved())!.budget.used; expect(await f.call()).toBeUndefined(); expect((await f.saved())!.budget.used).toBe(before + 1);
    await f.command("pause"); expect(f.aborted()).toBe(true); f.turn(); expect((await f.call()).block).toBe(true); expect(f.aborted()).toBe(false);
    await f.switch("fresh"); expect(await f.call()).toBeUndefined(); await f.switch("ordinary"); expect((await f.call()).block).toBe(true);
  });
  test("disappearing authorization aborts once and retains bounded latch", async () => {
    const f = await fixture(); await f.seed(); await f.command("resume"); await writeFile(f.goals, f.original);
    expect((await f.call()).block).toBe(true); expect(f.aborted()).toBe(true); f.turn(); expect((await f.call()).block).toBe(true); expect(f.aborted()).toBe(false);
  });
  test("last affordable call completes then tool result aborts exhausted grant", async () => {
    const f = await fixture(2); await f.seed(); await f.command("resume"); expect((await f.saved())!.budget.used).toBe(1);
    expect(await f.call()).toBeUndefined(); expect(f.aborted()).toBe(false); expect((await f.saved())!.status).toBe("stopped"); await f.result(); expect(f.aborted()).toBe(true);
    f.turn(); expect((await f.call()).block).toBe(true); expect(f.aborted()).toBe(false);
  });
  test("internal transfer and cancelled handoff preserve bounded session restriction", async () => {
    const f = await fixture(); await f.seed(); await f.command("resume auto"); await f.command("handoff"); expect(f.errors).toEqual([]); expect(f.messages).toHaveLength(1);
    await f.command("pause"); f.turn(); expect((await f.call()).block).toBe(true); expect(f.aborted()).toBe(false);
    await f.switch("fresh"); expect(await f.call()).toBeUndefined(); await f.switch("transferred"); expect((await f.call()).block).toBe(true);
    const cancelled = await fixture(); await cancelled.seed(); await cancelled.command("resume auto"); cancelled.cancelHandoff(); await cancelled.command("handoff"); expect(cancelled.errors).toEqual([]); expect((await cancelled.call()).block).toBe(true); expect(cancelled.aborted()).toBe(false);
  });
  for (const fault of ["throw", "headless", "wrong-parent"] as const) test(`handoff ${fault} cannot arm a later unrelated session`, async () => {
    const f = await fixture(); await f.seed(); await f.command("resume auto"); f.fault(fault); await f.command("handoff");
    expect(f.errors).toHaveLength(1); expect(f.messages).toEqual([]);
    await f.switch("ordinary"); expect((await f.call())?.block).toBe(true);
    await f.switch("unrelated"); const before = await f.text();
    expect(await f.call()).toBeUndefined(); expect(await f.text()).toBe(before);
  });
}
