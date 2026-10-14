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
  // Exercise the new generic damage rule on an accepted battle actor while
  // Estreya's home/trait/art registration remains explicitly unpublished.
  PlayableBattle Battle(int level,int hpPercent,bool bonus=true)
  {
   var d=JsonSerializer.Deserialize<CombatDefinitionCatalog>(JsonSerializer.Serialize(old,opt),opt);
   foreach(var job in d.jobs)job.hp=10000;
   for(int slot=0;slot<3;slot++){
    var skill=JsonSerializer.Deserialize<SkillCombatDef>(JsonSerializer.Serialize(draft.Skill(id,slot),opt),opt);
    var prior=d.Skill("heroine.iconoclast",slot);
    skill.id=prior.id;skill.ownerId=prior.ownerId;skill.sourceSkillId=prior.id;
    if(!bonus){skill.selfHpAtLeastPercent=0;skill.selfHpDamageBonusPercent=0;}
    d.skills[Array.IndexOf(d.skills,prior)]=skill;
   }
   var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=d.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=1,skillLevels=new[]{level,level,level}}).ToArray()};
   var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;foreach(var p in enemy.parts){p.baseHp=1000000;p.hpPerLevel=1;}
   var battle=new PlayableBattle(1,new PlayableProgress(),1453,combatDefinitions:d.WithFormation(new[]{"heroine.iconoclast","heroine.slayer","heroine.undermine","heroine.echidna","heroine.excalipan"}),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);
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
  foreach(var invalid in new[]{new[]{101,100},new[]{50,-1},new[]{0,100},new[]{50,0},new[]{50,1001}}){
   var d=Read();d.Skill(id,2).selfHpAtLeastPercent=invalid[0];d.Skill(id,2).selfHpDamageBonusPercent=invalid[1];
   bool rejected=false;try{d.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Reject malformed HP damage pair");
  }
  var nonAttack=Read();nonAttack.Skill(id,2).effectRuleId="effect.heal";
  bool invalidEffect=false;try{nonAttack.Validate();}catch(ArgumentException){invalidEffect=true;}
  check(invalidEffect,"HP damage bonus cannot be attached to healing");
  check(!old.heroines.Any(h=>h.id==id),"Estreya remains outside released catalog until art and Player acceptance");
  Console.WriteLine("PLAN12_ESTREYA_DRAFT_PASS 26 definitions / 3x7 skills / HP49-50-51 preview and execution / recoil / 30 pages / 5 events; NOT_PUBLISHED");
 }
}
