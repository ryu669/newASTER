using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class Plan10ArcaneTests {
 public static void Run(Action<bool,string> check,string resources){
  var options=new JsonSerializerOptions{IncludeFields=true};Func<CombatDefinitionCatalog> read=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-arcane.json")),options);var c=read();c.Validate();
  check(c.HeroineIds.Length==13 && c.HeroineIds.Select(c.PersonId).Distinct().Count()==11 && c.jobs.Length==11,"Arcane adds person with existing Gunner job");
  check(c.Skill("heroine.arcane",0).observedSkillLevel==7 && c.Skill("heroine.arcane",0).powerScale==2.08f && Enumerable.Range(1,2).All(i=>c.Skill("heroine.arcane",i).ruleOrigin=="newaster-original" && c.Skill("heroine.arcane",i).observedSkillLevel==0 && c.Skill("heroine.arcane",i).sourceFile==""),"One observed skill and two explicitly original skills");
  var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=c.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20}).ToArray()};
  Func<int,PlayableBattle> fixture=slot=>{var defs=read();foreach(var j in defs.jobs)j.hp=100000;var ids=defs.FormationIds;ids[slot]="heroine.arcane";var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;return new PlayableBattle(1,new PlayableProgress(),991,combatDefinitions:defs.WithFormation(ids),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);};
  Action<PlayableBattle,int> ready=(b,a)=>{int limit=0;while(b.AvailableHero!=a && !b.Ended && limit++<500)b.Pass();check(b.AvailableHero==a,"Arcane reaches READY");};
  for(int i=0;i<5;i++){
   var b=fixture(i);ready(b,i);int magazine=b.JobState(i).Magazines[0];check(b.Act(i,0,"body") && b.JobState(i).Magazines[0]==magazine-1,"Source attack consumes exactly one selected magazine round in all positions");ready(b,i);check(b.SelectMagazine(i,1) && b.Reload(i) && b.JobState(i).Magazines.All(n=>n==6),"Magazine switch and reload");
  }
  var buff=fixture(0);ready(buff,0);check(buff.Act(0,1,"body") && buff.State.Heroes.All(h=>h.TimedEffects.Any(e=>e.Kind=="critical" && e.Percent==15)),"Original relic survey buffs living allies");
  var volley=fixture(0);ready(volley,0);volley.State.Heroes[0].GainResource(15);check(volley.FullVolley(0,"body") && volley.JobState(0).Magazines[0]==0 && volley.JobState(0).Magazines[1]==6,"Full volley empties only chosen magazine");
  var invalid=read();invalid.Skill("heroine.arcane",1).observedSkillLevel=7;bool rejected=false;try{invalid.Validate();}catch(ArgumentException){rejected=true;}check(rejected,"Original skill cannot claim observation evidence");
  if(Environment.GetEnvironmentVariable("NEWASTER_PLAN10_ARCANE_ART")=="1"){
   var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-arcane-story-content.json")),options);story.Validate(c.HeroineIds,WorldCatalog.ColossusIds.ToArray());var home=ProductionStoryCatalog.Home(c,story);home.Validate(true);check(home.weaponNodes.Count(n=>n.heroineId=="heroine.arcane")==13 && home.assets.All(a=>!a.placeholder),"Arcane tree and adopted art");check(story.chapters.Where(x=>x.ownerId=="heroine.arcane").Sum(x=>x.pages.Length)==30 && story.events.Count(e=>e.ownerId=="heroine.arcane")==5,"Thirty original pages and five relationship events");
  }
  Console.WriteLine("PLAN10_ARCANE_PASS source provenance / magazines / five positions / original support");
 }
}
