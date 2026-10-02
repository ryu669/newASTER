using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using NewAster.Data;
public static class AutomaticChainTests
{
    private static int checks;
    static void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    public static void Main()
    {
        var manifest=new BattleIllustrationManifest {schemaVersion=1,placeholder=true,
            heroes=Enumerable.Range(0,5).Select(i=>new HeroIllustrationBinding {heroineId="hero-"+i,placeholder=true}).Reverse().ToArray(),
            parts=Enumerable.Range(0,4).Select(i=>new PartIllustrationBinding {partId="part-"+i,x=.1f,y=.1f,width=.2f,height=.2f}).ToArray()};
        manifest.Validate();Check(manifest.HeroIndex("hero-0")==4 && manifest.HeroIndex("missing")==-1,"Image bindings use heroine IDs, not file order");
        manifest.heroes[0].heroineId="hero-0";
        ExpectManifestFailure(manifest,"Duplicate heroine ID rejected");manifest.heroes[0].heroineId="hero-4";
        manifest.parts[0].x=float.NaN;ExpectManifestFailure(manifest,"NaN rectangle rejected");manifest.parts[0].x=.9f;
        ExpectManifestFailure(manifest,"Out-of-range rectangle rejected");manifest.parts[0].x=.1f;
        manifest.placeholder=false;ExpectManifestFailure(manifest,"Final manifest cannot hide placeholders");manifest.placeholder=true;
        manifest.heroes[0].placeholder=false;ExpectManifestFailure(manifest,"Final heroine requires a resource path");manifest.heroes[0].placeholder=true;
        manifest.parts[0]=null;ExpectManifestFailure(manifest,"Missing part binding rejected");
        var actions=new List<int>();
        var result=AutomaticChain.Resolve(0,5,0,new bool[5],i=>true,()=>false,max=>0,(i,bonus,step)=>actions.Add(i));
        Check(actions.SequenceEqual(new[]{1,2,3,4,0,1,2,3,4}),"Formation order and exactly one bonus lap");
        Check(result.FullChain && result.Participants==5 && result.BonusActions==5 && result.Checks.Count==5,"Five checks and ten total actions including command");
        Check(result.Checks.Last().ReturnCheck,"Return check is explicit");
        actions.Clear(); result=AutomaticChain.Resolve(3,5,1000,new[]{true,false,false,true,false},i=>true,()=>false,max=>0,(i,b,s)=>actions.Add(i));
        Check(actions.SequenceEqual(new[]{4,0,1,2,3,4,0,1,2}),"Wrapped formation order");
        Check(result.Checks.Select(c=>c.ProbabilityBp).SequenceEqual(new[]{6500,6500,7000,7000,7000}),"Cumulative bonus activates only after participant execution");
        result=AutomaticChain.Resolve(0,5,0,new bool[5],i=>true,()=>false,max=>5000,(i,b,s)=>throw new Exception("failed roll acted"));
        Check(result.Participants==1 && !result.FullChain && !result.Checks[0].Success,"50 percent boundary failure");
        int draws=0;actions.Clear();result=AutomaticChain.Resolve(0,5,0,new bool[5],i=>i==0||i==2,()=>false,max=>{draws++;return 0;},(i,b,s)=>actions.Add(i));
        Check(draws==2 && actions.SequenceEqual(new[]{2,0,2}),"Ineligible actors skipped without draws in normal and bonus laps");
        draws=0;result=AutomaticChain.Resolve(0,5,0,new bool[5],i=>i==0,()=>false,max=>{draws++;return 0;},(i,b,s)=>{});
        Check(draws==0 && !result.FullChain,"No self-chain when alone");
        bool ended=false;actions.Clear();result=AutomaticChain.Resolve(0,5,0,new bool[5],i=>true,()=>ended,max=>0,(i,b,s)=>{actions.Add(i);ended=true;});
        Check(actions.SequenceEqual(new[]{1}) && result.Checks.Count==1,"Victory stops actions and further RNG");
        actions.Clear();result=AutomaticChain.Resolve(0,5,0,new bool[5],i=>i!=0 || actions.Count<4,()=>false,max=>0,(i,b,s)=>actions.Add(i));
        Check(!result.FullChain && result.Checks.Count==4,"Ineligible origin prevents return roll");
        bool rejected=false;try{AutomaticChain.Resolve(0,5,0,new[]{true,true,true,false,false},i=>true,()=>false,max=>0,(i,b,s)=>{});}catch(ArgumentException){rejected=true;}
        Check(rejected,"Invalid bonus definitions rejected");
        // Low-damage explicit test heroine definitions avoid an early kill.
        var defs=Enumerable.Range(0,5).Select(i=>new HeroineChainAction("hero-"+i,"test-chain-"+i,.01m)).ToArray();
        bool sawFull=false,sawFailure=false,sawCast=false;
        for(int seed=1;seed<=180;seed++) {
            var a=new PlayableBattle(50,new PlayableProgress(),seed,heroineChainActions:defs);
            var b=new PlayableBattle(50,new PlayableProgress(),seed,heroineChainActions:defs);
            a.DrainPresentationEvents();b.DrainPresentationEvents();
            int actor=a.AvailableHero; long tick=a.Clock;
            Check(a.Act(actor,0,"body")&&b.Act(actor,0,"body"),"Valid command");
            var events=a.DrainPresentationEvents();b.DrainPresentationEvents();
            var chainEvents=events.Where(e=>e.Message.StartsWith("自動チェイン")||e.Message.StartsWith("フルチェイン")).ToArray();
            var origin=events.First(e=>e.Kind==BattlePresentationKind.Attack);
            Check(chainEvents.All(e=>e.Clock==tick && e.Resources.SequenceEqual(origin.Resources)),"Chain does not advance time or mutate resources");
            Check(a.State.BossHitPoints==b.State.BossHitPoints && a.Log==b.Log,"Same seed reproduces automatic resolution");
            Check(a.LastChainChecks.All(c=>c.ProbabilityBp>=5000 && c.ProbabilityBp<=7000),"Rates bounded");
            sawFull|=a.LastFullChain;sawFailure|=a.LastChainChecks.Any(c=>!c.Success);
        }
        Check(sawFull && sawFailure,"Seed sample exercises full chain and failure");
        var cast=new PlayableBattle(1,new PlayableProgress(),7,heroineChainActions:defs);
        while(cast.AvailableHero!=4&&!cast.Ended) cast.Pass();cast.DrainPresentationEvents();
        Check(cast.Act(4,1,"body"),"Start test cast");
        var castEvents=new List<BattlePresentationEvent>(cast.DrainPresentationEvents());
        Check(castEvents.First().Kind==BattlePresentationKind.CastStart,"Cast begins before any chain");
        for(int i=0;i<12 && cast.IsCasting(4)&&!cast.Ended;i++) {cast.Pass();castEvents.AddRange(cast.DrainPresentationEvents());}
        int release=castEvents.FindIndex(e=>e.Kind==BattlePresentationKind.CastRelease);
        Check(release>=0 && !cast.IsCasting(4),"Cast resolves normally after intervening commands");
        sawCast=castEvents.Take(release).All(e=>!e.Message.StartsWith("自動チェイン")&&!e.Message.StartsWith("フルチェイン"));
        Check(sawCast,"No chain at cast start or before release");
        bool castingParticipant=false;
        for(int seed=1;seed<=40;seed++) {
            var pending=new PlayableBattle(10,new PlayableProgress(),seed,heroineChainActions:defs);
            while(pending.AvailableHero!=4&&!pending.Ended) pending.Pass();
            pending.Act(4,1,"body");pending.DrainPresentationEvents();
            if(pending.AvailableHero<0) continue;
            pending.Act(pending.AvailableHero,0,"body");
            var ev=pending.DrainPresentationEvents();
            var participation=ev.FirstOrDefault(e=>e.Actor==4 && e.Message.StartsWith("自動チェイン"));
            if(participation!=null) { castingParticipant=true;Check(participation.Casting[4],"Casting participant retains its reservation during chain"); }
        }
        Check(castingParticipant,"Seed sample exercises casting heroine chain participation");
        actions.Clear(); int n=0;
        result=AutomaticChain.Resolve(0,5,0,new bool[5],i=>true,()=>false,max=>++n==5?5000:4999,(i,b,s)=>actions.Add(i));
        Check(actions.SequenceEqual(new[]{1,2,3,4})&&!result.FullChain,"4999 succeeds but failed return cannot trigger bonus lap");
        Console.WriteLine("AUTOMATIC_CHAIN_PASS "+checks+" assertions");
    }
    private static void ExpectManifestFailure(BattleIllustrationManifest manifest,string message)
    {
        bool rejected=false;try {manifest.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,message);
    }
}
