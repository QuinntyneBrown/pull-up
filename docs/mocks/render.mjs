// Render every ./docs/mocks/*.html into 3 PNG screenshots
// (360, 768, 1440 widths) under ./docs/mocks/screenshots/.
// Runs Chromium via the Playwright Node API. No bundler required.
//
// Usage:  node ./docs/mocks/render.mjs

import { chromium } from 'playwright';
import { readdir, mkdir, stat } from 'node:fs/promises';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join, basename, extname } from 'node:path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const MOCKS_DIR = __dirname;
const OUT_DIR = join(MOCKS_DIR, 'screenshots');

const BREAKPOINTS = [
  { width: 360,  height: 800,  label: '360'  },
  { width: 768,  height: 1024, label: '768'  },
  { width: 1440, height: 900,  label: '1440' },
];

async function listMocks() {
  const entries = await readdir(MOCKS_DIR);
  return entries
    .filter(name => extname(name).toLowerCase() === '.html')
    .map(name => join(MOCKS_DIR, name));
}

async function ensureOutDir() {
  try {
    await stat(OUT_DIR);
  } catch {
    await mkdir(OUT_DIR, { recursive: true });
  }
}

async function renderOne(browser, htmlPath, viewport) {
  const context = await browser.newContext({ viewport: { width: viewport.width, height: viewport.height } });
  const page = await context.newPage();
  const url = pathToFileURL(htmlPath).href;
  await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 });
  // Give web fonts a moment to settle (already covered by networkidle, but belt-and-suspenders).
  await page.waitForTimeout(250);

  const name = basename(htmlPath, '.html');
  const outFile = join(OUT_DIR, `${name}.${viewport.label}.png`);
  await page.screenshot({ path: outFile, fullPage: true });
  await context.close();
  return outFile;
}

async function main() {
  await ensureOutDir();
  const mocks = await listMocks();
  if (mocks.length === 0) {
    console.error('No HTML mocks found in', MOCKS_DIR);
    process.exit(1);
  }

  console.log(`Rendering ${mocks.length} mock(s) at ${BREAKPOINTS.length} breakpoint(s)...`);
  const browser = await chromium.launch();
  try {
    for (const htmlPath of mocks) {
      for (const bp of BREAKPOINTS) {
        const out = await renderOne(browser, htmlPath, bp);
        console.log('  wrote', out);
      }
    }
  } finally {
    await browser.close();
  }
  console.log('Done.');
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
