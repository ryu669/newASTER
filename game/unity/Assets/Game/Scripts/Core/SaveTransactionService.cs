using System;
using System.IO;
using System.Linq;
using System.Text;
namespace NewAster.Core
{
    // Each commit is immutable: latest + two backups. Only 'current' is atomically replaced.
    // A crash before that replacement leaves the entire previous set reachable and unchanged.
    public sealed class SaveTransactionService
    {
        public string Root {get;}
        public SaveValidationService Validation {get;}
        internal Action<string> Fault;
        public SaveTransactionService(string directory,SaveValidationService validation)
        {
            if(string.IsNullOrWhiteSpace(directory) || !Path.IsPathRooted(directory) || directory.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Contains(".."))throw new ArgumentException("Save root must be an absolute safe directory.");
            Root=Path.GetFullPath(directory);Validation=validation??throw new ArgumentNullException(nameof(validation));CheckLinks(Root);
        }
        internal static bool ExpectedFailure(Exception e)=>e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is InvalidOperationException || e is FormatException;
        internal static void CheckLinks(string path)
        {
            for(var item=new DirectoryInfo(Path.GetFullPath(path));item!=null;item=item.Parent)if(item.Exists && (item.Attributes&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Linked save directories are not allowed.");
            if(File.Exists(path) && (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("Linked save files are not allowed.");
        }
        public string SlotPath(int slot)
        {if(slot<1 || slot>2)throw new ArgumentOutOfRangeException(nameof(slot));string path=Path.Combine(Root,"slot-"+slot);CheckLinks(path);return path;}
        internal IDisposable Lock()
        {CheckLinks(Root);Directory.CreateDirectory(Root);CheckLinks(Path.Combine(Root,"operations.lock"));return new FileStream(Path.Combine(Root,"operations.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
        internal string Pointer(int slot)
        {
            string file=Path.Combine(SlotPath(slot),"current");CheckLinks(file);if(!File.Exists(file))return null;
            string id=File.ReadAllText(file,Encoding.UTF8).Trim();if(!Guid.TryParseExact(id,"N",out _))throw new ArgumentException("Invalid commit pointer.");return id;
        }
        internal string FilePath(int slot,string commit,int backup=0)
        {
            if(!Guid.TryParseExact(commit,"N",out _) || backup<0 || backup>2)throw new ArgumentException("Invalid save reference.");
            string path=Path.Combine(SlotPath(slot),"commit-"+commit,backup==0?"latest.json":"backup-"+backup+".json");CheckLinks(path);return path;
        }
        internal void RequirePointer(int slot,string expected)
        {if(Pointer(slot)!=expected)throw new InvalidOperationException("Save changed in another operation. Refresh before retrying.");}
        internal static void DurableWrite(string path,string text)
        {using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))using(var writer=new StreamWriter(stream,new UTF8Encoding(false))){writer.Write(text);writer.Flush();stream.Flush(true);}}
        internal string CommitLocked(int slot,string expected,GameSave document,bool restoring=false)
        {
            RequirePointer(slot,expected);Validation.Validate(document);
            var backups=new System.Collections.Generic.List<string>();
            string preserved=null;
            if(expected!=null){
                for(int n=0;n<=2;n++){
                    string old=FilePath(slot,expected,n);if(!File.Exists(old))continue;
                    try{Validation.Read(old);backups.Add(old);}catch(Exception e)when(ExpectedFailure(e)){
                        if(!restoring)throw;
                        if(n==0)preserved=old;
                    }
                }
            }
            string id=Guid.NewGuid().ToString("N"),folder=Path.GetDirectoryName(FilePath(slot,id));Directory.CreateDirectory(folder);
            DurableWrite(FilePath(slot,id),Validation.Encode(document));Validation.Read(FilePath(slot,id));Fault?.Invoke("latest-verified");
            for(int n=0;n<Math.Min(2,backups.Count);n++){
                File.Copy(backups[n],FilePath(slot,id,n+1));Validation.Read(FilePath(slot,id,n+1));
            }
            if(preserved!=null)File.Copy(preserved,Path.Combine(folder,"preserved-corrupt.json"));
            Fault?.Invoke("backups-verified");RequirePointer(slot,expected);
            string pointer=Path.Combine(SlotPath(slot),"current"),temp=Path.Combine(SlotPath(slot),"pointer-"+id+".tmp");DurableWrite(temp,id+"\n");Fault?.Invoke("before-publish");
            try{
                if(expected==null)File.Move(temp,pointer);else File.Replace(temp,pointer,null);
                Fault?.Invoke("after-publish");
            }catch(Exception e)when(ExpectedFailure(e)){if(Pointer(slot)!=id)throw;}
            // Obsolete complete bundles are reclaimed only after success. Orphan staging is safe.
            try{foreach(var directory in Directory.GetDirectories(SlotPath(slot),"commit-*")){
                if(Path.GetFileName(directory)=="commit-"+id || Path.GetFileName(directory)=="commit-"+expected)continue;
                try{CheckLinks(directory);Directory.Delete(directory,true);}catch(Exception e)when(ExpectedFailure(e)){}
            }}catch(Exception e)when(ExpectedFailure(e)){}
            return id;
        }
    }
}
