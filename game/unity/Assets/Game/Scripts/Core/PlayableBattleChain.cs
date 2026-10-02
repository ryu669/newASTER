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
            int participants=1;
            var outcome=AutomaticChain.Resolve(origin,5,skillBonus,marked,i=>State.Heroes[i].IsAlive,()=>Ended,
                max=>random.Next(max),(actor,bonus,step)=> {
                    // Placeholder definitions currently target the boss body. No resource APIs called.
                    var def=chainActions[actor];
                    int damage=State.ApplyBossDamage(Math.Max(1,(int)Math.Floor(State.Heroes[actor].Attack*def.Power)));
                    if(!bonus) participants++;
                    LastActionChain=participants; LastFullChain=bonus; LastChainActionCount=step;
                    string message=(bonus?"フルチェイン追加一周":"自動チェイン")+" / 味方"+(actor+1)+" / "+damage+"ダメージ";
                    Log+="\n"+message;
                    RecordPresentation(BattlePresentationKind.Attack,actor,"body",message,damage:damage);
                });
            Chain=outcome.Participants; LastActionChain=outcome.Participants;
            LastFullChain=outcome.FullChain; LastChainActionCount=outcome.Participants+outcome.BonusActions;
            LastChainChecks=Array.AsReadOnly(outcome.Checks.ToArray());
            Log+="\nチェイン終了 / "+outcome.Participants+"人"+(outcome.FullChain?" / 追加"+outcome.BonusActions+"行動":"");
        }
    }
}
