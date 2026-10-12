using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan12VindicatorTests
{
 public static void Run(Action<bool,string> check,string resources){
  var opt=new JsonSerializerOptions{IncludeFields=true};const string id="heroine.vindicator";
  CombatDefinitionCatalog Read()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/Vindicator/combat.json"),opt);
  var d=Read();d.Validate();var old=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan12-viper.json")),opt);
  check(d.HeroineIds.Length==23,"Vindicator append-only 23-form draft");
  foreach(var h in old.heroines)check(JsonSerializer.Serialize(h,opt)==JsonSerializer.Serialize(d.Hero(h.id),opt),"Preserved heroine "+h.id);
  var cards=HeroineIdentityCatalog.Traits(d.Hero(id),new FormalHeroineGrowth{heroineId=id});check(cards.Length==8 && cards.Select(c=>c.name).Distinct().Count()==8,"Eight distinct Vindicator traits");
  check(HeroineTraitRules.MasteryBonusForHero(id,"magic-defense",new FormalHeroineGrowth{heroineId=id,skillLevels=new[]{7,7,7}})==5,"Character mastery differs from duplicate-only job mastery");
  PlayableBattle Battle(int level,int seed=1453,int chance=-1){
   var defs=Read();foreach(var j in defs.jobs)j.hp=100000;
   if(chance>=0)defs.Skill(id,0).enemyWaitChancePercent=chance;
   var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=defs.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=20,skillLevels=new[]{level,level,level}}).ToArray()};
   var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;
   var b=new PlayableBattle(1,new PlayableProgress(),seed,combatDefinitions:defs.WithFormation(new[]{id,"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
   int guard=0;while(b.AvailableHero!=0 && !b.Ended && guard++<500)b.Pass();check(b.AvailableHero==0,"Vindicator READY");return b;
  }
  for(int level=1;level<=7;level++)for(int slot=0;slot<3;slot++){
   var b=Battle(level);var hero=b.State.Heroes[0];int hp=hero.HitPoints,boss=b.State.BossHitPoints,defense=hero.PhysicalDefense;
   check(b.Act(0,slot,"body"),"Vindicator executes level "+level+" slot "+slot);
   if(slot==1){check(hero.HitPoints==hp-hero.MaxHitPoints*20/100,"Self buff recoil is exactly 20% maximum HP once");check(hero.PhysicalDefense>defense && hero.ForcedTarget,"Buff installs physical defense and forced target before recoil");}
   else check(b.State.BossHitPoints<boss,"Attack damages enemy");
   if(slot==2)check(hero.HitPoints==hp-hero.MaxHitPoints*10/100,"Magic attack recoil is exactly 10%");
  }
  var counterBattle=Battle(1);var counterHero=counterBattle.State.Heroes[0];
  counterHero.PersonalTraitsEnabled=true;
  check(HeroinePersonalAbility.Damage(counterHero,new BattleSkill("reaction.defender",1m,0),"body")==20,"Personal trait boosts Defender counters");
  check(HeroinePersonalAbility.Damage(counterHero,new BattleSkill("ordinary",1m,0),"body")==0,"Counter trait does not boost ordinary attacks");
  counterHero.PersonalTraitsEnabled=false;
  check(HeroinePersonalAbility.Damage(counterHero,new BattleSkill("reaction.defender",1m,0),"body")==0,"Locked personal trait does not boost counters");
  bool success=false,failure=false;
  for(int seed=0;seed<24;seed++){
   var b=Battle(1,seed);long before=b.UpcomingOrder().Single(x=>x.Actor==-1).At;check(b.Act(0,0,"body"),"Probabilistic delay executes");long delta=b.UpcomingOrder().Single(x=>x.Actor==-1).At-before;
   check(delta==0 || delta==40,"Delay changes by exactly 40 or zero");success|=delta==40;failure|=delta==0;
  }
  check(success && failure,"60% delay exercises success and failure paths");
  foreach(int chance in new[]{0,100}){var b=Battle(1,5,chance);long before=b.UpcomingOrder().Single(x=>x.Actor==-1).At;b.Act(0,0,"body");check(b.UpcomingOrder().Single(x=>x.Actor==-1).At-before==40,"Legacy zero and explicit 100 preserve guaranteed wait");}
  var source=d.Skill(id,0);check(HeroineSkillRules.AtLevel(source,7).enemyWaitChancePercent==60 && HeroineSkillRules.Description(source,1,d.jobs.Single(j=>j.id=="job.defender")).Contains("60%の確率"),"Chance survives scaling and description");
  source.enemyWaitChancePercent=101;bool invalid=false;try{d.Validate();}catch(ArgumentException){invalid=true;}check(invalid,"Out-of-range chance rejected");
  d=Read();var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/Vindicator/story.json"),opt);
  var home=ProductionStoryCatalog.Home(d,story);home.Validate(true);
  var nodes=home.weaponNodes.Where(n=>n.heroineId==id && !n.initial).ToArray();
  check(nodes.Length==12 && nodes.Select(n=>n.terminal).Distinct().Count()==12 && nodes.All(n=>n.terminal.StartsWith("黒大剣リーパー・") && !cards.Any(c=>c.name==n.terminal)),"Twelve distinct weapon names do not reuse trait names");
  var oldStory=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan12-viper-story-content.json")),opt);var oldHome=ProductionStoryCatalog.Home(old,oldStory);
  var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5432,stones=3210,heroines=old.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion}};
  save.home.formationIds=old.FormationIds;HomeConditions.Refresh(save,oldHome);AffectionSaveAdapter.Migrate(save,oldHome);save.Validate();
  string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,opt);FormalCampaignSave Decode(string s)=>JsonSerializer.Deserialize<FormalCampaignSave>(s,opt);
  string beforeSave=Encode(save);var migrated=ProductionStoryMigration.Prepare(save,d,story,s=>Decode(Encode(s)));
  check(Encode(migrated)==beforeSave && !migrated.growth.heroines.Any(h=>h.heroineId==id),"Twenty-two-form save remains identical without auto-grant");
  var progression=new FormalProgression(migrated.growth,d.HeroineIds);var journal=new FormalCampaignJournal(migrated,Encode,Decode);var request=new GrowthRequest("vindicator.join",id,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && Encode(journal.Snapshot)==beforeSave,"Failed Vindicator join is atomic");
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.weaponNodeIds.Contains(id+".weapon.root"),"Vindicator joins with her own root");
  var banner=ProductionEconomyCatalog.Kinder(d.HeroineIds);banner.Validate(d.HeroineIds);int roll=0;check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"Vindicator is individually summonable");
  Console.WriteLine("PLAN12_VINDICATOR_DATA_PASS 3x7 skills / buff recoil / probabilistic delay / legacy delay; art and publication pending");
 }
}
