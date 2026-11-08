// VK Mini Apps bridge for PlatformTools.
// vkBridge is loaded by the VK WebGL template, which also calls VKWebAppInit
// right away so VK hides its own loading screen. Every function here checks
// for the bridge and fails softly when it's missing.
mergeInto(LibraryManager.library, {
  $ptvk: {
    ready: function () { return typeof vkBridge !== 'undefined' && window.ptVkReady === true; },
    reply: function (cb, id, ok, text) {
      var ptr = 0;
      if (text !== null && text !== undefined) ptr = stringToNewUTF8(String(text));
      {{{ makeDynCall('viii', 'cb') }}}(id, ok ? 1 : 0, ptr);
      if (ptr) _free(ptr);
    }
  },

  PTVK_IsAvailable__deps: ['$ptvk'],
  PTVK_IsAvailable: function () {
    return ptvk.ready() ? 1 : 0;
  },

  PTVK_LaunchParam: function (namePtr) {
    var value = '';
    try { value = new URLSearchParams(window.location.search).get(UTF8ToString(namePtr)) || ''; } catch (e) { }
    return stringToNewUTF8(value);
  },

  PTVK_GetUserInfo__deps: ['$ptvk'],
  PTVK_GetUserInfo: function (id, cb) {
    if (!ptvk.ready()) { ptvk.reply(cb, id, false, 'no_bridge'); return; }
    vkBridge.send('VKWebAppGetUserInfo')
      .then(function (data) { ptvk.reply(cb, id, true, JSON.stringify(data)); })
      .catch(function (e) { console.warn('[VK] GetUserInfo', e); ptvk.reply(cb, id, false, 'error'); });
  },

  PTVK_ShowAd__deps: ['$ptvk'],
  PTVK_ShowAd: function (id, formatPtr, cb) {
    var format = UTF8ToString(formatPtr); // "interstitial" or "reward"
    if (!ptvk.ready()) { ptvk.reply(cb, id, false, 'no_bridge'); return; }
    vkBridge.send('VKWebAppShowNativeAds', { ad_format: format })
      .then(function (data) { ptvk.reply(cb, id, !!(data && data.result), null); })
      .catch(function (e) { console.warn('[VK] ShowNativeAds', format, e); ptvk.reply(cb, id, false, 'error'); });
  },

  PTVK_SubscribePause: function (cb) {
    function send(paused) { {{{ makeDynCall('vi', 'cb') }}}(paused ? 1 : 0); }
    if (typeof vkBridge !== 'undefined') {
      vkBridge.subscribe(function (e) {
        var type = e && e.detail && e.detail.type;
        if (type === 'VKWebAppViewHide') send(true);
        else if (type === 'VKWebAppViewRestore') send(false);
      });
    }
    document.addEventListener('visibilitychange', function () { send(document.hidden); });
  }
});
