using System;
using System.IO;

namespace NewAster.Core
{
    public enum FormalLoadResult { Missing, Loaded, RecoveredBackup, Blocked }
    /// <summary>Separate formal file; never reads CampaignSaveV2. Codec is provided by the host.</summary>
    public sealed class FormalGrowthStore
    {
        private readonly string path, saveId;
        private readonly Func<FormalGrowthSave,string> encode;
        private readonly Func<string,FormalGrowthSave> decode;
        public FormalGrowthStore(string path,string saveId,Func<FormalGrowthSave,string> encode,Func<string,FormalGrowthSave> decode)
        {
            if(string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(saveId) || encode==null || decode==null) throw new ArgumentException("Save location, identity and codec required.");
            this.path=Path.GetFullPath(path);this.saveId=saveId;this.encode=encode;this.decode=decode;
        }
        private FormalGrowthSave Read(string file)
        {
            var state=decode(File.ReadAllText(file));
            if(state==null) throw new ArgumentException("Empty save payload.");
            state.Validate();
            if(state.saveId!=saveId) throw new ArgumentException("Save identity mismatch.");
            return state;
        }
        public FormalLoadResult Load(out FormalGrowthSave state)
        {
            state=null;
            if(!File.Exists(path)) {
                // An orphan backup is not a new-game slot. The caller must explicitly resolve it.
                return File.Exists(path+".bak")?FormalLoadResult.Blocked:FormalLoadResult.Missing;
            }
            try {
                var parsed=decode(File.ReadAllText(path));
                if(parsed!=null && (parsed.version!=1 || parsed.contentVersion!=FormalGrowthSave.ContentVersion || parsed.saveId!=saveId)) return FormalLoadResult.Blocked;
                state=Read(path);return FormalLoadResult.Loaded;
            } catch(Exception e) when(IsReadFailure(e)) {
                try {state=Read(path+".bak");return FormalLoadResult.RecoveredBackup;}
                catch(Exception backupError) when(IsReadFailure(backupError)) {state=null;return FormalLoadResult.Blocked;}
            }
        }
        private static bool IsReadFailure(Exception e) => e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException || e is FormatException;
        public bool Save(FormalGrowthSave next)
        {
            if(next==null) throw new ArgumentNullException(nameof(next));next.Validate();
            if(next.saveId!=saveId) throw new ArgumentException("Save identity mismatch.");
            // Validate the primary before touching it. Never roll a future or damaged file back implicitly.
            if(File.Exists(path)) {
                var previous=Read(path);
                if(next.revision!=previous.revision+1 || next.receipts.Length!=previous.receipts.Length+1 || previous.receipts.AnyMissingFrom(next)) throw new ArgumentException("Invalid durable revision transition.");
            } else if(File.Exists(path+".bak")) throw new InvalidOperationException("Orphan backup requires explicit recovery.");
            var directory=Path.GetDirectoryName(path);Directory.CreateDirectory(directory);
            var temp=path+".tmp";
            string text=encode(next.Copy());
            // Flush and validate all data before replacing the last normal primary.
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            using(var writer=new StreamWriter(stream)) {writer.Write(text);writer.Flush();stream.Flush(true);}
            var verified=Read(temp);
            if(encode(verified)!=encode(next)) throw new ArgumentException("Round-trip save verification failed.");
            if(File.Exists(path)) File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            return true;
        }
    }
    internal static class GrowthReceiptExtensions
    {
        internal static bool AnyMissingFrom(this GrowthReceipt[] previous,FormalGrowthSave next)
        {
            foreach(var receipt in previous) {
                bool found=false;
                foreach(var item in next.receipts) if(item.transactionId==receipt.transactionId && item.signature==receipt.signature) {found=true;break;}
                if(!found) return true;
            }
            return false;
        }
    }
}
