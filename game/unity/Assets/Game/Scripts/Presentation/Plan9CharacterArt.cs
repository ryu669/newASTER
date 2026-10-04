using System;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private string plan9Expression;
        private string plan9Cg;
        private void DrawPlan9EventCg()
        {
            GrowthStyles();TitleFill(new Rect(0,0,1600,900),titleInk);
            Label(40,20,1520,50,"アイコノクラスト ／ 交流CG制作候補 "+plan9Cg,heading,titleGold);
            var texture=Resources.Load<Texture2D>("Illustrations/iconoclast-event-"+plan9Cg+"-cg-candidate-v1");
            if(texture==null)throw new InvalidOperationException("Event CG candidate missing");
            GUI.DrawTexture(new Rect(40,90,1520,740),texture,ScaleMode.ScaleToFit,false);
            Label(40,845,1520,40,"本文・解放条件・正式採用の審査前",small,Color.white);
        }
        private readonly System.Collections.Generic.Dictionary<string,RenderTexture> plan9FacePatches=new System.Collections.Generic.Dictionary<string,RenderTexture>();
        private static readonly Rect IconoclastFace=new Rect(466f/1024,280f/1536,110f/1024,83f/1536);
        private void DrawPlan9CharacterArt()
        {
            GrowthStyles();TitleFill(new Rect(0,0,1600,900),titleInk);
            Label(40,30,1520,60,"アイコノクラスト ／ 表情合成の制作審査",heading,titleGold);
            TitleBorder(new Rect(40,145,1130,615));
            var standing=Resources.Load<Texture2D>("Illustrations/iconoclast-standing-candidate-v1");
            var source=plan9Expression=="normal"?null:Resources.Load<Texture2D>("Illustrations/iconoclast-expression-"+plan9Expression+"-source-v1");
            var sourceRegion=plan9Expression=="joy"?new Rect(461f/1024,230f/1536,110f/1024,83f/1536):
                plan9Expression=="puzzled"?new Rect(343f/1024,626f/1536,376f/1024,284f/1536):new Rect(403f/1230,588f/1280,440f/1230,327f/1280);
            DrawMappedExpression(new Rect(65,180,540,560),standing,source,IconoclastFace,sourceRegion,new Rect(0,0,1,1));
            DrawMappedExpression(new Rect(650,260,460,300),standing,source,IconoclastFace,sourceRegion,new Rect(.39f,.155f,.235f,.12f));
            string[] variants={"normal","joy","puzzled","determined"};string[] captions={"通常","喜び","困惑","決意"};
            for(int i=0;i<4;i++)if(TitleButton(1200,250+i*60,340,48,captions[i]))plan9Expression=variants[i];
            Label(40,800,1510,65,"元の立ち絵を保持し、顔の領域のみ重ねます。\n顔以外の不変と、顔の自然さを別に審査します。",small,Color.white);
        }
        private void DrawMappedExpression(Rect destination,Texture2D standing,Texture2D source,Rect face,Rect sourceRegion,Rect crop)
        {
            if(standing==null)throw new InvalidOperationException("Iconoclast standing image missing");
            float scale=Math.Min(destination.width/(standing.width*crop.width),destination.height/(standing.height*crop.height));
            destination=new Rect(destination.x+(destination.width-standing.width*crop.width*scale)/2,destination.y+(destination.height-standing.height*crop.height*scale)/2,standing.width*crop.width*scale,standing.height*crop.height*scale);
            GUI.BeginGroup(destination);
            try{
                GUI.DrawTextureWithTexCoords(new Rect(0,0,destination.width,destination.height),standing,new Rect(crop.x,1-crop.y-crop.height,crop.width,crop.height),true);
                if(source!=null){
                    var patch=new Rect((face.x-crop.x)/crop.width*destination.width,(face.y-crop.y)/crop.height*destination.height,face.width/crop.width*destination.width,face.height/crop.height*destination.height);
                    string key=source.name+sourceRegion.ToString();
                    if(!plan9FacePatches.TryGetValue(key,out var texture)){
                        var shader=Resources.Load<Shader>("Shaders/FacePatch");
                        if(shader==null)throw new InvalidOperationException("Face patch shader missing");
                        var material=new Material(shader);material.SetVector("_SourceRegion",new Vector4(sourceRegion.x,1-sourceRegion.y-sourceRegion.height,sourceRegion.width,sourceRegion.height));
                        texture=new RenderTexture(256,256,0,RenderTextureFormat.ARGB32){filterMode=FilterMode.Bilinear};texture.Create();
                        var previous=RenderTexture.active;
                        try{Graphics.Blit(source,texture,material);}finally{RenderTexture.active=previous;Destroy(material);}
                        plan9FacePatches.Add(key,texture);
                    }
                    GUI.DrawTexture(patch,texture,ScaleMode.StretchToFill,true);
                }
            }finally{GUI.EndGroup();}
        }
    }
}
