using System;
using System.Collections.Generic;
using BallGame.Audio;
using BallGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BallGame.UI
{
	public sealed class HapticsSettingsPanelController : MonoBehaviour
	{
		private static readonly HapticIntensityLevel[] Levels = new HapticIntensityLevel[4]
		{
			HapticIntensityLevel.Off,
			HapticIntensityLevel.Low,
			HapticIntensityLevel.Medium,
			HapticIntensityLevel.High
		};

		private static readonly string[] LevelLabels = new string[4] { "关闭", "低", "中", "高" };

		private static readonly Color SelectedColor = new Color(1f, 0.78f, 0.3f, 1f);

		private static readonly Color SelectedHighlightColor = new Color(1f, 0.86f, 0.48f, 1f);

		private Button settingsButton;

		private Button audioTabButton;

		private Button hapticsTabButton;

		private GameObject modalRoot;

		private GameObject audioPage;

		private GameObject hapticsPage;

		private Slider musicSlider;

		private Slider sfxSlider;

		private TMP_Text musicPercentText;

		private TMP_Text sfxPercentText;

		private AudioClip clickPreviewClip;

		private readonly Button[] levelButtons = (Button[])(object)new Button[4];

		private readonly TMP_Text[] levelTexts = (TMP_Text[])(object)new TMP_Text[4];

		private ColorBlock defaultOptionColors;

		private ColorBlock defaultTabColors;

		public bool IsOpen => (UnityEngine.Object)(object)modalRoot != (UnityEngine.Object)null && modalRoot.activeSelf;

		private void Awake()
		{
			BuildUiIfNeeded();
			Hide();
		}

		public void Show()
		{
			BuildUiIfNeeded();
			if (!((UnityEngine.Object)(object)modalRoot == (UnityEngine.Object)null))
			{
				RefreshAudioControls();
				RefreshSelection();
				ShowAudioPage();
				modalRoot.SetActive(true);
				modalRoot.transform.SetAsLastSibling();
			}
		}

		public void Hide()
		{
			if ((UnityEngine.Object)(object)modalRoot != (UnityEngine.Object)null)
			{
				modalRoot.SetActive(false);
			}
		}

		public void ShowAudioPage()
		{
			SetActivePage(showAudio: true);
		}

		public void ShowHapticsPage()
		{
			SetActivePage(showAudio: false);
		}

		public void SelectLevel(HapticIntensityLevel level)
		{
			MobileHaptics.SetIntensityLevel(level);
			RefreshSelection();
		}

		public void RefreshSelection()
		{
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			HapticIntensityLevel intensityLevel = MobileHaptics.IntensityLevel;
			for (int i = 0; i < levelButtons.Length; i++)
			{
				if (!((UnityEngine.Object)(object)levelButtons[i] == (UnityEngine.Object)null) && !((UnityEngine.Object)(object)levelTexts[i] == (UnityEngine.Object)null))
				{
					bool flag = Levels[i] == intensityLevel;
					((Selectable)levelButtons[i]).colors = GetSelectionColors(defaultOptionColors, flag);
					levelTexts[i].text = (flag ? ("✓ " + LevelLabels[i]) : LevelLabels[i]);
				}
			}
		}

		public void SetMusicVolume(float value)
		{
			float num = Mathf.Clamp01(value);
			AudioManager.Instance?.SetMusicVolume(num);
			SetPercentText(musicPercentText, num);
		}

		public void SetSfxVolume(float value)
		{
			float num = Mathf.Clamp01(value);
			AudioManager.Instance?.SetSfxVolume(num);
			SetPercentText(sfxPercentText, num);
		}

		public void PreviewSfxVolume()
		{
			AudioManager.Instance?.PlaySfx(clickPreviewClip);
		}

		private void BuildUiIfNeeded()
		{
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Expected O, but got Unknown
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Expected O, but got Unknown
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_018b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_0195: Unknown result type (might be due to invalid IL or missing references)
			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
			//IL_019e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_020d: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
			//IL_0248: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Unknown result type (might be due to invalid IL or missing references)
			//IL_0288: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02af: Expected O, but got Unknown
			//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0308: Unknown result type (might be due to invalid IL or missing references)
			//IL_0312: Expected O, but got Unknown
			//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ee: Expected O, but got Unknown
			//IL_043f: Unknown result type (might be due to invalid IL or missing references)
			//IL_044e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0475: Unknown result type (might be due to invalid IL or missing references)
			//IL_047a: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0506: Unknown result type (might be due to invalid IL or missing references)
			//IL_0520: Unknown result type (might be due to invalid IL or missing references)
			//IL_052a: Expected O, but got Unknown
			//IL_0586: Unknown result type (might be due to invalid IL or missing references)
			//IL_0595: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b8: Expected O, but got Unknown
			if ((UnityEngine.Object)(object)settingsButton != (UnityEngine.Object)null && (UnityEngine.Object)(object)modalRoot != (UnityEngine.Object)null)
			{
				return;
			}
			if ((UnityEngine.Object)(object)settingsButton == (UnityEngine.Object)null)
			{
				Transform settingsTransform = ((Component)this).transform.Find("SettingsButton")
					?? ((Component)this).transform.Find("SettingButton");
				settingsButton = settingsTransform != null ? ((Component)settingsTransform).GetComponent<Button>() : null;
			}
			if ((UnityEngine.Object)(object)settingsButton == (UnityEngine.Object)null)
			{
				Debug.LogWarning((object)"Cannot build settings UI: an existing SettingsButton was not found.", (UnityEngine.Object)(object)this);
				return;
			}
			Transform obj = ((Component)this).transform.Find("NormalButton");
			Button val = ((obj != null) ? ((Component)obj).GetComponent<Button>() : null);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				Debug.LogWarning((object)"Cannot build settings UI: NormalButton was not found.", (UnityEngine.Object)(object)this);
				return;
			}
			clickPreviewClip = Resources.Load<AudioClip>("Audio/SFX/Click");
			((UnityEvent)settingsButton.onClick).RemoveListener(new UnityAction(Show));
			((UnityEvent)settingsButton.onClick).AddListener(new UnityAction(Show));
			modalRoot = CreateImageObject("SettingsModal", ((Component)this).transform, new Color(0f, 0f, 0f, 0.58f));
			StretchToParent(modalRoot.GetComponent<RectTransform>());
			Button val2 = modalRoot.AddComponent<Button>();
			((Selectable)val2).transition = Selectable.Transition.None;
			((UnityEvent)val2.onClick).AddListener(new UnityAction(Hide));
			GameObject val3 = CreateImageObject("Panel", modalRoot.transform, new Color(1f, 0.94f, 0.78f, 1f));
			RectTransform component2 = val3.GetComponent<RectTransform>();
			Vector2 val4 = default(Vector2);
			val4 = new Vector2(0.5f, 0.5f);
			component2.pivot = val4;
			Vector2 anchorMin = (component2.anchorMax = val4);
			component2.anchorMin = anchorMin;
			component2.sizeDelta = new Vector2(760f, 620f);
			Graphic targetGraphic = ((Selectable)val).targetGraphic;
			Image val6 = (Image)(object)((targetGraphic is Image) ? targetGraphic : null);
			ApplyImageStyle(val3.GetComponent<Image>(), val6);
			Button val7 = val3.AddComponent<Button>();
			((Selectable)val7).transition = Selectable.Transition.None;
			TMP_Text val8 = CloneText(val, val3.transform, "Title", "设置");
			ConfigureTextRect(val8, new Vector2(0f, 250f), new Vector2(520f, 72f));
			val8.fontSize = 42f;
			val8.fontSizeMax = 42f;
			defaultTabColors = ((Selectable)val).colors;
			audioTabButton = CloneButton(val, val3.transform, "AudioTabButton", "音频");
			ConfigureButtonRect(audioTabButton, new Vector2(-145f, 170f), new Vector2(250f, 64f));
			((UnityEvent)audioTabButton.onClick).AddListener(new UnityAction(ShowAudioPage));
			hapticsTabButton = CloneButton(val, val3.transform, "HapticsTabButton", "震动");
			ConfigureButtonRect(hapticsTabButton, new Vector2(145f, 170f), new Vector2(250f, 64f));
			((UnityEvent)hapticsTabButton.onClick).AddListener(new UnityAction(ShowHapticsPage));
			audioPage = CreateRectObject("AudioPage", val3.transform);
			ConfigurePageRect(audioPage.GetComponent<RectTransform>());
			musicSlider = CreateVolumeRow(val, val6, audioPage.transform, "Music", "音乐音量", 58f, out musicPercentText);
			sfxSlider = CreateVolumeRow(val, val6, audioPage.transform, "Sfx", "音效音量", -62f, out sfxPercentText);
			((UnityEvent<float>)(object)musicSlider.onValueChanged).AddListener((UnityAction<float>)SetMusicVolume);
			((UnityEvent<float>)(object)sfxSlider.onValueChanged).AddListener((UnityAction<float>)SetSfxVolume);
			AddPointerUpListener(((Component)sfxSlider).gameObject, new UnityAction(PreviewSfxVolume));
			hapticsPage = CreateRectObject("HapticsPage", val3.transform);
			ConfigurePageRect(hapticsPage.GetComponent<RectTransform>());
			TMP_Text val9 = CloneText(val, hapticsPage.transform, "IntensityTitle", "震动强度");
			ConfigureTextRect(val9, new Vector2(0f, 65f), new Vector2(500f, 58f));
			val9.fontSize = 30f;
			val9.fontSizeMax = 30f;
			defaultOptionColors = ((Selectable)val).colors;
			for (int i = 0; i < Levels.Length; i++)
			{
				int capturedIndex = i;
				Button val10 = CloneButton(val, hapticsPage.transform, "Intensity" + Levels[i].ToString() + "Button", LevelLabels[i]);
				ConfigureButtonRect(val10, new Vector2(-246f + (float)i * 164f, -28f), new Vector2(142f, 72f));
				((UnityEvent)val10.onClick).AddListener((UnityAction)delegate
				{
					SelectLevel(Levels[capturedIndex]);
				});
				levelButtons[i] = val10;
				levelTexts[i] = ((Component)val10).GetComponentInChildren<TMP_Text>(true);
			}
			Button val11 = CloneButton(val, val3.transform, "CloseButton", "关闭");
			ConfigureButtonRect(val11, new Vector2(0f, -250f), new Vector2(200f, 64f));
			((UnityEvent)val11.onClick).AddListener(new UnityAction(Hide));
			RefreshAudioControls();
			RefreshSelection();
			SetActivePage(showAudio: true);
			modalRoot.SetActive(false);
		}

		private Slider CreateVolumeRow(Button buttonTemplate, Image templateImage, Transform parent, string prefix, string label, float y, out TMP_Text percentText)
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Expected O, but got Unknown
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_0184: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_021a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0222: Unknown result type (might be due to invalid IL or missing references)
			//IL_0224: Unknown result type (might be due to invalid IL or missing references)
			//IL_0225: Unknown result type (might be due to invalid IL or missing references)
			//IL_022d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0241: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
			TMP_Text val = CloneText(buttonTemplate, parent, prefix + "Label", label);
			ConfigureTextRect(val, new Vector2(-245f, y), new Vector2(180f, 54f));
			val.fontSize = 28f;
			val.fontSizeMax = 28f;
			GameObject val2 = new GameObject(prefix + "Slider", new Type[2]
			{
				typeof(RectTransform),
				typeof(Slider)
			});
			val2.layer = ((Component)parent).gameObject.layer;
			val2.transform.SetParent(parent, false);
			RectTransform component = val2.GetComponent<RectTransform>();
			Vector2 val3 = default(Vector2);
			val3 = new Vector2(0.5f, 0.5f);
			component.pivot = val3;
			Vector2 anchorMin = (component.anchorMax = val3);
			component.anchorMin = anchorMin;
			component.anchoredPosition = new Vector2(25f, y);
			component.sizeDelta = new Vector2(360f, 48f);
			Image component2 = CreateImageObject("Background", val2.transform, new Color(0.44f, 0.35f, 0.23f, 0.42f)).GetComponent<Image>();
			ApplyImageStyle(component2, templateImage);
			ConfigureHorizontalTrack(((Graphic)component2).rectTransform, 0f, 20f);
			GameObject val5 = CreateRectObject("Fill Area", val2.transform);
			ConfigureHorizontalTrack(val5.GetComponent<RectTransform>(), 12f, 16f);
			Image component3 = CreateImageObject("Fill", val5.transform, SelectedColor).GetComponent<Image>();
			ApplyImageStyle(component3, templateImage);
			StretchToParent(((Graphic)component3).rectTransform);
			GameObject val6 = CreateRectObject("Handle Slide Area", val2.transform);
			ConfigureHorizontalTrack(val6.GetComponent<RectTransform>(), 14f, 40f);
			Image component4 = CreateImageObject("Handle", val6.transform, Color.white).GetComponent<Image>();
			ApplyImageStyle(component4, templateImage);
			RectTransform rectTransform = ((Graphic)component4).rectTransform;
			val3 = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = val3;
			anchorMin = (rectTransform.anchorMax = val3);
			rectTransform.anchorMin = anchorMin;
			rectTransform.sizeDelta = new Vector2(38f, 44f);
			Slider component5 = val2.GetComponent<Slider>();
			component5.minValue = 0f;
			component5.maxValue = 1f;
			component5.wholeNumbers = false;
			component5.direction = Slider.Direction.LeftToRight;
			component5.fillRect = ((Graphic)component3).rectTransform;
			component5.handleRect = rectTransform;
			((Selectable)component5).targetGraphic = (Graphic)(object)component4;
			percentText = CloneText(buttonTemplate, parent, prefix + "Percent", "100%");
			ConfigureTextRect(percentText, new Vector2(285f, y), new Vector2(100f, 54f));
			percentText.fontSize = 26f;
			percentText.fontSizeMax = 26f;
			return component5;
		}

		private void RefreshAudioControls()
		{
			float num = (((UnityEngine.Object)(object)AudioManager.Instance != (UnityEngine.Object)null) ? AudioManager.Instance.MusicVolume : 1f);
			float num2 = (((UnityEngine.Object)(object)AudioManager.Instance != (UnityEngine.Object)null) ? AudioManager.Instance.SfxVolume : 1f);
			Slider obj = musicSlider;
			if (obj != null)
			{
				obj.SetValueWithoutNotify(num);
			}
			Slider obj2 = sfxSlider;
			if (obj2 != null)
			{
				obj2.SetValueWithoutNotify(num2);
			}
			SetPercentText(musicPercentText, num);
			SetPercentText(sfxPercentText, num2);
		}

		private void SetActivePage(bool showAudio)
		{
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)audioPage != (UnityEngine.Object)null)
			{
				audioPage.SetActive(showAudio);
			}
			if ((UnityEngine.Object)(object)hapticsPage != (UnityEngine.Object)null)
			{
				hapticsPage.SetActive(!showAudio);
			}
			if ((UnityEngine.Object)(object)audioTabButton != (UnityEngine.Object)null)
			{
				((Selectable)audioTabButton).colors = GetSelectionColors(defaultTabColors, showAudio);
			}
			if ((UnityEngine.Object)(object)hapticsTabButton != (UnityEngine.Object)null)
			{
				((Selectable)hapticsTabButton).colors = GetSelectionColors(defaultTabColors, !showAudio);
			}
		}

		private static ColorBlock GetSelectionColors(ColorBlock defaults, bool selected)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			if (selected)
			{
				defaults.normalColor = SelectedColor;
				defaults.selectedColor = SelectedColor;
				defaults.highlightedColor = SelectedHighlightColor;
			}
			return defaults;
		}

		private static void SetPercentText(TMP_Text label, float value)
		{
			if ((UnityEngine.Object)(object)label != (UnityEngine.Object)null)
			{
				label.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
			}
		}

		private static void AddPointerUpListener(GameObject target, UnityAction callback)
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			EventTrigger val = target.AddComponent<EventTrigger>();
			val.triggers = new List<EventTrigger.Entry>();
			EventTrigger.Entry val2 = new EventTrigger.Entry
			{
				eventID = (EventTriggerType)3
			};
			((UnityEvent<BaseEventData>)(object)val2.callback).AddListener((UnityAction<BaseEventData>)delegate
			{
				callback.Invoke();
			});
			val.triggers.Add(val2);
		}

		private static Button CloneButton(Button template, Transform parent, string objectName, string label)
		{
			GameObject val = UnityEngine.Object.Instantiate<GameObject>(((Component)template).gameObject, parent, false);
			((UnityEngine.Object)val).name = objectName;
			Button component = val.GetComponent<Button>();
			((UnityEventBase)component.onClick).RemoveAllListeners();
			TMP_Text componentInChildren = val.GetComponentInChildren<TMP_Text>(true);
			if ((UnityEngine.Object)(object)componentInChildren != (UnityEngine.Object)null)
			{
				componentInChildren.text = label;
			}
			return component;
		}

		private static TMP_Text CloneText(Button template, Transform parent, string objectName, string text)
		{
			TMP_Text componentInChildren = ((Component)template).GetComponentInChildren<TMP_Text>(true);
			TMP_Text val = UnityEngine.Object.Instantiate<TMP_Text>(componentInChildren, parent, false);
			((UnityEngine.Object)((Component)val).gameObject).name = objectName;
			val.text = text;
			return val;
		}

		private static GameObject CreateRectObject(string objectName, Transform parent)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			GameObject val = new GameObject(objectName, new Type[1] { typeof(RectTransform) });
			val.layer = ((Component)parent).gameObject.layer;
			val.transform.SetParent(parent, false);
			return val;
		}

		private static GameObject CreateImageObject(string objectName, Transform parent, Color color)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(objectName, new Type[3]
			{
				typeof(RectTransform),
				typeof(CanvasRenderer),
				typeof(Image)
			});
			val.layer = ((Component)parent).gameObject.layer;
			val.transform.SetParent(parent, false);
			((Graphic)val.GetComponent<Image>()).color = color;
			return val;
		}

		private static void ApplyImageStyle(Image image, Image template)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)image == (UnityEngine.Object)null) && !((UnityEngine.Object)(object)template == (UnityEngine.Object)null))
			{
				image.sprite = template.sprite;
				image.type = template.type;
			}
		}

		private static void ConfigureButtonRect(Button button, Vector2 position, Vector2 size)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			RectTransform component = ((Component)button).GetComponent<RectTransform>();
			Vector2 val = default(Vector2);
			val = new Vector2(0.5f, 0.5f);
			component.pivot = val;
			Vector2 anchorMin = (component.anchorMax = val);
			component.anchorMin = anchorMin;
			component.anchoredPosition = position;
			component.sizeDelta = size;
		}

		private static void ConfigureTextRect(TMP_Text text, Vector2 position, Vector2 size)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			RectTransform rectTransform = text.rectTransform;
			Vector2 val = default(Vector2);
			val = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = val;
			Vector2 anchorMin = (rectTransform.anchorMax = val);
			rectTransform.anchorMin = anchorMin;
			rectTransform.anchoredPosition = position;
			rectTransform.sizeDelta = size;
		}

		private static void ConfigurePageRect(RectTransform rect)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = default(Vector2);
			val = new Vector2(0.5f, 0.5f);
			rect.pivot = val;
			Vector2 anchorMin = (rect.anchorMax = val);
			rect.anchorMin = anchorMin;
			rect.anchoredPosition = new Vector2(0f, -20f);
			rect.sizeDelta = new Vector2(700f, 250f);
		}

		private static void ConfigureHorizontalTrack(RectTransform rect, float horizontalInset, float height)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			rect.anchorMin = new Vector2(0f, 0.5f);
			rect.anchorMax = new Vector2(1f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = new Vector2(horizontalInset, (0f - height) * 0.5f);
			rect.offsetMax = new Vector2(0f - horizontalInset, height * 0.5f);
		}

		private static void StretchToParent(RectTransform rect)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = Vector2.zero;
			rect.sizeDelta = Vector2.zero;
		}
	}
}
