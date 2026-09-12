import { lstat, realpath, readFile } from 'node:fs/promises';
import { resolve, relative, dirname, sep } from 'node:path';

export function canonicalPath(value: string): string {
  const path = value.replaceAll('\\', '/');
  if (!path || path.startsWith('/') || path.startsWith('~') || /[:<>"|*?\x00-\x1f]/.test(path)) throw new Error('Unsafe relative path');
  const parts = path.split('/');
  if (parts.some(p => !p || p === '.' || p === '..' || /[. ]$/.test(p) || p.toLowerCase() === '.git' || /^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)/i.test(p))) throw new Error('Unsafe Windows path component');
  return path.toLowerCase();
}

function owned(path: string, pattern: string): boolean {
  const root = canonicalPath(pattern.endsWith('/**') ? pattern.slice(0, -3) : pattern);
  if (/[*?\[\]{}]/.test(root)) throw new Error('Uncertain glob ownership must be serialized');
  return path === root || (pattern.endsWith('/**') && path.startsWith(root + '/'));
}

export function checkOwnership(files: string[], writes: string[], otherWrites: string[][] = []): void {
  if (!files.length || !writes.length) throw new Error('Empty patch or ownership');
  const roots = writes.map(p => canonicalPath(p.endsWith('/**') ? p.slice(0, -3) : p));
  if (roots.some(r => /[\[\]{}]/.test(r))) throw new Error('Uncertain glob ownership must be serialized');
  for (const other of otherWrites.flat()) {
    const candidate = canonicalPath(other.endsWith('/**') ? other.slice(0, -3) : other);
    if (/[\[\]{}]/.test(candidate)) throw new Error('Uncertain glob ownership must be serialized');
    if (roots.some(r => r === candidate || r.startsWith(candidate + '/') || candidate.startsWith(r + '/'))) throw new Error('Overlapping worker ownership');
  }
  const seen = new Set<string>();
  for (const file of files) {
    const path = canonicalPath(file);
    if (seen.has(path)) throw new Error('Case-colliding patch paths');
    seen.add(path);
    if (path === 'docs/backlog.md' || path === 'docs/product/goals.md' || path.endsWith('/tasks.md') || path.startsWith('.github/') || path.startsWith('.omp/') || path === '.gitignore') throw new Error('Primary-owned path in worker patch');
    if (!writes.some(w => owned(path, w))) throw new Error(`Out-of-scope patch: ${file}`);
  }
}

export async function assertNoReparse(root: string, file: string): Promise<void> {
  canonicalPath(file);
  const base = await realpath(root);
  let cursor = resolve(base, file);
  while (cursor !== base) {
    if (relative(base, cursor).startsWith('..' + sep)) throw new Error('Path escapes root');
    try {
      const info = await lstat(cursor);
      if (info.isSymbolicLink()) throw new Error('Reparse/symlink path rejected');
      const actual = await realpath(cursor);
      if (actual !== base && !actual.toLowerCase().startsWith((base + sep).toLowerCase())) throw new Error('Reparse target escapes root');
    } catch (error: unknown) {
      if (!(error instanceof Error && 'code' in error && error.code === 'ENOENT')) throw error;
    }
    cursor = dirname(cursor);
  }
}

async function git(root: string, args: string[], input?: string): Promise<string> {
  const child = Bun.spawn(['git', '-C', root, ...args], { stdin: input === undefined ? 'ignore' : new Blob([input]), stdout: 'pipe', stderr: 'pipe' });
  const [stdout, stderr, exit] = await Promise.all([new Response(child.stdout).text(), new Response(child.stderr).text(), child.exited]);
  if (exit) throw new Error(stderr.trim() || 'Git patch check failed');
  return stdout;
}

// This is an integration gate, not confinement of the worker process.
async function inspectText(root: string, patch: string, writes: string[], otherWrites: string[][]): Promise<string[]> {
  if (/^(?:new file mode|old mode|new mode|deleted file mode) (?:120000|160000)|^GIT binary patch|^Binary files /m.test(patch)) throw new Error('Link, submodule or binary patch rejected');
  const stats = await git(root, ['apply', '--numstat', '-z', '-'], patch);
  const files = stats.split('\0').filter(Boolean).map(row => {
    const match = /^\d+\t\d+\t([^\0]+)$/.exec(row);
    if (!match) throw new Error('Unsupported patch path or rename');
    return match[1];
  });
  checkOwnership(files, writes, otherWrites);
  for (const file of files) await assertNoReparse(root, file);
  await git(root, ['apply', '--check', '-'], patch);
  return files;
}

export async function inspectPatch(root: string, patchFile: string, writes: string[], otherWrites: string[][] = []): Promise<string[]> {
  return inspectText(root, await readFile(patchFile, 'utf8'), writes, otherWrites);
}

export async function integratePatch(root: string, patchFile: string, writes: string[]): Promise<string[]> {
  const branch = (await git(root, ['branch', '--show-current'])).trim();
  if (!branch.startsWith('feature/AIU-')) throw new Error('Integration requires the selected feature branch');
  const patch = await readFile(patchFile, 'utf8');
  const files = await inspectText(root, patch, writes, []);
  await git(root, ['apply', '-'], patch);
  return files;
}
