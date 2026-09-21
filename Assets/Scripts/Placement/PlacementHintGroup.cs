using System;
using System.Collections.Generic;
using BallGame.Core;
using UnityEngine;

namespace BallGame.Placement
{
    [Serializable]
    public sealed class PlacementHintGroup
    {
        public string groupName = "提示组";
        public List<PlacementHintTarget> targets = new List<PlacementHintTarget>();

        public bool Validate(out string error)
        {
            if (targets == null || targets.Count == 0) { error = "提示组为空，请配置目标"; return false; }
            foreach (var target in targets)
            {
                if (target == null || !target.IsValid) { error = "提示组含无效目标，请检查配置"; return false; }
                bool visible = false;
                foreach (var sprite in target.tool.Prefab.GetComponentsInChildren<SpriteRenderer>())
                    if (sprite.enabled && sprite.sprite != null) { visible = true; break; }
                if (!visible) { error = "提示道具缺少可显示的精灵"; return false; }
            }
            error = null;
            return true;
        }

        // Augmenting paths give maximum cardinality, unlike a greedy nearest/first match.
        // An instance can satisfy only one target, including overlapping tolerance regions.
        public int[] MatchTargets(IReadOnlyList<PlaceableTool> placed)
        {
            int count = targets != null ? targets.Count : 0;
            int[] owner = new int[placed.Count];
            for (int i = 0; i < owner.Length; i++) owner[i] = -1;
            for (int target = 0; target < count; target++)
                Augment(target, placed, owner, new bool[placed.Count]);
            int[] matches = new int[count];
            for (int i = 0; i < count; i++) matches[i] = -1;
            for (int i = 0; i < owner.Length; i++) if (owner[i] >= 0) matches[owner[i]] = i;
            return matches;
        }

        private bool Augment(int target, IReadOnlyList<PlaceableTool> placed, int[] owner, bool[] seen)
        {
            if (targets[target] == null) return false;
            for (int i = 0; i < placed.Count; i++)
            {
                if (seen[i] || !targets[target].IsSatisfiedBy(placed[i])) continue;
                seen[i] = true;
                if (owner[i] < 0 || Augment(owner[i], placed, owner, seen))
                {
                    owner[i] = target;
                    return true;
                }
            }
            return false;
        }

        public bool HasEnoughTools(PlacementManager placement, out string error)
        {
            var required = new Dictionary<ToolDefinition, int>();
            foreach (var target in targets)
            {
                required.TryGetValue(target.tool, out int count);
                required[target.tool] = count + 1;
            }
            var matches = MatchTargets(placement.PlacedTools);
            var fixedMatches = new HashSet<int>(matches);
            foreach (var pair in required)
            {
                long available = placement.CanPlace(pair.Key) ? placement.GetRemaining(pair.Key) : 0;
                for (int i = 0; i < placement.PlacedTools.Count; i++)
                {
                    var tool = placement.PlacedTools[i];
                    if (tool != null && !tool.IsPreview && tool.Definition == pair.Key
                        && (tool.CanEdit || fixedMatches.Contains(i))) available++;
                }
                if (available < pair.Value)
                {
                    error = "本组道具数量不足：" + pair.Key.DisplayName;
                    return false;
                }
            }
            error = null;
            return true;
        }
    }
}
