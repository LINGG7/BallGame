using System.Collections.Generic;
using UnityEngine;

namespace BallGame.Core
{
    [CreateAssetMenu(menuName = "Ball Game/Tool Catalog")]
    public sealed class ToolCatalog : ScriptableObject
    {
        [SerializeField] private List<ToolDefinition> tools = new List<ToolDefinition>();

        public IReadOnlyList<ToolDefinition> Tools => tools;
    }
}
