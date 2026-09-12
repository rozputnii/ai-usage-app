import { readFile, readdir, open, rename, unlink } from "node:fs/promises";
import { join } from "node:path";
import { assertNoReparse } from "./patch";

export interface Authorization {
  version: 1; revision: number; goalId: string; allowedItems: string[]; baseRef: string;
  capabilities: string[]; budget: { limit: number; used: number; unit: "steps" };
  currentItem: string | null; completedItems: string[]; sessionIds: string[];
  status: "active" | "paused" | "stopped"; stopReason: string | null;
}
export interface Task { id: string; status: string; dependencies: string[]; evidence: string; writes: string[]; shared: string[]; parallel: boolean; isolation: string; agent: string; ownership: string }
export interface Item { id: string; title: string; goal: string; status: string; dependencies: string[]; trigger: string; tasks: Task[]; taskFile: string | null }
export interface Project { root: string; activeGoal: string | null; goals: { id: string; scope: string[] }[]; items: Item[]; authorization: Authorization | null }
export interface Candidate { id: string; score: number; rationale: string[]; dimensions: number[] }
const heading = "## Execution authorization";
const fail = (message: string): never => { throw new Error(message); };
const same = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b);
const field = (text: string, name: string) => text.match(new RegExp(`^- ${name}: (.*)$`, "m"))?.[1]?.trim() ?? "";
function list(value: string): string[] {
  if (value.startsWith("[")) {
    const inner = value.slice(1, -1);
    if (!value.endsWith("]")) fail("Malformed list");
    return inner ? inner.split(",").map(x => x.trim().replace(/^"(.*)"$/, "$1")) : [];
  }
  return value ? value.split(",").map(x => x.trim()) : [];
}
function blocks(text: string, pattern: RegExp): { id: string; title: string; text: string }[] {
  const matches = [...text.matchAll(pattern)];
  return matches.map((m, index) => ({ id: m[1]!, title: m[2]!, text: text.slice(m.index! + m[0].length, matches[index + 1]?.index ?? text.length) }));
}
function shape(auth: Authorization): void {
  if (!auth || auth.version !== 1 || !Number.isSafeInteger(auth.revision) || auth.revision < 0 ||
      !/^G-\d+$/.test(auth.goalId) || typeof auth.baseRef !== "string" || !auth.baseRef.trim() ||
      !Array.isArray(auth.allowedItems) || !auth.allowedItems.length || auth.allowedItems.some(id => typeof id !== "string" || !/^AIU-\d+$/.test(id)) ||
      new Set(auth.allowedItems).size !== auth.allowedItems.length || !Array.isArray(auth.capabilities) ||
      !auth.capabilities.length || auth.capabilities.some(c => !["implementation", "research"].includes(c)) ||
      !auth.budget || auth.budget.unit !== "steps" || !Number.isSafeInteger(auth.budget.limit) || auth.budget.limit <= 0 ||
      !Number.isSafeInteger(auth.budget.used) || auth.budget.used < 0 || auth.budget.used > auth.budget.limit ||
      !Array.isArray(auth.completedItems) || new Set(auth.completedItems).size !== auth.completedItems.length ||
      !Array.isArray(auth.sessionIds) || new Set(auth.sessionIds).size !== auth.sessionIds.length ||
      auth.sessionIds.some(id => typeof id !== "string" || !/^[a-zA-Z0-9-]{1,128}$/.test(id)) ||
      auth.completedItems.some(id => !auth.allowedItems.includes(id)) ||
      (auth.currentItem !== null && (!auth.allowedItems.includes(auth.currentItem) || auth.completedItems.includes(auth.currentItem))) ||
      !["active", "paused", "stopped"].includes(auth.status) ||
      (auth.stopReason !== null && (typeof auth.stopReason !== "string" || !auth.stopReason.trim()))) fail("Invalid bounded authorization");
}
function authorization(text: string): Authorization | null {
  const sections = [...text.matchAll(/^## Execution authorization\s*$/gm)];
  if (!sections.length) return null;
  if (sections.length !== 1) fail("Duplicate authorization");
  const rest = text.slice(sections[0]!.index! + sections[0]![0].length);
  const section = rest.split(/^## /m)[0]!;
  const match = section.match(/^\s*```json\s*\n([\s\S]*?)\n```\s*$/);
  if (!match) fail("Malformed authorization block");
  const auth = JSON.parse(match[1]!) as Authorization;
  shape(auth);
  return auth;
}
export async function loadAuthorization(root: string): Promise<Authorization | null> {
  return authorization(await readFile(join(root, "docs/product/goals.md"), "utf8"));
}
export async function readProject(root: string): Promise<Project> {
  const [goalsText, backlog, dirs] = await Promise.all([
    readFile(join(root, "docs/product/goals.md"), "utf8"), readFile(join(root, "docs/backlog.md"), "utf8"),
    readdir(join(root, "docs/specs"), { withFileTypes: true }),
  ]);
  const goals = blocks(goalsText, /^## (G-\d+) - (.+)$/gm).map(b => ({ id: b.id, scope: list(field(b.text, "scope")) }));
  const items: Item[] = [];
  for (const b of blocks(backlog, /^## (AIU-\d+) - (.+)$/gm)) {
    const matches = dirs.filter(d => d.isDirectory() && d.name.startsWith(`${b.id}-`));
    if (matches.length > 1) fail(`Duplicate specification for ${b.id}`);
    let tasks: Task[] = [];
    let taskFile: string | null = null;
    if (matches.length) {
      try {
        taskFile = join(root, "docs/specs", matches[0]!.name, "tasks.md");
        const text = await readFile(taskFile, "utf8");
        tasks = blocks(text, /^### (T-\d+) - (.+)$/gm).map(t => ({ id: t.id, status: field(t.text, "status"), dependencies: list(field(t.text, "depends_on")), evidence: field(t.text, "evidence"), writes: list(field(t.text, "writes")), shared: list(field(t.text, "shared")), parallel: field(t.text, "parallel") === "true", isolation: field(t.text, "isolation"), agent: field(t.text, "agent"), ownership: field(t.text, "ownership") }));
      } catch (error) { if ((error as NodeJS.ErrnoException).code !== "ENOENT") throw error; }
    }
    items.push({ id: b.id, title: b.title, goal: field(b.text, "goal"), status: field(b.text, "status"), dependencies: list(field(b.text, "depends_on")), trigger: field(b.text, "trigger"), tasks, taskFile });
  }
  if (new Set(items.map(i => i.id)).size !== items.length || new Set(goals.map(g => g.id)).size !== goals.length) fail("Duplicate project IDs");
  return { root, activeGoal: goalsText.match(/^active_goal:\s*(G-\d{3})\s*$/m)?.[1] ?? null, goals, items, authorization: authorization(goalsText) };
}
export function verifyContinuation(previous: Authorization, next: Authorization): void {
  shape(next);
  if (next.goalId !== previous.goalId || next.baseRef !== previous.baseRef ||
      !same(next.allowedItems, previous.allowedItems) || !same(next.capabilities, previous.capabilities) ||
      next.budget.limit !== previous.budget.limit || next.budget.unit !== previous.budget.unit) fail("Authorization changed after owner confirmation");
  if (next.revision < previous.revision || next.budget.used < previous.budget.used ||
      previous.completedItems.some(id => !next.completedItems.includes(id)) ||
      previous.sessionIds.some(id => !next.sessionIds.includes(id)) ||
      (previous.currentItem !== null && next.currentItem !== previous.currentItem && !next.completedItems.includes(previous.currentItem))) fail("Authorization history regressed");
}
export function verifyAuthorization(project: Project, auth: Authorization, confirmed?: Authorization): void {
  shape(auth);
  if (confirmed) verifyContinuation(confirmed, auth);
  const goal = project.goals.find(g => g.id === auth.goalId);
  if (!goal || auth.allowedItems.some(id => !goal.scope.includes(id) || project.items.find(i => i.id === id)?.goal !== goal.id)) fail("Authorization exceeds goal scope");
  if (project.authorization && !same(project.authorization, auth)) fail("Stale authorization or changed scope/base");
  if (auth.status !== "active") fail(`Authorization is ${auth.status}`);
  if (auth.budget.used >= auth.budget.limit) fail("Execution budget exhausted");
  if (auth.currentItem !== null) {
    const item = project.items.find(i => i.id === auth.currentItem);
    if (!item || !["ready", "selected", "in-progress", "review", "research-needed"].includes(item.status) ||
        !item.dependencies.every(id => auth.completedItems.includes(id) || project.items.some(i => i.id === id && i.status === "done")) ||
        !(item.status === "research-needed" ? auth.capabilities.includes("research") : auth.capabilities.includes("implementation"))) fail("Selected item state or dependencies no longer permits execution");
  }
}
function eligible(project: Project, item: Item, auth: Authorization): boolean {
  return auth.allowedItems.includes(item.id) && item.goal === auth.goalId &&
    ["ready", "selected", "in-progress", "review", "research-needed"].includes(item.status) &&
    !auth.completedItems.includes(item.id) && item.dependencies.every(id => project.items.some(i => i.id === id && i.status === "done") || auth.completedItems.includes(id)) &&
    (item.status === "research-needed" ? auth.capabilities.includes("research") : auth.capabilities.includes("implementation")) &&
    !(item.tasks.length && item.tasks.every(t => t.status === "done" || t.status === "dropped"));
}
export function rank(project: Project, goalId: string): Candidate[] {
  const auth = project.authorization;
  if (!auth || auth.goalId !== goalId) return [];
  verifyAuthorization(project, auth);
  return project.items.filter(i => eligible(project, i, auth)).map(i => {
    const unblocked = project.items.reduce((count, other) => count + Number(other.dependencies.includes(i.id)), 0);
    const dimensions = [5, Math.min(5, unblocked), i.status === "research-needed" ? 5 : 3, 3, i.trigger === "now" ? 5 : i.trigger === "next" ? 4 : 2, 3];
    return { id: i.id, dimensions, score: 100 * dimensions.reduce((sum, score, index) => sum + [0.30, 0.20, 0.20, 0.15, 0.10, 0.05][index]! * score / 5, 0),
      rationale: ["Goal alignment: explicitly in scope (5/5)", `Work unblocked: ${unblocked} dependent outcomes (${dimensions[1]}/5)`,
        `Risk reduction: ${i.status === "research-needed" ? "research uncertainty" : "implementation"} (${dimensions[2]}/5)`,
        "User value: neutral estimate pending product evidence (3/5)", `Urgency: trigger ${i.trigger || "unspecified"} (${dimensions[4]}/5)`,
        "Effort efficiency: neutral estimate without measured effort (3/5); estimates are judgment, not objective value"] };
  }).sort((a, b) => b.score - a.score || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0));
}
// Exclusive temporary locks coordinate this engine's callers, not ambient same-user writers.
// Expected full document comparison precedes atomic replacement; this is not a sandbox.
async function transition(root: string, expected: Authorization | null, change: (project: Project, current: Authorization | null) => Authorization): Promise<Authorization> {
  await assertNoReparse(root, "docs/product/goals.md");
  const path = join(root, "docs/product/goals.md");
  const lockPath = `${path}.work-lock`;
  const lock = await open(lockPath, "wx");
  const temporary = `${path}.work-${crypto.randomUUID()}.tmp`;
  try {
    const before = await readFile(path, "utf8");
    const current = authorization(before);
    if (!same(current, expected)) fail("Stale authorization; reload before continuing");
    const project = await readProject(root);
    if (!same(project.authorization, current)) fail("Stale project state");
    const next = change(project, current);
    next.revision = (current?.revision ?? 0) + 1;
    shape(next);
    const block = `${heading}\n\n\`\`\`json\n${JSON.stringify(next, null, 2)}\n\`\`\`\n`;
    const output = current ? before.replace(/^## Execution authorization\s*\n[\s\S]*?(?=^## |$(?![\s\S]))/m, block + "\n") : `${before.trimEnd()}\n\n${block}`;
    const file = await open(temporary, "wx");
    try { await file.writeFile(output, "utf8"); await file.sync(); } finally { await file.close(); }
    if (await readFile(path, "utf8") !== before) fail("Stale goals document");
    await rename(temporary, path);
    return next;
  } finally {
    await unlink(temporary).catch(error => { if (error.code !== "ENOENT") throw error; });
    await lock.close();
    await unlink(lockPath);
  }
}
function spend(auth: Authorization, steps: number): Authorization {
  if (!Number.isSafeInteger(steps) || steps <= 0) fail("Steps must be a positive bounded integer");
  if (steps > auth.budget.limit - auth.budget.used) return { ...auth, status: "stopped", stopReason: "Execution step budget exhausted" };
  const used = auth.budget.used + steps;
  return { ...auth, budget: { ...auth.budget, used }, status: used === auth.budget.limit ? "stopped" : "active", stopReason: used === auth.budget.limit ? "Execution step budget exhausted" : null };
}
export async function select(root: string, id: string, auth: Authorization): Promise<void> {
  const persisted = await loadAuthorization(root);
  await transition(root, persisted ? auth : null, project => {
    verifyAuthorization(project, auth);
    if (!persisted && (auth.revision !== 0 || auth.budget.used !== 0 || auth.currentItem !== null || auth.completedItems.length)) fail("First adoption requires a fresh explicit grant");
    if (auth.currentItem !== null) fail("Complete the current item before selecting another");
    const item = project.items.find(i => i.id === id);
    if (!item || !eligible(project, item, auth)) fail("Item is ineligible or dependencies unresolved");
    return spend({ ...auth, currentItem: id }, 1);
  });
}
export async function pause(root: string, reason: string): Promise<void> {
  if (!reason.trim()) fail("Pause reason required");
  // Reload on contention/staleness is a caller concern; no queued work may bypass a persisted pause.
  const auth = await loadAuthorization(root);
  if (!auth) fail("No authorization to pause");
  await transition(root, auth, () => ({ ...auth, status: "paused", stopReason: reason }));
}
export async function resume(root: string, auth: Authorization, confirmed = false): Promise<void> {
  if (!confirmed) fail("Explicit owner confirmation required to resume");
  await transition(root, auth, project => {
    if (auth.status !== "paused") fail("Only paused authorization can resume");
    const active = { ...auth, status: "active" as const, stopReason: null };
    verifyAuthorization({ ...project, authorization: active }, active);
    return spend(active, 1);
  });
}
export async function charge(root: string, auth: Authorization, steps: number): Promise<Authorization> {
  let exceeded = false;
  const result = await transition(root, auth, project => {
    verifyAuthorization(project, auth);
    if (!auth.currentItem) fail("No selected item");
    exceeded = steps > auth.budget.limit - auth.budget.used;
    return spend(auth, steps);
  });
  if (exceeded) fail("Execution budget exceeded; authorization stopped before work");
  return result;
}
export async function handoff(root: string, auth: Authorization): Promise<void> {
  await transition(root, auth, project => { verifyAuthorization(project, auth); return spend(auth, 1); });
}
export async function noteSession(root: string, auth: Authorization, sessionId: string): Promise<Authorization> {
  return transition(root, auth, project => {
    verifyAuthorization(project, auth);
    return { ...auth, sessionIds: [...new Set([...auth.sessionIds, sessionId])] };
  });
}
export async function complete(root: string, auth: Authorization, evidence: string): Promise<Authorization> {
  if (!evidence.trim() || /^(not-run|none|todo)$/i.test(evidence.trim())) fail("Explicit completion evidence required");
  return transition(root, auth, project => {
    verifyAuthorization(project, auth);
    const item = project.items.find(i => i.id === auth.currentItem);
    if (!item || !item.tasks.length || auth.completedItems.includes(item.id)) fail("No completable item");
    if (!item.dependencies.every(id => auth.completedItems.includes(id) || project.items.some(i => i.id === id && i.status === "done"))) fail("Feature dependencies unresolved");
    if (item.tasks.some(t => t.status !== "done" || !t.evidence || /^(not-run|none|todo)$/i.test(t.evidence) || t.dependencies.some(id => !item.tasks.some(d => d.id === id && d.status === "done")))) fail("Tasks or task dependencies lack completion evidence");
    return spend({ ...auth, currentItem: null, completedItems: [...auth.completedItems, item.id] }, 1);
  });
}
