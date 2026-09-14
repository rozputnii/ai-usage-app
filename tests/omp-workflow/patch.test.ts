import { test, expect } from 'bun:test';
import { mkdtemp, mkdir, writeFile, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { checkOwnership, canonicalPath, integratePatch, inspectPatch } from '../../.omp/lib/patch';

test('worker paths stay within disjoint ownership and outside primary state', () => {
  checkOwnership(['scratch/left/result.txt'], ['scratch/left/**'], [['scratch/right/**']]);
  for (const path of ['../escape', 'C:/escape', '/escape', '~/.ssh', 'a/../b', '.GIT/config', 'a/file:stream', 'a/NUL.txt', 'a/trailing.']) expect(() => canonicalPath(path)).toThrow();
  expect(() => checkOwnership(['scratch/right/file'], ['scratch/left/**'])).toThrow('Out-of-scope');
  expect(() => checkOwnership(['scratch/left/a'], ['scratch/left/**'], [['SCRATCH/**']])).toThrow('Overlapping');
  expect(() => checkOwnership(['scratch/left/a'], ['scratch/left/**'], [['scratch/[a-z]*/**']])).toThrow();
  expect(() => checkOwnership(['docs/backlog.md'], ['docs/**'])).toThrow('Primary-owned');
  expect(() => checkOwnership(['scratch/A', 'scratch/a'], ['scratch/**'])).toThrow('Case-colliding');
});

test('primary checks feature branch, actual patch and clean application before integration', async () => {
  const root = await mkdtemp(join(tmpdir(), 'aiu-patch-'));
  const git = async (...args: string[]) => {
    const p = Bun.spawn(['git', '-C', root, ...args], { stdout: 'pipe', stderr: 'pipe' });
    expect(await p.exited).toBe(0);
  };
  try {
    await git('init', '-b', 'feature/AIU-001-disposable');
    await mkdir(join(root, 'scratch'), { recursive: true });
    const patch = join(root, 'change.patch');
    await writeFile(patch, 'diff --git a/scratch/result.txt b/scratch/result.txt\nnew file mode 100644\n--- /dev/null\n+++ b/scratch/result.txt\n@@ -0,0 +1 @@\n+isolated result\n');
    await expect(inspectPatch(root, patch, ['different/**'])).rejects.toThrow('Out-of-scope');
    await expect(readFile(join(root, 'scratch/result.txt'))).rejects.toThrow();
    expect(await integratePatch(root, patch, ['scratch/**'])).toEqual(['scratch/result.txt']);
    expect(await readFile(join(root, 'scratch/result.txt'), 'utf8')).toBe('isolated result\n');
    await expect(integratePatch(root, patch, ['scratch/**'])).rejects.toThrow();
    await writeFile(patch, 'diff --git a/scratch/link b/scratch/link\nnew file mode 120000\n--- /dev/null\n+++ b/scratch/link\n@@ -0,0 +1 @@\n+../../outside\n');
    await expect(inspectPatch(root, patch, ['scratch/**'])).rejects.toThrow('Link');
    await writeFile(patch, 'diff --git a/scratch/next.txt b/scratch/next.txt\nnew file mode 100644\n--- /dev/null\n+++ b/scratch/next.txt\n@@ -0,0 +1 @@\n+next result\n');
    await git('symbolic-ref', 'HEAD', 'refs/heads/main');
    expect(await inspectPatch(root, patch, ['scratch/**'])).toEqual(['scratch/next.txt']);
    await expect(integratePatch(root, patch, ['scratch/**'])).rejects.toThrow();
    await expect(readFile(join(root, 'scratch/next.txt'))).rejects.toThrow();
  } finally { await rm(root, { recursive: true, force: true }); }
});
