using System.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
namespace NewAster.Core
{
    public enum FormalRecoveryStatus { Healthy, Missing, Ready, NoValidBackup, Unsupported, Unavailable }
    public sealed class FormalRecoveryOffer
    {
        public FormalRecoveryStatus Status {get;}
        public bool CanRestore=>Status==FormalRecoveryStatus.Ready;
        internal readonly string fingerprint,candidate,sourcePath;
        internal FormalRecoveryOffer(FormalRecoveryStatus status,string sourcePath,string fingerprint=null,string candidate=null)
        {Status=status;this.sourcePath=sourcePath;this.fingerprint=fingerprint;this.candidate=candidate;}
    }
    public sealed partial class FormalCampaignStore
    {
        public string SavePath=>path;
        private static string Digest(byte[] bytes){using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(bytes));}
        private string DecodeBytes(byte[] bytes){using(var stream=new MemoryStream(bytes))using(var reader=new StreamReader(stream,System.Text.Encoding.UTF8,true))return reader.ReadToEnd();}
        private static bool UnsupportedHeader(FormalCampaignHeader h)=>h!=null && (h.version!=1 || h.saveId!=FormalCampaignSave.Identity || h.world!=null && h.world.version!=2 || h.growth!=null && (h.growth.version!=2 || h.growth.contentVersion!=FormalGrowthSave.ContentVersion || h.growth.saveId!="newaster.formal-growth") || h.engagement!=null && h.engagement.version!=1 || h.home!=null && (h.home.version!=1 || !HomeExperienceCatalog.SupportedVersion(h.home.contentVersion)) || h.collection!=null && (h.collection.version!=1 || !CollectionCatalog.SupportedVersion(h.collection.contentVersion)));
        private bool Unsupported(FormalCampaignSave s)=>s!=null && (s.version!=1 || s.saveId!=FormalCampaignSave.Identity || s.home!=null && (s.home.version!=1 || !HomeExperienceCatalog.SupportedVersion(s.home.contentVersion)) || s.growth!=null && (s.growth.version!=2 || s.growth.contentVersion!=FormalGrowthSave.ContentVersion || s.growth.saveId!="newaster.formal-growth") || s.world!=null && s.world.version!=2 || s.engagement!=null && s.engagement.version!=1 || s.collection!=null && (s.collection.version!=1 || !CollectionCatalog.SupportedVersion(s.collection.contentVersion) || s.collection.relics!=null && s.collection.relics.Any(r=>r!=null && !CollectionCatalog.SupportedVersion(r.contentVersion)) || s.collection.receipts!=null && s.collection.receipts.Any(r=>r!=null && r.battle!=null && (!CollectionCatalog.SupportedVersion(r.battle.contentVersion) || r.battle.combatVersion!="combat-v3-newaster-original" || !ColossusCombatDef.SupportedVersion(r.battle.colossusVersion)))));
        public FormalRecoveryOffer InspectRecovery()
        {
            try{
                bool exists=File.Exists(path);byte[] primary=exists?File.ReadAllBytes(path):null;
                if(exists){
                    try{string source=DecodeBytes(primary);var rootVersion=FormalCampaignJsonShape.RootInt32Member(source,"version");if(rootVersion.HasValue && rootVersion.Value!=1)return new FormalRecoveryOffer(FormalRecoveryStatus.Unsupported,path);var envelope=decodeHeader(source);if(UnsupportedHeader(envelope))return new FormalRecoveryOffer(FormalRecoveryStatus.Unsupported,path);var header=decode(source);if(Unsupported(header))return new FormalRecoveryOffer(FormalRecoveryStatus.Unsupported,path);header?.Validate();if(header!=null)return new FormalRecoveryOffer(FormalRecoveryStatus.Healthy,path);}
                    catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is FormatException){}
                }
                if(!File.Exists(path+".bak"))return new FormalRecoveryOffer(exists?FormalRecoveryStatus.NoValidBackup:FormalRecoveryStatus.Missing,path);
                byte[] backup=File.ReadAllBytes(path+".bak");
                FormalCampaignSave state;
                try{state=decode(DecodeBytes(backup));if(state==null || Unsupported(state))return new FormalRecoveryOffer(FormalRecoveryStatus.NoValidBackup,path);state.Validate();}
                catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is FormatException){return new FormalRecoveryOffer(FormalRecoveryStatus.NoValidBackup,path);}
                string fingerprint=(exists?Digest(primary):"missing")+"|"+Digest(backup);
                return new FormalRecoveryOffer(FormalRecoveryStatus.Ready,path,fingerprint,encode(state));
            }catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){return new FormalRecoveryOffer(FormalRecoveryStatus.Unavailable,path);}
        }
        public FormalCampaignSave RecoveryPreview(FormalRecoveryOffer offer)
        {
            if(offer==null || !offer.CanRestore || offer.sourcePath!=path)throw new ArgumentException("No recovery candidate for this slot.");
            var state=decode(offer.candidate);state.Validate();return state;
        }
        /// <summary>Explicit confirmation only. Revalidate both files and retain raw damaged primary before replacing it.</summary>
        public FormalCampaignSave RestoreConfirmed(FormalRecoveryOffer offer,out string preservedPath)
        {
            using(AcquireWriteLock())return RestoreLocked(offer,out preservedPath);
        }
        private FormalCampaignSave RestoreLocked(FormalRecoveryOffer offer,out string preservedPath)
        {
            preservedPath=null;var candidate=RecoveryPreview(offer);var now=InspectRecovery();
            if(now.Status==FormalRecoveryStatus.Healthy){var current=Read(path);if(encode(current)==offer.candidate)return current;throw new InvalidOperationException("Current save changed. Do not roll it back.");}
            if(!now.CanRestore || now.fingerprint!=offer.fingerprint || now.candidate!=offer.candidate)throw new InvalidOperationException("Recovery sources changed. Inspect again before confirming.");
            string temp=path+".restore-"+Guid.NewGuid().ToString("N")+".tmp";
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            using(var writer=new StreamWriter(stream)){writer.Write(offer.candidate);writer.Flush();stream.Flush(true);}
            if(encode(Read(temp))!=offer.candidate)throw new ArgumentException("Recovery roundtrip mismatch.");
            // Recheck after staging. A changed primary/backup must never be overwritten.
            now=InspectRecovery();if(!now.CanRestore || now.fingerprint!=offer.fingerprint)throw new InvalidOperationException("Recovery sources changed during preparation.");
            if(File.Exists(path)){
                preservedPath=path+".preserved-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N");
                File.Copy(path,preservedPath,false);
                now=InspectRecovery();if(!now.CanRestore || now.fingerprint!=offer.fingerprint || Digest(File.ReadAllBytes(preservedPath))!=offer.fingerprint.Split('|')[0])throw new InvalidOperationException("Recovery source changed while preserving it.");
                // Do not replace the known-good .bak with corrupt bytes.
                File.Replace(temp,path,null);
            }else File.Move(temp,path);
            var restored=Read(path);if(encode(restored)!=offer.candidate)throw new IOException("Recovered payload verification failed.");return restored;
        }
    }
}
