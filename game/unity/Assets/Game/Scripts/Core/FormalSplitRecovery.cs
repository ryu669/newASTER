using System;
using System.IO;
namespace NewAster.Core
{
    [Serializable] public sealed class FormalGrowthHeader { public int version; public string saveId,contentVersion; }
    [Serializable] public sealed class FormalWorldHeader { public int version; }
    /// <summary>Read-only sources. Confirmation creates a unified slot; never repairs or deletes inputs.</summary>
    public sealed partial class FormalCampaignStore
    {
        public FormalRecoveryOffer InspectSplitRecovery(string growthPath,string worldPath,
            Func<string,FormalGrowthHeader> growthHeader,Func<string,FormalGrowthSave> growthDecode,
            Func<string,FormalWorldHeader> worldHeader,Func<string,CampaignSaveV2> worldDecode,
            FormalGrowthSave initialGrowth,CampaignSaveV2 initialWorld)
        {
            try {
                if(File.Exists(path) || File.Exists(path+".bak"))return new FormalRecoveryOffer(FormalRecoveryStatus.Unsupported,path);
                bool usedBackup=false;
                var growth=ReadSplit(growthPath,t=>{var h=growthHeader(t);return h!=null && (h.version!=1 && h.version!=2 || h.saveId!="newaster.formal-growth" || h.contentVersion!=FormalGrowthSave.ContentVersion);},t=>{var s=growthDecode(t);if(s==null)throw new ArgumentException("Empty growth.");s.UpgradeFormalV1();s.Validate();if(s.saveId!="newaster.formal-growth")throw new ArgumentException("Growth identity.");return s;},initialGrowth,ref usedBackup);
                var world=ReadSplit(worldPath,t=>{var h=worldHeader(t);return h!=null && h.version!=2;},t=>{var s=worldDecode(t);FormalCampaignSave.ValidateWorld(s);return s;},initialWorld,ref usedBackup);
                if(!usedBackup)return new FormalRecoveryOffer(FormalRecoveryStatus.Healthy,path);
                var candidate=new FormalCampaignSave {growth=growth,world=world};candidate.Validate();
                return new FormalRecoveryOffer(FormalRecoveryStatus.Ready,path,SplitFingerprint(growthPath,worldPath),encode(candidate));
            }catch(NotSupportedException){return new FormalRecoveryOffer(FormalRecoveryStatus.Unsupported,path);}
            catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is FormatException){return new FormalRecoveryOffer(FormalRecoveryStatus.NoValidBackup,path);}
            catch(Exception e)when(e is IOException || e is UnauthorizedAccessException){return new FormalRecoveryOffer(FormalRecoveryStatus.Unavailable,path);}
        }
        private T ReadSplit<T>(string source,Func<string,bool> unsupported,Func<string,T> parse,T initial,ref bool usedBackup)
        {
            if(File.Exists(source)){
                string text=File.ReadAllText(source);
                try {if(unsupported(text))throw new NotSupportedException("Future split source.");return parse(text);}
                catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is FormatException){}
            }else if(!File.Exists(source+".bak"))return initial;
            if(!File.Exists(source+".bak"))throw new ArgumentException("No valid split backup.");
            string backup=File.ReadAllText(source+".bak");if(unsupported(backup))throw new NotSupportedException("Future split backup.");
            var recovered=parse(backup);usedBackup=true;return recovered;
        }
        private static string SplitFingerprint(string growthPath,string worldPath)
        {
            string result="";foreach(var file in new[]{Path.GetFullPath(growthPath),Path.GetFullPath(growthPath)+".bak",Path.GetFullPath(worldPath),Path.GetFullPath(worldPath)+".bak"})result+=file+":"+(File.Exists(file)?Digest(File.ReadAllBytes(file)):"missing")+"|";return result;
        }
        public FormalCampaignSave RestoreSplitConfirmed(FormalRecoveryOffer offer,string growthPath,string worldPath)
        {
            using(AcquireWriteLock()){
                var candidate=RecoveryPreview(offer);
                if(File.Exists(path)){
                    var current=Read(path);if(encode(current)==offer.candidate)return current;
                    throw new InvalidOperationException("Unified slot appeared. Inspect again.");
                }
                if(File.Exists(path+".bak") || offer.fingerprint!=SplitFingerprint(growthPath,worldPath))throw new InvalidOperationException("Split sources changed. Inspect again.");
                string temp=path+".split-"+Guid.NewGuid().ToString("N")+".tmp";
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                using(var writer=new StreamWriter(stream)){writer.Write(offer.candidate);writer.Flush();stream.Flush(true);}
                if(encode(Read(temp))!=offer.candidate)throw new ArgumentException("Split recovery roundtrip mismatch.");
                if(File.Exists(path+".bak") || offer.fingerprint!=SplitFingerprint(growthPath,worldPath))throw new InvalidOperationException("Split sources changed during preparation.");
                File.Move(temp,path);return Read(path);
            }
        }
    }
}
