using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan5WorldRelicTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        var options=new JsonSerializerOptions {IncludeFields=true};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=t=>JsonSerializer.Deserialize<FormalCampaignSave>(t,options);
        Action<Action,string> reject=(f,name)=>{bool failed=false;try{f();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException){failed=true;}check(failed,name);};
        var catalog=CollectionContractFixture.Create(combat);var dragon=catalog.owners[0];var relicId=catalog.relics[0].id;
        Func<FormalCampaignSave> fresh=()=>new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()},collection=new FormalCollectionLedger()};
        check(WorldCatalog.Colossi.Count==15 && WorldCatalog.Colossi.Count(c=>ColossusCombatCatalog.CanSummon(c.Id))==1,"Fifteen pages but only authored encounter summonable");
        var worldContract=new CampaignState(WorldCatalog.ColossusIds);
        foreach(var colossus in WorldCatalog.Colossi) {
            check(worldContract.ColossusUnlocks.IsUnlocked(colossus.Id),"Sequential page and final integration prerequisites resolve");
            var reward=worldContract.ClaimColossusVictory(colossus.Id,colossus.EnvironmentTags,new VictoryReward("world.first."+colossus.Id,1,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            check(reward.FirstClear && colossus.EnvironmentTags.All(worldContract.Terraforming.EnvironmentTags.Contains),"Every first clear applies its distinct multiple environments");
            int environments=worldContract.Terraforming.EnvironmentTags.Count,gardens=worldContract.Gardens.UnlockedGardenIds.Count,materials=worldContract.Progress.Materials;
            var repeat=worldContract.ClaimColossusVictory(colossus.Id,colossus.EnvironmentTags,new VictoryReward("world.repeat."+colossus.Id,50,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            check(!repeat.FirstClear && repeat.NewEnvironmentTags.Count==0 && repeat.NewGardenIds.Count==0 && worldContract.Terraforming.EnvironmentTags.Count==environments && worldContract.Gardens.UnlockedGardenIds.Count==gardens && worldContract.Progress.Materials>materials,"Repeated high-level rewards never duplicate environment or garden unlocks");
        }
        check(GardenCatalog.Requirements.All(g=>worldContract.Gardens.UnlockedGardenIds.Contains(g.GardenId)),"All garden environment combinations including integrated world resolve");
        reject(()=>ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[1]),"Unmade enemy cannot reuse dragon encounter");
        foreach(int level in new[]{1,9,10,19,20,29,30,39,40,44,45,49,50}){
            var band=catalog.rewardBands.Single(x=>x.ownerId==dragon.id && level>=x.minLevel && level<=x.maxLevel);
            check(band.draws==Math.Min(5,1+level/10),"Explicit hunt level boundaries");
            check(band.terraforming==4+Math.Min(4,level/10),"Higher summon levels never decrease world regeneration");
            var battle=new PlayableBattle(level,new PlayableProgress(),7,combatDefinitions:combat,colossusDefinition:ColossusCombatCatalog.Get(dragon.id));
            battle.State.AdvanceBossGauge(3);
            check(battle.NextAttackIsMajor && battle.NextEnemyAction.Contains(level>=45?"極大技":"大技"),"Level 44/45 ultimate preview boundary");
            check(battle.State.BreakPart("crystal-horn-crown",int.MaxValue) && battle.State.BossGauge==2 && !battle.NextAttackIsMajor,"Gauge part destruction interrupts telegraphed major");
            check(!battle.State.BreakPart("crystal-horn-crown",int.MaxValue) && battle.State.BossGauge==2,"Part break effect fires once");
            check(battle.NextEnemyAction.Contains("翼撃"),"Interrupted major uses ordinary alternative action");
        }
        var six=ColossusCombatCatalog.Get(dragon.id);six.parts=six.parts.Concat(new[]{new ColossusPartCombatDef {id="aux.one",role="auxiliary",breakEffect="",baseHp=200},new ColossusPartCombatDef {id="aux.two",role="auxiliary",breakEffect="",baseHp=200}}).ToArray();six.Validate();
        var sixBattle=new PlayableBattle(1,new PlayableProgress(),4,combatDefinitions:combat,colossusDefinition:six);
        check(sixBattle.State.Parts.Count==6,"Six part combat state accepts explicit definition");
        var reordered=ColossusCombatCatalog.Get(dragon.id);
        reordered.parts=six.parts.Where(p=>p.role!="armor").Concat(six.parts.Where(p=>p.role=="armor")).ToArray();
        var reorderedBattle=new PlayableBattle(1,new PlayableProgress(),4,combatDefinitions:combat,colossusDefinition:reordered);
        var protectedSkill=new BattleSkill("protection-check",1m,0,bodyPartProtection:true);
        var unprotectedSkill=new BattleSkill("unprotected-check",1m,0);
        var protectedHero=reorderedBattle.State.Heroes[0];
        check(ColossusCombatCatalog.PartName(reorderedBattle.State.Parts[5],5)=="右翼の装甲" && ColossusCombatCatalog.PartEffect(reorderedBattle.State.Parts[5])=="本体の軽減を解除","Reordered armor retains correct name and break explanation");
        check(ColossusCombatCatalog.PartEffect(reorderedBattle.State.Parts[4])=="追加効果なし","Auxiliary part never advertises armor break effect");
        int protectedDamage=BattleActionResolver.CalculateDamage(reorderedBattle.State,protectedHero,protectedSkill,"body");
        int fullDamage=BattleActionResolver.CalculateDamage(reorderedBattle.State,protectedHero,unprotectedSkill,"body");
        check(protectedDamage<fullDamage,"Reordered six part encounter retains armor protection");
        reorderedBattle.State.BreakPart(reordered.parts[2].id,int.MaxValue);
        check(BattleActionResolver.CalculateDamage(reorderedBattle.State,protectedHero,protectedSkill,"body")==protectedDamage,"Breaking third non-armor part preserves protection");
        reorderedBattle.State.BreakPart(reordered.parts.Single(p=>p.role=="armor").id,int.MaxValue);
        check(BattleActionResolver.CalculateDamage(reorderedBattle.State,protectedHero,protectedSkill,"body")==fullDamage,"Breaking actual sixth armor part removes protection");
        six.parts[0].baseHp=1;check(sixBattle.State.Parts[0].MaxHitPoints==312,"Encounter definition frozen independently of source mutation");
        six.parts=six.parts.Take(3).ToArray();reject(six.Validate,"Three part definition blocked");
        var invalid=ColossusCombatCatalog.Get(dragon.id);invalid.parts[0].role="attack";reject(invalid.Validate,"Duplicate battle role blocked");
        var weapon=catalog.Copy();weapon.weaponNodes[0].prerequisiteIds=new[]{weapon.weaponNodes[1].id};reject(weapon.Validate,"Weapon node cycles blocked");
        weapon=catalog.Copy();weapon.weaponNodes[0].materialIds=new[]{"unknown"};reject(weapon.Validate,"Heroine weapon source material references checked");
        var extended=CollectionContractFixture.Create(combat.FormationIds.Concat(new[]{"heroine.nonparticipant"}));var nonparticipant=extended.owners.Last();
        extended.Validate();
        var excluded=new BattleCollectionSession(extended,"party",dragon.id,1,0,combat.FormationIds);excluded.RecordCompletedSinging(dragon.poemIds[0]);
        check(!excluded.Finish(BattleEndReason.Defeat,Array.Empty<string>(),Array.Empty<string>()).acquiredPoemIds.Contains(nonparticipant.poemIds[0]),"Nonparticipant heroine excluded despite correspondence");
        bool foundEmpty=false,foundDrop=false;
        for(int seed=0;seed<50;seed++){
            var initial=fresh();var session=new BattleCollectionSession(catalog,"hunt."+seed,dragon.id,1,0,combat.FormationIds,seed);var req=new FormalBattleEndRequest(session.Finish(BattleEndReason.Victory,initial.world.poemIds,initial.world.unlockedStoryIds),0);
            var journal=new FormalCampaignJournal(initial,encode,decode);int builds=0;
            Func<CampaignSaveV2,CampaignSaveV2> world=w=>{builds++;var c=new CampaignState(WorldCatalog.ColossusIds,w);c.ClaimColossusVictory(dragon.id,WorldCatalog.Colossi[0].EnvironmentTags,new VictoryReward(req.Id,1,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return c.CreateSave();};
            FormalCampaignSave failed=null,saved=null;
            journal.CommitBattleEnd(req,catalog,world,s=>{failed=decode(encode(s));return false;});
            check(journal.Snapshot.collection.materials.Length==0 && journal.Snapshot.collection.relics.Length==0,"Failed hunt publishes neither resources nor relics");
            journal.CommitBattleEnd(req,null,null,s=>{saved=decode(encode(s));return true;});
            check(encode(failed)==encode(saved) && builds==1,"Save retry preserves every rolled outcome");
            var r=saved.collection.receipts[0];foundEmpty|=r.relicDrops.Length==0;foundDrop|=r.relicDrops.Length>0;
            check(r.relicDrawCount==1 && saved.collection.materials.Single().amount==11,"Victory source materials and draw count durable together");
            var reload=new FormalCampaignJournal(saved,encode,decode);check(reload.CommitBattleEnd(req,null,null,s=>false)==GrowthCommitResult.AlreadyCommitted,"Hunt receipt blocks replay after restart");
        }
        check(foundEmpty && foundDrop,"Fixture hunt can return both empty and acquired results");
        foreach(var rolls in new[]{new[]{79,799},new[]{80,800},new[]{99,990},new[]{100,1000}}){
            foreach(var op in new[]{RelicOperation.AttackUp,RelicOperation.HpUp}){
                var initial=fresh();initial.collection.materials=new[]{new CollectionMaterial {id=dragon.materialIds[0],sourceColossusId=dragon.id,amount=10000}};initial.collection.relics=new[]{new CollectionRelic {id=relicId,attackRoll=rolls[0],hpRoll=rolls[1]}};
                var state=new FormalCampaignJournal(initial,encode,decode);var req=new FormalRelicRequest("upgrade."+op,relicId,null,0,op);
                bool allowed=rolls[0]>=80 && rolls[0]<100;
                if(!allowed){reject(()=>state.CommitRelic(req,catalog,s=>true),"Per-item 80 percent and maximum boundary enforced");check(state.Snapshot.collection.materials[0].amount==10000,"Rejected direct upgrade never spends materials");}
                else{
                    check(state.CommitRelic(req,catalog,s=>false)==GrowthCommitResult.SaveFailed && encode(state.Snapshot)==encode(initial),"Direct upgrade failure is atomic");
                    state.CommitRelic(req,null,s=>true);
                    check(state.Snapshot.collection.materials[0].amount==9000,"Direct upgrade spends source material once");
                    check(state.Snapshot.collection.relics[0].attackRoll==(op==RelicOperation.AttackUp?Math.Min(100,rolls[0]+1):rolls[0]) && state.Snapshot.collection.relics[0].hpRoll==(op==RelicOperation.HpUp?Math.Min(1000,rolls[1]+10):rolls[1]),"Direct upgrade changes only selected stat");
                    check(state.CommitRelic(req,null,s=>throw new Exception())==GrowthCommitResult.AlreadyCommitted,"Direct upgrade duplicate receipt safe");
                }
            }
        }
        var levelling=fresh();levelling.collection.materials=new[]{new CollectionMaterial {id=dragon.materialIds[0],sourceColossusId=dragon.id,amount=1000000}};levelling.collection.relics=new[]{new CollectionRelic {id=relicId,attackRoll=100,hpRoll=1000}};
        var levels=new FormalCampaignJournal(levelling,encode,decode);
        for(int level=1;level<120;level++){
            var req=new FormalRelicRequest("level."+level,relicId,null,levels.Snapshot.revision,RelicOperation.LevelUp);levels.CommitRelic(req,catalog,s=>true);
            check(levels.Snapshot.collection.relics[0].level==level+1,"Relic progression reaches every level through 120");
        }
        reject(()=>levels.CommitRelic(new FormalRelicRequest("max",relicId,null,levels.Snapshot.revision,RelicOperation.LevelUp),catalog,s=>true),"Relic level 121 blocked");
        var equip=new FormalRelicRequest("equip",relicId,combat.FormationIds[0],levels.Snapshot.revision,RelicOperation.Equip);
        string preEquip=encode(levels.Snapshot);check(levels.CommitRelic(equip,catalog,s=>false)==GrowthCommitResult.SaveFailed && encode(levels.Snapshot)==preEquip,"Equip save failure retains prior equipment");
        reject(()=>levels.CommitActiveSeconds(60,s=>true),"Pending equipment save blocks other writes");levels.CommitRelic(equip,null,s=>true);
        reject(()=>levels.CommitRelic(new FormalRelicRequest("double",relicId,combat.FormationIds[1],levels.Snapshot.revision,RelicOperation.Equip),catalog,s=>true),"Same relic cannot equip two heroines");
        var naked=new PlayableBattle(1,new PlayableProgress(),9,combatDefinitions:combat,formalGrowth:levels.Snapshot.growth);
        var armed=new PlayableBattle(1,new PlayableProgress(),9,combatDefinitions:combat,formalGrowth:levels.Snapshot.growth,collectionGrowth:levels.Snapshot.collection);
        check(armed.State.Heroes[0].MaxHitPoints>naked.State.Heroes[0].MaxHitPoints && armed.State.Heroes[0].Attack>naked.State.Heroes[0].Attack,"Equipped fixed growth random stats and ability reach combat");
        check(Enumerable.Range(0,5).All(i=>armed.ChainRate(i)==naked.ChainRate(i)),"Relic stats and ability do not change chain rates");
        levels.CommitRelic(new FormalRelicRequest("remove",relicId,combat.FormationIds[0],levels.Snapshot.revision,RelicOperation.Unequip),catalog,s=>true);check(levels.Snapshot.collection.equipment.Length==0,"Unequip durable");
        var bad=decode(encode(levels.Snapshot));bad.collection.relics[0].contentVersion="future";reject(bad.Validate,"Same named relic with unknown content version blocked");
        var poor=fresh();poor.collection.relics=new[]{new CollectionRelic {id=relicId}};var insufficient=new FormalCampaignJournal(poor,encode,decode);reject(()=>insufficient.CommitRelic(new FormalRelicRequest("poor",relicId,null,0,RelicOperation.LevelUp),catalog,s=>true),"Missing source materials blocks upgrade");
        check(!insufficient.HasPending && insufficient.Snapshot.collection.relics[0].level==1,"Insufficient resources do not create partial upgrade");
        var unknownContent=decode(encode(levels.Snapshot)).collection;unknownContent.relics[0].id="unknown.relic";reject(()=>unknownContent.ValidateContent(catalog),"Unknown saved relic ID rejected against loaded content");
        unknownContent=decode(encode(levels.Snapshot)).collection;unknownContent.materials[0].sourceColossusId=WorldCatalog.ColossusIds[1];reject(()=>unknownContent.ValidateContent(catalog),"Saved material source must match resource owner");
        var mergeSource=fresh();mergeSource.collection.relics=new[]{new CollectionRelic {id=relicId,level=77,attackRoll=100,hpRoll=1000}};
        var merging=new FormalCampaignJournal(mergeSource,encode,decode);
        for(int seed=0;seed<8;seed++){
            var current=merging.Snapshot;var s=new BattleCollectionSession(catalog,"merge."+seed,dragon.id,50,current.revision,combat.FormationIds,seed);
            var req=new FormalBattleEndRequest(s.Finish(BattleEndReason.Victory,current.world.poemIds,current.world.unlockedStoryIds),current.revision);
            merging.CommitBattleEnd(req,catalog,w=>{var c=new CampaignState(WorldCatalog.ColossusIds,w);c.ClaimColossusVictory(dragon.id,WorldCatalog.Colossi[0].EnvironmentTags,new VictoryReward(req.Id,50,10,8,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return c.CreateSave();},p=>true);
            check(merging.Snapshot.collection.relics.Single().level==77 && merging.Snapshot.collection.relics.Single().attackRoll==100 && merging.Snapshot.collection.relics.Single().hpRoll==1000,"Low reacquisition never reduces existing level or stats");
            check(merging.Snapshot.collection.materials.Single().amount==60*(seed+1) && merging.Snapshot.world.firstClearIds.Length==1,"Replay resources accumulate without repeating first clear");
            check(merging.Snapshot.collection.receipts.Last().relicDrawCount==5,"High level records all five hunt attempts");
        }
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"newaster-plan5-"+Guid.NewGuid().ToString("N"));System.IO.Directory.CreateDirectory(directory);
        string path=System.IO.Path.Combine(directory,"formal.json");
        try{
            var store=new FormalCampaignStore(path,encode,decode,t=>JsonSerializer.Deserialize<FormalCampaignHeader>(t,options));
            store.Save(fresh());var next=merging.Snapshot;next.revision=1;store.Save(next);
            check(store.Load(out var disk)==FormalLoadResult.Loaded && encode(disk)==encode(next),"Actual file reload retains complete hunt inventory receipts and resources");
            var future=new {version=1,saveId=FormalCampaignSave.Identity,collection=new{version=2,contentVersion="future",receipts="changed-shape"}};
            System.IO.File.WriteAllText(path,JsonSerializer.Serialize(future,options));
            check(store.Load(out _)==FormalLoadResult.Blocked && store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Future nested collection shape stops before decoding and cannot silently recover older backup");
            check(System.IO.File.Exists(path+".bak"),"Unknown future save keeps previous backup intact");
        }finally{foreach(var file in System.IO.Directory.GetFiles(directory))System.IO.File.Delete(file);System.IO.Directory.Delete(directory);}
    }
}
