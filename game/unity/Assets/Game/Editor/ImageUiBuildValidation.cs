using System;
using UnityEditor;
using UnityEngine;
using NewAster.Presentation;

public static partial class PlayableBuild
{
    private static void ValidateImageUi()
    {
        foreach(string name in new[]{"panel-v1","parchment-v1","button-v1","library-v1"}){
            var image=ImageUiSkin.Image(name);
            Check(image!=null && image.width>=1024 && image.height>=512,"Image UI full resolution import: "+name);
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Game/Resources/UiImages/"+name+".png");
            Check(!importer.isReadable && !importer.mipmapEnabled,"UI avoids CPU image copies / mipmaps: "+name);
        }
        Check(ImageUiSkin.Image("button-v1").width==1536 && ImageUiSkin.Image("button-v1").height==1024,"Authored button UV dimensions");
        Debug.Log("IMAGE_UI_BUILD_PASS 9 image checks");
    }
}
