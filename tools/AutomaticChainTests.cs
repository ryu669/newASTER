using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class AutomaticChainTests
{
    private static int checks;
    static void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions {IncludeFields=true};
        string json=File.ReadAllText(args[0]);
        Func<CombatDefinitionCatalog> fresh=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(json,options);
        var catalog=fresh();catalog.Validate();
        Check(catalog.Healing().Length==3 && catalog.Chain().Length==5,"Actual JSON creates healing and chain definitions");
        catalog.heroines=catalog.heroines.Reverse().ToArray();catalog.skills=catalog.skills.Reverse().ToArray();catalog.chainActions=catalog.chainActions.Reverse().ToArray();catalog.Validate();
        Check(catalog.Skill("hero-4",1).castPercent==150 && catalog.Chain()[0].HeroId=="hero-0","JSON order does not change formation ownership");
        for(int seed=1;seed<=40;seed++) {
            var oldBattle=new PlayableBattle(10,new PlayableProgress(),seed);
            var dataBattle=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:catalog);
            int actor=oldBattle.AvailableHero;
            Check(oldBattle.PreviewDamage(actor,0,"body")==dataBattle.PreviewDamage(actor,0,"body"),"Trial JSON preserves attack preview");
            oldBattle.Act(actor,0,"body");dataBattle.Act(actor,0,"body");
            Check(oldBattle.State.BossHitPoints==dataBattle.State.BossHitPoints && oldBattle.Clock==dataBattle.Clock && oldBattle.Log==dataBattle.Log,"Actual JSON preserves trial resolution and RNG");
        }
        catalog=fresh();catalog.schemaVersion=2;ExpectCombatFailure(catalog,"Unknown version rejected");
        catalog=fresh();catalog.heroines[0].skills[0]="skill.missing";ExpectCombatFailure(catalog,"Missing skill reference rejected");
        catalog=fresh();catalog.skills[0].ownerId="hero-1";ExpectCombatFailure(catalog,"Cross-hero skill rejected");
        catalog=fresh();catalog.chainActions[0].heroineId="hero-1";ExpectCombatFailure(catalog,"Cross-hero chain rejected");
        catalog=fresh();catalog.chainActions[0].resourcePolicy="spend";ExpectCombatFailure(catalog,"Resource-spending chain rejected");
        catalog=fresh();catalog.chainActions[0].effectRuleId="effect.heal";ExpectCombatFailure(catalog,"Not-yet-supported chain effects rejected");
        catalog=fresh();catalog.skills[0].powerScale=float.NaN;ExpectCombatFailure(catalog,"Nonfinite skill scale rejected");
        catalog=fresh();catalog.Skill("hero-3",2).targetCount=2;ExpectCombatFailure(catalog,"All-allies target count must be five");
        catalog=fresh();catalog.Skill("hero-3",1).castPercent=100;ExpectCombatFailure(catalog,"Unsupported deferred heal rejected");
        catalog=fresh();catalog.Skill("hero-2",2).effectRuleId="effect.unknown";ExpectCombatFailure(catalog,"Unknown support effect rejected");
        catalog=fresh();catalog.Skill("hero-0",0).resourceCost=4;catalog.Skill("hero-0",0).name="データ側の技名";
        var custom=new PlayableBattle(10,new PlayableProgress(),22,combatDefinitions:catalog);
        Check(custom.SkillResourceCost(0,0)==4 && custom.SkillName(0,0)=="データ側の技名","UI values come from definitions");
        catalog.Skill("hero-0",0).resourceCost=0;catalog.Skill("hero-0",0).powerScale=99;
        Check(custom.SkillResourceCost(0,0)==4,"Runtime snapshots definitions against external mutation");
        catalog=fresh();foreach(var s in catalog.skills.Where(s=>s.effectRuleId=="effect.damage")) s.chainEligible=false;
        custom=new PlayableBattle(10,new PlayableProgress(),22,combatDefinitions:catalog);custom.DrainPresentationEvents();custom.Act(custom.AvailableHero,0,"body");
        Check(!custom.DrainPresentationEvents().Any(e=>e.Message.StartsWith("自動チェイン")) && custom.LastChainChecks.Count==0,"Definition controls attack chain eligibility");
        bool mixedRejected=false;try {new PlayableBattle(1,new PlayableProgress(),combatDefinitions:catalog,skillTimings:PlayableBattle.DefaultTimings());}catch(ArgumentException){mixedRejected=true;}
        Check(mixedRejected,"Mixed definition sources rejected");
        catalog=fresh();catalog.Skill("hero-3",1).targetCount=2;
        custom=new PlayableBattle(10,new PlayableProgress(),9,combatDefinitions:catalog);
        while(custom.AvailableHero!=3 && !custom.Ended) custom.Pass();custom.DrainPresentationEvents();
        custom.State.Heroes[0].TakeDamage(30);custom.State.Heroes[2].TakeDamage(30);custom.State.Heroes[3].GainResource(3);
        int beforeResource=custom.State.Heroes[3].JobResource;
        Check(custom.ActWithAllies(3,1,"body",new[]{0,2}),"JSON selected-target count can be two");
        var healingEvent=custom.DrainPresentationEvents().First(e=>e.Kind==BattlePresentationKind.Healing);
        Check(healingEvent.HealingTargets.SequenceEqual(new[]{0,2}) && healingEvent.Resources[3]==beforeResource-3,"Defined heal targets and cost reach event snapshots");
        catalog=fresh();catalog.Skill("hero-4",1).resourceCost=1;catalog.Skill("hero-4",1).chainEligible=false;
        custom=new PlayableBattle(10,new PlayableProgress(),9,combatDefinitions:catalog);
        while(custom.AvailableHero!=4 && !custom.Ended) custom.Pass();custom.DrainPresentationEvents();
        beforeResource=custom.State.Heroes[4].JobResource;
        Check(custom.Act(4,1,"body"),"JSON casting cost is used at reservation");
        var castingEvents=new List<BattlePresentationEvent>(custom.DrainPresentationEvents());
        Check(castingEvents.First().Kind==BattlePresentationKind.CastStart && castingEvents.First().Resources[4]==beforeResource-1,"Cast start snapshot uses defined cost");
        while(custom.IsCasting(4) && !custom.Ended) {custom.Pass();castingEvents.AddRange(custom.DrainPresentationEvents());}
        Check(castingEvents.Any(e=>e.Kind==BattlePresentationKind.CastRelease) && !castingEvents.Any(e=>e.Message.StartsWith("自動チェイン")),"Deferred cast obeys defined chain eligibility");
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
    private static void ExpectCombatFailure(CombatDefinitionCatalog catalog,string message)
    {
        bool rejected=false;try {catalog.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,message);
    }
}
