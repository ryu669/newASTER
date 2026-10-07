using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class Plan10RBuild
{
    public static void ValidateAndBuild()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(string path in Directory.GetFiles("Assets/Game/Resources/Illustrations","r-*.png")){
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            string resource="Illustrations/"+Path.GetFileNameWithoutExtension(path);
            var asset=AssetDatabase.LoadMainAssetAtPath(path.Replace('\\','/'));
            Debug.Log("R_TEXTURE_IMPORT "+resource+" main="+(asset==null?"null":asset.GetType().Name));
            if(Resources.Load<Texture2D>(resource)==null)throw new Exception("R texture import missing: "+resource);
            string digest;using(var hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
            if(!ProductionAssetAcceptance.Hashes.TryGetValue(resource,out var adopted) || adopted!=digest)throw new Exception("R asset adoption mismatch: "+resource);
        }
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-plan10").text);combat.Validate();
        var story=JsonUtility.FromJson<ProductionStoryContent>(Resources.Load<TextAsset>("Story/plan10-story-content").text);story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var home=ProductionStoryCatalog.Home(combat,story);home.Validate(true);
        foreach(var path in home.assets.Select(a=>a.resourcePath).Distinct()){
            if(Resources.Load(path)==null)throw new Exception("Missing R production asset: "+path);
            if(path.StartsWith("Illustrations/r-",StringComparison.Ordinal)){
                string actual;using(var hash=SHA256.Create())actual=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes("Assets/Game/Resources/"+path+".png"))).Replace("-","").ToLowerInvariant();
                if(ProductionAssetAcceptance.Hashes[path]!=actual)throw new Exception("R adoption hash mismatch: "+path);
            }
        }
        var serialized=JsonUtility.FromJson<ProductionStoryContent>(JsonUtility.ToJson(story));serialized.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        if(serialized.chapters.Where(c=>c.ownerId=="heroine.r").Sum(c=>c.pages.Length)!=30)throw new Exception("R pages lost through Unity JSON.");
        Debug.Log("PLAN10_R_UNITY_PASS six-person roster / thirty pages / adopted assets / Unity JSON");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Game/Scenes/Bootstrap.unity"},locationPathName="../Builds/plan10-r/newASTER.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Plan10 R build failed: "+report.summary.result);
        string notices="../Builds/plan10-r/ThirdPartyNotices/NotoSansCJKjp";Directory.CreateDirectory(notices);
        foreach(string name in new[]{"OFL.txt","NOTICE.txt"})File.Copy("Assets/Game/Resources/Fonts/"+name,Path.Combine(notices,name),true);
        Debug.Log("PLAN10_R_BUILD_PASS "+report.summary.totalSize+" bytes");
    }
}
