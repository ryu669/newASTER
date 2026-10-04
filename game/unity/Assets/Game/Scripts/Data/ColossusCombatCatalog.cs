using NewAster.Core;
namespace NewAster.Data
{
    public static class ColossusCombatCatalog
    {
        public static string PartName(BattlePart part,int index)
        {
            switch(part.Id) {
                case "crystal-horn-crown":return "結晶角冠";
                case "left-wing-root":return "左翼の根";
                case "right-wing-root":return "右翼の装甲";
                case "vine-wrapped-tail":return "蔓の尾";
                case "tyrant.crown":return "赤晶角冠";
                case "tyrant.claw":return "掘削の左爪";
                case "tyrant.plate":return "黒曜胸甲";
                case "tyrant.tail":return "鉱脈の尾";
                default:return "補助部位 "+(index+1);
            }
        }
        public static string PartEffect(BattlePart part)
        {
            switch(part.Role) {
                case "gauge":return "大技ゲージ上昇を止める";
                case "attack":return "敵の攻撃を弱める";
                case "armor":return "本体の軽減を解除";
                case "drain":return "資源妨害を止める";
                default:return "追加効果なし";
            }
        }
        public static bool CanSummon(string id)=>id==GreenReturnDragonVerticalSlice.ColossusId || id=="colossus.red-crystal-tyrant";
        public static string IllustrationResource(string id)
        {
            if(!CanSummon(id))throw new System.ArgumentException("Unauthored enemy illustration");
            return id=="colossus.red-crystal-tyrant"?"Illustrations/battle-red-crystal-tyrant-candidate-v1":"Illustrations/battle-formal";
        }
        public static ColossusCombatDef GetPlan8Trial(string id)
        {
            var definition=Get(id);definition.contentVersion=ColossusCombatDef.Plan8Version;
            definition.hpPerLevel=600;definition.damagePerLevel=8;definition.Validate();return definition;
        }
        public static ColossusCombatDef Get(string id)
        {
            if(!CanSummon(id))throw new System.ArgumentException("This colossus combat definition has not been authored.");
            if(id=="colossus.red-crystal-tyrant")return RedCrystalTyrant();
            var result=new ColossusCombatDef {
                id=id,baseHp=1500,hpPerLevel=120,gaugeMax=4,baseDamage=12,damagePerLevel=2,majorBonus=20,ultimateBonus=45,
                normalAction="翼撃",enragedAction="怒りの翼撃",majorAction="大技：緑晶の嵐",ultimateAction="極大技：星還の奔流",
                parts=new[]{
                    new ColossusPartCombatDef {id="crystal-horn-crown",role="gauge",breakEffect="gauge-down",baseHp=300,hpPerLevel=12},
                    new ColossusPartCombatDef {id="left-wing-root",role="attack",breakEffect="",baseHp=300,hpPerLevel=12},
                    new ColossusPartCombatDef {id="right-wing-root",role="armor",breakEffect="",baseHp=300,hpPerLevel=12},
                    new ColossusPartCombatDef {id="vine-wrapped-tail",role="drain",breakEffect="",baseHp=300,hpPerLevel=12}
                }
            };
            result.Validate();return result;
        }
        private static ColossusCombatDef RedCrystalTyrant()
        {
            var result=new ColossusCombatDef {
                id="colossus.red-crystal-tyrant",contentVersion=ColossusCombatDef.Plan9Version,
                baseHp=1800,hpPerLevel=145,gaugeMax=5,baseDamage=14,damagePerLevel=2,majorBonus=25,ultimateBonus=55,
                normalAction="岩砕の顎",enragedAction="赤熱の咆哮",majorAction="大技：鉱晶崩落",ultimateAction="極大技：渓谷断裂",
                enemySpeed=82,enrageHpPercent=40,enrageDamagePercent=140,attackBreakDamagePercent=60,majorDamageType="physical",majorWaitPercent=175,
                parts=new[]{
                    new ColossusPartCombatDef{id="tyrant.crown",role="gauge",breakEffect="gauge-down",baseHp=260,hpPerLevel=11},
                    new ColossusPartCombatDef{id="tyrant.claw",role="attack",breakEffect="",baseHp=390,hpPerLevel=15},
                    new ColossusPartCombatDef{id="tyrant.plate",role="armor",breakEffect="",baseHp=440,hpPerLevel=17},
                    new ColossusPartCombatDef{id="tyrant.tail",role="drain",breakEffect="",baseHp=330,hpPerLevel=13}
                },
                actionCycle=new[]{
                    new ColossusActionCombatDef{name="岩砕の顎",targetRule="single",damagePercent=100,gaugeGain=1,drainAmount=0,waitPercent=90},
                    new ColossusActionCombatDef{name="掘削爪の掃射",targetRule="all",requiredPartId="tyrant.claw",damagePercent=65,gaugeGain=1,drainAmount=1,waitPercent=125},
                    new ColossusActionCombatDef{name="鉱脈吸収",targetRule="highest-resource",requiredPartId="tyrant.tail",damagePercent=85,gaugeGain=2,drainAmount=2,waitPercent=110}
                }
            };
            result.Validate();return result;
        }
    }
}
