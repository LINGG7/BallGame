using System;
using System.Collections.Generic;
using BallGame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BallGame.UI
{
	public sealed class GameHudRefs : MonoBehaviour
	{
		[SerializeField]
		private ToolInventoryPanel inventoryPanel;

		[SerializeField]
		private PlacementControls placementControls;

		[SerializeField]
		private WorldTargetIndicator ballIndicator;

		[SerializeField]
		private WorldTargetIndicator goalIndicator;

		[SerializeField]
		private TMP_Text titleText;

		[SerializeField]
		private TMP_Text statusText;

		[SerializeField]
		private TMP_Text costText;

		[SerializeField]
		private Button runButton;

		[SerializeField]
		private Button resetButton;

		[SerializeField]
		private Button editButton;

		[SerializeField]
		private Button nextButton;

		[SerializeField]
		private Button returnButton;

		[SerializeField]
		private LevelIntroController levelIntro;

		[SerializeField]
		private ToolBarCollapser toolBarCollapser;

		[SerializeField]
		private GameObject moveBar;

		private WorldTargetIndicatorGroup ballIndicatorGroup;

		private WorldTargetIndicatorGroup goalIndicatorGroup;

		private Canvas canvas;

		private bool toolBarStateCaptured;

		private bool toolBarWasCollapsed;

		public ToolInventoryPanel InventoryPanel => inventoryPanel;

		public PlacementControls PlacementControls => placementControls;

		public WorldTargetIndicator BallIndicator => ballIndicator;

		public WorldTargetIndicator GoalIndicator => goalIndicator;

		public TMP_Text TitleText => titleText;

		public TMP_Text StatusText => statusText;

		public TMP_Text CostText
		{
			get
			{
				EnsureCostText();
				return costText;
			}
		}

		public Button RunButton => runButton;

		public Button ResetButton => resetButton;

		public Button EditButton => editButton;

		public Button NextButton => nextButton;

		public Button ReturnButton
		{
			get
			{
				EnsureReturnButton();
				return returnButton;
			}
		}

		public LevelIntroController LevelIntro
		{
			get
			{
				EnsureLevelIntro();
				return levelIntro;
			}
		}

		private void Awake()
		{
			canvas = ((Component)this).GetComponentInParent<Canvas>();
			EnsureLevelIntro();
			EnsureCostText();
			EnsureReturnButton();
			EnsureBuildControls();
			ballIndicatorGroup = GetOrCreateGroup("BallIndicatorGroup");
			goalIndicatorGroup = GetOrCreateGroup("GoalIndicatorGroup");
			Transform val = ((Component)this).transform.Find("Background");
			Image val2 = default(Image);
			if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null && ((Component)val).TryGetComponent<Image>(out val2))
			{
				((Behaviour)val2).enabled = false;
				((Graphic)val2).raycastTarget = false;
			}
		}

		public void ApplyGameState(GameState state)
		{
			EnsureBuildControls();
			if ((UnityEngine.Object)(object)moveBar != (UnityEngine.Object)null)
			{
				moveBar.SetActive(state == GameState.Build);
			}
			if ((UnityEngine.Object)(object)toolBarCollapser == (UnityEngine.Object)null)
			{
				return;
			}
			switch (state)
			{
			case GameState.Simulate:
				if (!toolBarStateCaptured)
				{
					toolBarWasCollapsed = toolBarCollapser.IsCollapsed;
					toolBarStateCaptured = true;
				}
				toolBarCollapser.SetCollapsed(collapsed: true);
				toolBarCollapser.SetToggleInteractable(interactable: false);
				break;
			default:
				if (state != GameState.Failed)
				{
					if (state == GameState.Build)
					{
						if (toolBarStateCaptured)
						{
							toolBarCollapser.SetCollapsed(toolBarWasCollapsed);
							toolBarStateCaptured = false;
						}
						toolBarCollapser.SetToggleInteractable(interactable: true);
					}
					break;
				}
				goto case GameState.Success;
			case GameState.Success:
				toolBarCollapser.SetCollapsed(collapsed: true);
				toolBarCollapser.SetToggleInteractable(interactable: false);
				break;
			}
		}

		private void EnsureBuildControls()
		{
			if ((UnityEngine.Object)(object)toolBarCollapser == (UnityEngine.Object)null)
			{
				Transform val = ((Component)this).transform.Find("Tool Bar");
				toolBarCollapser = (((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? ((Component)val).GetComponent<ToolBarCollapser>() : null);
			}
			if ((UnityEngine.Object)(object)moveBar == (UnityEngine.Object)null)
			{
				Transform val2 = ((Component)this).transform.Find("Move Bar");
				moveBar = (((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? ((Component)val2).gameObject : null);
			}
		}

		private void EnsureLevelIntro()
		{
			if ((UnityEngine.Object)(object)levelIntro == (UnityEngine.Object)null)
			{
				levelIntro = ((Component)this).GetComponent<LevelIntroController>();
			}
			if ((UnityEngine.Object)(object)levelIntro == (UnityEngine.Object)null)
			{
				levelIntro = ((Component)this).gameObject.AddComponent<LevelIntroController>();
			}
			levelIntro.Configure(((UnityEngine.Object)(object)titleText != (UnityEngine.Object)null) ? titleText.font : null);
		}

		private void EnsureCostText()
		{
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)costText != (UnityEngine.Object)null))
			{
				Transform val = ((Component)this).transform.Find("Top Bar/Cost");
				if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
				{
					val = ((Component)this).transform.Find("TopBar/Cost");
				}
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					costText = ((Component)val).GetComponent<TMP_Text>();
				}
				else if (!((UnityEngine.Object)(object)statusText == (UnityEngine.Object)null))
				{
					costText = UnityEngine.Object.Instantiate<TMP_Text>(statusText, statusText.transform.parent);
					((UnityEngine.Object)costText).name = "Cost";
					costText.text = string.Empty;
					costText.fontSize = 18f;
					costText.enableAutoSizing = true;
					RectTransform rectTransform = costText.rectTransform;
					rectTransform.sizeDelta = new Vector2(300f, rectTransform.sizeDelta.y);
					rectTransform.anchoredPosition = new Vector2(155f, rectTransform.anchoredPosition.y);
				}
			}
		}

		private void EnsureReturnButton()
		{
			if (!((UnityEngine.Object)(object)returnButton != (UnityEngine.Object)null))
			{
				Transform val = ((Component)this).transform.Find("Top Bar/Return");
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					returnButton = ((Component)val).GetComponent<Button>();
				}
			}
		}

		public void BindTargetIndicators(GameManager manager, Camera worldCamera, IEnumerable<Transform> ballTargets, IEnumerable<Transform> goalTargets)
		{
			if ((UnityEngine.Object)(object)canvas == (UnityEngine.Object)null)
			{
				canvas = ((Component)this).GetComponentInParent<Canvas>();
			}
			if ((UnityEngine.Object)(object)ballIndicatorGroup == (UnityEngine.Object)null)
			{
				ballIndicatorGroup = GetOrCreateGroup("BallIndicatorGroup");
			}
			if ((UnityEngine.Object)(object)goalIndicatorGroup == (UnityEngine.Object)null)
			{
				goalIndicatorGroup = GetOrCreateGroup("GoalIndicatorGroup");
			}
			ballIndicatorGroup.BindTargets(ballIndicator, manager, worldCamera, ballTargets, canvas);
			goalIndicatorGroup.BindTargets(goalIndicator, manager, worldCamera, goalTargets, canvas);
		}

		private WorldTargetIndicatorGroup GetOrCreateGroup(string groupName)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Expected O, but got Unknown
			Transform val = ((Component)this).transform.Find(groupName);
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				GameObject val2 = new GameObject(groupName, new Type[1] { typeof(RectTransform) });
				val = val2.transform;
				val.SetParent(((Component)this).transform, false);
			}
			GameObject gameObject = ((Component)val).gameObject;
			WorldTargetIndicatorGroup worldTargetIndicatorGroup = gameObject.GetComponent<WorldTargetIndicatorGroup>();
			if ((UnityEngine.Object)(object)worldTargetIndicatorGroup == (UnityEngine.Object)null)
			{
				worldTargetIndicatorGroup = gameObject.AddComponent<WorldTargetIndicatorGroup>();
			}
			return worldTargetIndicatorGroup;
		}
	}
}
