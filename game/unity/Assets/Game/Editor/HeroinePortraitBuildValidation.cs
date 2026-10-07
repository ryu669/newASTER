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
        if (!assetPath.StartsWith("Assets/Game/Resources/Illustrations/", StringComparison.Ordinal) || !Path.GetFileName(assetPath).Contains("-portrait-")) return;
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
    }
}
