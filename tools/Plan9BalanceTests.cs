using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan9BalanceTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        var rows=new List<object>();int failures=0;
        foreach(string id in WorldCatalog.ColossusIds)foreach(int heroLevel in new[]{1,10,30,120})foreach(int enemyLevel in new[]{1,10,45,50}){
            int wins=0,caps=0,commands=0;
            for(int seed=0;seed<8;seed++){
                var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(h=>new FormalHeroineGrowth{heroineId=h,level=heroLevel,awakeningStage=heroLevel>80?2:heroLevel>50?1:0}).ToArray()};
                var battle=new PlayableBattle(enemyLevel,new PlayableProgress(),seed,combatDefinitions:combat,formalGrowth:growth,colossusDefinition:ProductionEconomyCatalog.Enemy(id));int steps=0;
                while(!battle.Ended && steps++<4000){int actor=battle.AvailableHero;string target=battle.State.Parts.FirstOrDefault(p=>!p.IsBroken)?.Id??"body";if(!battle.Act(actor,0,target))battle.Pass();battle.DrainPresentationEvents();}
                if(battle.State.IsVictory)wins++;if(!battle.Ended)caps++;commands+=steps;
            }
            bool gate=caps==0 && (enemyLevel!=1 || heroLevel!=1 || wins>=7) && (enemyLevel!=50 || heroLevel!=1 || wins<=2) && (enemyLevel!=50 || heroLevel<30 || wins>=6);
            if(!gate)failures++;rows.Add(new{id,heroLevel,enemyLevel,seeds=8,wins,caps,meanCommands=commands/8.0,accepted=gate});
        }
        Directory.CreateDirectory("tmp");File.WriteAllText("tmp/plan9-balance.json",JsonSerializer.Serialize(new{schemaVersion=1,scope="deterministic functional difficulty; no performance or human timing",definition=ProductionEconomyCatalog.Version,seeds=Enumerable.Range(0,8),equipment="none",policy="basic intact parts then body",criteria="Lv1 versus Lv1 >=7/8; Lv1 versus Lv50 <=2/8; Lv30+ versus Lv50 >=6/8; no command caps",runs=1920,failures,rows},new JsonSerializerOptions{WriteIndented=true}));
        check(failures==0,"All fifteen production enemies meet declared difficulty and reachability gates; see tmp/plan9-balance.json");
    }
}
