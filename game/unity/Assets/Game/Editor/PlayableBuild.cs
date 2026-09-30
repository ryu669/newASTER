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
}
