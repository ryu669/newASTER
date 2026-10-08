using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class HeroinePortraitImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath!="Assets/Game/Resources/UI/book-emblems-v1.png" && assetPath!="Assets/Game/Resources/UI/weapon-effects-v1.png" && assetPath!="Assets/Game/Resources/UI/garden-memorial-furniture-v1.png" && (!assetPath.StartsWith("Assets/Game/Resources/Illustrations/", StringComparison.Ordinal) || !Path.GetFileName(assetPath).Contains("-portrait-") && !Path.GetFileName(assetPath).Contains("-weapon-tree-"))) return;
        Configure((TextureImporter)assetImporter);
    }
    internal static void Configure(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        var platform = importer.GetPlatformTextureSettings("Standalone");
        platform.name = "Standalone"; platform.overridden = true;
        platform.maxTextureSize = 4096; platform.format = TextureImporterFormat.RGBA32;
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(platform);
    }
}
public sealed class HeroinePortraitBuildValidation : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    public void OnPreprocessBuild(BuildReport report) => Validate();
    [MenuItem("newASTER/Validate Heroine Portraits")]
    public static void Validate()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var catalog = JsonUtility.FromJson<HeroinePortraitCatalog>(Resources.Load<TextAsset>("UI/heroine-portrait-framing").text);
        catalog.Validate();
        // Include new stages automatically, and exclude only anonymous diagnostic actors.
        foreach (string combatPath in Directory.GetFiles("Assets/Game/Resources/Combat", "battle-*.json"))
        {
            var combat = JsonUtility.FromJson<CombatDefinitionCatalog>(File.ReadAllText(combatPath));
            if (combat?.heroines == null) continue;
            foreach (var heroine in combat.heroines)
                if (heroine.id.StartsWith("heroine.", StringComparison.Ordinal)) catalog.Entry(heroine.id);
        }
        foreach (var entry in catalog.entries)
        {
            string path = "Assets/Game/Resources/" + entry.resourcePath + ".png";
            byte[] source = File.ReadAllBytes(path);
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(decoded, source, false)) throw new ArgumentException("Unreadable portrait: " + path);
                catalog.ValidateSource(entry, decoded.width, decoded.height);
                var pixels = decoded.GetPixels32();
                int x = Mathf.Clamp((int)(entry.faceCenterX * decoded.width), 0, decoded.width - 1);
                int y = Mathf.Clamp((int)((1 - entry.faceCenterY) * decoded.height), 0, decoded.height - 1);
                if (!pixels.Any(p => p.a == 0) || pixels[y * decoded.width + x].a < 230)
                    throw new ArgumentException("Portrait must have transparent background and opaque face: " + path);
                string digest; using (var hash = SHA256.Create()) digest = BitConverter.ToString(hash.ComputeHash(source)).Replace("-", "").ToLowerInvariant();
                if (!ProductionAssetAcceptance.Hashes.TryGetValue(entry.resourcePath, out var expected) || digest != expected)
                    throw new ArgumentException("Unaccepted portrait source hash: " + path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                HeroinePortraitImporter.Configure(importer); importer.SaveAndReimport();
                var imported = Resources.Load<Texture2D>(entry.resourcePath);
                if (imported == null || imported.width != decoded.width || imported.height != decoded.height || imported.mipmapCount != 1)
                    throw new ArgumentException("Portrait import reduced native detail: " + path);
            }
            finally { UnityEngine.Object.DestroyImmediate(decoded); }
        }
        Debug.Log("HEROINE_PORTRAIT_BUILD_PASS " + catalog.entries.Length + " native-size alpha hash imports");
        ValidateWeaponTrees(catalog);
    }
    private static void ValidateWeaponTrees(HeroinePortraitCatalog portraits)
    {
        var json=Resources.Load<TextAsset>("UI/heroine-weapon-trees");if(json==null)throw new ArgumentException("Missing weapon tree layouts.");
        var catalog=JsonUtility.FromJson<HeroineWeaponTreeCatalog>(json.text);catalog.Validate();
        foreach(var heroine in portraits.entries)catalog.Entry(heroine.heroineId);
        foreach(var e in catalog.entries){
            string[] suffixes={"root","alpha","beta","gamma","alpha.tier2","beta.tier2","gamma.tier2","alpha.tier3","beta.tier3","gamma.tier3","alpha.tier4","beta.tier4","gamma.tier4"};foreach(string suffix in suffixes)e.Position(e.heroineId+".weapon."+suffix);
            ValidateTreeImage(e.resourcePath,e);
            for(int i=0;i<e.nodes.Length;i++)for(int j=i+1;j<e.nodes.Length;j++)if(Math.Abs(e.nodes[i].position.x-e.nodes[j].position.x)*906<76 && Math.Abs(e.nodes[i].position.y-e.nodes[j].position.y)*604<85)throw new ArgumentException("Weapon node hit areas overlap: "+e.heroineId);
        }
        ValidateTreeImage("UI/weapon-effects-v1",null);
        Debug.Log("HEROINE_WEAPON_TREE_BUILD_PASS "+catalog.entries.Length+" native image trees, 195 opaque branch anchors, distinct effect image atlas");
    }
    private static void ValidateTreeImage(string resource,HeroineWeaponTreeDef layout)
    {
        string path="Assets/Game/Resources/"+resource+".png";byte[] bytes=File.ReadAllBytes(path);var image=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try{
            if(!ImageConversion.LoadImage(image,bytes,false) || (layout!=null?(image.width<1536 || image.height<1024 || Math.Abs(image.width/(float)image.height-1.5f)>.001f):image.width!=image.height || image.width<1024))throw new ArgumentException("Invalid native weapon tree/effect image: "+path);
            var pixels=image.GetPixels32();if(!pixels.Any(p=>p.a==0))throw new ArgumentException("Weapon tree needs transparent background: "+path);
            if(layout!=null)foreach(var n in layout.nodes){int x=(int)(n.position.x*image.width),y=(int)((1-n.position.y)*image.height);if(pixels[y*image.width+x].a<230)throw new ArgumentException("Weapon node is off its illustrated branch: "+n.nodeId);}
            string digest;using(var hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();if(!ProductionAssetAcceptance.Hashes.TryGetValue(resource,out var expected) || digest!=expected)throw new ArgumentException("Unaccepted weapon tree image: "+path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);HeroinePortraitImporter.Configure(importer);importer.SaveAndReimport();var imported=Resources.Load<Texture2D>(resource);if(imported.width!=image.width || imported.height!=image.height || imported.mipmapCount!=1)throw new ArgumentException("Weapon tree import reduced native pixels: "+path);
        }finally{UnityEngine.Object.DestroyImmediate(image);}
    }
}
