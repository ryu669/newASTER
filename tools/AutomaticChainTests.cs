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
        string referenceJson=File.ReadAllText(args[1]);
        Func<HeroineReferenceCatalog> freshReference=()=>JsonSerializer.Deserialize<HeroineReferenceCatalog>(referenceJson,options);
        var reference=freshReference();reference.Validate();
        Check(reference.formation.Length==5 && reference.heroines.Sum(h=>h.skills.Length)==15,"Five selected heroines and fifteen observed skills");
        reference.heroines=reference.heroines.Reverse().ToArray();reference.Validate();
        Check(reference.Hero(reference.formation[3]).jobId=="job.blaster","Formal roster resolves by ID, not JSON order");
        Check(reference.Hero("heroine.slayer").skills[1].effects.All(e=>e.turns==3),"Slayer self buffs have three-turn duration");
        Check(reference.Hero("heroine.iconoclast").skills[1].damageType=="magic","Berserker has observed magic attack");
        Check(reference.Hero("heroine.undermine").skills[1].effects.Any(e=>e.kind=="regen" && e.amount==140 && e.turns==4),"Defender regen is not generic party healing");
        Check(reference.Hero("heroine.echidna").skills.All(s=>s.damageType=="physical" && s.target=="enemy.range" && s.casting!="none"),"All three blaster skills cast physical ranged damage");
        Check(reference.Hero("heroine.excalipan").skills[1].effects.Any(e=>e.kind=="self-heal" && e.basis=="base-attack-percent" && e.amount==70),"Gunner healing preserves base attack basis");
        Action<Action<HeroineReferenceCatalog>,string> rejectReference=(mutate,message)=>{var candidate=freshReference();mutate(candidate);bool rejected=false;try{candidate.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,message);};
        rejectReference(r=>r.schemaVersion=2,"Unknown reference version rejected");
        rejectReference(r=>r.status="formal","Observed evidence cannot masquerade as executable formal data");
        rejectReference(r=>r.statContext="level-one-base","Equipment stats cannot masquerade as level-one bases");
        rejectReference(r=>r.formation[3]="hero-3","Placeholder IDs cannot replace formal roster");
        rejectReference(r=>r.heroines[3].jobId="job.healer","Blaster cannot be relabelled healer");
        rejectReference(r=>r.heroines[0].skills[0].id="heroine.echidna.1","Cross-owner skill rejected");
        rejectReference(r=>r.heroines[0].skills[0].effects[0].kind="invented","Unknown effect rejected");
        rejectReference(r=>r.heroines[0].observedStats.speed=-1,"Unknown base speed cannot be silently converted to observed value");
        rejectReference(r=>r.heroines[0].skills[0].casting="longer-maybe","Unsupported timing category rejected");
        rejectReference(r=>r.heroines[0].skills[0].attackPercent=0,"Damage attack cannot have missing multiplier");
        rejectReference(r=>r.heroines[0].sourceFile=null,"Missing evidence source rejected");
        rejectReference(r=>r.heroines[0].unresolved=new string[0],"Incomplete conversion cannot hide unresolved rules");
        string json=File.ReadAllText(args[0]);
        Func<CombatDefinitionCatalog> fresh=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(json,options);
        var catalog=fresh();catalog.Validate();
        Check(catalog.Healing().Length==3 && catalog.Chain().Length==5,"Actual JSON creates healing and chain definitions");
        catalog.heroines=catalog.heroines.Reverse().ToArray();catalog.skills=catalog.skills.Reverse().ToArray();catalog.chainActions=catalog.chainActions.Reverse().ToArray();catalog.Validate();
        Check(catalog.Skill("hero-4",1).castPercent==150 && catalog.Chain()[0].HeroId=="hero-0","JSON order does not change formation ownership");
        // Compare a legacy-equivalent fixed-action profile; the actual placeholder
        // JSON now intentionally has distinct part damage and healing actions.
        foreach(var a in catalog.chainActions) {a.effectRuleId="effect.damage";a.targetRuleId="target.boss-body";a.powerScale=.6f;a.baseHealing=0;}
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
        catalog=fresh();catalog.chainActions[0].effectRuleId="effect.unknown";ExpectCombatFailure(catalog,"Unknown chain effects rejected");
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
        ValidateFixedEffects(fresh);
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
    private static BattleState EffectState()
    {
        return new BattleState(1,Enumerable.Range(0,5).Select(i=>new BattleHero("hero-"+i,i==1?200:100,20,10)),
            Enumerable.Range(0,4).Select(i=>new BattlePart("part-"+i,new[]{20,30,20,50}[i],i==0?"gauge-down":"")),1000,4);
    }
    private static void ValidateFixedEffects(Func<CombatDefinitionCatalog> fresh)
    {
        var state=EffectState();state.Heroes[0].TakeDamage(40);state.Heroes[1].TakeDamage(100);state.Heroes[3].TakeDamage(100);
        foreach(var h in state.Heroes) h.GainResource(3);
        var weakest=new HeroineChainAction("hero-4","chain.test",0,ChainEffect.Heal,ChainTarget.LowestHpAlly,15);
        var effect=ChainActionResolver.Resolve(state,4,weakest);
        Check(effect.TargetIds.SequenceEqual(new[]{"hero-1"}) && effect.HealingTargets.SequenceEqual(new[]{1}) && effect.Amount==15,"Lowest HP ratio, not absolute HP, chooses target");
        Check(state.Heroes[3].HitPoints==0 && state.Heroes.All(h=>h.JobResource==3),"Fixed healing cannot resurrect or change resources");
        state=EffectState();state.Heroes[0].TakeDamage(50);state.Heroes[1].TakeDamage(100);
        effect=ChainActionResolver.Resolve(state,4,weakest);Check(effect.TargetIds.Single()=="hero-0","Equal ratios use formation order without RNG");
        state.Heroes[2].TakeDamage(10);state.Heroes[3].TakeDamage(100);
        effect=ChainActionResolver.Resolve(state,4,new HeroineChainAction("hero-4","chain.all",0,ChainEffect.Heal,ChainTarget.AllLivingAllies,500));
        Check(effect.TargetIds.Count==4 && effect.HealingTargets.SequenceEqual(new[]{0,1,2}) && effect.Amount==145,"All-living heal caps HP and tracks actual healed targets");
        effect=ChainActionResolver.Resolve(state,4,weakest);Check(effect.TargetIds.Count==0 && effect.Amount==0,"No wounded target is an explicit no-effect result");
        state.Heroes[4].TakeDamage(10);
        effect=ChainActionResolver.Resolve(state,4,new HeroineChainAction("hero-4","chain.self",0,ChainEffect.Heal,ChainTarget.Self,30));
        Check(effect.TargetIds.Single()=="hero-4" && effect.Amount==10,"Self heal is clamped");
        state=EffectState();state.AdvanceBossGauge(2);
        var partAttack=new HeroineChainAction("hero-4","chain.part",2,ChainEffect.Damage,ChainTarget.LowestHpPart);
        effect=ChainActionResolver.Resolve(state,4,partAttack);
        Check(effect.Target=="part-0" && effect.Amount==20 && effect.PartBroken && state.BossGauge==1 && state.BossHitPoints==1000,"Part damage honors tie order, cap and break effect");
        foreach(var p in state.Parts) state.BreakPart(p.Id,1000);
        effect=ChainActionResolver.Resolve(state,4,partAttack);Check(effect.TargetIds.Count==0 && state.BossHitPoints==1000,"No surviving parts cannot silently retarget body");
        state.ApplyBossDamage(1000);state.Heroes[0].TakeDamage(10);
        effect=ChainActionResolver.Resolve(state,4,weakest);Check(effect.TargetIds.Count==0 && state.Heroes[0].HitPoints==90,"Battle victory stops even a healing fixed action");
        state=EffectState();state.Heroes[4].TakeDamage(100);
        effect=ChainActionResolver.Resolve(state,4,weakest);Check(effect.TargetIds.Count==0,"Dead actor cannot perform a fixed action");
        bool rejected=false;try{ChainActionResolver.Resolve(state,0,partAttack);}catch(ArgumentException){rejected=true;}Check(rejected,"Resolver rejects action owned by another heroine");
        var catalog=fresh();catalog.chainActions[0].targetRuleId="target.self";ExpectCombatFailure(catalog,"Damage cannot target an ally");
        catalog=fresh();catalog.chainActions[2].targetRuleId="target.boss-body";ExpectCombatFailure(catalog,"Heal cannot target enemy");
        catalog=fresh();catalog.chainActions[2].powerScale=0;catalog.chainActions[2].baseHealing=0;ExpectCombatFailure(catalog,"Zero-strength healing is rejected");
        bool sawPart=false,sawHeal=false,sawFullHeal=false,sawCastingHeal=false;
        for(int seed=1;seed<=100;seed++) {
            catalog=fresh();
            catalog.chainActions[2].targetRuleId="target.all-living-allies";
            var a=new PlayableBattle(50,new PlayableProgress(),seed,combatDefinitions:catalog);
            var b=new PlayableBattle(50,new PlayableProgress(),seed,combatDefinitions:catalog);
            for(int i=0;i<5;i++){a.State.Heroes[i].TakeDamage(20);b.State.Heroes[i].TakeDamage(20);}
            a.DrainPresentationEvents();b.DrainPresentationEvents();int actor=a.AvailableHero;long tick=a.Clock;
            a.Act(actor,0,"body");b.Act(actor,0,"body");
            var events=a.DrainPresentationEvents();var fixedEvents=events.Where(e=>e.ChainActionId!=null).ToArray();var origin=events.First();
            Check(a.Log==b.Log && a.State.BossHitPoints==b.State.BossHitPoints && a.State.Heroes.Select(h=>h.HitPoints).SequenceEqual(b.State.Heroes.Select(h=>h.HitPoints)),"Mixed fixed effects reproduce with same seed");
            Check(fixedEvents.All(e=>e.Clock==tick && e.Resources.SequenceEqual(origin.Resources) && !string.IsNullOrEmpty(e.PresentationId)),"Fixed effects preserve logical time/resources and include presentation IDs");
            Check(fixedEvents.Where(e=>e.Kind==BattlePresentationKind.Healing).All(e=>e.Damage==0 && e.HealingTargets.All(i=>e.TargetIds.Contains("hero-"+i))),"Heal events identify actor separately from affected heroes");
            sawPart|=fixedEvents.Any(e=>e.Actor==1 && e.Target!="body");sawHeal|=fixedEvents.Any(e=>e.Kind==BattlePresentationKind.Healing);sawFullHeal|=fixedEvents.Any(e=>e.Kind==BattlePresentationKind.Healing && e.FullChain);
            var pending=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:catalog);
            while(pending.AvailableHero!=4 && !pending.Ended) pending.Pass();pending.Act(4,1,"body");pending.DrainPresentationEvents();
            if(pending.AvailableHero<0) continue;pending.State.Heroes[4].TakeDamage(40);long castAt=pending.NextAt(4);pending.Act(pending.AvailableHero,0,"body");
            var healed=pending.DrainPresentationEvents().FirstOrDefault(e=>e.Kind==BattlePresentationKind.Healing && e.ChainActionId!=null && e.HealingTargets.Contains(4));
            if(healed!=null && healed.Casting[4]) {sawCastingHeal=true;Check(pending.IsCasting(4) && pending.NextAt(4)==castAt,"Fixed healing preserves the exact cast reservation time");}
        }
        Check(sawPart && sawHeal && sawFullHeal,"Seed sample executes part attacks and healing in normal and bonus laps");
        Check(sawCastingHeal,"Fixed healing can affect a casting heroine without canceling its reservation");
    }
}
