using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed partial class PlayableBattle
    {
        private int[] skillChainBonuses=new int[3];
        private bool[] cumulativeChainActors=new bool[5];
        private HeroineChainAction[] chainActions;
        public bool LastFullChain { get; private set; }
        public int LastChainActionCount { get; private set; }
        public IReadOnlyList<ChainConnection> LastChainChecks { get; private set; }=Array.Empty<ChainConnection>();
        public int SkillChainBonusBp(int slot) => slot>=0 && slot<3?skillChainBonuses[slot]:0;
        public bool HasCumulativeChainBonus(int actor) => actor>=0 && actor<5 && cumulativeChainActors[actor];
        public string ChainActionDescription(int actor)
        {
            if(actor<0 || actor>=5) throw new ArgumentOutOfRangeException(nameof(actor));
            var action=chainActions[actor];
            switch(action.Target) {
                case ChainTarget.BossBody: return "本体へ攻撃";
                case ChainTarget.LowestHpPart: return "残りHP最少の部位へ攻撃";
                case ChainTarget.Self: return "自分を回復";
                case ChainTarget.LowestHpAlly: return "低HP割合の味方を回復";
                case ChainTarget.AllLivingAllies: return "生存する味方全員を回復";
                default: throw new InvalidOperationException("Unsupported fixed target.");
            }
        }
        private void GenerateChainModifiers()
        {
            for(int slot=0;slot<3;slot++) skillChainBonuses[slot]=random.Next(10000)<2500?1000:0;
            cumulativeChainActors=new bool[5];
            if(random.Next(10000)<5000) {
                int first=random.Next(5),other=random.Next(4); if(other>=first) other++;
                cumulativeChainActors[first]=cumulativeChainActors[other]=true;
            }
        }
        private void ResolveAutomaticChain(int origin,int skillBonus,bool[] marked)
        {
            LastFullChain=false; LastChainActionCount=1;
            if(!State.Heroes[origin].IsAlive) {LastChainChecks=Array.Empty<ChainConnection>();return;}
            int participants=1;
            var outcome=AutomaticChain.Resolve(origin,5,skillBonus,marked,i=>State.Heroes[i].IsAlive,()=>Ended,
                max=>random.Next(max),(actor,bonus,step)=> {
                    var def=chainActions[actor];
                    var effect=ChainActionResolver.Resolve(State,actor,def);
                    if(!bonus) participants++;
                    LastActionChain=participants; LastFullChain=bonus; LastChainActionCount=step;
                    string message=(bonus?"フルチェイン追加一周":"自動チェイン")+" / 味方"+(actor+1)+" / "+(effect.TargetIds.Count==0?"対象なし":def.Effect==ChainEffect.Heal?"回復 ＋"+effect.Amount:effect.Amount+"ダメージ");
                    Log+="\n"+message;
                    var kind=effect.TargetIds.Count==0?BattlePresentationKind.Support:def.Effect==ChainEffect.Heal?BattlePresentationKind.Healing:BattlePresentationKind.Attack;
                    RecordPresentation(kind,actor,effect.Target,message,damage:def.Effect==ChainEffect.Damage?effect.Amount:0,broken:effect.PartBroken,healingTargets:effect.HealingTargets,chainActionId:def.Id,presentationId:def.PresentationId,targetIds:effect.TargetIds);
                });
            Chain=outcome.Participants; LastActionChain=outcome.Participants;
            LastFullChain=outcome.FullChain; LastChainActionCount=outcome.Participants+outcome.BonusActions;
            LastChainChecks=Array.AsReadOnly(outcome.Checks.ToArray());
            Log+="\nチェイン終了 / "+outcome.Participants+"人"+(outcome.FullChain?" / 追加"+outcome.BonusActions+"行動":"");
        }
    }
}
