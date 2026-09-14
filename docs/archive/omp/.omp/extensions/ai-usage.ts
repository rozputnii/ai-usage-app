import type { ExtensionAPI, ExtensionCommandContext, ExtensionContext } from '@oh-my-pi/pi-coding-agent';
import { SettingsManager } from '@mariozechner/pi-coding-agent';
import type { Authorization, Project } from '../lib/work';
import * as work from '../lib/work';
import { checkOwnership, integratePatch, assertNoReparse } from '../lib/patch';
import { validate } from '../lib/validate';

async function git(root: string, args: string[]): Promise<string> {
  const child = Bun.spawn(['git', '-C', root, ...args], { stdout: 'pipe', stderr: 'pipe' });
  const [out, error, code] = await Promise.all([new Response(child.stdout).text(), new Response(child.stderr).text(), child.exited]);
  if (code) throw new Error(error.trim() || 'Local Git check failed');
  return out.trim();
}

async function branchGuard(root: string, auth?: Authorization): Promise<string> {
  const branch = await git(root, ['branch', '--show-current']);
  if (!branch.startsWith('feature/AIU-')) throw new Error('Use an identified feature/AIU- branch; no main writes');
  if (auth) {
    const [originalBranch, base] = auth.baseRef.split('@');
    if (branch !== originalBranch || !base || !/^[a-f0-9]{40}$/.test(base)) throw new Error('Branch or starting reference changed');
    await git(root, ['merge-base', '--is-ancestor', base, 'HEAD']);
  }
  return branch;
}

function draft(project: Project, goalId: string, baseRef: string, limit: number): Authorization {
  const goal = project.goals.find(g => g.id === goalId);
  if (!goal) throw new Error('Unknown goal');
  return { version: 1, revision: 0, goalId, allowedItems: [...goal.scope], baseRef, capabilities: ['implementation', 'research'], budget: { limit, used: 0, unit: 'steps' }, currentItem: null, completedItems: [], sessionIds: [], status: 'active', stopReason: null };
}

export default function (pi: ExtensionAPI) {
  // A new external session never arms itself from a file. Only this process's
  // explicit UI authorization or internal handoff can arm its primary.
  let armed = false;
  const boundedSessions = new Set<string>();
  let automatic = false;
  let transfer = false;
  let transferParent: { id: string; file: string } | undefined;
  let commandContext: ExtensionCommandContext | undefined;
  let finishing = false;
  let confirmed: Authorization | undefined;
  const disarm = (ctx: ExtensionContext) => { armed = false; automatic = false; transfer = false; ctx.abort(); };
  const remember = (ctx: ExtensionContext, auth: Authorization, previous = confirmed) => {
    try { if (previous) work.verifyContinuation(previous, auth); }
    catch (error) { disarm(ctx); throw error; }
    // A concurrent read may precede another accepted transition; never lower the watermark.
    if (confirmed && auth.revision >= confirmed.revision) confirmed = auth;
  };
  let workerSession: string | undefined;
  let isolatedWorker = false;
  const isNativeWorker = (ctx: ExtensionContext) => {
    const id = ctx.sessionManager.getSessionId();
    if (workerSession !== id) {
      const init = ctx.sessionManager.getEntries().findLast(entry => entry.type === 'session_init');
      isolatedWorker = init?.type === 'session_init' && init.agent === 'work-worker' && init.isolated === true;
      workerSession = id;
    }
    return isolatedWorker;
  };
  const announce = (ctx: ExtensionContext, message: string) => ctx.ui.notify(message, 'info');
  const showStatus = (ctx: ExtensionContext, auth: Authorization | null) => {
    if (auth) remember(ctx, auth);
    ctx.ui.setStatus('ai-usage', `AI Usage: ${auth?.goalId ?? '-'} | ${auth?.currentItem ?? '-'} | ${auth?.status ?? 'select'}${auth ? ` | ${auth.budget.used}/${auth.budget.limit} steps` : ''}`);
  };

  async function current(ctx: ExtensionContext): Promise<Authorization> {
    const previous = confirmed;
    try {
      const project = await work.readProject(ctx.cwd);
      if (!project.authorization) throw new Error('No explicit bounded work authorization');
      work.verifyAuthorization(project, project.authorization, previous);
      await branchGuard(ctx.cwd, project.authorization);
      remember(ctx, project.authorization, previous);
      return project.authorization;
    } catch (error) { if (armed) disarm(ctx); throw error; }
  }

  async function present(ctx: ExtensionContext) {
    const project = await work.readProject(ctx.cwd);
    const auth = project.authorization;
    const goalId = auth?.goalId ?? project.activeGoal;
    const active = auth ? auth.currentItem : project.items.find(i => i.goal === goalId && ['selected', 'in-progress', 'paused', 'review'].includes(i.status))?.id;
    ctx.ui.setStatus('ai-usage', `AI Usage: ${goalId ?? '-'} | ${active ?? '-'} | ${auth?.status ?? 'select'}`);
    let summary = `AI Usage: ${goalId ?? 'no selected goal'}; ${active ?? 'no active item'}; ${auth?.status ?? 'selection required'}. /work select, /work resume, /work verify. No automatic authorization from repository status.`;
    if (goalId && !active && (!auth || auth.status === 'active' && auth.budget.used < auth.budget.limit)) {
      const preview = auth ?? draft(project, goalId, 'read-only-preview', 32);
      summary += ` Eligible: ${work.rank({ ...project, authorization: preview }, goalId).slice(0, 5).map(c => `${c.id} (${c.score.toFixed(0)}/100)`).join(', ') || 'none'}. Ranking does not grant permission.`;
    }
    summary += ' Ordinary interactive tools use native OMP permissions; /work opts into bounded execution.';
    announce(ctx, summary);
  }

  async function start(ctx: ExtensionCommandContext) {
    if (!ctx.isIdle()) throw new Error('Wait for the current native turn before starting work');
    if (!armed) throw new Error('Confirm selection or resume in this session first');
    const auth = await current(ctx);
    showStatus(ctx, auth);
    if (!auth.currentItem) throw new Error('Select an eligible item first');
    await validate(ctx.cwd);
    await pi.setActiveTools([...new Set([...pi.getActiveTools(), 'goal'])]);
    if (!pi.getActiveTools().includes('goal')) throw new Error('Native Goal tool is unavailable; enable the native goal capability before running');
    const live = await current(ctx);
    if (!armed || live.revision !== auth.revision) throw new Error('Authorization changed while starting work; no prompt was sent');
    commandContext = ctx;
    pi.sendUserMessage(`Execute only ${auth.currentItem} under ${auth.goalId}. Use native Goal primitives for this item's objective, not a project-owned conversation loop. Read its spec and tasks.md; preserve authorization revision ${auth.revision} and cumulative ${auth.budget.used}/${auth.budget.limit} execution steps. Use native task batches only for independent declared tasks with agent:'work-worker', isolated:true and auto-application disabled. Name workers by task ID without hyphens (T01, T02). The primary alone checks actual patches and integrates using work_checkpoint. Do not modify authorization manually, expand scope, push, merge or start other backlog items. Request work_checkpoint finish only after real acceptance evidence and task completion; finish the native Goal and yield so the native idle boundary can hand off. No full repeated review loop.`);
  }

  const onSession = async (_event: unknown, ctx: ExtensionContext) => {
    // Headless lifecycle events consume transfer intent without revoking the live primary.
    if (!ctx.hasUI) { transfer = false; transferParent = undefined; return; }
    const incoming = ctx.sessionManager.getSessionId();
    const continuing = transfer && transferParent !== undefined
      && incoming !== transferParent.id && ctx.sessionManager.getHeader()?.parentSession === transferParent.file;
    transfer = false;
    transferParent = undefined;
    armed = continuing;
    if (!continuing) { automatic = false; confirmed = undefined; }
    if (continuing) boundedSessions.add(incoming);
    await present(ctx);
  };
  pi.on('session_start', onSession);
  pi.on('session_switch', onSession);

  pi.on('tool_call', async (event, ctx) => {
    // Native session metadata distinguishes isolated workers from unattended primaries.
    // Headless mode by itself never grants write permission.
    if (!ctx.hasUI && isNativeWorker(ctx)) return;
    if (ctx.hasUI && !armed && !boundedSessions.has(ctx.sessionManager.getSessionId())) return;
    if (!armed && ['read', 'grep', 'glob', 'web_search', 'ask', 'todo', 'goal', 'advise'].includes(event.toolName)) return;
    if (!armed) return { block: true, reason: 'Work is not armed: use /work select or /work resume' };
    try {
      const auth = await current(ctx);
      showStatus(ctx, await work.charge(ctx.cwd, auth, 1));
      if (event.toolName === 'task') {
        const settings = SettingsManager.create(ctx.cwd);
        if (!settings.get('task.isolation.enabled') || settings.get('task.isolation.apply') !== false) throw new Error('Native worker isolation must be enabled with automatic application disabled');
        if (!Number.isSafeInteger(settings.get('task.maxRuntimeMs')) || settings.get('task.maxRuntimeMs') <= 0) throw new Error('Native write workers require a finite positive runtime limit');
        await validate(ctx.cwd);
        const project = await work.readProject(ctx.cwd);
        const item = project.items.find(i => i.id === auth.currentItem);
        const input = event.input as { tasks?: { name?: string; agent?: string; isolated?: boolean }[] };
        if (!input.tasks?.length || !item) throw new Error('Use a declared native task batch');
        const packets = input.tasks.map(packet => {
          const task = item.tasks.find(t => t.id.replace('-', '') === packet.name);
          if (!task || packet.agent !== 'work-worker' || packet.isolated !== true || task.isolation !== 'required' || !task.parallel || task.shared.length || task.agent === 'primary' || !['pending', 'ready', 'in-progress'].includes(task.status) || task.dependencies.some(id => !item.tasks.some(t => t.id === id && t.status === 'done'))) throw new Error('Worker requires the bounded work-worker role and eligible isolated ownership');
          return task;
        });
        if (new Set(packets.map(t => t.id)).size !== packets.length) throw new Error('Duplicate task dispatch');
        for (const task of packets) checkOwnership(task.writes.map(p => p.endsWith('/**') ? p.slice(0, -3) + '/ownership-check' : p), task.writes, packets.filter(t => t !== task).map(t => t.writes));
      }
    } catch (error) {
      const saved = await work.loadAuthorization(ctx.cwd).catch(() => null);
      if (armed && (!saved || saved.status !== 'active')) disarm(ctx);
      return { block: true, reason: error instanceof Error ? error.message : 'Work gate failed' };
    }
  });

  pi.on('tool_result', async (_event, ctx) => {
    if (!armed || !ctx.hasUI) return;
    const previous = confirmed;
    try {
      const saved = await work.loadAuthorization(ctx.cwd);
      if (!saved) throw new Error('Execution authorization disappeared');
      remember(ctx, saved, previous);
      if (saved.status === 'stopped') {
        disarm(ctx);
        showStatus(ctx, saved);
        announce(ctx, 'Execution-step budget exhausted; aborting the native turn. No fresh work started.');
      }
    } catch (error) {
      disarm(ctx);
      ctx.ui.notify(error instanceof Error ? error.message : 'Authorization check failed', 'error');
    }
  });

  const handle = async (args: string, ctx: ExtensionCommandContext) => {
      const [action = 'status', ...rest] = args.trim().split(/\s+/).filter(Boolean);
        if (action === 'status') { await present(ctx); return; }
        if (action === 'verify') { await validate(ctx.cwd); announce(ctx, 'Document validation PASS'); return; }
        if (!ctx.hasUI) throw new Error('Owner-facing work transitions require a native interactive session');
        if (action === 'pause') {
          boundedSessions.add(ctx.sessionManager.getSessionId());
          armed = false; automatic = false; transfer = false; ctx.abort();
          await work.pause(ctx.cwd, 'Owner pause');
          showStatus(ctx, await work.loadAuthorization(ctx.cwd));
          announce(ctx, `Paused durably. ${ctx.getAsyncJobSnapshot()?.running.length ?? 0} native jobs remain; settle them before handoff. Isolated patches cannot auto-apply.`);
          return;
        }
        if (action === 'resume') {
          const auth = await work.loadAuthorization(ctx.cwd);
          if (!auth) throw new Error('No work to resume');
          if (!await ctx.ui.confirm('Continue recorded work?', `${auth.goalId}: ${auth.allowedItems.join(', ')}; current ${auth.currentItem ?? 'none'}; budget ${auth.budget.used}/${auth.budget.limit} steps. Confirm this recorded scope and consumption; files alone grant nothing. ${rest[0] === 'auto' ? 'Allow internal fresh-session continuation.' : 'Interactive execution only.'}`)) return;
          await branchGuard(ctx.cwd, auth);
          await validate(ctx.cwd);
          if (auth.status === 'paused') await work.resume(ctx.cwd, auth, true);
          else await current(ctx);
          showStatus(ctx, await work.noteSession(ctx.cwd, await current(ctx), ctx.sessionManager.getSessionId()));
          confirmed = await current(ctx);
          automatic = rest[0] === 'auto';
          boundedSessions.add(ctx.sessionManager.getSessionId());
          armed = true;
          announce(ctx, 'Work confirmed for this session. /work run starts the primary.');
          return;
        }
        if (action === 'select' || action === 'auto') {
          const project = await work.readProject(ctx.cwd);
          const goalId = rest[0] ?? project.authorization?.goalId ?? project.activeGoal;
          if (!goalId) throw new Error('Specify a goal ID');
          const branch = await branchGuard(ctx.cwd, project.authorization ?? undefined);
          if (project.authorization?.currentItem) throw new Error('An active item exists; use resume or pause, not a new grant');
          let auth = project.authorization ?? draft(project, goalId, `${branch}@${await git(ctx.cwd, ['rev-parse', 'HEAD'])}`, 32);
          if (auth.goalId !== goalId) throw new Error('Changing the authorized goal requires a separate owner decision');
          const candidates = work.rank({ ...project, authorization: auth }, goalId);
          const labels = candidates.slice(0, 5).map((c, i) => `${c.id} - ${c.score.toFixed(0)}/100${i === 0 ? ' (Recommended)' : ''}`);
          const choice = await ctx.ui.select('Ranked eligible work', [...labels, 'Another AIU', 'Cancel']);
          if (!choice || choice === 'Cancel') { announce(ctx, 'Selection cancelled; no documents changed'); return; }
          const id = choice === 'Another AIU' ? await ctx.ui.input('AIU identifier') : choice.split(' ')[0];
          if (!id || !candidates.some(c => c.id === id)) throw new Error('Item is outside eligible authorized scope');
          if (!project.authorization) {
            const value = await ctx.ui.input('Budget: guarded native tool calls plus transitions (not tokens or cost)', '32');
            if (value === undefined) return;
            const limit = Number(value);
            if (!Number.isSafeInteger(limit) || limit < 2 || limit > 10000) throw new Error('Budget must be an integer from 2 through 10000');
            auth = { ...auth, budget: { ...auth.budget, limit } };
            if (!await ctx.ui.confirm('Authorize local work?', `${goalId}: ${auth.allowedItems.join(', ')}; ${limit} execution steps. No remote publication, paid resources or provider-account access. ${action === 'auto' ? 'Internal fresh-session continuation is allowed within this scope.' : 'Interactive execution only.'}`)) return;
          }
          await validate(ctx.cwd);
          await assertNoReparse(ctx.cwd, 'docs/product/goals.md');
          await work.select(ctx.cwd, id, auth);
          showStatus(ctx, await work.noteSession(ctx.cwd, await current(ctx), ctx.sessionManager.getSessionId()));
          confirmed = await current(ctx);
          boundedSessions.add(ctx.sessionManager.getSessionId());
          armed = true; automatic = action === 'auto';
          announce(ctx, `Selected ${id}; ${candidates.find(c => c.id === id)?.rationale.join('; ')}. /work run starts implementation.`);
          if (automatic) await start(ctx);
          return;
        }
        if (action === 'run') { await start(ctx); return; }
        const auth = await current(ctx);
        if (!armed) throw new Error('Confirm work in this session first');
        if (ctx.getAsyncJobSnapshot()?.running.length) throw new Error('Settle native workers before integration or handoff');
        await validate(ctx.cwd);
        if (action === 'integrate') {
          const [id, ...path] = rest;
          const item = (await work.readProject(ctx.cwd)).items.find(i => i.id === auth.currentItem);
          const task = item?.tasks.find(t => t.id === id);
          showStatus(ctx, await work.charge(ctx.cwd, auth, 1));
          if (!task || !path.length || task.isolation !== 'required' || task.shared.length || task.agent === 'primary' || !['pending', 'ready', 'in-progress'].includes(task.status) || task.dependencies.some(id => !item!.tasks.some(t => t.id === id && t.status === 'done'))) throw new Error('Expected an eligible isolated task ID and inspected patch path');
          const files = await integratePatch(ctx.cwd, path.join(' '), task.writes);
          announce(ctx, `Integrated ${files.join(', ')}. Run targeted checks and record evidence in tasks.md; integration is not completion.`);
          return;
        }
        if (action === 'finish') {
          showStatus(ctx, await work.complete(ctx.cwd, auth, rest.join(' ')));
          announce(ctx, 'Recorded completed item; no completed work will be selected again.');
          if (!automatic) return;
        } else if (action !== 'handoff') throw new Error('Unknown /work operation');
        if (!automatic) throw new Error('Internal handoff requires explicit /work auto authorization in this session');
        const before = await current(ctx);
        await work.handoff(ctx.cwd, before);
        const saved = await work.loadAuthorization(ctx.cwd);
        showStatus(ctx, saved);
        if (!saved || saved.status !== 'active') { armed = false; automatic = false; announce(ctx, 'Budget stop; no fresh work started'); return; }
        const candidates = work.rank(await work.readProject(ctx.cwd), saved.goalId);
        if (!saved.currentItem && !candidates.length) { armed = false; automatic = false; announce(ctx, 'Authorized goal scope exhausted. No next goal started.'); return; }
        const parentFile = ctx.sessionManager.getSessionFile();
        if (!parentFile) throw new Error('Internal handoff requires a native parent session file');
        transferParent = { id: ctx.sessionManager.getSessionId(), file: parentFile };
        transfer = true;
        try {
          const result = await ctx.newSession({ parentSession: parentFile });
          if (result.cancelled) { armed = false; automatic = false; return; }
        } catch (error) {
          armed = false; automatic = false;
          throw error;
        } finally {
          transfer = false;
          transferParent = undefined;
        }
        if (!armed || !automatic) throw new Error('Handoff interrupted; explicit resume required');
        const restored = await work.noteSession(ctx.cwd, saved, ctx.sessionManager.getSessionId());
        await branchGuard(ctx.cwd, saved);
        if (!restored.currentItem) await work.select(ctx.cwd, candidates[0]!.id, restored);
        await start(ctx);
  };
  pi.registerCommand('work', {
    description: 'Scoped work: status, select, run, auto, pause, resume, verify, finish, handoff, integrate',
    handler: async (args, ctx) => {
      try { await handle(args, ctx); }
      catch (error) { ctx.ui.notify(error instanceof Error ? error.message : 'Work operation failed', 'error'); }
    },
  });
  pi.registerTool({
    name: 'work_checkpoint',
    label: 'Work checkpoint',
    description: 'Primary-only checked patch integration or completion request. Completion waits for native idle, then revalidates evidence and bounded authorization. Never grants new scope.',
    parameters: pi.typebox.Type.Object({
      operation: pi.typebox.Type.Union([pi.typebox.Type.Literal('integrate'), pi.typebox.Type.Literal('finish')]),
      task: pi.typebox.Type.Optional(pi.typebox.Type.String()),
      patch: pi.typebox.Type.Optional(pi.typebox.Type.String()),
      evidence: pi.typebox.Type.Optional(pi.typebox.Type.String()),
    }),
    async execute(_id, params, signal, _update, ctx) {
      if (!ctx.hasUI || !armed || !commandContext) throw new Error('No authorized primary command context');
      if (params.operation === 'integrate') {
        if (!params.task || !params.patch) throw new Error('Task ID and patch path required');
        await handle(`integrate ${params.task} ${params.patch}`, commandContext);
        return { content: [{ type: 'text', text: 'Patch integrated. Run targeted checks and record evidence; this is not task completion.' }] };
      }
      if (!params.evidence?.trim() || finishing) throw new Error('Evidence required; only one completion request may be pending');
      const ownerContext = commandContext;
      const sessionId = ctx.sessionManager.getSessionId();
      finishing = true;
      void ownerContext.waitForIdle().then(async () => {
        if (signal?.aborted || !armed || sessionId !== ownerContext.sessionManager.getSessionId()) throw new Error('Completion cancelled by pause or session change');
        await handle(`finish ${params.evidence}`, ownerContext);
      }).catch(error => ctx.ui.notify(error instanceof Error ? error.message : 'Completion failed', 'error')).finally(() => { finishing = false; });
      return { content: [{ type: 'text', text: 'Completion requested, not completed. Finish the native Goal and yield. Evidence and authorization will be checked again at native idle.' }] };
    },
  });
}
