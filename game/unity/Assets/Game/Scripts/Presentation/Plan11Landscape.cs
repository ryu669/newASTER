using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private Texture2D[] terraformBackdrops;
        private Texture2D terraformGlow;
        private static readonly Color[] TerraformColors = {new Color(.25f,.8f,.43f),new Color(.2f,.65f,1),new Color(.75f,.55f,.4f),new Color(.65f,.8f,1),new Color(1,.85f,.4f),new Color(.6f,.55f,1),new Color(.25f,1,.85f)};
        private void TerraformTextures()
        {
            if(terraformBackdrops!=null)return;
            string[] names={"forest-far","world-water-far","world-crystal-far","heaven-forest-far","world-oasis-far","world-stars-far","world-confluence-far"};
            terraformBackdrops=names.Select(n=>Resources.Load<Texture2D>("Illustrations/"+n+"-candidate-v1")).ToArray();
            terraformGlow=new Texture2D(32,32,TextureFormat.RGBA32,false);terraformGlow.name="Terraform glow";
            var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float distance=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/16;pixels[y*32+x]=new Color(1,1,1,Mathf.Clamp01(1-distance)*Mathf.Clamp01(1-distance));}
            terraformGlow.SetPixels(pixels);terraformGlow.Apply(false,true);
        }
        private void TerraformDot(float x,float y,float size,Color color)
        {
            var previous=GUI.color;GUI.color=color;GUI.DrawTexture(new Rect(x-size/2,y-size/2,size,size),terraformGlow);GUI.color=previous;
        }
        private void TerraformLine(Vector2 start,Vector2 end,float width,Color color)
        {
            var previous=GUI.matrix;float length=Vector2.Distance(start,end);
            GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg,start);
            GrowthFill(start.x,start.y-width/2,length,width,color);GUI.matrix=previous;
        }
        private void TerraformLayer(Rect bounds,Texture2D image,float alpha)
        {if(image==null)return;var old=GUI.color;GUI.color=new Color(1,1,1,alpha);GUI.DrawTexture(bounds,image,ScaleMode.ScaleAndCrop,true);GUI.color=old;}
        private void DrawTerraformLandscape(Rect bounds,TerraformSave s,bool baseLayer)
        {
            TerraformTextures();GUI.BeginGroup(bounds);
            try {
                var local=new Rect(0,0,bounds.width,bounds.height);float w=local.width,h=local.height,t=Time.realtimeSinceStartup;
                var formed=s.domains.Where(d=>TerraformRules.DomainIds.Contains(d.domainId) && d.currentLevel>0).OrderByDescending(d=>d.currentLevel).ThenBy(d=>Array.IndexOf(TerraformRules.DomainIds,d.domainId)).ToArray();
                if(baseLayer){
                    GrowthFill(0,0,w,h,new Color(.025f,.045f,.09f));
                    if(formed.Length==0){for(int i=0;i<35;i++)TerraformDot(Mathf.Repeat(i*73.5f,w),Mathf.Repeat(i*39.3f,h),3,new Color(.7f,.8f,1,.55f));}
                    else{
                        var main=formed[0];int index=Array.IndexOf(TerraformRules.DomainIds,main.domainId);
                        TerraformLayer(local,terraformBackdrops[index],Mathf.Lerp(.32f,1,Mathf.Min(main.currentLevel,5)/5f));
                        var extreme=TerraformCatalog.Extremes.FirstOrDefault(e=>e.id==main.activeExtremeId);
                        if(extreme!=null)TerraformLayer(new Rect(w*.32f,0,w*.68f,h),terraformBackdrops[Array.IndexOf(TerraformRules.DomainIds,extreme.fusionDomainId)],.48f);
                        foreach(var domain in formed){
                            int i=Array.IndexOf(TerraformRules.DomainIds,domain.domainId);float intensity=domain.currentLevel/7f;
                            var fusion=TerraformCatalog.Extremes.FirstOrDefault(e=>e.id==domain.activeExtremeId);
                            if(fusion!=null)DrawTerraformFusion(w*(i+.5f)/7,h*(.48f+(i%3)*.07f),Mathf.Max(14,w/14),fusion.fusionDomainId);
                            if(i==0)for(int j=0;j<8;j++)TerraformDot(w*(j+.5f)/8,h*.84f+Mathf.Sin(j*2.3f)*h*.05f,12+intensity*20,new Color(.3f,.9f,.4f,.5f));
                            else if(i==1)for(int j=0;j<7;j++)TerraformLine(new Vector2(w*j/7,h*.79f),new Vector2(w*(j+.8f)/7,h*.79f),2,new Color(.35f,.75f,1,.5f));
                            else if(i==2)for(int j=0;j<6;j++){Vector2 a=new Vector2(w*j/6,h*.74f),b=new Vector2(w*(j+.5f)/6,h*.64f);TerraformLine(a,b,3,new Color(.75f,.65f,.5f,.55f));}
                            else if(i==3)for(int j=0;j<5;j++)TerraformDot(w*(j+.5f)/5,h*.18f,22+intensity*30,new Color(.8f,.9f,1,.35f));
                            else if(i==4)TerraformDot(w*.78f,h*.2f,40+intensity*80,new Color(1,.9f,.55f,.65f));
                            else if(i==5)for(int j=0;j<24;j++)TerraformDot(Mathf.Repeat(j*83.7f,w),Mathf.Repeat(j*17.3f,h*.45f),4,new Color(.9f,.85f,1,.8f));
                            else for(int j=0;j<8;j++){float x=w*(j+.3f)/8,height=h*(.06f+.05f*Mathf.Sin(j*5));GrowthFill(x,h*.85f-height,w*.035f,height,new Color(.05f,.17f,.2f,.65f));GrowthFill(x+2,h*.85f-height+4,2,height*.7f,new Color(.3f,1,.85f,.7f));}
                        }
                    }
                }
                foreach(string deep in formed.Where(d=>d.currentLevel>=6 && d.activeDeepRecordId!=null).Select(d=>d.activeDeepRecordId).Distinct())DrawTerraformModifier(local,deep,t);
                if(s.activeWorldPhenomenonId!=null)DrawTerraformPhenomenon(local,s.activeWorldPhenomenonId,t);
            }finally{GUI.EndGroup();}
        }
        private void DrawTerraformFusion(float x,float y,float size,string domain)
        {
            int index=Array.IndexOf(TerraformRules.DomainIds,domain);if(index<0)return;
            Color color=TerraformColors[index];color.a=.7f;
            switch(index){
                case 0:TerraformLine(new Vector2(x,y+size),new Vector2(x,y-size),2,color);TerraformLine(new Vector2(x,y),new Vector2(x-size*.6f,y-size*.5f),2,color);TerraformDot(x,y-size,size,color);break;
                case 1:for(int j=0;j<3;j++)TerraformLine(new Vector2(x-size,y+j*4),new Vector2(x+size,y+j*4),1,color);break;
                case 2:TerraformLine(new Vector2(x-size,y+size),new Vector2(x,y-size),2,color);TerraformLine(new Vector2(x,y-size),new Vector2(x+size,y+size),2,color);break;
                case 3:TerraformDot(x-size*.4f,y,size*1.3f,color);TerraformDot(x+size*.4f,y,size*1.3f,color);TerraformLine(new Vector2(x-size,y+size),new Vector2(x+size,y+size),2,color);break;
                case 4:TerraformDot(x,y,size*2,color);TerraformLine(new Vector2(x-size,y),new Vector2(x+size,y),1,color);break;
                case 5:TerraformDot(x-size,y-size,7,color);TerraformDot(x,y,7,color);TerraformDot(x+size,y-size*.4f,7,color);TerraformLine(new Vector2(x-size,y-size),new Vector2(x,y),1,color);TerraformLine(new Vector2(x,y),new Vector2(x+size,y-size*.4f),1,color);break;
                case 6:for(int j=0;j<3;j++){GrowthFill(x-size+j*size*.7f,y-size*(.5f+j*.2f),size*.35f,size*(.7f+j*.2f),new Color(.04f,.15f,.19f,.8f));TerraformLine(new Vector2(x-size+j*size*.7f,y-size*.5f),new Vector2(x-size+j*size*.7f,y+size*.2f),1,color);}break;
            }
        }
        private void DrawTerraformModifier(Rect bounds,string id,float t)
        {
            float w=bounds.width,h=bounds.height;int index=Array.IndexOf(TerraformRules.DeepIds,id);if(index<0)return;
            Color color=TerraformColors[index];color.a=.28f;
            for(int i=0;i<12;i++){
                float x=w*(i+.5f)/12,y=h*.45f+Mathf.Sin(i*2.5f)*h*.2f;
                switch(index){
                    case 0:TerraformDot(x,h*.82f,20+8*Mathf.Sin(t+i),color);TerraformLine(new Vector2(x,h*.85f),new Vector2(x-8,h*.75f),2,color);break;
                    case 1:TerraformLine(new Vector2(x,0),new Vector2(x,h),1,color);TerraformLine(new Vector2(0,h*i/12),new Vector2(w,h*i/12),1,color);break;
                    case 2:TerraformDot(x,y,24+Mathf.Sin(t*.7f+i)*8,color);TerraformLine(new Vector2(x-10,y),new Vector2(x+10,y),1,color);break;
                    case 3:TerraformDot(x,y+Mathf.Sin(t+i)*h*.08f,30,color);break;
                    case 4:TerraformLine(new Vector2(x,0),new Vector2(x,h),1,color);TerraformDot(x,h*.5f,16,color);break;
                    case 5:GrowthFill(x+Mathf.Sin(t*.4f+i)*10,y,w*.05f,5,color);break;
                    case 6:TerraformLine(new Vector2(x,y-8),new Vector2(x,y+8),1,color);TerraformLine(new Vector2(x-8,y),new Vector2(x+8,y),1,color);break;
                }
            }
        }
        private void DrawTerraformPhenomenon(Rect bounds,string id,float t)
        {
            float w=bounds.width,h=bounds.height;
            if(id=="aurora"){
                for(int band=0;band<5;band++)for(int j=0;j<24;j++){
                    float x=w*j/24,y=h*(.16f+band*.035f)+Mathf.Sin(j*.28f+t*.2f+band)*h*.055f;
                    TerraformLine(new Vector2(x,y),new Vector2(x+w/24,y+Mathf.Cos(j*.28f+t*.2f)*h*.013f),5,new Color(.2f+band*.12f,1-band*.12f,.6f+band*.08f,.35f));
                }return;
            }
            if(id=="thunderclouds"){
                for(int j=0;j<14;j++)TerraformDot(w*j/13,h*.14f,Mathf.Max(80,w*.2f),new Color(.08f,.1f,.17f,.8f));
                if(Mathf.Repeat(t,4)>.18f)return;
                GrowthFill(0,0,w,h,new Color(.7f,.85f,1,.12f));Vector2 point=new Vector2(w*.62f,h*.14f);
                for(int j=0;j<5;j++){var next=new Vector2(point.x+(j%2==0?-15:10),point.y+h*.1f);TerraformLine(point,next,3,new Color(.85f,.95f,1,.9f));point=next;}return;
            }
            if(id=="sea_fireworks"){
                for(int burst=0;burst<3;burst++){
                    float phase=Mathf.Repeat(t*.3f+burst*.31f,1),radius=phase*h*.23f;var center=new Vector2(w*(burst+.6f)/3.8f,h*.36f);
                    var color=TerraformColors[(burst*2)%7];color.a=1-phase;
                    for(int ray=0;ray<14;ray++){float angle=ray*Mathf.PI*2/14;var end=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;TerraformLine(end,end+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*5,2,color);TerraformDot(end.x,h*.85f+(h*.7f-end.y)*.1f,5,new Color(color.r,color.g,color.b,color.a*.5f));}
                }return;
            }
            for(int j=0;j<50;j++){
                float x=Mathf.Repeat(j*73.1f+t*(id=="rain"?18:6),w),y=Mathf.Repeat(j*41.7f+t*(id=="rain"?150:id=="snow"?24:12),h);
                switch(id){
                    case "rain":TerraformLine(new Vector2(x,y),new Vector2(x-3,y+14),1,new Color(.6f,.8f,1,.45f));break;
                    case "snow":TerraformDot(x+Mathf.Sin(t+j)*4,y,7,new Color(1,1,1,.85f));break;
                    case "petals":TerraformLine(new Vector2(x,y),new Vector2(x+Mathf.Sin(t+j)*6,y+3),3,new Color(1,.65f,.82f,.75f));break;
                    case "fireflies":TerraformDot(x,h*.45f+y*.55f,10,new Color(.7f,1,.25f,.2f+.6f*Mathf.Abs(Mathf.Sin(t+j))));break;
                    case "meteors":if(j<8){float travel=Mathf.Repeat(t*.16f+j*.137f,1);var point=new Vector2(w*travel,h*.5f*travel+j*h*.045f);TerraformLine(point,point-new Vector2(30,15),1,new Color(.8f,.9f,1,.65f));TerraformDot(point.x,point.y,7,Color.white);}break;
                    case "crystal_rain":TerraformLine(new Vector2(x,y-5),new Vector2(x+3,y),1,new Color(.6f,1,1,.9f));TerraformLine(new Vector2(x+3,y),new Vector2(x,y+5),1,new Color(.8f,.65f,1,.9f));break;
                    case "neon_festival":if(j<18){var color=TerraformColors[j%7];color.a=.45f;TerraformDot(x,h*.78f+Mathf.Sin(t+j)*h*.04f,20,color);TerraformLine(new Vector2(x,h*.78f),new Vector2(x,h*.94f),1,color);}break;
                }
            }
        }
    }
}
