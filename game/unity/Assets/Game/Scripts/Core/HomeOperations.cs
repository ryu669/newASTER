using System;
using System.Linq;
using System.Globalization;
namespace NewAster.Core
{
    // Payload is immutable; the journal freezes the entire candidate on first save failure.
    public sealed class HomeOperation
    {
        public string Kind{get;} public string Target{get;} public string Owner{get;} public string Garden{get;} public string Zone{get;} public float X{get;} public float Y{get;}
        public string Key=>string.Join("/",new[]{Target,Owner??"-",Garden??"-",Zone??"-",X.ToString("R",CultureInfo.InvariantCulture),Y.ToString("R",CultureInfo.InvariantCulture)}.Select(Uri.EscapeDataString));
        public HomeOperation(string kind,string target,string owner=null,string garden=null,string zone=null,float x=0,float y=0)
        {if(!HomeExperienceCatalog.Id(target) || float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))throw new ArgumentException("Invalid operation payload.");Kind=kind;Target=target;Owner=owner;Garden=garden;Zone=zone;X=x;Y=y;}
    }
    public static class HomeRules
    {
        public static int Balance(FormalCampaignSave s,string id)=>s.collection?.materials.SingleOrDefault(m=>m.id==id)?.amount??0;
        public static void Spend(FormalCampaignSave s,HomeCost[] costs)
        {
            if(costs==null)throw new ArgumentException("Undefined cost.");
            var totals=costs.GroupBy(c=>c.resourceId).Select(g=>new HomeCost{resourceId=g.Key,amount=g.Aggregate(0,(sum,c)=>checked(sum+c.amount))}).ToArray();
            if(totals.Any(c=>c.amount<0 || Balance(s,c.resourceId)<c.amount))throw new ArgumentException("素材が不足しています。");
            foreach(var c in totals.Where(c=>c.amount>0))s.collection.materials.Single(m=>m.id==c.resourceId).amount-=c.amount;
        }
        public static void Grant(FormalCampaignSave s,HomeExperienceCatalog c,HomeCost[] rewards)
        {
            s.collection=s.collection??new FormalCollectionLedger{contentVersion=c.contentVersion==HomeExperienceCatalog.TrialVersion?CollectionCatalog.TrialVersion:c.contentVersion==HomeExperienceCatalog.ProductionVersion?CollectionCatalog.ProductionVersion:CollectionCatalog.FixtureVersion};
            foreach(var r in rewards){var source=c.materials.SingleOrDefault(m=>m.id==r.resourceId);if(source==null)throw new ArgumentException("Unsupported reward.");var m=s.collection.materials.SingleOrDefault(x=>x.id==r.resourceId);if(m==null){m=new CollectionMaterial{id=r.resourceId,sourceColossusId=source.colossusId};s.collection.materials=s.collection.materials.Concat(new[]{m}).ToArray();}m.amount=checked(m.amount+r.amount);}
        }
        public static void Owned(FormalCampaignSave s,string hero){if(!s.growth.heroines.Any(h=>h.heroineId==hero))throw new ArgumentException("Unknown heroine.");}
        public static void Apply(FormalCampaignSave s,HomeExperienceCatalog c,HomeOperation op)
        {
            var h=s.home;
            switch(op.Kind){
                case "panzer-loadout": {
                    Owned(s,op.Target);if(op.Target!="heroine.shell" || !c.heroineIds.Contains(op.Target))throw new ArgumentException("Unknown panzer.");var l=new PanzerLoadout(op.Owner,op.Garden,op.Zone);
                    h.panzerEquipment=(h.panzerEquipment??Array.Empty<HomePanzerEquipment>()).Where(e=>e.heroineId!=op.Target).Concat(new[]{new HomePanzerEquipment{heroineId=op.Target,resistance=l.Resistance,firstTool=l.FirstTool,secondTool=l.SecondTool}}).ToArray();break;}
                case "formation": {
                    Owned(s,op.Target);if(!c.heroineIds.Contains(op.Target) || !int.TryParse(op.Owner,out int slot) || slot<0 || slot>=5)throw new ArgumentException("編成枠を選択してください。");
                    var ids=h.formationIds==null || h.formationIds.Length==0?c.heroineIds.Take(5).ToArray():(string[])h.formationIds.Clone();
                    if(ids.Length!=5)throw new ArgumentException("編成には5人が必要です。");int previous=Array.FindIndex(ids,id=>c.PersonId(id)==c.PersonId(op.Target));if(previous>=0)ids[previous]=ids[slot];ids[slot]=op.Target;h.formationIds=ids;break;}
                case "weapon": {
                    var n=c.weaponNodes.Single(n0=>n0.id==op.Target);Owned(s,n.heroineId);
                    if(!(n.abilityId=="ability.home-fixture.attack" && n.skillId=="skill.home-fixture.preview" || c.contentVersion==HomeExperienceCatalog.ProductionVersion && n.abilityId=="ability.production.weapon-attack" && n.skillId=="skill.production.weapon-basic"))throw new ArgumentException("この武器効果は未対応です。");
                    if(!h.weaponNodeIds.Contains(n.id)){if(n.parentIds.Any(id=>!h.weaponNodeIds.Contains(id)))throw new ArgumentException("すべての親ノードが必要です。");Spend(s,n.costs);h.weaponNodeIds=h.weaponNodeIds.Concat(new[]{n.id}).ToArray();}break;}
                case "weapon-level": {
                    var n=c.weaponNodes.Single(n0=>n0.id==op.Target);Owned(s,n.heroineId);
                    if(c.contentVersion!=HomeExperienceCatalog.ProductionVersion || !h.weaponNodeIds.Contains(n.id) || !int.TryParse(op.Owner,out int next) || next!=h.WeaponLevel(n.id)+1 || next>7)throw new ArgumentException("取得済み神器を次のLvへ強化してください。");
                    Spend(s,WeaponGrowthRules.Costs(n,h.WeaponLevel(n.id),c));
                    h.weaponLevels=(h.weaponLevels??Array.Empty<HomeWeaponLevel>()).Where(w=>w.nodeId!=n.id).Concat(new[]{new HomeWeaponLevel{nodeId=n.id,level=next}}).ToArray();break;}
                case "equip": {
                    Owned(s,op.Owner);h.weaponEquipment=h.weaponEquipment.Where(e=>e.heroineId!=op.Owner).ToArray();
                    if(op.Target!="unequip"){var n=c.weaponNodes.Single(n0=>n0.id==op.Target);if(n.heroineId!=op.Owner || !h.weaponNodeIds.Contains(n.id))throw new ArgumentException("Unowned weapon.");h.weaponEquipment=h.weaponEquipment.Concat(new[]{new HomeWeaponEquipment{heroineId=op.Owner,nodeId=n.id}}).ToArray();}break;}
                case "craft": {
                    var f=c.furniture.Single(f0=>f0.id==op.Target);if(!HomeExperienceCatalog.Id(op.Owner) || h.furnitureInstances.Any(i=>i.instanceId==op.Owner))throw new ArgumentException("Duplicate furniture instance.");Spend(s,f.costs);h.furnitureInstances=h.furnitureInstances.Concat(new[]{new HomeFurnitureInstance{instanceId=op.Owner,defId=f.id}}).ToArray();break;}
                case "place": {
                    var i=h.furnitureInstances.Single(i0=>i0.instanceId==op.Target);var p=new HomePlacement{instanceId=i.instanceId,defId=i.defId,gardenId=op.Garden,zoneId=op.Zone,orientationId="orientation.default",x=op.X,y=op.Y};
                    HomeGeometry.Validate(s,c,p);h.furniturePlacements=h.furniturePlacements.Where(old=>old.instanceId!=p.instanceId).Concat(new[]{p}).ToArray();ClearUse(h,p.instanceId);break;}
                case "remove":h.furniturePlacements=h.furniturePlacements.Where(p=>p.instanceId!=op.Target).ToArray();ClearUse(h,op.Target);break;
                case "occupant": {
                    Owned(s,op.Target);if(!s.world.unlockedGardenIds.Contains(op.Garden) || !c.gardens.Any(g=>g.id==op.Garden && !g.unmade) || op.X<0 || op.X>1 || op.Y<0 || op.Y>1)throw new ArgumentException("Unavailable garden position.");
                    h.occupants=h.occupants.Where(o=>c.PersonId(o.heroineId)!=c.PersonId(op.Target)).Concat(new[]{new HomeOccupant{heroineId=op.Target,gardenId=op.Garden,slotId="slot.idle",x=op.X,y=op.Y}}).ToArray();break;}
                case "use": {
                    var o=h.occupants.Single(o0=>o0.heroineId==op.Target);var p=h.furniturePlacements.Single(p0=>p0.instanceId==op.Owner && p0.gardenId==o.gardenId);var f=c.furniture.Single(f0=>f0.id==p.defId);var slot=f.slots.FirstOrDefault(sl=>!h.occupants.Any(other=>other.heroineId!=o.heroineId && other.furnitureInstanceId==p.instanceId && other.slotId==sl.id));
                    // Placeholder SD has no action animation: show the reason, retain idle.
                    if(slot==null || c.assets.Single(a=>a.id==f.assetId).placeholder && !(f.supportedHeroineIds??Array.Empty<string>()).Contains(op.Target)){o.furnitureInstanceId=null;o.actionId=null;o.slotId="slot.idle";break;}
                    o.furnitureInstanceId=p.instanceId;o.slotId=slot.id;o.actionId=slot.actionIds[0];o.x=p.x+(slot.offset.x-f.drawAnchor.x)*f.size01.x;o.y=p.y+(slot.offset.y-f.drawAnchor.y)*f.size01.y;break;}
                case "talk": {
                    Owned(s,op.Target);var rule=c.interactions.SingleOrDefault(t=>t.heroineId==op.Target);if(rule==null || rule.affectionGain<=0 || rule.costs==null)throw new ArgumentException("交流費用・増分が未定です。");Spend(s,rule.costs);var a=h.affections.SingleOrDefault(a0=>a0.heroineId==op.Target);if(a==null){a=new HomeAffection{heroineId=op.Target};h.affections=h.affections.Concat(new[]{a}).ToArray();}a.value=checked(a.value+rule.affectionGain);var occupant=h.occupants.SingleOrDefault(o=>o.heroineId==op.Target);if(occupant!=null){occupant.furnitureInstanceId=null;occupant.actionId=null;occupant.slotId="slot.idle";}break;}
                default:throw new ArgumentException("Unsupported home operation.");
            }
            HomeConditions.Refresh(s,c);
        }
        private static void ClearUse(FormalHomeProgress h,string instance){foreach(var o in h.occupants.Where(o=>o.furnitureInstanceId==instance)){o.furnitureInstanceId=null;o.actionId=null;o.slotId="slot.idle";}}
    }
    public sealed partial class FormalCampaignJournal
    {
        public GrowthCommitResult CommitHomeOperation(FormalHomeRequest request,HomeExperienceCatalog catalog,HomeOperation operation,Func<FormalCampaignSave,bool> save)
        {
            if(operation==null || request.OperationKey!=operation.Key || request.Kind!=(operation.Kind=="equip"?"weapon":operation.Kind=="remove"?"place":operation.Kind=="use"?"occupant":operation.Kind))throw new ArgumentException("Operation signature mismatch.");
            return CommitHomeCandidate(request,catalog,s=>{HomeRules.Apply(s,catalog,operation);return s;},save);
        }
    }
    public static class HomeGeometry
    {
        private static decimal Q(float v){if(float.IsNaN(v) || float.IsInfinity(v))throw new ArgumentException("Nonfinite coordinate.");return Math.Round((decimal)v,6,MidpointRounding.AwayFromZero);}
        private static decimal[] Rect(HomePlacement p,HomeFurnitureLayout f)=>new[]{Q(p.x)-Q(f.drawAnchor.x)*Q(f.size01.x)+Q(f.footprint.x)*Q(f.size01.x),Q(p.y)-Q(f.drawAnchor.y)*Q(f.size01.y)+Q(f.footprint.y)*Q(f.size01.y),Q(f.footprint.width)*Q(f.size01.x),Q(f.footprint.height)*Q(f.size01.y)};
        public static void Validate(FormalCampaignSave s,HomeExperienceCatalog c,HomePlacement p)
        {
            if(!s.world.unlockedGardenIds.Contains(p.gardenId) || !s.home.furnitureInstances.Any(i=>i.instanceId==p.instanceId && i.defId==p.defId) || p.orientationId!="orientation.default")throw new ArgumentException("Unknown placement.");
            var g=c.gardens.Single(g0=>g0.id==p.gardenId);var z=g.zones.Single(z0=>z0.id==p.zoneId);var f=c.furniture.Single(f0=>f0.id==p.defId);var r=Rect(p,f);var b=z.bounds;
            if(p.x<0 || p.x>1 || p.y<0 || p.y>1 || r[0]<Q(b.x) || r[1]<Q(b.y) || r[0]+r[2]>Q(b.x)+Q(b.width) || r[1]+r[3]>Q(b.y)+Q(b.height))throw new ArgumentException("家具の接地範囲が区画外です。");
            foreach(var old in s.home.furniturePlacements.Where(o=>o.instanceId!=p.instanceId && o.gardenId==p.gardenId && o.zoneId==p.zoneId)){var t=Rect(old,c.furniture.Single(f0=>f0.id==old.defId));if(r[0]<t[0]+t[2] && t[0]<r[0]+r[2] && r[1]<t[1]+t[3] && t[1]<r[1]+r[3])throw new ArgumentException("家具の接地範囲が重なっています。");}
        }
    }
}
