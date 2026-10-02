using NewAster.Core;
namespace NewAster.Data
{
    public static class ColossusCombatCatalog
    {
        public static bool CanSummon(string id)=>id==GreenReturnDragonVerticalSlice.ColossusId;
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