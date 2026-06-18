mergeInto(LibraryManager.library, {

  JS_NotifyStateChange: function (statePtr) {
    var state = UTF8ToString(statePtr);
    if (window.unityBridge && window.unityBridge.onStateChange) {
      window.unityBridge.onStateChange(state);
    }
  },

  JS_NotifyBookHighlight: function (titlePtr) {
    var title = UTF8ToString(titlePtr);
    if (window.unityBridge && window.unityBridge.onBookHighlight) {
      window.unityBridge.onBookHighlight(title);
    }
  },

  JS_NotifyCoverUrl: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    if (window.unityBridge && window.unityBridge.onCoverUrl) {
      window.unityBridge.onCoverUrl(url);
    }
  },

  JS_NotifyCoverLoading: function (loading) {
    if (window.unityBridge && window.unityBridge.onCoverLoading) {
      window.unityBridge.onCoverLoading(!!loading);
    }
  },

  JS_NotifySubmitResult: function (okInt) {
    if (window.unityBridge && window.unityBridge.onSubmitResult) {
      window.unityBridge.onSubmitResult(!!okInt);
    }
  },

  JS_NotifyBookOpen: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    if (window.unityBridge && window.unityBridge.onBookOpen) {
      window.unityBridge.onBookOpen(json);
    }
  },

  JS_NotifyCoverUrls: function (urlsPtr) {
    var json = UTF8ToString(urlsPtr);
    if (window.unityBridge && window.unityBridge.onCoverUrls) {
      window.unityBridge.onCoverUrls(json);
    }
  },

  JS_GetReducedMotion: function () {
    return window.__reducedMotion ? 1 : 0;
  },

});
