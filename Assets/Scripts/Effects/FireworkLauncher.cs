using System.Collections;
using UnityEngine;

namespace BallGame.Effects
{
    public sealed class FireworkLauncher : MonoBehaviour
    {
        [SerializeField] private FireworkEffect fireworkPrefab;
        [SerializeField] private bool playOnEnable = true;
        [Header("Launch Area")]
        [Min(0f)]
        [SerializeField] private float horizontalRange = 8f;
        [Min(0.1f)]
        [SerializeField] private float minimumExplosionHeight = 2f;
        [Min(0.1f)]
        [SerializeField] private float maximumExplosionHeight = 5f;
        [Header("Schedule")]
        [Min(1)]
        [SerializeField] private int fireworksPerRound = 8;
        [Min(0f)]
        [SerializeField] private float launchInterval = 0.35f;
        [Min(0f)]
        [SerializeField] private float roundInterval = 1.5f;

        private Coroutine launchRoutine;
        private bool missingPrefabWarningShown;

        public bool IsPlaying => launchRoutine != null;

        private void OnEnable()
        {
            if (Application.isPlaying && playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnValidate()
        {
            horizontalRange = Mathf.Max(0f, horizontalRange);
            fireworksPerRound = Mathf.Max(1, fireworksPerRound);
            launchInterval = Mathf.Max(0f, launchInterval);
            roundInterval = Mathf.Max(0f, roundInterval);

            float minimum = Mathf.Max(0.1f, Mathf.Min(minimumExplosionHeight, maximumExplosionHeight));
            float maximum = Mathf.Max(0.1f, Mathf.Max(minimumExplosionHeight, maximumExplosionHeight));
            minimumExplosionHeight = minimum;
            maximumExplosionHeight = maximum;
        }

        public void Play()
        {
            if (!CanStartLaunching())
            {
                return;
            }

            launchRoutine = StartCoroutine(LaunchContinuously());
        }

        public void PlayOnce(int count)
        {
            if (!CanStartLaunching())
            {
                return;
            }

            launchRoutine = StartCoroutine(LaunchSingleRound(ClampSingleRoundCount(count)));
        }

        public void Stop()
        {
            if (launchRoutine != null)
            {
                StopCoroutine(launchRoutine);
                launchRoutine = null;
            }

            FireworkEffect[] activeFireworks = GetComponentsInChildren<FireworkEffect>(true);
            for (int i = 0; i < activeFireworks.Length; i++)
            {
                FireworkEffect firework = activeFireworks[i];
                if (firework == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(firework.gameObject);
                }
                else
                {
                    DestroyImmediate(firework.gameObject);
                }
            }
        }

        internal void ConfigureForCamera(Camera targetCamera)
        {
            if (targetCamera == null)
            {
                return;
            }

            float worldPlaneZ = transform.position.z;
            float distance = Mathf.Max(0.01f, Mathf.Abs(worldPlaneZ - targetCamera.transform.position.z));
            Vector3 bottomCenter = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0, distance));
            Vector3 left = targetCamera.ViewportToWorldPoint(new Vector3(0.1f, 0, distance));
            Vector3 right = targetCamera.ViewportToWorldPoint(new Vector3(0.9f, 0, distance));
            Vector3 minimumPoint = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.6f, distance));
            Vector3 maximumPoint = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.9f, distance));

            transform.position = new Vector3(bottomCenter.x, bottomCenter.y, worldPlaneZ);
            horizontalRange = Mathf.Max(0f, Mathf.Abs(right.x - left.x));
            minimumExplosionHeight = Mathf.Max(0.1f, minimumPoint.y - bottomCenter.y);
            maximumExplosionHeight = Mathf.Max(minimumExplosionHeight, maximumPoint.y - bottomCenter.y);
        }

        private bool CanStartLaunching()
        {
            if (launchRoutine != null || !isActiveAndEnabled || !Application.isPlaying)
            {
                return false;
            }

            if (fireworkPrefab == null)
            {
                if (!missingPrefabWarningShown)
                {
                    Debug.LogWarning("FireworkLauncher requires a FireworkEffect prefab.", this);
                    missingPrefabWarningShown = true;
                }

                return false;
            }

            missingPrefabWarningShown = false;
            return true;
        }

        private static int ClampSingleRoundCount(int count)
        {
            return Mathf.Max(1, count);
        }

        private IEnumerator LaunchContinuously()
        {
            while (true)
            {
                for (int i = 0; i < fireworksPerRound; i++)
                {
                    LaunchOne();
                    if (launchInterval > 0f && i < fireworksPerRound - 1)
                    {
                        yield return new WaitForSeconds(launchInterval);
                    }
                }

                if (roundInterval > 0f)
                {
                    yield return new WaitForSeconds(roundInterval);
                }
                else
                {
                    yield return null;
                }
            }
        }

        private IEnumerator LaunchSingleRound(int count)
        {
            for (int i = 0; i < count; i++)
            {
                LaunchOne();
                if (launchInterval > 0f && i < count - 1)
                {
                    yield return new WaitForSeconds(launchInterval);
                }
            }

            yield return null;
            launchRoutine = null;
        }

        private void LaunchOne()
        {
            float height = Random.Range(minimumExplosionHeight, maximumExplosionHeight);
            float horizontalOffset = Random.Range(-horizontalRange * 0.5f, horizontalRange * 0.5f);
            Vector3 launchLine = transform.position;
            Vector3 explosionPosition = new Vector3(
                launchLine.x + horizontalOffset,
                launchLine.y + height,
                launchLine.z);

            FireworkEffect firework = Instantiate(fireworkPrefab, explosionPosition, Quaternion.identity, transform);
            firework.Play(height);
        }

        private void OnDrawGizmosSelected()
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(minimumExplosionHeight, maximumExplosionHeight));
            float maximum = Mathf.Max(minimum, Mathf.Max(minimumExplosionHeight, maximumExplosionHeight));
            Vector3 origin = transform.position;

            Gizmos.color = new Color(1f, 0.55f, 0.1f, 1f);
            Gizmos.DrawLine(
                origin + Vector3.left * horizontalRange * 0.5f,
                origin + Vector3.right * horizontalRange * 0.5f);

            Gizmos.color = new Color(1f, 0.8f, 0.15f, 0.65f);
            Vector3 center = origin + Vector3.up * ((minimum + maximum) * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(horizontalRange, maximum - minimum, 0f));
        }
    }
}
