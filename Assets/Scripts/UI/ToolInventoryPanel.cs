using System.Collections.Generic;
using BallGame.Core;
using BallGame.Placement;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BallGame.UI
{
	public sealed class ToolInventoryPanel : MonoBehaviour
	{
		[SerializeField]
		private ToolInventoryButton buttonPrefab;

		[SerializeField]
		private RectTransform contentRoot;

		[SerializeField]
		private Button previousPageButton;

		[SerializeField]
		private Button nextPageButton;

		[SerializeField]
		private TMP_Text pageLabel;

		[SerializeField]
		private int itemsPerPage = 6;

		private readonly List<ToolInventoryButton> buttons = new List<ToolInventoryButton>();

		private PlacementManager placementManager;

		private int currentPage;

		private bool pageButtonsWired;

		private const float ButtonSpacing = 20f;

		private const float TopPadding = 0f;

		private const float DefaultButtonWidth = 100f;

		private const float DefaultButtonHeight = 100f;

		private int TotalPages => (buttons.Count != 0) ? Mathf.CeilToInt((float)buttons.Count / (float)Mathf.Max(1, itemsPerPage)) : 0;

		private void Awake()
		{
			EnsureContentRoot();
			WirePageButtons();
		}

		public void Build(LevelDefinition level, PlacementManager manager)
		{
			EnsureContentRoot();
			WirePageButtons();
			if ((UnityEngine.Object)(object)placementManager != (UnityEngine.Object)null)
			{
				placementManager.InventoryChanged -= Refresh;
			}
			placementManager = manager;
			Clear();
			currentPage = 0;
			if ((UnityEngine.Object)(object)level == (UnityEngine.Object)null || (UnityEngine.Object)(object)placementManager == (UnityEngine.Object)null)
			{
				UpdatePaginationControls();
				return;
			}
			if (level.Mode == LevelMode.LowestCost && (UnityEngine.Object)(object)level.ToolCatalog != (UnityEngine.Object)null)
			{
				foreach (ToolDefinition tool in level.ToolCatalog.Tools)
				{
					CreateButton(tool);
				}
			}
			else
			{
				foreach (LevelDefinition.ToolStock tool2 in level.Tools)
				{
					CreateButton(tool2.tool);
				}
			}
			placementManager.InventoryChanged += Refresh;
			SetPage(0);
			Refresh();
		}

		private void CreateButton(ToolDefinition tool)
		{
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)tool == (UnityEngine.Object)null)
			{
				return;
			}
			ToolInventoryButton toolInventoryButton = (((UnityEngine.Object)(object)tool.InventoryButtonPrefab != (UnityEngine.Object)null) ? tool.InventoryButtonPrefab : buttonPrefab);
			if (!((UnityEngine.Object)(object)toolInventoryButton == (UnityEngine.Object)null))
			{
				ToolInventoryButton toolInventoryButton2 = UnityEngine.Object.Instantiate<ToolInventoryButton>(toolInventoryButton, GetContentRoot());
				RectTransform component = ((Component)toolInventoryButton2).GetComponent<RectTransform>();
				if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
				{
					((Transform)component).localScale = Vector3.one;
					((Transform)component).localRotation = Quaternion.identity;
					component.anchoredPosition = Vector2.zero;
				}
				toolInventoryButton2.Bind(tool, placementManager);
				buttons.Add(toolInventoryButton2);
			}
		}

		public void Refresh()
		{
			foreach (ToolInventoryButton button in buttons)
			{
				if ((UnityEngine.Object)(object)button != (UnityEngine.Object)null)
				{
					button.Refresh();
				}
			}
			UpdatePaginationControls();
		}

		public void PreviousPage()
		{
			SetPage(currentPage - 1);
		}

		public void NextPage()
		{
			SetPage(currentPage + 1);
		}

		public void SetPage(int page)
		{
			int totalPages = TotalPages;
			currentPage = ((totalPages != 0) ? Mathf.Clamp(page, 0, totalPages - 1) : 0);
			ApplyCurrentPage();
		}

		private void Clear()
		{
			foreach (ToolInventoryButton button in buttons)
			{
				if ((UnityEngine.Object)(object)button != (UnityEngine.Object)null)
				{
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)button).gameObject);
				}
			}
			buttons.Clear();
			ApplyCurrentPage();
		}

		private Transform GetContentRoot()
		{
			EnsureContentRoot();
			return (Transform)(((UnityEngine.Object)(object)contentRoot != (UnityEngine.Object)null) ? ((object)contentRoot) : ((object)((Component)this).transform));
		}

		private void ResizeContentToButtons()
		{
			//IL_0191: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)contentRoot == (UnityEngine.Object)null)
			{
				return;
			}
			float num = 0f;
			float num2 = 0f;
			int num3 = currentPage * Mathf.Max(1, itemsPerPage);
			int num4 = Mathf.Min(num3 + Mathf.Max(1, itemsPerPage), buttons.Count);
			for (int i = num3; i < num4; i++)
			{
				RectTransform val = (((UnityEngine.Object)(object)buttons[i] != (UnityEngine.Object)null) ? ((Component)buttons[i]).GetComponent<RectTransform>() : null);
				float num5;
				Rect rect;
				if (!((UnityEngine.Object)(object)val != (UnityEngine.Object)null))
				{
					num5 = 100f;
				}
				else
				{
					rect = val.rect;
					num5 = rect.width;
				}
				float num6 = num5;
				float num7;
				if (!((UnityEngine.Object)(object)val != (UnityEngine.Object)null))
				{
					num7 = 100f;
				}
				else
				{
					rect = val.rect;
					num7 = rect.height;
				}
				float num8 = num7;
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					val.anchorMin = new Vector2(0.5f, 1f);
					val.anchorMax = new Vector2(0.5f, 1f);
					val.pivot = new Vector2(0.5f, 0.5f);
					val.anchoredPosition = new Vector2(0f, 0f - (num + num8 * 0.5f));
				}
				num += num8 + 20f;
				num2 = Mathf.Max(num2, num6);
			}
			float num9 = Mathf.Max(0f, num - 20f);
			contentRoot.anchorMin = new Vector2(0.5f, 1f);
			contentRoot.anchorMax = new Vector2(0.5f, 1f);
			contentRoot.pivot = new Vector2(0.5f, 1f);
			contentRoot.anchoredPosition = Vector2.zero;
			contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num2);
			contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num9);
			Canvas.ForceUpdateCanvases();
		}

		private void ApplyCurrentPage()
		{
			int num = Mathf.Max(1, itemsPerPage);
			int num2 = currentPage * num;
			int num3 = Mathf.Min(num2 + num, buttons.Count);
			for (int i = 0; i < buttons.Count; i++)
			{
				if ((UnityEngine.Object)(object)buttons[i] != (UnityEngine.Object)null)
				{
					((Component)buttons[i]).gameObject.SetActive(i >= num2 && i < num3);
				}
			}
			ResizeContentToButtons();
			UpdatePaginationControls();
		}

		private void UpdatePaginationControls()
		{
			int totalPages = TotalPages;
			bool flag = totalPages > 1;
			if ((UnityEngine.Object)(object)previousPageButton != (UnityEngine.Object)null)
			{
				((Selectable)previousPageButton).interactable = flag && currentPage > 0;
			}
			if ((UnityEngine.Object)(object)nextPageButton != (UnityEngine.Object)null)
			{
				((Selectable)nextPageButton).interactable = flag && currentPage < totalPages - 1;
			}
			if ((UnityEngine.Object)(object)pageLabel != (UnityEngine.Object)null)
			{
				pageLabel.text = ((totalPages == 0) ? "0/0" : $"{currentPage + 1}/{totalPages}");
			}
		}

		private void WirePageButtons()
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Expected O, but got Unknown
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Expected O, but got Unknown
			if (!pageButtonsWired)
			{
				if ((UnityEngine.Object)(object)previousPageButton != (UnityEngine.Object)null)
				{
					((UnityEvent)previousPageButton.onClick).AddListener(new UnityAction(PreviousPage));
				}
				if ((UnityEngine.Object)(object)nextPageButton != (UnityEngine.Object)null)
				{
					((UnityEvent)nextPageButton.onClick).AddListener(new UnityAction(NextPage));
				}
				pageButtonsWired = true;
			}
		}

		private void UnwirePageButtons()
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Expected O, but got Unknown
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Expected O, but got Unknown
			if (pageButtonsWired)
			{
				if ((UnityEngine.Object)(object)previousPageButton != (UnityEngine.Object)null)
				{
					((UnityEvent)previousPageButton.onClick).RemoveListener(new UnityAction(PreviousPage));
				}
				if ((UnityEngine.Object)(object)nextPageButton != (UnityEngine.Object)null)
				{
					((UnityEvent)nextPageButton.onClick).RemoveListener(new UnityAction(NextPage));
				}
				pageButtonsWired = false;
			}
		}

		private void EnsureContentRoot()
		{
			if (!((UnityEngine.Object)(object)contentRoot != (UnityEngine.Object)null))
			{
				Transform val = ((Component)this).transform.Find("Viewport");
				Transform val2 = (((UnityEngine.Object)(object)val != (UnityEngine.Object)null) ? val.Find("Content") : ((Component)this).transform.Find("Content"));
				contentRoot = (((UnityEngine.Object)(object)val2 != (UnityEngine.Object)null) ? ((Component)val2).GetComponent<RectTransform>() : null);
			}
		}

		private void OnDestroy()
		{
			UnwirePageButtons();
			if ((UnityEngine.Object)(object)placementManager != (UnityEngine.Object)null)
			{
				placementManager.InventoryChanged -= Refresh;
			}
		}
	}
}
