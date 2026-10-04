using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using NewAster.Core;
public static class Plan8EconomyMeasurements
{
    public static int Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
        var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(args[0]),json);
        var banner=JsonSerializer.Deserialize<FormalKinderBanner>(File.ReadAllText(args[1]),json);banner.Validate(combat.FormationIds);
        var samples=new List<object>();var heroes=combat.FormationIds.ToDictionary(id=>id,id=>0);int heroines=0,nectar=0,crystal=0;
        for(int seed=0;seed<20;seed++){
            var random=new Random(seed);int heroCount=0;var local=heroes.Keys.ToDictionary(id=>id,id=>0);
            for(int i=0;i<10000;i++){var outcome=banner.Draw(max=>random.Next(max));if(outcome.kind=="heroine"){heroCount++;heroines++;heroes[outcome.heroineId]++;local[outcome.heroineId]++;}else if(outcome.kind=="nectar")nectar++;else crystal++;}
            samples.Add(new{seed,draws=10000,heroCount,heroes=local});
        }
        int total=200000;double rate=heroines/(double)total,halfWidth=1.96*Math.Sqrt(rate*(1-rate)/total);
        int nectarCost=5*FormalProgression.LevelCost(1,120),crystalCost=400;
        var output=new{schemaVersion=1,scope="seeded statistical draws and arithmetic supply bounds; no simulated currency grants, not actual Lv120 arrival",bannerVersion=banner.contentVersion,stoneCost=300,tenDrawCost=3000,heroineRateBasisPoints=300,perHeroineRateBasisPoints=60,exchangePoints=100,pointsPerPaidDraw=1,statisticalDraws=total,observedHeroineRate=rate,approximate95PercentSamplingInterval=new[]{rate-halfWidth,rate+halfWidth},heroes,nectarOutcomes=nectar,crystalOutcomes=crystal,samples,budget=new{fiveHeroNectarTo120=nectarCost,fiveHeroCrystalsTo120=crystalCost,initialNectar=2940,initialCrystals=20,oneLevelHeroTo120=FormalProgression.LevelCost(1,120),bounds=new[]{1,10,50}.Select(level=>new{enemyLevel=level,nectarPerVictory=60+10*level,crystalsPerVictory=Math.Min(5,1+level/10),stonesPerVictory=30+2*level,materialPerVictory=10+level,nectarOnlyWinsWithoutGacha=(int)Math.Ceiling((nectarCost-2940d)/(60+10*level)),crystalOnlyWinsWithoutGacha=(int)Math.Ceiling((crystalCost-20d)/Math.Min(5,1+level/10)),hundredPaidDrawsWinsAfterOptional3000Intro=(int)Math.Ceiling(27000d/(30+2*level))}),qualification="constant enemy-level arithmetic bounds exclude access, combat survival, intermediate costs and real time; actual progression is the separately saved journey"},humanTimingMeasured=false,performanceMeasured=false};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2])));File.WriteAllText(args[2],JsonSerializer.Serialize(output,json));Console.WriteLine("PLAN8_ECONOMY_MEASUREMENTS_PASS draws="+total+" output="+Path.GetFullPath(args[2]));return 0;
    }
}
