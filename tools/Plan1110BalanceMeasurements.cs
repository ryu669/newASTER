using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan1110BalanceMeasurements
{
    // A reproducible baseline, not a claim that this policy plays every job optimally.
    public static void Main(string[] args)
    {
        var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(args[0]),new JsonSerializerOptions{IncludeFields=true});
        combat.Validate();
        if(args.Length>4 && bool.Parse(args[4])){Progression(combat,args[1]);return;}
        if(args.Length>3 && bool.Parse(args[3])){Arsenal(combat,args[1]);return;}
        if(args.Length>2 && bool.Parse(args[2])){Equipment(combat,args[1]);return;}
        var rows=new List<object>();
        foreach(var candidate in combat.HeroineIds)
        {
            var formation=new List<string>{candidate};
            foreach(var support in new[]{"heroine.echidna","heroine.undermine","heroine.slayer","heroine.iconoclast","heroine.excalipan"})
                if(formation.Count<5 && formation.All(x=>combat.PersonId(x)!=combat.PersonId(support)))formation.Add(support);
            var selected=combat.WithFormation(formation.ToArray());
            foreach(var levels in new[]{new[]{1,1},new[]{10,10},new[]{30,30},new[]{50,50},new[]{120,50}})
            foreach(var enemy in WorldCatalog.ColossusIds)
            {
                int wins=0,caps=0,commands=0;long clock=0;var deaths=new int[5];
                for(int seed=0;seed<8;seed++)
                {
                    var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=levels[0],awakeningStage=levels[0]>80?2:levels[0]>50?1:0}).ToArray()};
                    var b=new PlayableBattle(levels[1],new PlayableProgress(),seed,combatDefinitions:selected,formalGrowth:growth,colossusDefinition:ProductionEconomyCatalog.Enemy(enemy),useJobRulesV2:true);
                    int steps=0;
                    while(!b.Ended && steps++<4000){Command(b);b.DrainPresentationEvents();}
                    if(b.State.IsVictory)wins++;if(!b.Ended)caps++;
                    commands+=steps;clock+=b.Clock;
                    for(int i=0;i<5;i++)if(!b.State.Heroes[i].IsAlive)deaths[i]++;
                }
                rows.Add(new{candidate,job=combat.Hero(candidate).jobId,formation,heroLevel=levels[0],enemyLevel=levels[1],enemy,seeds=8,wins,caps,meanCommands=commands/8.0,meanClock=clock/8.0,deaths});
            }
            Console.WriteLine("Measured "+candidate);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(args[1])??".");
        File.WriteAllText(args[1],JsonSerializer.Serialize(new{schemaVersion=1,definition=args[0],equipment="none; skill level 1; no duplicates",policy="candidate plus four distinct-person initial supports; heal below 65%; highest preview damage per cast+recovery; job utilities; intact parts first",limitations="Support composition varies to exclude the same person. Results compare complete parties, not isolated job strength. No claim of optimal play or final equipment balance.",runs=rows.Count*8,rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    static void Equipment(CombatDefinitionCatalog combat,string output)
    {
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json"),new JsonSerializerOptions{IncludeFields=true});
        var catalog=ProductionStoryCatalog.Collection(combat,story);var rows=new List<object>();
        foreach(var variant in new[]{"none","attack","ramp-before","ramp-after"})
        {
            var effect=catalog.Oopart("relic.special.ramp").effects[0];effect.scalingValue=variant=="ramp-before"?5:10;effect.maxValue=variant=="ramp-before"?20:60;
            foreach(int heroLevel in new[]{10,30,50})foreach(string enemy in WorldCatalog.ColossusIds)
            {
                int wins=0,caps=0;long clock=0;int commands=0;
                for(int seed=0;seed<16;seed++)
                {
                    var ledger=new FormalCollectionLedger{contentVersion=catalog.contentVersion,ooparts=new OopartInventorySave()};ledger.ooparts.SyncFormation(combat.FormationIds);
                    if(variant!="none") {string id="relic.special."+(variant=="attack"?"attack":"ramp");ledger.ooparts.progress=new[]{new OopartProgress{oopartId=id}};OopartService.Equip(ledger.ooparts,0,id);}
                    var growth=new FormalGrowthSave{saveId="balance",heroines=combat.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=heroLevel}).ToArray()};
                    var b=new PlayableBattle(50,new PlayableProgress(),seed,combatDefinitions:combat,formalGrowth:growth,colossusDefinition:ProductionEconomyCatalog.Enemy(enemy),useJobRulesV2:true,collectionGrowth:ledger,relicCatalog:catalog);
                    int steps=0;while(!b.Ended && steps++<4000){Command(b);b.DrainPresentationEvents();}
                    if(b.State.IsVictory)wins++;if(!b.Ended)caps++;clock+=b.Clock;commands+=steps;
                }
                rows.Add(new{variant,heroLevel,enemyLevel=50,enemy,seeds=16,wins,caps,meanClock=clock/16d,meanCommands=commands/16d});
            }
            Console.WriteLine("Measured equipment "+variant);
        }
        var curves=Enumerable.Range(0,31).Select(turn=>new{turn,before=Math.Min(20,turn*5),after=Math.Min(60,turn*10),constant=45}).ToArray();
        var economy=new[]{1,10,30,50}.Select(level=>{var reward=new FormalVictoryRequest("measure",WorldCatalog.ColossusIds[0],level,0);return new{enemyLevel=level,reward.Nectar,reward.Crystals,reward.Stones,level50PartyWins=(int)Math.Ceiling(5d*FormalProgression.LevelCost(1,50)/reward.Nectar),level120PartyWins=(int)Math.Ceiling(5d*FormalProgression.LevelCost(1,120)/reward.Nectar),threeMaxSkillsWins=(int)Math.Ceiling(3d*Enumerable.Range(1,6).Sum(HeroineSkillRules.UpgradeCost)/reward.Nectar),ringWins=(int)Math.Ceiling((double)AffectionRingService.Price/reward.Stones),materials=catalog.owners.First(o=>o.kind=="colossus").materialIds.Select(id=>new{id,amount=catalog.resources.Single(r=>r.id==id).DropAmount(level)})};}).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(output)??".");File.WriteAllText(output,JsonSerializer.Serialize(new{schemaVersion=1,runs=rows.Count*16,scope="initial formation, job v2, skill level 1, one slot with level 1 oopart and identical zero random rolls; no weapons; enemy Lv50; before and after simulated with same seeds",curves,economy,economyLimitations="Arithmetic victory counts exclude initial funds, gacha, daily/time rewards, access and survival. Ring also has daily and active-play income.",rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    static void Arsenal(CombatDefinitionCatalog combat,string output)
    {
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json"),new JsonSerializerOptions{IncludeFields=true});
        var catalog=ProductionStoryCatalog.Collection(combat,story);var home=ProductionStoryCatalog.Home(combat,story);
        var rows=new List<object>();var nodes=new List<object>();var effects=new List<object>();
        foreach(var candidate in combat.HeroineIds)
        {
            var formation=new List<string>{candidate};foreach(var support in new[]{"heroine.echidna","heroine.undermine","heroine.slayer","heroine.iconoclast","heroine.excalipan"})if(formation.Count<5 && formation.All(x=>combat.PersonId(x)!=combat.PersonId(support)))formation.Add(support);
            var selected=combat.WithFormation(formation.ToArray());
            var growth=new FormalGrowthSave{saveId="balance",heroines=combat.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=10}).ToArray()};
            var variants=new List<(string id,HomeWeaponNode node,int level,string oopart)>();variants.Add(("none",null,1,null));
            foreach(var n in home.weaponNodes.Where(n=>n.heroineId==candidate && !n.initial && new[]{1,4}.Contains(WeaponGrowthRules.Tier(n))))foreach(int level in new[]{1,7})
            {
                variants.Add((n.id+".lv"+level,n,level,null));
                nodes.Add(new{candidate,n.id,route=WeaponGrowthRules.Branch(n),tier=WeaponGrowthRules.Tier(n),level,attack=WeaponGrowthRules.Attack(n,level),power=WeaponGrowthRules.Power(n,level),physical=WeaponGrowthRules.Physical(n,level),magic=WeaponGrowthRules.Magic(n,level),speed=WeaponGrowthRules.Speed(n,level),n.uniqueAbilityKind,n.uniqueAbilityPercent,unlock=n.costs,upgrade=Enumerable.Range(1,level-1).SelectMany(l=>WeaponGrowthRules.Costs(n,l,home)).GroupBy(c=>c.resourceId).Select(g=>new{material=g.Key,amount=g.Sum(c=>c.amount),winsAt50=(int)Math.Ceiling((double)g.Sum(c=>c.amount)/catalog.resources.Single(r=>r.id==g.Key).DropAmount(50))})});
            }
            foreach(var d in catalog.oopartDefs){variants.Add((d.id,null,1,d.id));var ctx=new OopartEffectContext{jobId=combat.Hero(candidate).jobId};effects.Add(new{candidate,d.id,values=d.effects.Select(e=>new{e.effectType,e.targetId,e.scalingType,e.baseValue,e.scalingValue,e.maxValue,value=OopartEffectEngine.Value(e,ctx)})});}
            foreach(var v in variants)
            {
                // Representative physical, fire and integration enemies. Same seeds for every variant.
                foreach(var enemy in new[]{"colossus.red-crystal-tyrant","colossus.final-flame-ice-phoenix","colossus.newborn-asteria"})
                {
                    int wins=0,caps=0,stepsTotal=0;long clock=0,winningClock=0;int candidateDeaths=0;
                    for(int seed=0;seed<4;seed++)
                    {
                        var progress=FormalHomeProgress.Empty(home.contentVersion);progress.formationIds=formation.ToArray();
                        if(v.node!=null){progress.weaponNodeIds=home.weaponNodes.Where(n=>n.heroineId==candidate).Select(n=>n.id).ToArray();progress.weaponEquipment=new[]{new HomeWeaponEquipment{heroineId=candidate,nodeId=v.node.id}};progress.weaponLevels=new[]{new HomeWeaponLevel{nodeId=v.node.id,level=v.level}};}
                        var ledger=new FormalCollectionLedger{contentVersion=catalog.contentVersion,ooparts=new OopartInventorySave()};ledger.ooparts.SyncFormation(formation.ToArray());
                        if(v.oopart!=null){var d=catalog.Oopart(v.oopart);var roll=new StatValues();foreach(var r in d.randomStats)roll.Set(r.stat,(r.maximum+4)/5);ledger.ooparts.progress=new[]{new OopartProgress{oopartId=d.id,accumulatedRandomStats=roll}};OopartService.Equip(ledger.ooparts,0,d.id);}
                        var b=new PlayableBattle(50,new PlayableProgress(),seed,combatDefinitions:selected,formalGrowth:growth,colossusDefinition:ProductionEconomyCatalog.Enemy(enemy),useJobRulesV2:true,collectionGrowth:ledger,relicCatalog:catalog,homeProgress:progress,homeCatalog:home);
                        int steps=0;while(!b.Ended && steps++<4000){Command(b);b.DrainPresentationEvents();}if(b.State.IsVictory){wins++;winningClock+=b.Clock;}if(!b.Ended)caps++;if(!b.State.Heroes[0].IsAlive)candidateDeaths++;clock+=b.Clock;stepsTotal+=steps;
                    }
                    rows.Add(new{candidate,job=combat.Hero(candidate).jobId,variant=v.id,enemy,seeds=4,wins,caps,candidateDeaths,meanClock=clock/4d,meanWinningClock=wins==0?(double?)null:winningClock/(double)wins,meanCommands=stepsTotal/4d});
                }
            }
            Console.WriteLine("Measured arsenal "+candidate);
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{schemaVersion=1,runs=rows.Count*4,scope="15 candidate forms plus four distinct initial supports; Lv10 against Lv50; three representative enemies; slot0 only; weapon tiers1/4 levels1/7; all42 ooparts Lv1 with identical 20% random stats; no combinations yet; four seeds exploratory, not statistical proof",nodes,effects,rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    static void Progression(CombatDefinitionCatalog combat,string output)
    {
        var story=JsonSerializer.Deserialize<ProductionStoryContent>(File.ReadAllText("game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json"),new JsonSerializerOptions{IncludeFields=true});
        var collection=ProductionStoryCatalog.Collection(combat,story);var home=ProductionStoryCatalog.Home(combat,story);var banner=ProductionEconomyCatalog.Kinder(combat.HeroineIds);
        var duplicateSamples=Enumerable.Range(0,5).Select(_=>new List<int>()).ToArray();
        for(int seed=0;seed<2000;seed++){
            var rng=new Random(seed);int copies=0,paid=0,next=1;while(copies<5){paid++;if(banner.Draw(rng.Next).heroineId==combat.HeroineIds[0])copies++;if(paid%FormalKinderBanner.ExchangeCost==0)copies++;while(next<=Math.Min(copies,5)){duplicateSamples[next-1].Add(paid);next++;}}
        }
        var duplicateBudget=duplicateSamples.Select((s,i)=>{var ordered=s.OrderBy(x=>x).ToArray();return new{duplicateRank=i+1,traitRank=CombatTraitCatalog.Rank(i+1),samples=ordered.Length,meanPaidDraws=s.Average(),medianPaidDraws=ordered[ordered.Length/2],p95PaidDraws=ordered[(int)(ordered.Length*.95)],guaranteedPaidDraws=(i+1)*FormalKinderBanner.ExchangeCost,guaranteedStones=(i+1)*FormalKinderBanner.ExchangeCost*FormalKinderBanner.StoneCost};}).ToArray();
        var weaponBudget=new List<object>();
        foreach(var n in home.weaponNodes.Where(n=>!n.initial && new[]{1,4}.Contains(WeaponGrowthRules.Tier(n))))foreach(int level in new[]{1,7}){
            var ancestors=new HashSet<string>();void Visit(HomeWeaponNode node){if(!ancestors.Add(node.id))return;foreach(string parent in node.parentIds)Visit(home.weaponNodes.Single(x=>x.id==parent));}Visit(n);
            var costs=home.weaponNodes.Where(x=>ancestors.Contains(x.id)).SelectMany(x=>x.costs).Concat(Enumerable.Range(1,level-1).SelectMany(l=>WeaponGrowthRules.Costs(n,l,home))).GroupBy(c=>c.resourceId).Select(g=>new{material=g.Key,owner=collection.resources.Single(r=>r.id==g.Key).ownerId,amount=g.Sum(c=>c.amount),wins=(int)Math.Ceiling((double)g.Sum(c=>c.amount)/collection.resources.Single(r=>r.id==g.Key).DropAmount(50))}).ToArray();
            weaponBudget.Add(new{n.heroineId,n.id,level,costs,winsAtEnemy50=costs.GroupBy(c=>c.owner).Sum(g=>g.Max(c=>c.wins))});
        }
        var huntBudget=collection.rewardBands.Select(b=>new{b.ownerId,b.minLevel,b.maxLevel,draws=b.draws,pool=b.relicIds.Length,expectedWinsForOneSpecific=1d/(1-Math.Pow(1-.35/b.relicIds.Length,b.draws)),p95WinsForOneSpecific=(int)Math.Ceiling(Math.Log(.05)/(b.draws*Math.Log(1-.35/b.relicIds.Length)))}).ToArray();
        var rows=new List<object>();var masteries=new List<object>();
        foreach(string candidate in combat.HeroineIds){
            var formation=new List<string>{candidate};foreach(string support in new[]{"heroine.echidna","heroine.undermine","heroine.slayer","heroine.iconoclast","heroine.excalipan"})if(formation.Count<5 && formation.All(x=>combat.PersonId(x)!=combat.PersonId(support)))formation.Add(support);
            var selected=combat.WithFormation(formation.ToArray());var set=CombatTraitCatalog.Resolve(combat.Hero(candidate));masteries.Add(new{candidate,set.masteryTraitId,effects=CombatTraitCatalog.Get(set.masteryTraitId).effects});
            foreach(int rank in new[]{0,1,4,5})foreach(string equipment in new[]{"none","affinity","weapon-and-affinity"})foreach(string enemy in new[]{"colossus.red-crystal-tyrant","colossus.final-flame-ice-phoenix","colossus.newborn-asteria"}){
                int wins=0,caps=0;long winningClock=0;
                for(int seed=0;seed<8;seed++){
                    var growth=new FormalGrowthSave{saveId="balance",heroines=combat.HeroineIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=10,duplicateRank=h==candidate?rank:0}).ToArray()};
                    var progress=FormalHomeProgress.Empty(home.contentVersion);progress.formationIds=formation.ToArray();
                    if(equipment=="weapon-and-affinity"){var n=home.weaponNodes.Single(x=>x.heroineId==candidate && WeaponGrowthRules.Route(x)==0 && WeaponGrowthRules.Tier(x)==4);progress.weaponNodeIds=home.weaponNodes.Where(x=>x.heroineId==candidate).Select(x=>x.id).ToArray();progress.weaponEquipment=new[]{new HomeWeaponEquipment{heroineId=candidate,nodeId=n.id}};progress.weaponLevels=new[]{new HomeWeaponLevel{nodeId=n.id,level=7}};}
                    var ledger=new FormalCollectionLedger{contentVersion=collection.contentVersion,ooparts=new OopartInventorySave()};ledger.ooparts.SyncFormation(formation.ToArray());
                    if(equipment!="none"){string id="relic."+combat.Hero(candidate).jobId+".affinity";var d=collection.Oopart(id);var roll=new StatValues();foreach(var r in d.randomStats)roll.Set(r.stat,(r.maximum+4)/5);ledger.ooparts.progress=new[]{new OopartProgress{oopartId=id,accumulatedRandomStats=roll}};OopartService.Equip(ledger.ooparts,0,id);}
                    var b=new PlayableBattle(50,new PlayableProgress(),seed,combatDefinitions:selected,formalGrowth:growth,colossusDefinition:ProductionEconomyCatalog.Enemy(enemy),useJobRulesV2:true,collectionGrowth:ledger,relicCatalog:collection,homeProgress:progress,homeCatalog:home);int steps=0;while(!b.Ended && steps++<4000){Command(b);b.DrainPresentationEvents();}if(b.State.IsVictory){wins++;winningClock+=b.Clock;}if(!b.Ended)caps++;
                }
                rows.Add(new{candidate,duplicateRank=rank,traitRank=CombatTraitCatalog.Rank(rank),equipment,enemy,seeds=8,wins,caps,meanWinningClock=wins==0?(double?)null:winningClock/(double)wins});
            }
            Console.WriteLine("Measured progression "+candidate);
        }
        File.WriteAllText(output,JsonSerializer.Serialize(new{schemaVersion=1,runs=rows.Count*8,scope="Lv10 vs Lv50; candidate duplicate ranks0/1/4/5; four supports rank0; includes both common traits and inherent duplicate stat gains, not mastery alone; minimum random rolls; exploratory common policy",duplicateBudget,duplicateBudgetLimitations="Target already owned; 2000 seeded paths; every100 paid draws exchanged for target ticket; no other max-rank overflow or free tickets; no real-time estimate",weaponBudget,huntBudget,masteries,rows},new JsonSerializerOptions{WriteIndented=true}));
    }
    public static void Command(PlayableBattle b)
    {
        int a=b.AvailableHero;if(a<0){b.Pass();return;}
        var h=b.State.Heroes[a];var j=b.JobState(a);string target=b.State.Parts.FirstOrDefault(p=>!p.IsBroken)?.Id??"body";
        if(b.RequiresPanzerDefense(a)){b.DefendPanzer(a);return;}
        if(j.Id=="job.gambler"){b.SpinGamblerSlot(a,target);return;}
        b.ActivateJobGauge(a);
        if(j.Id=="job.fighter")b.SelectBoostUnits(a,h.JobResource);
        if(j.Id=="job.chaser")b.SelectChaserGear(a,h.JobResource>=4?3:h.JobResource>=2?2:1);
        if(j.Id=="job.sniper" && b.StartSniperMode(a,target))return;
        if(j.Id=="job.gunner" && j.Magazines[j.SelectedMagazine]==0){if(j.Magazines[1-j.SelectedMagazine]>0)b.SelectMagazine(a,1-j.SelectedMagazine);else {b.Reload(a);return;}}
        int[] allies=Enumerable.Range(0,5).Where(i=>b.State.Heroes[i].IsAlive).OrderBy(i=>(double)b.State.Heroes[i].HitPoints/b.State.Heroes[i].MaxHitPoints).ToArray();
        foreach(int slot in Enumerable.Range(0,3))
        {
            var heal=b.HealingSkill(a,slot);
            if(heal==null || b.IsSelfBuff(a,slot) || !b.ConditionsSatisfied(a,slot) || b.SkillResourceCost(a,slot)>h.JobResource)continue;
            if(allies.Length>0 && b.State.Heroes[allies[0]].HitPoints<.65*b.State.Heroes[allies[0]].MaxHitPoints)
            {
                var selected=allies.Take(heal.TargetCount).ToArray();
                if(b.ActWithAllies(a,slot,target,selected))return;
            }
        }
        foreach(int slot in Enumerable.Range(0,3).Where(s=>b.IsAttackSkill(a,s) && b.ConditionsSatisfied(a,s) && b.SkillResourceCost(a,s)<=h.JobResource).OrderByDescending(s=>(double)b.PreviewDamage(a,s,target)/Math.Max(1,b.CastDelay(a,s)+b.CommandRecoveryDelay(a,s))))
            if(b.Act(a,slot,target))return;
        b.Pass();
    }
}
