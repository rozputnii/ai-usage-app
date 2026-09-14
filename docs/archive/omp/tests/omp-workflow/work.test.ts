import { afterEach, expect, test } from "bun:test";
import { mkdtemp, mkdir, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { charge, complete, handoff, loadAuthorization, pause, rank, readProject, resume, select, verifyAuthorization, type Authorization } from "../../.omp/lib/work";

const roots: string[] = [];
afterEach(async () => { await Promise.all(roots.splice(0).map(root => rm(root, { recursive: true, force: true }))); });
async function fixture(limit = 12): Promise<{ root: string; auth: Authorization }> {
  const root = await mkdtemp(join(tmpdir(), "aiu-work-"));
  roots.push(root);
  await mkdir(join(root, "docs/product"), { recursive: true });
  await mkdir(join(root, "docs/specs/AIU-001-first"), { recursive: true });
  await writeFile(join(root, "docs/product/goals.md"), "# Goals\n\n## G-001 - Local workflow\n- status: in-progress\n- scope: AIU-001, AIU-002\n\n## Owner notes\nPreserve this text.\n");
  await writeFile(join(root, "docs/backlog.md"), "# Backlog\n\n## AIU-001 - First\n- goal: G-001\n- status: in-progress\n- depends_on: []\n- trigger: now\n\n## AIU-002 - Second\n- goal: G-001\n- status: ready\n- depends_on: [AIU-001]\n- trigger: next\n");
  await writeFile(join(root, "docs/specs/AIU-001-first/tasks.md"), "# Tasks\n\n### T-01 - Execute\n- status: in-progress\n- depends_on: []\n- evidence: not-run\n");
  return { root, auth: { version: 1, revision: 0, goalId: "G-001", allowedItems: ["AIU-001", "AIU-002"], baseRef: "refs/heads/local@abc123", capabilities: ["implementation"], budget: { limit, used: 0, unit: "steps" }, currentItem: null, completedItems: [], sessionIds: [], status: "active", stopReason: null } };
}
async function current(root: string): Promise<Authorization> {
  const auth = await loadAuthorization(root);
  if (!auth) throw new Error("Expected persisted authorization");
  return auth;
}

test("cancellation leaves documents untouched; first selection requires explicit bounded adoption", async () => {
  const { root, auth } = await fixture();
  const before = await readFile(join(root, "docs/product/goals.md"), "utf8");
  expect(rank(await readProject(root), "G-001")).toEqual([]);
  expect(await loadAuthorization(root)).toBeNull();
  expect(await readFile(join(root, "docs/product/goals.md"), "utf8")).toBe(before);
  await select(root, "AIU-001", auth);
  const saved = await current(root);
  expect(saved.currentItem).toBe("AIU-001");
  expect(saved.budget.used).toBe(1);
  expect(saved.baseRef).toBe(auth.baseRef);
  expect(await readFile(join(root, "docs/product/goals.md"), "utf8")).toContain("Preserve this text.");
});

test("owner pause invalidates stale work and resume needs explicit confirmation", async () => {
  const { root, auth } = await fixture();
  await select(root, "AIU-001", auth);
  const old = await current(root);
  await pause(root, "Owner requested a checkpoint");
  await expect(handoff(root, old)).rejects.toThrow("Stale");
  const paused = await current(root);
  await expect(charge(root, paused, 1)).rejects.toThrow("paused");
  await expect(resume(root, paused)).rejects.toThrow("confirmation");
  expect((await current(root)).status).toBe("paused");
  await resume(root, paused, true);
  const resumed = await current(root);
  expect(resumed.status).toBe("active");
  expect(resumed.budget.used).toBe(old.budget.used + 1);
  expect(resumed.currentItem).toBe(old.currentItem);
});

test("budget exhaustion persists stop before over-budget work across sessions", async () => {
  const { root, auth } = await fixture(3);
  await select(root, "AIU-001", auth);
  const selected = await current(root);
  await expect(charge(root, selected, 3)).rejects.toThrow("stopped before work");
  const stopped = await current(root);
  expect(stopped.status).toBe("stopped");
  expect(stopped.budget.used).toBe(1);
  await expect(handoff(root, stopped)).rejects.toThrow("stopped");
  await expect(resume(root, stopped, true)).rejects.toThrow("Only paused");
});

test("exact remaining budget is monotonic and cannot be replenished", async () => {
  const { root, auth } = await fixture(3);
  await select(root, "AIU-001", auth);
  const saved = await charge(root, await current(root), 2);
  expect(saved.budget.used).toBe(3);
  expect(saved.status).toBe("stopped");
  await expect(charge(root, { ...saved, budget: { ...saved.budget, used: 0 } }, 1)).rejects.toThrow("Stale");
});

test("out-of-goal, unresolved dependencies, malformed scope and changed base fail closed", async () => {
  const { root, auth } = await fixture();
  await expect(select(root, "AIU-002", auth)).rejects.toThrow("dependencies");
  await expect(select(root, "AIU-999", { ...auth, allowedItems: ["AIU-999"] })).rejects.toThrow("scope");
  await expect(select(root, "AIU-001", { ...auth, allowedItems: ["AIU-001", "AIU-001"] })).rejects.toThrow("Invalid");
  await expect(select(root, "AIU-001", { ...auth, budget: { ...auth.budget, limit: Infinity } })).rejects.toThrow("Invalid");
  await select(root, "AIU-001", auth);
  const saved = await current(root);
  await expect(handoff(root, { ...saved, baseRef: "different" })).rejects.toThrow("Stale");
  await expect(handoff(root, { ...saved, allowedItems: [...saved.allowedItems, "AIU-003"] })).rejects.toThrow("Stale");
});

test("completion requires task evidence and completed dependencies; handoff never replays", async () => {
  const { root, auth } = await fixture();
  await select(root, "AIU-001", auth);
  const selected = await current(root);
  await expect(complete(root, selected, "not-run")).rejects.toThrow("evidence");
  await expect(complete(root, selected, "local integration artifact")).rejects.toThrow("Tasks");
  const tasksPath = join(root, "docs/specs/AIU-001-first/tasks.md");
  await writeFile(tasksPath, "# Tasks\n\n### T-01 - Execute\n- status: done\n- depends_on: [T-02]\n- evidence: artifact/check\n");
  await expect(complete(root, selected, "local integration artifact")).rejects.toThrow("dependencies");
  await writeFile(tasksPath, "# Tasks\n\n### T-01 - Execute\n- status: done\n- depends_on: []\n- evidence: artifact/check\n");
  const done = await complete(root, selected, "local integration artifact");
  expect(done.completedItems).toEqual(["AIU-001"]);
  expect(done.currentItem).toBeNull();
  await expect(complete(root, done, "replay")).rejects.toThrow("No completable");
  await expect(select(root, "AIU-001", done)).rejects.toThrow("ineligible");
  await handoff(root, done);
  const handed = await current(root);
  verifyAuthorization(await readProject(root), handed, selected);
  expect(handed.completedItems).toEqual(done.completedItems);
  expect(handed.allowedItems).toEqual(done.allowedItems);
  expect(handed.baseRef).toBe(done.baseRef);
  expect(handed.budget.used).toBe(done.budget.used + 1);
  await expect(handoff(root, done)).rejects.toThrow("Stale");
  expect(rank(await readProject(root), "G-001").map(c => c.id)).toEqual(["AIU-002"]);
  await select(root, "AIU-002", handed);
  expect((await current(root)).currentItem).toBe("AIU-002");
});

test("parallel callers cannot both spend the same persisted revision", async () => {
  const { root, auth } = await fixture();
  await select(root, "AIU-001", auth);
  const saved = await current(root);
  const results = await Promise.allSettled([charge(root, saved, 1), charge(root, saved, 1)]);
  expect(results.filter(r => r.status === "fulfilled")).toHaveLength(1);
  expect((await current(root)).budget.used).toBe(saved.budget.used + 1);
});

test("malformed durable authorization cannot silently become a new grant", async () => {
  const { root, auth } = await fixture();
  const path = join(root, "docs/product/goals.md");
  await writeFile(path, `${await readFile(path, "utf8")}\n## Execution authorization\n\n\`\`\`json\n{}\n\`\`\`\n`);
  await expect(loadAuthorization(root)).rejects.toThrow("Invalid");
  await expect(select(root, "AIU-001", auth)).rejects.toThrow("Invalid");
});

test("editing durable scope or budget cannot enlarge a live owner grant", async () => {
  const { root, auth } = await fixture();
  await select(root, "AIU-001", { ...auth, allowedItems: ["AIU-001"] });
  const confirmed = await current(root);
  const path = join(root, "docs/product/goals.md");
  const text = await readFile(path, "utf8");
  for (const changed of [
    { ...confirmed, allowedItems: ["AIU-001", "AIU-002"] },
    { ...confirmed, budget: { ...confirmed.budget, limit: 100 } },
    { ...confirmed, budget: { ...confirmed.budget, used: 0 }, revision: confirmed.revision + 1 },
  ]) {
    await writeFile(path, text.replace(/```json\n[\s\S]*?\n```/, "```json\n" + JSON.stringify(changed) + "\n```"));
    const project = await readProject(root);
    expect(() => verifyAuthorization(project, project.authorization!, confirmed)).toThrow();
  }
});
