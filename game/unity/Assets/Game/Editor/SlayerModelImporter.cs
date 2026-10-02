using UnityEditor;
using UnityEngine;

public sealed class SlayerModelImporter : AssetPostprocessor
{
    public override uint GetVersion() => 3;
    private void OnPreprocessModel()
    {
        if(!assetPath.EndsWith("/slayer-production-v1.fbx") && !assetPath.EndsWith("/slayer-beauty-v2.fbx")) return;
        var importer=(ModelImporter)assetImporter;
        importer.animationType=ModelImporterAnimationType.Legacy;
        importer.importAnimation=true; importer.importCameras=false; importer.importLights=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.isReadable=false; importer.meshCompression=ModelImporterMeshCompression.Off;
        importer.maxBonesPerVertex=4; importer.skinWeights=ModelImporterSkinWeights.Custom;
        // Explicit take mapping works on first import, before defaultClipAnimations is populated.
        importer.clipAnimations=new[]{Clip("Idle",60,true),Clip("Attack",24,false),Clip("Cast",30,true),Clip("Hit",18,false),Clip("Victory",60,false)};
    }
    private static ModelImporterClipAnimation Clip(string name,int end,bool loop) => new ModelImporterClipAnimation {
        name=name,takeName="Slayer_Rig|"+name,firstFrame=0,lastFrame=end,loopTime=loop,wrapMode=loop?WrapMode.Loop:WrapMode.Once
    };
}
