using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BallGame.LevelDesign
{
    /// <summary>
    /// Uses a browser-native input element in WebGL so IME composition (for example
    /// Chinese pinyin selection) is handled by the browser instead of the Unity canvas.
    /// </summary>
    public sealed class CollaborativeWebGLTextInputBridge : MonoBehaviour, IPointerClickHandler
    {
        private TMP_InputField input;
        private string receiverName;
        private string changedMethod;
        private string endedMethod;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void BallGameOpenTextInput(
            string receiver,
            string onChanged,
            string onEnded,
            string value,
            float left,
            float top,
            float width,
            float height,
            int characterLimit);
#endif

        public void Configure(TMP_InputField target, GameObject receiver, string onChanged, string onEnded)
        {
            input = target;
            receiverName = receiver != null ? receiver.name : string.Empty;
            changedMethod = onChanged;
            endedMethod = onEnded;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (input == null || !input.interactable || string.IsNullOrEmpty(receiverName)) return;

            RectTransform rectTransform = input.transform as RectTransform;
            if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0) return;

            Canvas canvas = input.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);

            float left = Mathf.Clamp01(bottomLeft.x / Screen.width);
            float top = Mathf.Clamp01(1f - topRight.y / Screen.height);
            float width = Mathf.Clamp01((topRight.x - bottomLeft.x) / Screen.width);
            float height = Mathf.Clamp01((topRight.y - bottomLeft.y) / Screen.height);
            WebGLInput.captureAllKeyboardInput = false;
            BallGameOpenTextInput(
                receiverName,
                changedMethod,
                endedMethod,
                input.text ?? string.Empty,
                left,
                top,
                width,
                height,
                input.characterLimit);
#endif
        }

        public static void RestoreUnityKeyboardCapture()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }

        private void OnDisable()
        {
            RestoreUnityKeyboardCapture();
        }
    }
}
