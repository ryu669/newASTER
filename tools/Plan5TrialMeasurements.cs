using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan5TrialMeasurements
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=false};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=t=>JsonSerializer.Deserialize<FormalCampaignSave>(t,options);
        var catalog=CollectionContractFixture.Create(combat);var owner=catalog.owners[0];
        var hunts=new List<object>();
        foreach(int level in new[]{1,10,20,30,40}) {
            int attempts=0,drops=0,empty=0;
            for(int seed=0;seed<500;seed++) {
                var initial=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth"}};
                var session=new BattleCollectionSession(catalog,"measurement."+seed,owner.id,level,0,combat.FormationIds,seed);
                var request=new FormalBattleEndRequest(session.Finish(BattleEndReason.Victory,initial.world.poemIds,initial.world.unlockedStoryIds),0);
                var journal=new FormalCampaignJournal(initial,encode,decode);
                journal.CommitBattleEnd(request,catalog,w=>{var state=new CampaignState(WorldCatalog.ColossusIds,w);state.ClaimColossusVictory(owner.id,WorldCatalog.Colossi[0].EnvironmentTags,new VictoryReward(request.Id,level,10,4,Array.Empty<string>()),Array.Empty<StoryRequirement>(),Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);return state.CreateSave();},s=>true);
                var result=journal.Snapshot.collection.receipts.Single();attempts+=result.relicDrawCount;drops+=result.relicDrops.Length;if(result.relicDrops.Length==0)empty++;
            }
            double rate=(double)drops/attempts;check(rate>.68 && rate<.82,"Measured fixture per-attempt drop rate remains near 75 percent");
            hunts.Add(new{level,fights=500,attempts,drops,empty,rate});
        }
        var encounters=new List<object>();
        foreach(int heroLevel in new[]{1,10,30,60,120})foreach(int bossLevel in new[]{1,10,44,45,50}) {
            int wins=0,commands=0,songs=0;long ticks=0;double playback=0;
            for(int seed=0;seed<20;seed++) {
                var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=heroLevel,awakeningStage=heroLevel>80?2:heroLevel>50?1:0}).ToArray()};
                var battle=new PlayableBattle(bossLevel,new PlayableProgress(),seed,combatDefinitions:combat,formalGrowth:growth,colossusDefinition:ColossusCombatCatalog.Get(owner.id));
                int completed=0,steps=0;battle.CompletedEnemyAction=()=>completed++;
                while(!battle.Ended && steps++<20000) {
                    if(!battle.Act(battle.AvailableHero,0,"body"))battle.Pass();
                    foreach(var e in battle.DrainPresentationEvents())playback+=BattleVisualCue.Duration(e.Kind,e.Major);
                }
                check(battle.Ended && steps<20000,"Measured encounter finishes with normal commands");
                wins+=battle.State.IsVictory?1:0;commands+=steps;songs+=completed;ticks+=battle.Clock;
            }
            encounters.Add(new{heroLevel,bossLevel,runs=20,wins,meanCommands=commands/20d,meanSinging=songs/20d,meanTimelineTicks=ticks/20d,meanPlaybackSeconds=playback/20d});
        }
        string output=Path.Combine("tmp","plan5-trial-measurements.json");Directory.CreateDirectory("tmp");
        File.WriteAllText(output,JsonSerializer.Serialize(new{contentVersion=catalog.contentVersion,status="fixture-measurement",strategy="basic attacks only; no player thinking or UI input time",hunts,encounters},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("PLAN5_TRIAL_MEASUREMENTS "+Path.GetFullPath(output));
    }
}
