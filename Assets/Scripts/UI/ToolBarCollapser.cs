using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BallGame.UI
{
	public sealed class ToolBarCollapser : MonoBehaviour
	{
		[SerializeField]
		private RectTransform toolBar;

		[SerializeField]
		private Button toggleButton;

		[SerializeField]
		private TMP_Text toggleLabel;

		[SerializeField]
		[Min(0f)]
		private float duration = 0.35f;

		private Tween movementTween;

		private bool isCollapsed;

		private float expandedX;

		public bool IsCollapsed => isCollapsed;

		private void Awake()
		{
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)toolBar == (UnityEngine.Object)null)
			{
				Transform transform = ((Component)this).transform;
				toolBar = (RectTransform)(object)((transform is RectTransform) ? transform : null);
			}
			expandedX = (((UnityEngine.Object)(object)toolBar != (UnityEngine.Object)null) ? toolBar.anchoredPosition.x : 0f);
			isCollapsed = false;
			UpdateToggleLabel();
		}

		private void OnEnable()
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			if ((UnityEngine.Object)(object)toggleButton != (UnityEngine.Object)null)
			{
				((UnityEvent)toggleButton.onClick).RemoveListener(new UnityAction(Toggle));
				((UnityEvent)toggleButton.onClick).AddListener(new UnityAction(Toggle));
			}
		}

		private void OnDisable()
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			if ((UnityEngine.Object)(object)toggleButton != (UnityEngine.Object)null)
			{
				((UnityEvent)toggleButton.onClick).RemoveListener(new UnityAction(Toggle));
			}
			StopMovement();
		}

		public void Toggle()
		{
			SetCollapsed(!isCollapsed);
		}

		public void SetCollapsed(bool collapsed, bool animate = true)
		{
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)toolBar == (UnityEngine.Object)null) && isCollapsed != collapsed)
			{
				isCollapsed = collapsed;
				UpdateToggleLabel();
				StopMovement();
				float num = (isCollapsed ? GetCollapsedX() : expandedX);
				if (!animate || duration <= 0f)
				{
					SetHorizontalPosition(num);
				}
				else
				{
					movementTween = Tween.UIAnchoredPositionX(toolBar, num, duration, (Ease)27, 1, (CycleMode)0, 0f, 0f, true);
				}
			}
		}

		public void SetToggleInteractable(bool interactable)
		{
			if ((UnityEngine.Object)(object)toggleButton != (UnityEngine.Object)null)
			{
				((Selectable)toggleButton).interactable = interactable;
			}
		}

		private void StopMovement()
		{
			if (movementTween.isAlive)
			{
				movementTween.Stop();
			}
		}

		private void SetHorizontalPosition(float x)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)toolBar == (UnityEngine.Object)null))
			{
				Vector2 anchoredPosition = toolBar.anchoredPosition;
				anchoredPosition.x = x;
				toolBar.anchoredPosition = anchoredPosition;
			}
		}

		private float GetCollapsedX()
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			float result;
			if (!((UnityEngine.Object)(object)toolBar != (UnityEngine.Object)null))
			{
				result = expandedX;
			}
			else
			{
				Rect rect = toolBar.rect;
				result = 0f - rect.width;
			}
			return result;
		}

		private void UpdateToggleLabel()
		{
			if ((UnityEngine.Object)(object)toggleLabel != (UnityEngine.Object)null)
			{
				toggleLabel.text = (isCollapsed ? ">" : "<");
			}
		}
	}
}
