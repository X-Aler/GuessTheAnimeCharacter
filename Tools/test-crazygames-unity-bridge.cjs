const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

(async () => {
    const library = {}, messages = [], scripts = [], settingsListeners = [];
    const sdk = {
        init: async options => {
            assert.equal(options.wrapper.engine, 'unity');
            assert.equal(options.wrapper.sdkVersion, '5.36.0');
        },
        game: {
            addSettingsChangeListener: listener => settingsListeners.push(listener),
            addQualityChangeListener() {},
        },
        ad: { hasAdblock: async () => false, addAdblockPopupListener() {} },
    };
    // Deliberately no custom HTML globals and no preloaded SDK.
    const window = { location: { href: 'https://game-files.crazygames.com/quiz/' } };
    const document = {
        createElement: () => ({ addEventListener(event, callback) {
            if (event === 'load') setImmediate(() => { window.CrazyGames = { SDK: sdk }; callback(); });
        } }),
        head: { appendChild: script => scripts.push(script) },
    };
    vm.runInNewContext(fs.readFileSync('Assets/Plugins/crazySDK.jslib', 'utf8'), {
        window, document, UTF8ToString: value => value, LibraryManager: { library }, mergeInto: Object.assign,
        SendMessage: (target, method, data) => messages.push({ target, method, data }), console,
    });
    assert.doesNotThrow(() => library.InitSDK('5.36.0'));
    await new Promise(setImmediate);
    await new Promise(setImmediate);
    assert.equal(scripts.length, 1);
    assert.equal(scripts[0].src, 'https://sdk.crazygames.com/crazygames-sdk-v3.js');
    assert.equal(window.UnitySDK.version, '5.36.0');
    assert.ok(messages.some(m => m.target === 'CrazySDKSingleton' && m.method === 'JSLibCallback_Init'));
    settingsListeners[0]({ muteAudio: true });
    assert.ok(messages.some(m => m.method === 'JSLibCallback_SettingsChangeListener' && JSON.parse(m.data).muteAudio));
    console.log('Official CrazyGames Unity bridge: SDK download/init and settings callbacks work without index.html.');
})().catch(error => { console.error(error); process.exitCode = 1; });
