using System.Collections.Generic;
using BallGame.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BallGame.UI
{
	public sealed class ShopPanelController : MonoBehaviour
	{
		private const string ShopModalResourcePath = "UI/ShopModal";

		private Button shopButton;

		private GameObject modalRoot;

		private Button recommendedTab;

		private Button skinsTab;

		private Transform cardArea;

		private Image selectedPreview;

		private TMP_Text selectedPreviewName;

		private TMP_Text selectedState;

		private Button actionButton;

		private TMP_Text actionLabel;

		private TMP_Text statusText;

		private Button cardTemplate;

		private readonly List<Button> cardButtons = new List<Button>();

		private readonly List<GameObject> cardRoots = new List<GameObject>();

		private int selectedIndex;

		private bool recommendedOnly = true;

		public bool IsOpen => (UnityEngine.Object)(object)modalRoot != (UnityEngine.Object)null && modalRoot.activeSelf;

		private void Awake()
		{
			ResolveEntryAndModal();
			Hide();
		}

		public void Show()
		{
			ResolveEntryAndModal();
			if (!((UnityEngine.Object)(object)modalRoot == (UnityEngine.Object)null))
			{
				selectedIndex = FindCurrentIndex();
				recommendedOnly = true;
				Refresh();
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

		private void ResolveEntryAndModal()
		{
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Expected O, but got Unknown
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Expected O, but got Unknown
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Expected O, but got Unknown
			//IL_0163: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Expected O, but got Unknown
			//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ea: Expected O, but got Unknown
			//IL_0303: Unknown result type (might be due to invalid IL or missing references)
			//IL_030d: Expected O, but got Unknown
			//IL_0326: Unknown result type (might be due to invalid IL or missing references)
			//IL_0330: Expected O, but got Unknown
			if ((UnityEngine.Object)(object)shopButton == (UnityEngine.Object)null)
			{
				Transform obj = ((Component)this).transform.Find("ShopButton");
				shopButton = ((obj != null) ? ((Component)obj).GetComponent<Button>() : null);
				if ((UnityEngine.Object)(object)shopButton != (UnityEngine.Object)null)
				{
					((UnityEvent)shopButton.onClick).RemoveListener(new UnityAction(Show));
					((UnityEvent)shopButton.onClick).AddListener(new UnityAction(Show));
				}
			}
			if ((UnityEngine.Object)(object)modalRoot != (UnityEngine.Object)null)
			{
				return;
			}
			GameObject val = Resources.Load<GameObject>("UI/ShopModal");
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				Debug.LogWarning((object)"Missing shop modal prefab: UI/ShopModal", (UnityEngine.Object)(object)this);
				return;
			}
			modalRoot = UnityEngine.Object.Instantiate<GameObject>(val, ((Component)this).transform);
			((UnityEngine.Object)modalRoot).name = "ShopModal";
			Stretch(modalRoot.GetComponent<RectTransform>());
			Button component = modalRoot.GetComponent<Button>();
			if (component != null)
			{
				((UnityEvent)component.onClick).AddListener(new UnityAction(Hide));
			}
			Transform val2 = modalRoot.transform.Find("Panel");
			if (val2 != null)
			{
				Transform obj2 = val2.Find("BackButton");
				if (obj2 != null)
				{
					Button component2 = ((Component)obj2).GetComponent<Button>();
					if (component2 != null)
					{
						((UnityEvent)component2.onClick).AddListener(new UnityAction(Hide));
					}
				}
			}
			object obj3;
			if (val2 == null)
			{
				obj3 = null;
			}
			else
			{
				Transform obj4 = val2.Find("CategorySidebar/RecommendedTab");
				obj3 = ((obj4 != null) ? ((Component)obj4).GetComponent<Button>() : null);
			}
			recommendedTab = (Button)obj3;
			object obj5;
			if (val2 == null)
			{
				obj5 = null;
			}
			else
			{
				Transform obj6 = val2.Find("CategorySidebar/SkinsTab");
				obj5 = ((obj6 != null) ? ((Component)obj6).GetComponent<Button>() : null);
			}
			skinsTab = (Button)obj5;
			cardArea = ((val2 != null) ? val2.Find("CardArea") : null);
			object obj7;
			if (val2 == null)
			{
				obj7 = null;
			}
			else
			{
				Transform obj8 = val2.Find("CardTemplate");
				obj7 = ((obj8 != null) ? ((Component)obj8).GetComponent<Button>() : null);
			}
			cardTemplate = (Button)obj7;
			Transform val3 = ((val2 != null) ? val2.Find("SkinDetail") : null);
			object obj9;
			if (val3 == null)
			{
				obj9 = null;
			}
			else
			{
				Transform obj10 = val3.Find("DetailPreview");
				obj9 = ((obj10 != null) ? ((Component)obj10).GetComponent<Image>() : null);
			}
			selectedPreview = (Image)obj9;
			object obj11;
			if (val3 == null)
			{
				obj11 = null;
			}
			else
			{
				Transform obj12 = val3.Find("SelectedName");
				obj11 = ((obj12 != null) ? ((Component)obj12).GetComponent<TMP_Text>() : null);
			}
			selectedPreviewName = (TMP_Text)obj11;
			object obj13;
			if (val3 == null)
			{
				obj13 = null;
			}
			else
			{
				Transform obj14 = val3.Find("SelectedState");
				obj13 = ((obj14 != null) ? ((Component)obj14).GetComponent<TMP_Text>() : null);
			}
			selectedState = (TMP_Text)obj13;
			object obj15;
			if (val3 == null)
			{
				obj15 = null;
			}
			else
			{
				Transform obj16 = val3.Find("ActionButton");
				obj15 = ((obj16 != null) ? ((Component)obj16).GetComponent<Button>() : null);
			}
			actionButton = (Button)obj15;
			Button obj17 = actionButton;
			actionLabel = ((obj17 != null) ? ((Component)obj17).GetComponentInChildren<TMP_Text>(true) : null);
			object obj18;
			if (val3 == null)
			{
				obj18 = null;
			}
			else
			{
				Transform obj19 = val3.Find("Status");
				obj18 = ((obj19 != null) ? ((Component)obj19).GetComponent<TMP_Text>() : null);
			}
			statusText = (TMP_Text)obj18;
			Button obj20 = recommendedTab;
			if (obj20 != null)
			{
				((UnityEvent)obj20.onClick).AddListener((UnityAction)delegate
				{
					recommendedOnly = true;
					Refresh();
				});
			}
			Button obj21 = skinsTab;
			if (obj21 != null)
			{
				((UnityEvent)obj21.onClick).AddListener((UnityAction)delegate
				{
					recommendedOnly = false;
					Refresh();
				});
			}
			Button obj22 = actionButton;
			if (obj22 != null)
			{
				((UnityEvent)obj22.onClick).AddListener(new UnityAction(ActivateSelected));
			}
			BuildCards();
			modalRoot.SetActive(false);
		}

		private void BuildCards()
		{
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a0: Expected O, but got Unknown
			if ((UnityEngine.Object)(object)cardArea == (UnityEngine.Object)null || (UnityEngine.Object)(object)cardTemplate == (UnityEngine.Object)null || (UnityEngine.Object)(object)BallSkinService.Catalog == (UnityEngine.Object)null)
			{
				return;
			}
			((Component)cardTemplate).gameObject.SetActive(false);
			for (int i = 0; i < BallSkinService.Catalog.Skins.Count; i++)
			{
				BallSkinEntry ballSkinEntry = BallSkinService.Catalog.Skins[i];
				Button val = UnityEngine.Object.Instantiate<Button>(cardTemplate, cardArea);
				((UnityEngine.Object)val).name = "SkinCard_" + ballSkinEntry.id;
				((Component)val).gameObject.SetActive(true);
				int num = i % 3;
				SetRect(((Component)val).GetComponent<RectTransform>(), new Vector2(-265f + (float)num * 265f, 0f), new Vector2(235f, 300f));
				Transform obj = ((Component)val).transform.Find("Name");
				TMP_Text val2 = ((obj != null) ? ((Component)obj).GetComponent<TMP_Text>() : null);
				Transform obj2 = ((Component)val).transform.Find("State");
				TMP_Text val3 = ((obj2 != null) ? ((Component)obj2).GetComponent<TMP_Text>() : null);
				Transform obj3 = ((Component)val).transform.Find("Preview");
				Image val4 = ((obj3 != null) ? ((Component)obj3).GetComponent<Image>() : null);
				if ((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null)
				{
					val2.text = ballSkinEntry.displayName;
				}
				if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
				{
					val4.sprite = ballSkinEntry.previewSprite;
				}
				int captured = i;
				((UnityEventBase)val.onClick).RemoveAllListeners();
				((UnityEvent)val.onClick).AddListener((UnityAction)delegate
				{
					SelectCard(captured);
				});
				cardButtons.Add(val);
				cardRoots.Add(((Component)val).gameObject);
			}
		}

		private void SelectCard(int index)
		{
			selectedIndex = index;
			if ((UnityEngine.Object)(object)statusText != (UnityEngine.Object)null)
			{
				statusText.text = string.Empty;
			}
			Refresh();
		}

		private void ActivateSelected()
		{
			if ((UnityEngine.Object)(object)BallSkinService.Catalog == (UnityEngine.Object)null || selectedIndex < 0 || selectedIndex >= BallSkinService.Catalog.Skins.Count)
			{
				return;
			}
			BallSkinEntry ballSkinEntry = BallSkinService.Catalog.Skins[selectedIndex];
			if (!BallSkinService.IsUnlocked(ballSkinEntry.id))
			{
				statusText.text = "正在准备解锁…";
				BallSkinService.TryUnlock(ballSkinEntry.id, delegate(bool success)
				{
					statusText.text = (success ? "皮肤已解锁，可以装备" : "暂未解锁");
					Refresh();
				});
			}
			else
			{
				bool flag = BallSkinService.EquipSkin(ballSkinEntry.id);
				statusText.text = (flag ? ("已装备：" + ballSkinEntry.displayName) : "该皮肤尚未解锁");
				Refresh();
			}
		}

		private void Refresh()
		{
			if ((UnityEngine.Object)(object)BallSkinService.Catalog == (UnityEngine.Object)null || BallSkinService.Catalog.Skins.Count == 0)
			{
				return;
			}
			string currentSkinId = BallSkinService.CurrentSkinId;
			for (int i = 0; i < cardButtons.Count; i++)
			{
				BallSkinEntry ballSkinEntry = BallSkinService.Catalog.Skins[i];
				cardRoots[i].SetActive(!recommendedOnly || ballSkinEntry.featured);
				Transform obj = cardRoots[i].transform.Find("State");
				TMP_Text val = ((obj != null) ? ((Component)obj).GetComponent<TMP_Text>() : null);
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					val.text = ((currentSkinId == ballSkinEntry.id) ? "已装备" : ((!BallSkinService.IsUnlocked(ballSkinEntry.id)) ? "尚未解锁" : ((selectedIndex == i) ? "已选中" : "已拥有")));
				}
			}
			BallSkinEntry ballSkinEntry2 = BallSkinService.Catalog.Skins[Mathf.Clamp(selectedIndex, 0, BallSkinService.Catalog.Skins.Count - 1)];
			selectedPreview.sprite = ballSkinEntry2.previewSprite;
			selectedPreviewName.text = ballSkinEntry2.displayName;
			bool flag = BallSkinService.IsUnlocked(ballSkinEntry2.id);
			selectedState.text = ((currentSkinId == ballSkinEntry2.id) ? "当前装备" : (flag ? "已拥有，可装备" : "尚未解锁"));
			((Selectable)actionButton).interactable = currentSkinId != ballSkinEntry2.id;
			actionLabel.text = ((currentSkinId == ballSkinEntry2.id) ? "已装备" : (flag ? "装备" : ((BallSkinService.RewardedAdService == null) ? "解锁" : "观看广告解锁")));
			SetTabColors(recommendedTab, recommendedOnly);
			SetTabColors(skinsTab, !recommendedOnly);
		}

		private int FindCurrentIndex()
		{
			if ((UnityEngine.Object)(object)BallSkinService.Catalog == (UnityEngine.Object)null)
			{
				return 0;
			}
			for (int i = 0; i < BallSkinService.Catalog.Skins.Count; i++)
			{
				if (BallSkinService.Catalog.Skins[i].id == BallSkinService.CurrentSkinId)
				{
					return i;
				}
			}
			return 0;
		}

		private static void SetTabColors(Button button, bool selected)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)button == (UnityEngine.Object)null))
			{
				ColorBlock colors = ((Selectable)button).colors;
				colors.normalColor = (selected ? new Color(1f, 0.82f, 0.42f, 1f) : new Color(0.87f, 0.81f, 0.69f, 1f));
				((Selectable)button).colors = colors;
			}
		}

		private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = new Vector2(0.5f, 0.5f);
			rect.pivot = val;
			Vector2 anchorMin = (rect.anchorMax = val);
			rect.anchorMin = anchorMin;
			rect.anchoredPosition = position;
			rect.sizeDelta = size;
		}

		private static void Stretch(RectTransform rect)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			Vector2 offsetMin = (rect.offsetMax = Vector2.zero);
			rect.offsetMin = offsetMin;
		}
	}
}
