// Fails when the committed NSwag clients no longer match the API. Regenerates both documents into
// a temporary folder and compares them byte for byte with src/app/api/generated. Run after every
// API change: `npm run api:generate` fixes what this reports.
import { execSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, readdirSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

const projectRoot = resolve(import.meta.dirname, '..');
const generatedDir = join(projectRoot, 'src', 'app', 'api', 'generated');
const tempDir = mkdtempSync(join(tmpdir(), 'surac-dev-contract-'));

try {
  for (const document of ['public', 'admin']) {
    execSync(`npx nswag run nswag.${document}.json /variables:OutputDir=${JSON.stringify(tempDir)}`, {
      cwd: projectRoot,
      stdio: 'inherit',
    });
  }
  // Prettier resolves its config from the file's location; the temp folder has none, so pass it.
  execSync(`npx prettier --config .prettierrc.json --write ${JSON.stringify(join(tempDir, '*.ts'))}`, {
    cwd: projectRoot,
    stdio: 'inherit',
  });

  const normalize = (text) => text.replace(/\r\n/g, '\n');
  const stale = readdirSync(tempDir).filter((file) => {
    const fresh = readFileSync(join(tempDir, file), 'utf8');
    try {
      return normalize(fresh) !== normalize(readFileSync(join(generatedDir, file), 'utf8'));
    } catch {
      return true;
    }
  });

  if (stale.length > 0) {
    console.error(`API contract drift. Regenerate with "npm run api:generate":\n  ${stale.join('\n  ')}`);
    for (const file of stale) {
      // Exit code 1 is git's "differences found", not an error.
      try {
        execSync(
          `git --no-pager diff --no-index -- ${JSON.stringify(join(generatedDir, file))} ${JSON.stringify(join(tempDir, file))}`,
          {
            cwd: projectRoot,
            stdio: 'inherit',
          },
        );
      } catch {
        // diff output already printed
      }
    }
    process.exit(1);
  }
  console.log('API contract up to date.');
} finally {
  rmSync(tempDir, { recursive: true, force: true });
}
