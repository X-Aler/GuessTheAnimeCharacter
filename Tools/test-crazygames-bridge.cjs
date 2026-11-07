const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync('Assets/PluginYourGames/Platforms/CrazyGames/Plugins/CrazyGames.jslib', 'utf8');
function harness({ locale = 'ru-RU', unavailable = false, rejected = false, request } = {}) {
    const messages = [], events = [], listeners = {};
    let callbacks;
    const sdk = {
        game: { settings: { muteAudio: true }, addSettingsChangeListener: f => listeners.settings = f,
            loadingStop: () => events.push('ready'), gameplayStart: () => events.push('start'),
            gameplayStop: () => events.push('stop'), happytime: () => events.push('happy') },
        user: { get systemInfo() { if (unavailable) throw Error('Unavailable'); return { locale }; } },
        ad: { requestAd: (type, cb) => { assert.equal(type, 'midgame'); callbacks = cb; if (request) request(cb); } }
    };
    const library = {};
    const window = { CrazyGames: { SDK: sdk }, cgSdkReady: rejected ? Promise.reject(Error('Init failed')) : Promise.resolve(sdk), cgShowError: e => events.push(e) };
    vm.runInNewContext(source, { window, document: { hidden: false, addEventListener: (n, f) => listeners[n] = f },
        SendMessage: (target, method, value) => { assert.equal(target, 'YG2Instance'); messages.push([method, value]); },
        LibraryManager: { library }, mergeInto: Object.assign, console: { warn() {} } });
    return { library, window, messages, events, listeners, get callbacks() { return callbacks; } };
}
(async () => {
    for (const locale of ['ru-RU', 'en-US', '', null]) {
        const h = harness({ locale }); h.library.CG_Init(); await new Promise(setImmediate);
        assert.equal(h.messages.find(m => m[0] === 'CrazyInit')[1], locale || 'en');
        assert.deepEqual(h.messages[0], ['CrazyMute', 'true']);
        h.listeners.settings({ muteAudio: false });
        assert.deepEqual(h.messages.at(-1), ['CrazyMute', 'false']);
        for (let kind = 0; kind < 4; kind++) h.library.CG_Event(kind);
        assert.deepEqual(h.events, ['ready', 'start', 'stop', 'happy']);
    }
    const fallback = harness({ unavailable: true }); fallback.library.CG_Init(); await new Promise(setImmediate);
    assert.ok(fallback.messages.some(m => m[0] === 'CrazyInit' && m[1] === 'en'));
    const failed = harness({ rejected: true }); failed.library.CG_Init(); await new Promise(setImmediate);
    assert.ok(failed.messages.some(m => m[0] === 'CrazyInitError'));
    assert.ok(!failed.messages.some(m => m[0] === 'CrazyInit'));
    const ad = harness(); ad.library.CG_Interstitial(); ad.library.CG_Interstitial();
    assert.equal(ad.messages.length, 1); // pending requests cannot overlap
    ad.callbacks.adStarted(); ad.callbacks.adFinished(); ad.callbacks.adError(Error('Late duplicate'));
    assert.deepEqual(ad.messages, [['CrazyAdOpen', undefined], ['CrazyAdClose', 'true']]);
    assert.equal(ad.window.cgAdPending, false);
    const cancelled = harness(); cancelled.library.CG_Interstitial(); cancelled.callbacks.adError(Error('No fill'));
    assert.deepEqual(cancelled.messages.map(m => m[0]), ['CrazyAdOpen', 'CrazyAdClose', 'CrazyAdError']);
    assert.equal(cancelled.messages[1][1], 'false');
    const thrown = harness({ request: () => { throw Error('Request failed'); } }); thrown.library.CG_Interstitial();
    assert.equal(thrown.window.cgAdPending, false);
    assert.equal(thrown.messages[1][1], 'false');
    console.log('CrazyGames bridge: initialization, locale fallback, mute, lifecycle, overlapping ads, success, errors and duplicate callbacks passed.');
})().catch(error => { console.error(error); process.exitCode = 1; });
