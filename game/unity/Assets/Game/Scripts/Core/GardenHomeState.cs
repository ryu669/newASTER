using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public sealed class FurnitureRecipe
    {
        public string FurnitureId { get; }
        public string RequiredGardenId { get; }
        public IReadOnlyDictionary<string, int> MaterialCosts { get; }

        public FurnitureRecipe(string furnitureId, string requiredGardenId, IReadOnlyDictionary<string, int> materialCosts)
        {
            if (string.IsNullOrWhiteSpace(furnitureId) || string.IsNullOrWhiteSpace(requiredGardenId))
                throw new ArgumentException("Furniture and garden ids are required.");
            FurnitureId = furnitureId;
            RequiredGardenId = requiredGardenId;
            MaterialCosts = new Dictionary<string, int>((materialCosts ?? throw new ArgumentNullException(nameof(materialCosts)))
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value > 0));
        }
    }

    public sealed class FurniturePlacement
    {
        public string PlacementId { get; }
        public string GardenId { get; }
        public string FurnitureId { get; }
        public FurniturePlacement(string placementId, string gardenId, string furnitureId)
        {
            PlacementId = placementId;
            GardenId = gardenId;
            FurnitureId = furnitureId;
        }
    }

    /// <summary>
    /// 箱庭のクラフト・配置・ヒロイン待機配置。
    /// 家具を置かないことは本編・恋人イベントの解放条件には使わない。
    /// </summary>
    public sealed class GardenHomeState
    {
        private readonly Dictionary<string, int> _furnitureInventory = new Dictionary<string, int>();
        private readonly Dictionary<string, FurniturePlacement> _placements = new Dictionary<string, FurniturePlacement>();
        private readonly Dictionary<string, string> _heroineGardenIds = new Dictionary<string, string>();

        public IReadOnlyDictionary<string, int> FurnitureInventory => _furnitureInventory;
        public IReadOnlyDictionary<string, FurniturePlacement> Placements => _placements;
        public IReadOnlyDictionary<string, string> HeroineGardenIds => _heroineGardenIds;

        /// <summary>素材不足なら何も減らさない。素材台帳の保存は呼び出し側でこの成功と同じ保存単位にする。</summary>
        public bool TryCraft(FurnitureRecipe recipe, IDictionary<string, int> materialStock)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (materialStock == null || recipe.MaterialCosts.Any(cost => !materialStock.TryGetValue(cost.Key, out var count) || count < cost.Value)) return false;
            foreach (var cost in recipe.MaterialCosts) materialStock[cost.Key] -= cost.Value;
            _furnitureInventory[recipe.FurnitureId] = _furnitureInventory.TryGetValue(recipe.FurnitureId, out var count) ? count + 1 : 1;
            return true;
        }

        public bool TryPlace(GardenUnlockState gardens, string gardenId, string furnitureId, string placementId)
        {
            if (gardens == null) throw new ArgumentNullException(nameof(gardens));
            if (string.IsNullOrWhiteSpace(placementId) || _placements.ContainsKey(placementId) || !gardens.IsUnlocked(gardenId)) return false;
            if (!_furnitureInventory.TryGetValue(furnitureId, out var count) || count <= 0) return false;
            _furnitureInventory[furnitureId] = count - 1;
            _placements.Add(placementId, new FurniturePlacement(placementId, gardenId, furnitureId));
            return true;
        }

        /// <summary>撤去では家具を失わない。</summary>
        public bool RemovePlacement(string placementId)
        {
            if (!_placements.TryGetValue(placementId, out var placement)) return false;
            _placements.Remove(placementId);
            _furnitureInventory[placement.FurnitureId] = _furnitureInventory.TryGetValue(placement.FurnitureId, out var count) ? count + 1 : 1;
            return true;
        }

        public bool TryPlaceHeroine(GardenUnlockState gardens, string heroineId, string gardenId)
        {
            if (gardens == null) throw new ArgumentNullException(nameof(gardens));
            if (string.IsNullOrWhiteSpace(heroineId) || !gardens.IsUnlocked(gardenId)) return false;
            _heroineGardenIds[heroineId] = gardenId;
            return true;
        }

        public bool RemoveHeroine(string heroineId) => _heroineGardenIds.Remove(heroineId);
    }
}
