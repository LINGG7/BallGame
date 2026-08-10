mergeInto(LibraryManager.library, {
  BallGameOpenTextInput: function (
    receiverPtr,
    changedMethodPtr,
    endedMethodPtr,
    valuePtr,
    normalizedLeft,
    normalizedTop,
    normalizedWidth,
    normalizedHeight,
    characterLimit
  ) {
    var receiver = UTF8ToString(receiverPtr);
    var changedMethod = UTF8ToString(changedMethodPtr);
    var endedMethod = UTF8ToString(endedMethodPtr);
    var value = UTF8ToString(valuePtr);
    var canvas = Module.canvas || document.querySelector('canvas');
    if (!canvas) return;

    var previous = window.__ballGameLevelTitleInput;
    if (previous && previous.parentNode) previous.parentNode.removeChild(previous);

    var canvasRect = canvas.getBoundingClientRect();
    var input = document.createElement('input');
    input.type = 'text';
    input.value = value;
    input.autocomplete = 'off';
    input.spellcheck = false;
    input.setAttribute('aria-label', 'Level title');
    if (characterLimit > 0) input.maxLength = characterLimit;

    input.style.position = 'fixed';
    input.style.boxSizing = 'border-box';
    input.style.left = (canvasRect.left + normalizedLeft * canvasRect.width) + 'px';
    input.style.top = (canvasRect.top + normalizedTop * canvasRect.height) + 'px';
    input.style.width = Math.max(1, normalizedWidth * canvasRect.width) + 'px';
    input.style.height = Math.max(1, normalizedHeight * canvasRect.height) + 'px';
    input.style.zIndex = '2147483647';
    input.style.border = '1px solid rgba(255,255,255,0.45)';
    input.style.borderRadius = '2px';
    input.style.outline = 'none';
    input.style.padding = '0 7px';
    input.style.background = 'rgb(41,46,51)';
    input.style.color = 'white';
    input.style.fontFamily = 'sans-serif';
    input.style.fontSize = Math.max(14, normalizedHeight * canvasRect.height * 0.42) + 'px';

    var sendChanged = function () {
      SendMessage(receiver, changedMethod, input.value);
    };
    var finish = function () {
      SendMessage(receiver, endedMethod, input.value);
      if (window.__ballGameLevelTitleInput === input) {
        window.__ballGameLevelTitleInput = null;
      }
      window.setTimeout(function () {
        if (input.parentNode) input.parentNode.removeChild(input);
      }, 0);
    };

    input.addEventListener('input', sendChanged);
    input.addEventListener('blur', finish, { once: true });
    input.addEventListener('keydown', function (event) {
      if (event.key === 'Enter' && !event.isComposing) {
        event.preventDefault();
        input.blur();
      }
    });

    document.body.appendChild(input);
    window.__ballGameLevelTitleInput = input;
    input.focus();
    input.setSelectionRange(input.value.length, input.value.length);
  }
});
