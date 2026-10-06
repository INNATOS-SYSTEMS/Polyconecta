// npm run parity [-- --routes=/pedidos,/fabricacion/BOL-2026-0001]
import { spawnSync } from 'node:child_process';
const arg = process.argv.find(a => a.startsWith('--routes='));
const env = { ...process.env, ...(arg ? { PARITY_ROUTES: arg.slice('--routes='.length) } : {}) };
const r = spawnSync('npx', ['playwright', 'test', '--config', 'e2e/playwright.config.ts', 'parity/'], { stdio: 'inherit', env });
process.exit(r.status ?? 1);
