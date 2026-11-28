// localStorage bridge for LocalStorageProgressStore (story-005).
// Every call is wrapped in try/catch: private browsing or blocked storage must never stop the game.
mergeInto(LibraryManager.library, {
  WFS_LocalStorageGet: function (keyPtr) {
    var value = null;
    try {
      value = window.localStorage.getItem(UTF8ToString(keyPtr));
    } catch (e) {
      value = null;
    }
    if (value === null) return 0;
    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);
    return buffer;
  },

  WFS_LocalStorageSet: function (keyPtr, valuePtr) {
    try {
      window.localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr));
      return 1;
    } catch (e) {
      return 0;
    }
  }
});
