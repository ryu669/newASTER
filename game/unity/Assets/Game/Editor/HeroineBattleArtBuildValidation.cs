using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
public sealed class HeroineBattleArtImporter:AssetPostprocessor
{
    internal static bool IsBattleArt(string path)=>path.Replace('\\','/').StartsWith("Assets/Game/Resources/Illustrations/",StringComparison.Ordinal) && new[]{"-standing-","-attack-","-hit-","-cutin-","-bare-"}.Any(Path.GetFileName(path).Contains);
    private void OnPreprocessTexture(){if(IsBattleArt(assetPath))Configure((TextureImporter)assetImporter);}
    internal static void Configure(TextureImporter importer){importer.npotScale=TextureImporterNPOTScale.None;}
}
public sealed class HeroineBattleArtBuildValidation:IPreprocessBuildWithReport
{
    public int callbackOrder=>-95;
    public void OnPreprocessBuild(BuildReport report)=>Validate();
    internal static int BigEndian(byte[] data,int at)=>(data[at]<<24)|(data[at+1]<<16)|(data[at+2]<<8)|data[at+3];
    public static void Validate()
    {
        int count=0;
        foreach(string path in Directory.GetFiles("Assets/Game/Resources/Illustrations","*.png").Where(HeroineBattleArtImporter.IsBattleArt)){
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new BuildFailedException(path+" / importer / Missing texture importer");
            if(importer.npotScale!=TextureImporterNPOTScale.None){HeroineBattleArtImporter.Configure(importer);importer.SaveAndReimport();}
            byte[] source=File.ReadAllBytes(path);int width=BigEndian(source,16),height=BigEndian(source,20);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null || (long)texture.width*height!=(long)width*texture.height)throw new BuildFailedException(path+" / aspect / Source "+width+"x"+height+" imported "+texture?.width+"x"+texture?.height);
            count++;
        }
        if(count==0)throw new BuildFailedException("No battle art found for aspect validation.");
        Debug.Log("HEROINE_BATTLE_ART_ASPECT_PASS images="+count+" npot=None sourceRatio=preserved");
    }
}
