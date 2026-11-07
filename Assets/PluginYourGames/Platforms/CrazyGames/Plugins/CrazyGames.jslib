mergeInto(LibraryManager.library, {
    CG_Init: function () {
        window.cgSdkReady.then(function (sdk) {
            var send = function (method, value) { SendMessage('YG2Instance', method, value); };
            var mute = function (settings) { send('CrazyMute', String(!!settings.muteAudio)); };
            mute(sdk.game.settings);
            sdk.game.addSettingsChangeListener(mute);
            var locale = 'en';
            try {
                var info = sdk.user.systemInfo;
                locale = info && info.locale || 'en';
            } catch (error) { console.warn('CrazyGames locale unavailable, using English', error); }
            send('CrazyInit', locale);
            document.addEventListener('visibilitychange', function () {
                send('CrazyFocus', String(!document.hidden));
            });
            send('CrazyFocus', String(!document.hidden));
        }).catch(function (error) {
            SendMessage('YG2Instance', 'CrazyInitError', String(error));
            window.cgShowError('Could not connect to CrazyGames. Please reload the page.');
        });
    },
    CG_Event: function (kind) {
        var game = window.CrazyGames.SDK.game;
        if (kind === 0) game.loadingStop();
        if (kind === 1) game.gameplayStart();
        if (kind === 2) game.gameplayStop();
        if (kind === 3) game.happytime();
    },
    CG_Interstitial: function () {
        if (window.cgAdPending) return;
        window.cgAdPending = true;
        var finished = false;
        var opened = false;
        // Pause input and audio while waiting too; terminal callbacks restore the saved state.
        SendMessage('YG2Instance', 'CrazyAdOpen');
        var finish = function (shown, error) {
            if (finished) return;
            finished = true;
            window.cgAdPending = false;
            SendMessage('YG2Instance', 'CrazyAdClose', shown ? 'true' : 'false');
            if (error) SendMessage('YG2Instance', 'CrazyAdError', String(error));
        };
        try {
            window.CrazyGames.SDK.ad.requestAd('midgame', {
                adStarted: function () { opened = true; },
                adFinished: function () { finish(opened); },
                adError: function (error) { finish(false, error); }
            });
        } catch (error) { finish(false, error); }
    }
});
