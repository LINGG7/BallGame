using BallGame.Placement;
using BallGame.Core;

// Only Unity/placement boundaries are doubled. The group matcher and supply validation
// above are compiled directly from the production PlacementHintGroup.cs source.
namespace UnityEngine
{
    public class SpriteRenderer { public bool enabled = true; public object sprite = new object(); }
    public class GameObject
    {
        public T[] GetComponentsInChildren<T>() where T : new() => new[] { new T() };
    }
}
namespace BallGame.Core
{
    public class ToolDefinition
    {
        public UnityEngine.GameObject Prefab = new UnityEngine.GameObject();
        public string DisplayName = "Tool";
    }
}
namespace BallGame.Placement
{
    public class PlaceableTool { public int Id; public bool CanEdit = true; public bool IsPreview; public ToolDefinition Definition; }
    public class PlacementHintTarget
    {
        public ToolDefinition tool;
        public bool IsValid = true;
        public Func<PlaceableTool, bool> Accepts = _ => false;
        public bool IsSatisfiedBy(PlaceableTool candidate) => candidate != null && !candidate.IsPreview && candidate.Definition == tool && Accepts(candidate);
    }
    public class PlacementManager
    {
        public List<PlaceableTool> PlacedTools = new List<PlaceableTool>();
        public Dictionary<ToolDefinition, int> Remaining = new Dictionary<ToolDefinition, int>();
        public int GetRemaining(ToolDefinition tool) => Remaining.TryGetValue(tool, out int count) ? count : 0;
        public bool CanPlace(ToolDefinition tool) => GetRemaining(tool) > 0;
    }
}
static class Program
{
    static int assertions;
    static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
    static int Brute(bool[,] edges, int target, int used)
    {
        if (target == edges.GetLength(0)) return 0;
        int best = Brute(edges, target + 1, used);
        for (int i = 0; i < edges.GetLength(1); i++)
            if (edges[target, i] && (used & (1 << i)) == 0) best = Math.Max(best, 1 + Brute(edges, target + 1, used | (1 << i)));
        return best;
    }
    static void Main()
    {
        var random = new Random(91734);
        for (int run = 0; run < 3000; run++)
        {
            int targets = random.Next(1, 7), instances = random.Next(0, 7);
            var edges = new bool[targets, instances];
            var definition = new ToolDefinition();
            var group = new PlacementHintGroup();
            var tools = Enumerable.Range(0, instances).Select(i => new PlaceableTool { Id = i, Definition = definition }).ToList();
            for (int t = 0; t < targets; t++)
            {
                int row = t;
                for (int i = 0; i < instances; i++) edges[t, i] = random.Next(3) != 0;
                group.targets.Add(new PlacementHintTarget { tool = definition, Accepts = tool => edges[row, tool.Id] });
            }
            int[] matches = group.MatchTargets(tools);
            Check(matches.Count(i => i >= 0) == Brute(edges, 0, 0), "Matcher failed to maximize coverage.");
            Check(matches.Where(i => i >= 0).Distinct().Count() == matches.Count(i => i >= 0), "Instance counted twice.");
            for (int t = 0; t < targets; t++) Check(matches[t] < 0 || edges[t, matches[t]], "Illegal matching edge.");
        }
        var type = new ToolDefinition();
        var pair = new PlacementHintGroup { targets = new List<PlacementHintTarget>
        {
            new PlacementHintTarget { tool = type, Accepts = _ => true },
            new PlacementHintTarget { tool = type, Accepts = _ => true }
        }};
        var manager = new PlacementManager();
        manager.Remaining[type] = 1;
        Check(!pair.HasEnoughTools(manager, out _), "One item cannot fill two targets.");
        manager.PlacedTools.Add(new PlaceableTool { Definition = type, CanEdit = false });
        Check(pair.HasEnoughTools(manager, out _), "Correct fixed item plus one stock should suffice.");
        manager.PlacedTools[0].IsPreview = true;
        Check(!pair.HasEnoughTools(manager, out _), "Preview must not count toward availability.");
        manager.Remaining[type] = int.MaxValue;
        manager.PlacedTools[0].IsPreview = false;
        Check(pair.HasEnoughTools(manager, out _), "Unlimited inventory overflowed.");
        Check(pair.Validate(out _), "Valid group rejected.");
        pair.targets[1].IsValid = false;
        Check(!pair.Validate(out _), "Invalid member silently skipped.");
        pair.targets.Clear();
        Check(!pair.Validate(out _), "Empty group accepted.");
        Console.WriteLine($"PASS: 3000 matching graphs compared with exhaustive search; {assertions} assertions including supply/validation.");
    }
}
