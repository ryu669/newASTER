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
}
