using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PlayableBuild
{
    private static PlayableBattle LegacyBattle(int level,PlayableProgress progress,int seed=1,System.Collections.Generic.IEnumerable<HealingSkillDefinition> definitions=null) => new PlayableBattle(level,progress,seed,definitions,false);
    private static int assertions;
    private static void Check(bool condition,string message) { assertions++; if(!condition) throw new Exception(message); }
    public static void ValidateAndBuild()
    {
        Validate();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[] { "Assets/Game/Scenes/Bootstrap.unity" },
            locationPathName="../Builds/playable/newASTER.exe",
            target=BuildTarget.StandaloneWindows64, options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("PLAYABLE_BUILD_PASS "+assertions+" assertions / "+report.summary.totalSize+" bytes");
    }
    public static void Validate()
    {
        assertions=0;
        ValidatePlayback();
        ValidateVisualCues();
        ValidateSlayerModel();
        ValidateTimeline();
        ValidateDistinctSupport();
        ValidateEncounterPhase();
        ValidateBattleDecisions();
        var c=new CampaignState(WorldCatalog.ColossusIds);
        var first=WorldCatalog.Colossi[0];
        Check(!c.Playable.Train(c.Progress,0),"Training without funds must fail");
        var b=LegacyBattle(1,c.Playable);
        Check(b.Act(0,0,"body"),"First action"); Check(!b.Act(0,0,"body"),"Double action blocked");
        for(int rounds=0;!b.Ended && rounds<50;rounds++) {
            int turn=b.Turn;
            for(int h=0;h<5 && !b.Ended && turn==b.Turn;h++) {
                if(b.Acted[h]) continue;
                var part=b.State.Parts.FirstOrDefault(p=>!p.IsBroken);
                int slot=b.State.Heroes[h].JobResource>=3 && b.HealingSkill(h,1)==null?1:0;
                Check(b.Act(h,slot,part?.Id??"body"),"Full progression strategy executes a valid skill");
            }
        }
        Check(b.State.IsVictory,"Starter party must win Lv1 with part strategy");
        for(int i=0;i<6;i++) {
            var poems=GreenReturnDragonVerticalSlice.PoemIds.Where(id=>!c.Progress.CollectedPoemIds.Contains(id)).Take(4);
            var r=new VictoryReward("test-"+i,1,10,4,poems);
            var claim=c.ClaimColossusVictory(first.Id,first.EnvironmentTags,r,GreenReturnDragonVerticalSlice.StoryChapters,Array.Empty<TerraformingMilestone>(),GardenCatalog.Requirements);
            Check(claim.Reward.Claimed,"Reward granted");
            int money=c.Progress.Materials;
            Check(!c.ClaimColossusVictory(first.Id,first.EnvironmentTags,r,GreenReturnDragonVerticalSlice.StoryChapters,null,GardenCatalog.Requirements).Reward.Claimed && c.Progress.Materials==money,"No duplicate reward");
        }
        Check(c.Progress.CollectedPoemIds.Count==24 && c.Progress.UnlockedStoryIds.Count==3,"All poem chapters unlock");
        Check(c.Gardens.UnlockedGardenIds.Count>0,"Garden unlock");
        Check(c.Playable.Train(c.Progress,0),"Train"); Check(c.Playable.Grow(c.Progress,0,0),"Weapon growth");
        Check(c.Playable.Craft(c.Progress,0),"Furniture craft"); Check(c.Playable.Place(0,2),"Furniture place");
        Check(c.Playable.Place(0,1) && c.Playable.Slots[2]==-1,"Furniture moves instead of duplicating");
        Check(c.Playable.Visit(c.Progress,0),"Affection");
        Check(c.Playable.TryKinderDraw(.02m,.99m,out var drawnHeroine,out var drawnIndex) && drawnHeroine && drawnIndex==4,"Kinder Garden 3% heroine draw");
        Check(c.Playable.KinderStones==9 && c.Playable.Duplicates[4]==1,"Kinder Garden result persists in progress");
        c.Progress.MarkStoryRead(GreenReturnDragonVerticalSlice.StoryChapters[0].StoryId);
        c.Playable.RecordVictory(45);
        var restored=new CampaignState(WorldCatalog.ColossusIds,JsonUtility.FromJson<CampaignSaveV2>(JsonUtility.ToJson(c.CreateSave())));
        Check(restored.Playable.Levels[0]==2 && restored.Playable.Branches[0]==1 && restored.Playable.Affections[0]==5 && restored.Playable.Slots[1]==0 && restored.Playable.HighestLevel==50 && restored.Playable.KinderDrawCount==1 && restored.Playable.Duplicates[4]==1,"Save progression roundtrip");
        Check(restored.Progress.ReadStoryIds.Count==1 && restored.Progress.Materials==c.Progress.Materials,"Save wallet and read flag");
        var awakenedSave=c.CreateSave(); awakenedSave.heroineLevels[0]=120; awakenedSave.heroineAwakenings[0]=2;
        var awakenedRestored=new CampaignState(WorldCatalog.ColossusIds,JsonUtility.FromJson<CampaignSaveV2>(JsonUtility.ToJson(awakenedSave)));
        Check(awakenedRestored.Playable.Levels[0]==120 && awakenedRestored.Playable.Awakenings[0]==2,"Unity JSON preserves awakening and level120");
        awakenedSave.heroineTraitRanks[0]=3; awakenedSave.overflowEnhancementMaterials=2;
        var strengthenedRestored=new CampaignState(WorldCatalog.ColossusIds,JsonUtility.FromJson<CampaignSaveV2>(JsonUtility.ToJson(awakenedSave)));
        Check(strengthenedRestored.Playable.TraitRanks[0]==3 && strengthenedRestored.Playable.OverflowEnhancementMaterials==2,"Unity JSON preserves duplicate strength and generic material");
        var legacy=new PlayableProgress(new CampaignSaveV2 { heroineLevels=null,weaponBranches=null,furnitureSlots=null });
        Check(legacy.Levels.All(l=>l==1) && legacy.Slots.All(l=>l==-1),"Legacy save defaults");
        var defeat=LegacyBattle(45,new PlayableProgress()); for(int i=0;i<100 && !defeat.Ended;i++) defeat.EndTurn();
        Check(defeat.Ended && !defeat.State.IsVictory,"Defeat ends battle");
        Check(!defeat.Act(0,0,"body"),"Ended battle blocks actions");
        for(int i=1;i<WorldCatalog.Colossi.Count;i++) { var col=WorldCatalog.Colossi[i]; c.ClaimColossusVictory(col.Id,col.EnvironmentTags,new VictoryReward("world-"+i,1,10,4,null),null,null,GardenCatalog.Requirements); }
        Check(c.ColossusUnlocks.IsUnlocked(WorldCatalog.ColossusIds[14]),"Final world unlock");
        Debug.Log("PLAYABLE_VALIDATION_PASS "+assertions+" assertions");
    }
    private static void ValidateBattleDecisions()
    {
        ValidateAwakeningProgress();
        ValidateKinderRewards();
        var progress=new PlayableProgress();
        var battle=LegacyBattle(1,progress,42);
        int preview=battle.PreviewDamage(0,1,"body"), before=battle.State.BossHitPoints;
        Check(battle.Act(0,1,"body") && before-battle.State.BossHitPoints==preview,"Attack preview matches actual body damage");
        int resource=battle.State.Heroes[1].JobResource;
        Check(battle.PreviewDamage(1,1,"missing")==0 && !battle.Act(1,1,"missing") && battle.State.Heroes[1].JobResource==resource && !battle.Acted[1],"Invalid target consumes neither resource nor action");
        Check(battle.Act(2,2,"body") && battle.Chain==0 && battle.Guarded,"Support breaks attack chain and guards party");
        var expected=Enumerable.Range(0,5).Select(battle.PreviewEnemyDamage).ToArray();
        var hp=battle.State.Heroes.Select(h=>h.HitPoints).ToArray();
        battle.EndTurn();
        Check(Enumerable.Range(0,5).All(i=>hp[i]-battle.State.Heroes[i].HitPoints==expected[i]),"Enemy preview matches guarded damage");

        var major=LegacyBattle(1,progress);
        for(int i=0;i<3;i++) major.EndTurn();
        Check(major.NextAttackIsMajor && major.State.BossGauge==3,"Low-level major attack is telegraphed");
        before=major.State.Heroes[0].HitPoints;
        preview=major.PreviewEnemyDamage(0); major.EndTurn();
        Check(major.State.BossGauge==0 && before-major.State.Heroes[0].HitPoints==preview && major.Log.Contains("大技"),"Low-level major attack executes and consumes gauge");
        var interrupt=LegacyBattle(1,progress);
        for(int i=0;i<3;i++) interrupt.EndTurn();
        interrupt.State.BreakPart(interrupt.State.Parts[0].Id,999);
        Check(!interrupt.NextAttackIsMajor,"Horn destruction cancels pending major attack");
        var highProgress=new PlayableProgress();
        for(int i=0;i<5;i++) highProgress.Levels[i]=50;
        var ultimate=LegacyBattle(45,highProgress);
        for(int i=0;i<4;i++) ultimate.EndTurn();
        Check(ultimate.Log.Contains("極大技") && ultimate.State.BossGauge==0,"Level45 enables stronger ultimate");

        bool fullChain=false, stoppedChain=false, differentBonuses=false;
        int baseline=LegacyBattle(45,highProgress,1).PreviewDamage(0,0,"body");
        for(int seed=1;seed<=100;seed++) {
            var a=LegacyBattle(45,highProgress,seed);
            var b=LegacyBattle(45,highProgress,seed);
            differentBonuses|=a.ChainRate(0)!=LegacyBattle(45,highProgress,1).ChainRate(0);
            Check(a.PreviewDamage(0,0,"body")==baseline,"Chain-rate bonus does not directly increase damage");
            for(int hero=0;hero<5;hero++) {
                a.Act(hero,0,"body"); b.Act(hero,0,"body");
                Check(a.Log==b.Log && a.State.BossHitPoints==b.State.BossHitPoints,"Seed reproduces chain rolls and damage");
                fullChain|=a.Log.Contains("5 CHAIN"); stoppedChain|=a.Log.Contains("チェイン終了");
            }
        }
        Check(fullChain && stoppedChain && differentBonuses,"Seed sample contains five-person chains, failures and varying turn bonuses");
        var grown=new PlayableProgress(); for(int i=0;i<15;i++) grown.Branches[i]=3;
        Check(LegacyBattle(1,grown,42).ChainRate(0)==LegacyBattle(1,progress,42).ChainRate(0),"Equipment growth does not change chain probability");
        var overkill=LegacyBattle(1,highProgress);
        var part=overkill.State.Parts[0]; before=part.HitPoints;
        var hit=BattleActionResolver.Resolve(overkill.State,"hero-0",new BattleSkill("overkill",10m,0),part.Id);
        Check(hit.PartBroken && hit.Damage==before,"Part damage reports actual HP loss instead of overkill");
        var starter=LegacyBattle(1,progress);
        for(int rounds=0;!starter.Ended && rounds<50;rounds++) {
            int turn=starter.Turn;
            for(int hero=0;hero<5 && !starter.Ended && starter.Turn==turn;hero++) {
                if(starter.Acted[hero]) continue;
                var nextPart=starter.State.Parts.FirstOrDefault(p=>!p.IsBroken);
                int slot=starter.State.Heroes[hero].JobResource>=3 && starter.HealingSkill(hero,1)==null?1:0;
                Check(starter.Act(hero,slot,nextPart?.Id??"body"),"Starter strategy always executes a valid attack slot");
            }
        }
        Check(starter.State.IsVictory,"Starter party can still win using part strategy after probability fix");
    }
    private static void ValidateAwakeningProgress()
    {
        var wallet=new ProgressState(new CampaignSaveV2 { materials=4000 });
        var p=new PlayableProgress();
        int balance=wallet.Materials;
        Check(p.LevelCap(0)==50 && !p.Awaken(wallet,0) && wallet.Materials==balance,"Early awakening does not spend materials");
        Check(p.TrainingCost(0,5)==11 && p.Train(wallet,0,5) && p.Levels[0]==6 && balance-wallet.Materials==11,"Batch training sums per-level costs");
        var poor=new ProgressState(new CampaignSaveV2 { materials=1 });
        Check(!p.Train(poor,0,10) && p.Levels[0]==6 && poor.Materials==1,"Unaffordable batch training is atomic");
        Check(!p.Train(wallet,-1,5) && !p.Train(wallet,0,0) && !p.Train(null,0,1) && !p.Awaken(wallet,5),"Invalid training and awakening inputs are rejected");
        Check(p.Train(wallet,0,999) && p.Levels[0]==50,"Training clamps to base cap");
        balance=wallet.Materials;
        Check(!p.Train(wallet,0) && wallet.Materials==balance,"Training at cap consumes nothing");
        Check(!p.Awaken(poor,0) && p.Awakenings[0]==0 && poor.Materials==1,"Awakening without materials is atomic");
        Check(p.Awaken(wallet,0) && p.Awakenings[0]==1 && p.Levels[0]==50 && p.LevelCap(0)==80 && balance-wallet.Materials==PlayableProgress.FirstAwakeningCost,"First awakening opens cap80 without changing level");
        balance=wallet.Materials;
        Check(!p.Awaken(wallet,0) && wallet.Materials==balance,"Second awakening requires cap80");
        Check(p.Train(wallet,0,29) && p.Levels[0]==79,"Train beyond level50 after awakening");
        int lastCost=p.TrainingCost(0,10); balance=wallet.Materials;
        Check(lastCost==p.TrainingCost(0,1) && p.Train(wallet,0,10) && p.Levels[0]==80 && balance-wallet.Materials==lastCost,"Batch at cap charges only actual growth");
        Check(p.Awaken(wallet,0) && p.LevelCap(0)==120 && p.Levels[0]==80,"Second awakening opens cap120");
        Check(p.Train(wallet,0,int.MaxValue) && p.Levels[0]==120,"Large request stops safely at final cap");
        balance=wallet.Materials;
        Check(!p.Awaken(wallet,0) && !p.Train(wallet,0) && wallet.Materials==balance,"Final cap cannot consume extra materials");
        var save=new CampaignSaveV2(); p.CopyTo(save); wallet.CopyTo(save);
        var restored=new PlayableProgress(save);
        Check(restored.Levels[0]==120 && restored.Awakenings[0]==2 && restored.LevelCap(0)==120,"Save restores awakened levels above50");
        save.heroineAwakenings[0]=0; save.heroineLevels[0]=1;
        Check(p.Levels[0]==120 && restored.Awakenings[0]==2,"Save arrays do not alias live state");
        var legacy=new PlayableProgress(new CampaignSaveV2 { heroineAwakenings=null, heroineLevels=new[] {50,35} });
        Check(legacy.Levels[0]==50 && legacy.Levels[1]==35 && legacy.Levels[2]==1 && legacy.Awakenings.All(a=>a==0),"Older and partial saves keep levels and default awakening");
        var malformed=new PlayableProgress(new CampaignSaveV2 { heroineAwakenings=new[] {-1,99,1},heroineLevels=new[] {999,999,-3} });
        Check(malformed.Levels[0]==50 && malformed.Levels[1]==120 && malformed.Levels[2]==1,"Out-of-range save data is bounded by awakening cap");
        var beforeBattle=LegacyBattle(1,new PlayableProgress(),42);
        var afterBattle=LegacyBattle(1,p,42);
        Check(afterBattle.State.Heroes[0].Attack>beforeBattle.State.Heroes[0].Attack && afterBattle.State.Heroes[0].MaxHitPoints>beforeBattle.State.Heroes[0].MaxHitPoints && afterBattle.ChainRate(0)==beforeBattle.ChainRate(0),"Awakened training improves stats without changing chain rate");
    }
    private static void ValidateEncounterPhase()
    {
        var battle=LegacyBattle(1,new PlayableProgress());
        int normal=battle.PreviewEnemyDamage(0);
        battle.State.ApplyBossDamage(battle.State.BossMaxHitPoints/2-1);
        Check(!battle.IsEnraged && battle.PreviewEnemyDamage(0)==normal,"Enrage does not start above half HP");
        battle.State.ApplyBossDamage(1);
        Check(battle.IsEnraged && battle.PreviewEnemyDamage(0)>normal && battle.NextEnemyAction.Contains("怒り"),"Half HP activates visible stronger attack");
        battle.State.AdvanceBossGauge(3);
        Check(battle.NextEnemyAction.Contains("大技"),"Gauge major action takes precedence over phase attack name");
        int preview=battle.PreviewEnemyDamage(0), hp=battle.State.Heroes[0].HitPoints;
        battle.EndTurn();
        Check(hp-battle.State.Heroes[0].HitPoints==preview,"Enraged major damage matches UI forecast");
        battle.State.ApplyBossDamage(int.MaxValue);
        hp=battle.State.Heroes[0].HitPoints; battle.EndTurn();
        Check(!battle.IsEnraged && battle.State.Heroes[0].HitPoints==hp,"Victory ends phase and enemy actions");
    }
    private static void ValidateDistinctSupport()
    {
        var p=new PlayableProgress();
        var self=LegacyBattle(1,p); self.State.Heroes[0].TakeDamage(80); self.State.Heroes[1].TakeDamage(80);
        int other=self.State.Heroes[1].HitPoints;
        Check(self.Act(0,2,"body") && self.State.Heroes[0].HitPoints>62 && self.State.Heroes[1].HitPoints==other && !self.Guarded,"Self healing affects only caster");
        var seal=LegacyBattle(1,p); seal.State.AdvanceBossGauge(3);
        Check(seal.NextAttackIsMajor && seal.Act(1,2,"body") && seal.State.BossGauge==2 && !seal.NextAttackIsMajor,"Seal delays telegraphed major attack");
        var heal=LegacyBattle(1,p); foreach(var h in heal.State.Heroes) h.TakeDamage(80); heal.State.Heroes[0].TakeDamage(999);
        Check(!heal.Act(3,1,"body") && !heal.Act(3,1,"body",0) && !heal.Act(3,1,"body",5) && heal.State.Heroes[3].JobResource==3 && !heal.Acted[3],"Missing, dead or invalid ally consumes neither resource nor action");
        int preview=heal.PreviewHealing(3,1); int before=heal.State.Heroes[1].HitPoints;
        Check(preview>0 && heal.Act(3,1,"body",1) && heal.State.Heroes[1].HitPoints==before+preview && heal.State.Heroes[2].HitPoints==62 && heal.State.Heroes[0].HitPoints==0,"Target healing matches preview and does not heal others or resurrect");
        Check(heal.State.Heroes[3].JobResource==0 && heal.Acted[3] && !heal.Act(3,1,"body",2),"Target support costs three resources and one action exactly once");
        var full=LegacyBattle(1,p);
        Check(!full.CanHeal(1) && !full.Act(3,1,"body",1) && !full.Act(3,2,"body") && full.RemainingActions==5,"Full HP target and group do not waste action");
        full.State.Heroes[3].TakeDamage(1);
        Check(full.PreviewHealing(3,3)==1 && full.Act(3,1,"body",3) && full.State.Heroes[3].HitPoints==full.State.Heroes[3].MaxHitPoints,"Self selection and overheal clamp are supported");
        var acted=LegacyBattle(1,p); acted.State.Heroes[0].TakeDamage(40); acted.Act(0,0,"body");
        Check(acted.CanHeal(0) && acted.Act(3,1,"body",0) && acted.Acted[0] && acted.RemainingActions==3,"Already acted ally may be healed but never gets an implicit extra action");
        var group=LegacyBattle(1,p); foreach(var h in group.State.Heroes) h.TakeDamage(80); group.State.Heroes[0].TakeDamage(999);
        int[] expected=Enumerable.Range(0,5).Select(i=>group.State.Heroes[i].HitPoints+group.PreviewHealing(3,i,2)).ToArray();
        Check(group.Act(3,2,"body") && Enumerable.Range(0,5).All(i=>group.State.Heroes[i].HitPoints==expected[i]) && group.LastHealingTargets.SequenceEqual(new[]{1,2,3,4}),"All healing uses its own lower power and heals every living ally without resurrection");
        Check(group.State.Heroes[3].JobResource==0 && group.RemainingActions==3,"All healing costs once rather than per target");
        Check(LegacyBattle(1,p).PreviewDamage(3,1,"body")==0,"Healing slot never advertises attack damage");
        var definitions=PlayableBattle.DefaultHealingSkills().Where(d=>!(d.Actor==3 && d.Slot==1)).Concat(new[]{new HealingSkillDefinition(3,1,"二人回復検証",HealingTargetRule.SelectedAllies,2,3,35,1m)});
        var pair=LegacyBattle(1,p,1,definitions); foreach(var h in pair.State.Heroes) h.TakeDamage(80);
        Check(!pair.ActWithAllies(3,1,"body",new[]{0}) && !pair.ActWithAllies(3,1,"body",new[]{0,0}) && !pair.ActWithAllies(3,1,"body",new[]{0,1,2}) && !pair.ActWithAllies(3,1,"body",new[]{0,9}) && pair.State.Heroes[3].JobResource==3 && pair.RemainingActions==5,"Selected count, duplicates and invalid candidates reject atomically");
        Check(pair.ActWithAllies(3,1,"body",new[]{0,2}) && pair.State.Heroes[0].HitPoints==120 && pair.State.Heroes[2].HitPoints==120 && pair.State.Heroes[1].HitPoints==62 && pair.LastHealingTargets.SequenceEqual(new[]{0,2}) && pair.State.Heroes[3].JobResource==0,"Two-target definition heals exactly selected two at one action cost");
        var low=LegacyBattle(1,p); low.State.Heroes[0].TakeDamage(20); low.State.Heroes[3].SpendResource(1);
        Check(!low.Act(3,2,"body") && !low.Act(3,1,"body",0) && low.State.Heroes[0].HitPoints==122 && !low.Acted[3],"Insufficient resource leaves all targets and action untouched");
        var finish=LegacyBattle(1,p); finish.State.Heroes[0].TakeDamage(80); foreach(int i in new[]{0,1,2,4}) finish.Act(i,0,"body");
        int finalPreview=finish.PreviewHealing(3,0,2);
        Check(finish.Act(3,2,"body") && finish.Turn==2 && finish.State.Heroes[0].HitPoints==62+finalPreview-14 && finish.LastHealingTargets.Count==5,"Last actor healing resolves before enemy damage and retains effect targets for presentation");
        bool invalidDefinition=false, duplicateDefinition=false;
        try { new HealingSkillDefinition(3,1,"Invalid",HealingTargetRule.Self,2,3,35,1m); } catch(ArgumentException) { invalidDefinition=true; }
        var duplicate=PlayableBattle.DefaultHealingSkills().Concat(new[]{PlayableBattle.DefaultHealingSkills()[0]});
        try { LegacyBattle(1,p,1,duplicate); } catch(ArgumentException) { duplicateDefinition=true; }
        Check(invalidDefinition && duplicateDefinition,"Invalid target cardinality and duplicate skill slots reject configuration");
        var missing=LegacyBattle(1,p,1,Array.Empty<HealingSkillDefinition>());
        Check(!missing.Act(3,2,"body") && missing.State.Heroes[3].JobResource==3 && !missing.Acted[3],"Missing support definition cannot silently consume resource");
        var last=LegacyBattle(1,p);
        for(int i=0;i<5;i++) last.Act(i,0,"body");
        Check(last.Turn==2 && last.Chain==0 && last.LastActionChain>=1 && last.LastActionChain<=5 && last.RemainingActions==5,"Enemy turn preserves last resolved chain while resetting new turn actions");
        var supply=LegacyBattle(1,p);
        Check(supply.Act(4,2,"body") && supply.State.Heroes[4].JobResource==0 && supply.State.Heroes.Take(4).All(h=>h.JobResource==5),"Supply charges caster and replenishes allies");
        Check(!supply.Act(4,2,"body") && supply.State.Heroes[0].JobResource==5,"Supply cannot be repeated in same turn");
    }
    private static void ValidateTimeline()
    {
        var p=new PlayableProgress(); var a=new PlayableBattle(1,p,42);
        Check(a.UsesTimeline && a.AvailableHero==0 && a.Clock==91 && a.UpcomingOrder().Select(e=>e.Actor).SequenceEqual(new[]{0,3,4,1,-1,2}),"Initial order follows speed including enemy instead of party rounds");
        int hp=a.State.BossHitPoints, resource=a.State.Heroes[1].JobResource;
        Check(!a.Act(1,0,"body") && a.Clock==91 && a.State.BossHitPoints==hp && a.State.Heroes[1].JobResource==resource,"Out-of-order actor cannot consume or advance time");
        long start=a.Clock; a.Act(0,0,"body");
        Check(a.NextAt(0)==start+a.RecoveryDelay(0,0) && a.AvailableHero==3 && a.Clock==96,"Normal attack schedules only its user and advances to next actor");
        var heavy=new PlayableBattle(1,p,42); heavy.Act(0,1,"body");
        Check(heavy.NextAt(0)>a.NextAt(0) && heavy.RecoveryDelay(0,2)>heavy.RecoveryDelay(0,1),"Slightly long and long recovery delay next command independently");
        Check(SkillTimingDefinition.Delay(200,150)<SkillTimingDefinition.Delay(100,150) && SkillTimingDefinition.Delay(100,50)<SkillTimingDefinition.Delay(100,150),"Speed and timing length affect simulated delay monotonically");
        var cast=new PlayableBattle(1,p,7); while(cast.AvailableHero!=4) cast.Pass();
        hp=cast.State.BossHitPoints; start=cast.Clock;
        Check(cast.Act(4,1,"body") && cast.IsCasting(4) && cast.State.BossHitPoints==hp && cast.NextAt(4)==start+cast.CastDelay(4,1) && cast.State.Heroes[4].JobResource==0,"Cast reserves cost but deals no damage until its completion event");
        Check(cast.AvailableHero==1 && cast.UpcomingOrder().Any(e=>e.Actor==4 && e.IsCast) && !cast.Act(4,1,"body"),"Other heroes act while caster is blocked and cast event appears in queue");
        for(int i=0;i<20 && cast.IsCasting(4);i++) cast.Pass();
        Check(!cast.IsCasting(4) && cast.State.BossHitPoints<hp && cast.NextAt(4)==start+cast.CastDelay(4,1)+cast.RecoveryDelay(4,1) && cast.EnemyActionCount>0 && cast.Log.Contains("詠唱発動"),"Spell resolves after intervening enemy actions then adds separate recovery time");
        var canceled=new PlayableBattle(1,p); while(canceled.AvailableHero!=4) canceled.Pass();
        string target=canceled.State.Parts[0].Id; hp=canceled.State.BossHitPoints;
        canceled.Act(4,1,target); canceled.State.BreakPart(target,999);
        for(int i=0;i<20 && canceled.IsCasting(4);i++) canceled.Pass();
        Check(canceled.State.BossHitPoints==hp && !canceled.IsCasting(4) && canceled.Log.Contains("詠唱不発"),"Destroyed cast target fizzles without unannounced retargeting");
        var dead=new PlayableBattle(1,p); while(dead.AvailableHero!=4) dead.Pass(); dead.Act(4,1,"body"); dead.State.Heroes[4].TakeDamage(9999); hp=dead.State.BossHitPoints; dead.Pass();
        Check(!dead.IsCasting(4) && dead.State.BossHitPoints==hp && dead.UpcomingOrder().All(e=>e.Actor!=4),"Defeated caster loses pending spell and leaves action queue");
        var invalid=new PlayableBattle(1,p); while(invalid.AvailableHero!=4) invalid.Pass(); start=invalid.Clock; resource=invalid.State.Heroes[4].JobResource;
        Check(!invalid.Act(4,1,"missing") && !invalid.IsCasting(4) && invalid.Clock==start && invalid.State.Heroes[4].JobResource==resource,"Invalid cast target consumes no cost or timeline event");
        var timings=PlayableBattle.DefaultTimings(); timings[4,1]=new SkillTimingDefinition(150,50);
        var shortCast=new PlayableBattle(1,p,1,null,true,timings); while(shortCast.AvailableHero!=4) shortCast.Pass();
        Check(shortCast.CastDelay(4,1)<shortCast.RecoveryDelay(4,1) && shortCast.TimingDescription(4,1).Contains("詠唱：短い"),"Casting and recovery are independently configured per skill");
        var chainCast=new PlayableBattle(1,p,42); chainCast.Act(0,0,"body"); chainCast.Act(3,0,"body");
        Check(chainCast.AvailableHero==4 && chainCast.PreviewDamage(4,1,"body")==28,"Delayed spell preview excludes the immediate attack chain that casting will end");
        var tieTimings=PlayableBattle.DefaultTimings(); tieTimings[4,1]=new SkillTimingDefinition(100,82);
        var tie=new PlayableBattle(1,p,1,null,true,tieTimings); while(tie.AvailableHero!=4) tie.Pass(); hp=tie.State.BossHitPoints;
        tie.Act(4,1,"body"); while(tie.IsCasting(4)) tie.Pass();
        Check(tie.Clock==182 && tie.AvailableHero==0 && tie.State.BossHitPoints==hp-28 && tie.LastCastResolvedActor==4,"Equal-time spell resolves before hero command with deterministic tie priority");
        for(int seed=1;seed<=10;seed++) {
            var run=new PlayableBattle(1,p,seed); long clock=run.Clock;
            for(int step=0;step<150 && !run.Ended;step++) {
                int actor=run.AvailableHero; var part=run.State.Parts.FirstOrDefault(x=>!x.IsBroken);
                Check(run.Act(actor,0,part?.Id??"body") && run.Clock>=clock && run.LastActionChain<=5,"Timeline actions preserve monotonic clock and five-unique-hero chain cap"); clock=run.Clock;
            }
            Check(run.State.IsVictory,"Scheduled starter party can defeat first colossus with part strategy");
        }
    }
    private static void ValidateVisualCues()
    {
        Check(BattleVisualCue.Progress(-1,BattlePresentationKind.Attack,false)==0 && BattleVisualCue.Progress(10,BattlePresentationKind.Attack,false)==1,"Visual progress clamps without changing combat time");
        Check(BattleVisualCue.Travel(0)==0 && BattleVisualCue.Travel(.56f)==1,"Projectile reaches selected target at hit phase");
        Check(BattleVisualCue.Impact(.55f)==0 && BattleVisualCue.Impact(.56f)==1 && BattleVisualCue.Impact(1)==0,"Impact starts after travel and fades to zero");
        Check(BattleVisualCue.Duration(BattlePresentationKind.CastStart,false)==.7f && BattleVisualCue.Duration(BattlePresentationKind.Enemy,true)==1.1f,"Playback and effects share provisional duration definitions");
    }
    private static void ValidateSlayerModel()
    {
        const string path="Assets/Game/Resources/Characters/Slayer/slayer-beauty-v2.fbx";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Check(asset!=null,"Slayer FBX is available to runtime Resources");
        var meshes=asset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var body=meshes.Single(r=>r.name=="Body_Common");
        Check(body.sharedMesh.vertexCount>10000 && body.bones.Length>15,"Full body mesh has a deforming common skeleton");
        Check(meshes.Any(r=>r.name.StartsWith("Outfit_Rose")) && meshes.Any(r=>r.name.StartsWith("Outfit_Training")),"Two distinct garment sets exist without replacing the body");
        Check(body.bones.Any(b=>b.name=="Hand.R") && body.bones.Any(b=>b.name=="Head") && body.bones.Any(b=>b.name=="Finger3.L"),"Hand head and finger bones survive export");
        Check(meshes.Any(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Any(i=>r.sharedMesh.GetBlendShapeName(i).EndsWith("Blink"))),"Blink deformation survives FBX import");
        Check(Resources.Load<Shader>("HeroineBeauty")!=null,"Character close-up shader is packaged with the player");
        foreach(var part in new[]{"Hair_Styled","WingWing.L","WingWing.R"}) {
            var renderer=meshes.Single(r=>r.name==part);
            string expected=part=="Hair_Styled"?"Head":part.Substring(4);
            Check(renderer.sharedMesh.boneWeights.All(w=>
                (w.weight0==0 || renderer.bones[w.boneIndex0].name==expected) &&
                (w.weight1==0 || renderer.bones[w.boneIndex1].name==expected) &&
                (w.weight2==0 || renderer.bones[w.boneIndex2].name==expected) &&
                (w.weight3==0 || renderer.bones[w.boneIndex3].name==expected)),"Repeated mesh parts retain intended bone binding: "+part);
        }
        foreach(var prefix in new[]{"Eye_White","Eye_Iris","Eye_Lid"}) {
            var parts=meshes.Where(r=>r.name.StartsWith(prefix)).ToArray();
            Check(parts.Length==(prefix=="Eye_Lid"?4:2) && parts.All(r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount).Any(i=>r.sharedMesh.GetBlendShapeName(i).EndsWith("Blink"))),"Both eyes have coordinated blink geometry: "+prefix);
        }
        Check(meshes.Any(r=>r.name=="Mouth" && Enumerable.Range(0,r.sharedMesh.blendShapeCount).Any(i=>r.sharedMesh.GetBlendShapeName(i).EndsWith("Smile"))),"Smile deformation survives FBX import");
        var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        Debug.Log("SLAYER_IMPORTED_CLIPS "+string.Join(",",clips.Select(c=>c.name)));
        foreach(var name in new[]{"Idle","Attack","Cast","Hit","Victory"}) Check(clips.Any(c=>c.name==name && c.length>0 && c.legacy),"Slayer motion imported: "+name);
        var copy=UnityEngine.Object.Instantiate(asset);
        try {
            foreach(var clip in clips) {
                clip.SampleAnimation(copy,clip.length*.5f);
                var head=copy.GetComponentsInChildren<Transform>().First(t=>t.name=="Head");
                Check(!float.IsNaN(head.position.x) && !float.IsInfinity(head.position.y),"Sampled model pose remains finite: "+clip.name);
            }
        } finally { UnityEngine.Object.DestroyImmediate(copy); }
    }
    private static void ValidatePlayback()
    {
        var p=new PlayableProgress(); var battle=new PlayableBattle(1,p);
        Check(battle.DrainPresentationEvents().Count==0,"Battle creation has no phantom animation");
        int hp=battle.State.BossHitPoints; battle.Act(0,0,"body");
        var first=battle.DrainPresentationEvents().Single();
        Check(first.Kind==BattlePresentationKind.Attack && first.Actor==0 && first.Clock==91 && first.BossHp==hp-first.Damage && first.HeroHp.Count==5 && first.PartHp.Count==4,"Attack captures exact result and battle time");
        battle.State.Heroes[0].TakeDamage(70); battle.Act(3,2,"body");
        var healing=battle.DrainPresentationEvents().Single();
        Check(healing.Kind==BattlePresentationKind.Healing && healing.HealingTargets.Count==5 && healing.HeroHp[0]>72 && first.HeroHp[0]==142,"Healing snapshot and target list do not mutate prior event");
        battle.Act(4,1,"body"); var start=battle.DrainPresentationEvents().Single();
        Check(start.Kind==BattlePresentationKind.CastStart && start.Casting[4] && start.Resources[4]==0 && start.Damage==0,"Cast start is its own zero-damage presentation event");
        var collected=new System.Collections.Generic.List<BattlePresentationEvent>();
        while(battle.IsCasting(4)) { battle.Pass(); collected.AddRange(battle.DrainPresentationEvents()); }
        var release=collected.Single(e=>e.Kind==BattlePresentationKind.CastRelease);
        Check(collected.Any(e=>e.Kind==BattlePresentationKind.Enemy) && release.Actor==4 && release.Target=="body" && release.Damage>0 && release.Clock==250 && !release.Casting[4],"Cast release retains real caster target damage and committed time");
        Check(collected.Zip(collected.Skip(1),(a,b)=>a.Sequence<b.Sequence && a.Clock<=b.Clock).All(x=>x) && battle.DrainPresentationEvents().Count==0,"Events drain once in chronological order");
        var batch=collected.Take(3).ToArray(); var queue=new BattlePlaybackQueue(); queue.Enqueue(batch);
        var current=queue.Current; queue.Tick(10,true);
        Check(queue.Busy && queue.Count==3 && queue.Current==current,"Pause retains playback event and blocks progress");
        queue.Tick(.4f,false); Check(queue.Current==current,"Partial playback does not skip event");
        queue.Tick(10,false); Check(queue.Current==batch[1] && queue.Count==2,"Large frame advances at most one event for visibility");
        bool duplicate=false; try { queue.Enqueue(batch); } catch(ArgumentException) { duplicate=true; }
        Check(duplicate && queue.Count==2,"Duplicate replay batch rejects atomically");
        hp=battle.State.BossHitPoints; long clock=battle.Clock; int cost=battle.State.Heroes[4].JobResource;
        queue.Skip(); Check(!queue.Busy && queue.Count==0 && battle.State.BossHitPoints==hp && battle.Clock==clock && battle.State.Heroes[4].JobResource==cost,"Skipping animation never executes combat or spends resources again");
        queue.Reset(); queue.Enqueue(new[]{first}); Check(queue.Current==first,"Fresh battle reset accepts new sequence starting at one");
        queue.Tick(1,false); Check(!queue.Busy,"Queue finishes without requiring new player input");
        var invalid=new PlayableBattle(1,p); invalid.Act(0,1,"missing");
        Check(invalid.DrainPresentationEvents().Count==0,"Rejected input cannot create an animation or false damage");
        var killed=new PlayableBattle(1,p); killed.State.ApplyBossDamage(killed.State.BossHitPoints-1); killed.Act(0,0,"body");
        var last=killed.DrainPresentationEvents();
        Check(killed.State.IsVictory && last.Count==1 && last[0].BossHp==0 && last[0].Kind==BattlePresentationKind.Attack,"Finishing blow has a terminal visual snapshot without a phantom enemy attack");
    }
    private static void ValidateKinderRewards()
    {
        var wallet=new ProgressState(); var p=new PlayableProgress();
        int stones=p.KinderStones;
        Check(!p.TryKinderDraw(wallet,1m,0m,out _,out _) && !p.TryKinderDraw(wallet,0m,-.1m,out _,out _) && p.KinderStones==stones && p.KinderDrawCount==0,"Invalid rolls consume no currency");
        Check(!p.TryKinderDraw(.5m,.5m,out _,out _) && p.KinderStones==stones,"Material rewards require wallet instead of silent non-grant");
        for(int i=0;i<4;i++) {
            int balance=wallet.Materials;
            Check(p.TryKinderDraw(wallet,.03m,i/4m,out var heroine,out var index) && !heroine && index==i && wallet.Materials-balance==PlayableProgress.KinderMaterialReward(i),"Each material pool actually grants advertised quantity");
        }
        Check(p.KinderStones==stones-4 && p.KinderDrawCount==4,"Currency and draw count change once per reward");
        Check(p.TryKinderDraw(wallet,.029999m,.999999m,out var rare,out var target) && rare && target==4 && p.Duplicates[4]==1,"Heroine boundary and final target selection");
        Check(p.StrengthenDuplicate(4) && p.TraitRanks[4]==1 && p.Duplicates[4]==0,"Duplicate converts to permanent strength exactly once");
        Check(!p.StrengthenDuplicate(4) && !p.UseOverflowEnhancement(4) && !p.StrengthenDuplicate(-1),"Absent materials and invalid heroine do not strengthen");
        var rich=new PlayableProgress(new CampaignSaveV2 { kinderStones=10, heroineDuplicates=new[] {7,0,0,0,0} });
        for(int i=0;i<5;i++) Check(rich.StrengthenDuplicate(0),"Use duplicates up to rank cap");
        Check(rich.TraitRanks[0]==5 && rich.Duplicates[0]==2 && rich.StrengthenDuplicate(0) && rich.OverflowEnhancementMaterials==1,"Previously held excess duplicates convert to generic material");
        Check(rich.TryKinderDraw(wallet,0m,0m,out _,out _) && rich.OverflowEnhancementMaterials==2 && rich.Duplicates[0]==1,"New capped duplicate converts automatically");
        Check(rich.UseOverflowEnhancement(1) && rich.TraitRanks[1]==1 && rich.OverflowEnhancementMaterials==1,"Generic material strengthens another heroine");
        Check(!rich.UseOverflowEnhancement(0) && rich.OverflowEnhancementMaterials==1,"Rank cap cannot consume generic materials");
        var baseBattle=LegacyBattle(1,new PlayableProgress(),72); var strongBattle=LegacyBattle(1,rich,72);
        Check(strongBattle.State.Heroes[0].Attack-baseBattle.State.Heroes[0].Attack==5*PlayableProgress.DuplicateAttackGain && strongBattle.State.Heroes[0].MaxHitPoints-baseBattle.State.Heroes[0].MaxHitPoints==5*PlayableProgress.DuplicateHitPointGain && strongBattle.ChainRate(0)==baseBattle.ChainRate(0),"Duplicate strength affects HP and attack but never chain chance");
        var save=new CampaignSaveV2(); rich.CopyTo(save); var restored=new PlayableProgress(save);
        Check(restored.TraitRanks[0]==5 && restored.TraitRanks[1]==1 && restored.OverflowEnhancementMaterials==1 && restored.Duplicates[0]==1,"Strength and conversion survive save restoration");
        var legacy=new PlayableProgress(new CampaignSaveV2 { heroineTraitRanks=null, heroineDuplicates=new[] {3} });
        Check(legacy.TraitRanks.All(r=>r==0) && legacy.Duplicates[0]==3,"Legacy saves preserve pending duplicates without automatic spending");
        var saturated=new ProgressState(new CampaignSaveV2 {materials=int.MaxValue});
        stones=p.KinderStones; int draws=p.KinderDrawCount;
        Check(!p.TryKinderDraw(saturated,.8m,.8m,out _,out _) && p.KinderStones==stones && p.KinderDrawCount==draws,"Material overflow rejects entire draw before charging");
        var capped=new PlayableProgress(new CampaignSaveV2 { kinderStones=1,heroineTraitRanks=new[] {5,0,0,0,0},overflowEnhancementMaterials=int.MaxValue,kinderDrawCount=100 });
        Check(!capped.TryKinderDraw(wallet,0m,0m,out _,out _) && capped.KinderStones==1 && !capped.TryKinderExchange(0) && capped.AvailableKinderExchanges==1,"Conversion overflow preserves stone and exchange ticket");
        Check(capped.TryKinderExchange(1) && capped.Duplicates[1]==1 && capped.AvailableKinderExchanges==0 && !capped.TryKinderExchange(1),"Exchange grants once and consumes exactly one entitlement");
        Check(new PlayableProgress(new CampaignSaveV2 {kinderDrawCount=100,kinderExchangeCount=999}).AvailableKinderExchanges==0,"Invalid saved exchange count cannot yield negative tickets");
        var sample=new PlayableProgress(new CampaignSaveV2 {kinderStones=20000});
        var sampleWallet=new ProgressState(); var heroes=new int[5]; var materials=new int[4];
        for(int roll=0;roll<1000;roll++) for(int choice=0;choice<20;choice++) {
            if(!sample.TryKinderDraw(sampleWallet,roll/1000m,choice/20m,out var heroine,out var index)) throw new Exception("Sample draw rejected");
            if(heroine) heroes[index]++; else materials[index]++;
        }
        Check(heroes.All(n=>n==120) && materials.All(n=>n==4850),"Exhaustive roll grid matches 3%, equal heroines and equal material probabilities");
        Check(sample.KinderDrawCount==20000 && sample.KinderStones==0 && sampleWallet.Materials==4850*(4+6+8+10),"Sample currency and awarded materials reconcile");
    }
}
