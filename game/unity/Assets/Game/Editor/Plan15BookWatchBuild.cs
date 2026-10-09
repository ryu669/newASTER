using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using NewAster.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class Plan15BookWatchBuild
{
    private static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    public static void Build()
    {
        var navigation=new BookNavigationState(new[]{new BookOrderedSubject(BookBookmark.Colossi,"enemy.first",0),new BookOrderedSubject(BookBookmark.Heroines,"hero.first",0)});
        navigation.ChangeBookmark(BookBookmark.Heroines);navigation.FlipPage();navigation.PageState.searchQuery="sample";
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",stones=(long)int.MaxValue+300},bookNavigation=navigation.Capture(),playRewards=new PlayRewardState{rewardRemainderSeconds=1799.75,totalPlaySeconds=1799,lastDailyRewardDate="2026-10-08"}};
        var loaded=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(save));loaded.Validate();navigation=new BookNavigationState(new[]{new BookOrderedSubject(BookBookmark.Colossi,"enemy.first",0),new BookOrderedSubject(BookBookmark.Heroines,"hero.first",0)});navigation.Restore(loaded.bookNavigation);
        Check(navigation.SubjectId=="hero.first" && navigation.Face==BookFace.Details && navigation.PageState.searchQuery=="sample","BK-03 Unity save navigation roundtrip");Check(loaded.growth.stones==(long)int.MaxValue+300 && loaded.playRewards.rewardRemainderSeconds==1799.75,"EC-06 Unity fractional time and 64-bit wallet roundtrip");
        Debug.Log("PLAN15_16_UNITY_SAVE_PASS");
        var args=Environment.GetCommandLineArgs();int outputAt=Array.IndexOf(args,"-buildOutput");string player=outputAt>=0?Path.GetFullPath(args[outputAt+1]):Path.GetFullPath("../Builds/plan11-5-6/newASTER.exe");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName=player,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Plan11-5/6 build failed: "+report.summary.result);
        string notices=Path.Combine(Path.GetDirectoryName(player),"ThirdPartyNotices/NotoSansCJKjp");Directory.CreateDirectory(notices);foreach(var f in new[]{"OFL.txt","NOTICE.txt"})File.Copy("Assets/Game/Resources/Fonts/"+f,Path.Combine(notices,f),true);
        Debug.Log("PLAN15_16_BUILD_PASS "+report.summary.totalSize+" bytes");
    }
}
