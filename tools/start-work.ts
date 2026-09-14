import { existsSync } from 'node:fs';
import { delimiter, join } from 'node:path';

// OMP 18.1.18 probes isolation ownership with Unix ps on non-Linux hosts.
// MSYS ps can hang on native Windows PIDs. Give OMP a native Windows PATH;
// its supported unavailable-start-token fallback retains PID liveness checks.
// This changes only this child process, never the user's profile or machine PATH.
const environment = { ...process.env };
if (process.platform === 'win32') {
  const nativePath = (process.env.PATH ?? '').split(delimiter).filter(directory =>
    !directory || !['msys-2.0.dll', 'cygwin1.dll'].some(runtime => existsSync(join(directory, runtime))),
  ).join(delimiter);
  for (const key of Object.keys(environment)) if (key.toLowerCase() === 'path') delete environment[key];
  environment.PATH = nativePath;
}
const child = Bun.spawn(['omp', '--profile', 'ai-usage', ...Bun.argv.slice(2)], {
  env: environment,
  stdin: 'inherit',
  stdout: 'inherit',
  stderr: 'inherit',
});
process.exit(await child.exited);
