using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    /// <summary>一人のヒロインを象徴する装備の樹のノード定義。座標は根を下、枝を上へ描くための表示用値。</summary>
    public sealed class WeaponTreeNode
    {
        public string Id { get; }
        public string WeaponId { get; }
        public float DisplayX { get; }
        public float DisplayY { get; }
        public string TerminalPath { get; }
        public IReadOnlyCollection<string> ParentIds { get; }
        public IReadOnlyDictionary<string, int> MaterialCosts { get; }

        public WeaponTreeNode(string id, string weaponId, float displayX, float displayY, string terminalPath,
            IEnumerable<string> parentIds, IReadOnlyDictionary<string, int> materialCosts)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(weaponId)) throw new ArgumentException("Node and weapon ids are required.");
            if (displayY < 0f) throw new ArgumentOutOfRangeException(nameof(displayY));
            if (!string.IsNullOrEmpty(terminalPath) && terminalPath != "alpha" && terminalPath != "beta" && terminalPath != "gamma")
                throw new ArgumentException("Terminal path must be alpha, beta, gamma, or empty.", nameof(terminalPath));
            Id = id;
            WeaponId = weaponId;
            DisplayX = displayX;
            DisplayY = displayY;
            TerminalPath = terminalPath ?? string.Empty;
            ParentIds = (parentIds ?? Enumerable.Empty<string>()).Where(parent => !string.IsNullOrWhiteSpace(parent)).Distinct().ToArray();
            MaterialCosts = new Dictionary<string, int>((materialCosts ?? new Dictionary<string, int>())
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value >= 0));
        }
    }

    /// <summary>樹の解放状態。素材を消費する実処理は、全コストを確認してから呼び出し側で同一保存単位に確定する。</summary>
    public sealed class WeaponTreeState
    {
        private readonly IReadOnlyDictionary<string, WeaponTreeNode> _nodes;
        private readonly HashSet<string> _unlockedNodeIds;
        public IReadOnlyCollection<string> UnlockedNodeIds => _unlockedNodeIds;

        public WeaponTreeState(IEnumerable<WeaponTreeNode> nodes, string initialNodeId)
        {
            var allNodes = (nodes ?? throw new ArgumentNullException(nameof(nodes))).ToArray();
            if (allNodes.Length == 0 || allNodes.Select(node => node.Id).Distinct().Count() != allNodes.Length)
                throw new ArgumentException("Weapon nodes must be non-empty and unique.", nameof(nodes));
            _nodes = allNodes.ToDictionary(node => node.Id);
            if (!_nodes.TryGetValue(initialNodeId, out var initial) || initial.ParentIds.Count != 0)
                throw new ArgumentException("The initial node must be a root node.", nameof(initialNodeId));
            _unlockedNodeIds = new HashSet<string> { initial.Id };
        }

        public bool CanUnlock(string nodeId, IReadOnlyDictionary<string, int> ownedMaterials)
        {
            if (!_nodes.TryGetValue(nodeId, out var node) || _unlockedNodeIds.Contains(nodeId)) return false;
            if (!node.ParentIds.All(_unlockedNodeIds.Contains)) return false;
            return node.MaterialCosts.All(cost => ownedMaterials != null && ownedMaterials.TryGetValue(cost.Key, out var count) && count >= cost.Value);
        }

        public bool TryUnlock(string nodeId, IReadOnlyDictionary<string, int> ownedMaterials)
        {
            if (!CanUnlock(nodeId, ownedMaterials)) return false;
            return _unlockedNodeIds.Add(nodeId);
        }

        public bool IsBloomed(string nodeId) => _unlockedNodeIds.Contains(nodeId);
    }
}
