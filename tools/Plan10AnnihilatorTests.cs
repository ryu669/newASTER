using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class Plan10AnnihilatorTests
{
    public static void Run(Action<bool,string> check,string resources)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        Func<CombatDefinitionCatalog> read=()=>JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10-annihilator.json")),options);
        var c=read();c.Validate();
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText(Path.Combine(resources,"Story/plan10-annihilator-story-content.json")),options);story.Validate(c.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        check(c.HeroineIds.Length==8 && c.HeroineIds.Select(c.PersonId).Distinct().Count()==7,"Eight forms are seven distinct people");
        string normal="heroine.annihilator",holy="heroine.annihilator-holy";
        bool rejected=false;try{c.WithFormation(new[]{normal,holy}.Concat(c.FormationIds.Skip(2)).ToArray());}catch(ArgumentException){rejected=true;}check(rejected,"Two costumes cannot occupy two party slots");
        var home=ProductionStoryCatalog.Home(c,story);var collection=ProductionStoryCatalog.Collection(c,story);
        var original=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(resources,"Combat/battle-plan10.json")),options);
        foreach(var h in original.heroines){check(JsonSerializer.Serialize(h,options)==JsonSerializer.Serialize(c.Hero(h.id),options),"Existing heroine preserved: "+h.id);foreach(var id in h.skills)check(JsonSerializer.Serialize(original.skills.Single(s=>s.id==id),options)==JsonSerializer.Serialize(c.skills.Single(s=>s.id==id),options),"Existing skill preserved: "+id);}
        var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=c.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20}).ToArray()};
        var save=new FormalCampaignSave{growth=growth,world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=collection.contentVersion}};
        save.home.affections=new[]{new HomeAffection{heroineId=normal,value=20}};save.home.loverHeroineIds=new[]{normal};save.home.readEventIds=new[]{normal+".event.2"};save.home.unlockedEventIds=new[]{normal+".event.2"};HomeConditions.Refresh(save,home);save.Validate();save.home.ValidateContent(home,save);
        check(save.home.affections.Single(a=>a.heroineId==holy).value==20 && save.home.loverHeroineIds.Contains(holy),"Same woman's affection and lover state are shared");
        Func<FormalCampaignSave,FormalCampaignSave> clone=s=>JsonSerializer.Deserialize<FormalCampaignSave>(JsonSerializer.Serialize(s,options),options);
        var first=clone(save);first.growth.heroines=first.growth.heroines.Where(h=>h.heroineId!=holy).ToArray();first.home.affections=first.home.affections.Where(a=>a.heroineId!=holy).ToArray();first.home.loverHeroineIds=new[]{normal};
        var journal=new FormalCampaignJournal(first,s=>JsonSerializer.Serialize(s,options),s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options));var progression=new FormalProgression(first.growth,c.HeroineIds);var join=new GrowthRequest("test.holy-join",holy,first.growth.revision,GrowthOperation.ReceiveHeroine);
        check(progression.Commit(join,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && !journal.Snapshot.home.loverHeroineIds.Contains(holy),"Failed second-form join does not publish inherited relationship");
        check(progression.Commit(join,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.loverHeroineIds.Contains(holy),"Retry joins and inherits relationship in one durable transaction");
        check(progression.Commit(join,g=>throw new Exception("repeat save"))==GrowthCommitResult.AlreadyCommitted,"Joining costume retries once");
        var swapped=clone(save);swapped.home.formationIds=new[]{normal}.Concat(c.FormationIds.Skip(1)).ToArray();HomeRules.Apply(swapped,home,new HomeOperation("formation",holy,owner:"1"));check(swapped.home.formationIds[1]==holy && !swapped.home.formationIds.Contains(normal) && swapped.home.formationIds.Select(home.PersonId).Distinct().Count()==5,"Alternate costume changes the existing person's party slot");
        check(home.weaponNodes.Count(n=>n.heroineId==normal)==13 && home.weaponNodes.Count(n=>n.heroineId==holy)==13,"Both costumes have independent complete weapon trees");
        foreach(string id in new[]{normal,holy})check(story.chapters.Where(x=>x.ownerId==id).Sum(x=>x.pages.Length)==30 && story.events.Count(x=>x.ownerId==id)==5,"Each form has thirty original pages and five events");
        Func<string,int,PlayableBattle> fixture=(id,slot)=>{var defs=read();foreach(var j in defs.jobs)j.hp=100000;var ids=defs.FormationIds;ids[slot]=id;var enemy=ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);enemy.baseHp=10000000;enemy.hpPerLevel=1;return new PlayableBattle(1,new PlayableProgress(),991,combatDefinitions:defs.WithFormation(ids),formalGrowth:growth,colossusDefinition:enemy,useJobRulesV2:true);};
        Action<PlayableBattle,int> ready=(b,a)=>{int limit=0;while(b.AvailableHero!=a && !b.Ended && limit++<500)b.Pass();check(!b.Ended && b.AvailableHero==a,"Form reaches own READY");};
        foreach(string id in new[]{normal,holy})for(int slot=0;slot<5;slot++){var b=fixture(id,slot);ready(b,slot);check(b.JobState(slot).Id==(id==normal?"job.fighter":"job.healer"),"Job follows form at every formation position");}
        var buff=fixture(holy,0);ready(buff,0);buff.State.Heroes[0].GainResource(10);long clock=buff.Clock;int gauge=buff.State.Heroes[0].JobResource;
        check(!buff.ActWithAllies(0,1,"body",new int[0]) && buff.Clock==clock && buff.State.Heroes[0].JobResource==gauge,"Missing support target neither spends life nor advances time");
        check(buff.ActWithAllies(0,1,"body",new[]{2}),"Support can select a full-health ally");check(buff.State.Heroes[2].TimedEffects.Any(e=>e.Kind=="attack" && e.Percent==40) && !buff.State.Heroes[1].TimedEffects.Any(e=>e.Kind=="attack"),"Single-ally support remains targeted");
        var cure=fixture(holy,0);ready(cure,0);cure.State.Heroes[1].TakeDamage(100);cure.State.Heroes[1].Status.Add(new EnemyStatusDef{kind="bleed",amount=150});int hurt=cure.State.Heroes[1].HitPoints;
        check(cure.ActWithAllies(0,0,"body",new[]{1}) && cure.State.Heroes[1].HitPoints>hurt && !cure.State.Heroes[1].Status.Active("bleed") && cure.State.Heroes[1].Status.Meter("bleed")==0,"Holy heal cleanses before healing, including residual status meter");
        var full=fixture(holy,0);ready(full,0);full.State.Heroes[2].Status.Add(new EnemyStatusDef{kind="sickness",amount=100});check(full.CanChooseAlly(0,0,2) && full.ActWithAllies(0,0,"body",new[]{2}) && !full.State.Heroes[2].Status.Active("sickness"),"Full-health ailment can be targeted for cleanse");
        var ultimate=fixture(holy,0);ready(ultimate,0);ultimate.State.Heroes[0].GainResource(10);foreach(var ally in ultimate.State.Heroes)ally.TakeDamage(50);int wounded=ultimate.State.Heroes[3].HitPoints;check(ultimate.Act(0,2,"body") && ultimate.State.Heroes[3].HitPoints>wounded,"Holy all-enemy magic also heals living allies");
        var life=fixture(holy,0);ready(life,0);life.State.Heroes[0].GainResource(10);check(life.UseLifeTool(0,"invest",-1) && life.State.Heroes[0].JobResourceMax==12,"Life investment increases encounter resource capacity");
        ready(life,0);life.State.Heroes[0].GainResource(20);var recipient=life.State.Heroes[1];recipient.TakeDamage(20);check(life.UseLifeTool(0,"overheal",1) && recipient.HitPoints>recipient.MaxHitPoints && recipient.HitPoints<=(long)recipient.MaxHitPoints*125/100,"Overheal exceeds maximum within 125 percent ceiling");
        int hp=recipient.HitPoints;recipient.Heal(1);check(recipient.HitPoints>=hp,"Normal healing preserves existing overheal");
        ready(life,0);life.State.Heroes[0].GainResource(20);check(life.UseLifeTool(0,"maxhp",1) && recipient.LifeMaxHpPercent==20,"Maximum HP life operation applies encounter-only increase");
        ready(life,0);life.State.Heroes[0].GainResource(20);recipient.TakeDamage(int.MaxValue);check(life.UseLifeTool(0,"revive",1) && recipient.IsAlive,"Life operation revives a fallen ally");
        var fresh=fixture(holy,0);check(fresh.State.Heroes[0].JobResourceMax==10 && fresh.State.Heroes[1].LifeMaxHpPercent==0,"Life investment and maximum HP never leak into next encounter");
        var n=fixture(normal,0);ready(n,0);n.State.Heroes[0].TakeDamage(100);int before=n.State.Heroes[0].HitPoints;check(n.Act(0,0,"body") && n.State.Heroes[0].HitPoints>before && n.State.Heroes[0].TimedEffects.Any(e=>e.Kind=="attack"),"Normal declaration heals and applies its attack buff");
        var charge=fixture(normal,0);ready(charge,0);charge.State.Heroes[0].GainResource(10);check(charge.SelectBoostUnits(0,5) && charge.SkillResourceCost(0,1)==3 && charge.SkillResourceCost(0,2)==5 && charge.SkillResourceCost(0,0)==0,"Normal boost consumption respects per-skill three/five caps");
        check(c.WeaponSkillSlot(normal)==1 && c.WeaponSkillSlot(holy)==2 && c.WeaponSkillSlot("heroine.r")==0,"Weapon affects attack slot without replacing normal declaration or holy healing");
        foreach(string id in new[]{normal,holy}){
            var ids=new[]{id}.Concat(c.FormationIds.Skip(1)).ToArray();var defs=c.WithFormation(ids);var equipment=clone(save).home;
            var plain=new PlayableBattle(1,new PlayableProgress(),991,combatDefinitions:defs,formalGrowth:growth,homeProgress:equipment,homeCatalog:home,useJobRulesV2:true);
            string node=id+".weapon.alpha";equipment.weaponNodeIds=equipment.weaponNodeIds.Union(new[]{node}).ToArray();equipment.weaponEquipment=new[]{new HomeWeaponEquipment{heroineId=id,nodeId=node}};
            var armed=new PlayableBattle(1,new PlayableProgress(),991,combatDefinitions:defs,formalGrowth:growth,homeProgress:equipment,homeCatalog:home,useJobRulesV2:true);
            ready(plain,0);ready(armed,0);plain.State.Heroes[0].GainResource(10);armed.State.Heroes[0].GainResource(10);int slot=c.WeaponSkillSlot(id);check(armed.PreviewDamage(0,slot,"body")>plain.PreviewDamage(0,slot,"body") && armed.SkillName(0,0)==plain.SkillName(0,0),"Weapon improves the attack and preserves support skill: "+id);
        }
        Console.WriteLine("PLAN10_ANNIHILATOR_PASS shared identity / independent forms / targeted support / life operations / narrative");
    }
}
