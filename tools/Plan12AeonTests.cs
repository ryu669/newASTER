using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

internal static class Plan12AeonTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        const string hero="heroine.aeon";
        var options=new JsonSerializerOptions{IncludeFields=true};
        string drafts=Path.GetFullPath(Path.Combine(resources,"../ContentDrafts/Aeon"));
        Func<CombatDefinitionCatalog> read=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(drafts,"combat.json")),options);
        var combat=read();combat.Validate();
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(drafts,"story.json")),options);
        story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        ProductionStoryCatalog.Collection(combat,story).Validate();
        check(combat.HeroineIds.Length==16 && combat.HeroineIds.Select(combat.PersonId).Distinct().Count()==13,"Aeon adds an independent person and form");
        check(!File.ReadAllText(Path.Combine(resources,"Combat/battle-plan11-7.json")).Contains(hero),"Historical pre-Aeon catalog remains intact");
        check(story.chapters.Where(c=>c.ownerId==hero).Sum(c=>c.pages.Length)==30 && story.chapters.Where(c=>c.ownerId==hero).Sum(c=>c.poems.Length)==18,"Aeon has thirty authored pages and eighteen enemy correspondences");
        var events=story.events.Where(e=>e.ownerId==hero).OrderBy(e=>e.affectionRequired).ToArray();
        check(events.Length==5 && events.Select(e=>e.affectionRequired).SequenceEqual(new[]{1,5,10,15,20}) && events.Count(e=>e.establishesLover)==1 && events[2].establishesLover,"Relationship has three basic and two lover events, with one confession");
        var restored=JsonSerializer.Deserialize<ProductionStoryContent>(JsonSerializer.Serialize(story,options),options);
        restored.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        check(restored.chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.pages).SequenceEqual(story.chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.pages)),"JSON roundtrip preserves Aeon's complete chapter text");
        var baseGrowth=new FormalHeroineGrowth{heroineId=hero,level=1};
        var cards=HeroineIdentityCatalog.Traits(combat.Hero(hero),baseGrowth);
        check(cards.Length==8 && cards.All(c=>!c.description.Contains("交流特性")) && cards.Single(c=>c.id=="mastery.general").description.Contains("Rank 1 / 5"),"Eight named traits retain flavor and duplicate-based general mastery");
        baseGrowth.duplicateRank=5;
        check(HeroineIdentityCatalog.Traits(combat.Hero(hero),baseGrowth).Single(c=>c.id=="mastery.general").description.Contains("Rank 5 / 5"),"General mastery reaches rank five through duplicates");
        baseGrowth.duplicateRank=0;baseGrowth.skillLevels=new[]{7,7,7};
        check(HeroineIdentityCatalog.Traits(combat.Hero(hero),baseGrowth).Single(c=>c.id=="mastery.general").description.Contains("Rank 1 / 5"),"Max skill levels never raise duplicate-only general mastery");
        check(DailyReactionProfiles.For(hero).main=="cool","Aeon has her own daily-reaction profile");
        for(int level=1;level<=7;level++)for(int slot=0;slot<3;slot++){
            var skill=HeroineSkillRules.AtLevel(combat.Skill(hero,slot),level);
            check(skill.powerScale>0 && skill.recoveryPercent==combat.Skill(hero,slot).recoveryPercent && skill.resourceCost==0,"Aeon skill level improves potency without free waiting or command gauge");
        }
        var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20}).ToArray()};
        Func<int,string,PlayableBattle> battle=(position,commander)=>{
            var defs=read();foreach(var job in defs.jobs)job.hp=100000;
            var ids=new[]{hero,"heroine.slayer-swim","heroine.iconoclast","heroine.undermine","heroine.echidna"};
            string swap=ids[position];ids[position]=hero;ids[0]=swap;
            var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;
            return new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:defs.WithFormation(ids),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true,deployment:new BattleDeployment(commander));
        };
        Action<PlayableBattle,int> ready=(b,actor)=>{int limit=0;while(b.AvailableHero!=actor && !b.Ended && limit++<500)b.Pass();check(b.AvailableHero==actor,"Aeon reaches READY");};
        for(int position=0;position<5;position++){
            var b=battle(position,hero);ready(b,position);
            int formationScale=100+b.State.Heroes[position].TraitEffect("formation");
            check(b.CommanderActor==position && b.State.Heroes[2].GeneralSpeedPercent==8*formationScale/100 && b.State.Heroes[4].GeneralAttackPercent==10*formationScale/100,"Aeon's own five-slot formation and duplicate-rank traits apply from every commander position");
            b.State.Heroes[position].GainResource(15);long before=b.Clock;
            check(b.ActivateGeneralCommand(position) && b.Clock==before && b.AvailableHero==position && b.State.Heroes[position].JobResource==0,"Aeon command spends gauge once and preserves READY and clock");
            check(b.State.Heroes[2].GeneralSpeedPercent==16*formationScale/100 && !b.ActivateGeneralCommand(position),"Command switches to enhanced slots and rejects repeat activation");
        }
        var summer=battle(0,"heroine.slayer-swim");
        int summerScale=100+summer.State.Heroes[summer.CommanderActor].TraitEffect("formation");
        check(summer.State.Heroes[0].GeneralAttackPercent==15*summerScale/100 && summer.State.Heroes[4].GeneralCriticalBp==1000*summerScale/100 && summer.State.Heroes[2].GeneralSpeedPercent==0,"Selecting the other general applies only her profile");
        for(int slot=0;slot<3;slot++){
            var b=battle(0,hero);ready(b,0);int before=b.State.BossHitPoints;
            check(b.Act(0,slot,"body") && b.State.BossHitPoints<before,"Each of Aeon's three skills executes real damage");
            int supportScale=100+b.State.Heroes[0].TraitEffect("own-support");
            if(slot==0)check(b.State.Heroes[0].TimedSpeedPercent==20*supportScale/100,"Divine Memory applies its speed buff with general support traits");
            if(slot==1)check(b.State.Heroes[0].TimedEffects.Any(e=>e.Kind=="attack" && e.Percent==30*supportScale/100),"End Roll applies its attack buff with general support traits");
        }
        var partBonus=battle(0,hero);var neutral=read();neutral.Skill(hero,1).partScale=1;
        foreach(var job in neutral.jobs)job.hp=100000;
        var enemyNeutral=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemyNeutral.baseHp=10000000;enemyNeutral.hpPerLevel=1;
        var partNeutral=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:neutral.WithFormation(new[]{hero,"heroine.slayer-swim","heroine.iconoclast","heroine.undermine","heroine.echidna"}),formalGrowth:growth,colossusDefinition:enemyNeutral,useJobRulesV2:true,deployment:new BattleDeployment(hero));
        string target=partBonus.State.Parts.First(p=>!p.IsBroken).Id;
        check(partBonus.PreviewDamage(0,1,target)>partNeutral.PreviewDamage(0,1,target) && partBonus.PreviewDamage(0,1,"body")==partNeutral.PreviewDamage(0,1,"body"),"End Roll's part bonus never applies to the boss body");
        var missing=read();missing.generalFormations=missing.generalFormations.Where(p=>p.ownerId!=hero).ToArray();
        bool rejected=false;try{missing.Validate();}catch(ArgumentException){rejected=true;}
        check(rejected,"Missing Aeon formation rejects the catalog instead of borrowing another general's profile");
        Continuity(check,resources,combat,story,options);
        Console.WriteLine("PLAN12_AEON_DATA_PASS story / collection / eight traits / duplicate mastery / three skills / two generals / body-part separation / old-save continuity");
    }
    private static void Continuity(Action<bool,string> check,string resources,CombatDefinitionCatalog combat,ProductionStoryContent story,JsonSerializerOptions options)
    {
        const string hero="heroine.aeon";
        var oldCombat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan11-7.json")),options);
        var oldStory=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-shangrila-story-content.json")),options);
        var oldHome=ProductionStoryCatalog.Home(oldCombat,oldStory);var oldCollection=ProductionStoryCatalog.Collection(oldCombat,oldStory);
        var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=4321,stones=1234,awakeningCrystals=321,kinderPoints=125,tickets=new[]{new HeroineTicket{heroineId="heroine.slayer",count=2}},heroines=oldCombat.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20,duplicateRank=2,skillLevels=new[]{3,4,5}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=oldCollection.contentVersion}};
        save.home.formationIds=oldCombat.FormationIds;save.home.affections=new[]{new HomeAffection{heroineId="heroine.slayer",value=20}};
        save.home.loverHeroineIds=new[]{"heroine.slayer"};save.home.readEventIds=new[]{"heroine.slayer.event.2"};save.home.unlockedEventIds=save.home.readEventIds;
        save.world.poemIds=oldStory.chapters.Where(c=>c.ownerId=="heroine.slayer").SelectMany(c=>c.poems).Select(p=>p.id).ToArray();
        HomeConditions.Refresh(save,oldHome);
        save.world.readStoryIds=save.world.unlockedStoryIds.Where(id=>id.StartsWith("heroine.slayer.poem-chapter.")).Take(1).ToArray();
        save.home.weaponNodeIds=save.home.weaponNodeIds.Concat(new[]{"heroine.slayer.weapon.alpha"}).ToArray();
        save.home.weaponLevels=save.home.weaponLevels.Concat(new[]{new HomeWeaponLevel{nodeId="heroine.slayer.weapon.alpha",level=5}}).ToArray();
        save.home.weaponEquipment=new[]{new HomeWeaponEquipment{heroineId="heroine.slayer",nodeId="heroine.slayer.weapon.alpha"}};
        save.collection.ooparts=new OopartInventorySave{slots=oldCombat.FormationIds.Select(id=>new FormationSlot{heroineFormId=id}).ToArray()};
        for(int i=1;i<=3;i++){
            save.collection.ooparts.slots=oldCombat.FormationIds.Skip(i-1).Concat(oldCombat.FormationIds.Take(i-1)).Select(id=>new FormationSlot{heroineFormId=id}).ToArray();
            OopartService.SavePreset(save.collection.ooparts,"preset."+i,"編成"+i);
        }
        save.Validate();save.home.ValidateContent(oldHome,save);
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        string before=encode(save);var migrated=ProductionStoryMigration.Prepare(save,combat,story,s=>decode(encode(s)));
        check(encode(save)==before && encode(migrated)==before,"Expanding fifteen to sixteen forms preserves every old-save field exactly");
        check(!migrated.growth.heroines.Any(h=>h.heroineId==hero) && !migrated.home.weaponNodeIds.Any(id=>id.StartsWith(hero+".")),"Catalog expansion never grants Aeon or her roots for free");
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        var nodes=home.weaponNodes.Where(n=>n.heroineId==hero).ToArray();check(nodes.Length==13 && nodes.Select(n=>n.id).Distinct().Count()==13,"Aeon owns thirteen independent weapon nodes");
        foreach(var branch in new[]{"alpha","beta","gamma"})for(int tier=2;tier<=4;tier++){
            var prior=nodes.Single(n=>n.id==hero+".weapon."+branch+(tier==2?"":".tier"+(tier-1)));
            var next=nodes.Single(n=>n.id==hero+".weapon."+branch+".tier"+tier);
            check(WeaponGrowthRules.EffectKinds(prior).All(WeaponGrowthRules.EffectKinds(next).Contains),"Aeon branch retains earlier effects at "+next.id);
        }
        var banner=ProductionEconomyCatalog.Kinder(combat.HeroineIds);banner.Validate(combat.HeroineIds);int roll=0;
        check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,hero)).heroineId==hero,"Normal summon banner can draw Aeon");
        var journal=new FormalCampaignJournal(migrated,encode,decode);var progression=new FormalProgression(migrated.growth,combat.HeroineIds);
        var request=new GrowthRequest("aeon.join",hero,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
        string pendingBefore=encode(journal.Snapshot);
        check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==pendingBefore,"Failed Aeon join preserves all old data atomically");
        check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed,"Aeon joins through normal durable growth transaction");
        var joined=decode(encode(journal.Snapshot));joined.Validate();joined.home.ValidateContent(home,joined);
        check(joined.growth.heroines.Single(h=>h.heroineId==hero).level==1 && joined.home.weaponNodeIds.Contains(hero+".weapon.root") && !joined.home.loverHeroineIds.Contains(hero),"New Aeon starts independently with only her root");
        check(joined.growth.nectar==4321 && joined.growth.stones==1234 && joined.growth.kinderPoints==125 && joined.home.WeaponLevel("heroine.slayer.weapon.alpha")==5 && joined.world.readStoryIds.SequenceEqual(save.world.readStoryIds),"Join preserves currency, old weapon level and story reading");
        check(progression.Commit(request,g=>throw new Exception("duplicate save"))==GrowthCommitResult.AlreadyCommitted,"Retry does not grant a duplicate or mastery rank");
    }
}
