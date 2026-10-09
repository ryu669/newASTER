using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Generated fixtures are bundled separately and removed before the product build.
public sealed class Plan119LoadFixtureImporter:AssetPostprocessor
{
    internal const string Root="Assets/Plan119LoadTestGenerated";
    private void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith(Root+"/",StringComparison.Ordinal))return;
        var importer=(TextureImporter)assetImporter;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        importer.mipmapEnabled=false;importer.isReadable=false;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.textureCompression=TextureImporterCompression.Compressed;
    }
}
public static class Plan119LoadFixtureBuild
{
    public static void BuildAndPlayer(){Build();Plan119BuildValidation.Build();}
    public static void Build()
    {
        string root=Plan119LoadFixtureImporter.Root;
        if(Directory.Exists(root) || File.Exists(root+".meta"))throw new InvalidOperationException("Fixture directory already exists; preserve and inspect it: "+root);
        string output=Path.GetFullPath("../../tmp/plan11-9/fixtures");Directory.CreateDirectory(output);
        const int width=1536,height=1024,count=320;
        var pixels=new Color32[width*height];
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
        try{
            Directory.CreateDirectory(root);
            AssetDatabase.StartAssetEditing();
            try{
                for(int form=0;form<count;form++){
                    // Unique deterministic synthetic pixels; no production artwork is copied.
                    for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                        uint hash=unchecked((uint)((x/12)*73856093^(y/12)*19349663^form*83492791));
                        hash^=hash>>13;hash*=1274126177;hash^=hash>>16;
                        pixels[y*width+x]=new Color32((byte)hash,(byte)(hash>>8),(byte)(hash>>16),(byte)(x<64 || x>=width-64?0:255));
                    }
                    texture.SetPixels32(pixels);texture.Apply(false);
                    File.WriteAllBytes(root+"/q119-load-"+form.ToString("D3")+".png",texture.EncodeToPNG());
                }
            }finally{AssetDatabase.StopAssetEditing();}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var assets=Directory.GetFiles(root,"*.png").Select(p=>p.Replace('\\','/')).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            if(assets.Length!=count)throw new InvalidOperationException("Incomplete fixture set.");
            var manifest=BuildPipeline.BuildAssetBundles(output,new[]{new AssetBundleBuild{assetBundleName="plan119-images",assetNames=assets}},BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64);
            if(manifest==null)throw new InvalidOperationException("Fixture bundle failed.");
            Debug.Log("PLAN11_9_FIXTURE_BUILD_PASS images="+count+" size="+width+"x"+height+" bytes="+new FileInfo(Path.Combine(output,"plan119-images")).Length+" testOnly=true");
        }finally{
            UnityEngine.Object.DestroyImmediate(texture);
            if(!AssetDatabase.DeleteAsset(root) && Directory.Exists(root))throw new InvalidOperationException("Remove owned fixture directory before product build: "+root);
        }
        if(Directory.Exists(root))throw new InvalidOperationException("Test fixtures must not enter the product build.");
    }
}
