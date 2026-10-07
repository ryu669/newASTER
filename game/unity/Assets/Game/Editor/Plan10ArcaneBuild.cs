using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class Plan10ArcaneBuild
{
    public static void ValidateAndBuild()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var paths=Directory.GetFiles("Assets/Game/Resources/Illustrations","arcane-*.png");
        if(paths.Length!=18)throw new Exception("Arcane requires 18 distinct art assets.");
        foreach(string path in paths){
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            string resource="Illustrations/"+Path.GetFileNameWithoutExtension(path);if(Resources.Load<Texture2D>(resource)==null)throw new Exception("Missing texture "+resource);
            string digest;using(var hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
            if(!ProductionAssetAcceptance.Hashes.TryGetValue(resource,out var adopted) || adopted!=digest)throw new Exception("Asset adoption mismatch "+resource);
        }
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-plan10-arcane").text);combat.Validate();
        var story=JsonUtility.FromJson<ProductionStoryContent>(Resources.Load<TextAsset>("Story/plan10-arcane-story-content").text);story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        foreach(var path in home.assets.Select(a=>a.resourcePath).Distinct())if(Resources.Load(path)==null)throw new Exception("Missing production art "+path);
        var roundtrip=JsonUtility.FromJson<ProductionStoryContent>(JsonUtility.ToJson(story));roundtrip.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        foreach(string id in new[]{"heroine.arcane"})if(roundtrip.chapters.Where(c=>c.ownerId==id).Sum(c=>c.pages.Length)!=30)throw new Exception("Narrative lost in Unity JSON "+id);
        Debug.Log("PLAN10_ARCANE_UNITY_PASS thirteen forms / eleven people / thirty Arcane pages / eighteen images");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName="../Builds/plan10-arcane/newASTER.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Arcane build failed "+report.summary.result);
        string notices="../Builds/plan10-arcane/ThirdPartyNotices/NotoSansCJKjp";Directory.CreateDirectory(notices);foreach(string name in new[]{"OFL.txt","NOTICE.txt"})File.Copy("Assets/Game/Resources/Fonts/"+name,Path.Combine(notices,name),true);
        Debug.Log("PLAN10_ARCANE_BUILD_PASS "+report.summary.totalSize+" bytes");
    }
}
