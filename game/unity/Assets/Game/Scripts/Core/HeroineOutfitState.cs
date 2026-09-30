using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    /// <summary>同一ボーン・共通ボディに着せる衣装メッシュの定義。能力・ジョブ状態を衣装で変えない。</summary>
    public sealed class HeroineOutfitDefinition
    {
        public string Id { get; }
        public string MeshAssetId { get; }
        public IReadOnlyCollection<string> CoveredBodyRegions { get; }
        public HeroineOutfitDefinition(string id, string meshAssetId, IEnumerable<string> coveredBodyRegions)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(meshAssetId)) throw new ArgumentException("Outfit and mesh ids are required.");
            Id = id; MeshAssetId = meshAssetId;
            CoveredBodyRegions = (coveredBodyRegions ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
        }
    }

    public sealed class HeroineOutfitState
    {
        private readonly IReadOnlyDictionary<string, HeroineOutfitDefinition> _outfits;
        public string EquippedOutfitId { get; private set; }
        public HeroineOutfitDefinition EquippedOutfit => _outfits[EquippedOutfitId];
        public HeroineOutfitState(IEnumerable<HeroineOutfitDefinition> outfits, string initialOutfitId)
        {
            _outfits = (outfits ?? throw new ArgumentNullException(nameof(outfits))).ToDictionary(outfit => outfit.Id);
            if (!_outfits.ContainsKey(initialOutfitId)) throw new ArgumentException("Initial outfit is unavailable.", nameof(initialOutfitId));
            EquippedOutfitId = initialOutfitId;
        }
        public bool TryEquip(string outfitId)
        {
            if (!_outfits.ContainsKey(outfitId)) return false;
            EquippedOutfitId = outfitId;
            return true;
        }
    }
}
