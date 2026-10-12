using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan12ViperTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(path),options);
        string draft=Path.GetFullPath("game/unity/Assets/Game/ContentDrafts/Viper");
        var combat=Read<CombatDefinitionCatalog>(Path.Combine(draft,"combat.json"));combat.Validate();
        var story=Read<ProductionStoryContent>(Path.Combine(draft,"story.json"));story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        const string id="heroine.viper";
        var old=Read<CombatDefinitionCatalog>(Path.Combine(resources,"Combat/battle-plan12-invisible.json"));
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        var cards=HeroineIdentityCatalog.Traits(combat.Hero(id),new FormalHeroineGrowth{heroineId=id});
        check(cards.Length==8 && cards.Select(c=>c.name).Distinct().Count()==8,"Viper has eight distinct authored traits");
        var nodes=home.weaponNodes.Where(n=>n.heroineId==id && !n.initial).ToArray();
        check(nodes.Length==12 && nodes.Select(n=>n.terminal).Distinct().Count()==12 && nodes.All(n=>n.terminal.StartsWith("アヴェンジャー・") && !cards.Any(c=>c.name==n.terminal)),"Viper weapon variants do not reuse trait names");
        check(HeroineTraitRules.MasteryBonusForHero(id,"physical-defense",new FormalHeroineGrowth{heroineId=id,skillLevels=new[]{7,7,7}})==5,"Mastered character trait supplies the displayed physical defense");
        check(combat.HeroineIds.Length==22 && !old.HeroineIds.Contains(id),"Viper remains a draft without changing the live catalog");
        foreach(var hero in old.heroines)check(JsonSerializer.Serialize(hero,options)==JsonSerializer.Serialize(combat.heroines.Single(h=>h.id==hero.id),options),"Existing heroine remains exact: "+hero.id);
        check(story.chapters.Where(c=>c.ownerId==id).Sum(c=>c.pages.Length)==30 && story.events.Count(e=>e.ownerId==id)==5,"Viper owns thirty original story pages and five events");
        check(combat.Skill(id,2).ruleOrigin=="newaster-original" && combat.Skill(id,2).sourceFile=="" && combat.Skill(id,2).observedSkillLevel==0,"Unobserved third skill is explicitly original");
        for(int slot=0;slot<3;slot++)for(int level=1;level<=7;level++){
            var defs=Read<CombatDefinitionCatalog>(Path.Combine(draft,"combat.json"));foreach(var j in defs.jobs)j.hp=100000;
            var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=defs.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=20,skillLevels=new[]{level,level,level}}).ToArray()};
            var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;
            var battle=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:defs.WithFormation(new[]{id,"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
            int steps=0;while(battle.AvailableHero!=0 && !battle.Ended && steps++<500)battle.Pass();
            check(battle.AvailableHero==0,"Viper reaches READY");int hp=battle.State.BossHitPoints;
            check(battle.Act(0,slot,"body") && battle.State.BossHitPoints<hp,"Viper skill executes with gunner rules at level "+level+" slot "+slot);
        }
        var oldStory=Read<ProductionStoryContent>(Path.Combine(resources,"Story/plan12-invisible-story-content.json"));var oldHome=ProductionStoryCatalog.Home(old,oldStory);
        var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5432,stones=3210,heroines=old.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion}};
        save.home.formationIds=old.FormationIds;HomeConditions.Refresh(save,oldHome);AffectionSaveAdapter.Migrate(save,oldHome);save.Validate();
        string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,options);FormalCampaignSave Decode(string s)=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        string beforeSave=Encode(save);var migrated=ProductionStoryMigration.Prepare(save,combat,story,s=>Decode(Encode(s)));
        check(Encode(migrated)==beforeSave && !migrated.growth.heroines.Any(h=>h.heroineId==id),"Twenty-one-form save preserves all fields and does not auto-grant Viper");
        var progression=new FormalProgression(migrated.growth,combat.HeroineIds);var journal=new FormalCampaignJournal(migrated,Encode,Decode);var request=new GrowthRequest("viper.join",id,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
        check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && Encode(journal.Snapshot)==beforeSave,"Failed Viper join is atomic");
        check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.weaponNodeIds.Contains(id+".weapon.root"),"Viper joins with her own root");
        var banner=ProductionEconomyCatalog.Kinder(combat.HeroineIds);banner.Validate(combat.HeroineIds);int roll=0;check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"Viper is individually summonable");
        Console.WriteLine("PLAN12_VIPER_DRAFT_DATA_PASS 21 preserved / 22 draft / 3 skills x 7 levels; art and publication pending");
    }
}
