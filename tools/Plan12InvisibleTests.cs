using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan12InvisibleTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(resources,path)),options);
        var combat=Read<CombatDefinitionCatalog>("Combat/battle-plan12-invisible.json");combat.Validate();
        var story=Read<ProductionStoryContent>("Story/plan12-invisible-story-content.json");story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        const string id="heroine.invisible";
        check(HeroineSkillRules.Description(combat.Skill(id,0),1,combat.Job("job.healer")).Contains("回復対象に攻撃＋20%") && HeroineSkillRules.Description(combat.Skill(id,2),1,combat.Job("job.healer")).Contains("魔法防御＋30%"),"Healing skill descriptions disclose their recipient buffs");
        check(combat.HeroineIds.Length==21 && home.weaponNodes.Count(n=>n.heroineId==id)==13,"Invisible is the twenty-first independent form");
        var nodes=home.weaponNodes.Where(n=>n.heroineId==id && !n.initial).ToArray();
        check(nodes.Select(n=>n.terminal).Distinct().Count()==12 && nodes.All(n=>n.terminal.StartsWith("可能性の獣・") && n.terminal!=HeroineAuthoredNames.For(id).Personal),"Twelve weapon variants have distinct names");
        check(story.chapters.Where(c=>c.ownerId==id).Sum(c=>c.pages.Length)==30 && story.events.Count(e=>e.ownerId==id)==5,"Invisible owns original complete story");
        var actor=new BattleHero("test",1000,100,0,magicDefense:100);
        actor.ApplySelfEffects(new[]{new TimedSelfEffectDef{kind="magic-defense",percent=40,turns=2}});
        check(actor.MagicDefense==140,"Magic defense affects the actual defense stat");actor.CompleteOwnerCommand();check(actor.MagicDefense==140,"Magic defense survives its first duration step");actor.CompleteOwnerCommand();check(actor.MagicDefense==100,"Magic defense expires rather than becoming permanent");
        for(int slot=0;slot<3;slot++){
            foreach(int level in Enumerable.Range(1,7))HeroineSkillRules.AtLevel(combat.Skill(id,slot),level);
            var defs=Read<CombatDefinitionCatalog>("Combat/battle-plan12-invisible.json");foreach(var j in defs.jobs)j.hp=100000;
            var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=defs.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=20}).ToArray()};
            var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;
            var b=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:defs.WithFormation(new[]{id,"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
            int steps=0;while(b.AvailableHero!=0 && !b.Ended && steps++<500)b.Pass();check(b.AvailableHero==0,"Invisible reaches READY");
            foreach(var h in b.State.Heroes)h.TakeDamage(10000);
            if(slot==1){int hp=b.State.BossHitPoints;check(b.Act(0,slot,"body"),"Invisible attacks");check(b.State.BossHitPoints<hp && b.State.Heroes[0].TimedEffects.Any(e=>e.Kind=="speed"),"Light attack damages and supplies its authored speed effect");}
            else{
                var hero=b.State.Heroes[0];int expected=(int)Math.Floor(hero.Attack*b.HealingSkill(0,slot).AttackScale)*110/100;
                int preview=b.PreviewHealing(0,1,slot),before=b.State.Heroes[1].HitPoints,defense=b.State.Heroes[1].MagicDefense;
                check(hero.HasVisibleTrait(id+".trait.personal") && preview==expected,"Personal healing bonus is reflected in preview");
                check(b.ActWithAllies(0,slot,"body",new[]{1}),"Invisible heals selected allies");
                check(b.State.Heroes[1].HitPoints-before==preview,"Committed healing equals the preview exactly");
                check(b.LastHealingTargets.Count==(slot==0?1:5) && b.State.Heroes[1].MagicDefense>defense,"Correct recipients receive healing and magic defense");
            }
        }
        var old=Read<CombatDefinitionCatalog>("Combat/battle-plan12-first-batch.json");var oldStory=Read<ProductionStoryContent>("Story/plan12-first-batch-story-content.json");var oldHome=ProductionStoryCatalog.Home(old,oldStory);
        var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5432,stones=3210,heroines=old.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion}};
        save.home.formationIds=old.FormationIds;HomeConditions.Refresh(save,oldHome);AffectionSaveAdapter.Migrate(save,oldHome);save.Validate();
        string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,options);FormalCampaignSave Decode(string s)=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        string beforeSave=Encode(save);var migrated=ProductionStoryMigration.Prepare(save,combat,story,s=>Decode(Encode(s)));
        check(Encode(migrated)==beforeSave && !migrated.growth.heroines.Any(h=>h.heroineId==id),"Twenty-form save migration preserves all fields and does not auto-grant Invisible");
        var progression=new FormalProgression(migrated.growth,combat.HeroineIds);var journal=new FormalCampaignJournal(migrated,Encode,Decode);var request=new GrowthRequest("invisible.join",id,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
        check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && Encode(journal.Snapshot)==beforeSave,"Failed join is atomic");
        check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.weaponNodeIds.Contains(id+".weapon.root"),"Normal join grants her own root");
        var banner=ProductionEconomyCatalog.Kinder(combat.HeroineIds);banner.Validate(combat.HeroineIds);int roll=0;check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"Invisible is individually summonable");
        Console.WriteLine("PLAN12_INVISIBLE_DATA_PASS healing preview/commit + magic-defense expiry + all skills + 20-to-21 atomic save + summon");
    }
}
