using System.Collections.Generic;
using BallGame.Core;
using UnityEngine;

namespace BallGame.UI
{
    public sealed class WorldTargetIndicatorGroup : MonoBehaviour
    {
        private readonly List<WorldTargetIndicator> indicators = new List<WorldTargetIndicator>();

        public void BindTargets(WorldTargetIndicator template, GameManager manager, Camera worldCamera, IEnumerable<Transform> targets, Canvas canvas)
        {
            if (template == null)
            {
                HideAll();
                return;
            }

            if (!indicators.Contains(template))
            {
                indicators.Insert(0, template);
            }

            int index = 0;
            if (targets != null)
            {
                foreach (Transform target in targets)
                {
                    if (target == null)
                    {
                        continue;
                    }

                    WorldTargetIndicator indicator = GetOrCreateIndicator(template, index);
                    indicator.gameObject.SetActive(true);
                    indicator.Bind(manager, worldCamera, target, canvas);
                    index++;
                }
            }

            for (int i = index; i < indicators.Count; i++)
            {
                if (indicators[i] != null)
                {
                    indicators[i].gameObject.SetActive(false);
                }
            }
        }

        public void HideAll()
        {
            for (int i = 0; i < indicators.Count; i++)
            {
                if (indicators[i] != null)
                {
                    indicators[i].gameObject.SetActive(false);
                }
            }
        }

        private WorldTargetIndicator GetOrCreateIndicator(WorldTargetIndicator template, int index)
        {
            while (indicators.Count <= index)
            {
                WorldTargetIndicator clone = Instantiate(template, template.transform.parent);
                clone.name = template.name + "_" + indicators.Count.ToString("00");
                indicators.Add(clone);
            }

            return indicators[index] != null ? indicators[index] : template;
        }
    }
}
