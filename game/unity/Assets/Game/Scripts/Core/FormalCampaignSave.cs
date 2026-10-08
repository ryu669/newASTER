using System;
using System.Linq;
using System.IO;

namespace NewAster.Core
{
    // Read the stable envelope header without interpreting future payload fields.
    [Serializable] public sealed class FormalCollectionHeader { public int version; public string contentVersion; }
    [Serializable] public sealed class FormalEngagementHeader {public int version;}
    [Serializable] public sealed class ProductionNarrativeArchive
    {
        public int version=1;
        public string homeVersion;
        public string[] readStoryIds=Array.Empty<string>(),unlockedEventIds=Array.Empty<string>(),readEventIds=Array.Empty<string>(),loverHeroineIds=Array.Empty<string>(),claimedRewardIds=Array.Empty<string>();
        public HomeReadLine[] readLineKeys=Array.Empty<HomeReadLine>();
        public void Validate()
        {
            if(version!=1 || homeVersion!="none" && homeVersion!=HomeExperienceCatalog.FixtureVersion)throw new ArgumentException("Unknown archived narrative version.");
            foreach(var ids in new[]{readStoryIds,unlockedEventIds,readEventIds,loverHeroineIds,claimedRewardIds})
                if(ids==null || ids.Any(id=>!CollectionCatalog.ValidId(id)) || ids.Distinct().Count()!=ids.Length)throw new ArgumentException("Invalid archived narrative flags.");
            if(readLineKeys==null || readLineKeys.Any(l=>l==null || !CollectionCatalog.ValidId(l.sceneId) || !CollectionCatalog.ValidId(l.lineId) || l.scriptVersion<1))throw new ArgumentException("Invalid archived read lines.");
        }
    }
    [Serializable] public sealed class FormalCampaignHeader
    {public int version;public string saveId;public FormalWorldHeader world;public FormalGrowthHeader growth;public FormalEngagementHeader engagement;public FormalCollectionHeader collection;public FormalHomeHeader home;}
    [Serializable] public sealed class FormalCampaignSave
    {
        public const string Identity="newaster.formal-campaign";
        public int version=1;
        public string saveId=Identity;
        public long revision;
        public CampaignSaveV2 world;
        public FormalGrowthSave growth;
        // Optional additive field: earlier formal envelopes retain all existing state.
        public FormalEngagementState engagement;
        // Additive contract; missing on earlier formal saves means no new collection receipts.
        public FormalCollectionLedger collection;
        // Missing or explicit null means no Plan 6 state; never infer from legacy arrays.
        public FormalHomeProgress home;
        // Additive Plan11-2 progress. Runtime positions/actions are intentionally absent.
        public GardenLifeProgress gardenLife;
        public AffectionProgress affection;
        // Keep prior diagnostic narrative flags without treating them as authored read completion.
        public ProductionNarrativeArchive previousNarrative;
        public void Validate()
        {
            if(version!=1 || saveId!=Identity || revision<0 || world==null || growth==null)throw new ArgumentException("Unsupported formal campaign.");
            growth.Validate();
            if(growth.saveId!="newaster.formal-growth")throw new ArgumentException("Growth identity mismatch.");
            ValidateWorld(world);
            engagement?.Validate();
            collection?.Validate();
            home?.Validate();
            gardenLife?.Validate();
            affection?.Validate();
            previousNarrative?.Validate();
            if(home!=null && home.receipts.Any(r=>growth.receipts.Any(g=>g.transactionId==r.transactionId) || world.claimedBattleIds.Contains(r.transactionId)))throw new ArgumentException("Home transaction identity conflicts with existing receipt.");
            if(collection!=null)foreach(var r in collection.receipts)
                if(!growth.receipts.Any(g=>g.transactionId==r.battle.battleId && g.signature==new FormalBattleEndRequest(r,0).Signature))throw new ArgumentException("Collection transaction receipt mismatch.");
            if(collection!=null)foreach(var receipt in collection.receipts)
                if(!world.claimedBattleIds.Contains(receipt.battle.battleId) || receipt.acquiredPoemIds.Any(id=>!world.poemIds.Contains(id)) || receipt.unlockedChapterIds.Any(id=>!world.unlockedStoryIds.Contains(id)))throw new ArgumentException("Collection receipt without durable world state.");
            foreach(var receipt in growth.receipts.Where(r=>r.signature.StartsWith("victory|",StringComparison.Ordinal)))
                if(!world.claimedBattleIds.Contains(receipt.transactionId))throw new ArgumentException("Victory receipt without world reward.");
        }
        public static void ValidateWorld(CampaignSaveV2 w)
        {
            if(w!=null)TerraformRules.Validate(w.terraform);
            if(w==null || w.version!=2 || w.materials<0 || w.terraformingExperience<0 || w.highestBattleLevel<1 || w.highestBattleLevel>50 || w.kinderStones<0 || w.kinderDrawCount<0 || w.kinderExchangeCount<0 || w.overflowEnhancementMaterials<0)throw new ArgumentException("Invalid world wallet.");
            CheckArray(w.heroineLevels,5,1,120);CheckArray(w.heroineAwakenings,5,0,2);CheckArray(w.weaponBranches,15,0,3);CheckArray(w.affections,5,0,100);CheckArray(w.furnitureSlots,3,-1,2);CheckArray(w.heroineDuplicates,5,0,int.MaxValue);CheckArray(w.heroineTraitRanks,5,0,5);
            if(w.craftedFurniture==null || w.craftedFurniture.Length!=3)throw new ArgumentException("Invalid furniture state.");
            foreach(var ids in new[]{w.claimedBattleIds,w.poemIds,w.unlockedStoryIds,w.readStoryIds,w.unlockedMilestoneIds,w.firstClearIds,w.appliedColossusIds,w.environmentTags,w.unlockedGardenIds})
                if(ids==null || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count()!=ids.Length)throw new ArgumentException("Invalid world ID set.");
        }
        private static void CheckArray(int[] values,int count,int min,int max){if(values==null || values.Length!=count || values.Any(v=>v<min || v>max))throw new ArgumentException("Invalid world array.");}
    }
    /// <summary>One durable envelope. Split formal files are import sources only, never dual-written.</summary>
    public sealed partial class FormalCampaignStore
    {
        private readonly string path;
        private readonly Func<FormalCampaignSave,string> encode;
        private readonly Func<string,FormalCampaignSave> decode;
        private readonly Func<string,FormalCampaignHeader> decodeHeader;
        public FormalCampaignStore(string path,Func<FormalCampaignSave,string> encode,Func<string,FormalCampaignSave> decode,Func<string,FormalCampaignHeader> decodeHeader)
        {this.path=Path.GetFullPath(path);this.encode=encode??throw new ArgumentNullException(nameof(encode));this.decode=decode??throw new ArgumentNullException(nameof(decode));this.decodeHeader=decodeHeader??throw new ArgumentNullException(nameof(decodeHeader));}
        private FormalCampaignSave Read(string file){var s=decode(File.ReadAllText(file));if(s==null)throw new ArgumentException("Empty campaign.");s.Validate();return s;}
        public FormalLoadResult Load(out FormalCampaignSave save)
        {
            save=null;
            if(!File.Exists(path))return File.Exists(path+".bak")?FormalLoadResult.Blocked:FormalLoadResult.Missing;
            try{
                string source=File.ReadAllText(path);var rootVersion=FormalCampaignJsonShape.RootInt32Member(source,"version");if(rootVersion.HasValue && rootVersion.Value!=1)return FormalLoadResult.Blocked;var envelope=decodeHeader(source);
                if(UnsupportedHeader(envelope))return FormalLoadResult.Blocked;
                var header=decode(source);
                // Never hide future/foreign content behind an older backup.
                if(Unsupported(header))return FormalLoadResult.Blocked;
                save=Read(path);return FormalLoadResult.Loaded;
            }catch(Exception e)when(ReadFailure(e)){
                try{save=Read(path+".bak");return FormalLoadResult.RecoveredBackup;}
                catch(Exception b)when(ReadFailure(b)){return FormalLoadResult.Blocked;}
            }
        }
        private static bool ReadFailure(Exception e)=>e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException || e is FormatException;
        public bool Save(FormalCampaignSave next)
        {
            next.Validate();using(AcquireWriteLock())return SaveLocked(next);
        }
        private FileStream AcquireWriteLock()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));return new FileStream(path+".write.lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        }
        private bool SaveLocked(FormalCampaignSave next)
        {
            next.Validate();string payload=encode(next);
            if(File.Exists(path)){
                var old=Read(path);
                if(payload==encode(old))return true;
                if(next.revision!=checked(old.revision+1))throw new ArgumentException("Stale campaign revision.");
            }else if(File.Exists(path+".bak") || next.revision!=0)throw new ArgumentException("Campaign initialization requires an empty slot.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            using(var writer=new StreamWriter(stream)){writer.Write(payload);writer.Flush();stream.Flush(true);}
            if(encode(Read(temp))!=payload)throw new ArgumentException("Campaign roundtrip mismatch.");
            try{if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}
            catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){
                // A replacement may be durable even if the completion notification failed.
                try{if(encode(Read(path))==payload)return true;}catch(Exception r)when(ReadFailure(r)){}throw;
            }
            return true;
        }
    }
}
