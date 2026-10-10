using System;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private string plan9Expression;
        private string plan9ArtHero="heroine.iconoclast";
        private string plan9Cg;
        private void DrawPlan9EventCg()
        {
            GrowthStyles();TitleFill(new Rect(0,0,1600,900),titleInk);
            Label(40,20,1520,50,combatDefinitions.Hero(plan9ArtHero).name+" ／ 交流CG制作候補 "+plan9Cg,heading,titleGold);
            var texture=Resources.Load<Texture2D>("Illustrations/"+plan9ArtHero.Substring("heroine.".Length)+"-event-"+plan9Cg+"-cg-candidate-v1");
            if(texture==null)throw new InvalidOperationException("Event CG candidate missing");
            GUI.DrawTexture(new Rect(40,90,1520,740),texture,ScaleMode.ScaleToFit,false);
            Label(40,845,1520,40,"本文・解放条件・正式採用の審査前",small,Color.white);
        }
        private Material plan9FaceMaterial;
        private Material FacePatchMaterial()
        {
            if(plan9FaceMaterial==null){var shader=Resources.Load<Shader>("Shaders/FacePatch");if(shader==null)throw new InvalidOperationException("Face patch shader missing");plan9FaceMaterial=new Material(shader);}
            return plan9FaceMaterial;
        }
        private readonly NewAster.Core.BoundedCache<string,RenderTexture> plan9FacePatches=new NewAster.Core.BoundedCache<string,RenderTexture>(12,released:texture=>UnityEngine.Object.Destroy(texture));
        private static readonly Rect IconoclastFace=new Rect(466f/1024,280f/1536,110f/1024,83f/1536);
        private void DrawPlan9CharacterArt()
        {
            GrowthStyles();TitleFill(new Rect(0,0,1600,900),titleInk);
            Label(40,30,1520,60,combatDefinitions.Hero(plan9ArtHero).name+" ／ 表情合成の制作審査",heading,titleGold);
            TitleBorder(new Rect(40,145,1130,615));
            string prefix=plan9ArtHero.Substring("heroine.".Length);
            bool undermine=prefix=="undermine";
            bool echidna=prefix=="echidna";
            bool excalipan=prefix=="excalipan";
            bool slayer=prefix=="slayer";
            var standing=Resources.Load<Texture2D>("Illustrations/"+prefix+"-standing-candidate-v1");
            var source=plan9Expression=="normal"?null:Resources.Load<Texture2D>("Illustrations/"+prefix+"-expression-"+plan9Expression+(slayer?"-candidate-v1":"-source-v1"));
            var sourceRegion=plan9Expression=="joy"?new Rect(461f/1024,230f/1536,110f/1024,83f/1536):
                plan9Expression=="puzzled"?new Rect(343f/1024,626f/1536,376f/1024,284f/1536):new Rect(403f/1230,588f/1280,440f/1230,327f/1280);
            var face=undermine?new Rect(473f/1024,302f/1536,100f/1024,82f/1536):IconoclastFace;
            if(undermine || prefix=="iconoclast")sourceRegion=face;
            if(slayer){face=new Rect(435f/1024,178f/1536,111f/1024,98f/1536);sourceRegion=face;}
            if(echidna){face=new Rect(466f/1024,210f/1536,113f/1024,76f/1536);sourceRegion=face;}
            if(excalipan){face=new Rect(432f/1024,182f/1536,130f/1024,104f/1536);sourceRegion=new Rect(face.x,(plan9Expression=="joy"?174f:182f)/1536,face.width,face.height);}
            DrawMappedExpression(new Rect(65,180,540,560),standing,source,face,sourceRegion,new Rect(0,0,1,1));
            DrawMappedExpression(new Rect(650,260,460,300),standing,source,face,sourceRegion,slayer?new Rect(.39f,.09f,.23f,.13f):excalipan?new Rect(.39f,.10f,.23f,.13f):echidna?new Rect(.40f,.115f,.22f,.12f):undermine?new Rect(.40f,.18f,.22f,.12f):new Rect(.39f,.155f,.235f,.12f));
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
                        var material=FacePatchMaterial();material.SetVector("_SourceRegion",new Vector4(sourceRegion.x,1-sourceRegion.y-sourceRegion.height,sourceRegion.width,sourceRegion.height));
                        texture=new RenderTexture(256,256,0,RenderTextureFormat.ARGB32){filterMode=FilterMode.Bilinear,name="heroine-face-patch/"+key};texture.Create();
                        var previous=RenderTexture.active;
                        try{Graphics.Blit(source,texture,material);}finally{RenderTexture.active=previous;material.SetTexture("_MainTex",null);}
                        plan9FacePatches[key]=texture;
                    }
                    GUI.DrawTexture(patch,texture,ScaleMode.StretchToFill,true);
                }
            }finally{GUI.EndGroup();}
        }
    }
}
