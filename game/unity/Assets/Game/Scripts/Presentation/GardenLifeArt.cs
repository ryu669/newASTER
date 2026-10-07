using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        private Texture2D gardenMemorialAtlas;
        [Serializable] private sealed class MemorialSprite{public int index;public HomeRect uv;public float aspect;}
        [Serializable] private sealed class MemorialLayout{public MemorialSprite[] entries;}
        private MemorialLayout memorialLayout;
        private void DrawMemorialFurniture(Rect area,HomePlacement p)
        {
            if(gardenMemorialAtlas==null)gardenMemorialAtlas=Resources.Load<Texture2D>(ProductionGardenLifeCatalog.FurnitureAtlas);if(gardenMemorialAtlas==null)throw new InvalidOperationException("Missing memorial furniture atlas.");
            int index=Array.IndexOf(GardenLifeCatalog.RecipeIds,p.defId.Substring("furniture.memorial.".Length));if(index<0)throw new ArgumentException("Unknown memorial image.");
            if(memorialLayout==null)memorialLayout=JsonUtility.FromJson<MemorialLayout>(Resources.Load<TextAsset>("UI/garden-memorial-layout").text);
            var sprite=memorialLayout.entries.Single(e=>e.index==index);float[] heights={88,120,200,130,150,150,140,140,180,185};float height=area.width/1600*heights[index],width=height*sprite.aspect;
            var r=new Rect(area.x+p.x*area.width-width*.5f,area.y+p.y*area.height-height,width,height);var old=GUI.matrix;try{
                var pivot=new Vector2(r.center.x,r.yMax);if(p.orientationId.Contains("flip"))GUIUtility.ScaleAroundPivot(new Vector2(-1,1),pivot);
                if(p.orientationId.Contains("rotate"))GUIUtility.RotateAroundPivot(90,pivot);
                GUI.DrawTextureWithTexCoords(r,gardenMemorialAtlas,new Rect(sprite.uv.x,sprite.uv.y,sprite.uv.width,sprite.uv.height),true);
            }finally{GUI.matrix=old;}
        }
        private void DrawLifeEnvironment(Rect view,GardenLifeSetting setting)
        {
            string time=setting.timePhase;Color tint=time=="morning"?new Color(1,.72f,.4f,.09f):time=="evening"?new Color(.9f,.33f,.18f,.17f):time=="night"?new Color(.025f,.035f,.15f,.36f):Color.clear;
            GrowthFill(view.x,view.y,view.width,view.height,tint);
            string weather=gardenLifeRuntime?.Weather??setting.weather;float t=Time.realtimeSinceStartup;
            if(weather=="cloudy" || weather=="rain")GrowthFill(view.x,view.y,view.width,view.height,new Color(.08f,.14f,.22f,.14f));
            int count=ArtSampleSettings.ReducedMotion || ArtSampleSettings.ReducedFlash?12:48;
            if(weather=="rain" || weather=="snow")for(int i=0;i<count;i++){
                float x=view.x+Mathf.Repeat(i*127.1f+t*(weather=="snow"?8:35),view.width),y=view.y+Mathf.Repeat(i*53.4f+t*(weather=="snow"?28:210),view.height);
                if(weather=="snow"){TerraformTextures();TerraformDot(x,y,6,new Color(.92f,.97f,1,.65f));}else GrowthFill(x,y,1,14,new Color(.62f,.79f,.96f,.28f));
            }
        }
        private void DrawGardenViewFrame(Rect view,string frameId)
        {
            int frameIndex=Array.IndexOf(GardenLifeCatalog.Frames,frameId);if(frameIndex<0)throw new ArgumentException("Unknown view frame.");
            Color[] palettes={Color.white,new Color(1,.65f,.85f),new Color(.55f,1,.65f),new Color(.5f,.8f,1),new Color(.75f,.65f,1),new Color(.82f,.96f,1),new Color(1,.8f,.3f),new Color(1,.8f,.65f),new Color(.3f,1,.95f),new Color(1,.92f,.72f)};
            var oldColor=GUI.color;GUI.color=palettes[frameIndex];var image=ImageUiSkin.Image("panel-v1");float border=22;
            GUI.DrawTextureWithTexCoords(new Rect(view.x,view.y,view.width,border),image,new Rect(0,.82f,1,.18f));
            GUI.DrawTextureWithTexCoords(new Rect(view.x,view.yMax-border,view.width,border),image,new Rect(0,0,1,.18f));
            GUI.DrawTextureWithTexCoords(new Rect(view.x,view.y,border,view.height),image,new Rect(0,0,.13f,1));
            GUI.DrawTextureWithTexCoords(new Rect(view.xMax-border,view.y,border,view.height),image,new Rect(.87f,0,.13f,1));
            int[] emblems={3,1,2,7,8,7,4,2,5,8};int index=Array.IndexOf(GardenLifeCatalog.Frames,frameId);if(index<0)throw new ArgumentException("Unknown view frame.");
            foreach(var p in new[]{new Vector2(view.x+28,view.y+28),new Vector2(view.xMax-96,view.y+28),new Vector2(view.x+28,view.yMax-96),new Vector2(view.xMax-96,view.yMax-96)})DrawBookEmblem(new Rect(p.x,p.y,68,68),emblems[index]);GUI.color=oldColor;
        }
        private void DrawGardenLifeRecords()
        {
            var life=LifeSnapshot().gardenLife;Panel(95,168,1420,646,dark);Label(130,186,1000,46,"生活記録 ／ "+life.records.Length+" / 30",heading,Color.white);
            gardenLifeScroll=GUI.BeginScrollView(new Rect(130,245,1340,490),gardenLifeScroll,new Rect(0,0,1315,Math.Max(490,30*83)));
            for(int i=0;i<GardenLifeCatalog.Discoveries.Length;i++){
                var d=GardenLifeCatalog.Discoveries[i];var r=life.records.SingleOrDefault(x=>x.discoveryId==d.id);Label(15,i*83,1270,35,r==null?"未発見 ／ "+d.name:d.name,text,r==null?muted:gold);
                string body=r==null?GardenLifeDiscoveries.Hint(d):ProductionGardenCatalog.GardenName(r.context.gardenId)+" ／ "+LifeTimeName(r.context.timePhase)+" ／ "+LifeWeatherName(r.context.weather)+" ／ "+string.Join("・",r.context.domains.Select(x=>TerraformRules.DomainNames[Array.IndexOf(TerraformRules.DomainIds,x.domainId)]+" Lv"+x.currentLevel));
                Label(15,i*83+38,1270,32,body,new GUIStyle(small){fontSize=16},Color.white);
            }GUI.EndScrollView();if(Btn(1190,750,280,44,"新天地へ戻る"))gardenLifeRecords=false;
        }
    }
}
