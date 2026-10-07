using System;
using System.Linq;
using NewAster.Core;
using UnityEngine;
namespace NewAster.Presentation
{
    public sealed partial class PrototypeBootstrap
    {
        // Composition keeps the base landscape, extreme shape and deep modifier independent.
        private void DrawTerraformLandscape(Rect bounds,TerraformSave s,bool baseLayer)
        {
            var colors=new[]{new Color(.13f,.55f,.30f),new Color(.1f,.42f,.7f),new Color(.48f,.32f,.21f),new Color(.4f,.65f,.85f),new Color(.95f,.78f,.3f),new Color(.12f,.13f,.3f),new Color(.42f,.47f,.54f)};
            if(baseLayer){
                GrowthFill(bounds.x,bounds.y,bounds.width,bounds.height,new Color(.06f,.09f,.17f));
                for(int i=0;i<7;i++){
                    var d=s.domains.Single(x=>x.domainId==TerraformRules.DomainIds[i]);float width=bounds.width/7;
                    GrowthFill(bounds.x+i*width,bounds.y+bounds.height*(1-d.currentLevel/9f),width,bounds.height*d.currentLevel/9f,colors[i]);
                    if(d.activeExtremeId!=null && d.activeExtremeId.StartsWith(d.domainId+"_") && TerraformRules.DomainIds.Contains(d.activeExtremeId.Substring(d.domainId.Length+1)))for(int j=0;j<5;j++)GrowthFill(bounds.x+i*width+j*width/5,bounds.y+bounds.height*.3f+j*8,5,bounds.height*.4f,colors[Array.IndexOf(TerraformRules.DomainIds,d.activeExtremeId.Substring(d.domainId.Length+1))]);
                }
            }
            foreach(var d in s.domains.Where(x=>x.currentLevel>=6 && x.activeDeepRecordId!=null)){
                int deep=Array.IndexOf(TerraformRules.DeepIds,d.activeDeepRecordId);if(deep<0)continue;
                for(int j=0;j<6;j++)GrowthFill(bounds.x+(j+.3f)*bounds.width/6,bounds.y+(deep+1)*bounds.height/9,18,3,colors[deep]);
            }
            int phenomenon=Array.IndexOf(TerraformRules.Phenomena,s.activeWorldPhenomenonId);if(phenomenon<0)return;
            float t=Time.realtimeSinceStartup;
            for(int j=0;j<45;j++){
                float x=bounds.x+Mathf.Repeat(j*73.1f+t*(phenomenon==0?14:4),bounds.width),y=bounds.y+Mathf.Repeat(j*41.7f+t*(phenomenon==0?110:20),bounds.height);
                Color color=phenomenon==0?new Color(.5f,.7f,1,.65f):phenomenon==2?new Color(1,.65f,.8f,.8f):phenomenon==3?new Color(.8f,1,.3f,.8f):colors[phenomenon%7];
                GrowthFill(x,y,phenomenon>=4?18:3,phenomenon==0?15:3,color);
            }
        }
    }
}
