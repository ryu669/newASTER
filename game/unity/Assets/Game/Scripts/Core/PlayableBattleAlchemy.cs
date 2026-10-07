using System;
using System.Linq;
namespace NewAster.Core
{
    // Authored recipes are independent of formation size. Inputs are copied and validated before spending.
    public sealed class AlchemyRecipe
    {
        public string Id {get;}
        public string Name {get;}
        private readonly int[] units;
        public int[] Units=>units.ToArray();
        public bool Weakness {get;}
        public AlchemyRecipe(string id,string name,int[] inputs,bool weakness=false){if(inputs==null || inputs.Length!=5 || inputs.Any(n=>n<0 || n>15) || inputs.Sum()<1 || inputs.Sum()>15)throw new ArgumentException("Invalid alchemy recipe.");Id=id;Name=name;units=inputs.ToArray();Weakness=weakness;}
    }
    public static class AlchemyRules
    {
        public static string[] Attributes(string hero)=>hero=="heroine.oriflamme"?new[]{"火","雷","闇","光","斬撃"}:Array.Empty<string>();
        public static AlchemyRecipe[] Recipes(string hero)=>Attributes(hero).Length==0?Array.Empty<AlchemyRecipe>():new[]{new AlchemyRecipe("flame","炎の錬成",new[]{3,0,0,0,0}),new AlchemyRecipe("catalyst","混合触媒",new[]{2,2,1,0,0}),new AlchemyRecipe("weakness","炎の弱点",new[]{2,0,0,1,0},true)};
    }
    public sealed partial class PlayableBattle
    {
        private string[] alchemyHitTargets=Array.Empty<string>();
        private string[] LastAttackTargetsForAlchemy(int actor,int slot)=>alchemyHitTargets;
        public string[] AlchemyAttributes(int actor)=>actor>=0 && actor<5?AlchemyRules.Attributes(State.Heroes[actor].Id):Array.Empty<string>();
        public string AlchemyPreview(int actor,int[] units){var attrs=AlchemyAttributes(actor);if(units==null || units.Length!=5 || attrs.Length!=5)return "錬成不可";return string.Join(" / ",attrs.Where((a,i)=>units[i]>0).Select(a=>a+"×"+units[Array.IndexOf(attrs,a)]))+" ／ 投入 "+units.Sum();}
        public bool CanTransmute(int actor,int[] units,string target,bool weakness=false)
        {
            if(!JobReady(actor) || !Job(actor,"alchemist") || units==null || units.Length!=5 || units.Any(n=>n<0 || n>15) || units.Sum()<1 || units.Sum()>State.Heroes[actor].JobResource || AlchemyAttributes(actor).Length!=5)return false;
            if(weakness && (units[0]<2 || units[3]<1))return false;
            return target=="body" || State.Parts.Any(p=>p.Id==target && !p.IsBroken);
        }
        public bool Transmute(int actor,int[] inputs,string target,bool weakness=false)
        {
            var units=inputs?.ToArray();if(!CanTransmute(actor,units,target,weakness))return false;
            var attributes=AlchemyAttributes(actor).Where((a,i)=>units[i]>0).ToArray();int cost=units.Sum();
            if(weakness){State.Heroes[actor].SpendResource(cost);State.EnemyStatus(target).SetFireVulnerability(Math.Min(50,cost*10),3);RecordPresentation(BattlePresentationKind.Support,actor,target,"錬成：火弱点＋"+Math.Min(50,cost*10)+"%・3ターン ／ READYを維持",standalone:true);}
            else{
                decimal power=.35m*cost+(units.Count(n=>n>0)>=3?.25m:0m);
                var skill=new BattleSkill("alchemy.oriflamme",power,cost,criticalChanceBp:State.Heroes[actor].CriticalChanceBp,damageCap:10000,damageType:"magic",attributes:attributes,statusEffects:units[0]>0?new[]{new EnemyStatusDef{kind="burn",amount=units[0]*20}}:null);
                var hit=BattleActionResolver.Resolve(State,State.Heroes[actor].Id,skill,target,max=>random.Next(max));if(!hit.Accepted)return false;
                RecordPresentation(BattlePresentationKind.Attack,actor,target,"錬成："+AlchemyPreview(actor,units)+" ／ READYを維持",damage:hit.Damage,broken:hit.PartBroken,targetIds:hit.TargetIds,standalone:true);
            }
            return true;
        }
    }
}
