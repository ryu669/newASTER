using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class Plan12FirstBatchTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        string drafts=Path.GetFullPath(Path.Combine(resources,"../ContentDrafts/FirstBatch"));
        Func<CombatDefinitionCatalog> read=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(drafts,"combat.json")),options);
        var combat=read();combat.Validate();
        var namedHome=ProductionStoryCatalog.Home(combat,JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(drafts,"story.json")),options));
        var namedNodes=namedHome.weaponNodes.Where(n=>!n.initial).ToArray();
        check(namedNodes.Select(n=>n.terminal).Distinct().Count()==namedNodes.Length,"Weapon names are unique across every branch, tier and heroine");
        var traits=combat.HeroineIds.SelectMany(id=>{var a=HeroineAuthoredNames.For(id);return new[]{a.Innate,a.Personal,a.Mastery};}).ToArray();
        check(namedNodes.All(n=>!traits.Contains(n.terminal) && !traits.Contains(n.weaponTraitName)),"No tree node or final weapon ability reuses a character trait name");
        check(namedNodes.All(n=>n.terminal==WeaponNodeNames.Name(n.heroineId,WeaponGrowthRules.Route(n),WeaponGrowthRules.Tier(n))),"Every route uses weapon variant and consistent Roman tier I through IV");
        check(WeaponNodeNames.Format("聖剣オーロラ",new[]{"イスィリオ","ユースティ","セリスィ"},0,1)=="聖剣オーロラ・イスィリオⅠ" && WeaponNodeNames.Format("聖剣オーロラ",new[]{"イスィリオ","ユースティ","セリスィ"},2,4)=="聖剣オーロラ・セリスィⅣ","User's Aurora naming examples retain weapon and route names");
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(drafts,"story.json")),options);
        story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var heroes=new[]{"heroine.iron-maiden","heroine.avalanche","heroine.anathema","heroine.shiva"};
        check(combat.HeroineIds.Length==20 && combat.HeroineIds.Select(combat.PersonId).Distinct().Count()==17,"Five additions give twenty forms and seventeen people");
        foreach(var id in heroes){
            var g=new FormalHeroineGrowth{heroineId=id,level=20};
            var cards=HeroineIdentityCatalog.Traits(combat.Hero(id),g);
            check(cards.Length==8 && cards.All(c=>!c.description.Contains("交流特性")),"New heroine has eight authored traits with flavor");
            check(cards.Single(c=>c.id.StartsWith("mastery.",StringComparison.Ordinal)).description.Contains("Rank 1 / 5"),"Base job mastery remains rank one");
            g.skillLevels=new[]{7,7,7};
            check(HeroineIdentityCatalog.Traits(combat.Hero(id),g).Single(c=>c.id.StartsWith("mastery.",StringComparison.Ordinal)).description.Contains("Rank 1 / 5"),"Skill investment never raises duplicate-only mastery");
            g.duplicateRank=5;
            check(HeroineIdentityCatalog.Traits(combat.Hero(id),g).Single(c=>c.id.StartsWith("mastery.",StringComparison.Ordinal)).description.Contains("Rank 5 / 5"),"Duplicates unlock highest mastery rank");
            check(story.chapters.Where(c=>c.ownerId==id).Sum(c=>c.pages.Length)==30 && story.chapters.Where(c=>c.ownerId==id).Sum(c=>c.poems.Length)==18,"Each new heroine has three full chapters and eighteen poem links");
            var events=story.events.Where(e=>e.ownerId==id).ToArray();
            check(events.Length==5 && events.All(e=>e.paragraphs.Length==8) && events.Count(e=>e.establishesLover)==1,"Five independent events contain one explicit confession");
            DailyReactionProfiles.For(id);
            for(int slot=0;slot<3;slot++){
                for(int level=1;level<=7;level++)HeroineSkillRules.AtLevel(combat.Skill(id,slot),level);
                var defs=read();foreach(var j in defs.jobs)j.hp=100000;
                var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;
                var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=defs.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=20}).ToArray()};
                var b=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:defs.WithFormation(new[]{id,"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
                int count=0;while(b.AvailableHero!=0 && !b.Ended && count++<500)b.Pass();check(b.AvailableHero==0,"New heroine reaches READY");
                foreach(var h in b.State.Heroes)h.TakeDamage(1000);
                int before=b.State.BossHitPoints,hp=b.State.Heroes[1].HitPoints;
                check(b.Act(0,slot,"body"),"Each new command executes in its own job");
                if(id=="heroine.shiva" && slot==1){
                    check(b.State.Heroes[1].HitPoints>hp && b.LastHealingTargets.Count==5,"Shiva heals all living allies");
                    check(b.State.Heroes.All(h=>h.TimedEffects.Any(e=>e.Kind=="attack") && h.TimedEffects.Any(e=>e.Kind=="critical")),"Shiva healing applies attack and critical buffs to all recipients");
                }else if(id=="heroine.iron-maiden" && slot==1)check(b.State.Heroes[0].TimedEffects.Any(e=>e.Kind=="physical-protection"),"Iron Maiden creates her own guard effect");
                else check(b.State.BossHitPoints<before,"Authored attack causes real damage");
                if(id=="heroine.avalanche" && slot==0)check(b.State.Heroes[0].TimedEffects.Any(e=>e.Kind=="attack-reduction"),"Avalanche's efficient first shot retains its attack drawback");
                if(id=="heroine.shiva" && slot==2)check(b.State.Heroes[1].HitPoints>hp,"Shiva offensive finisher also heals allies");
            }
        }
        check(combat.Skill("heroine.shiva",0).ignoreDefenseBp==10000 && combat.Skill("heroine.anathema",1).ignoreDefenseBp==2000,"Defense profiles remain distinct");
        Continuity(check,resources,combat,story,options,heroes);
        Console.WriteLine("PLAN12_FIRST_BATCH_DATA_PASS four people / twelve skills / distinct jobs / healing buffs / duplicate-only mastery / 120 pages");
    }
    private static void Continuity(Action<bool,string> check,string resources,CombatDefinitionCatalog combat,ProductionStoryContent story,JsonSerializerOptions options,string[] heroes)
    {
        // Run only after the dedicated art has been adopted into production resources.
        if(!File.Exists(Path.Combine(resources,"Combat/battle-plan12-first-batch.json")))return;
        var old=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan12-aeon.json")),options);
        var oldStory=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan12-aeon-story-content.json")),options);
        var oldHome=ProductionStoryCatalog.Home(old,oldStory);var oldCollection=ProductionStoryCatalog.Collection(old,oldStory);
        var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5432,stones=3210,kinderPoints=155,heroines=old.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=oldCollection.contentVersion}};
        save.home.formationIds=old.FormationIds;HomeConditions.Refresh(save,oldHome);AffectionSaveAdapter.Migrate(save,oldHome);
        AffectionService.State(save,oldHome,"heroine.aeon").level=20;
        save.collection.ooparts=new OopartInventorySave{slots=old.FormationIds.Select(id=>new FormationSlot{heroineFormId=id}).ToArray()};
        for(int i=1;i<=3;i++)OopartService.SavePreset(save.collection.ooparts,"batch.preset."+i,"編成"+i);
        save.Validate();
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var before=encode(save);var migrated=ProductionStoryMigration.Prepare(save,combat,story,s=>decode(encode(s)));
        check(before==encode(save) && before==encode(migrated),"Sixteen-to-twenty expansion preserves every existing field, affection and three presets");
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        check(!migrated.growth.heroines.Any(h=>heroes.Contains(h.heroineId)),"No new person is granted by save loading");
        var banner=ProductionEconomyCatalog.Kinder(combat.HeroineIds);banner.Validate(combat.HeroineIds);
        foreach(var id in heroes){
            int roll=0;check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"Each new heroine is individually summonable");
            check(home.weaponNodes.Count(n=>n.heroineId==id)==13,"Each new heroine owns thirteen independent tree nodes");
            var journal=new FormalCampaignJournal(migrated,encode,decode);var progression=new FormalProgression(migrated.growth,combat.HeroineIds);
            var request=new GrowthRequest("batch.join."+id,id,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
            check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && encode(journal.Snapshot)==encode(migrated),"Failed new join leaves the old save intact");
            check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed,"New heroine joins through atomic normal growth");
            var joined=journal.Snapshot;
            check(joined.growth.heroines.Single(h=>h.heroineId==id).level==1 && joined.home.weaponNodeIds.Contains(id+".weapon.root") && AffectionService.State(joined,home,id).level==0,"New person receives only her own root and starts with independent affection");
            check(joined.growth.stones==3210 && joined.growth.nectar==5432 && AffectionService.State(joined,home,"heroine.aeon").level==20,"Join preserves old money and Aeon affection");
        }
    }
}
