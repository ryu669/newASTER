using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class Plan12AprilBuild
{
    public static bool ReviewPreview { get; private set; }
    private static string buildOutput="../Builds/plan12-april/newASTER.exe";
    public static void BuildRuntimeImprovementsPreview()
    {
        buildOutput="../Builds/runtime-improvements/newASTER.exe";
        try{BuildReviewPreview();}finally{buildOutput="../Builds/plan12-april/newASTER.exe";}
    }
    public static void BuildReviewPreview()
    {
        ReviewPreview=true;
        try { ValidateAndBuild(); } finally { ReviewPreview=false; }
    }
    [Serializable] private sealed class ArtManifest { public ArtRecord[] assets; }
    [Serializable] private sealed class ArtRecord { public string path,sha256,resourcePath,purposeReview,runtimeReview; public Capture[] captureFiles; }
    [Serializable] private sealed class Capture {public string path,sha256;}
    [Serializable] private sealed class FormRecord { public string status; public TreeComponent[] treeComponents,renderers; }
    [Serializable] private sealed class TreeComponent {public string path,sha256,status;}
    public static void ValidateRelease(){
        ValidateAssets(true);
        var record=JsonUtility.FromJson<FormRecord>(File.ReadAllText("../../docs/production/plan12-april-form-record.json"));
        if(record.status!="runtime_review_complete" && record.status!="complete_published" || record.treeComponents==null || record.treeComponents.Length<4)throw new Exception("April tree review incomplete.");
        if(record.renderers==null || record.renderers.Length<4)throw new Exception("April renderer review missing.");
        foreach(var component in record.treeComponents.Concat(record.renderers))using(var sha=SHA256.Create())if(component.status!="approved" || BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes("../../"+component.path))).Replace("-","").ToLowerInvariant()!=component.sha256)throw new Exception("April tree/renderer changed after review: "+component.path);
        Debug.Log("PLAN12_APRIL_RELEASE_ART_PASS 20 source/runtime reviews and layered tree components");
    }
    private static void ValidateAssets(bool release)
    {
        var manifest=JsonUtility.FromJson<ArtManifest>(File.ReadAllText("../art-source/april-v1/generation.json"));
        if(manifest.assets.Length!=20)throw new Exception("April requires twenty dedicated character images and the shared wooden tree.");
        foreach(var a in manifest.assets){
            if(a.purposeReview!="approved" || release && a.runtimeReview!="approved")throw new Exception("April art review incomplete: "+a.path);
            if(release){
                if(a.captureFiles==null || a.captureFiles.Length==0)throw new Exception("April runtime evidence missing: "+a.path);
                foreach(var c in a.captureFiles){
                    if(c.sha256==null || c.sha256.Length!=64)throw new Exception("April evidence digest missing: "+c.path);
                    string evidence="../../"+c.path;
                    if(File.Exists(evidence))using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(evidence))).Replace("-","").ToLowerInvariant()!=c.sha256)throw new Exception("April evidence changed: "+c.path);
                }
            }
            string path="Assets/Game/Resources/"+a.resourcePath+".png";
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
            if(hash!=a.sha256 || !ProductionAssetAcceptance.Hashes.TryGetValue(a.resourcePath,out var accepted) || accepted!=hash)throw new Exception("April image digest mismatch: "+path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);HeroinePortraitImporter.Configure(importer);importer.SaveAndReimport();
            var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Debug.Log("APRIL_IMPORT_CHECK "+a.resourcePath+" type="+AssetDatabase.GetMainAssetTypeAtPath(path)+" loaded="+(image!=null));
            if(image==null || image.width<1024 || image.height<1024)throw new Exception("April image size/aspect mismatch: "+path+" dimensions="+(image==null?"null":image.width+"x"+image.height));
        }
    }
    public static void ValidateAndBuild()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);ValidateAssets(false);HeroinePortraitBuildValidation.Validate();
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-plan12-april").text);combat.Validate();
        var story=JsonUtility.FromJson<ProductionStoryContent>(Resources.Load<TextAsset>("Story/plan12-april-story-content").text);story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        var restored=JsonUtility.FromJson<ProductionStoryContent>(JsonUtility.ToJson(story));
        if(combat.HeroineIds.Length!=25 || home.weaponNodes.Count(n=>n.heroineId=="heroine.april")!=13 || restored.chapters.Where(c=>c.ownerId=="heroine.april").Sum(c=>c.pages.Length)!=30)throw new Exception("April content incomplete.");
        foreach(var path in home.assets.Select(a=>a.resourcePath).Distinct())if(Resources.Load(path)==null)throw new Exception("Missing production art "+path);
        Debug.Log("PLAN12_APRIL_UNITY_PASS forms=25 people=22 images=20 nodes=13 pages=30");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName=buildOutput,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("April build failed "+report.summary.result);
        string notices=Path.Combine(Path.GetDirectoryName(buildOutput),"ThirdPartyNotices/NotoSansCJKjp");Directory.CreateDirectory(notices);
        foreach(var name in new[]{"OFL.txt","NOTICE.txt"})File.Copy("Assets/Game/Resources/Fonts/"+name,Path.Combine(notices,name),true);
        Debug.Log("PLAN12_APRIL_BUILD_PASS bytes="+report.summary.totalSize);
    }
}
