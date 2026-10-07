using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Plan11Build
{
    public static void Build()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName="../Builds/plan11-1/newASTER.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Plan11 build failed: "+report.summary.result);
        string notices=Path.GetFullPath("../Builds/plan11-1/ThirdPartyNotices/NotoSansCJKjp");Directory.CreateDirectory(notices);
        File.Copy("Assets/Game/Resources/Fonts/OFL.txt",Path.Combine(notices,"OFL.txt"),true);
        File.Copy("Assets/Game/Resources/Fonts/NOTICE.txt",Path.Combine(notices,"NOTICE.txt"),true);
        Debug.Log("PLAN11_BUILD_PASS "+report.summary.totalSize+" bytes");
    }
}
