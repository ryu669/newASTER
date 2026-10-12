using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan12AprilTests
{
 public static void Run(Action<bool,string> check,string resources){
  var opt=new JsonSerializerOptions{IncludeFields=true};const string id="heroine.april";
  CombatDefinitionCatalog Read()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/April/combat.json"),opt);
  var d=Read();d.Validate();var old=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan12-weapon-master.json")),opt);
  check(d.HeroineIds.Length==25,"April append-only 25 forms");
  foreach(var h in old.heroines)check(JsonSerializer.Serialize(h,opt)==JsonSerializer.Serialize(d.Hero(h.id),opt),"Preserved heroine "+h.id);
  var cards=HeroineIdentityCatalog.Traits(d.Hero(id),new FormalHeroineGrowth{heroineId=id});check(cards.Length==8 && cards.Select(c=>c.name).Distinct().Count()==8,"Eight distinct April traits");
  check(HeroineTraitRules.MasteryBonusForHero(id,"hp",new FormalHeroineGrowth{heroineId=id,skillLevels=new[]{7,7,7}})==5,"Character mastery HP five percent");
  PlayableBattle Battle(int level){
   var defs=Read();foreach(var j in defs.jobs)j.hp=100000;
   var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=defs.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=20,skillLevels=new[]{level,level,level}}).ToArray()};
   var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;enemy.statusResistances=Array.Empty<EnemyStatusResistanceDef>();foreach(var p in enemy.parts){p.baseHp=1000000;p.hpPerLevel=1;}
   var b=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:defs.WithFormation(new[]{id,"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
   int guard=0;while(b.AvailableHero!=0 && !b.Ended && guard++<500)b.Pass();check(b.AvailableHero==0,"April READY");return b;
  }
  for(int level=1;level<=7;level++)for(int slot=0;slot<3;slot++){
   var b=Battle(level);var hero=b.State.Heroes[0];int boss=b.State.BossHitPoints;
   if(slot==1){
    var recipient=b.State.Heroes[1];recipient.TakeDamage(recipient.HitPoints-1);
    int other=b.State.Heroes[2].HitPoints;
    check(b.ActWithAllies(0,slot,"body",new[]{1}),"April heals selected ally level "+level);
    check(recipient.HitPoints>1 && b.State.Heroes[2].HitPoints==other && b.State.BossHitPoints==boss,"Selected healing does not leak to others or damage boss");
   }else{
    check(b.Act(0,slot,"body"),"April executes attack level "+level+" slot "+slot);
    check(b.State.BossHitPoints<boss,"April attack damages boss");
   }
  }
  check(d.Skill(id,0).damageType=="physical" && d.Skill(id,0).attributes.Contains("刺突"),"April physical thrust");
  check(d.Skill(id,2).damageType=="magic" && d.Skill(id,2).attributes.Contains("風") && d.Skill(id,2).targetRuleId=="target.all-enemies","April wind magic all targets");
  check(HeroinePersonalAbility.For(id).Kind=="healing" && HeroinePersonalAbility.For(id).Percent==10,"April personal healing ten percent");
  var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/ContentDrafts/April/story.json"),opt);story.Validate(d.HeroineIds,WorldCatalog.ColossusIds.ToArray());
  var home=ProductionStoryCatalog.Home(d,story);home.Validate(true);var nodes=home.weaponNodes.Where(n=>n.heroineId==id && !n.initial).ToArray();
  check(nodes.Length==12 && nodes.Select(n=>n.terminal).Distinct().Count()==12 && nodes.All(n=>n.terminal.StartsWith("リクレイマー・") && !cards.Any(c=>c.name==n.terminal)),"Weapon variants distinct from traits");
  var oldStory=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan12-weapon-master-story-content.json")),opt);var oldHome=ProductionStoryCatalog.Home(old,oldStory);
  var save=new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5432,stones=3210,heroines=old.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=23,duplicateRank=2,skillLevels=new[]{4,5,6}}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(oldHome.contentVersion),collection=new FormalCollectionLedger{contentVersion=CollectionCatalog.ProductionVersion}};
  save.home.formationIds=old.FormationIds;HomeConditions.Refresh(save,oldHome);AffectionSaveAdapter.Migrate(save,oldHome);save.Validate();
  string Encode(FormalCampaignSave s)=>JsonSerializer.Serialize(s,opt);FormalCampaignSave Decode(string s)=>JsonSerializer.Deserialize<FormalCampaignSave>(s,opt);
  string beforeSave=Encode(save);var migrated=ProductionStoryMigration.Prepare(save,d,story,s=>Decode(Encode(s)));check(Encode(migrated)==beforeSave,"Old twenty-four-form save preserved exactly without granting new heroine");
  var progression=new FormalProgression(migrated.growth,d.HeroineIds);var journal=new FormalCampaignJournal(migrated,Encode,Decode);var request=new GrowthRequest("april.join",id,migrated.growth.revision,GrowthOperation.ReceiveHeroine);
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && Encode(journal.Snapshot)==beforeSave,"Failed join atomic");
  check(progression.Commit(request,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.weaponNodeIds.Contains(id+".weapon.root"),"Successful join receives own tree root");
  var banner=ProductionEconomyCatalog.Kinder(d.HeroineIds);banner.Validate(d.HeroineIds);int roll=0;check(banner.Draw(max=>roll++==0?0:Array.IndexOf(banner.heroineIds,id)).heroineId==id,"April individually summonable");
  Console.WriteLine("PLAN12_APRIL_DATA_PASS skills=3x7 selected-heal wind-magic old-save atomic-join");
 }
}
