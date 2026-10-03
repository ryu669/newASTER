using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using NewAster.Core;
using NewAster.Data;

public static class Plan8BattleMeasurements
{
    private static readonly JsonSerializerOptions Json=new JsonSerializerOptions{IncludeFields=true,WriteIndented=true};
    public static int Main(string[] args)
    {
        var combat=JsonSerializer.Deserialize<CombatDefinitionCatalog>(File.ReadAllText(args[0]),Json);combat.Validate();
        int seeds=int.Parse(args[2]),damageSlope=int.Parse(args[3]),hpSlope=int.Parse(args[4]);
        if(seeds!=2 && seeds!=20)throw new ArgumentException("Pilot uses two seeds; full matrix uses twenty.");
        var definition=args[5]=="True"?ColossusCombatCatalog.GetPlan8Trial(WorldCatalog.ColossusIds[0]):ColossusCombatCatalog.Get(WorldCatalog.ColossusIds[0]);
        if(damageSlope>=0)definition.damagePerLevel=damageSlope;
        if(hpSlope>=0)definition.hpPerLevel=hpSlope;definition.Validate();
        var rows=new List<object>();var runs=new List<Run>();
        foreach(int hero in new[]{1,10,30,60,120})foreach(int enemy in new[]{1,10,44,45,50})foreach(string policy in new[]{"basic-body","basic-parts","resource-parts"}){
            var group=new List<Run>();for(int seed=0;seed<seeds;seed++)group.Add(Fight(combat,definition,hero,enemy,policy,seed));
            runs.AddRange(group);rows.Add(new{heroLevel=hero,enemyLevel=enemy,policy,runs=seeds,wins=group.Count(r=>r.outcome=="victory"),defeats=group.Count(r=>r.outcome=="defeat"),censored=group.Count(r=>r.outcome=="step-cap"),meanCommands=group.Average(r=>r.commands),meanSinging=group.Average(r=>r.completedEnemyActions),meanTimelineTicks=group.Average(r=>(double)r.timelineTicks),meanScheduledPlaybackSeconds=group.Average(r=>r.scheduledPlaybackSeconds),meanRemainingHpFraction=group.Average(r=>r.remainingHpFraction)});
        }
        var output=new{schemaVersion=1,scope="deterministic-functional-simulation; not-performance-or-human-timing",phase=seeds==2?"pilot":"full-matrix",combatVersion=combat.schemaVersion,definition,heroLevels=new[]{1,10,30,60,120},enemyLevels=new[]{1,10,44,45,50},seeds=Enumerable.Range(0,seeds).ToArray(),stepCap=20000,equipment="none",duplicateRank=0,awakening="0 for Lv1/10/30, 1 for Lv60, 2 for Lv120; scenario precondition, not initial distribution",policies=new[]{"basic-body: slot0 body; pass only if unavailable/rejected","basic-parts: slot0 first intact gauge, attack, armor, drain; then body","resource-parts: same part order; heal weakest below 65% if usable; self buff every fourth owner command; highest preview damage among usable attack skills; fallback slot0/pass"},totalRuns=runs.Count,rows,runs};
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));File.WriteAllText(args[1],JsonSerializer.Serialize(output,Json));
        Console.WriteLine("PLAN8_BATTLE_MATRIX_PASS runs="+runs.Count+" output="+Path.GetFullPath(args[1]));return 0;
    }
    public sealed class Run
    {
        public int heroLevel,enemyLevel,seed,commands,rejected,completedEnemyActions,brokenParts;
        public string policy,outcome,inputSha256;
        public long timelineTicks;public double scheduledPlaybackSeconds,remainingHpFraction;
        public int[] skillUses=new int[15];
    }
    private static Run Fight(CombatDefinitionCatalog combat,ColossusCombatDef definition,int heroLevel,int enemyLevel,string policy,int seed)
    {
        var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=heroLevel,awakeningStage=heroLevel>80?2:heroLevel>50?1:0}).ToArray()};
        var battle=new PlayableBattle(enemyLevel,new PlayableProgress(),seed,combatDefinitions:combat,formalGrowth:growth,colossusDefinition:definition);
        var run=new Run{heroLevel=heroLevel,enemyLevel=enemyLevel,seed=seed,policy=policy};var inputs=new StringBuilder();var ownerCommands=new int[5];
        battle.CompletedEnemyAction=()=>run.completedEnemyActions++;
        while(!battle.Ended && run.commands<20000){
            int actor=battle.AvailableHero,slot=0,ally=-1;string target="body";
            if(policy!="basic-body")target=battle.State.Parts.FirstOrDefault(p=>!p.IsBroken)?.Id??"body";
            if(actor>=0 && policy=="resource-parts"){
                int weakest=Enumerable.Range(0,5).Where(i=>battle.State.Heroes[i].IsAlive).OrderBy(i=>(double)battle.State.Heroes[i].HitPoints/battle.State.Heroes[i].MaxHitPoints).First();
                for(int s=0;s<3;s++)if(battle.HealingSkill(actor,s)!=null && battle.State.Heroes[weakest].HitPoints*100L<battle.State.Heroes[weakest].MaxHitPoints*65L && battle.PreviewHealing(actor,weakest,s)>0){slot=s;ally=weakest;break;}
                if(ally<0){
                    var usable=Enumerable.Range(0,3).Where(s=>battle.ConditionsSatisfied(actor,s) && battle.State.Heroes[actor].JobResource>=battle.SkillResourceCost(actor,s)).ToArray();
                    int buff=usable.FirstOrDefault(s=>battle.IsSelfBuff(actor,s));
                    if(ownerCommands[actor]%4==0 && battle.IsSelfBuff(actor,buff))slot=buff;
                    else slot=usable.Where(s=>battle.IsAttackSkill(actor,s)).OrderByDescending(s=>battle.PreviewDamage(actor,s,target)).FirstOrDefault();
                }
            }
            bool accepted=actor>=0 && battle.Act(actor,slot,target,ally);
            inputs.Append(actor).Append(':').Append(slot).Append(':').Append(target).Append(':').Append(ally).Append(':').Append(accepted).Append('\n');
            if(accepted){run.skillUses[actor*3+slot]++;ownerCommands[actor]++;}else{run.rejected++;battle.Pass();}
            run.commands++;foreach(var e in battle.DrainPresentationEvents())run.scheduledPlaybackSeconds+=BattleVisualCue.Duration(e.Kind,e.Major);
        }
        run.outcome=battle.State.IsVictory?"victory":battle.Ended?"defeat":"step-cap";run.timelineTicks=battle.Clock;run.brokenParts=battle.State.Parts.Count(p=>p.IsBroken);
        run.remainingHpFraction=(double)battle.State.Heroes.Sum(h=>h.HitPoints)/battle.State.Heroes.Sum(h=>h.MaxHitPoints);
        using(var hash=SHA256.Create())run.inputSha256=Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(inputs.ToString())));
        return run;
    }
}
