using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class HeroineSanctuaryTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat,string storyJson)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(storyJson,options);var home=ProductionStoryCatalog.Home(combat,story);var collection=ProductionStoryCatalog.Collection(combat,story);
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=50000,heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=collection.contentVersion,materials=home.materials.Select(m=>new CollectionMaterial{id=m.id,sourceColossusId=m.colossusId,amount=10000}).ToArray()}};
        var journal=new FormalCampaignJournal(save,encode,decode);var progress=new FormalProgression(save.growth,combat.FormationIds);string hero=combat.FormationIds[0];
        var poor=save.growth.Copy();poor.nectar=59;var poorProgress=new FormalProgression(poor,combat.FormationIds);bool insufficient=false;try{poorProgress.Preview(new GrowthRequest("poor",hero,0,GrowthOperation.Skill,2,skillSlot:0));}catch(ArgumentException){insufficient=true;}check(insufficient && poorProgress.Snapshot.nectar==59,"Insufficient nectar cannot raise skill or spend");
        foreach(int badSlot in new[]{-1,3}){bool rejected=false;try{progress.Preview(new GrowthRequest("invalid.slot."+badSlot,hero,0,GrowthOperation.Skill,2,skillSlot:badSlot));}catch(ArgumentException){rejected=true;}check(rejected && progress.Snapshot.nectar==50000,"Invalid skill slots are rejected without mutation");}
        var potent=save.growth.Copy();potent.heroines[0].skillLevels=new[]{7,1,1};var damageBase=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:save.growth);var damageUp=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:potent);for(int turn=0;damageBase.AvailableHero!=0 && turn<30;turn++){damageBase.Pass();damageUp.Pass();}check(damageUp.PreviewDamage(0,0,"body")>damageBase.PreviewDamage(0,0,"body") && damageUp.State.Heroes[0].Attack==damageBase.State.Heroes[0].Attack,"Skill Lv7 increases actual damage preview independently of mastery and stats");
        var legacy=JsonSerializer.Deserialize<FormalHeroineGrowth>("{\"heroineId\":\"heroine.slayer\",\"level\":9}",options);check(legacy.SkillLevel(0)==1 && legacy.Copy().SkillLevel(2)==1,"Absent skill fields preserve prior level and start skills at Lv1");
        for(int slot=0;slot<3;slot++)for(int level=2;level<=7;level++){
            var before=progress.Snapshot;var request=new GrowthRequest("sanctuary.skill."+slot+"."+level,hero,before.revision,GrowthOperation.Skill,level,skillSlot:slot);
            check(progress.Preview(request).NectarCost==60*(level-1),"Skill preview shows the authored incremental cost");
            check(progress.Commit(request,s=>false)==GrowthCommitResult.SaveFailed && progress.Snapshot.nectar==before.nectar && progress.Snapshot.heroines[0].SkillLevel(slot)==level-1,"Failed skill persistence changes neither cost nor level");
            check(progress.Commit(request,s=>journal.CommitGrowth(s,p=>true))==GrowthCommitResult.Committed,"Pending skill candidate retries without rebuilding");
            check(progress.Commit(request,s=>throw new Exception("second debit"))==GrowthCommitResult.AlreadyCommitted && progress.Snapshot.nectar==before.nectar-60*(level-1),"Skill receipt prevents duplicate debit");
        }
        check(progress.Snapshot.heroines[0].skillLevels.SequenceEqual(new[]{7,7,7}) && save.growth.heroines[0].SkillLevel(0)==1,"Skill snapshots deeply isolate arrays and reach cap7");
        bool refused=false;try{progress.Preview(new GrowthRequest("cap",hero,progress.Snapshot.revision,GrowthOperation.Skill,8,skillSlot:0));}catch(ArgumentException){refused=true;}check(refused,"Skill level8 is refused without spend");
        var invalid=progress.Snapshot;invalid.heroines[0].skillLevels=new[]{0,7,7};refused=false;try{invalid.Validate();}catch(ArgumentException){refused=true;}check(refused,"Explicit invalid skill data is not silently repaired");
        invalid=progress.Snapshot;invalid.heroines[0].skillLevels=new[]{7};refused=false;try{invalid.Validate();}catch(ArgumentException){refused=true;}check(refused,"Malformed skill array is rejected");
        foreach(string id in combat.FormationIds){
            var before=save.growth.Copy();var after=before.Copy();after.heroines.Single(h=>h.heroineId==id).skillLevels=new[]{7,7,7};
            var a=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:before);var b=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:after);int actor=Array.IndexOf(combat.FormationIds,id);var job=combat.Hero(id).jobId;
            check(b.State.Heroes[actor].Speed==a.State.Heroes[actor].Speed && b.ChainRate(actor)==a.ChainRate(actor),"Skill mastery never changes speed or chain rate");
            check(HeroineTraitRules.Mastered(after.heroines[actor]) && !HeroineTraitRules.Mastered(before.heroines[actor]),"Mastery requires all three skills at7");
            check(job=="job.fighter"?b.State.Heroes[actor].CriticalChanceBp==a.State.Heroes[actor].CriticalChanceBp+500:job=="job.berserker"?b.State.Heroes[actor].Attack>a.State.Heroes[actor].Attack:job=="job.defender"?b.State.Heroes[actor].MaxHitPoints>a.State.Heroes[actor].MaxHitPoints:job=="job.blaster"?b.State.Heroes[actor].MagicDefense>a.State.Heroes[actor].MagicDefense:b.State.Heroes[actor].PhysicalDefense>a.State.Heroes[actor].PhysicalDefense,"Each heroine mastery applies its distinct actual battle bonus");
            for(int slot=0;slot<3;slot++){var s=combat.Skill(id,slot);var upgraded=HeroineSkillRules.AtLevel(s,7);check(upgraded.powerScale>=s.powerScale && upgraded.recoveryPercent==s.recoveryPercent && upgraded.castPercent==s.castPercent && upgraded.resourceCost==s.resourceCost && HeroineSkillRules.Description(s,7,combat.Job(job)).Length>25,"Every skill has live effect text and capped scaling without timing/resource changes");}
            check(HeroineIdentityCatalog.Traits(combat.Hero(id),after.heroines[actor]).Length==3,"Each heroine has three authored trait cards");
        }
        var state=journal.Snapshot;
        foreach(var n in home.weaponNodes.Where(n=>n.heroineId==hero)){
            var op=new HomeOperation("weapon",n.id);var request=new FormalHomeRequest("tree.acquire."+n.id,"weapon",journal.Snapshot.revision,home.contentVersion,op.Key);
            check(journal.CommitHomeOperation(request,home,op,s=>true)==GrowthCommitResult.Committed,"All thirteen production nodes can actually be acquired with their dependencies");
        }
        string node=hero+".weapon.alpha.tier4";
        for(int lv=2;lv<=7;lv++){
            var before=journal.Snapshot;var op=new HomeOperation("weapon-level",node,lv.ToString());var request=new FormalHomeRequest("tree.level."+lv,"weapon-level",before.revision,home.contentVersion,op.Key);var cost=WeaponGrowthRules.Costs(home.weaponNodes.Single(n=>n.id==node),lv-1,home)[0];
            check(journal.CommitHomeOperation(request,home,op,s=>false)==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==encode(before),"Weapon upgrade failure is atomic across materials and level");
            check(journal.CommitHomeOperation(request,home,op,s=>true)==GrowthCommitResult.Committed && journal.Snapshot.home.WeaponLevel(node)==lv && HomeRules.Balance(journal.Snapshot,cost.resourceId)==HomeRules.Balance(before,cost.resourceId)-cost.amount,"Weapon upgrade retry consumes its displayed materials once");
        }
        var equip=new HomeOperation("equip",node,hero);journal.CommitHomeOperation(new FormalHomeRequest("tree.equip","weapon",journal.Snapshot.revision,home.contentVersion,equip.Key),home,equip,s=>true);
        var armed=journal.Snapshot;var baseBattle=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:armed.growth);var armedBattle=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:armed.growth,homeCatalog:home,homeProgress:armed.home);
        check(armedBattle.State.Heroes[0].Attack>baseBattle.State.Heroes[0].Attack && armedBattle.SkillName(0,0)==home.weaponNodes.Single(n=>n.id==node).terminal,"Lv7 equipped weapon changes next battle attack and command profile");
        var old=decode(encode(save));old.home.contentVersion=HomeExperienceCatalog.PreviousProductionVersion;old.home.weaponNodeIds=new[]{hero+".weapon.root",hero+".weapon.alpha"};old.home.weaponEquipment=new[]{new HomeWeaponEquipment{heroineId=hero,nodeId=hero+".weapon.alpha"}};old.home.weaponLevels=null;old.growth.heroines[0].skillLevels=null;string original=encode(old);
        var migrated=ProductionStoryMigration.Prepare(old,combat,story,s=>decode(encode(s)));
        check(encode(old)==original && migrated.home.weaponEquipment[0].nodeId==hero+".weapon.alpha" && migrated.home.WeaponLevel(hero+".weapon.alpha")==1 && migrated.growth.heroines[0].SkillLevel(0)==1,"RC1 content migration retains equipped stable nodes and missing skill/weapon fields mean Lv1");
        var roster=new HeroineRoster(Enumerable.Range(0,256).Select(i=>new HeroineRosterEntry{id="heroine.test."+i.ToString("D3"),name="人物"+i.ToString("D3"),jobId=i%2==0?"job.fighter":"job.gunner",stage="available",originalStats=true,originalSkills=true}),new[]{"job.fighter","job.gunner"},new[]{"job.fighter","job.gunner"});
        var entries=roster.Search("",null,null);var pages=Enumerable.Range(0,22).SelectMany(p=>entries.Skip(p*12).Take(12)).ToArray();check(pages.Length==256 && pages.Select(e=>e.id).Distinct().Count()==256,"Twelve-card pagination selects all256 without cycling individual heroes");
        check(roster.Search("人物12",null,null).Length==10 && roster.Search("", "job.gunner",null).Length==128 && roster.Search("人物12","job.fighter",null).Length==5,"Card search combines names and job filtering");
        check(roster.Search("not found",null,null).Length==0 && roster.Search("",null,new[]{"heroine.test.255"}).Single().id=="heroine.test.255","Empty and owned-only card results are explicit");
    }
}
