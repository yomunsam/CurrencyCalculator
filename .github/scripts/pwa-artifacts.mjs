import { readFile, writeFile, access } from 'node:fs/promises';
import { resolve, relative, isAbsolute } from 'node:path';
import { pathToFileURL } from 'node:url';
import { createHash } from 'node:crypto';
import { gzipSync, brotliCompressSync } from 'node:zlib';

const hash = bytes => `sha256-${createHash('sha256').update(bytes).digest('base64')}`;

async function writeWithCompression(file, content) {
    await writeFile(file, content);
    // 修改发布文件时同步已有的压缩副本，避免静态服务器发出旧内容。
    for (const [extension, compress] of [['.gz', gzipSync], ['.br', brotliCompressSync]]) {
        try { await access(file + extension); }
        catch { continue; }
        await writeFile(file + extension, compress(Buffer.from(content)));
    }
}

export async function prepareArtifacts(directory, basePath) {
    const root = resolve(directory);
    const manifestFile = resolve(root, 'service-worker-assets.js');
    const text = await readFile(manifestFile, 'utf8');
    const match = text.match(/^\s*self\.assetsManifest\s*=\s*({[\s\S]*})\s*;?\s*$/);
    if (!match) throw new Error('Unrecognized service-worker asset manifest');
    const manifest = JSON.parse(match[1]);
    if (basePath !== undefined) {
        if (!/^\/(?:[\w.-]+\/)*$/.test(basePath)) throw new Error('Invalid application base path');
        const indexFile = resolve(root, 'index.html');
        const html = await readFile(indexFile, 'utf8');
        if (!/<base\s+href="[^"]*"\s*\/?\s*>/.test(html)) throw new Error('Missing base href');
        const updated = html.replace(/<base\s+href="[^"]*"\s*\/?\s*>/, `<base href="${basePath}" />`);
        const indexAsset = manifest.assets.find(asset => asset.url === 'index.html');
        if (!indexAsset) throw new Error('index.html is missing from the offline manifest');
        indexAsset.hash = hash(updated);
        manifest.version = createHash('sha256').update(JSON.stringify(manifest.assets)).digest('base64url').slice(0, 20);
        await writeWithCompression(indexFile, updated);
        await writeWithCompression(manifestFile, `self.assetsManifest = ${JSON.stringify(manifest, null, 2)};\n`);
    }
    // 直接核对最终发布产物，不能假设文本替换成功就等于离线缓存可安装。
    for (const asset of manifest.assets) {
        const file = resolve(root, asset.url);
        const local = relative(root, file);
        if (local.startsWith('..') || isAbsolute(local)) throw new Error(`Asset escapes publish root: ${asset.url}`);
        if (hash(await readFile(file)) !== asset.hash) throw new Error(`Integrity mismatch: ${asset.url}`);
    }
    for (const required of ['index.html', 'fallback/latest-rates.json', 'lib/bootstrap-icons/fonts/bootstrap-icons.woff2']) {
        if (!manifest.assets.some(asset => asset.url === required)) throw new Error(`Missing offline asset: ${required}`);
    }
    return manifest;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    const manifest = await prepareArtifacts(process.argv[2], process.argv[3]);
    console.log(`Verified ${manifest.assets.length} offline assets, version ${manifest.version}`);
}
