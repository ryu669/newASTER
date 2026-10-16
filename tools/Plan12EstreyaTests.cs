using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan12EstreyaTests
{
 public static void Run(Action<bool,string> check,string resources)
 {
  var opt=new JsonSerializerOptions{IncludeFields=true};const string id="heroine.estreya";
  CombatDefinitionCatalog Read()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/Estreya/combat.json"),opt);
  var draft=Read();draft.Validate();
  var old=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan12-april.json")),opt);
  check(draft.heroines.Length==old.heroines.Length+1,"Estreya draft adds exactly one form");
  foreach(var h in old.heroines)check(JsonSerializer.Serialize(h,opt)==JsonSerializer.Serialize(draft.Hero(h.id),opt),"Estreya preserves existing heroine "+h.id);
  foreach(var s in old.skills)check(JsonSerializer.Serialize(s,opt)==JsonSerializer.Serialize(draft.skills.Single(x=>x.id==s.id),opt),"Estreya preserves existing skill "+s.id);
  var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/Estreya/story.json"),opt);story.Validate(draft.HeroineIds,WorldCatalog.ColossusIds.ToArray());
  check(story.chapters.Count(c=>c.ownerId==id)==3 && story.chapters.Where(c=>c.ownerId==id).Sum(c=>c.pages.Length)==30 && story.events.Count(e=>e.ownerId==id)==5,"Estreya authored story completeness");
  PlayableBattle Battle(int level,int hpPercent,bool bonus=true)
  {
   var d=Read();
   foreach(var job in d.jobs)job.hp=10000;
   if(!bonus){d.Skill(id,2).selfHpAtLeastPercent=0;d.Skill(id,2).selfHpDamageBonusPercent=0;}
   var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=d.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=1,skillLevels=new[]{level,level,level}}).ToArray()};
   var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;foreach(var p in enemy.parts){p.baseHp=1000000;p.hpPerLevel=1;}
   var battle=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:d.WithFormation(new[]{id,"heroine.slayer","heroine.undermine","heroine.echidna","heroine.excalipan"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
   int guard=0;while(battle.AvailableHero!=0 && !battle.Ended && guard++<500)battle.Pass();
   check(battle.AvailableHero==0,"Estreya rule test reaches READY");
   var hero=battle.State.Heroes[0];hero.TakeDamage(hero.HitPoints-hero.MaxHitPoints*hpPercent/100);
   return battle;
  }
  for(int level=1;level<=7;level++){
   var levelSkill=HeroineSkillRules.AtLevel(draft.Skill(id,2),level);
   check(levelSkill.selfHpAtLeastPercent==50 && levelSkill.selfHpDamageBonusPercent==100,"HP bonus survives skill level conversion "+level);
   foreach(int percent in new[]{49,50,51}){
    var b=Battle(level,percent);var neutral=Battle(level,percent,false);
    int preview=b.PreviewDamage(0,2,"body"),normal=neutral.PreviewDamage(0,2,"body");
    check(percent<50?preview==normal:preview>normal,"HP bonus exact boundary "+percent+" level "+level);
    int before=b.State.BossHitPoints,neutralBefore=neutral.State.BossHitPoints;
    check(b.Act(0,2,"body") && neutral.Act(0,2,"body"),"Low HP never forbids Estreya finisher");
    int dealt=before-b.State.BossHitPoints,neutralDealt=neutralBefore-neutral.State.BossHitPoints;
    check(percent<50?dealt==neutralDealt:dealt>neutralDealt,"Executed damage agrees with preview boundary "+percent);
   }
   var recoil=Battle(level,100);int hp=recoil.State.Heroes[0].HitPoints,max=recoil.State.Heroes[0].MaxHitPoints;
   check(recoil.Act(0,1,"body"),"Estreya recoil attack executes");
   check(recoil.State.Heroes[0].HitPoints==hp-max*5/100,"Estreya exact five percent recoil");
   var quick=Battle(level,100);check(quick.Act(0,0,"body"),"Estreya quick attack executes");
  }
  var cards=HeroineIdentityCatalog.Traits(draft.Hero(id),new FormalHeroineGrowth{heroineId=id});
  check(cards.Length==8 && cards.Select(c=>c.name).Distinct().Count()==8,"Eight distinct Estreya traits");
  var heat=Battle(1,100);var actor=heat.State.Heroes[0];int attack=actor.Attack;
  string part=heat.State.Parts[0].Id;heat.State.BreakPart(part,heat.State.Parts[0].HitPoints-1);
  check(heat.Act(0,0,part) && actor.PersonalKillAttackPercent==5 && actor.Attack>attack,"Estreya actual part kill grants heat-up once");
  for(int n=0;n<10;n++)actor.PersonalEnemyDefeated();
  check(actor.PersonalKillAttackPercent==30,"Heat-up cannot exceed thirty percent");
  var fresh=Battle(1,100);check(fresh.State.Heroes[0].PersonalKillAttackPercent==0,"Heat-up never persists between battles");
  fresh.State.Heroes[0].PersonalTraitsEnabled=false;fresh.State.Heroes[0].PersonalEnemyDefeated();
  check(fresh.State.Heroes[0].PersonalKillAttackPercent==0,"Disabled personal trait cannot accumulate");
  check(HeroineTraitRules.MasteryBonusForHero(id,"attack",new FormalHeroineGrowth{heroineId=id,skillLevels=new[]{7,7,7}})==5,"Estreya character mastery bonus");
  foreach(var invalid in new[]{new[]{101,100},new[]{50,-1},new[]{0,100},new[]{50,0},new[]{50,1001}}){
   var d=Read();d.Skill(id,2).selfHpAtLeastPercent=invalid[0];d.Skill(id,2).selfHpDamageBonusPercent=invalid[1];
   bool rejected=false;try{d.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Reject malformed HP damage pair");
  }
  var nonAttack=Read();nonAttack.Skill(id,2).effectRuleId="effect.heal";
  bool invalidEffect=false;try{nonAttack.Validate();}catch(ArgumentException){invalidEffect=true;}
  check(invalidEffect,"HP damage bonus cannot be attached to healing");
  var home=ProductionStoryCatalog.Home(draft,story);home.Validate(true);var nodes=home.weaponNodes.Where(n=>n.heroineId==id && !n.initial).ToArray();
  check(nodes.Length==12 && nodes.Select(n=>n.terminal).Distinct().Count()==12 && nodes.All(n=>n.terminal.StartsWith("拳甲メテオラ・") && !cards.Any(c=>c.name==n.terminal)),"Weapon variants distinct from traits");
  var oldStory=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan12-april-story-content.json")),opt);var oldHome=ProductionStoryCatalog.Home(old,oldStory);
  var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5432,stones=3210,heroines=old.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion}};
  save.home.formationIds=old.FormationIds;HomeConditions.Refresh(save,oldHome);AffectionSaveAdapter.Migrate(save,oldHome);save.Validate();
  string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,opt);FormalCampaignSave Decode(string s)=>JsonSerializer.Deserialize<FormalCampaignSave>(s,opt);
  string beforeSave=Encode(save);var migrated=ProductionStoryMigration.Prepare(save,draft,story,s=>Decode(Encode(s)));check(Encode(migrated)==beforeSave,"Old twenty-five-form save preserved exactly without granting new heroine");
  var progression=new FormalProgression(migrated.growth,draft.HeroineIds);var journal=new FormalCampaignJournal(migrated,Encode,Decode);var request=new GrowthRequest("estreya.join",id,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && Encode(journal.Snapshot)==beforeSave,"Failed join atomic");
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.weaponNodeIds.Contains(id+".weapon.root"),"Successful join receives own tree root");
  var banner=ProductionEconomyCatalog.Kinder(draft.HeroineIds);banner.Validate(draft.HeroineIds);int roll=0;check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"Estreya individually summonable");
  check(!old.heroines.Any(h=>h.id==id),"Estreya is absent from the previous April catalog");
  Console.WriteLine("PLAN12_ESTREYA_DATA_PASS 26 definitions / 3x7 skills / HP49-50-51 preview and execution / recoil / 30 pages / 5 events; visual acceptance recorded separately");
 }
}
