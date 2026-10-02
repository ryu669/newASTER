using System;
using System.Collections.Generic;

namespace NewAster.Core
{
    public sealed class OopartDefinition
    {
        public const int LevelCap = 120;
        public string Id { get; }
        public int RandomStatMaximum { get; }

        public OopartDefinition(string id, int randomStatMaximum)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An oopart id is required.", nameof(id));
            if (randomStatMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(randomStatMaximum));
            Id = id;
            RandomStatMaximum = randomStatMaximum;
        }
    }

    public sealed class OopartInstance
    {
        public string DefinitionId { get; }
        public int Level { get; private set; } = 1;
        public int RandomStat { get; private set; }
        public bool DirectUpgradeUnlocked { get; private set; }

        internal OopartInstance(OopartDefinition definition, int randomStat)
        {
            DefinitionId = definition.Id;
            RandomStat = randomStat;
            DirectUpgradeUnlocked = randomStat >= DirectUpgradeThreshold(definition);
        }

        internal bool Merge(OopartDefinition definition, int incomingRandomStat)
        {
            var improved = incomingRandomStat > RandomStat;
            if (improved) RandomStat = incomingRandomStat;
            DirectUpgradeUnlocked |= RandomStat >= DirectUpgradeThreshold(definition);
            return improved;
        }

        internal bool TryDirectUpgrade(OopartDefinition definition, int amount)
        {
            if (!DirectUpgradeUnlocked || amount <= 0 || RandomStat >= definition.RandomStatMaximum) return false;
            RandomStat = Math.Min(definition.RandomStatMaximum, RandomStat + amount);
            return true;
        }

        private static int DirectUpgradeThreshold(OopartDefinition definition) => (int)Math.Ceiling(definition.RandomStatMaximum * 0.8d);
    }

    /// <summary>同名オーパーツは低い抽選値で劣化させない。固定成長と固有能力は定義側で付与する。</summary>
    public sealed class OopartInventory
    {
        private readonly Dictionary<string, OopartInstance> _instances = new Dictionary<string, OopartInstance>();
        public IReadOnlyDictionary<string, OopartInstance> Instances => _instances;

        public OopartInstance Merge(OopartDefinition definition, int incomingRandomStat)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (incomingRandomStat < 0 || incomingRandomStat > definition.RandomStatMaximum)
                throw new ArgumentOutOfRangeException(nameof(incomingRandomStat));
            if (!_instances.TryGetValue(definition.Id, out var instance))
            {
                instance = new OopartInstance(definition, incomingRandomStat);
                _instances.Add(definition.Id, instance);
                return instance;
            }
            instance.Merge(definition, incomingRandomStat);
            return instance;
        }

        public bool TryDirectUpgrade(OopartDefinition definition, int materialAmount, int materialCost, int increase)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (materialCost <= 0 || increase <= 0 || materialAmount < materialCost) return false;
            return _instances.TryGetValue(definition.Id, out var instance) && instance.TryDirectUpgrade(definition, increase);
        }
    }
}
