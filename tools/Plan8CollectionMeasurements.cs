using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan8CollectionMeasurements
{
    public sealed class Result
    {
        public int seed,battles,wins,defeats,stepCaps,singing,duplicates,missingEnemyPoems;public string selector,strategy;
        public int? firstEnemyChapter,allEnemyPoems;public Dictionary<string,int?> heroineFirstChapters=new Dictionary<string,int?>();
        public double scheduledPlaybackSeconds;
    }
    public static int Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
        var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(args[0]),json);
        var story=JsonSerializer.Deserialize<TrialStoryContent>(File.ReadAllText(args[1]),json);var catalog=TrialStoryCatalog.Collection(combat,story);
        var results=new List<Result>();
        foreach(bool prefer in new[]{false,true})foreach(string strategy in new[]{"basic-fast","wait-two-songs"})for(int seed=0;seed<20;seed++){
            var row=new Result{seed=seed,selector=prefer?"missing-priority":"uniform",strategy=strategy};
            foreach(var hero in combat.FormationIds)row.heroineFirstChapters.Add(hero,null);
            var owned=new HashSet<string>();var chapters=new HashSet<string>();var songs=catalog.owners.Single(o=>o.id==story.colossusId).poemIds;
            while(row.battles<120 && row.allEnemyPoems==null){
                int battleSeed=seed*10000+row.battles;row.battles++;
                var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()};
                var definition=ColossusCombatCatalog.GetPlan8Trial(story.colossusId);
                var battle=new PlayableBattle(1,new PlayableProgress(),battleSeed,combatDefinitions:combat,formalGrowth:growth,colossusDefinition:definition);
                var session=new BattleCollectionSession(catalog,"collection."+seed+"."+row.battles,story.colossusId,1,0,combat.FormationIds,battleSeed,definition.contentVersion);
                var random=new Random(unchecked(battleSeed^0x534F4E47));int completed=0;
                battle.CompletedEnemyAction=()=>{string poem=TrialSingingSelector.Select(catalog,story.colossusId,combat.FormationIds,owned,session.Snapshot.heardPoemIds,random,prefer);if(owned.Contains(poem)||session.Snapshot.heardPoemIds.Contains(poem))row.duplicates++;session.RecordCompletedSinging(poem);completed++;row.singing++;};
                int steps=0;while(!battle.Ended && steps++<20000){if(strategy=="wait-two-songs" && completed<2 || !battle.Act(battle.AvailableHero,0,"body"))battle.Pass();foreach(var e in battle.DrainPresentationEvents())row.scheduledPlaybackSeconds+=BattleVisualCue.Duration(e.Kind,e.Major);}
                if(!battle.Ended){row.stepCaps++;break;}
                var reason=battle.State.IsVictory?BattleEndReason.Victory:BattleEndReason.Defeat;if(reason==BattleEndReason.Victory)row.wins++;else row.defeats++;
                var receipt=session.Finish(reason,owned,chapters);owned.UnionWith(receipt.acquiredPoemIds);chapters.UnionWith(receipt.unlockedChapterIds);
                if(row.firstEnemyChapter==null && chapters.Contains(TrialStoryCatalog.Id(story.chapters[0].id)))row.firstEnemyChapter=row.battles;
                if(songs.All(owned.Contains))row.allEnemyPoems=row.battles;
                foreach(var hero in combat.FormationIds)if(row.heroineFirstChapters[hero]==null && chapters.Contains(TrialStoryCatalog.Id(story.chapters.Single(c=>c.ownerId==hero).id)))row.heroineFirstChapters[hero]=row.battles;
            }
            row.missingEnemyPoems=songs.Count(id=>!owned.Contains(id));results.Add(row);
        }
        double? Quantile(IEnumerable<int?> source,double fraction){var values=source.Where(v=>v.HasValue).Select(v=>v.Value).OrderBy(v=>v).ToArray();return values.Length==0?(double?)null:values[(int)Math.Ceiling(values.Length*fraction)-1];}
        var summary=results.GroupBy(r=>new{r.selector,r.strategy}).Select(g=>new{g.Key.selector,g.Key.strategy,runs=g.Count(),censored=g.Count(r=>r.allEnemyPoems==null),firstChapterMedianBattles=Quantile(g.Select(r=>r.firstEnemyChapter),.5),firstChapterP90Battles=Quantile(g.Select(r=>r.firstEnemyChapter),.9),allPoemsMedianBattles=Quantile(g.Select(r=>r.allEnemyPoems),.5),allPoemsP90Battles=Quantile(g.Select(r=>r.allEnemyPoems),.9),duplicateRate=(double)g.Sum(r=>r.duplicates)/g.Sum(r=>r.singing),heroines=combat.FormationIds.Select(id=>new{id,censored=g.Count(r=>r.heroineFirstChapters[id]==null),medianBattles=Quantile(g.Select(r=>r.heroineFirstChapters[id]),.5),p90Battles=Quantile(g.Select(r=>r.heroineFirstChapters[id]),.9)})});
        var output=new{schemaVersion=1,scope="actual completed battle actions and collection contracts; automated not human timing",enemyVersion=ColossusCombatDef.Plan8Version,heroLevel=1,enemyLevel=1,gear="none",duplicateRank=0,battleCap=120,stepCap=20000,seedRule="seed*10000 + zero-based battle index; song RNG = battle seed XOR 0x534F4E47",timeMedianSeconds=(double?)null,timeP90Seconds=(double?)null,quantiles="nearest rank among completed runs; censored reported separately",summary,results};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2])));File.WriteAllText(args[2],JsonSerializer.Serialize(output,json));Console.WriteLine("PLAN8_COLLECTION_MEASUREMENTS_PASS runs="+results.Count+" output="+Path.GetFullPath(args[2]));return 0;
    }
}
