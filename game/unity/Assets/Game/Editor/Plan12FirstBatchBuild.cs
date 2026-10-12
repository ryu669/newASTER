using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class Plan12FirstBatchBuild
{
    public static void ValidateAndBuild()
    {
        Plan12AssetReviewGate.Validate();
        if(ProductionContentPaths.Combat=="Combat/battle-plan12-first-batch")Plan12AssetReviewGate.ValidateRelease();
        HeroinePortraitBuildValidation.Validate();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var paths=new[]{"iron-maiden","avalanche","anathema","shiva"}.SelectMany(k=>Directory.GetFiles("Assets/Game/Resources/Illustrations","*"+k+"*.png")).ToArray();
        if(paths.Length!=84)throw new Exception("Aeon requires 21 dedicated images.");
        foreach(var path in paths){
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureShape=TextureImporterShape.Texture2D;HeroinePortraitImporter.Configure(importer);importer.SaveAndReimport();
            string resource="Illustrations/"+Path.GetFileNameWithoutExtension(path);
            if(Resources.Load<Texture2D>(resource)==null)throw new Exception("Missing Aeon image "+resource);
            string digest;using(var hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
            if(!ProductionAssetAcceptance.Hashes.TryGetValue(resource,out var adopted)||adopted!=digest)throw new Exception("Aeon image hash mismatch "+resource);
        }
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-plan12-first-batch").text);combat.Validate();
        var story=JsonUtility.FromJson<ProductionStoryContent>(Resources.Load<TextAsset>("Story/plan12-first-batch-story-content").text);story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        if(combat.HeroineIds.Length!=20 || new[]{"heroine.iron-maiden","heroine.avalanche","heroine.anathema","heroine.shiva"}.Any(id=>home.weaponNodes.Count(n=>n.heroineId==id)!=13))throw new Exception("Aeon production integration missing.");
        foreach(var path in home.assets.Select(a=>a.resourcePath).Distinct())if(Resources.Load(path)==null)throw new Exception("Missing production image "+path);
        var restored=JsonUtility.FromJson<ProductionStoryContent>(JsonUtility.ToJson(story));restored.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        if(restored.chapters.Where(c=>c.ownerId=="heroine.aeon").Sum(c=>c.pages.Length)!=30)throw new Exception("Aeon pages lost in Unity serialization.");
        Debug.Log("PLAN12_FIRST_BATCH_UNITY_PASS 20 forms / 17 people / 84 new images / 52 new tree nodes / 120 new pages");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName="../Builds/plan12-first-batch/newASTER.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Aeon build failed "+report.summary.result);
        string notices="../Builds/plan12-first-batch/ThirdPartyNotices/NotoSansCJKjp";Directory.CreateDirectory(notices);
        foreach(var name in new[]{"OFL.txt","NOTICE.txt"})File.Copy("Assets/Game/Resources/Fonts/"+name,Path.Combine(notices,name),true);
        Debug.Log("PLAN12_FIRST_BATCH_BUILD_PASS "+report.summary.totalSize+" bytes");
    }
}
