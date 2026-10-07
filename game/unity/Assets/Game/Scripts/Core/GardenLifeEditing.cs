using System;
using System.Linq;
using System.Collections.Generic;
namespace NewAster.Core
{
    public sealed class GardenLifeEditor
    {
        private readonly HomePlacement[] original;
        private HomePlacement[] working;
        private readonly HomeExperienceCatalog catalog;
        private readonly Stack<HomePlacement[]> undo=new Stack<HomePlacement[]>(),redo=new Stack<HomePlacement[]>();
        public string GardenId{get;}
        public HomePlacement[] Placements=>Copy(working);
        public int UndoCount=>undo.Count;
        public int RedoCount=>redo.Count;
        public GardenLifeEditor(FormalHomeProgress home,string garden,HomeExperienceCatalog catalog=null){this.catalog=catalog;GardenId=garden;original=Copy(home.furniturePlacements);working=Copy(original);}
        public static HomePlacement Clone(HomePlacement p)=>new HomePlacement{instanceId=p.instanceId,defId=p.defId,gardenId=p.gardenId,zoneId=p.zoneId,orientationId=p.orientationId,x=p.x,y=p.y};
        private static HomePlacement[] Copy(HomePlacement[] a)=>a.Select(Clone).ToArray();
        private void Change(Action change){undo.Push(Copy(working));if(undo.Count>50){var keep=undo.Take(50).Reverse().ToArray();undo.Clear();foreach(var item in keep)undo.Push(item);}redo.Clear();change();}
        public void Place(HomePlacement p,float gridStep=0)
        {
            if(p==null || !GardenLifeCatalog.Orientations.Contains(p.orientationId) || p.gardenId!=GardenId || !GardenLifeProgress.Coordinate(p.x) || !GardenLifeProgress.Coordinate(p.y))throw new ArgumentException("Invalid edit position.");
            p=Clone(p);if(gridStep>0){p.x=(float)Math.Round(p.x/gridStep)*gridStep;p.y=(float)Math.Round(p.y/gridStep)*gridStep;}
            if(ReservedPlacement(p) && !original.Any(o=>o.instanceId==p.instanceId && o.gardenId==p.gardenId && o.x==p.x && o.y==p.y))throw new ArgumentException("入口・通路・景観ポイントには配置できません。");
            var value=p;Change(()=>working=working.Where(o=>o.instanceId!=value.instanceId).Concat(new[]{value}).ToArray());
        }
        public static bool Reserved(float x,float y)=>x<.1f && y>.78f || x>.47f && x<.53f && y>.47f && y<.89f || y<.43f || Enumerable.Range(0,4).Any(i=>Math.Abs(x-(.16f+i*.21f))<.018f && Math.Abs(y-(.58f+(i%2)*.15f))<.018f);
        private bool ReservedPlacement(HomePlacement p)
        {
            if(Reserved(p.x,p.y))return true;if(catalog==null)return false;
            var f=catalog.furniture.Single(x=>x.id==p.defId);var r=HomeGeometry.Footprint(p,f);
            Func<decimal,decimal,decimal,decimal,bool> intersects=(x,y,w,h)=>r[0]<x+w && x<r[0]+r[2] && r[1]<y+h && y<r[1]+r[3];
            return intersects(0,.78m,.1m,.22m) || intersects(.47m,.47m,.06m,.42m) || Enumerable.Range(0,4).Any(i=>intersects((decimal)(.16f+i*.21f)-.018m,(decimal)(.58f+(i%2)*.15f)-.018m,.036m,.036m));
        }
        public void Store(string instanceId){Change(()=>working=working.Where(p=>p.instanceId!=instanceId).ToArray());}
        public void StoreAll(){Change(()=>working=working.Where(p=>p.gardenId!=GardenId).ToArray());}
        public void Rotate(string instanceId,bool flip)
        {var p=Clone(working.Single(p=>p.instanceId==instanceId));int index=Array.IndexOf(GardenLifeCatalog.Orientations,p.orientationId);p.orientationId=GardenLifeCatalog.Orientations[index^(flip?1:2)];Place(p);}
        public bool Undo(){if(undo.Count==0)return false;redo.Push(Copy(working));working=undo.Pop();return true;}
        public bool Redo(){if(redo.Count==0)return false;undo.Push(Copy(working));working=redo.Pop();return true;}
        public void Cancel(){working=Copy(original);undo.Clear();redo.Clear();}
        public void Apply(FormalCampaignSave save,HomeExperienceCatalog catalog)
        {
            var h=save.home;if(h==null)throw new ArgumentException("Missing garden inventory.");h.furniturePlacements=Placements;
            foreach(var o in h.occupants.Where(o=>o.furnitureInstanceId!=null))if(!h.furniturePlacements.Any(p=>p.instanceId==o.furnitureInstanceId && p.gardenId==o.gardenId)){o.furnitureInstanceId=null;o.actionId=null;o.slotId="slot.idle";}
            h.ValidateContent(catalog,save);
        }
        public GardenLayoutPreset SavePreset(int index)=>new GardenLayoutPreset{gardenId=GardenId,index=index,items=working.Where(p=>p.gardenId==GardenId).Select(p=>new GardenLayoutItem{furnitureDefId=p.defId,x=p.x,y=p.y,orientationId=p.orientationId}).ToArray()};
        public Dictionary<string,int> Shortages(GardenLayoutPreset preset,FormalHomeProgress home)
        {
            var available=home.furnitureInstances.Where(i=>!working.Any(p=>p.instanceId==i.instanceId && p.gardenId!=GardenId)).ToArray();
            return preset.items.GroupBy(i=>i.furnitureDefId).Select(g=>new{key=g.Key,count=Math.Max(0,g.Count()-available.Count(i=>i.defId==g.Key))}).Where(g=>g.count>0).ToDictionary(g=>g.key,g=>g.count);
        }
        public void ApplyPreset(GardenLayoutPreset preset,FormalHomeProgress home,bool omitMissing=false)
        {
            if(preset.gardenId!=GardenId || preset.index<0 || preset.index>=5)throw new ArgumentException("Different garden layout.");if(!omitMissing && Shortages(preset,home).Count>0)throw new ArgumentException("レイアウトの家具が不足しています。作成せず中止します。");
            var pool=home.furnitureInstances.Where(i=>!working.Any(p=>p.instanceId==i.instanceId && p.gardenId!=GardenId)).ToList();var result=working.Where(p=>p.gardenId!=GardenId).Select(Clone).ToList();
            foreach(var item in preset.items){var instance=pool.FirstOrDefault(i=>i.defId==item.furnitureDefId);if(instance==null)continue;pool.Remove(instance);if(ReservedPlacement(new HomePlacement{defId=item.furnitureDefId,x=item.x,y=item.y,orientationId=item.orientationId}) && !original.Any(p=>p.instanceId==instance.instanceId && p.x==item.x && p.y==item.y))throw new ArgumentException("保存配置が予約領域と重なっています。");result.Add(new HomePlacement{instanceId=instance.instanceId,defId=instance.defId,gardenId=GardenId,zoneId="zone.ground",x=item.x,y=item.y,orientationId=item.orientationId});}
            Change(()=>working=result.ToArray());
        }
    }
    public static class GardenLifeCrafting
    {
        public static bool Unlocked(FormalCampaignSave save,string defId)
        {const string prefix="furniture.memorial.";return defId.StartsWith(prefix)?(save.gardenLife?.unlockedRecipeIds.Contains(defId.Substring(prefix.Length))??false):TerraformRules.FurnitureUnlocked(save.world,defId,save.home);}
        public static int Maximum(FormalCampaignSave save,HomeFurnitureLayout def)
        {if(!Unlocked(save,def.id))return 0;if(def.costs==null || def.costs.Length==0 || def.costs.Any(c=>c.amount<=0))throw new ArgumentException("Crafting requires formal positive costs.");return def.costs.GroupBy(c=>c.resourceId).Min(g=>HomeRules.Balance(save,g.Key)/g.Sum(c=>c.amount));}
        public static void Craft(FormalCampaignSave save,HomeExperienceCatalog home,string defId,int quantity,string transactionId)
        {
            var def=home.furniture.Single(f=>f.id==defId);if(quantity<1 || quantity>10000 || quantity>Maximum(save,def))throw new ArgumentException("作成数または素材が不足しています。");
            var costs=def.costs.Select(c=>new HomeCost{resourceId=c.resourceId,amount=checked(c.amount*quantity)}).ToArray();HomeRules.Spend(save,costs);
            save.home.furnitureInstances=save.home.furnitureInstances.Concat(Enumerable.Range(0,quantity).Select(i=>new HomeFurnitureInstance{instanceId="furniture.life."+transactionId+"."+i,defId=defId})).ToArray();
        }
    }
}
