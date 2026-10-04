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
        public static bool CanSummon(string id)=>id==GreenReturnDragonVerticalSlice.ColossusId;
        public static ColossusCombatDef GetPlan8Trial(string id)
        {
            var definition=Get(id);definition.contentVersion=ColossusCombatDef.Plan8Version;
            definition.hpPerLevel=600;definition.damagePerLevel=8;definition.Validate();return definition;
        }
        public static ColossusCombatDef Get(string id)
        {
            if(!CanSummon(id))throw new System.ArgumentException("This colossus combat definition has not been authored.");
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
    }
}
