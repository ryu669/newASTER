using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

// Deterministic acceptance of the current authored roster. Not human timing data.
public static class Plan1110FinalBalanceMeasurements
{
    static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
    static CombatDefinitionCatalog Combat;static CollectionCatalog Collection;static HomeExperienceCatalog Home;
    static readonly Dictionary<string,int> Utilities=new Dictionary<string,int>();
    static readonly string[] StressEnemies={"colossus.red-crystal-tyrant","colossus.final-flame-ice-phoenix","colossus.newborn-asteria"};
    sealed class Result
    {
        public string profile,candidate,enemy,policy,oopart;public int level,duplicateRank,route,rollPercent,seeds,wins,caps,deaths,enemyScaling;public long commands,clock,winningClock,enemyActions;public bool accepted;
    }
    public static void Main(string[] args)
    {
        Combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(args[0]),Json);Combat.Validate();
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json"),Json);
        Collection=ProductionStoryCatalog.Collection(Combat,story);Home=ProductionStoryCatalog.Home(Combat,story);
        if(args[5]=="TeamFollowup")TeamFollowup(args[1]);else if(args[5]=="Teams")Teams(args[1]);else if(args[5]=="Journey")Journey(args[1]);else if(args[5]=="FinishJourney")new EarnedJourney(args[1],true).FinishRing();else if(args[5]=="Audit")Audit(args[1]);else Matrix(args[1]);
    }
    static string[] Formation(string candidate)
    {
        var f=new List<string>{candidate};foreach(string id in new[]{"heroine.echidna","heroine.undermine","heroine.slayer","heroine.iconoclast","heroine.excalipan"})if(f.Count<5 && f.All(h=>Combat.PersonId(h)!=Combat.PersonId(id)))f.Add(id);return f.ToArray();
    }
    static FormalGrowthSave Growth(int level,int rank,int skill)=>new FormalGrowthSave{saveId="final-balance",heroines=Combat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=level,awakeningStage=level>80?2:level>50?1:0,duplicateRank=rank,skillLevels=new[]{skill,skill,skill}}).ToArray()};
    sealed class TeamResult
    {
        public string[] formation,jobs;public string enemy;public int level,skill,seeds,wins,caps,deaths,weaponTier,seedStart;public long clock,winningClock;
    }
    static TeamResult Team(string[] ids,string enemy,int level,int skill,int seeds,int weaponTier=0,int seedStart=0)
    {
        var r=new TeamResult{formation=ids,jobs=ids.Select(h=>Combat.Hero(h).jobId).ToArray(),enemy=enemy,level=level,skill=skill,seeds=seeds,weaponTier=weaponTier,seedStart=seedStart};var selected=Combat.WithFormation(ids);
        FormalHomeProgress home=null;if(weaponTier>0){home=FormalHomeProgress.Empty(Home.contentVersion);home.formationIds=ids;home.weaponNodeIds=Home.weaponNodes.Where(n=>ids.Contains(n.heroineId) && (n.initial || WeaponGrowthRules.Route(n)==0 && WeaponGrowthRules.Tier(n)<=weaponTier)).Select(n=>n.id).ToArray();home.weaponEquipment=ids.Select(id=>new HomeWeaponEquipment{heroineId=id,nodeId=Home.weaponNodes.Single(n=>n.heroineId==id && WeaponGrowthRules.Route(n)==0 && WeaponGrowthRules.Tier(n)==weaponTier).id}).ToArray();}
        for(int seed=seedStart;seed<seedStart+seeds;seed++){
            var b=new PlayableBattle(50,new PlayableProgress(),seed,combatDefinitions:selected,formalGrowth:Growth(level,0,skill),colossusDefinition:ProductionEconomyCatalog.Enemy(enemy),useJobRulesV2:true,homeProgress:home,homeCatalog:home==null?null:Home);
            var policy=new Policy(true);int steps=0;while(!b.Ended && steps++<4000){policy.Command(b);b.DrainPresentationEvents();}
            if(b.State.IsVictory){r.wins++;r.winningClock+=b.Clock;}if(!b.Ended)r.caps++;r.clock+=b.Clock;r.deaths+=b.State.Heroes.Count(h=>!h.IsAlive);
        }return r;
    }
    static void TeamFollowup(string input)
    {
        using(var doc=JsonDocument.Parse(File.ReadAllText(input))){if(!doc.RootElement.GetProperty("complete").GetBoolean())throw new Exception("Finish base team measurement first.");
            var baseline=JsonSerializer.Deserialize<List<TeamResult>>(doc.RootElement.GetProperty("rows").GetRawText(),Json).Where(r=>r.level==50 && r.enemy==StressEnemies[2]).ToArray();
            var failed=baseline.Where(r=>r.wins==0).ToArray();var rows=new List<TeamResult>();foreach(var row in failed)foreach(int tier in new[]{1,2})rows.Add(Team(row.formation,row.enemy,50,7,4,tier));
            var initial=new HashSet<string>(Combat.FormationIds);var best=baseline.Where(r=>r.wins==r.seeds && !r.formation.Any(initial.Contains)).OrderBy(r=>r.winningClock).First();
            foreach(string enemy in WorldCatalog.ColossusIds)rows.Add(Team(best.formation,enemy,50,7,8,seedStart:4));
            File.WriteAllText(input+".followup.json",JsonSerializer.Serialize(new{schemaVersion=1,complete=true,runs=rows.Sum(r=>r.seeds),caps=rows.Sum(r=>r.caps),failedParties=failed.Length,scope="Paired retry of zero-win parties with all-five attack-route tier1/2 weapons Lv1, same four seeds; synthetic gear comparison, not earned acquisition. No ooparts or duplicates. Separately, fastest no-initial-party that won4/4 faces all15 enemies on eight held-out seeds4..11 without gear. Selected winner is not a universal recommendation.",holdoutFormation=best.formation,rows},Json));if(rows.Any(r=>r.caps>0))throw new Exception("Followup capped.");}
    }
    static void Teams(string output)
    {
        var ids=Combat.HeroineIds;var parties=new List<string[]>();
        for(int a=0;a<ids.Length-4;a++)for(int b=a+1;b<ids.Length-3;b++)for(int c=b+1;c<ids.Length-2;c++)for(int d=c+1;d<ids.Length-1;d++)for(int e=d+1;e<ids.Length;e++){
            var party=new[]{ids[a],ids[b],ids[c],ids[d],ids[e]};if(party.Select(Combat.PersonId).Distinct().Count()==5)parties.Add(party);
        }
        var rows=new List<TeamResult>();int index=0;
        foreach(var party in parties){rows.Add(Team(party,StressEnemies[2],50,7,4));if(++index%100==0){WriteTeams(output,rows,false,parties.Count);Console.WriteLine("TEAMS "+index+"/"+parties.Count);}}
        // A fixed, spread-out sample includes all jobs plus strongest and weakest observed
        // parties. It checks other enemies and a lower investment without replaying the
        // already measured final-enemy Lv50 profile.
        var sample=parties.Where((p,i)=>i%(Math.Max(1,parties.Count/24))==0).Take(24).Concat(rows.OrderBy(r=>r.wins).ThenByDescending(r=>r.clock).Take(8).Select(r=>r.formation)).Concat(rows.OrderByDescending(r=>r.wins).ThenBy(r=>r.winningClock).Take(8).Select(r=>r.formation)).GroupBy(p=>string.Join(",",p)).Select(g=>g.First()).ToArray();
        foreach(var party in sample){foreach(string enemy in WorldCatalog.ColossusIds.Where(id=>id!=StressEnemies[2]))rows.Add(Team(party,enemy,50,7,4));foreach(string enemy in StressEnemies)rows.Add(Team(party,enemy,30,4,4));}
        WriteTeams(output,rows,true,parties.Count);if(rows.Any(r=>r.caps>0))throw new Exception("Team test operation cap reached.");
    }
    static void WriteTeams(string output,List<TeamResult> rows,bool complete,int legalParties)
    {
        File.WriteAllText(output,JsonSerializer.Serialize(new{schemaVersion=1,complete,legalParties,runs=rows.Sum(r=>r.seeds),caps=rows.Sum(r=>r.caps),scope="All legal five-person combinations, canonical catalog order, final enemy Lv50, heroes Lv50 skill7 duplicate0 no gear, four fixed seeds. Sampled parties additionally face all other enemies and Lv30 skill4 stress. Uses existing job-support policy, not optimal play or all formation permutations. No win gate imposed on specialized parties; losses are evidence for analysis.",rows},Json));
    }
    static void Matrix(string output)
    {
        var rows=new List<Result>();
        foreach(string id in Combat.HeroineIds)
        {
            var selected=Combat.WithFormation(Formation(id));
            foreach(bool advanced in new[]{false,true})
            {
                rows.Add(Measure("starter",selected,1,0,1,WorldCatalog.ColossusIds[0],1,advanced,8,-1,null,20,false));
                rows.Add(Measure("underlevel",selected,1,0,1,StressEnemies[2],50,advanced,8,-1,null,20,false));
                foreach(string enemy in WorldCatalog.ColossusIds)rows.Add(Measure(advanced?"trained":"trained-diagnostic",selected,50,0,7,enemy,50,advanced,4,-1,null,20,false));
                foreach(string enemy in StressEnemies){rows.Add(Measure("support-stress",selected,10,0,1,enemy,50,advanced,8,-1,null,20,false));rows.Add(Measure("unskilled-diagnostic",selected,50,0,1,enemy,50,advanced,4,-1,null,20,false));}
            }
            foreach(int route in new[]{0,1,2})foreach(var oopart in Collection.oopartDefs)foreach(int roll in new[]{20,100})
                rows.Add(Measure("max-slot",selected,50,0,7,StressEnemies[2],50,true,2,route,oopart.id,roll,false));
            foreach(int route in new[]{0,1,2})foreach(int rank in new[]{0,5})foreach(bool advanced in new[]{false,true})foreach(string enemy in WorldCatalog.ColossusIds)
                rows.Add(Measure("max-party",selected,120,rank,7,enemy,50,advanced,2,route,"affinity",100,true));
            Console.WriteLine("FINAL_MATRIX "+id+" runs="+rows.Sum(r=>r.seeds)+" failures="+rows.Count(r=>!r.accepted));
            WriteMatrix(output,rows);
        }
        if(rows.Any(r=>!r.accepted))throw new Exception("Final balance acceptance failed; inspect "+output);
    }
    static void WriteMatrix(string output,List<Result> rows)
    {
        var summary=rows.GroupBy(r=>r.profile).Select(g=>new{profile=g.Key,runs=g.Sum(r=>r.seeds),wins=g.Sum(r=>r.wins),caps=g.Sum(r=>r.caps),failures=g.Count(r=>!r.accepted)}).ToArray();
        File.WriteAllText(output,JsonSerializer.Serialize(new{schemaVersion=1,runs=rows.Sum(r=>r.seeds),failures=rows.Count(r=>!r.accepted),criteria="starter >=7/8; trained Lv50 skill7 with job-support policy >=3/4; max-slot and max-party all wins; underlevel <=2/8; diagnostic and support-stress profiles have no victory minimum; zero 4000-operation caps everywhere",scope="Current15 forms, all15 enemies, three weapon routes, all42 ooparts. Max-slot crosses all42 with three routes and20/100% rolls. Max-party uses distinct slot equipment, all15 enemies, duplicate0/5, both command policies. Damage/heal-only Lv50 and skill1 are diagnostic: ignoring songs/volley is not a required winning strategy. Synthetic gear fixtures are separate from earned journey.",summary,utilities=Utilities,rows},Json));
    }
    static void Audit(string output)
    {
        // Reclassify the recorded damage-only control group without replaying encounters.
        // Keep the raw file alongside the final report so its losses remain visible.
        string raw=output+".raw.json";if(!File.Exists(raw))File.Copy(output,raw);
        using(var doc=JsonDocument.Parse(File.ReadAllText(raw))){var root=doc.RootElement;var rows=JsonSerializer.Deserialize<List<Result>>(root.GetProperty("rows").GetRawText(),Json);foreach(var r in rows.Where(r=>r.profile=="trained" && r.policy=="damage-heal")){r.profile="trained-diagnostic";r.accepted=r.caps==0;}foreach(var entry in root.GetProperty("utilities").EnumerateObject())Utilities[entry.Name]=entry.Value.GetInt32();
            if(rows.Sum(r=>r.seeds)!=16320 || Combat.HeroineIds.Any(h=>rows.Where(r=>r.candidate==h).Sum(r=>r.seeds)!=1088))throw new Exception("Incomplete final matrix.");
            WriteMatrix(output,rows);if(rows.Any(r=>!r.accepted))throw new Exception("Final matrix failed accepted job-aware criteria.");}
    }
    static Result Measure(string profile,CombatDefinitionCatalog selected,int level,int rank,int skill,string enemy,int enemyLevel,bool advanced,int seeds,int route,string oopart,int roll,bool party)
    {
        var r=new Result{profile=profile,candidate=selected.FormationIds[0],enemy=enemy,level=level,duplicateRank=rank,route=route,oopart=oopart,rollPercent=roll,policy=advanced?"job-support":"damage-heal",seeds=seeds,enemyScaling=0};
        for(int seed=0;seed<seeds;seed++)
        {
            var progress=FormalHomeProgress.Empty(Home.contentVersion);progress.formationIds=selected.FormationIds;
            var ledger=new FormalCollectionLedger{contentVersion=Collection.contentVersion,ooparts=new OopartInventorySave()};ledger.ooparts.SyncFormation(selected.FormationIds);
            var used=new HashSet<string>();
            for(int i=0;i<(party?5:1);i++)
            {
                string hero=selected.FormationIds[i];
                if(route>=0){var node=Home.weaponNodes.Single(n=>n.heroineId==hero && WeaponGrowthRules.Route(n)==route && WeaponGrowthRules.Tier(n)==4);progress.weaponNodeIds=progress.weaponNodeIds.Concat(Home.weaponNodes.Where(n=>n.heroineId==hero).Select(n=>n.id)).ToArray();progress.weaponEquipment=progress.weaponEquipment.Concat(new[]{new HomeWeaponEquipment{heroineId=hero,nodeId=node.id}}).ToArray();progress.weaponLevels=progress.weaponLevels.Concat(new[]{new HomeWeaponLevel{nodeId=node.id,level=7}}).ToArray();}
                if(oopart==null)continue;
                string relic=party?new[]{"relic."+Combat.Hero(hero).jobId+".affinity","relic.special.attack","relic.special.speed","relic.special.ramp","relic.special.defense"}.First(x=>!used.Contains(x)):oopart;used.Add(relic);
                var d=Collection.Oopart(relic);var stats=new StatValues();foreach(var stat in d.randomStats)stats.Set(stat.stat,(stat.maximum*roll+99)/100);
                ledger.ooparts.progress=ledger.ooparts.progress.Concat(new[]{new OopartProgress{oopartId=relic,level=120,accumulatedRandomStats=stats}}).ToArray();OopartService.Equip(ledger.ooparts,i,relic);
            }
            var b=new PlayableBattle(enemyLevel,new PlayableProgress(),seed,combatDefinitions:selected,formalGrowth:Growth(level,rank,skill),colossusDefinition:ProductionEconomyCatalog.Enemy(enemy),useJobRulesV2:true,homeProgress:progress,homeCatalog:Home,collectionGrowth:ledger,relicCatalog:Collection);
            var policy=new Policy(advanced);int steps=0;while(!b.Ended && steps++<4000){policy.Command(b);b.DrainPresentationEvents();}
            if(b.State.IsVictory){r.wins++;r.winningClock+=b.Clock;}if(!b.Ended)r.caps++;r.enemyActions+=b.EnemyActionCount;r.commands+=steps;r.clock+=b.Clock;r.deaths+=b.State.Heroes.Count(h=>!h.IsAlive);
        }
        r.accepted=r.caps==0 && (profile=="starter"?r.wins>=7:profile=="underlevel"?r.wins<=2:profile=="trained"?r.wins>=3:profile.StartsWith("max-",StringComparison.Ordinal)?r.wins==seeds:true);return r;
    }
    static void Journey(string output)
    {
        var run=new EarnedJourney(output);run.Run();
    }
    sealed class EarnedJourney
    {
        readonly string output;FormalCampaignJournal journal;int battles,wins,losses,serial;long commands,clock;readonly List<object> stages=new List<object>();
        string Id()=>"earned."+(++serial);
        string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,Json);
        FormalCampaignSave Decode(string s)=>JsonSerializer.Deserialize<FormalCampaignSave>(s,Json);
        public EarnedJourney(string path,bool resume=false)
        {
            output=path;if(resume){using(var report=JsonDocument.Parse(File.ReadAllText(path))){var root=report.RootElement;if(!root.GetProperty("complete").GetBoolean())throw new Exception("Resume requires a completed earned path.");battles=root.GetProperty("battles").GetInt32();wins=root.GetProperty("wins").GetInt32();losses=root.GetProperty("losses").GetInt32();commands=root.GetProperty("commands").GetInt64();clock=root.GetProperty("clock").GetInt64();foreach(var stage in root.GetProperty("stages").EnumerateArray())stages.Add(stage.Clone());}var restored=Decode(File.ReadAllText(path+".save.json"));journal=new FormalCampaignJournal(restored,Encode,Decode);serial=restored.growth.receipts.Select(r=>r.transactionId).Concat(restored.home.receipts.Select(r=>r.transactionId)).Concat(restored.affection.receipts.Select(r=>r.id)).Where(id=>id.StartsWith("earned.",StringComparison.Ordinal)).Select(id=>int.Parse(id.Substring(7))).Max();return;}
            var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=2940,awakeningCrystals=20,heroines=Combat.FormationIds.Select(h=>new FormalHeroineGrowth{heroineId=h}).ToArray()},home=FormalHomeProgress.Empty(Home.contentVersion),collection=new FormalCollectionLedger{contentVersion=Collection.contentVersion}};
            save.home.formationIds=Combat.FormationIds;WeaponGrowthRules.EnsureRoots(save,Home);AffectionSaveAdapter.Migrate(save,Home);journal=new FormalCampaignJournal(save,Encode,Decode);
        }
        void Done(GrowthCommitResult result){if(result!=GrowthCommitResult.Committed)throw new Exception("Earned operation did not commit: "+result);}
        void Mark(string stage)
        {
            var s=journal.Snapshot;stages.Add(new{stage,battles,wins,losses,nectar=s.growth.nectar,crystals=s.growth.awakeningCrystals,stones=s.growth.stones,heroes=s.growth.heroines.Select(h=>new{h.heroineId,h.level,h.duplicateRank,h.skillLevels}).ToArray()});
            s.Validate();File.WriteAllText(output,JsonSerializer.Serialize(new{schemaVersion=1,complete=stage=="complete",scope="One deterministic earned campaign, seed 17 plus encounter index. Initial five, initial 2940 nectar and20 crystals; no injected rewards, levels, duplicates, gear or terraforming. Battles and all purchases use production transactions. No login/idle/gacha income. Counts include overlapping material income.",battles,wins,losses,commands,clock,stages,finalSave=stage=="complete"?s:null},Json));
            File.WriteAllText(output+".save.json",Encode(s));journal=new FormalCampaignJournal(Decode(File.ReadAllText(output+".save.json")),Encode,Decode);Console.WriteLine("EARNED "+stage+" battles="+battles);
        }
        bool Battle(string enemy,int level,bool hunt=false)
        {
            if(++battles>1800)throw new Exception("Earned journey exceeded1800 encounters.");var s=journal.Snapshot;var definition=ProductionEconomyCatalog.Enemy(enemy);TerraformRules.RequireIntegration(s.world,enemy);
            var selected=Combat.WithFormation(s.home.formationIds);int seed=17+battles;
            var session=new BattleCollectionSession(Collection,Id(),enemy,level,s.revision,selected.FormationIds,seed,definition.contentVersion,hunt);
            var b=new PlayableBattle(level,new PlayableProgress(),seed,combatDefinitions:selected,formalGrowth:s.growth,colossusDefinition:definition,useJobRulesV2:true,homeProgress:s.home,homeCatalog:Home,collectionGrowth:s.collection,relicCatalog:Collection);
            var poems=Collection.owners.Single(o=>o.id==enemy).poemIds;var rng=new Random(seed^0x534F4E47);b.CompletedEnemyAction=()=>session.RecordCompletedSinging(poems[rng.Next(poems.Length)]);
            int steps=0;var policy=new Policy(true);while(!b.Ended && steps++<4000){policy.Command(b);b.DrainPresentationEvents();}if(!b.Ended)throw new Exception("Earned encounter capped.");commands+=steps;clock+=b.Clock;if(b.State.IsVictory)wins++;else losses++;
            var receipt=session.Finish(b.State.IsVictory?BattleEndReason.Victory:BattleEndReason.Defeat,s.world.poemIds,s.world.unlockedStoryIds);var request=new FormalBattleEndRequest(receipt,s.revision);
            Func<CampaignSaveV2,CampaignSaveV2> victory=w=>{var state=new CampaignState(WorldCatalog.ColossusIds,w);state.ClaimColossusVictory(enemy,WorldCatalog.Colossi.Single(c=>c.Id==enemy).EnvironmentTags,new VictoryReward(request.Id,level,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return state.CreateSave();};
            Done(journal.CommitBattleEnd(request,Collection,victory,p=>true,Home));if(battles%25==0)Mark("checkpoint");return b.State.IsVictory;
        }
        void Farm(string enemy=null)
        {
            int level=Math.Min(50,journal.Snapshot.growth.heroines.Min(h=>h.level));if(!Battle(enemy??WorldCatalog.ColossusIds[0],level,enemy!=null))throw new Exception("Sustainable farm defeated at "+enemy+" Lv"+level);
        }
        void Growth(string hero,GrowthOperation op,int target=0,int slot=-1)
        {
            while(true){var g=new FormalProgression(journal.Snapshot.growth,Combat.HeroineIds);var r=new GrowthRequest(Id(),hero,g.Snapshot.revision,op,target,skillSlot:slot);try{g.Preview(r);}catch(ArgumentException){Farm();continue;}Done(g.Commit(r,s=>journal.CommitGrowth(s,p=>true,Home)));break;}
        }
        void Materials(HomeCost[] costs)
        {
            foreach(var c in costs)while(HomeRules.Balance(journal.Snapshot,c.resourceId)<c.amount)Farm(Home.materials.Single(m=>m.id==c.resourceId).colossusId);
        }
        void HomeOp(HomeOperation op)
        {
            Done(journal.CommitHomeOperation(new FormalHomeRequest(Id(),op.Kind=="equip"?"weapon":op.Kind,journal.Revision,Home.contentVersion,op.Key),Home,op,p=>true));
        }
        public void Run()
        {
            foreach(string hero in Combat.FormationIds)Growth(hero,GrowthOperation.Level,10);Mark("intro-level10");
            foreach(string enemy in WorldCatalog.ColossusIds.Take(14))if(!Battle(enemy,1))throw new Exception("First clear failed: "+enemy);Mark("fourteen-first-clears");
            for(int target=20;target<=50;target+=10)foreach(string hero in Combat.FormationIds)Growth(hero,GrowthOperation.Level,target);Mark("party-level50");
            foreach(string enemy in WorldCatalog.ColossusIds.Take(14))if(!Battle(enemy,20))throw new Exception("Tier3 clear failed: "+enemy);
            foreach(string domain in TerraformRules.DomainIds)for(int level=1;level<=3;level++)Done(journal.CommitTerraform(new FormalTerraformRequest(Id(),journal.Revision,TerraformOperation.SetLevel,domain,level),Collection,p=>true));
            if(!Battle(WorldCatalog.ColossusIds[14],1))throw new Exception("Integration failed.");Mark("integration-unlocked");
            foreach(string hero in Combat.FormationIds){Growth(hero,GrowthOperation.Awaken);Growth(hero,GrowthOperation.Level,80);Growth(hero,GrowthOperation.Awaken);Growth(hero,GrowthOperation.Level,120);for(int slot=0;slot<3;slot++)for(int lv=2;lv<=7;lv++)Growth(hero,GrowthOperation.Skill,lv,slot);}Mark("party-level120-skill7");
            foreach(string hero in Combat.FormationIds)
            {
                var nodes=Home.weaponNodes.Where(n=>n.heroineId==hero && !n.initial && WeaponGrowthRules.Route(n)==0).OrderBy(n=>WeaponGrowthRules.Tier(n)).ToArray();
                foreach(var node in nodes){Materials(node.costs);HomeOp(new HomeOperation("weapon",node.id));}var end=nodes.Last();for(int lv=2;lv<=7;lv++){Materials(WeaponGrowthRules.Costs(end,lv-1,Home));HomeOp(new HomeOperation("weapon-level",end.id,owner:lv.ToString()));}HomeOp(new HomeOperation("equip",end.id,owner:hero));
            }Mark("five-final-weapons-level7");
            for(int slot=0;slot<5;slot++)
            {
                string relic="relic."+Combat.Hero(Combat.FormationIds[slot]).jobId+".affinity";var def=Collection.relics.Single(r=>r.id==relic);string enemy=Collection.rewardBands.First(b=>b.minLevel<=50 && b.maxLevel>=50 && b.relicIds.Contains(relic)).ownerId;int attempts=0;
                while(!journal.Snapshot.collection.relics.Any(r=>r.id==relic)){if(++attempts>200)throw new Exception("Target hunt exceeded200.");if(!Battle(enemy,50,true))throw new Exception("Hunt defeated.");}
                Done(journal.CommitOopart(new OopartRequest(Id(),journal.Revision,"equip",relic,slot:slot),Collection,Combat.FormationIds,p=>true,Home));
                Materials(def.materialIds.Select(id=>new HomeCost{resourceId=id,amount=7140}).ToArray());Done(journal.CommitOopart(new OopartRequest(Id(),journal.Revision,"level",relic,count:120),Collection,Combat.FormationIds,p=>true,Home));
                if(journal.Snapshot.collection.ooparts.progress.Single(p=>p.oopartId==relic).level!=120)throw new Exception("Earned oopart did not reach120.");
            }Mark("five-ooparts-level120");
            FinishRing();
        }
        public void FinishRing()
        {
            if(journal.Snapshot.growth.eternalRings==0){while(journal.Snapshot.growth.stones<10000)Farm();Done(journal.CommitAffection(new AffectionRequest(Id(),journal.Revision,"ring-purchase",Array.Empty<string>(),"ring"),Home,p=>true));Mark("earned-ring-purchase");}
            while(AffectionService.State(journal.Snapshot,Home,Combat.FormationIds[0]).level<20)Farm();Done(journal.CommitAffection(new AffectionRequest(Id(),journal.Revision,"ring-use",new[]{Combat.FormationIds[0]},"ring"),Home,p=>true));Mark("earned-ring-use");
            foreach(string enemy in WorldCatalog.ColossusIds)if(!Battle(enemy,50))throw new Exception("Earned final sweep defeated: "+enemy);Mark("complete");
        }
    }
    static void Used(string key){Utilities[key]=Utilities.TryGetValue(key,out int n)?n+1:1;}
    sealed class Policy
    {
        readonly bool advanced;readonly long[] buffAt={-1000,-1000,-1000,-1000,-1000};
        public Policy(bool advanced){this.advanced=advanced;}
        public void Command(PlayableBattle b)
        {
            if(!advanced){Plan1110BalanceMeasurements.Command(b);return;}
            int a=b.AvailableHero;if(a<0){b.Pass();return;}var h=b.State.Heroes[a];var j=b.JobState(a);string target=b.State.Parts.FirstOrDefault(p=>!p.IsBroken)?.Id??"body";
            if(b.RequiresPanzerDefense(a)){if(b.DefendPanzer(a))Used("panzer-defense");return;}
            if(j.Id=="job.gambler"){if(b.SpinGamblerSlot(a,target))Used("gambler-slot");return;}
            if(j.Id=="job.general" && b.ActivateGeneralCommand(a))Used("general-command");
            if(j.Id=="job.chaser" && j.NitroCount>=5 && b.SelectChaserNitro(a))Used("chaser-nitro");
            if(j.Id=="job.artist"){
                if(j.Singing){if(b.ContinueSong(a))Used("artist-continue");return;}
                if(h.JobResource>=6 && b.StartSong(a)){Used("artist-start");return;}
            }
            if(j.Id=="job.panzer" && h.HitPoints<h.MaxHitPoints/2 && b.UsePanzerTool(a,0,a)){Used("panzer-repair");return;}
            if(j.Id=="job.healer"){
                int dead=Array.FindIndex(b.State.Heroes.ToArray(),x=>!x.IsAlive);
                if(dead>=0 && b.UseLifeTool(a,"revive",dead)){Used("healer-revive");return;}
            }
            if(j.Id=="job.alchemist" && h.JobResource>=3){int amount=Math.Min(15,h.JobResource);if(b.Transmute(a,new[]{amount,0,0,0,0},target)){Used("alchemy");if(b.Ended)return;target=b.State.Parts.FirstOrDefault(p=>!p.IsBroken)?.Id??"body";}}
            if(j.Id=="job.gunner" && b.FullVolley(a,target)){Used("gunner-volley");return;}
            if(j.Id=="job.blaster"){
                double best=-1;int cast=100,repeats=1;
                foreach(int c in new[]{0,50,100,200})foreach(int n in new[]{1,2,3})if(b.SelectBlaster(a,c,n))foreach(int slot in Enumerable.Range(0,3).Where(s=>b.IsAttackSkill(a,s) && b.ConditionsSatisfied(a,s))){double score=(double)b.PreviewDamage(a,slot,target)*n/Math.Max(1,b.CastDelay(a,slot)+b.CommandRecoveryDelay(a,slot));if(score>best){best=score;cast=c;repeats=n;}}
                if(b.SelectBlaster(a,cast,repeats))Used("blaster-cast-repeat");
            }
            if(b.Clock-buffAt[a]>=300 && !b.State.Heroes.Any(x=>x.IsAlive && x.HitPoints<x.MaxHitPoints/2))
                foreach(int slot in Enumerable.Range(0,3).Where(s=>b.IsSelfBuff(a,s) && b.ConditionsSatisfied(a,s) && b.SkillResourceCost(a,s)<=h.JobResource)){
                    int ally=Enumerable.Range(0,5).Where(i=>b.State.Heroes[i].IsAlive).OrderByDescending(i=>b.State.Heroes[i].Attack).First();
                    long at=b.Clock;if(b.Act(a,slot,target,ally)){buffAt[a]=at;Used(b.IsAllyBuff(a,slot)?"ally-buff":"self-buff");return;}
                }
            Plan1110BalanceMeasurements.Command(b);
        }
    }
}
