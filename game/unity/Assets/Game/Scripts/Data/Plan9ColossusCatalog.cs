using System;
using System.Collections.Generic;
using NewAster.Core;
namespace NewAster.Data
{
    public static partial class ColossusCombatCatalog
    {
        // Add an enemy here only with its own definition and connected art manifest.
        private static readonly Dictionary<string,Func<ColossusCombatDef>> AuthoredDefinitions=new Dictionary<string,Func<ColossusCombatDef>> {
            {GreenReturnDragonVerticalSlice.ColossusId,GreenReturnDragon},
            {"colossus.red-crystal-tyrant",RedCrystalTyrant},
            {"colossus.memory-crystal-dragon",MemoryCrystalDragon}
        };
        private static ColossusCombatDef MemoryCrystalDragon()=>new ColossusCombatDef {
            id="colossus.memory-crystal-dragon",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=2000,hpPerLevel=155,gaugeMax=4,baseDamage=13,damagePerLevel=2,majorBonus=28,ultimateBonus=58,
            normalAction="結晶砲撃",enragedAction="記憶過負荷",majorAction="大技：記晶降星",ultimateAction="極大技：万象再記録",
            enemySpeed=100,enrageHpPercent=55,enrageDamagePercent=130,attackBreakDamagePercent=65,majorDamageType="magic",majorWaitPercent=160,
            parts=new[]{
                new ColossusPartCombatDef{id="memory.core",role="gauge",breakEffect="gauge-down",baseHp=290,hpPerLevel=12},
                new ColossusPartCombatDef{id="memory.cannon",role="attack",breakEffect="",baseHp=370,hpPerLevel=15},
                new ColossusPartCombatDef{id="memory.shield",role="armor",breakEffect="",baseHp=420,hpPerLevel=17},
                new ColossusPartCombatDef{id="memory.antenna",role="drain",breakEffect="",baseHp=270,hpPerLevel=11}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="結晶砲撃",targetRule="lowest-hp",requiredPartId="memory.cannon",damageType="magic",damagePercent=100,gaugeGain=1,drainAmount=0,waitPercent=100},
                new ColossusActionCombatDef{name="記憶走査",targetRule="highest-resource",requiredPartId="memory.antenna",damageType="magic",damagePercent=75,gaugeGain=0,drainAmount=3,waitPercent=80},
                new ColossusActionCombatDef{name="高速照射",targetRule="all",requiredPartId="memory.cannon",damageType="magic",damagePercent=65,gaugeGain=2,drainAmount=0,waitPercent=70},
                new ColossusActionCombatDef{name="機殻の踏撃",targetRule="single",damagePercent=90,gaugeGain=1,drainAmount=0,waitPercent=110}
            }
        };
    }
}
