using System;
using System.Linq;
using System.Collections.Generic;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        [Serializable] private sealed class TreePiece
        {
            public string asset;
            public int route,tier;
            public float x,y,size,angle,opacity;
            public bool mirror;
        }
        [Serializable] private sealed class LayeredTree
        {
            public string heroineId,foliageAsset,flowerAsset;
            public Color foliageColor=Color.white,flowerColor=Color.white;
            public HeroineWeaponTreeAnchor[] nodes;
            public TreePiece[] canopy,flowers;
        }
        [Serializable] private sealed class TreeColors { public string heroineId; public Color foliageColor,flowerColor; }
        [Serializable] private sealed class TreeColorCatalog { public TreeColors[] entries; }
        private readonly Dictionary<string,LayeredTree> layeredTrees=new Dictionary<string,LayeredTree>();
        private static bool HasLayeredWeaponTree(string hero)=>!string.IsNullOrEmpty(hero) && hero.StartsWith("heroine.",StringComparison.Ordinal);
        private readonly Dictionary<string,Texture2D> layeredTreeTextures=new Dictionary<string,Texture2D>();
        private Texture2D LayeredTreeTexture(string asset)
        {
            if(!layeredTreeTextures.TryGetValue(asset,out var texture)){
                texture=Resources.Load<Texture2D>("UI/WeaponTrees/"+asset);
                if(texture==null)throw new InvalidOperationException("Missing layered tree asset: "+asset);
                layeredTreeTextures.Add(asset,texture);
            }
            return texture;
        }
        private void DrawTreePiece(Rect area,TreePiece piece,string asset,Color? tint=null)
        {
            var texture=LayeredTreeTexture(asset);
            float width=area.width*piece.size,height=width*texture.height/texture.width;
            var center=new Vector2(area.x+area.width*piece.x,area.y+area.height*piece.y);
            // Draw in the group's local coordinates. The book already applies its
            // aspect transform; changing GUI.matrix here can move art outside the page.
            var previous=GUI.color;
            try{
                var color=tint??Color.white;
                GUI.color=new Color(previous.r*color.r,previous.g*color.g,previous.b*color.b,previous.a*color.a*(piece.opacity>0?piece.opacity:1));
                GUI.DrawTextureWithTexCoords(new Rect(center.x-width/2,center.y-height/2,width,height),texture,new Rect(piece.mirror?1:0,0,piece.mirror?-1:1,1),true);
            }finally{GUI.color=previous;}
        }
        private Texture2D weaponRoundFrame;
        private void DrawRoundWeaponNode(float x,float y,HomeWeaponNode node,bool acquired,bool selected,bool equipped)
        {
            if(weaponRoundFrame==null){
                const int size=128;weaponRoundFrame=new Texture2D(size,size,TextureFormat.RGBA32,false);weaponRoundFrame.filterMode=FilterMode.Bilinear;
                var pixels=new Color[size*size];
                for(int py=0;py<size;py++)for(int px=0;px<size;px++){
                    float dx=px-63.5f,dy=py-63.5f,r=Mathf.Sqrt(dx*dx+dy*dy);
                    Color c=r<54?new Color(.025f,.065f,.085f,.94f):Color.clear;
                    if(r>49 && r<53 || r>56 && r<59)c=new Color(.77f,.61f,.30f,1);
                    // Small gold ornaments at the eight compass points.
                    for(int k=0;k<8;k++){float a=k*Mathf.PI/4;float ox=dx-Mathf.Cos(a)*55,oy=dy-Mathf.Sin(a)*55;if(Mathf.Abs(ox)+Mathf.Abs(oy)<5)c=new Color(.98f,.84f,.49f,1);}
                    pixels[py*size+px]=c;
                }
                weaponRoundFrame.SetPixels(pixels);weaponRoundFrame.Apply(false,true);
            }
            var prior=GUI.color;
            try{
                GUI.color=equipped?new Color(.5f,.9f,1):selected?new Color(1,1,.8f):acquired?Color.white:new Color(.65f,.68f,.7f);
                GUI.DrawTexture(new Rect(x-35,y-35,70,70),weaponRoundFrame,ScaleMode.ScaleToFit,true);
            }finally{GUI.color=prior;}
            DrawWeaponEffect(new Rect(x-19,y-19,38,38),WeaponGrowthRules.Icon(node),acquired);
        }
        private bool TryDrawLayeredWeaponTree(string hero,HomeWeaponNode[] nodes,HashSet<string> acquired,ref HeroineWeaponTreeDef layout,out Rect area)
        {
            area=default;
            if(!HasLayeredWeaponTree(hero))return false;
            if(!layeredTrees.TryGetValue(hero,out var art)){
                var source=Resources.Load<TextAsset>("UI/WeaponTrees/common-layout");
                if(source==null)throw new InvalidOperationException("Missing shared weapon tree layout.");
                art=JsonUtility.FromJson<LayeredTree>(source.text);
                art.heroineId=hero;
                foreach(var anchor in art.nodes)anchor.nodeId=hero+".weapon."+anchor.nodeId;
                var colors=JsonUtility.FromJson<TreeColorCatalog>(Resources.Load<TextAsset>("UI/WeaponTrees/heroine-colors").text);
                var tint=colors.entries.Single(c=>c.heroineId==hero);
                art.foliageColor=tint.foliageColor;art.flowerColor=tint.flowerColor;
                layeredTrees.Add(hero,art);
            }
            layout=new HeroineWeaponTreeDef{heroineId=hero,resourcePath=layout.resourcePath,nodes=art.nodes};
            area=ContainImage(new Rect(600,220,906,608),1536,1024);
            // Only acquired non-root node IDs grow leaves and flowers.
            var unlocked=new bool[3,5];
            foreach(var node in nodes)if(!node.initial && acquired.Contains(node.id))unlocked[WeaponGrowthRules.Route(node),WeaponGrowthRules.Tier(node)]=true;
            if(Event.current.type!=EventType.Repaint)return true;
            var paintArea=new Rect(0,0,area.width,area.height);
            GUI.BeginGroup(area);
            try{
                foreach(var piece in art.canopy)if(unlocked[piece.route,piece.tier])DrawTreePiece(paintArea,piece,art.foliageAsset??"silver-foliage",art.foliageColor);
                GUI.DrawTexture(new Rect(paintArea.width*.1f,paintArea.height*.1f,paintArea.width*.8f,paintArea.height*.8f),LayeredTreeTexture("common-tree"),ScaleMode.ScaleToFit,true);
                foreach(var piece in art.flowers)if(unlocked[piece.route,piece.tier])DrawTreePiece(paintArea,piece,art.flowerAsset??"crimson-flower",art.flowerColor);
            }finally{GUI.EndGroup();}
            return true;
        }
    }
}
