using System;
using System.Collections.Generic;
namespace NewAster.Core
{
    // Pure chain sequencing. Normal commands, resources and the timeline are not inputs.
    public sealed class ChainConnection
    {
        public int Candidate { get; }
        public int ProbabilityBp { get; }
        public int Roll { get; }
        public bool ReturnCheck { get; }
        public bool Success => Roll < ProbabilityBp;
        public ChainConnection(int candidate,int probability,int roll,bool returning)
        { Candidate=candidate; ProbabilityBp=probability; Roll=roll; ReturnCheck=returning; }
    }
    public sealed class AutomaticChainResult
    {
        public int Participants { get; internal set; }=1;
        public int BonusActions { get; internal set; }
        public bool FullChain { get; internal set; }
        public List<ChainConnection> Checks { get; }=new List<ChainConnection>();
    }
    public static class AutomaticChain
    {
        public static AutomaticChainResult Resolve(int origin,int count,int skillBonusBp,bool[] bonusActors,
            Func<int,bool> canAct,Func<bool> ended,Func<int,int> draw,Action<int,bool,int> execute)
        {
            if(count<1 || origin<0 || origin>=count || bonusActors==null || bonusActors.Length!=count ||
                (skillBonusBp!=0 && skillBonusBp!=1000)) throw new ArgumentException("Invalid chain context.");
            int marked=0; foreach(bool b in bonusActors) if(b) marked++;
            if(marked>2) throw new ArgumentException("At most two cumulative actors.");
            var result=new AutomaticChainResult();
            var visited=new HashSet<int>{origin}; int cursor=origin;
            int cumulative=bonusActors[origin]?500:0;
            while(!ended()) {
                int candidate=-1; bool returning=false;
                for(int offset=1;offset<=count;offset++) {
                    int i=(cursor+offset)%count;
                    if(i==origin) { returning=true; if(visited.Count>1 && canAct(i)) candidate=i; break; }
                    if(!visited.Contains(i) && canAct(i)) { candidate=i; break; }
                }
                if(candidate<0) break;
                int roll=draw(10000); if(roll<0 || roll>=10000) throw new ArgumentOutOfRangeException("draw");
                var check=new ChainConnection(candidate,5000+skillBonusBp+cumulative,roll,returning);
                result.Checks.Add(check); if(!check.Success) break;
                if(returning) {
                    result.FullChain=true;
                    for(int offset=0;offset<count && !ended();offset++) {
                        int i=(origin+offset)%count; if(!canAct(i)) continue;
                        result.BonusActions++; execute(i,true,result.Participants+result.BonusActions);
                    }
                    break;
                }
                visited.Add(candidate); result.Participants++;
                execute(candidate,false,result.Participants);
                if(bonusActors[candidate]) cumulative+=500;
                cursor=candidate;
            }
            return result;
        }
    }
    // Explicit placeholder heroine definitions; never inferred from job or command skill.
    public sealed class HeroineChainAction
    {
        public string HeroId { get; }
        public string Id { get; }
        public decimal Power { get; }
        public HeroineChainAction(string heroId,string id,decimal power)
        { if(string.IsNullOrWhiteSpace(heroId)||string.IsNullOrWhiteSpace(id)||power<=0) throw new ArgumentException(); HeroId=heroId; Id=id; Power=power; }
    }
}
