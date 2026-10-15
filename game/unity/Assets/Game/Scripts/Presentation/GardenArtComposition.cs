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
        private readonly BoundedCache<string,GardenLayers> gardenLayers=new BoundedCache<string,GardenLayers>(24,released:ReleaseGardenLayers);
        private static void ReleaseGardenLayers(GardenLayers layers){if(layers.back!=null){layers.back.Release();Destroy(layers.back);}if(layers.front!=null){layers.front.Release();Destroy(layers.front);}}
        private GardenLayers GardenImageLayers(Texture2D source,Vector2[][] regions,string key)
        {
            if(source==null || regions.Length==0)return null;
            if(gardenLayers.TryGetValue(key,out var cached))return cached;
            if(Event.current.type!=EventType.Repaint)return null;
            var shader=Resources.Load<Shader>("Shaders/GardenRegionMask");if(shader==null || !shader.isSupported)throw new InvalidOperationException("Garden composition shader unavailable.");
            var mask=new Texture2D(source.width,source.height,TextureFormat.Alpha8,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var polygons=regions.Select(region=>region.SelectMany(point=>new[]{point.x,point.y}).ToArray()).ToArray();
            mask.LoadRawTextureData(PolygonAlphaMask.Rasterize(source.width,source.height,polygons));mask.Apply(false,true);
            var material=new Material(shader);material.SetTexture("_RegionMask",mask);var layers=new GardenLayers();var previous=RenderTexture.active;
            try {
                layers.back=new RenderTexture(source.width,source.height,0,RenderTextureFormat.ARGB32){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};layers.back.Create();
                layers.front=new RenderTexture(source.width,source.height,0,RenderTextureFormat.ARGB32){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};layers.front.Create();
                material.SetFloat("_Front",0);Graphics.Blit(source,layers.back,material);material.SetFloat("_Front",1);Graphics.Blit(source,layers.front,material);
            } finally {RenderTexture.active=previous;Destroy(material);Destroy(mask);}
            gardenLayers[key]=layers;return layers;
        }
        private void OnDestroy()
        {
            illustrationView?.Dispose();
            plan9FacePatches.Clear();if(plan9FaceMaterial!=null)Destroy(plan9FaceMaterial);
            gardenLayers.Clear();
        }
        private static GardenArtUse GardenUse(string furnitureId,string heroineId="heroine.slayer")
        {
            var original=gardenArtUses.SingleOrDefault(a=>a.furnitureId==furnitureId);
            if(original==null && furnitureId.StartsWith("furniture.production.",StringComparison.Ordinal))return new GardenArtUse{furnitureId=furnitureId,action="look",ground=new Vector2(.5f,.95f),contact=new Vector2(1.1f,.95f),actorContact=new Vector2(.5f,.98f),actorScale=.9f};
            if(original==null || heroineId=="heroine.slayer")return original;
            if(heroineId=="heroine.excalipan")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=original.action=="work"?new Vector2(.5f,.43f):original.contact,
                actorContact=original.action=="sit"?new Vector2(.52f,.60f):original.action=="work"?new Vector2(.41f,.49f):new Vector2(.5f,.97f),
                actorScale=original.action=="work"?1f:original.actorScale,
                hands=original.action=="work"?new[]{GardenPolygon(.29f,.46f,.52f,.46f,.53f,.52f,.29f,.52f)}:Array.Empty<Vector2[]>()};
            if(heroineId=="heroine.echidna")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=original.action=="work"?new Vector2(.5f,.43f):original.contact,
                actorContact=original.action=="sit"?new Vector2(.55f,.59f):original.action=="work"?new Vector2(.64f,.51f):new Vector2(.5f,.97f),
                actorScale=original.action=="work"?1f:original.actorScale,
                hands=original.action=="work"?new[]{GardenPolygon(.53f,.47f,.76f,.47f,.77f,.53f,.53f,.53f)}:Array.Empty<Vector2[]>()};
            if(heroineId=="heroine.undermine")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=original.action=="work"?new Vector2(.5f,.43f):original.contact,
                actorContact=original.action=="sit"?new Vector2(.50f,.64f):original.action=="work"?new Vector2(.50f,.49f):new Vector2(.5f,.97f),
                actorScale=original.action=="work"?1f:original.actorScale,
                hands=original.action=="work"?new[]{GardenPolygon(.38f,.44f,.60f,.44f,.60f,.52f,.38f,.52f)}:Array.Empty<Vector2[]>()};
            // ESTREYA_FURNITURE_BEGIN
            if(heroineId=="heroine.estreya" && original.action=="work")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=new Vector2(.5f,.43f),actorContact=new Vector2(.60f,.53f),actorScale=1.2f,
                hands=new[]{GardenPolygon(.47f,.46f,.59f,.46f,.68f,.49f,.74f,.49f,.76f,.58f,.71f,.61f,.60f,.60f,.50f,.56f),GardenPolygon(.65f,.45f,.70f,.44f,.72f,.49f,.68f,.50f)}};
            // ESTREYA_FURNITURE_END
            // APRIL_FURNITURE_BEGIN
            if(heroineId=="heroine.april" && original.action=="work")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=new Vector2(.5f,.43f),actorContact=new Vector2(.48f,.533f),actorScale=1.5f,
                hands=new[]{GardenPolygon(.35f,.50f,.43f,.50f,.43f,.60f,.35f,.60f),GardenPolygon(.53f,.53f,.61f,.53f,.61f,.61f,.53f,.61f)}};
            // APRIL_FURNITURE_END
            // WEAPON_MASTER_FURNITURE_BEGIN
            if(heroineId=="heroine.weapon-master" && original.action=="sit")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=original.contact,actorContact=new Vector2(.5f,.57f),actorScale=original.actorScale};
            if(heroineId=="heroine.weapon-master" && original.action=="work")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=new Vector2(.5f,.43f),actorContact=new Vector2(.408f,.447f),actorScale=1.5f,
                hands=new[]{GardenPolygon(.330f,.402f,.375f,.402f,.387f,.422f,.381f,.440f,.335f,.436f),GardenPolygon(.432f,.402f,.478f,.402f,.484f,.426f,.475f,.442f,.434f,.438f)}};
            // WEAPON_MASTER_FURNITURE_END
            if(heroineId=="heroine.vindicator" && original.action=="work")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=new Vector2(.5f,.43f),actorContact=new Vector2(.5f,.485f),actorScale=1.5f,
                hands=new[]{GardenPolygon(.402f,.462f,.443f,.462f,.455f,.481f,.452f,.503f,.414f,.506f,.397f,.485f),GardenPolygon(.551f,.462f,.589f,.462f,.606f,.483f,.593f,.506f,.553f,.503f,.545f,.482f)}};
            if(heroineId=="heroine.viper" && original.action=="work")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=new Vector2(.5f,.43f),actorContact=new Vector2(.5f,.53f),actorScale=1.5f,
                hands=new[]{GardenPolygon(.407f,.524f,.433f,.507f,.451f,.507f,.463f,.524f,.463f,.543f,.453f,.55f,.441f,.544f,.426f,.548f,.409f,.539f),GardenPolygon(.539f,.524f,.551f,.507f,.568f,.507f,.594f,.524f,.592f,.539f,.575f,.548f,.56f,.544f,.548f,.55f,.539f,.543f)}};
            if(heroineId=="heroine.invisible" && original.action=="work")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=new Vector2(.5f,.43f),actorContact=new Vector2(.5f,.5f),actorScale=1.5f,
                hands=new[]{GardenPolygon(.442f,.443f,.467f,.447f,.476f,.48f,.485f,.505f,.478f,.522f,.469f,.518f,.463f,.544f,.449f,.536f,.437f,.523f,.425f,.499f,.423f,.48f,.436f,.467f),GardenPolygon(.533f,.443f,.558f,.443f,.564f,.467f,.577f,.48f,.575f,.499f,.563f,.523f,.551f,.536f,.537f,.544f,.531f,.518f,.522f,.522f,.515f,.505f,.524f,.48f)}};
            if(new[]{"heroine.iron-maiden","heroine.avalanche","heroine.anathema","heroine.shiva","heroine.invisible","heroine.viper","heroine.vindicator","heroine.weapon-master","heroine.april","heroine.estreya"}.Contains(heroineId)){
                float handY=heroineId=="heroine.iron-maiden"?.46f:heroineId=="heroine.avalanche"?.47f:heroineId=="heroine.anathema"?.50f:.49f;
                return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                    contact=original.action=="work"?new Vector2(.5f,.43f):original.contact,
                    actorContact=original.action=="sit"?new Vector2(.5f,.64f):original.action=="work"?new Vector2(.52f,handY+.05f):new Vector2(.5f,.98f),
                    actorScale=original.action=="work"?1f:original.actorScale,
                    hands=original.action=="work"?new[]{GardenPolygon(.39f,handY-.025f,.47f,handY-.025f,.54f,handY+.025f,.40f,handY+.025f),GardenPolygon(.59f,handY-.025f,.67f,handY-.025f,.69f,handY+.025f,.59f,handY+.025f)}:Array.Empty<Vector2[]>()};
            }
            if(heroineId=="heroine.aeon")return new GardenArtUse{furnitureId=original.furnitureId,action=original.action,ground=original.ground,front=original.front,
                contact=original.action=="work"?new Vector2(.5f,.43f):original.contact,
                actorContact=original.action=="sit"?new Vector2(.51f,.65f):original.action=="work"?new Vector2(.65f,.68f):new Vector2(.5f,.98f),
                actorScale=original.action=="work"?1f:original.actorScale,
                hands=original.action=="work"?new[]{GardenPolygon(.54f,.57f,.60f,.56f,.65f,.58f,.68f,.61f,.63f,.61f,.58f,.60f,.54f,.60f),GardenPolygon(.71f,.55f,.76f,.55f,.80f,.57f,.83f,.60f,.78f,.59f,.73f,.58f)}:Array.Empty<Vector2[]>()};
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
            return use!=null && occupant.actionId=="action."+use.action && SampleImage(occupant.heroineId.Substring("heroine.".Length)+"-sd-"+use.action,optional:true)!=null?placement:null;
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
            string prefix=heroineId.Substring("heroine.".Length);
            var actor=occupied && use!=null?SampleImage(prefix+"-sd-"+use.action,optional:true):null;
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
