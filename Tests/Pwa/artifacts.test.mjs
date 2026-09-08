import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createHash } from 'node:crypto';
import { gzipSync, gunzipSync } from 'node:zlib';
import { prepareArtifacts } from '../../.github/scripts/pwa-artifacts.mjs';

test('子路径重写更新入口哈希、版本及压缩副本，并验证全部资源', async t => {
    const root = await mkdtemp(join(tmpdir(), 'cc-pwa-'));
    t.after(() => rm(root, { recursive: true }));
    const html = '<html><base href="/" /></html>';
    const assets = [];
    for (const [url, content] of [
        ['index.html', html], ['fallback/latest-rates.json', '{}'],
        ['lib/bootstrap-icons/fonts/bootstrap-icons.woff2', 'font']
    ]) {
        await mkdir(join(root, url, '..'), { recursive: true });
        await writeFile(join(root, url), content);
        assets.push({ url, hash: `sha256-${createHash('sha256').update(content).digest('base64')}` });
    }
    await writeFile(join(root, 'index.html.gz'), gzipSync(html));
    await writeFile(join(root, 'service-worker-assets.js'), `self.assetsManifest = ${JSON.stringify({ version: 'old', assets }, null, 2)};`);
    const manifest = await prepareArtifacts(root, '/CurrencyCalculator/');
    assert.notEqual(manifest.version, 'old');
    assert.match(await readFile(join(root, 'index.html'), 'utf8'), /href="\/CurrencyCalculator\/"/);
    assert.equal(gunzipSync(await readFile(join(root, 'index.html.gz'))).toString(), await readFile(join(root, 'index.html'), 'utf8'));
    await writeFile(join(root, 'fallback/latest-rates.json'), 'changed');
    await assert.rejects(prepareArtifacts(root), /Integrity mismatch/);
});
