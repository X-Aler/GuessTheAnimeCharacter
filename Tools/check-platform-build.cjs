const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');

const [folder, platform] = process.argv.slice(2);
assert.ok(folder && ['CrazyGames', 'YandexGames'].includes(platform),
    'Usage: node Tools/check-platform-build.cjs Builds/CrazyGames CrazyGames');
const result = JSON.parse(fs.readFileSync(path.join(folder, 'platform-build-result.json')));
assert.equal(result.platform, platform);
assert.equal(result.result, 'Succeeded');
assert.equal(result.errors, 0);
const files = fs.readdirSync(path.join(folder, 'Build'));
const framework = files.find(name => name.endsWith('.framework.js.br'));
assert.ok(framework, 'Expected Brotli framework without decompression fallback');
const source = zlib.brotliDecompressSync(fs.readFileSync(path.join(folder, 'Build', framework))).toString();
const html = fs.readFileSync(path.join(folder, 'index.html'), 'utf8');
if (platform === 'CrazyGames') {
    assert.ok(source.includes('crazygames-sdk-v3.js'), 'SDK download must be inside the framework');
    assert.ok(source.includes('CrazySDKSingleton'), 'Official Unity SDK callback target must be present');
    assert.ok(!source.includes('cgSdkReady'), 'Obsolete HTML-dependent bridge must be absent');
    assert.ok(!/\bysdk\b|YaGames|YandexGames|InitGame_js/.test(source), 'Yandex native code must be absent');
    assert.ok(!/ysdk|yandex|cgSdkReady/i.test(html), 'Incorrect HTML template');
} else {
    assert.ok(/ysdk|YaGames/.test(source), 'Expected Yandex bridge');
    assert.ok(!/crazygames-sdk|CrazySDKSingleton|cgSdkReady/.test(source), 'CrazyGames native code must be absent');
    assert.ok(!html.includes('crazygames-sdk'), 'Incorrect HTML template');
}
console.log(`${platform}: build succeeded; compressed framework contains the selected SDK and excludes the other bridge.`);
