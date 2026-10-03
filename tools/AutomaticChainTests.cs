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
        CollectionContractTests.Run(Check);
        FormalProgressionTests.Run(Check);
        FormalCampaignTests.Run(Check);
        FormalRecoveryTests.Run(Check);
        FormalEngagementTests.Run(Check);
        var options=new JsonSerializerOptions {IncludeFields=true};
        FormalKinderTests.Run(Check,JsonSerializer.Deserialize<FormalKinderBanner>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]),"../Economy/kinder-trial.json")),options));
        string formalJson=File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]),"battle-formal.json"));
        Func<CombatDefinitionCatalog> formal=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(formalJson,options);
        FormalProgressionTests.Battle(Check,formal());
        BattleEndTests.Run(Check,formal());
        Plan5WorldRelicTests.Run(Check,formal());
        Plan6HomeTests.Run(Check,formal());
        Plan6BookTests.Run(Check);
        Plan5TrialMeasurements.Run(Check,formal());
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
        catalog=fresh();catalog.schemaVersion=3;ExpectCombatFailure(catalog,"Unknown version rejected");
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
        ValidateAttackFollowUps(fresh);
        ValidateTimedSelfEffects(fresh);
        ValidateExtendedCombat(fresh,freshReference());
        ValidateDamageDefense(fresh);
        ValidateFormalCombat(formal,freshReference());
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
    private static void ValidateFormalCombat(Func<CombatDefinitionCatalog> fresh,HeroineReferenceCatalog evidence)
    {
        var catalog=fresh();catalog.Validate();
        Check(catalog.IsFormal && catalog.Healing().Length==0 && catalog.Chain().Length==5,"Formal roster has no borrowed generic healer");
        Check(catalog.formation.SequenceEqual(evidence.formation),"Executable formal roster matches all five selected identities");
        Check(catalog.contentReferences.Count(r=>r.kind=="poem-link")==90 && catalog.contentReferences.Count(r=>r.kind=="affinity-event")==15 && catalog.contentReferences.Count(r=>r.kind=="lover-event")==10,"Complete heroine content references have exact required counts");
        foreach(var id in catalog.formation) for(int slot=0;slot<3;slot++) {
            var observed=evidence.Hero(id).skills[slot];var skill=catalog.Skill(id,slot);
            Check(skill.name==observed.name && skill.sourceSkillId==observed.id && skill.powerScale==observed.attackPercent/100f,"Confirmed skill name and Lv7 multiplier retained "+observed.id);
            Check(skill.castPercent>0==(observed.casting!="none"),"Confirmed casting category retained "+observed.id);
            Check(skill.sourceFile==evidence.Hero(id).sourceFile && skill.sourceSecond==15 && (observed.attackPercent==0 || skill.damageType==observed.damageType),"Source file and physical/magic category retained "+observed.id);
            foreach(var e in observed.effects.Where(e=>!EnemyStatusState.Kinds.Contains(e.kind))) {
                bool present=e.turns>0?skill.selfEffects.Any(s=>s.kind==e.kind && s.percent==e.amount && s.turns==e.turns):
                    e.kind=="critical"?skill.criticalBonusBp==e.amount*100:
                    e.kind=="damage-cap"?skill.damageCap==10000+e.amount:
                    e.kind=="self-damage"?skill.selfDamageMaxHpPercent==e.amount:
                    e.kind=="self-heal"?skill.selfHealingBaseAttackPercent==e.amount:
                    e.kind=="wt-add"?skill.enemyWaitAdd==e.amount:
                    e.kind=="wt-reduce-percent"?skill.selfWaitReductionPercent==e.amount:
                    e.kind=="charge-consume-max"?skill.chargeConsumeMax==e.amount:
                    e.kind=="ignore-defense"?skill.ignoreDefenseBp==10000:
                    e.kind=="special-weapon-damage"?skill.specialWeaponBonusPercent==e.amount:false;
                Check(present,"Every observed component is implemented "+observed.id+" "+e.kind);
            }
            foreach(var e in observed.effects.Where(e=>EnemyStatusState.Kinds.Contains(e.kind))) Check(skill.statusEffects.Any(s=>s.kind==e.kind && s.amount==e.amount),"Observed accumulation retained "+observed.id+" "+e.kind);
        }
        catalog.heroines=catalog.heroines.Reverse().ToArray();catalog.skills=catalog.skills.Reverse().ToArray();catalog.jobs=catalog.jobs.Reverse().ToArray();catalog.contentReferences=catalog.contentReferences.Reverse().ToArray();catalog.Validate();
        Check(catalog.HeroIdAt(3)=="heroine.echidna" && catalog.Skill(catalog.HeroIdAt(3),2).castPercent==125,"Formal references are independent of array order");
        Action<Action<CombatDefinitionCatalog>,string> reject=(mutate,message)=>{var bad=fresh();mutate(bad);ExpectCombatFailure(bad,message);};
        reject(c=>c.jobs=null,"Missing formal jobs rejected");
        reject(c=>c.jobs[0].resourceMax=0,"Missing resource capacity rejected");
        reject(c=>c.heroines[0].traitId=c.heroines[1].traitId,"Cross-person trait rejected");
        reject(c=>c.heroines[0].weaponTreeId="missing","Missing weapon reference rejected");
        reject(c=>c.heroines[0].poemChapters=new string[0],"Missing poem chapters rejected");
        reject(c=>c.heroines[0].poemLinks[0]=c.heroines[1].poemLinks[0],"Cross-person poem link rejected");
        reject(c=>c.heroines[0].affinityEventIds=new string[2],"Incomplete affinity event references rejected");
        reject(c=>c.heroines[0].loverEventIds=null,"Missing lover event references rejected");
        reject(c=>c.heroines[0].hpBp=9999,"Invalid job plus character stat budget rejected");
        reject(c=>c.heroines[0].speedBp=10501,"Speed construction range enforced");
        reject(c=>c.jobs[0].hp=int.MaxValue,"Overflowing formal job base rejected");
        reject(c=>c.enemyStatusResistances=new[]{new EnemyStatusResistanceDef {kind="burn",resistanceBp=10001}},"Invalid status resistance rejected");
        reject(c=>c.designOrigin="source-confirmed","Original rules cannot masquerade as source-confirmed");
        reject(c=>c.skills[0].sourceSkillId=null,"Missing observation provenance rejected");
        reject(c=>c.chainActions[0].ruleOrigin=null,"Missing original fixed-action provenance rejected");
        reject(c=>c.skills[0].statusEffects=new[]{new EnemyStatusDef {kind="invented",amount=100}},"Unknown status rejected");
        reject(c=>c.Skill("heroine.slayer",1).statusEffects=new[]{new EnemyStatusDef {kind="burn",amount=100}},"Self support cannot accumulate enemy status");
        reject(c=>c.skills[0].conditions=new[]{new SkillConditionDef {kind="trait-equipped",threshold=1,referenceId="heroine.iconoclast.trait"}},"Cross-person condition rejected");
        var status=new EnemyStatusState();status.Add(new EnemyStatusDef {kind="burn",amount=99});
        Check(!status.Active("burn") && status.Meter("burn")==99,"Status value is accumulation, not probability");
        status.Add(new EnemyStatusDef {kind="burn",amount=1});
        Check(status.Active("burn") && status.Meter("burn")==0 && status.Dot(1000)==20,"Threshold 100 applies three-action burn");
        status.Tick();status.Tick();Check(status.Active("burn") && status.Remaining("burn")==1,"Status duration lasts three enemy opportunities");
        status.Tick();Check(!status.Active("burn"),"Status expires independently");
        status.Add(new EnemyStatusDef {kind="poison",amount=175});Check(status.Meter("poison")==75 && status.Remaining("poison")==3,"Overflow is retained below threshold");
        status.Add(new EnemyStatusDef {kind="bleed",amount=100});Check(status.Dot(1000)==50,"Independent poison and bleed damages add");
        var resistant=new EnemyStatusState(new[]{new EnemyStatusResistanceDef {kind="burn",resistanceBp=5000},new EnemyStatusResistanceDef {kind="stun",resistanceBp=10000}});
        resistant.Add(new EnemyStatusDef {kind="burn",amount=100});Check(resistant.Meter("burn")==50 && !resistant.Active("burn"),"Resistance reduces accumulation instead of adding a hidden random roll");
        resistant.Add(new EnemyStatusDef {kind="burn",amount=100});Check(resistant.Active("burn"),"Resisted values still accumulate to threshold");
        resistant.Add(new EnemyStatusDef {kind="stun",amount=1000});Check(!resistant.Active("stun") && resistant.Meter("stun")==0,"Full resistance is deterministic immunity");
        var state=new BattleState(1,Enumerable.Range(0,5).Select(i=>new BattleHero("hero-"+i,1000,100,10)),Enumerable.Range(0,4).Select(i=>new BattlePart("part-"+i,1000,"")),10000,4);
        var actor=state.Heroes[0];actor.TakeDamage(100);actor.GainResource(10);int draws=0;
        var effects=new[]{new EnemyStatusDef {kind="burn",amount=120}};
        var all=new BattleSkill("all",1,3,70,10,criticalChanceBp:2000,targetRule:"target.all-enemies",statusEffects:effects);
        effects[0].amount=1;
        all.StatusEffects[0].amount=1;
        var result=BattleActionResolver.Resolve(state,actor.Id,all,"body",max=>{draws++;return 9999;});
        Check(result.Accepted && result.TargetIds.Count==5 && result.Damage==500,"All attack independently hits body and all four living parts");
        Check(draws==1 && actor.JobResource==7 && result.SelfHealing==70 && result.SelfDamage==100 && actor.HitPoints==870,"Multi-target cost critical and self follow-ups occur only once");
        Check(state.BossStatus.Meter("burn")==20 && state.Parts.All(p=>p.Status.Active("burn")),"Statuses are local per target and copied from definitions");
        state.BreakPart("part-1",10000);
        Check(EnemyAttackTargets.Resolve(state,"target.enemy-range","part-0").SequenceEqual(new[]{"part-0"}),"Range excludes broken adjacent parts rather than adding a substitute");
        Check(EnemyAttackTargets.Resolve(state,"target.enemy-range","body").SequenceEqual(new[]{"body","part-0"}),"Body range has explicit stable adjacency");
        Check(EnemyAttackTargets.Resolve(state,"target.selected-enemy","part-1").Length==0,"Broken single target is invalid");
        int resourceBefore=actor.JobResource,hpBefore=state.BossHitPoints;
        result=BattleActionResolver.Resolve(state,actor.Id,new BattleSkill("range",1,3,targetRule:"target.enemy-range"),"part-1");
        Check(!result.Accepted && actor.JobResource==resourceBefore && state.BossHitPoints==hpBefore,"Invalid range anchor fails atomically");
        bool invalidDraw=false;try{BattleActionResolver.Resolve(state,actor.Id,all,"body",max=>10000);}catch(ArgumentException){invalidDraw=true;}
        Check(invalidDraw && actor.JobResource==resourceBefore && state.BossHitPoints==hpBefore,"Invalid group critical draw leaves all targets and cost unchanged");
        var condition=new[]{new SkillConditionDef {kind="boss-status-active",threshold=1,referenceId="burn"}};
        Check(SkillConditionDef.AllSatisfied(state,0,condition),"Active boss status can gate a skill");
        Check(!SkillConditionDef.AllSatisfied(state,0,new[]{new SkillConditionDef {kind="trait-equipped",threshold=1,referenceId="absent.trait"}}),"Trait condition checks actual equipped definition");
        // Execute each of the actual fifteen commands independently, including every Blaster slot.
        for(int index=0;index<5;index++) for(int slot=0;slot<3;slot++) {
            var defs=fresh();foreach(var s in defs.skills) s.chainEligible=false;
            var legacy=new PlayableProgress();legacy.Levels[0]=120;legacy.Branches[0]=10;
            var battle=new PlayableBattle(50,legacy,40+index*3+slot,combatDefinitions:defs);
            Check(battle.IsFormal && battle.State.Heroes[index].Id==defs.formation[index],"Formal identity bound for skill "+index+"/"+slot);
            Check(battle.State.Heroes[0].MaxHitPoints==350 && battle.State.Heroes[0].BaseAttack==47,"Legacy levels and equipment are not transferred into formal identity");
            int attempts=0;while(battle.AvailableHero!=index && !battle.Ended && attempts++<100) battle.Pass();
            battle.State.Heroes[index].GainResource(15);
            battle.DrainPresentationEvents();int resource=battle.State.Heroes[index].JobResource;
            int cost=battle.SkillResourceCost(index,slot),prediction=battle.PreviewDamage(index,slot,"body");
            Check(battle.Act(index,slot,"body"),"Actual formal command executes "+defs.Skill(defs.formation[index],slot).id);
            var events=new List<BattlePresentationEvent>(battle.DrainPresentationEvents());
            while(battle.IsCasting(index) && !battle.Ended && attempts++<200) {battle.Pass();events.AddRange(battle.DrainPresentationEvents());}
            var command=events.FirstOrDefault(e=>e.Actor==index && (e.Kind==BattlePresentationKind.Attack || e.Kind==BattlePresentationKind.CastRelease));
            if(defs.Skill(defs.formation[index],slot).effectRuleId=="effect.damage") {
                Check(command!=null && command.Damage>=prediction,"Attack or cast resolves real defined effects "+index+"/"+slot);
                Check(command.TargetIds.Count==(index==3?3:index==1 && slot==2 || index==4 && slot==2?5:1),"Actual formal target count "+index+"/"+slot);
            } else Check(battle.State.Heroes[index].TimedEffects.Count==defs.Skill(defs.formation[index],slot).selfEffects.Length,"Actual formal self-buff applies all components");
            Check(legacy.Levels[0]==120 && legacy.Branches[0]==10,"Formal encounter does not mutate the legacy save");
        }
        // Resource rejection must leave cast/timeline/status untouched.
        var emptyDefs=fresh();var empty=new PlayableBattle(1,new PlayableProgress(),2,combatDefinitions:emptyDefs);
        while(empty.AvailableHero!=3 && !empty.Ended) empty.Pass();empty.State.Heroes[3].SpendResource(15); // Explicitly empty the available balance.
        empty.State.Heroes[3].SpendResource(empty.State.Heroes[3].JobResource);
        long clock=empty.Clock;hpBefore=empty.State.BossHitPoints;
        Check(!empty.Act(3,2,"body") && empty.Clock==clock && !empty.IsCasting(3) && empty.State.BossHitPoints==hpBefore,"Insufficient magic rejects cast without mutation");
        var cancelDefs=fresh();foreach(var s in cancelDefs.skills)s.chainEligible=false;
        var cancel=new PlayableBattle(1,new PlayableProgress(),3,combatDefinitions:cancelDefs);
        while(cancel.AvailableHero!=3 && !cancel.Ended) cancel.Pass();
        string anchor=cancel.State.Parts[0].Id;cancel.State.Heroes[3].GainResource(10);
        Check(cancel.Act(3,2,anchor),"Range cast reserves its selected anchor");
        cancel.State.BreakPart(anchor,100000);var canceledEvents=new List<BattlePresentationEvent>(cancel.DrainPresentationEvents());
        int guard=0;while(cancel.IsCasting(3) && !cancel.Ended && guard++<100){cancel.Pass();canceledEvents.AddRange(cancel.DrainPresentationEvents());}
        Check(canceledEvents.Any(e=>e.Actor==3 && e.Kind==BattlePresentationKind.CastCanceled) && !cancel.State.Parts[1].Status.Active("fracture"),"Destroyed range anchor cancels release with no statuses or silent retarget");
        var wtDefs=fresh();foreach(var s in wtDefs.skills)s.chainEligible=false;
        var wt=new PlayableBattle(1,new PlayableProgress(),17,combatDefinitions:wtDefs);
        while(wt.AvailableHero!=1 && !wt.Ended)wt.Pass();
        long originalBoss=wt.UpcomingOrder().First(e=>e.Actor<0).At,commandClock=wt.Clock;
        Check(wt.Act(1,0,"body") && wt.NextAt(1)==commandClock+wt.RecoveryDelay(1,0)/2,"Berserker self WT reduction changes only next normal action");
        Check(wt.UpcomingOrder().First(e=>e.Actor<0).At==originalBoss+30 && wt.State.Heroes[1].JobResource==3,"Berserker WT adds boss ticks and gains declared anger");
        var charge=new PlayableBattle(1,new PlayableProgress(),18,combatDefinitions:wtDefs);
        while(charge.AvailableHero!=2 && !charge.Ended)charge.Pass();charge.State.Heroes[2].GainResource(15);
        Check(charge.SkillResourceCost(2,2)==9 && charge.Act(2,2,"body") && charge.State.Heroes[2].JobResource==6,"Defender consumes at most nine of fifteen charges once");
        var gunner=new PlayableBattle(1,new PlayableProgress(),19,combatDefinitions:wtDefs);gunner.State.Heroes[4].GainResource(2);
        Check(gunner.AvailableHero==4 && gunner.PreviewDamage(4,2,"body")==809,"Special weapon plus-200 rule applies body protection only to body, not the four parts");
        Check(gunner.Act(4,2,"body") && gunner.State.Heroes[4].JobResource==1,"Special weapon spends six bullets and gains one declared post-attack bullet");
        var stun=new PlayableBattle(1,new PlayableProgress(),20,combatDefinitions:fresh());
        stun.State.BossStatus.Add(new EnemyStatusDef {kind="stun",amount=100});int enemyCount=stun.EnemyActionCount;
        var untouched=stun.State.Heroes.Select(h=>h.HitPoints).ToArray();
        while(stun.EnemyActionCount==enemyCount && !stun.Ended)stun.Pass();
        Check(untouched.SequenceEqual(stun.State.Heroes.Select(h=>h.HitPoints)) && !stun.State.BossStatus.Active("stun"),"Boss stun skips exactly one scheduled attack and expires");
        var snapshot=stun.DrainPresentationEvents().Last();string priorStatus=snapshot.EnemyStatuses[0];
        stun.State.BossStatus.Add(new EnemyStatusDef {kind="poison",amount=100});
        Check(snapshot.EnemyStatuses[0]==priorStatus,"Presentation status strings are immutable snapshots");
        var dotBreak=new PlayableBattle(1,new PlayableProgress(),21,combatDefinitions:fresh());
        dotBreak.State.AdvanceBossGauge(3);dotBreak.State.Parts[0].ApplyDamage(dotBreak.State.Parts[0].HitPoints-1);
        dotBreak.State.Parts[0].Status.Add(new EnemyStatusDef {kind="burn",amount=100});enemyCount=dotBreak.EnemyActionCount;
        while(dotBreak.EnemyActionCount==enemyCount && !dotBreak.Ended)dotBreak.Pass();
        Check(dotBreak.State.Parts[0].IsBroken && dotBreak.DrainPresentationEvents().Last(e=>e.Kind==BattlePresentationKind.Enemy).Major==false,"Part DoT destruction suppresses the pending major before target and damage selection");
        // Fixed actions are individually owned and do not invoke normal resource/cast rules.
        var fixedBattle=new PlayableBattle(50,new PlayableProgress(),5,combatDefinitions:fresh());
        for(int i=0;i<5;i++) {
            int resource=fixedBattle.State.Heroes[i].JobResource;long at=fixedBattle.NextAt(i);
            var action=fresh().Chain()[i];result=null;
            var fixedResult=ChainActionResolver.Resolve(fixedBattle.State,i,action);
            Check(fixedResult.Amount>0 && fixedBattle.State.Heroes[i].JobResource==resource && fixedBattle.NextAt(i)==at,"Formal fixed action is independent "+i);
        }
        int victories=0;
        Func<int,string> simulate=seed=>{
            var battle=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:fresh());var trace=new List<string>();int steps=0;
            while(!battle.Ended && steps<300) {
                int who=battle.AvailableHero,slot=steps%3;bool used=false;
                for(int k=0;k<3;k++) {int candidate=(slot+k)%3;if(battle.State.Heroes[who].JobResource>=battle.SkillResourceCost(who,candidate) && battle.ConditionsSatisfied(who,candidate)){used=battle.Act(who,candidate,"body");if(used)break;}}
                if(!used)battle.Pass();steps++;
                foreach(var e in battle.DrainPresentationEvents())trace.Add(e.Clock+":"+e.Actor+":"+e.Kind+":"+e.Damage+":"+e.BossHp+":"+string.Join(",",e.HeroHp)+":"+string.Join(",",e.Resources));
            }
            Check(battle.Ended && steps<300,"Formal full encounter terminates seed "+seed);
            if(battle.State.IsVictory) victories++;
            return string.Join(";",trace);
        };
        for(int seed=0;seed<50;seed++) Check(simulate(seed)==simulate(seed),"Formal five-person encounter deterministic replay seed "+seed);
        Check(victories>0,"The actual formal Lv1 formation can defeat the preview colossus");
    }

    private static void ValidateDamageDefense(Func<CombatDefinitionCatalog> fresh)
    {
        var state=new BattleState(1,Enumerable.Range(0,5).Select(i=>new BattleHero("hero-"+i,1000,100,10)),Enumerable.Range(0,4).Select(i=>new BattlePart("part-"+i,1000,"",3000,1000)),10000,4,1000,3000);
        var hero=state.Heroes[0];
        var physical=new BattleSkill("physical",2,0);
        var magic=new BattleSkill("magic",2,0,damageType:"magic");
        Check(BattleActionResolver.CalculateDamage(state,hero,physical,"body")==100,"Physical damage selects physical body defense");
        Check(BattleActionResolver.CalculateDamage(state,hero,magic,"body")==50,"Magic damage selects magic body defense");
        Check(BattleActionResolver.CalculateDamage(state,hero,physical,"part-0")==50,"Part has independent physical defense");
        Check(BattleActionResolver.CalculateDamage(state,hero,magic,"part-0")==100,"Part has independent magic defense");
        Check(BattleActionResolver.CalculateDamage(state,hero,new BattleSkill("ignore",2,0,ignoreDefenseBp:10000),"body")==200,"Full defense ignore preserves raw damage");
        Check(BattleActionResolver.CalculateDamage(state,hero,new BattleSkill("partial",2,0,ignoreDefenseBp:5000),"body")==133,"Partial defense ignore rounds only final damage");
        Check(BattleActionResolver.CalculateDamage(state,hero,new BattleSkill("critical.cap",2,0,damageCap:120),"body",true)==120,"Critical then defense then cap");
        var result=BattleActionResolver.Resolve(state,hero.Id,physical,"body");
        Check(result.Damage==100 && state.BossHitPoints==9900,"Resolve uses shared defense calculation");
        hero.GainResource(3);int resourceBefore=hero.JobResource;
        var fixedResult=ChainActionResolver.Resolve(state,0,new HeroineChainAction(hero.Id,"fixed.magic",2,damageType:"magic"));
        Check(fixedResult.Amount==50 && hero.JobResource==resourceBefore,"Fixed magic action uses defense without cost or critical RNG");
        fixedResult=ChainActionResolver.Resolve(state,0,new HeroineChainAction(hero.Id,"fixed.part",2,target:ChainTarget.LowestHpPart,ignoreDefenseBp:10000));
        Check(fixedResult.Amount==200 && fixedResult.Target=="part-0","Fixed part action honors independent defense-ignore definition");
        var cappedState=new BattleState(1,state.Heroes,Enumerable.Range(0,4).Select(i=>new BattlePart("part-"+i,1000,"",int.MaxValue,int.MaxValue)),10000,4,int.MaxValue,int.MaxValue);
        Check(BattleActionResolver.CalculateDamage(cappedState,hero,physical,"body")==1,"Extreme defense has no overflow and preserves one damage minimum");
        bool rejected=false;try{new BattleSkill("bad",1,0,damageType:"true");}catch(ArgumentException){rejected=true;}Check(rejected,"Unknown damage type rejected");
        rejected=false;try{new BattleSkill("bad",1,0,ignoreDefenseBp:10001);}catch(ArgumentException){rejected=true;}Check(rejected,"Defense ignore above 100 percent rejected");
        rejected=false;try{new BattlePart("bad",10,"",-1);}catch(ArgumentException){rejected=true;}Check(rejected,"Negative part defense rejected");
        var catalog=fresh();catalog.enemyPhysicalDefense=1000;catalog.enemyMagicDefense=3000;
        var def=catalog.Skill("hero-0",0);def.damageType="magic";def.ignoreDefenseBp=5000;def.chainEligible=false;catalog.Validate();
        var battle=new PlayableBattle(1,new PlayableProgress(),29,combatDefinitions:catalog);int preview=battle.PreviewDamage(0,0,"body");
        catalog.enemyMagicDefense=0;def.damageType="physical";def.ignoreDefenseBp=10000;
        int hp=battle.State.BossHitPoints;Check(battle.Act(0,0,"body"),"Defined magic attack accepted");
        Check(hp-battle.State.BossHitPoints==preview && preview>0,"Preview matches copied definition and defense snapshot");
        catalog=fresh();catalog.Skill("hero-0",0).damageType="unknown";ExpectCombatFailure(catalog,"Unknown damage profile rejected at loading");
        catalog=fresh();catalog.enemyPhysicalDefense=-1;ExpectCombatFailure(catalog,"Negative enemy defense rejected at loading");
        catalog=fresh();catalog.Skill("hero-0",2).ignoreDefenseBp=1;ExpectCombatFailure(catalog,"Support cannot silently gain defense-ignore attack behavior");
        // Identical seeds remain deterministic; changing defense never consumes extra random numbers.
        for(int seed=0;seed<50;seed++) {
            var a=fresh();var b=fresh();b.enemyPhysicalDefense=1000;
            a.Skill("hero-0",0).criticalBonusBp=b.Skill("hero-0",0).criticalBonusBp=2000;
            a.Skill("hero-0",0).chainEligible=b.Skill("hero-0",0).chainEligible=false;
            var first=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:a);
            var second=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:b);
            first.Act(0,0,"body");second.Act(0,0,"body");
            Check(first.Clock==second.Clock && first.State.Heroes[0].JobResource==second.State.Heroes[0].JobResource,"Defense has no cost or timeline side effects seed "+seed);
        }
    }

    private static void ValidateExtendedCombat(Func<CombatDefinitionCatalog> fresh,HeroineReferenceCatalog reference)
    {
        var state=EffectState();int draws=0;var hero=state.Heroes[0];hero.GainResource(3);
        var critical=new BattleSkill("critical",1,3,criticalChanceBp:1500,criticalMultiplierPercent:210);
        var outcome=BattleActionResolver.Resolve(state,hero.Id,critical,"body",max=>{draws++;return 1499;});
        Check(outcome.Critical && outcome.CriticalRoll==1499 && outcome.Damage==42 && draws==1,"Critical threshold and multiplier use one explicit draw");
        hero.GainResource(3);outcome=BattleActionResolver.Resolve(state,hero.Id,critical,"body",max=>{draws++;return 1500;});
        Check(!outcome.Critical && outcome.Damage==20 && draws==2,"Roll equal to threshold is noncritical");
        outcome=BattleActionResolver.Resolve(state,hero.Id,critical,"body",max=>{draws++;return 0;});
        Check(!outcome.Accepted && draws==2,"Resource failure does not draw critical RNG");
        hero.GainResource(3);outcome=BattleActionResolver.Resolve(state,hero.Id,critical,"missing",max=>{draws++;return 0;});
        Check(!outcome.Accepted && draws==2 && hero.JobResource==3,"Invalid target does not draw or spend");
        bool rejected=false;try{BattleActionResolver.Resolve(state,hero.Id,critical,"body");}catch(ArgumentException){rejected=true;}
        Check(rejected && hero.JobResource==3,"Missing critical RNG is rejected before mutation");
        rejected=false;int bossHp=state.BossHitPoints;try{BattleActionResolver.Resolve(state,hero.Id,critical,"body",max=>10000);}catch(ArgumentException){rejected=true;}
        Check(rejected && hero.JobResource==3 && state.BossHitPoints==bossHp,"Invalid critical draw is rejected before mutation");
        outcome=BattleActionResolver.Resolve(state,hero.Id,new BattleSkill("guaranteed",1,0,criticalChanceBp:10000,damageCap:25),"body",max=>{draws++;return 9999;});
        Check(outcome.Critical && outcome.Damage==25 && outcome.CriticalRoll==-1 && draws==2,"Guaranteed critical needs no RNG and damage cap applies after multiplier");
        hero.ApplySelfEffects(new[]{Timed("critical",20,3),Timed("critical-damage",60,3)});
        Check(hero.CriticalChanceBp==2000 && hero.CriticalMultiplierPercent==210,"Critical buffs expose separate chance and damage parameters");
        hero.ApplySelfEffects(new[]{Timed("critical",100,3)});Check(hero.CriticalChanceBp==10000,"Critical chance caps at 100%");
        state.Heroes[2].ApplySelfEffects(new[]{Timed("forced-target",1,4)});
        Check(EnemyTargetSelector.Resolve(state,false,0).SequenceEqual(new[]{2}),"Forced target overrides single enemy attack selection");
        Check(EnemyTargetSelector.Resolve(state,true,0).Length==5,"Forced target does not cancel an all-party attack");
        state.Heroes[1].ApplySelfEffects(new[]{Timed("forced-target",1,4)});
        Check(EnemyTargetSelector.Resolve(state,false,0).SequenceEqual(new[]{1}),"Multiple forced targets choose stable formation order");
        state.Heroes[1].TakeDamage(1000);state.Heroes[2].TakeDamage(1000);
        Check(EnemyTargetSelector.Resolve(state,false,0).SequenceEqual(new[]{0}),"Dead forced targets are excluded");
        var conditions=new[]{new SkillConditionDef {kind="hp-at-most-percent",threshold=50},new SkillConditionDef {kind="broken-parts-at-least",threshold=1},new SkillConditionDef {kind="resource-at-least",threshold=3}};
        state=EffectState();hero=state.Heroes[0];hero.GainResource(3);hero.TakeDamage(50);
        Check(!SkillConditionDef.AllSatisfied(state,0,conditions),"Compound conditions require all predicates");
        state.BreakPart("part-0",1000);Check(SkillConditionDef.AllSatisfied(state,0,conditions),"HP threshold is inclusive and broken-part predicate resolves");
        hero.Heal(1);Check(!SkillConditionDef.AllSatisfied(state,0,conditions),"HP threshold does not round down into eligibility");
        var catalog=fresh();catalog.Skill("hero-0",0).conditions=new[]{new SkillConditionDef {kind="hp-at-most-percent",threshold=50}};
        var battle=new PlayableBattle(1,new PlayableProgress(),8,combatDefinitions:catalog);long clock=battle.Clock;int wallet=battle.State.Heroes[0].JobResource;
        Check(!battle.ConditionsSatisfied(0,0) && battle.PreviewDamage(0,0,"body")==0 && !battle.Act(0,0,"body") && battle.Clock==clock && battle.State.Heroes[0].JobResource==wallet,"Failed command condition is shared by preview and execution without time or resource effects");
        catalog.Skill("hero-0",0).conditions[0].threshold=100;
        Check(!battle.ConditionsSatisfied(0,0),"Nested condition definitions are cloned against external edits");
        battle.State.Heroes[0].TakeDamage(80);Check(battle.ConditionsSatisfied(0,0) && battle.PreviewDamage(0,0,"body")>0,"Condition changes follow current battle state");
        catalog=fresh();catalog.Skill("hero-0",0).conditions=new[]{new SkillConditionDef {kind="arbitrary-code",threshold=0}};ExpectCombatFailure(catalog,"Unknown conditions rejected");
        catalog=fresh();catalog.Skill("hero-0",0).conditions=new[]{new SkillConditionDef {kind="resource-at-least",threshold=11}};ExpectCombatFailure(catalog,"Out-of-range resource condition rejected");
        catalog=fresh();catalog.Skill("hero-0",0).conditions=new[]{new SkillConditionDef {kind="hp-at-most-percent",threshold=50},new SkillConditionDef {kind="hp-at-most-percent",threshold=30}};ExpectCombatFailure(catalog,"Duplicate condition kind rejected");
        catalog=fresh();catalog.Skill("hero-0",0).criticalBonusBp=10001;ExpectCombatFailure(catalog,"Out-of-range critical probability rejected");
        catalog=fresh();catalog.Skill("hero-0",0).damageCap=-1;ExpectCombatFailure(catalog,"Negative damage cap rejected");
        catalog=fresh();catalog.Skill("hero-0",2).criticalBonusBp=1500;ExpectCombatFailure(catalog,"Heal-only critical attack bonus rejected");
        catalog=SelfBuffCatalog(fresh,Timed("critical",20,3),Timed("critical-damage",60,3),Timed("attack",15,3));
        battle=new PlayableBattle(1,new PlayableProgress(),8,combatDefinitions:catalog);battle.Act(0,1,"body");
        Check(battle.State.Heroes[0].Attack==26 && battle.PreviewCriticalChanceBp(0,0)==2000 && battle.State.Heroes[0].CriticalMultiplierPercent==210,"Observed Slayer self-buff components can be resolved together");
        catalog=fresh();var third=catalog.Skill("hero-4",2);third.effectRuleId="effect.damage";third.targetRuleId="target.selected-enemy";third.powerScale=2.4f;third.castPercent=125;third.chainEligible=false;
        battle=new PlayableBattle(1,new PlayableProgress(),8,combatDefinitions:catalog);while(battle.AvailableHero!=4 && !battle.Ended) battle.Pass();
        Check(battle.PreviewDamage(4,2,"body")>0 && battle.Act(4,2,"body") && battle.IsCasting(4),"Third slot supports independent attack and casting timing");
        var events=new List<BattlePresentationEvent>(battle.DrainPresentationEvents());while(battle.IsCasting(4) && !battle.Ended){battle.Pass();events.AddRange(battle.DrainPresentationEvents());}
        Check(events.Any(e=>e.Kind==BattlePresentationKind.CastRelease && e.Actor==4),"Third-slot spell releases instead of using index-based support");
        catalog=fresh();catalog.Skill("hero-0",0).selfEffects=new[]{Timed("attack",20,3)};catalog.Skill("hero-0",0).chainEligible=false;
        battle=new PlayableBattle(1,new PlayableProgress(),8,combatDefinitions:catalog);battle.Act(0,0,"body");
        Check(battle.State.Heroes[0].Attack==27 && battle.State.Heroes[0].TimedEffects[0].RemainingCommands==3,"Attack can grant three future commands of self buff without immediate expiration");
        catalog=fresh();catalog.Skill("hero-4",1).selfEffects=new[]{Timed("attack",20,3)};catalog.Skill("hero-4",1).chainEligible=false;
        battle=new PlayableBattle(1,new PlayableProgress(),8,combatDefinitions:catalog);while(battle.AvailableHero!=4 && !battle.Ended) battle.Pass();battle.Act(4,1,"body");
        Check(battle.State.Heroes[4].TimedEffects.Count==0,"Attack-plus-buff is not granted at cast reservation");
        while(battle.IsCasting(4) && !battle.Ended) battle.Pass();
        Check(battle.State.Heroes[4].TimedEffects.Single().RemainingCommands==3,"Cast release grants a full future duration without double completion");
        catalog=fresh();catalog.schemaVersion=2;catalog.status="integration-trial";catalog.formation=(string[])reference.formation.Clone();
        for(int i=0;i<5;i++) {
            string old="hero-"+i;var h=catalog.Hero(old);h.id=catalog.formation[i];h.name=reference.Hero(h.id).name;h.jobId=reference.Hero(h.id).jobId;
            foreach(var skill in catalog.skills.Where(s=>s.ownerId==old)) skill.ownerId=h.id;
            foreach(var action in catalog.chainActions.Where(a=>a.heroineId==old)) action.heroineId=h.id;
        }
        catalog.Validate();catalog.heroines=catalog.heroines.Reverse().ToArray();catalog.skills=catalog.skills.Reverse().ToArray();catalog.chainActions=catalog.chainActions.Reverse().ToArray();catalog.Validate();
        battle=new PlayableBattle(1,new PlayableProgress(),8,combatDefinitions:catalog);
        Check(battle.State.Heroes.Select(h=>h.Id).SequenceEqual(reference.formation) && catalog.Chain().Select(a=>a.HeroId).SequenceEqual(reference.formation),"Formal IDs resolve command and fixed action ownership independently of JSON array order");
        catalog.formation[0]="heroine.missing";ExpectCombatFailure(catalog,"Missing formation reference rejected");
        catalog.formation=(string[])reference.formation.Clone();catalog.formation[0]=catalog.formation[1];ExpectCombatFailure(catalog,"Duplicate formation reference rejected");
        catalog.formation=(string[])reference.formation.Clone();catalog.status="formal";ExpectCombatFailure(catalog,"Integration trial cannot declare unverified full formal status");
        catalog=fresh();catalog.formation=Array.Empty<string>();catalog.Validate();Check(catalog.HeroIdAt(0)=="hero-0","Legacy empty formation from Unity JSON roundtrip remains compatible");
        for(int seed=1;seed<=50;seed++) {
            var a=fresh();a.Skill("hero-0",0).criticalBonusBp=1500;
            var first=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:a);var second=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:a);
            first.Act(0,0,"body");second.Act(0,0,"body");
            Check(first.Log==second.Log && first.Clock==second.Clock && first.State.BossHitPoints==second.State.BossHitPoints,"Critical and chain streams are reproducible seed="+seed);
        }
    }
    private static CombatDefinitionCatalog SelfBuffCatalog(Func<CombatDefinitionCatalog> fresh,params TimedSelfEffectDef[] effects)
    {
        var catalog=fresh();var skill=catalog.Skill("hero-0",1);
        skill.effectRuleId="effect.self-buff";skill.targetRuleId="target.self";skill.powerScale=0;skill.castPercent=0;skill.chainEligible=false;skill.selfEffects=effects;
        catalog.Skill("hero-0",0).chainEligible=false;catalog.Validate();return catalog;
    }
    private static TimedSelfEffectDef Timed(string kind,int percent,int turns) => new TimedSelfEffectDef {kind=kind,percent=percent,turns=turns};
    private static void ValidateTimedSelfEffects(Func<CombatDefinitionCatalog> fresh)
    {
        var hero=new BattleHero("test",100,20,10);
        hero.ApplySelfEffects(new[]{Timed("attack",15,3),Timed("physical-protection",40,4),Timed("regen",140,4)});
        Check(hero.BaseAttack==20 && hero.Attack==23 && hero.ProtectPhysicalDamage(19)==11,"Effective attack and physical protection floor while base attack stays immutable");
        hero.TakeDamage(60);Check(hero.RegenerateAtOwnerReady()==32 && hero.HitPoints==72,"Regen uses current attack and heals only at explicit owner readiness");
        Check(hero.RegenerateAtOwnerReady()==28 && hero.HitPoints==100,"Regen caps to missing HP");
        var saved=hero.TimedEffects;hero.CompleteOwnerCommand();
        Check(saved.Single(e=>e.Kind=="attack").RemainingCommands==3 && hero.TimedEffects.Single(e=>e.Kind=="attack").RemainingCommands==2,"Effect snapshots cannot change after later command completion");
        hero.CompleteOwnerCommand();Check(hero.Attack==23,"Last remaining command keeps attack bonus");
        hero.CompleteOwnerCommand();Check(hero.Attack==20 && !hero.TimedEffects.Any(e=>e.Kind=="attack") && hero.TimedEffects.Single(e=>e.Kind=="regen").RemainingCommands==1,"Three-command buff expires independently of four-command effects");
        hero.CompleteOwnerCommand();Check(hero.TimedEffects.Count==0 && hero.ProtectPhysicalDamage(19)==19,"Final completion expires all effects");
        hero.ApplySelfEffects(new[]{Timed("attack",50,2)});hero.ApplySelfEffects(new[]{Timed("attack",15,3)});
        Check(hero.Attack==23 && hero.TimedEffects.Count==1 && hero.TimedEffects[0].RemainingCommands==3,"Reapplication replaces same kind without stacking");
        bool rejected=false;try{hero.ApplySelfEffects(new[]{Timed("regen",140,4),Timed("unknown",50,2)});}catch(ArgumentException){rejected=true;}
        Check(rejected && hero.TimedEffects.Count==1 && hero.TimedEffects[0].Kind=="attack","Effect batch validation is atomic");
        hero.TakeDamage(1000);Check(hero.TimedEffects.Count==0 && !hero.ApplySelfEffects(new[]{Timed("attack",15,3)}) && hero.RegenerateAtOwnerReady()==0,"Death clears effects and regeneration cannot revive");
        hero=new BattleHero("large",int.MaxValue,int.MaxValue,10);hero.ApplySelfEffects(new[]{Timed("attack",1000,10),Timed("physical-protection",100,1)});
        Check(hero.Attack==int.MaxValue && hero.ProtectPhysicalDamage(int.MaxValue)==0,"Large buffs saturate and 100% physical protection gives zero damage");
        var state=EffectState();state.Heroes[0].ApplySelfEffects(new[]{Timed("attack",50,3)});state.Heroes[0].TakeDamage(40);
        var outcome=BattleActionResolver.Resolve(state,"hero-0",new BattleSkill("buffed.attack",1,0,70),"body");
        Check(outcome.Damage==30 && outcome.SelfHealing==14,"Damage uses effective attack but follow-up healing uses base attack");
        ChainActionResolver.Resolve(state,0,new HeroineChainAction("hero-0","fixed",1));
        Check(state.Heroes[0].TimedEffects[0].RemainingCommands==3,"Independent fixed action does not consume buff duration");
        var catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));
        var battle=new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:catalog);int actor=battle.AvailableHero;
        Check(battle.IsSelfBuff(actor,1) && battle.PreviewDamage(actor,1,"body")==0 && battle.SelfBuffDescription(actor,1).Contains("15%"),"UI recognizes self-buff command rather than fake damage or healing");
        int resource=battle.State.Heroes[actor].JobResource;battle.DrainPresentationEvents();
        catalog.Skill("hero-0",1).selfEffects[0].percent=1000;
        Check(battle.Act(actor,1,"missing") && battle.State.Heroes[actor].Attack==26 && battle.State.Heroes[actor].TimedEffects[0].RemainingCommands==3,"Self target ignores stale enemy selection and clones nested definitions");
        var events=battle.DrainPresentationEvents();
        Check(events[0].Kind==BattlePresentationKind.Support && events[0].TargetIds.SequenceEqual(new[]{"hero-0"}) && events[0].HeroEffects[0][0].RemainingCommands==3,"Buff activation event contains immutable duration snapshot and target ID");
        Check(battle.State.Heroes[actor].JobResource==resource-3 && battle.LastChainChecks.Count==0,"Self-buff consumes defined resource once without chain checks");
        while(battle.AvailableHero!=0 && !battle.Ended) battle.Pass();
        Check(battle.State.Heroes[0].TimedEffects[0].RemainingCommands==3,"Other actors and enemies do not consume owner's buff");
        battle.DrainPresentationEvents();battle.Act(0,0,"body");
        Check(battle.State.Heroes[0].TimedEffects[0].RemainingCommands==2 && events[0].HeroEffects[0][0].RemainingCommands==3,"Owner success consumes one duration while older presentation remains unchanged");
        events=battle.DrainPresentationEvents();Check(events.Any(e=>e.Kind==BattlePresentationKind.Support && e.HeroEffects[0][0].RemainingCommands==2),"Expiration update has a final-state presentation cue");
        catalog=SelfBuffCatalog(fresh,Timed("regen",140,4),Timed("physical-protection",40,4));
        battle=new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:catalog);battle.State.Heroes[0].TakeDamage(60);battle.DrainPresentationEvents();
        battle.Act(0,1,"body");var collected=new List<BattlePresentationEvent>(battle.DrainPresentationEvents());
        Check(!collected.Any(e=>e.Message.StartsWith("再生")),"Regen does not fire on grant command");
        Check(battle.PreviewEnemyDamage(0)==8,"Enemy physical preview applies 40% protection after existing modifiers");
        while(battle.AvailableHero!=0 && !battle.Ended) {battle.Pass();collected.AddRange(battle.DrainPresentationEvents());}
        var regen=collected.Single(e=>e.Message.StartsWith("再生"));
        Check(regen.HealingTargets.SequenceEqual(new[]{0}) && regen.TargetIds.SequenceEqual(new[]{"hero-0"}) && regen.Chain==0 && !regen.FullChain,"Owner-ready regeneration has correct target and standalone chain metadata");
        Check(battle.State.Heroes[0].TimedEffects.All(e=>e.RemainingCommands==4),"Regen tick does not consume command duration");
        battle.Pass();Check(battle.State.Heroes[0].TimedEffects.All(e=>e.RemainingCommands==3),"Pass consumes duration once");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));
        battle=new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:catalog);battle.State.Heroes[0].SpendResource(3);battle.DrainPresentationEvents();
        Check(!battle.Act(0,1,"body") && battle.State.Heroes[0].TimedEffects.Count==0 && battle.DrainPresentationEvents().Count==0,"Failed self-buff does not mutate duration or emit effects");
        catalog=fresh();battle=new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:catalog);
        while(battle.AvailableHero!=4 && !battle.Ended) battle.Pass();
        hero=battle.State.Heroes[4];hero.ApplySelfEffects(new[]{Timed("attack",50,1)});int forecast=battle.PreviewDamage(4,1,"body");battle.DrainPresentationEvents();
        Check(battle.Act(4,1,"body") && hero.TimedEffects.Count==0,"Cast reservation consumes owner duration, not release");
        collected=new List<BattlePresentationEvent>(battle.DrainPresentationEvents());
        while(battle.IsCasting(4) && !battle.Ended) {battle.Pass();collected.AddRange(battle.DrainPresentationEvents());}
        Check(collected.First(e=>e.Kind==BattlePresentationKind.CastRelease).Damage==forecast,"Reserved cast retains buffed attack despite effect expiring at reservation");
        catalog=fresh();battle=new PlayableBattle(1,new PlayableProgress(),11,combatDefinitions:catalog);
        while(battle.AvailableHero!=4 && !battle.Ended) battle.Pass();hero=battle.State.Heroes[4];hero.ApplySelfEffects(new[]{Timed("attack",50,3),Timed("regen",140,3)});
        battle.DrainPresentationEvents();battle.Act(4,1,"body");collected=new List<BattlePresentationEvent>(battle.DrainPresentationEvents());
        while(battle.IsCasting(4) && !battle.Ended) {battle.Pass();collected.AddRange(battle.DrainPresentationEvents());}
        Check(hero.TimedEffects.All(e=>e.RemainingCommands==2) && !collected.Any(e=>e.Message.StartsWith("再生") && e.Actor==4),"Cast release is not another owner command or regeneration opportunity");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));catalog.Skill("hero-0",1).selfEffects=new[]{Timed("speed",20,3)};ExpectCombatFailure(catalog,"Unsupported speed buff rejected, not silently ignored");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));catalog.Skill("hero-0",1).selfEffects=new[]{Timed("attack",15,3),Timed("attack",20,3)};ExpectCombatFailure(catalog,"Duplicate effect kinds rejected");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));catalog.Skill("hero-0",1).selfEffects[0].turns=0;ExpectCombatFailure(catalog,"Zero duration rejected");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));catalog.Skill("hero-0",1).selfEffects[0].percent=0;ExpectCombatFailure(catalog,"Zero effect rejected");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));catalog.Skill("hero-0",1).targetRuleId="target.all-living-allies";ExpectCombatFailure(catalog,"Unsupported party buff rejected");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));catalog.Skill("hero-0",1).castPercent=100;ExpectCombatFailure(catalog,"Unsupported casted self-buff rejected");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));catalog.Skill("hero-0",1).chainEligible=true;ExpectCombatFailure(catalog,"Undecided buff-started chain rejected");
        catalog=fresh();catalog.Skill("hero-0",2).selfEffects=new[]{Timed("attack",15,3)};ExpectCombatFailure(catalog,"Unsupported heal-plus-buff rejected");
        catalog=SelfBuffCatalog(fresh,Timed("attack",15,3));rejected=false;try{new PlayableBattle(1,new PlayableProgress(),useTimeline:false,combatDefinitions:catalog);}catch(ArgumentException){rejected=true;}
        Check(rejected,"Legacy non-timeline mode cannot silently change duration semantics");
        for(int seed=1;seed<=50;seed++) {
            var a=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:SelfBuffCatalog(fresh,Timed("attack",15,3)));
            var b=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:SelfBuffCatalog(fresh,Timed("attack",30,3)));
            a.Act(0,1,"body");b.Act(0,1,"body");
            Check(a.Clock==b.Clock && a.State.Heroes.Select(h=>h.JobResource).SequenceEqual(b.State.Heroes.Select(h=>h.JobResource)),"Buff amount does not change time or cost seed="+seed);
            while(a.AvailableHero!=0 && !a.Ended) a.Pass();while(b.AvailableHero!=0 && !b.Ended) b.Pass();
            Check(a.SkillChainBonusBp(0)==b.SkillChainBonusBp(0) && Enumerable.Range(0,5).All(i=>a.HasCumulativeChainBonus(i)==b.HasCumulativeChainBonus(i)),"Buff application adds no random draw seed="+seed);
        }
    }
    private static void ValidateAttackFollowUps(Func<CombatDefinitionCatalog> fresh)
    {
        var state=EffectState();var hero=state.Heroes[0];hero.TakeDamage(40);hero.GainResource(3);
        var skill=new BattleSkill("test.follow-up",1,3,70,10);
        var outcome=BattleActionResolver.Resolve(state,hero.Id,skill,"body");
        Check(outcome.Accepted && outcome.Damage==20 && outcome.SelfHealing==14 && outcome.SelfDamage==10 && hero.HitPoints==64,"Attack heals from base attack then recoils from maximum HP");
        Check(hero.JobResource==0 && state.BossHitPoints==980,"Compound attack pays once and attacks once");
        outcome=BattleActionResolver.Resolve(state,hero.Id,skill,"body");
        Check(!outcome.Accepted && outcome.SelfHealing==0 && outcome.SelfDamage==0 && hero.HitPoints==64 && state.BossHitPoints==980,"Resource failure has no follow-ups");
        hero.GainResource(3);int hp=hero.HitPoints;
        outcome=BattleActionResolver.Resolve(state,hero.Id,skill,"missing");
        Check(!outcome.Accepted && hero.HitPoints==hp && hero.JobResource==3,"Invalid target has no cost or follow-ups");
        state.BreakPart("part-0",1000);outcome=BattleActionResolver.Resolve(state,hero.Id,skill,"part-0");
        Check(!outcome.Accepted && hero.HitPoints==hp && hero.JobResource==3,"Broken target has no follow-ups");
        state=EffectState();hero=state.Heroes[0];hero.TakeDamage(3);
        outcome=BattleActionResolver.Resolve(state,hero.Id,new BattleSkill("test.clamp",1,0,70),"part-0");
        Check(outcome.PartBroken && outcome.SelfHealing==3 && hero.HitPoints==100,"Self healing clamps and still applies after part break");
        outcome=BattleActionResolver.Resolve(state,hero.Id,new BattleSkill("test.full",1,0,70),"body");
        Check(outcome.Accepted && outcome.SelfHealing==0,"Full HP does not invalidate an attack or claim healing");
        state=EffectState();hero=state.Heroes[0];hero.TakeDamage(95);state.ApplyBossDamage(999);
        outcome=BattleActionResolver.Resolve(state,hero.Id,new BattleSkill("test.last-hit",1,0,0,10),"body");
        Check(outcome.Victory && outcome.Damage==1 && outcome.SelfDamage==5 && !hero.IsAlive,"Killing blow finishes recoil; report actual HP loss, not nominal damage");
        outcome=BattleActionResolver.Resolve(state,hero.Id,new BattleSkill("test.ended",1,0,70),"body");
        Check(!outcome.Accepted && hero.HitPoints==0 && outcome.SelfHealing==0,"No resurrection or follow-ups after battle ends");
        state=EffectState();hero=state.Heroes[0];hero.TakeDamage(100);
        outcome=BattleActionResolver.Resolve(state,hero.Id,new BattleSkill("test.dead",1,0,70),"body");
        Check(!outcome.Accepted && hero.HitPoints==0 && state.BossHitPoints==1000,"Dead actor cannot attack or heal itself");
        hero=new BattleHero("large",int.MaxValue,int.MaxValue,10);hero.TakeDamage(100);hero.Heal(int.MaxValue);
        Check(hero.HitPoints==int.MaxValue,"Healing addition cannot wrap to negative HP");
        var catalog=fresh();catalog.Skill("hero-0",0).selfHealingBaseAttackPercent=-1;ExpectCombatFailure(catalog,"Negative self healing rejected");
        catalog=fresh();catalog.Skill("hero-0",0).selfDamageMaxHpPercent=101;ExpectCombatFailure(catalog,"Over-maximum recoil rejected");
        catalog=fresh();catalog.Skill("hero-0",2).selfDamageMaxHpPercent=10;ExpectCombatFailure(catalog,"Heal-only skill cannot carry attack follow-ups");
        catalog=fresh();catalog.Skill("hero-0",0).selfHealingBaseAttackPercent=70;catalog.Skill("hero-0",0).chainEligible=false;
        var battle=new PlayableBattle(1,new PlayableProgress(),7,combatDefinitions:catalog);int actor=battle.AvailableHero;
        battle.State.Heroes[actor].TakeDamage(30);battle.DrainPresentationEvents();
        int expectedHeal=battle.State.Heroes[actor].BaseAttack*70/100;
        catalog.Skill("hero-0",0).selfHealingBaseAttackPercent=1000;
        Check(battle.Act(actor,0,"body"),"JSON follow-up attack executes");
        var events=battle.DrainPresentationEvents();
        Check(events[0].Kind==BattlePresentationKind.Attack && events[1].Kind==BattlePresentationKind.Healing && events[1].HealingTargets.SequenceEqual(new[]{actor}) && events[1].Message.EndsWith(expectedHeal.ToString()),"Snapshot fields and actual self-healing targets reach ordered events");
        Check(events[0].Clock==events[1].Clock && battle.LastHealingTargets.SequenceEqual(new[]{actor}),"Attack follow-up is one timeline command");
        catalog=fresh();catalog.Skill("hero-0",0).selfDamageMaxHpPercent=10;
        battle=new PlayableBattle(1,new PlayableProgress(),7,combatDefinitions:catalog);actor=battle.AvailableHero;
        battle.State.Heroes[actor].TakeDamage(battle.State.Heroes[actor].HitPoints-1);battle.DrainPresentationEvents();
        Check(battle.Act(actor,0,"body") && !battle.State.Heroes[actor].IsAlive && battle.LastChainChecks.Count==0,"Lethal recoil stops chain initiation without RNG draws");
        events=battle.DrainPresentationEvents();
        Check(events.Any(e=>e.Kind==BattlePresentationKind.Support && e.TargetIds.SequenceEqual(new[]{"hero-0"})) && !events.Any(e=>e.Message.StartsWith("自動チェイン")),"Recoil event identifies affected actor without inventing another attack");
        catalog=fresh();catalog.Skill("hero-4",1).selfHealingBaseAttackPercent=70;catalog.Skill("hero-4",1).chainEligible=false;
        battle=new PlayableBattle(1,new PlayableProgress(),7,combatDefinitions:catalog);
        while(battle.AvailableHero!=4 && !battle.Ended) battle.Pass();
        battle.State.Heroes[4].TakeDamage(30);battle.DrainPresentationEvents();hp=battle.State.Heroes[4].HitPoints;
        int resource=battle.State.Heroes[4].JobResource;
        Check(battle.Act(4,1,"body") && battle.IsCasting(4) && battle.State.Heroes[4].HitPoints==hp,"Casting start does not heal early");
        Check(battle.State.Heroes[4].JobResource==resource-3,"Casting reserves cost only once");
        var allEvents=new List<BattlePresentationEvent>(battle.DrainPresentationEvents());
        while(battle.IsCasting(4) && !battle.Ended) {battle.Pass();allEvents.AddRange(battle.DrainPresentationEvents());}
        int release=allEvents.FindIndex(e=>e.Kind==BattlePresentationKind.CastRelease);
        Check(release>=0 && allEvents[release+1].Kind==BattlePresentationKind.Healing && allEvents[release+1].Actor==4 && allEvents[release].Clock==allEvents[release+1].Clock,"Deferred healing follows cast release at the same logical time");
        battle=new PlayableBattle(1,new PlayableProgress(),7,combatDefinitions:catalog);
        while(battle.AvailableHero!=4 && !battle.Ended) battle.Pass();
        battle.State.Heroes[4].TakeDamage(30);battle.DrainPresentationEvents();string partId=battle.State.Parts[3].Id;
        Check(battle.Act(4,1,partId),"Deferred self-healing reserves a valid part target");
        battle.State.BreakPart(partId,10000);allEvents=new List<BattlePresentationEvent>(battle.DrainPresentationEvents());
        while(battle.IsCasting(4) && !battle.Ended) {battle.Pass();allEvents.AddRange(battle.DrainPresentationEvents());}
        Check(allEvents.Any(e=>e.Kind==BattlePresentationKind.CastCanceled) && !allEvents.Any(e=>e.Kind==BattlePresentationKind.Healing && e.Actor==4),"Canceled spell has no self-healing follow-up");
        for(int seed=1;seed<=100;seed++) {
            var unchanged=fresh();var changed=fresh();changed.Skill("hero-0",0).selfHealingBaseAttackPercent=70;
            var a=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:unchanged);
            var b=new PlayableBattle(10,new PlayableProgress(),seed,combatDefinitions:changed);
            a.State.Heroes[0].TakeDamage(30);b.State.Heroes[0].TakeDamage(30);
            a.Act(0,0,"body");b.Act(0,0,"body");
            Check(a.Clock==b.Clock && a.State.Heroes.Select(h=>h.JobResource).SequenceEqual(b.State.Heroes.Select(h=>h.JobResource)),"Self healing does not add waits or costs seed="+seed);
            Check(a.LastChainChecks.Select(c=>(c.Candidate,c.Roll,c.ProbabilityBp)).SequenceEqual(b.LastChainChecks.Select(c=>(c.Candidate,c.Roll,c.ProbabilityBp))),"Nonlethal self healing adds no RNG draw seed="+seed);
        }
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
