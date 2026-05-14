mergeInto(LibraryManager.library, {
  MessageUnityToReact: function (viewName) {
    window.dispatchReactUnityEvent("MessageUnityToReact", UTF8ToString(viewName));
  },
});