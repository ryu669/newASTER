using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan12WeaponMasterTests
{
 public static void Run(Action<bool,string> check,string resources){
  var opt=new JsonSerializerOptions{IncludeFields=true};const string id="heroine.weapon-master";
  CombatDefinitionCatalog Read()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/WeaponMaster/combat.json"),opt);
  var d=Read();d.Validate();var old=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan12-vindicator.json")),opt);
  check(d.HeroineIds.Length==24,"Weapon Master append-only 24 forms");
  foreach(var h in old.heroines)check(JsonSerializer.Serialize(h,opt)==JsonSerializer.Serialize(d.Hero(h.id),opt),"Preserved heroine "+h.id);
  var cards=HeroineIdentityCatalog.Traits(d.Hero(id),new FormalHeroineGrowth{heroineId=id});check(cards.Length==8 && cards.Select(c=>c.name).Distinct().Count()==8,"Eight distinct Weapon Master traits");
  check(HeroineTraitRules.MasteryBonusForHero(id,"attack",new FormalHeroineGrowth{heroineId=id,skillLevels=new[]{7,7,7}})==5,"Character mastery attack five percent");
  PlayableBattle Battle(int level){
   var defs=Read();foreach(var j in defs.jobs)j.hp=100000;
   var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=defs.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=20,skillLevels=new[]{level,level,level}}).ToArray()};
   var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;enemy.statusResistances=Array.Empty<EnemyStatusResistanceDef>();foreach(var p in enemy.parts){p.baseHp=1000000;p.hpPerLevel=1;}
   var b=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:defs.WithFormation(new[]{id,"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
   int guard=0;while(b.AvailableHero!=0 && !b.Ended && guard++<500)b.Pass();check(b.AvailableHero==0,"Weapon Master READY");return b;
  }
  for(int level=1;level<=7;level++)for(int slot=0;slot<3;slot++){
   var b=Battle(level);var hero=b.State.Heroes[0];int boss=b.State.BossHitPoints,attack=hero.Attack,critical=hero.CriticalChanceBp;
   check(b.Act(0,slot,"body"),"Weapon Master executes level "+level+" slot "+slot);
   if(slot==0){check(b.State.EnemyStatus("body").DefenseReductionPercent==20,"Defense reduction applied after hit");check(b.State.Parts.All(p=>p.Status.DefenseReductionPercent==0),"Selected-target defense debuff does not leak to parts");}
   if(slot==1){check(b.State.BossHitPoints==boss && hero.Attack>attack && hero.CriticalChanceBp>critical,"Self buff increases attack and critical without enemy damage");}
   else check(b.State.BossHitPoints<boss,"Attack damages enemy");
   if(slot==2)check(b.State.EnemyStatus("body").Meter("burn")==40 && b.State.Parts.All(p=>p.Status.Meter("burn")==40),"All-target burn accumulates forty independently");
  }
  var battle=Battle(1);var actor=battle.State.Heroes[0];actor.PersonalTraitsEnabled=true;var skill=new BattleSkill("test",1m,0);
  check(HeroinePersonalAbility.Damage(actor,skill,"body",battle.State)==0,"Burn trait inactive before threshold");
  battle.State.EnemyStatus("body").Add(new EnemyStatusDef{kind="burn",amount=100});
  check(HeroinePersonalAbility.Damage(actor,skill,"body",battle.State)==20,"Burn trait activates after threshold");
  check(HeroinePersonalAbility.Damage(actor,skill,battle.State.Parts[0].Id,battle.State)==0,"Burn trait target-local");
  actor.PersonalTraitsEnabled=false;check(HeroinePersonalAbility.Damage(actor,skill,"body",battle.State)==0,"Locked burn trait has no effect");
  var status=new EnemyStatusState();status.SetDefenseReduction(20,3);status.Tick();status.Tick();check(status.DefenseReductionPercent==20,"Defense debuff lasts three enemy actions");status.Tick();check(status.DefenseReductionPercent==0,"Defense debuff expires");
  int before=BattleActionResolver.CalculateDamage(battle.State,actor,skill,"body");battle.State.EnemyStatus("body").SetDefenseReduction(20,3);check(BattleActionResolver.CalculateDamage(battle.State,actor,skill,"body")>before,"Defense reduction changes damage preview and resolution formula");
  foreach(int bad in new[]{-1,51}){var invalid=Read();invalid.Skill(id,0).enemyDefenseReductionPercent=bad;bool rejected=false;try{invalid.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Invalid defense reduction rejected");}
  var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/WeaponMaster/story.json"),opt);story.Validate(d.HeroineIds,WorldCatalog.ColossusIds.ToArray());
  var home=ProductionStoryCatalog.Home(d,story);home.Validate(true);var nodes=home.weaponNodes.Where(n=>n.heroineId==id && !n.initial).ToArray();
  check(nodes.Length==12 && nodes.Select(n=>n.terminal).Distinct().Count()==12 && nodes.All(n=>n.terminal.StartsWith("換装銃架ハルモニア・") && !cards.Any(c=>c.name==n.terminal)),"Weapon variants distinct from traits");
  var oldStory=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan12-vindicator-story-content.json")),opt);var oldHome=ProductionStoryCatalog.Home(old,oldStory);
  var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5432,stones=3210,heroines=old.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion}};
  save.home.formationIds=old.FormationIds;HomeConditions.Refresh(save,oldHome);AffectionSaveAdapter.Migrate(save,oldHome);save.Validate();
  string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,opt);FormalCampaignSave Decode(string s)=>JsonSerializer.Deserialize<FormalCampaignSave>(s,opt);
  string beforeSave=Encode(save);var migrated=ProductionStoryMigration.Prepare(save,d,story,s=>Decode(Encode(s)));check(Encode(migrated)==beforeSave,"Old twenty-three-form save preserved exactly without granting new heroine");
  var progression=new FormalProgression(migrated.growth,d.HeroineIds);var journal=new FormalCampaignJournal(migrated,Encode,Decode);var request=new GrowthRequest("weapon-master.join",id,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && Encode(journal.Snapshot)==beforeSave,"Failed join atomic");
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.weaponNodeIds.Contains(id+".weapon.root"),"Successful join receives own tree root");
  var banner=ProductionEconomyCatalog.Kinder(d.HeroineIds);banner.Validate(d.HeroineIds);int roll=0;check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"Weapon Master individually summonable");
  Console.WriteLine("PLAN12_WEAPON_MASTER_DATA_PASS skills=3x7 target-local-defense burn-trait self-buff old-save atomic-join");
 }
}
