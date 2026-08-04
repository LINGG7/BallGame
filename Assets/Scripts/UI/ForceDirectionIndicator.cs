using System;
using BallGame.Core;
using UnityEngine;

namespace BallGame.UI
{
	public sealed class ForceDirectionIndicator : MonoBehaviour
	{
		private const string ArrowResourcePath = "Art/UI/Arrow";

		private const string ArrowObjectName = "ForceDirectionArrow";

		[SerializeField]
		private Vector2 localDirection = Vector2.up;

		[SerializeField]
		private Color arrowColor = Color.white;

		[SerializeField]
		private float worldWidth = 0.22f;

		[SerializeField]
		private float worldLength = 0.8f;

		private GameManager gameManager;

		private SpriteRenderer arrowRenderer;

		public Vector2 LocalDirection => localDirection;

		public SpriteRenderer ArrowRenderer => arrowRenderer;

		private void Awake()
		{
			EnsureArrow();
			FindGameManager();
			RefreshArrow();
		}

		private void OnEnable()
		{
			FindGameManager();
			SubscribeToGameManager();
			RefreshVisibility();
		}

		private void OnDisable()
		{
			UnsubscribeFromGameManager();
			SetVisible(visible: false);
		}

		private void LateUpdate()
		{
			if ((UnityEngine.Object)(object)gameManager == (UnityEngine.Object)null)
			{
				FindGameManager();
				SubscribeToGameManager();
			}
			RefreshArrow();
			RefreshVisibility();
		}

		public void Configure(Vector2 direction, Color color, float width = 0.22f, float length = 0.8f)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			localDirection = ((direction.sqrMagnitude > 0.0001f) ? direction.normalized : Vector2.up);
			arrowColor = color;
			worldWidth = Mathf.Max(0.01f, width);
			worldLength = Mathf.Max(0.01f, length);
			EnsureArrow();
			RefreshArrow();
			RefreshVisibility();
		}

		private void EnsureArrow()
		{
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Expected O, but got Unknown
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)arrowRenderer == (UnityEngine.Object)null)
			{
				Transform val = ((Component)this).transform.Find("ForceDirectionArrow");
				if ((UnityEngine.Object)(object)val != (UnityEngine.Object)null)
				{
					arrowRenderer = ((Component)val).GetComponent<SpriteRenderer>();
				}
			}
			if ((UnityEngine.Object)(object)arrowRenderer == (UnityEngine.Object)null)
			{
				GameObject val2 = new GameObject("ForceDirectionArrow", new Type[1] { typeof(SpriteRenderer) });
				val2.transform.SetParent(((Component)this).transform, false);
				arrowRenderer = val2.GetComponent<SpriteRenderer>();
			}
			if ((UnityEngine.Object)(object)arrowRenderer.sprite == (UnityEngine.Object)null)
			{
				arrowRenderer.sprite = Resources.Load<Sprite>("Art/UI/Arrow");
			}
			((Renderer)arrowRenderer).sortingOrder = 20;
			arrowRenderer.color = arrowColor;
		}

		private void RefreshArrow()
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			if (!((UnityEngine.Object)(object)arrowRenderer == (UnityEngine.Object)null))
			{
				Vector2 val = ((localDirection.sqrMagnitude > 0.0001f) ? localDirection.normalized : Vector2.up);
				((Component)arrowRenderer).transform.localPosition = (Vector2)(localDirection.normalized * 0.35f);
				((Component)arrowRenderer).transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(val.y, val.x) * 57.29578f + 90f);
				Vector3 lossyScale = ((Component)this).transform.lossyScale;
				float num = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.x));
				float num2 = Mathf.Max(0.0001f, Mathf.Abs(lossyScale.y));
				Vector2 val2;
				if (!((UnityEngine.Object)(object)arrowRenderer.sprite != (UnityEngine.Object)null))
				{
					val2 = Vector2.one;
				}
				else
				{
					Bounds bounds = arrowRenderer.sprite.bounds;
					val2 = (Vector2)(bounds.size);
				}
				Vector2 val3 = val2;
				float num3 = Mathf.Max(0.0001f, val3.x);
				float num4 = Mathf.Max(0.0001f, val3.y);
				((Component)arrowRenderer).transform.localScale = new Vector3(worldWidth / (num3 * num), worldLength / (num4 * num2), 1f);
				arrowRenderer.color = arrowColor;
			}
		}

		private void FindGameManager()
		{
			if ((UnityEngine.Object)(object)gameManager == (UnityEngine.Object)null)
			{
				gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
			}
		}

		private void SubscribeToGameManager()
		{
			if ((UnityEngine.Object)(object)gameManager != (UnityEngine.Object)null)
			{
				gameManager.StateChanged -= HandleGameStateChanged;
				gameManager.StateChanged += HandleGameStateChanged;
			}
		}

		private void UnsubscribeFromGameManager()
		{
			if ((UnityEngine.Object)(object)gameManager != (UnityEngine.Object)null)
			{
				gameManager.StateChanged -= HandleGameStateChanged;
			}
		}

		private void HandleGameStateChanged(GameState state)
		{
			RefreshVisibility();
		}

		private void RefreshVisibility()
		{
			SetVisible((UnityEngine.Object)(object)gameManager != (UnityEngine.Object)null && gameManager.State == GameState.Build);
		}

		private void SetVisible(bool visible)
		{
			if ((UnityEngine.Object)(object)arrowRenderer != (UnityEngine.Object)null)
			{
				((Renderer)arrowRenderer).enabled = visible && (UnityEngine.Object)(object)arrowRenderer.sprite != (UnityEngine.Object)null;
			}
		}
	}
}
