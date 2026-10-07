using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class Plan10SlayerSwimTests {
 public static void Run(Action<bool,string> check,string resources){
  var options=new JsonSerializerOptions{IncludeFields=true};Func<CombatDefinitionCatalog> read=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-slayer-swim.json")),options);var c=read();c.Validate();
  check(c.HeroineIds.Length==12 && c.HeroineIds.Select(c.PersonId).Distinct().Count()==10 && c.jobs.Length==11,"Summer form adds general, shares Slayer identity");
  bool duplicate=false;try{var ids=c.FormationIds;ids[1]="heroine.slayer-swim";c.WithFormation(ids);}catch(ArgumentException){duplicate=true;}check(duplicate,"Slayer forms cannot deploy together");
  var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=c.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20}).ToArray()};
  Func<int,PlayableBattle> fixture=slot=>{var defs=read();foreach(var j in defs.jobs)j.hp=100000;var ids=defs.FormationIds;ids[0]="heroine.r";ids[slot]="heroine.slayer-swim";var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;var b=new PlayableBattle(1,new PlayableProgress(),991,combatDefinitions:defs.WithFormation(ids),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true,deployment:new BattleDeployment("heroine.slayer-swim"));b.State.BreakPart(b.State.Parts.Single(p=>p.Role=="drain").Id,int.MaxValue);return b;};
  Action<PlayableBattle,int> ready=(b,a)=>{int limit=0;while(b.AvailableHero!=a && !b.Ended && limit++<500)b.Pass();check(b.AvailableHero==a,"General reaches READY");};
  for(int i=0;i<5;i++){
   var b=fixture(i);ready(b,i);check(b.CommanderActor==i && b.State.Heroes[0].GeneralAttackPercent==15 && b.State.Heroes[4].GeneralCriticalBp==1000,"One commander applies own profile in every position");
   b.State.Heroes[i].GainResource(15);long clock=b.Clock;check(b.ActivateGeneralCommand(i) && b.Clock==clock && b.AvailableHero==i && b.State.Heroes[i].JobResource==0,"Commander activation spends once without action consumption");check(!b.ActivateGeneralCommand(i) && b.State.Heroes[0].GeneralAttackPercent==30,"Repeated activation rejects insufficient gauge");
  }
  var attack=fixture(0);ready(attack,0);check(attack.Act(0,0,"body") && attack.State.BossStatus.AttackReductionPercent==20 && attack.State.Heroes[0].TimedSpeedPercent==20,"Source attack applies enemy debuff and self speed");
  var buff=fixture(0);ready(buff,0);check(buff.Act(0,1,"body") && buff.State.Heroes.All(h=>h.TimedEffects.Any(e=>e.Kind=="attack" && e.Percent==30)),"Summer support buffs all five living allies");
  var ultimate=fixture(0);ready(ultimate,0);check(ultimate.Act(0,2,"body") && ultimate.State.BossStatus.Meter("burn")>0,"Source ultimate burns all enemy targets");
  var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-slayer-swim-story-content.json")),options);story.Validate(c.HeroineIds,WorldCatalog.ColossusIds.ToArray());var home=ProductionStoryCatalog.Home(c,story);home.Validate(true);
  check(home.weaponNodes.Count(n=>n.heroineId=="heroine.slayer-swim")==13 && home.assets.All(a=>!a.placeholder),"Summer weapon tree and dedicated adopted art");check(story.chapters.Where(x=>x.ownerId=="heroine.slayer-swim").Sum(x=>x.pages.Length)==30 && story.events.Count(e=>e.ownerId=="heroine.slayer-swim")==5,"Thirty original pages and five relationship events");
  var expires=fixture(0);ready(expires,0);expires.State.Heroes[0].GainResource(15);expires.ActivateGeneralCommand(0);long expiry=expires.Clock+300;int steps=0;while(expires.Clock<expiry && steps++<1000)expires.Pass();check(expires.Clock>=expiry && expires.State.Heroes[0].GeneralAttackPercent==15 && !expires.JobState(0).Empowered(expires.Clock),"Command enhancement expires after exactly three hundred Clock");
  var fallen=fixture(0);ready(fallen,0);fallen.Pass();fallen.State.Heroes[0].TakeDamage(int.MaxValue);fallen.Pass();check(fallen.State.Heroes.All(h=>h.GeneralAttackPercent==0 && h.GeneralCriticalBp==0),"A fallen commander stops all personal formation effects");
  Console.WriteLine("PLAN10_SLAYER_SWIM_PASS general / five positions / source skills / shared identity / art / story");
 }
}
