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
            {"colossus.heaven-tree-orochi",HeavenTreeOrochi},
            {"colossus.reenactment-yimir",ReenactmentYimir},
            {"colossus.emerald-star-astal",EmeraldStarAstal},
            {"colossus.amber-king-serpent",AmberKingSerpent},
            {"colossus.white-divine-dragon-mother",WhiteDivineDragonMother},
            {"colossus.black-smoke-citadel",BlackSmokeCitadel},
            {"colossus.dead-king-megadeath",DeadKingMegadeath},
            {"colossus.final-flame-ice-phoenix",FinalFlameIcePhoenix},
            {"colossus.newborn-asteria",NewbornAsteria}
        };
        private static ColossusCombatDef NewbornAsteria()=>new ColossusCombatDef {
            id="colossus.newborn-asteria",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=4200,hpPerLevel=250,gaugeMax=6,baseDamage=19,damagePerLevel=2,majorBonus=48,ultimateBonus=80,
            normalAction="星爪の選定",enragedAction="新世界の鼓動",majorAction="大技：七界星環",ultimateAction="極大技：万象新生",
            enemySpeed=100,enrageHpPercent=50,enrageDamagePercent=145,attackBreakDamagePercent=55,majorDamageType="magic",majorWaitPercent=200,
            parts=new[]{
                new ColossusPartCombatDef{id="asteria.ring",role="gauge",breakEffect="gauge-down",baseHp=500,hpPerLevel=20},
                new ColossusPartCombatDef{id="asteria.claw",role="attack",breakEffect="",baseHp=620,hpPerLevel=24},
                new ColossusPartCombatDef{id="asteria.chest",role="armor",breakEffect="",baseHp=720,hpPerLevel=27},
                new ColossusPartCombatDef{id="asteria.tail",role="drain",breakEffect="",baseHp=530,hpPerLevel=21}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="星爪の選定",targetRule="lowest-hp",requiredPartId="asteria.claw",damageType="physical",damagePercent=140,gaugeGain=1,waitPercent=100},
                new ColossusActionCombatDef{name="七界の共鳴",targetRule="all",damageType="magic",damagePercent=85,gaugeGain=2,waitPercent=135},
                new ColossusActionCombatDef{name="世界尾の収奪",targetRule="highest-resource",requiredPartId="asteria.tail",damageType="magic",damagePercent=90,gaugeGain=0,drainAmount=4,waitPercent=100},
                new ColossusActionCombatDef{name="星獣の踏撃",targetRule="single",damageType="physical",damagePercent=110,gaugeGain=1,waitPercent=115}
            }
        };
        private static ColossusCombatDef FinalFlameIcePhoenix()=>new ColossusCombatDef {
            id="colossus.final-flame-ice-phoenix",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=3700,hpPerLevel=235,gaugeMax=6,baseDamage=18,damagePerLevel=2,majorBonus=45,ultimateBonus=75,
            normalAction="焔翼の突撃",enragedAction="双極の羽化",majorAction="大技：焔氷交響",ultimateAction="極大技：終焔氷界",
            enemySpeed=105,enrageHpPercent=50,enrageDamagePercent=140,attackBreakDamagePercent=60,majorDamageType="magic",majorWaitPercent=180,
            parts=new[]{
                new ColossusPartCombatDef{id="phoenix.crown",role="gauge",breakEffect="gauge-down",baseHp=450,hpPerLevel=19},
                new ColossusPartCombatDef{id="phoenix.flame",role="attack",breakEffect="",baseHp=570,hpPerLevel=23},
                new ColossusPartCombatDef{id="phoenix.ice",role="armor",breakEffect="",baseHp=660,hpPerLevel=25},
                new ColossusPartCombatDef{id="phoenix.tail",role="drain",breakEffect="",baseHp=480,hpPerLevel=20}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="焔翼の突撃",targetRule="single",requiredPartId="phoenix.flame",damageType="physical",damagePercent=140,gaugeGain=1,waitPercent=90},
                new ColossusActionCombatDef{name="氷晶の吹雪",targetRule="all",damageType="magic",damagePercent=80,gaugeGain=2,waitPercent=130},
                new ColossusActionCombatDef{name="焔尾の収束",targetRule="highest-resource",requiredPartId="phoenix.tail",damageType="magic",damagePercent=80,gaugeGain=1,drainAmount=3,waitPercent=105},
                new ColossusActionCombatDef{name="氷晶の吹雪",targetRule="all",damageType="magic",damagePercent=80,gaugeGain=2,waitPercent=130}
            }
        };
        private static ColossusCombatDef DeadKingMegadeath()=>new ColossusCombatDef {
            id="colossus.dead-king-megadeath",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=3400,hpPerLevel=225,gaugeMax=6,baseDamage=17,damagePerLevel=2,majorBonus=43,ultimateBonus=73,
            normalAction="夜剣の指名",enragedAction="亡王の覚醒",majorAction="大技：魂葬の夜",ultimateAction="極大技：死王の帰還",
            enemySpeed=94,enrageHpPercent=45,enrageDamagePercent=140,attackBreakDamagePercent=55,majorDamageType="magic",majorWaitPercent=190,
            parts=new[]{
                new ColossusPartCombatDef{id="megadeath.crown",role="gauge",breakEffect="gauge-down",baseHp=420,hpPerLevel=18},
                new ColossusPartCombatDef{id="megadeath.sword",role="attack",breakEffect="",baseHp=540,hpPerLevel=22},
                new ColossusPartCombatDef{id="megadeath.armor",role="armor",breakEffect="",baseHp=620,hpPerLevel=24},
                new ColossusPartCombatDef{id="megadeath.lantern",role="drain",breakEffect="",baseHp=450,hpPerLevel=19}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="夜剣の指名",targetRule="lowest-hp",requiredPartId="megadeath.sword",damageType="physical",damagePercent=135,gaugeGain=2,waitPercent=110},
                new ColossusActionCombatDef{name="魂灯の収奪",targetRule="highest-resource",requiredPartId="megadeath.lantern",damageType="magic",damagePercent=85,gaugeGain=0,drainAmount=4,waitPercent=95},
                new ColossusActionCombatDef{name="夜霧の王令",targetRule="all",damageType="magic",damagePercent=75,gaugeGain=2,waitPercent=145}
            }
        };
        private static ColossusCombatDef BlackSmokeCitadel()=>new ColossusCombatDef {
            id="colossus.black-smoke-citadel",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=3600,hpPerLevel=230,gaugeMax=5,baseDamage=18,damagePerLevel=2,majorBonus=42,ultimateBonus=72,
            normalAction="重鉄砲撃",enragedAction="城炉過熱",majorAction="大技：黒煙総砲",ultimateAction="極大技：灰の王都",
            enemySpeed=66,enrageHpPercent=35,enrageDamagePercent=150,attackBreakDamagePercent=55,majorDamageType="physical",majorWaitPercent=230,
            parts=new[]{
                new ColossusPartCombatDef{id="citadel.core",role="gauge",breakEffect="gauge-down",baseHp=410,hpPerLevel=18},
                new ColossusPartCombatDef{id="citadel.cannon",role="attack",breakEffect="",baseHp=530,hpPerLevel=21},
                new ColossusPartCombatDef{id="citadel.gate",role="armor",breakEffect="",baseHp=650,hpPerLevel=25},
                new ColossusPartCombatDef{id="citadel.exhaust",role="drain",breakEffect="",baseHp=440,hpPerLevel=19}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="重鉄砲撃",targetRule="single",requiredPartId="citadel.cannon",damageType="physical",damagePercent=145,gaugeGain=1,waitPercent=150},
                new ColossusActionCombatDef{name="城脚地震",targetRule="all",damageType="physical",damagePercent=80,gaugeGain=2,waitPercent=180},
                new ColossusActionCombatDef{name="排熱吸収",targetRule="highest-resource",requiredPartId="citadel.exhaust",damageType="magic",damagePercent=75,gaugeGain=0,drainAmount=3,waitPercent=130}
            }
        };
        private static ColossusCombatDef WhiteDivineDragonMother()=>new ColossusCombatDef {
            id="colossus.white-divine-dragon-mother",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=3200,hpPerLevel=215,gaugeMax=6,baseDamage=15,damagePerLevel=2,majorBonus=40,ultimateBonus=70,
            normalAction="白翼の羽撃",enragedAction="母竜の祈り",majorAction="大技：生命光輪",ultimateAction="極大技：白き再生の空",
            enemySpeed=86,enrageHpPercent=50,enrageDamagePercent=135,attackBreakDamagePercent=60,majorDamageType="magic",majorWaitPercent=185,
            parts=new[]{
                new ColossusPartCombatDef{id="mother.crown",role="gauge",breakEffect="gauge-down",baseHp=390,hpPerLevel=17},
                new ColossusPartCombatDef{id="mother.wing",role="attack",breakEffect="",baseHp=490,hpPerLevel=20},
                new ColossusPartCombatDef{id="mother.chest",role="armor",breakEffect="",baseHp=580,hpPerLevel=23},
                new ColossusPartCombatDef{id="mother.tail",role="drain",breakEffect="",baseHp=420,hpPerLevel=18}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="白翼の羽撃",targetRule="single",requiredPartId="mother.wing",damageType="physical",damagePercent=115,gaugeGain=1,waitPercent=105},
                new ColossusActionCombatDef{name="白光の祈り",targetRule="all",damageType="magic",damagePercent=65,gaugeGain=2,waitPercent=125},
                new ColossusActionCombatDef{name="生命走査",targetRule="highest-resource",requiredPartId="mother.tail",damageType="magic",damagePercent=80,gaugeGain=1,drainAmount=2,waitPercent=100}
            }
        };
        private static ColossusCombatDef AmberKingSerpent()=>new ColossusCombatDef {
            id="colossus.amber-king-serpent",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=3000,hpPerLevel=205,gaugeMax=5,baseDamage=16,damagePerLevel=2,majorBonus=39,ultimateBonus=69,
            normalAction="砂牙の指名",enragedAction="琥珀王の憤怒",majorAction="大技：王砂嵐",ultimateAction="極大技：時砂の王域",
            enemySpeed=80,enrageHpPercent=40,enrageDamagePercent=145,attackBreakDamagePercent=60,majorDamageType="magic",majorWaitPercent=210,
            parts=new[]{
                new ColossusPartCombatDef{id="serpent.crown",role="gauge",breakEffect="gauge-down",baseHp=370,hpPerLevel=16},
                new ColossusPartCombatDef{id="serpent.fang",role="attack",breakEffect="",baseHp=470,hpPerLevel=19},
                new ColossusPartCombatDef{id="serpent.armor",role="armor",breakEffect="",baseHp=550,hpPerLevel=22},
                new ColossusPartCombatDef{id="serpent.tail",role="drain",breakEffect="",baseHp=400,hpPerLevel=17}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="砂牙の指名",targetRule="lowest-hp",requiredPartId="serpent.fang",damageType="physical",damagePercent=120,gaugeGain=1,waitPercent=110},
                new ColossusActionCombatDef{name="砂丘の波",targetRule="all",damageType="magic",damagePercent=75,gaugeGain=2,waitPercent=160},
                new ColossusActionCombatDef{name="時砂吸収",targetRule="highest-resource",requiredPartId="serpent.tail",damageType="magic",damagePercent=80,gaugeGain=1,drainAmount=3,waitPercent=125}
            }
        };
        private static ColossusCombatDef EmeraldStarAstal()=>new ColossusCombatDef {
            id="colossus.emerald-star-astal",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=2700,hpPerLevel=190,gaugeMax=6,baseDamage=15,damagePerLevel=2,majorBonus=37,ultimateBonus=67,
            normalAction="彗星爪撃",enragedAction="翠星共鳴",majorAction="大技：星輪降雨",ultimateAction="極大技：翠星創世",
            enemySpeed=110,enrageHpPercent=50,enrageDamagePercent=135,attackBreakDamagePercent=60,majorDamageType="magic",majorWaitPercent=155,
            parts=new[]{
                new ColossusPartCombatDef{id="astal.ring",role="gauge",breakEffect="gauge-down",baseHp=340,hpPerLevel=14},
                new ColossusPartCombatDef{id="astal.claw",role="attack",breakEffect="",baseHp=430,hpPerLevel=18},
                new ColossusPartCombatDef{id="astal.shield",role="armor",breakEffect="",baseHp=510,hpPerLevel=21},
                new ColossusPartCombatDef{id="astal.tail",role="drain",breakEffect="",baseHp=360,hpPerLevel=15}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="彗星爪撃",targetRule="lowest-hp",requiredPartId="astal.claw",damageType="physical",damagePercent=115,gaugeGain=1,waitPercent=80},
                new ColossusActionCombatDef{name="翠星の瞬き",targetRule="all",damageType="magic",damagePercent=60,gaugeGain=2,waitPercent=90},
                new ColossusActionCombatDef{name="衛星吸光",targetRule="highest-resource",requiredPartId="astal.tail",damageType="magic",damagePercent=75,gaugeGain=1,drainAmount=2,waitPercent=100}
            }
        };
        private static ColossusCombatDef ReenactmentYimir()=>new ColossusCombatDef {
            id="colossus.reenactment-yimir",contentVersion=ColossusCombatDef.Plan9Version,
            baseHp=2800,hpPerLevel=195,gaugeMax=5,baseDamage=16,damagePerLevel=2,majorBonus=38,ultimateBonus=68,
            normalAction="石腕の再演",enragedAction="季節重奏",majorAction="大技：四季再演",ultimateAction="極大技：時環の終幕",
            enemySpeed=74,enrageHpPercent=45,enrageDamagePercent=140,attackBreakDamagePercent=60,majorDamageType="physical",majorWaitPercent=200,
            parts=new[]{
                new ColossusPartCombatDef{id="yimir.crown",role="gauge",breakEffect="gauge-down",baseHp=350,hpPerLevel=15},
                new ColossusPartCombatDef{id="yimir.arm",role="attack",breakEffect="",baseHp=480,hpPerLevel=19},
                new ColossusPartCombatDef{id="yimir.chest",role="armor",breakEffect="",baseHp=520,hpPerLevel=21},
                new ColossusPartCombatDef{id="yimir.wheel",role="drain",breakEffect="",baseHp=380,hpPerLevel=16}
            },
            actionCycle=new[]{
                new ColossusActionCombatDef{name="石腕の再演",targetRule="single",requiredPartId="yimir.arm",damageType="physical",damagePercent=135,gaugeGain=1,drainAmount=0,waitPercent=140},
                new ColossusActionCombatDef{name="四季の波紋",targetRule="all",damageType="magic",damagePercent=65,gaugeGain=2,drainAmount=0,waitPercent=130},
                new ColossusActionCombatDef{name="記録輪走査",targetRule="highest-resource",requiredPartId="yimir.wheel",damageType="magic",damagePercent=70,gaugeGain=0,drainAmount=3,waitPercent=100}
            }
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
