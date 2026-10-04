using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
using UnityEngine;

namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        // Authored image coordinates are independent of journal placement, collision footprints and costs.
        private sealed class GardenArtUse
        {
            public string furnitureId,action;
            public Vector2 ground,contact,actorContact;
            public float actorScale;
            public Vector2[][] front=Array.Empty<Vector2[]>(),hands=Array.Empty<Vector2[]>();
        }
        private static readonly GardenArtUse[] gardenArtUses={
            new GardenArtUse{furnitureId="furniture.fixture.0",action="sit",ground=new Vector2(.5f,.9f),contact=new Vector2(.52f,.59f),actorContact=new Vector2(.53f,.71f),actorScale=1.2f,
                front=new[]{GardenPolygon(.04f,.39f,.13f,.39f,.16f,.41f,.17f,.425f,.24f,.445f,.29f,.475f,.322f,.53f,.33f,.59f,.30f,.64f,.35f,.91f,.08f,.92f),GardenPolygon(.78f,.41f,.82f,.43f,.85f,.435f,.92f,.44f,.995f,.49f,1f,.86f,.73f,.86f,.76f,.64f,.81f,.57f)}},
            new GardenArtUse{furnitureId="furniture.fixture.1",action="work",ground=new Vector2(.5f,.92f),contact=new Vector2(.5f,.92f),actorContact=new Vector2(.5f,.98f),actorScale=.95f,
                front=new[]{GardenPolygon(.04f,.4f,.15f,.39f,.24f,.42f,.29f,.36f,.31f,.343f,.43f,.33f,.46f,.326f,.55f,.322f,.58f,.34f,.66f,.405f,.70f,.40f,.715f,.34f,.80f,.325f,.83f,.31f,.845f,.30f,.85f,.33f,.87f,.34f,.92f,.38f,.99f,.39f,.99f,.98f,.04f,.98f)},hands=new[]{GardenPolygon(.322f,.326f,.345f,.327f,.365f,.35f,.373f,.375f,.385f,.394f,.397f,.402f,.411f,.419f,.431f,.427f,.437f,.446f,.451f,.454f,.49f,.46f,.52f,.44f,.537f,.414f,.54f,.392f,.562f,.365f,.579f,.374f,.598f,.407f,.603f,.458f,.601f,.497f,.584f,.528f,.553f,.543f,.508f,.527f,.446f,.494f,.418f,.493f,.392f,.48f,.368f,.469f,.36f,.451f,.357f,.427f,.347f,.409f,.329f,.4f,.31f,.389f,.307f,.365f,.311f,.344f)}},
            new GardenArtUse{furnitureId="furniture.fixture.2",action="look",ground=new Vector2(.5f,.96f),contact=new Vector2(1.12f,.96f),actorContact=new Vector2(.55f,.98f),actorScale=.9f}
        };
        private static Vector2[] GardenPolygon(params float[] xy)
        {
            var result=new Vector2[xy.Length/2];for(int i=0;i<result.Length;i++)result[i]=new Vector2(xy[i*2],xy[i*2+1]);return result;
        }
        private sealed class GardenLayers { public RenderTexture back,front; }
        private readonly Dictionary<string,GardenLayers> gardenLayers=new Dictionary<string,GardenLayers>();
        private static bool GardenMaskContains(Vector2 point,Vector2[][] regions)
        {
            foreach(var polygon in regions){bool inside=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++){
                var a=polygon[i];var b=polygon[j];if((a.y>point.y)!=(b.y>point.y) && point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
            }if(inside)return true;}return false;
        }
        private GardenLayers GardenImageLayers(Texture2D source,Vector2[][] regions,string key)
        {
            if(source==null || regions.Length==0)return null;
            if(gardenLayers.TryGetValue(key,out var cached))return cached;
            if(Event.current.type!=EventType.Repaint)return null;
            var shader=Resources.Load<Shader>("Shaders/GardenRegionMask");if(shader==null || !shader.isSupported)throw new InvalidOperationException("Garden composition shader unavailable.");
            var mask=new Texture2D(source.width,source.height,TextureFormat.Alpha8,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[source.width*source.height];for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)pixels[y*source.width+x]=new Color32(255,255,255,GardenMaskContains(new Vector2((x+.5f)/source.width,1-(y+.5f)/source.height),regions)?(byte)255:(byte)0);
            mask.SetPixels32(pixels);mask.Apply(false,true);
            var material=new Material(shader);material.SetTexture("_RegionMask",mask);var layers=new GardenLayers();var previous=RenderTexture.active;
            try {
                layers.back=new RenderTexture(source.width,source.height,0,RenderTextureFormat.ARGB32){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};layers.back.Create();
                layers.front=new RenderTexture(source.width,source.height,0,RenderTextureFormat.ARGB32){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};layers.front.Create();
                material.SetFloat("_Front",0);Graphics.Blit(source,layers.back,material);material.SetFloat("_Front",1);Graphics.Blit(source,layers.front,material);
            } finally {RenderTexture.active=previous;Destroy(material);Destroy(mask);}
            gardenLayers.Add(key,layers);return layers;
        }
        private void OnDestroy()
        {
            foreach(var patch in plan9FacePatches.Values){patch.Release();Destroy(patch);}plan9FacePatches.Clear();
            foreach(var layers in gardenLayers.Values){layers.back.Release();layers.front.Release();Destroy(layers.back);Destroy(layers.front);}gardenLayers.Clear();
        }
        private static GardenArtUse GardenUse(string furnitureId,string heroineId="heroine.slayer")
        {
            var original=gardenArtUses.SingleOrDefault(a=>a.furnitureId==furnitureId);
            if(original==null || heroineId=="heroine.slayer")return original;
            if(heroineId!="heroine.iconoclast")return null;
            return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=original.action=="work"?new Vector2(.5f,.43f):original.contact,
                actorContact=original.action=="sit"?new Vector2(.50f,.66f):original.action=="work"?new Vector2(.43f,.60f):new Vector2(.55f,.94f),
                actorScale=original.action=="work"?1.35f:original.actorScale,
                hands=original.action=="work"?new[]{GardenPolygon(.26f,.558f,.52f,.558f,.55f,.62f,.43f,.635f,.27f,.625f)}:Array.Empty<Vector2[]>()};
        }
        private HomePlacement GardenUsePlacement(HomeOccupant occupant,FormalHomeProgress state)
        {
            if(occupant.furnitureInstanceId==null)return null;
            var placement=state.furniturePlacements.SingleOrDefault(p=>p.instanceId==occupant.furnitureInstanceId && p.gardenId==occupant.gardenId);
            var use=placement==null?null:GardenUse(placement.defId,occupant.heroineId);
            return use!=null && occupant.actionId=="action."+use.action?placement:null;
        }
        private Rect GardenFurnitureImageRect(Rect area,HomePlacement placement)
        {
            var definition=HomeData().furniture.Single(f=>f.id==placement.defId);var use=GardenUse(placement.defId);
            if(use==null)return new Rect(area.x+(placement.x-definition.drawAnchor.x*definition.size01.x)*area.width,area.y+(placement.y-definition.drawAnchor.y*definition.size01.y)*area.height,definition.size01.x*area.width,definition.size01.y*area.height);
            float size=definition.size01.x*area.width*.8f;
            return new Rect(area.x+placement.x*area.width-use.ground.x*size,area.y+placement.y*area.height-use.ground.y*size,size,size);
        }
        private static Rect GardenActorImageRect(Rect furniture, GardenArtUse use)
        {
            float size=furniture.width*use.actorScale;
            return new Rect(furniture.x+use.contact.x*furniture.width-use.actorContact.x*size,furniture.y+use.contact.y*furniture.height-use.actorContact.y*size,size,size);
        }
        private void DrawGardenArtUse(Rect furniture,Texture2D texture,GardenArtUse use,bool occupied,string heroineId="heroine.slayer")
        {
            string prefix=heroineId=="heroine.iconoclast"?"iconoclast":"slayer";
            var actor=occupied && use!=null?SampleImage(prefix+"-sd-"+use.action):null;
            var furnitureLayers=actor==null?null:GardenImageLayers(texture,use.front,use.furnitureId);
            if(texture!=null)GUI.DrawTexture(furniture,(Texture)furnitureLayers?.back??texture,ScaleMode.ScaleToFit,true);
            if(actor==null)return;
            var actorLayers=GardenImageLayers(actor,use.hands,prefix+"-"+use.action);var actorRect=GardenActorImageRect(furniture,use);
            GUI.DrawTexture(actorRect,(Texture)actorLayers?.back??actor,ScaleMode.ScaleToFit,true);
            if(furnitureLayers!=null)GUI.DrawTexture(furniture,furnitureLayers.front,ScaleMode.ScaleToFit,true);
            if(actorLayers!=null)GUI.DrawTexture(actorRect,actorLayers.front,ScaleMode.ScaleToFit,true);
        }
    }
}
