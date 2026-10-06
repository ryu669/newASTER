using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class CollectionOwnerDef
    {
        public string id,kind,previousOwnerId;
        public string[] poemIds,chapterIds,environmentIds,materialIds;
        public bool integration;
    }
    [Serializable] public sealed class CollectionPoemDef { public string id,ownerId,chapterId; }
    [Serializable] public sealed class CollectionChapterDef
    {
        public string id,ownerId;
        public string[] poemIds;
        // null means not authored. Fixture text must never become readable formal content.
        public string textId;
    }
    [Serializable] public sealed class CollectionLinkDef { public string id,ownerId,sourcePoemId,targetPoemId; }
    [Serializable] public sealed class CollectionWeaponNodeDef { public string id,ownerId; public string[] materialIds,prerequisiteIds; }
    [Serializable] public sealed class CollectionResourceDef { public string id,kind,ownerId,name; public int rarity,minDropLevel;
        public int DropAmount(int level)=>level<minDropLevel?0:rarity<=1?10+level:1+level/(rarity==2?10:rarity==3?20:30);
        public string RarityName=>rarity>=4?"SSR":rarity==3?"SR":rarity==2?"R":"N"; }
    [Serializable] public sealed class CollectionRelicDef
    {
        public string id,abilityId;
        public string[] materialIds;
        public int maxLevel=120;
        public int attackPercent=5,hpPercent;
    }
    [Serializable] public sealed class CollectionRewardBandDef
    {
        public string ownerId;
        public int minLevel,maxLevel,draws,terraforming;
        public bool allowEmpty;
        public string[] relicIds;
    }
    [Serializable] public sealed class CollectionCatalog
    {
        public const string TrialVersion="collection-trial-story-2026-10-04";
        public const string CandidateVersion="collection-production-story-2026-10-04";
        public const string ProductionVersion="collection-initial-five-rc1-2026-10-05";
        public static bool SupportedVersion(string version)=>version==FixtureVersion || version==TrialVersion || version==CandidateVersion || version==ProductionVersion;
        public const string FixtureVersion="collection-fixture-2026-10-03";
        public int schemaVersion=1;
        public string contentVersion=FixtureVersion,status="fixture";
        public CollectionOwnerDef[] owners;
        public CollectionPoemDef[] poems;
        public CollectionChapterDef[] chapters;
        public CollectionLinkDef[] links;
        public CollectionResourceDef[] resources;
        public CollectionRelicDef[] relics;
        public CollectionRewardBandDef[] rewardBands;
        public CollectionWeaponNodeDef[] weaponNodes=Array.Empty<CollectionWeaponNodeDef>();
        internal static bool ValidId(string id)=>!string.IsNullOrWhiteSpace(id) && !id.Contains("|") && !id.Contains(",");
        private static Dictionary<string,T> Index<T>(T[] values,Func<T,string> id)
        {
            if(values==null || values.Any(x=>x==null || !ValidId(id(x))) || values.Select(id).Distinct().Count()!=values.Length)
                throw new ArgumentException("Missing or duplicate collection ID.");
            return values.ToDictionary(id);
        }
        private static void IdSet(string[] ids)
        {
            if(ids==null || ids.Any(x=>!ValidId(x)) || ids.Distinct().Count()!=ids.Length)
                throw new ArgumentException("Invalid collection ID set.");
        }
        public void Validate()
        {
            if(schemaVersion!=1 || !(contentVersion==FixtureVersion && status=="fixture" || contentVersion==TrialVersion && status=="development-trial" || contentVersion==CandidateVersion && status=="production-candidate" || contentVersion==ProductionVersion && status=="release"))
                throw new ArgumentException("Unsupported collection content.");
            var os=Index(owners,x=>x.id);var ps=Index(poems,x=>x.id);var cs=Index(chapters,x=>x.id);
            var rs=Index(resources,x=>x.id);var relicIndex=Index(relics,x=>x.id);
            var nodes=Index(weaponNodes,x=>x.id);
            if(owners.Count(x=>x.kind=="colossus")!=15 || owners.Count(x=>x.kind=="heroine")<5)
                throw new ArgumentException("Expected fifteen colossi and the initial five heroines.");
            foreach(var o in owners){
                if(o.kind!="colossus" && o.kind!="heroine")throw new ArgumentException("Unknown collection owner.");
                IdSet(o.poemIds);IdSet(o.chapterIds);IdSet(o.environmentIds);IdSet(o.materialIds);
                int total=o.kind=="colossus"?24:18,perChapter=total/3;
                if(o.poemIds.Length!=total || o.chapterIds.Length!=3)throw new ArgumentException("Invalid poem or chapter count.");
                foreach(var id in o.poemIds)if(!ps.TryGetValue(id,out var p) || p.ownerId!=o.id || !o.chapterIds.Contains(p.chapterId))throw new ArgumentException("Poem owner or chapter mismatch.");
                foreach(var id in o.chapterIds){
                    if(!cs.TryGetValue(id,out var c) || c.ownerId!=o.id || (contentVersion==FixtureVersion?!string.IsNullOrEmpty(c.textId):(contentVersion==ProductionVersion || contentVersion==CandidateVersion)?c.textId!="text.production."+c.id+".intro":!string.IsNullOrEmpty(c.textId) && !c.textId.StartsWith("text.trial.plan8.",StringComparison.Ordinal)))throw new ArgumentException("Invalid or unapproved chapter text.");
                    IdSet(c.poemIds);
                    if(c.poemIds.Length!=perChapter || c.poemIds.Any(p=>!o.poemIds.Contains(p) || ps[p].chapterId!=id))throw new ArgumentException("Invalid chapter membership.");
                }
                foreach(var id in o.environmentIds)if(!rs.TryGetValue(id,out var r) || r.kind!="environment")throw new ArgumentException("Unknown environment.");
                foreach(var id in o.materialIds)if(!rs.TryGetValue(id,out var r) || r.kind!="material" || r.ownerId!=o.id)throw new ArgumentException("Material source mismatch.");
                if(o.kind=="heroine" && (!string.IsNullOrEmpty(o.previousOwnerId) || o.integration || o.environmentIds.Length!=0))throw new ArgumentException("Invalid heroine progression.");
                if(!string.IsNullOrEmpty(o.previousOwnerId) && (!os.TryGetValue(o.previousOwnerId,out var previous) || previous.kind!="colossus" || previous.integration))throw new ArgumentException("Unknown progression prerequisite.");
                var visited=new HashSet<string>();var at=o;
                while(at!=null){if(!visited.Add(at.id))throw new ArgumentException("Cyclic progression.");at=string.IsNullOrEmpty(at.previousOwnerId)?null:os[at.previousOwnerId];}
            }
            var colossi=owners.Where(x=>x.kind=="colossus").ToArray();
            if(colossi.Count(x=>x.integration)!=1 || colossi.Count(x=>!x.integration && string.IsNullOrEmpty(x.previousOwnerId))!=1 ||
               colossi.Where(x=>x.integration).Any(x=>!string.IsNullOrEmpty(x.previousOwnerId)) ||
               colossi.Count(x=>!x.integration && !colossi.Any(y=>y.previousOwnerId==x.id))!=1 ||
               colossi.Where(x=>!string.IsNullOrEmpty(x.previousOwnerId)).GroupBy(x=>x.previousOwnerId).Any(x=>x.Count()!=1))
                throw new ArgumentException("Invalid linear world unlock sequence.");
            foreach(var p in poems)if(!os.TryGetValue(p.ownerId,out var o) || !o.poemIds.Contains(p.id))throw new ArgumentException("Orphan poem.");
            foreach(var c in chapters)if(!os.TryGetValue(c.ownerId,out var o) || !o.chapterIds.Contains(c.id))throw new ArgumentException("Orphan chapter.");
            foreach(var r in resources)if((r.kind!="material" && r.kind!="environment") || !os.TryGetValue(r.ownerId,out var o) || !(r.kind=="material"?o.materialIds:o.environmentIds).Contains(r.id))throw new ArgumentException("Orphan resource.");
            foreach(var r in resources.Where(r=>r.kind=="material" && os[r.ownerId].kind=="colossus"))if(contentVersion==ProductionVersion && (r.rarity<1 || r.rarity>4 || r.minDropLevel<1 || r.minDropLevel>50 || string.IsNullOrWhiteSpace(r.name)))throw new ArgumentException("Invalid production material rarity or drop level.");
            foreach(var node in weaponNodes){
                IdSet(node.materialIds);IdSet(node.prerequisiteIds);
                if(!os.TryGetValue(node.ownerId,out var owner) || owner.kind!="heroine" || node.materialIds.Length==0 || node.materialIds.Any(id=>!rs.TryGetValue(id,out var resource) || resource.kind!="material") || node.prerequisiteIds.Any(id=>!nodes.TryGetValue(id,out var parent) || parent.ownerId!=node.ownerId))throw new ArgumentException("Invalid heroine weapon node.");
                ValidateWeaponCycle(node,nodes,new HashSet<string>());
            }
            Index(links,x=>x.id);
            if(links.Any(x=>!ValidId(x.ownerId)) || links.Select(x=>x.sourcePoemId+"|"+x.targetPoemId).Distinct().Count()!=links.Length)throw new ArgumentException("Invalid poem links.");
            foreach(var l in links)
                if(!ps.TryGetValue(l.sourcePoemId,out var source) || !ps.TryGetValue(l.targetPoemId,out var target) ||
                   os[source.ownerId].kind!="colossus" || os[target.ownerId].kind!="heroine" || l.ownerId!=target.ownerId)throw new ArgumentException("Invalid poem correspondence.");
            foreach(var r in relics){
                IdSet(r.materialIds);
                if(r.maxLevel!=120 || !(contentVersion!=ProductionVersion && r.abilityId=="ability.fixture.attack" || (contentVersion==ProductionVersion || contentVersion==CandidateVersion) && r.abilityId.StartsWith("ability.production.relic.")) || r.attackPercent<0 || r.attackPercent>20 || r.hpPercent<0 || r.hpPercent>20 || r.materialIds.Length==0 || r.materialIds.Any(id=>!rs.TryGetValue(id,out var m) || m.kind!="material"))throw new ArgumentException("Invalid relic definition.");
            }
            if(rewardBands==null || rewardBands.Any(x=>x==null))throw new ArgumentException("Missing reward bands.");
            foreach(var o in colossi){
                var bands=rewardBands.Where(x=>x.ownerId==o.id).OrderBy(x=>x.minLevel).ToArray();
                int level=1,lastDraws=0,lastTerraforming=0;
                foreach(var b in bands){
                    IdSet(b.relicIds);
                    if(b.minLevel!=level || b.maxLevel<b.minLevel || b.maxLevel>50 || b.draws<1 || b.draws<lastDraws ||
                       b.terraforming<1 || b.terraforming<lastTerraforming || b.relicIds.Length==0 || b.relicIds.Any(id=>!relicIndex.ContainsKey(id)))
                        throw new ArgumentException("Invalid reward band.");
                    level=b.maxLevel+1;lastDraws=b.draws;lastTerraforming=b.terraforming;
                }
                if(level!=51)throw new ArgumentException("Incomplete reward levels.");
            }
            if(rewardBands.Any(x=>!os.TryGetValue(x.ownerId,out var o) || o.kind!="colossus"))throw new ArgumentException("Unknown reward owner.");
        }
        public CollectionCatalog Copy()
        {
            // Keep the combat/session boundary independent of serializers and Unity.
            return new CollectionCatalog {
                schemaVersion=schemaVersion,contentVersion=contentVersion,status=status,
                owners=owners.Select(o=>new CollectionOwnerDef {id=o.id,kind=o.kind,previousOwnerId=o.previousOwnerId,integration=o.integration,poemIds=(string[])o.poemIds.Clone(),chapterIds=(string[])o.chapterIds.Clone(),environmentIds=(string[])o.environmentIds.Clone(),materialIds=(string[])o.materialIds.Clone()}).ToArray(),
                poems=poems.Select(p=>new CollectionPoemDef {id=p.id,ownerId=p.ownerId,chapterId=p.chapterId}).ToArray(),
                chapters=chapters.Select(c=>new CollectionChapterDef {id=c.id,ownerId=c.ownerId,poemIds=(string[])c.poemIds.Clone(),textId=c.textId}).ToArray(),
                links=links.Select(l=>new CollectionLinkDef {id=l.id,ownerId=l.ownerId,sourcePoemId=l.sourcePoemId,targetPoemId=l.targetPoemId}).ToArray(),
                resources=resources.Select(r=>new CollectionResourceDef {id=r.id,kind=r.kind,ownerId=r.ownerId,name=r.name,rarity=r.rarity,minDropLevel=r.minDropLevel}).ToArray(),
                relics=relics.Select(r=>new CollectionRelicDef {id=r.id,abilityId=r.abilityId,maxLevel=r.maxLevel,attackPercent=r.attackPercent,hpPercent=r.hpPercent,materialIds=(string[])r.materialIds.Clone()}).ToArray(),
                rewardBands=rewardBands.Select(b=>new CollectionRewardBandDef {ownerId=b.ownerId,minLevel=b.minLevel,maxLevel=b.maxLevel,draws=b.draws,terraforming=b.terraforming,allowEmpty=b.allowEmpty,relicIds=(string[])b.relicIds.Clone()}).ToArray()
                ,weaponNodes=weaponNodes.Select(n=>new CollectionWeaponNodeDef {id=n.id,ownerId=n.ownerId,materialIds=(string[])n.materialIds.Clone(),prerequisiteIds=(string[])n.prerequisiteIds.Clone()}).ToArray()
            };
        }
        private static void ValidateWeaponCycle(CollectionWeaponNodeDef node,Dictionary<string,CollectionWeaponNodeDef> nodes,HashSet<string> path)
        {
            if(!path.Add(node.id))throw new ArgumentException("Cyclic weapon nodes.");
            foreach(var id in node.prerequisiteIds)ValidateWeaponCycle(nodes[id],nodes,path);
            path.Remove(node.id);
        }
    }
    public enum BattleEndReason { Victory, Defeat, Retreat }
    [Serializable] public sealed class CollectionBattleRecord
    {
        public string battleId,colossusId,contentVersion;
        public string combatVersion="combat-v3-newaster-original",colossusVersion=ColossusCombatDef.Version;
        public int level,seed;
        public long revision;
        public string[] formationIds,heardPoemIds;
        public void Validate()
        {
            if(!CollectionCatalog.ValidId(battleId) || !CollectionCatalog.ValidId(colossusId) || !CollectionCatalog.SupportedVersion(contentVersion) || combatVersion!="combat-v3-newaster-original" || !ColossusCombatDef.SupportedVersion(colossusVersion) || level<1 || level>50 || revision<0 ||
               formationIds==null || formationIds.Length!=5 || formationIds.Any(x=>!CollectionCatalog.ValidId(x)) || formationIds.Distinct().Count()!=5 ||
               heardPoemIds==null || heardPoemIds.Any(x=>!CollectionCatalog.ValidId(x)) || heardPoemIds.Distinct().Count()!=heardPoemIds.Length)throw new ArgumentException("Invalid battle collection record.");
        }
        public CollectionBattleRecord Copy()=>new CollectionBattleRecord {battleId=battleId,colossusId=colossusId,contentVersion=contentVersion,combatVersion=combatVersion,colossusVersion=colossusVersion,level=level,seed=seed,revision=revision,formationIds=(string[])formationIds.Clone(),heardPoemIds=(string[])heardPoemIds.Clone()};
    }
    [Serializable] public sealed class CollectionReceipt
    {
        public CollectionBattleRecord battle;
        public BattleEndReason reason;
        public string[] acquiredPoemIds,unlockedChapterIds;
        public int relicDrawCount;
        public CollectionRelic[] relicDrops=Array.Empty<CollectionRelic>();
    }
    [Serializable] public sealed class FormalCollectionLedger
    {
        public int version=1;
        public string contentVersion=CollectionCatalog.FixtureVersion;
        public CollectionReceipt[] receipts=Array.Empty<CollectionReceipt>();
        public CollectionMaterial[] materials=Array.Empty<CollectionMaterial>();
        public CollectionRelic[] relics=Array.Empty<CollectionRelic>();
        public CollectionEquipment[] equipment=Array.Empty<CollectionEquipment>();
        public void ValidateContent(CollectionCatalog catalog)
        {
            Validate();catalog.Validate();if(contentVersion!=catalog.contentVersion)throw new ArgumentException("Collection ledger content version mismatch.");
            foreach(var receipt in receipts){
                var b=receipt.battle;
                if(!catalog.owners.Any(o=>o.id==b.colossusId && o.kind=="colossus") || b.formationIds.Any(id=>!catalog.owners.Any(o=>o.id==id && o.kind=="heroine")) ||
                   b.heardPoemIds.Any(id=>!catalog.poems.Any(p=>p.id==id && p.ownerId==b.colossusId)) ||
                   receipt.acquiredPoemIds.Any(id=>!catalog.poems.Any(p=>p.id==id && (p.ownerId==b.colossusId || b.formationIds.Contains(p.ownerId)))) ||
                   receipt.unlockedChapterIds.Any(id=>!catalog.chapters.Any(c=>c.id==id)) || receipt.relicDrops.Any(r=>!catalog.relics.Any(d=>d.id==r.id)))throw new ArgumentException("Unknown collection receipt content.");
            }
            if(materials.Any(m=>!catalog.resources.Any(r=>r.id==m.id && r.kind=="material" && r.ownerId==m.sourceColossusId)) || relics.Any(r=>!catalog.relics.Any(d=>d.id==r.id)) || equipment.Any(e=>!catalog.owners.Any(o=>o.id==e.heroineId && o.kind=="heroine")))throw new ArgumentException("Unknown inventory content.");
        }
        public void Validate()
        {
            if(version!=1 || !CollectionCatalog.SupportedVersion(contentVersion) || receipts==null || receipts.Any(x=>x==null || x.battle==null) || receipts.Select(x=>x.battle.battleId).Distinct().Count()!=receipts.Length)throw new ArgumentException("Invalid collection ledger.");
            if(materials==null || materials.Any(x=>x==null || !CollectionCatalog.ValidId(x.id) || !CollectionCatalog.ValidId(x.sourceColossusId) || x.amount<0) || materials.Select(x=>x.id).Distinct().Count()!=materials.Length ||
               relics==null || relics.Any(x=>x==null || !CollectionCatalog.ValidId(x.id) || !CollectionCatalog.SupportedVersion(x.contentVersion) || x.level<1 || x.level>120 || x.attackRoll<0 || x.attackRoll>100 || x.hpRoll<0 || x.hpRoll>1000) || relics.Select(x=>x.id).Distinct().Count()!=relics.Length ||
               equipment==null || equipment.Any(x=>x==null || !CollectionCatalog.ValidId(x.heroineId) || !relics.Any(r=>r.id==x.relicId)) || equipment.Select(x=>x.heroineId).Distinct().Count()!=equipment.Length || equipment.Select(x=>x.relicId).Distinct().Count()!=equipment.Length)throw new ArgumentException("Invalid material or relic inventory.");
            foreach(var r in receipts){
                r.battle.Validate();
                if(r.relicDrawCount<0 || r.relicDrawCount>5 || r.relicDrops==null || r.relicDrops.Length>r.relicDrawCount || r.reason!=BattleEndReason.Victory && (r.relicDrawCount!=0 || r.relicDrops.Length!=0) || r.relicDrops.Any(x=>x==null || !CollectionCatalog.SupportedVersion(x.contentVersion) || !CollectionCatalog.ValidId(x.id) || x.level!=1 || x.attackRoll<0 || x.attackRoll>100 || x.hpRoll<0 || x.hpRoll>1000) || !Enum.IsDefined(typeof(BattleEndReason),r.reason) || r.acquiredPoemIds==null || r.unlockedChapterIds==null ||
                   new[]{r.acquiredPoemIds,r.unlockedChapterIds}.Any(ids=>ids.Any(x=>!CollectionCatalog.ValidId(x)) || ids.Distinct().Count()!=ids.Length))throw new ArgumentException("Invalid collection receipt.");
            }
        }
    }
    /// <summary>Freeze IDs, level, party and content when starting combat. Hearing is independent of presentation.</summary>
    public sealed class BattleCollectionSession
    {
        private readonly CollectionCatalog catalog;
        private readonly CollectionBattleRecord battle;
        private readonly HashSet<string> heard=new HashSet<string>();
        private CollectionReceipt ended;
        public BattleCollectionSession(CollectionCatalog catalog,string battleId,string colossusId,int level,long revision,IEnumerable<string> formation,int seed=0,string colossusVersion=ColossusCombatDef.Version)
        {
            catalog.Validate();this.catalog=catalog.Copy();
            battle=new CollectionBattleRecord {battleId=battleId,colossusId=colossusId,contentVersion=catalog.contentVersion,colossusVersion=colossusVersion,level=level,seed=seed,revision=revision,formationIds=(formation??throw new ArgumentNullException(nameof(formation))).ToArray(),heardPoemIds=Array.Empty<string>()};
            battle.Validate();
            if(!this.catalog.owners.Any(x=>x.id==colossusId && x.kind=="colossus") || battle.formationIds.Any(id=>!this.catalog.owners.Any(x=>x.id==id && x.kind=="heroine")))throw new ArgumentException("Unknown battle owner.");
        }
        public CollectionBattleRecord Snapshot {get{var r=battle.Copy();r.heardPoemIds=heard.OrderBy(x=>x,StringComparer.Ordinal).ToArray();return r;}}
        public bool RecordCompletedSinging(string poemId)
        {
            if(ended!=null)throw new InvalidOperationException("Collection has ended.");
            if(!catalog.poems.Any(x=>x.id==poemId && x.ownerId==battle.colossusId))throw new ArgumentException("Singing owner mismatch.");
            return heard.Add(poemId);
        }
        public CollectionReceipt Finish(BattleEndReason reason,IEnumerable<string> ownedPoems,IEnumerable<string> unlockedChapters)
        {
            if(!Enum.IsDefined(typeof(BattleEndReason),reason))throw new ArgumentException("Unknown end reason.");
            if(ended!=null){if(ended.reason!=reason)throw new InvalidOperationException("Battle end reason changed.");return Copy(ended);}
            var owned=new HashSet<string>(ownedPoems??throw new ArgumentNullException(nameof(ownedPoems)));
            var unlocked=new HashSet<string>(unlockedChapters??throw new ArgumentNullException(nameof(unlockedChapters)));
            var candidates=new HashSet<string>(heard);
            // Resolve even if the enemy poem was previously owned, and include fallen starting members.
            foreach(var l in catalog.links.Where(x=>heard.Contains(x.sourcePoemId)))
                if(battle.formationIds.Contains(catalog.poems.Single(x=>x.id==l.targetPoemId).ownerId))candidates.Add(l.targetPoemId);
            var acquired=candidates.Where(x=>!owned.Contains(x)).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            owned.UnionWith(candidates);
            var chapters=catalog.chapters.Where(c=>!unlocked.Contains(c.id) && c.poemIds.All(owned.Contains)).Select(c=>c.id).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            ended=new CollectionReceipt {battle=Snapshot,reason=reason,acquiredPoemIds=acquired,unlockedChapterIds=chapters};
            return Copy(ended);
        }
        private static CollectionReceipt Copy(CollectionReceipt r)=>new CollectionReceipt {battle=r.battle.Copy(),reason=r.reason,acquiredPoemIds=(string[])r.acquiredPoemIds.Clone(),unlockedChapterIds=(string[])r.unlockedChapterIds.Clone()};
    }
}
