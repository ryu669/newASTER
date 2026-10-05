using System;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class FormalHomeHeader { public int version; public string contentVersion; }
    [Serializable] public sealed class HomeFurnitureInstance { public string instanceId,defId; }
    [Serializable] public sealed class HomePlacement
    { public string instanceId,defId,gardenId,zoneId,orientationId; public float x,y; }
    [Serializable] public sealed class HomeOccupant
    { public string heroineId,gardenId,slotId,furnitureInstanceId,actionId; public float x=.5f,y=.5f; }
    [Serializable] public sealed class HomeAffection { public string heroineId; public int value; }
    [Serializable] public sealed class HomeWeaponEquipment { public string heroineId,nodeId; }
    [Serializable] public sealed class HomeWeaponLevel { public string nodeId; public int level=1; }
    [Serializable] public sealed class HomeReadLine { public string sceneId,lineId; public int scriptVersion; }
    [Serializable] public sealed class HomeReceipt { public string transactionId,kind,signature,resultHash; }
    [Serializable] public sealed class FormalHomeProgress
    {
        public int version; public string contentVersion;
        public string[] formationIds=Array.Empty<string>();
        public HomeFurnitureInstance[] furnitureInstances=Array.Empty<HomeFurnitureInstance>();
        public HomePlacement[] furniturePlacements=Array.Empty<HomePlacement>();
        public HomeOccupant[] occupants=Array.Empty<HomeOccupant>();
        public string[] weaponNodeIds=Array.Empty<string>(),loverHeroineIds=Array.Empty<string>(),unlockedEventIds=Array.Empty<string>(),readEventIds=Array.Empty<string>(),claimedRewardIds=Array.Empty<string>();
        public HomeAffection[] affections=Array.Empty<HomeAffection>(); public HomeReadLine[] readLineKeys=Array.Empty<HomeReadLine>();
        public HomeReceipt[] receipts=Array.Empty<HomeReceipt>();
        public HomeWeaponEquipment[] weaponEquipment=Array.Empty<HomeWeaponEquipment>();
        public HomeWeaponLevel[] weaponLevels=Array.Empty<HomeWeaponLevel>();
        public int WeaponLevel(string nodeId)=>weaponLevels?.SingleOrDefault(w=>w.nodeId==nodeId)?.level??1;
        public static FormalHomeProgress Empty(string contentVersion)=>new FormalHomeProgress{version=1,contentVersion=contentVersion};
        private static void Set(string[] ids){if(ids==null || ids.Any(id=>!HomeExperienceCatalog.Id(id)) || ids.Distinct().Count()!=ids.Length)throw new ArgumentException("Invalid home ID set.");}
        private static void Index<T>(T[] entries,Func<T,string> key) where T:class
        {if(entries==null || entries.Any(x=>x==null || !HomeExperienceCatalog.Id(key(x))) || entries.Select(key).Distinct().Count()!=entries.Length)throw new ArgumentException("Missing or duplicate home entity.");}
        public void Validate()
        {
            if(version!=1 || !HomeExperienceCatalog.SupportedVersion(contentVersion))throw new ArgumentException("Unsupported home progress.");
            if(formationIds!=null && formationIds.Length>0){Set(formationIds);if(formationIds.Length!=5)throw new ArgumentException("編成は異なる5人です。");}
            Index(furnitureInstances,x=>x.instanceId);Index(furniturePlacements,x=>x.instanceId);Index(occupants,x=>x.heroineId);Index(affections,x=>x.heroineId);Index(receipts,x=>x.transactionId);
            Index(weaponEquipment,x=>x.heroineId);if(weaponEquipment.Any(e=>!weaponNodeIds.Contains(e.nodeId)))throw new ArgumentException("Weapon must be acquired.");
            if(weaponLevels!=null){Index(weaponLevels,x=>x.nodeId);if(weaponLevels.Any(w=>w.level<1 || w.level>7 || !weaponNodeIds.Contains(w.nodeId)))throw new ArgumentException("神器Lvは取得済みノードの1〜7です。");}
            foreach(var ids in new[]{weaponNodeIds,loverHeroineIds,unlockedEventIds,readEventIds,claimedRewardIds})Set(ids);
            if(readEventIds.Any(id=>!unlockedEventIds.Contains(id)) || affections.Any(a=>a.value<0) || furnitureInstances.Any(x=>!HomeExperienceCatalog.Id(x.defId)))throw new ArgumentException("Invalid home progression.");
            foreach(var p in furniturePlacements){
                if(!HomeExperienceCatalog.Id(p.defId) || !HomeExperienceCatalog.Id(p.gardenId) || !HomeExperienceCatalog.Id(p.zoneId) || p.orientationId!="orientation.default" || float.IsNaN(p.x) || float.IsNaN(p.y) || p.x<0 || p.x>1 || p.y<0 || p.y>1 || !furnitureInstances.Any(i=>i.instanceId==p.instanceId && i.defId==p.defId))throw new ArgumentException("Invalid furniture placement.");
            }
            foreach(var o in occupants){
                if(!HomeExperienceCatalog.Id(o.gardenId) || !HomeExperienceCatalog.Id(o.slotId) || float.IsNaN(o.x) || float.IsNaN(o.y) || float.IsInfinity(o.x) || float.IsInfinity(o.y) || o.x<0 || o.x>1 || o.y<0 || o.y>1)throw new ArgumentException("Invalid garden occupant.");
                bool usesFurniture=!string.IsNullOrEmpty(o.furnitureInstanceId);
                if(usesFurniture!=!string.IsNullOrEmpty(o.actionId) || usesFurniture && !furniturePlacements.Any(p=>p.instanceId==o.furnitureInstanceId && p.gardenId==o.gardenId))throw new ArgumentException("Invalid occupant furniture reference.");
            }
            if(occupants.Where(o=>!string.IsNullOrEmpty(o.furnitureInstanceId)).GroupBy(o=>o.furnitureInstanceId+"|"+o.slotId).Any(g=>g.Count()>1))throw new ArgumentException("Furniture interaction slot already occupied.");
            if(readLineKeys==null || readLineKeys.Any(x=>x==null || !HomeExperienceCatalog.Id(x.sceneId) || !HomeExperienceCatalog.Id(x.lineId) || x.scriptVersion<1) || readLineKeys.Select(x=>x.sceneId+"|"+x.scriptVersion+"|"+x.lineId).Distinct().Count()!=readLineKeys.Length)throw new ArgumentException("Invalid read-line identity.");
            if(receipts.Any(r=>!HomeExperienceCatalog.Id(r.kind) || string.IsNullOrWhiteSpace(r.signature) || string.IsNullOrWhiteSpace(r.resultHash)))throw new ArgumentException("Invalid home transaction receipt.");
        }
        public void ValidateContent(HomeExperienceCatalog catalog,FormalCampaignSave campaign)
        {
            Validate();catalog.Validate();if(catalog.contentVersion!=contentVersion)throw new ArgumentException("Home content version mismatch.");
            var heroes=campaign.growth.heroines.Select(h=>h.heroineId).ToArray();
            if(formationIds!=null && formationIds.Any(id=>!heroes.Contains(id) || !catalog.heroineIds.Contains(id)))throw new ArgumentException("未所持の誓女は編成できません。");
            foreach(var e in weaponEquipment)if(!heroes.Contains(e.heroineId) || !catalog.weaponNodes.Any(n=>n.id==e.nodeId && n.heroineId==e.heroineId))throw new ArgumentException("Invalid weapon owner.");
            foreach(var instance in furnitureInstances)if(!catalog.furniture.Any(f=>f.id==instance.defId))throw new ArgumentException("Unknown furniture definition.");
            foreach(var p in furniturePlacements)if(!campaign.world.unlockedGardenIds.Contains(p.gardenId) || !catalog.gardens.Any(g=>g.id==p.gardenId && g.zones.Any(z=>z.id==p.zoneId)))throw new ArgumentException("Unknown or locked placement garden.");
            foreach(var p in furniturePlacements)HomeGeometry.Validate(campaign,catalog,p);
            foreach(var o in occupants){if(!heroes.Contains(o.heroineId) || !catalog.heroineIds.Contains(o.heroineId) || !campaign.world.unlockedGardenIds.Contains(o.gardenId) || !catalog.gardens.Any(g=>g.id==o.gardenId && !g.unmade))throw new ArgumentException("Unknown or locked occupant.");if(!string.IsNullOrEmpty(o.furnitureInstanceId)){var instance=furnitureInstances.Single(i=>i.instanceId==o.furnitureInstanceId);if(!catalog.furniture.Single(f=>f.id==instance.defId).slots.Any(s=>s.id==o.slotId && s.actionIds.Contains(o.actionId)))throw new ArgumentException("Unknown furniture action.");}}
            foreach(var id in weaponNodeIds){var node=catalog.weaponNodes.SingleOrDefault(n=>n.id==id);if(node==null || !heroes.Contains(node.heroineId) || node.parentIds.Any(parent=>!weaponNodeIds.Contains(parent)))throw new ArgumentException("Unknown or orphan acquired weapon node.");}
            if(affections.Any(a=>!heroes.Contains(a.heroineId) || !catalog.heroineIds.Contains(a.heroineId)))throw new ArgumentException("Unknown affection owner.");
            foreach(var id in unlockedEventIds)if(!catalog.events.Any(e=>e.id==id && heroes.Contains(e.heroineId)))throw new ArgumentException("Unknown event.");
            foreach(var id in claimedRewardIds)if(!catalog.events.Any(e=>e.id==id && e.rewards.Length>0 && readEventIds.Contains(id)) && !catalog.chapters.Any(c=>c.id==id && c.rewards.Length>0 && campaign.world.readStoryIds.Contains(id)))throw new ArgumentException("Unknown or incomplete first reward source.");
            foreach(var id in loverHeroineIds)if(!heroes.Contains(id) || !catalog.events.Any(e=>e.heroineId==id && e.establishesLover && readEventIds.Contains(e.id)))throw new ArgumentException("Lover status requires explicit completed event.");
            foreach(var line in readLineKeys){var script=catalog.scripts.SingleOrDefault(s=>s.id==line.sceneId);if(script==null || line.scriptVersion>script.scriptVersion || line.scriptVersion==script.scriptVersion && !script.commands.Any(c=>c.kind=="line" && c.lineId==line.lineId))throw new ArgumentException("Unknown read line or script version.");}
        }
    }
}
