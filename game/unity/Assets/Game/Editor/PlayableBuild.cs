using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PlayableBuild
{
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
        ValidateDistinctSupport();
        ValidateBattleDecisions();
        var c=new CampaignState(WorldCatalog.ColossusIds);
        var first=WorldCatalog.Colossi[0];
        Check(!c.Playable.Train(c.Progress,0),"Training without funds must fail");
        var b=new PlayableBattle(1,c.Playable);
        Check(b.Act(0,0,"body"),"First action"); Check(!b.Act(0,0,"body"),"Double action blocked");
        for(int rounds=0;!b.Ended && rounds<50;rounds++) {
            int turn=b.Turn;
            for(int h=0;h<5 && !b.Ended && turn==b.Turn;h++) {
                if(b.Acted[h]) continue;
                var part=b.State.Parts.FirstOrDefault(p=>!p.IsBroken);
                b.Act(h,b.State.Heroes[h].JobResource>=3?1:0,part?.Id??"body");
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
        var defeat=new PlayableBattle(45,new PlayableProgress()); for(int i=0;i<100 && !defeat.Ended;i++) defeat.EndTurn();
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
        var battle=new PlayableBattle(1,progress,42);
        int preview=battle.PreviewDamage(0,1,"body"), before=battle.State.BossHitPoints;
        Check(battle.Act(0,1,"body") && before-battle.State.BossHitPoints==preview,"Attack preview matches actual body damage");
        int resource=battle.State.Heroes[1].JobResource;
        Check(battle.PreviewDamage(1,1,"missing")==0 && !battle.Act(1,1,"missing") && battle.State.Heroes[1].JobResource==resource && !battle.Acted[1],"Invalid target consumes neither resource nor action");
        Check(battle.Act(2,2,"body") && battle.Chain==0 && battle.Guarded,"Support breaks attack chain and guards party");
        var expected=Enumerable.Range(0,5).Select(battle.PreviewEnemyDamage).ToArray();
        var hp=battle.State.Heroes.Select(h=>h.HitPoints).ToArray();
        battle.EndTurn();
        Check(Enumerable.Range(0,5).All(i=>hp[i]-battle.State.Heroes[i].HitPoints==expected[i]),"Enemy preview matches guarded damage");

        var major=new PlayableBattle(1,progress);
        for(int i=0;i<3;i++) major.EndTurn();
        Check(major.NextAttackIsMajor && major.State.BossGauge==3,"Low-level major attack is telegraphed");
        before=major.State.Heroes[0].HitPoints;
        preview=major.PreviewEnemyDamage(0); major.EndTurn();
        Check(major.State.BossGauge==0 && before-major.State.Heroes[0].HitPoints==preview && major.Log.Contains("大技"),"Low-level major attack executes and consumes gauge");
        var interrupt=new PlayableBattle(1,progress);
        for(int i=0;i<3;i++) interrupt.EndTurn();
        interrupt.State.BreakPart(interrupt.State.Parts[0].Id,999);
        Check(!interrupt.NextAttackIsMajor,"Horn destruction cancels pending major attack");
        var highProgress=new PlayableProgress();
        for(int i=0;i<5;i++) highProgress.Levels[i]=50;
        var ultimate=new PlayableBattle(45,highProgress);
        for(int i=0;i<4;i++) ultimate.EndTurn();
        Check(ultimate.Log.Contains("極大技") && ultimate.State.BossGauge==0,"Level45 enables stronger ultimate");

        bool fullChain=false, stoppedChain=false, differentBonuses=false;
        int baseline=new PlayableBattle(45,highProgress,1).PreviewDamage(0,0,"body");
        for(int seed=1;seed<=100;seed++) {
            var a=new PlayableBattle(45,highProgress,seed);
            var b=new PlayableBattle(45,highProgress,seed);
            differentBonuses|=a.ChainRate(0)!=new PlayableBattle(45,highProgress,1).ChainRate(0);
            Check(a.PreviewDamage(0,0,"body")==baseline,"Chain-rate bonus does not directly increase damage");
            for(int hero=0;hero<5;hero++) {
                a.Act(hero,0,"body"); b.Act(hero,0,"body");
                Check(a.Log==b.Log && a.State.BossHitPoints==b.State.BossHitPoints,"Seed reproduces chain rolls and damage");
                fullChain|=a.Log.Contains("5 CHAIN"); stoppedChain|=a.Log.Contains("チェイン終了");
            }
        }
        Check(fullChain && stoppedChain && differentBonuses,"Seed sample contains five-person chains, failures and varying turn bonuses");
        var grown=new PlayableProgress(); for(int i=0;i<15;i++) grown.Branches[i]=3;
        Check(new PlayableBattle(1,grown,42).ChainRate(0)==new PlayableBattle(1,progress,42).ChainRate(0),"Equipment growth does not change chain probability");
        var overkill=new PlayableBattle(1,highProgress);
        var part=overkill.State.Parts[0]; before=part.HitPoints;
        var hit=BattleActionResolver.Resolve(overkill.State,"hero-0",new BattleSkill("overkill",10m,0),part.Id);
        Check(hit.PartBroken && hit.Damage==before,"Part damage reports actual HP loss instead of overkill");
        var starter=new PlayableBattle(1,progress);
        for(int rounds=0;!starter.Ended && rounds<50;rounds++) {
            int turn=starter.Turn;
            for(int hero=0;hero<5 && !starter.Ended && starter.Turn==turn;hero++) {
                if(starter.Acted[hero]) continue;
                var nextPart=starter.State.Parts.FirstOrDefault(p=>!p.IsBroken);
                starter.Act(hero,starter.State.Heroes[hero].JobResource>=3?1:0,nextPart?.Id??"body");
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
        var beforeBattle=new PlayableBattle(1,new PlayableProgress(),42);
        var afterBattle=new PlayableBattle(1,p,42);
        Check(afterBattle.State.Heroes[0].Attack>beforeBattle.State.Heroes[0].Attack && afterBattle.State.Heroes[0].MaxHitPoints>beforeBattle.State.Heroes[0].MaxHitPoints && afterBattle.ChainRate(0)==beforeBattle.ChainRate(0),"Awakened training improves stats without changing chain rate");
    }
    private static void ValidateDistinctSupport()
    {
        var p=new PlayableProgress();
        var self=new PlayableBattle(1,p); self.State.Heroes[0].TakeDamage(80); self.State.Heroes[1].TakeDamage(80);
        int other=self.State.Heroes[1].HitPoints;
        Check(self.Act(0,2,"body") && self.State.Heroes[0].HitPoints>62 && self.State.Heroes[1].HitPoints==other && !self.Guarded,"Self healing affects only caster");
        var seal=new PlayableBattle(1,p); seal.State.AdvanceBossGauge(3);
        Check(seal.NextAttackIsMajor && seal.Act(1,2,"body") && seal.State.BossGauge==2 && !seal.NextAttackIsMajor,"Seal delays telegraphed major attack");
        var heal=new PlayableBattle(1,p); foreach(var h in heal.State.Heroes) h.TakeDamage(80); heal.State.Heroes[0].TakeDamage(999);
        Check(heal.Act(3,2,"body") && heal.State.Heroes[0].HitPoints==0 && heal.State.Heroes.Skip(1).All(h=>h.HitPoints>62),"Group healing does not resurrect defeated allies");
        var supply=new PlayableBattle(1,p);
        Check(supply.Act(4,2,"body") && supply.State.Heroes[4].JobResource==0 && supply.State.Heroes.Take(4).All(h=>h.JobResource==5),"Supply charges caster and replenishes allies");
        Check(!supply.Act(4,2,"body") && supply.State.Heroes[0].JobResource==5,"Supply cannot be repeated in same turn");
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
        var baseBattle=new PlayableBattle(1,new PlayableProgress(),72); var strongBattle=new PlayableBattle(1,rich,72);
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
