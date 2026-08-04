using UnityEngine;

namespace BallGame.Core
{
	public sealed class MapBounds2D : MonoBehaviour
	{
		[SerializeField]
		private Vector2 center = new Vector2(0f, -0.4f);

		[SerializeField]
		private Vector2 size = new Vector2(20f, 9f);

		public Vector2 Center => center;

		public Vector2 Size => size;

		public Vector2 Min => center - size * 0.5f;

		public Vector2 Max => center + size * 0.5f;

		public Vector3 ClampCameraPosition(Camera targetCamera, Vector3 position)
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			if ((UnityEngine.Object)(object)targetCamera == (UnityEngine.Object)null)
			{
				return position;
			}
			float orthographicSize = targetCamera.orthographicSize;
			float num = orthographicSize * targetCamera.aspect;
			Vector2 min = Min;
			Vector2 max = Max;
			position.x = ClampAxis(position.x, min.x + num, max.x - num, center.x);
			position.y = ClampAxis(position.y, min.y + orthographicSize, max.y - orthographicSize, center.y);
			return position;
		}

		public void SetBounds(Vector2 newCenter, Vector2 newSize)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			center = newCenter;
			size = new Vector2(Mathf.Max(0.01f, newSize.x), Mathf.Max(0.01f, newSize.y));
		}

		private static float ClampAxis(float value, float min, float max, float fallback)
		{
			return (min > max) ? fallback : Mathf.Clamp(value, min, max);
		}

		private void OnDrawGizmos()
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.85f);
			Gizmos.DrawWireCube((Vector2)(center), (Vector2)(size));
		}
	}
}
