using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
// Isolated diagnostic build only; this does not accept or release the first batch.
public static class LayeredWeaponTreePreviewBuild
{
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var name in new[]{"common-tree","silver-foliage-simple-v2","crimson-flower","silver-leaf","ruby","iron-clasp"})
            if(Resources.Load<Texture2D>("UI/WeaponTrees/"+name)==null)throw new Exception("Missing tree texture: "+name);
        if(Resources.Load<TextAsset>("UI/WeaponTrees/iron-maiden")==null)throw new Exception("Missing tree layout");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName="../Builds/layered-tree-preview/newASTER.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Tree preview build failed");
        Debug.Log("LAYERED_TREE_PREVIEW_BUILD_PASS");
    }
}
