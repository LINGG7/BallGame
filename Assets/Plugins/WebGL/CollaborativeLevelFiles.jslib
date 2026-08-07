mergeInto(LibraryManager.library, {
  BallGameDownloadText: function (filenamePtr, contentPtr) {
    var filename = UTF8ToString(filenamePtr);
    var content = UTF8ToString(contentPtr);
    var blob = new Blob([content], { type: 'application/json;charset=utf-8' });
    var url = URL.createObjectURL(blob);
    var anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  },
  BallGamePickJson: function (objectPtr, methodPtr) {
    var objectName = UTF8ToString(objectPtr);
    var methodName = UTF8ToString(methodPtr);
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json,application/json';
    input.onchange = function () {
      var file = input.files && input.files[0];
      if (!file) return;
      var reader = new FileReader();
      reader.onload = function () { SendMessage(objectName, methodName, reader.result); };
      reader.readAsText(file, 'utf-8');
    };
    input.click();
  }
});
