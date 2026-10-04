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
            {"colossus.memory-crystal-dragon",MemoryCrystalDragon},
            {"colossus.sky-tower-machine",SkyTowerMachine},
            {"colossus.crystal-rose-princess",CrystalRosePrincess},
            {"colossus.silver-sea-whale",SilverSeaWhale},
            {"colossus.heaven-tree-orochi",HeavenTreeOrochi}
        };
        private static ColossusCombatDef HeavenTreeOrochi()=>new ColossusCombatDef {
            id="colossus.heaven-tree-orochi",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=2600,hpPerLevel=185,gaugeMax=6,baseDamage=14,damagePerLevel=2,majorBonus=36,ultimateBonus=66,
            normalAction="蛇枝掃射",enragedAction="天樹繁茂",majorAction="大技：八岐翠嵐",ultimateAction="極大技：世界樹回生",
            enemySpeed=84,enrageHpPercent=50,enrageDamagePercent=135,attackBreakDamagePercent=60,majorDamageType="magic",majorWaitPercent=175,
            parts=new[]{
                new ColossusPartCombatDef{id="orochi.bud",role="gauge",breakEffect="gauge-down",baseHp=330,hpPerLevel=14},
                new ColossusPartCombatDef{id="orochi.branch",role="attack",breakEffect="",baseHp=450,hpPerLevel=18},
                new ColossusPartCombatDef{id="orochi.shield",role="armor",breakEffect="",baseHp=490,hpPerLevel=20},
                new ColossusPartCombatDef{id="orochi.root",role="drain",breakEffect="",baseHp=390,hpPerLevel=16}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="蛇枝掃射",targetRule="all",requiredPartId="orochi.branch",damageType="physical",damagePercent=70,gaugeGain=2,drainAmount=0,waitPercent=125},
                new ColossusActionCombatDef{name="吸水根の収奪",targetRule="highest-resource",requiredPartId="orochi.root",damageType="magic",damagePercent=70,gaugeGain=1,drainAmount=3,waitPercent=120},
                new ColossusActionCombatDef{name="天葉の光",targetRule="lowest-hp",damageType="magic",damagePercent=100,gaugeGain=1,drainAmount=0,waitPercent=100}
            }
        };
        private static ColossusCombatDef SilverSeaWhale()=>new ColossusCombatDef {
            id="colossus.silver-sea-whale",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=2500,hpPerLevel=180,gaugeMax=5,baseDamage=15,damagePerLevel=2,majorBonus=34,ultimateBonus=64,
            normalAction="銀潮の波",enragedAction="荒潮浮上",majorAction="大技：銀海奔流",ultimateAction="極大技：大海の再誕",
            enemySpeed=76,enrageHpPercent=40,enrageDamagePercent=140,attackBreakDamagePercent=65,majorDamageType="magic",majorWaitPercent=190,
            parts=new[]{
                new ColossusPartCombatDef{id="whale.core",role="gauge",breakEffect="gauge-down",baseHp=340,hpPerLevel=14},
                new ColossusPartCombatDef{id="whale.fin",role="attack",breakEffect="",baseHp=420,hpPerLevel=17},
                new ColossusPartCombatDef{id="whale.armor",role="armor",breakEffect="",baseHp=500,hpPerLevel=20},
                new ColossusPartCombatDef{id="whale.tail",role="drain",breakEffect="",baseHp=370,hpPerLevel=15}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="銀潮の波",targetRule="all",damageType="magic",damagePercent=60,gaugeGain=1,drainAmount=0,waitPercent=135},
                new ColossusActionCombatDef{name="潜航突進",targetRule="single",requiredPartId="whale.fin",damageType="physical",damagePercent=130,gaugeGain=1,drainAmount=0,waitPercent=110},
                new ColossusActionCombatDef{name="潮流吸収",targetRule="highest-resource",requiredPartId="whale.tail",damageType="magic",damagePercent=75,gaugeGain=2,drainAmount=2,waitPercent=150}
            }
        };
        private static ColossusCombatDef CrystalRosePrincess()=>new ColossusCombatDef {
            id="colossus.crystal-rose-princess",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=2200,hpPerLevel=170,gaugeMax=5,baseDamage=13,damagePerLevel=2,majorBonus=32,ultimateBonus=62,
            normalAction="荊の指名",enragedAction="紅晶開花",majorAction="大技：水晶花嵐",ultimateAction="極大技：永遠の薔薇園",
            enemySpeed=92,enrageHpPercent=50,enrageDamagePercent=135,attackBreakDamagePercent=60,majorDamageType="magic",majorWaitPercent=165,
            parts=new[]{
                new ColossusPartCombatDef{id="rose.crown",role="gauge",breakEffect="gauge-down",baseHp=300,hpPerLevel=13},
                new ColossusPartCombatDef{id="rose.whip",role="attack",breakEffect="",baseHp=360,hpPerLevel=15},
                new ColossusPartCombatDef{id="rose.shield",role="armor",breakEffect="",baseHp=440,hpPerLevel=18},
                new ColossusPartCombatDef{id="rose.root",role="drain",breakEffect="",baseHp=330,hpPerLevel=14}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="荊の指名",targetRule="lowest-hp",requiredPartId="rose.whip",damageType="physical",damagePercent=105,gaugeGain=1,drainAmount=0,waitPercent=100},
                new ColossusActionCombatDef{name="花弁の輪舞",targetRule="all",damageType="magic",damagePercent=65,gaugeGain=1,drainAmount=0,waitPercent=115},
                new ColossusActionCombatDef{name="根脈吸収",targetRule="highest-resource",requiredPartId="rose.root",damageType="magic",damagePercent=80,gaugeGain=2,drainAmount=3,waitPercent=125}
            }
        };
        private static ColossusCombatDef SkyTowerMachine()=>new ColossusCombatDef {
            id="colossus.sky-tower-machine",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=2100,hpPerLevel=165,gaugeMax=6,baseDamage=14,damagePerLevel=2,majorBonus=30,ultimateBonus=60,
            normalAction="気圧弾",enragedAction="暴風圏",majorAction="大技：天圧解放",ultimateAction="極大技：天空崩流",
            enemySpeed=88,enrageHpPercent=45,enrageDamagePercent=135,attackBreakDamagePercent=65,majorDamageType="magic",majorWaitPercent=180,
            parts=new[]{
                new ColossusPartCombatDef{id="sky.ring",role="gauge",breakEffect="gauge-down",baseHp=310,hpPerLevel=13},
                new ColossusPartCombatDef{id="sky.cannon",role="attack",breakEffect="",baseHp=390,hpPerLevel=16},
                new ColossusPartCombatDef{id="sky.barrier",role="armor",breakEffect="",baseHp=460,hpPerLevel=18},
                new ColossusPartCombatDef{id="sky.turbine",role="drain",breakEffect="",baseHp=300,hpPerLevel=12}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="気圧弾",targetRule="single",requiredPartId="sky.cannon",damageType="physical",damagePercent=110,gaugeGain=1,drainAmount=0,waitPercent=100},
                new ColossusActionCombatDef{name="圧力蓄積",targetRule="highest-resource",requiredPartId="sky.turbine",damageType="magic",damagePercent=55,gaugeGain=3,drainAmount=2,waitPercent=125},
                new ColossusActionCombatDef{name="暴風放出",targetRule="all",requiredPartId="sky.cannon",damageType="magic",damagePercent=80,gaugeGain=2,drainAmount=0,waitPercent=140},
                new ColossusActionCombatDef{name="塔影の衝撃",targetRule="single",damagePercent=85,gaugeGain=1,drainAmount=0,waitPercent=105}
            }
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
