import { access } from 'node:fs/promises';
import { homedir } from 'node:os';
import { join, resolve } from 'node:path';

export async function dotnetCommand(): Promise<string> {
  const name = process.platform === 'win32' ? 'dotnet.exe' : 'dotnet';
  const configured = process.env.DOTNET_ROOT;
  if (configured) return join(configured, name);
  const local = join(homedir(), '.dotnet', 'ai-usage-sdk', name);
  try { await access(local); return local; } catch { return name; }
}

export async function validate(root: string): Promise<void> {
  const project = resolve(import.meta.dir, '../../tools/AiUsage.ProjectValidation');
  const child = Bun.spawn([await dotnetCommand(), 'run', '--project', project, '--no-restore', '--', '--root', root, '--json'], {
    cwd: root,
    env: { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_GENERATE_ASPNET_CERTIFICATE: 'false' },
    stdout: 'pipe', stderr: 'pipe',
  });
  const [stdout, stderr, exit] = await Promise.all([new Response(child.stdout).text(), new Response(child.stderr).text(), child.exited]);
  if (exit !== 0) throw new Error(`Document validation failed (${exit}): ${stdout.trim()} ${stderr.trim()}`);
}
