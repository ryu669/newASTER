using System;
using System.IO;
using System.Linq;
namespace NewAster.Core
{
    public enum SaveSlotStatus { Empty,Ready,Corrupt,Unsupported,Unavailable }
    public sealed class SaveSlotSummary
    {
        public int Slot {get;internal set;}
        public int Backup {get;internal set;}
        public SaveSlotStatus Status {get;internal set;}
        public SaveHeader Header {get;internal set;}
        public long PlaySeconds {get;internal set;}
        public int Heroes {get;internal set;}
        public int Victories {get;internal set;}
        public int HighestLevel {get;internal set;}
        internal string Commit,Checksum;
    }
    public sealed class SaveBackupService
    {
        private readonly SaveTransactionService transactions;
        public SaveBackupService(SaveTransactionService transactions){this.transactions=transactions;}
        internal SaveSlotSummary InspectLocked(int slot,int backup)
        {
            var result=new SaveSlotSummary{Slot=slot,Backup=backup,Status=SaveSlotStatus.Empty};
            try{
                result.Commit=transactions.Pointer(slot);if(result.Commit==null)return result;
                string file=transactions.FilePath(slot,result.Commit,backup);
                if(!File.Exists(file)){result.Status=backup==0?SaveSlotStatus.Corrupt:SaveSlotStatus.Empty;return result;}
                var doc=transactions.Validation.Read(file);var save=transactions.Validation.Validate(doc);
                result.Header=doc.header;result.Checksum=doc.checksum;result.Status=SaveSlotStatus.Ready;
                result.PlaySeconds=save.playRewards?.totalPlaySeconds??save.engagement?.activeSeconds??0;result.Heroes=save.growth.heroines.Length;result.Victories=save.world.claimedBattleIds.Length;result.HighestLevel=save.world.highestBattleLevel;
            }catch(UnsupportedSaveException){result.Status=SaveSlotStatus.Unsupported;}
            catch(Exception e)when(SaveTransactionService.ExpectedFailure(e)){result.Status=SaveSlotStatus.Corrupt;}
            return result;
        }
        public SaveSlotSummary Inspect(int slot,int backup=0)
        {if(backup<0 || backup>2)throw new ArgumentOutOfRangeException(nameof(backup));using(transactions.Lock())return InspectLocked(slot,backup);}
    }
    public sealed class SaveRestoreService
    {
        private readonly SaveTransactionService transactions;
        public SaveRestoreService(SaveTransactionService transactions){this.transactions=transactions;}
        internal string RestoreLocked(SaveSlotSummary offer,DateTime now)
        {
            if(offer==null || offer.Backup<1 || offer.Backup>2 || offer.Status!=SaveSlotStatus.Ready)throw new ArgumentException("Choose a valid backup first.");
            transactions.RequirePointer(offer.Slot,offer.Commit);
            var doc=transactions.Validation.Read(transactions.FilePath(offer.Slot,offer.Commit,offer.Backup));
            if(doc.checksum!=offer.Checksum)throw new InvalidOperationException("Backup changed since confirmation.");
            var save=transactions.Validation.Validate(doc);
            return transactions.CommitLocked(offer.Slot,offer.Commit,transactions.Validation.Create(save,doc.header.saveId,now),true);
        }
    }
    public sealed class SaveDeleteConfirmation
    {
        public int Slot {get;internal set;}
        public int Stage {get;internal set;}
        internal string Commit,Owner;
    }
    public sealed class SaveSlotManager
    {
        public const int SlotCount=2;
        public int ActiveSlot {get;private set;}=1;
        public bool ActiveSlotSettingPending {get;private set;}
        public bool DeleteCleanupPending {get;private set;}
        public SaveTransactionService Transactions {get;}
        public SaveBackupService Backups {get;}
        public SaveRestoreService Restore {get;}
        private readonly string owner=Guid.NewGuid().ToString("N");
        private readonly string[] observed=new string[3];
        public static string GenerationDirectory(string baseDirectory,int generation)
        {if(generation<0 || generation>1)throw new ArgumentOutOfRangeException(nameof(generation));return Path.Combine(baseDirectory,generation==0?"development-saves":"release-saves-generation-1");}
        public SaveSlotManager(SaveTransactionService transactions)
        {
            Transactions=transactions;Backups=new SaveBackupService(transactions);Restore=new SaveRestoreService(transactions);
            string active=Path.Combine(transactions.Root,"active-slot");SaveTransactionService.CheckLinks(active);
            if(File.Exists(active) && int.TryParse(File.ReadAllText(active),out int slot) && slot>=1 && slot<=2)ActiveSlot=slot;
            for(int n=1;n<=2;n++){try{observed[n]=transactions.Pointer(n);}catch(Exception e)when(SaveTransactionService.ExpectedFailure(e)){} }
            try{CleanupDeletedSlots();}catch(Exception e)when(SaveTransactionService.ExpectedFailure(e)){DeleteCleanupPending=true;}
        }
        public SaveSlotSummary Inspect(int slot,int backup=0)
        {var result=Backups.Inspect(slot,backup);observed[slot]=result.Commit;return result;}
        public FormalCampaignSave Load(int slot)
        {
            using(Transactions.Lock()){
                var offer=Backups.InspectLocked(slot,0);if(offer.Status!=SaveSlotStatus.Ready)throw new ArgumentException("This slot cannot be loaded. Choose a backup or another slot.");
                var save=Transactions.Validation.Validate(Transactions.Validation.Read(Transactions.FilePath(slot,offer.Commit)));
                observed[slot]=offer.Commit;SetActiveLocked(slot);return save;
            }
        }
        public bool Save(int slot,FormalCampaignSave save,DateTime now,bool makeActive=true)
        {
            Transactions.SlotPath(slot);
            using(Transactions.Lock()){
                string expected=observed[slot];Transactions.RequirePointer(slot,expected);
                string id=expected==null?Guid.NewGuid().ToString("N"):Transactions.Validation.Read(Transactions.FilePath(slot,expected)).header.saveId;
                var document=Transactions.Validation.Create(save,id,now);
                observed[slot]=Transactions.CommitLocked(slot,expected,document);
                if(makeActive)SetActiveLocked(slot);return true;
            }
        }
        public bool AutoSave(FormalCampaignSave save,DateTime now)=>Save(ActiveSlot,save,now,false);
        public FormalCampaignSave RestoreConfirmed(SaveSlotSummary offer,DateTime now)
        {
            using(Transactions.Lock()){
                observed[offer.Slot]=Restore.RestoreLocked(offer,now);SetActiveLocked(offer.Slot);
                return Transactions.Validation.Validate(Transactions.Validation.Read(Transactions.FilePath(offer.Slot,observed[offer.Slot])));
            }
        }
        public SaveDeleteConfirmation RequestDelete(int slot)
        {using(Transactions.Lock())return new SaveDeleteConfirmation{Slot=slot,Commit=DeleteFingerprint(slot),Owner=owner,Stage=1};}
        private string DeleteFingerprint(int slot)
        {string file=Path.Combine(Transactions.SlotPath(slot),"current");SaveTransactionService.CheckLinks(file);return File.Exists(file)?File.ReadAllText(file):null;}
        public void ConfirmDelete(SaveDeleteConfirmation ticket)
        {if(ticket==null || ticket.Owner!=owner || ticket.Stage!=1)throw new ArgumentException("First delete confirmation is required.");ticket.Stage=2;}
        public void BackDeleteConfirmation(SaveDeleteConfirmation ticket)
        {if(ticket==null || ticket.Owner!=owner || ticket.Stage!=2)throw new ArgumentException("Final delete confirmation is required.");ticket.Stage=1;}
        public void DeleteConfirmed(SaveDeleteConfirmation ticket)
        {
            if(ticket==null || ticket.Owner!=owner || ticket.Stage!=2)throw new ArgumentException("Two delete confirmations are required.");
            using(Transactions.Lock()){
                if(DeleteFingerprint(ticket.Slot)!=ticket.Commit)throw new InvalidOperationException("Save changed since delete confirmation.");
                string source=Transactions.SlotPath(ticket.Slot);
                if(Directory.Exists(source)){
                    string retired=Path.Combine(Transactions.Root,"deleted-slot-"+ticket.Slot+"-"+Guid.NewGuid().ToString("N"));
                    SaveTransactionService.CheckLinks(source);Directory.Move(source,retired);
                    // This tree belongs only to the selected slot; it cannot include another slot.
                    try{SaveTransactionService.CheckLinks(retired);Directory.Delete(retired,true);DeleteCleanupPending=false;}
                    catch(Exception e)when(SaveTransactionService.ExpectedFailure(e)){DeleteCleanupPending=true;}
                }
                observed[ticket.Slot]=null;ticket.Stage=3;
            }
        }
        public void FlushActiveSlotSetting()
        {if(ActiveSlotSettingPending)using(Transactions.Lock())SetActiveLocked(ActiveSlot);}
        public void CleanupDeletedSlots()
        {
            using(Transactions.Lock()){
                DeleteCleanupPending=false;
                foreach(string path in Directory.GetDirectories(Transactions.Root,"deleted-slot-*")){
                    string name=Path.GetFileName(path);if(!(name.StartsWith("deleted-slot-1-",StringComparison.Ordinal) || name.StartsWith("deleted-slot-2-",StringComparison.Ordinal)) || !Guid.TryParseExact(name.Substring(15),"N",out _))continue;
                    try{SaveTransactionService.CheckLinks(path);Directory.Delete(path,true);}catch(Exception e)when(SaveTransactionService.ExpectedFailure(e)){DeleteCleanupPending=true;}
                }
            }
        }
        private void SetActiveLocked(int slot)
        {
            ActiveSlot=slot;
            try{string path=Path.Combine(Transactions.Root,"active-slot"),temp=path+"-"+Guid.NewGuid().ToString("N")+".tmp";SaveTransactionService.CheckLinks(path);SaveTransactionService.DurableWrite(temp,slot.ToString());
                if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);ActiveSlotSettingPending=false;
            }catch(Exception e)when(SaveTransactionService.ExpectedFailure(e)){ActiveSlotSettingPending=true;}
        }
    }
    // Deferred changes are acknowledged in the in-memory journal. Important operations persist
    // the complete current snapshot, including these changes, before consuming their rewards.
    public sealed class DelayedSaveCoordinator
    {
        public bool Dirty=>pending!=null;
        public double DueAt {get;private set;}
        private FormalCampaignSave pending;
        private readonly SaveSlotManager slots;
        public DelayedSaveCoordinator(SaveSlotManager slots){this.slots=slots;}
        public bool Queue(FormalCampaignSave save,double clock)
        {pending=slots.Transactions.Validation.Copy(save);DueAt=clock+5;return true;}
        public bool SaveImmediate(FormalCampaignSave save,DateTime now)
        {if(!slots.AutoSave(save,now))return false;pending=null;return true;}
        public bool Flush(double clock,DateTime now,bool force=false)
        {if(pending==null || !force && clock<DueAt)return true;if(!slots.AutoSave(pending,now))return false;pending=null;return true;}
    }
}
