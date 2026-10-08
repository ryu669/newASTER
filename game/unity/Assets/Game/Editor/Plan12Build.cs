using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class Plan12Build
{
    public static void Build()
    {
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName="../Builds/plan11-2/newASTER.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Plan12 build failed: "+report.summary.result);
        string notices=Path.GetFullPath("../Builds/plan11-2/ThirdPartyNotices/NotoSansCJKjp");Directory.CreateDirectory(notices);
        foreach(var file in new[]{"OFL.txt","NOTICE.txt"})File.Copy("Assets/Game/Resources/Fonts/"+file,Path.Combine(notices,file),true);
        Debug.Log("PLAN12_BUILD_PASS "+report.summary.totalSize+" bytes");
    }
}
