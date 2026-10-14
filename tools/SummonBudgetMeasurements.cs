using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class SummonBudgetMeasurements
{
    public static void Main(string[] args)
    {
        var options=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
        var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(args[0]),options);combat.Validate();
        var rows=new List<object>();const int samples=20000;
        foreach(int forms in new[]{15,combat.HeroineIds.Length})foreach(bool owned in new[]{false,true}){
            var ids=combat.HeroineIds.Take(forms).ToArray();var banner=ProductionEconomyCatalog.Kinder(ids);banner.Validate(ids);
            var draws=Enumerable.Range(0,6).Select(_=>new List<int>()).ToArray();
            for(int seed=0;seed<samples;seed++){
                var random=new Random(seed);int paid=0,copies=owned?1:0,recorded=owned?1:0;
                while(copies<6){
                    paid++;if(banner.Draw(random.Next).heroineId==ids[0])copies++;
                    if(paid%FormalKinderBanner.ExchangeCost==0)copies++;
                    while(recorded<Math.Min(copies,6)){draws[recorded].Add(paid);recorded++;}
                }
            }
            for(int rank=owned?1:0;rank<=5;rank++){
                var sorted=draws[rank].OrderBy(v=>v).ToArray();int median=sorted[sorted.Length/2],p95=sorted[(int)Math.Ceiling(sorted.Length*.95)-1];
                rows.Add(new{forms,initiallyOwned=owned,duplicateRank=rank,traitRank=CombatTraitCatalog.Rank(rank),samples,meanDraws=sorted.Average(),medianDraws=median,p95Draws=p95,guaranteedDraws=(rank+(owned?0:1))*100,medianStones=median*300,medianEarnedStonesAfterIntro=Math.Max(0,median*300-3000),medianLevel10WinsAfterIntro=(int)Math.Ceiling(Math.Max(0,median*300-3000)/50d),medianLevel50WinsAfterIntro=(int)Math.Ceiling(Math.Max(0,median*300-3000)/130d),medianDaysAt300Stones=(int)Math.Ceiling(Math.Max(0,median*300-3000)/300d)});
            }
        }
        File.WriteAllText(args[1],JsonSerializer.Serialize(new{schemaVersion=1,scope="Actual current banner.Draw, equal form weights, 100-point target ticket immediately used; initial ownership separated; duplicate-only mastery unchanged",limitations="Seeded simulation, not observed user play. Intro gift applied once per hypothetical starting budget. Victory/day columns are alternative single-source cost conversions, not combined completion-time predictions. Free tickets and other overflow excluded.",rows},options));
        Console.WriteLine("SUMMON_BUDGET_PASS forms="+combat.HeroineIds.Length+" paths="+(samples*4));
    }
}
