using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class FormalRecoveryTests
{
    public static void Run(Action<bool,string> check)
    {
        var options=new JsonSerializerOptions {IncludeFields=true};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=t=>{try{return JsonSerializer.Deserialize<FormalCampaignSave>(t,options);}catch(JsonException e){throw new ArgumentException("JSON",e);}};
        Action<Action,string> reject=(action,name)=>{bool threw=false;try{action();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is IOException){threw=true;}check(threw,name);};
        var seed=new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",stones=888,kinderPoints=100,nectar=500,heroines=new[]{new FormalHeroineGrowth {heroineId="heroine.slayer",level=40}},tickets=new[]{new HeroineTicket {heroineId="heroine.slayer",count=2}}}};
        string dir=Path.Combine(Path.GetTempPath(),"newaster-recovery-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"formal.json");
        Func<string,FormalCampaignHeader> header=t=>{try{return JsonSerializer.Deserialize<FormalCampaignHeader>(t,options);}catch(JsonException e){throw new ArgumentException("Header JSON",e);}};
        var store=new FormalCampaignStore(path,encode,decode,header);byte[] damaged={255,0,1,33};
        RunSplit(check,options,encode,decode,header,seed);
        try{
            check(store.InspectRecovery().Status==FormalRecoveryStatus.Missing,"Empty slot has no recovery action");
            store.Save(seed);check(store.InspectRecovery().Status==FormalRecoveryStatus.Healthy,"Healthy current never offers rollback");
            var changed=decode(encode(seed));changed.revision=1;changed.world.materials+=5;store.Save(changed);string backup=File.ReadAllText(path+".bak");
            File.WriteAllBytes(path,damaged);var offer=store.InspectRecovery();
            check(offer.CanRestore && offer.Status==FormalRecoveryStatus.Ready,"Corrupt current with valid backup offers explicit recovery");
            var preview=store.RecoveryPreview(offer);check(preview.growth.heroines[0].level==40 && preview.growth.stones==888 && preview.growth.tickets[0].count==2,"Recovery preview includes complete unified inventory");
            preview.growth.stones=0;preview.world.materials=999;
            check(store.RecoveryPreview(offer).growth.stones==888 && File.ReadAllBytes(path).SequenceEqual(damaged) && File.ReadAllText(path+".bak")==backup,"Preview cannot mutate offer or files");
            var restored=store.RestoreConfirmed(offer,out var retained);
            check(File.Exists(retained) && File.ReadAllBytes(retained).SequenceEqual(damaged),"Corrupt raw bytes retained under unique non-overwriting name");
            check(File.ReadAllText(path+".bak")==backup,"Known-good backup not replaced with corrupt bytes");
            check(store.Load(out var loaded)==FormalLoadResult.Loaded && encode(loaded)==encode(seed) && encode(restored)==encode(seed),"Recovery restores entire snapshot without grants or draws");
            string after=File.ReadAllText(path);int files=Directory.GetFiles(dir).Length;
            store.RestoreConfirmed(offer,out var repeatedArchive);
            check(repeatedArchive==null && File.ReadAllText(path)==after && Directory.GetFiles(dir).Length==files,"Repeated confirmation is no-op after exact recovery");
            var journal=new FormalCampaignJournal(restored,encode,decode);var w=journal.Snapshot.world;w.materials+=1;journal.CommitWorld(w,store.Save);
            check(store.Load(out loaded)==FormalLoadResult.Loaded && loaded.revision==1 && loaded.growth.stones==888,"Recovered game can save normal future progress");
            reject(()=>store.RestoreConfirmed(offer,out _),"Old offer cannot roll back a later healthy revision");
            File.WriteAllBytes(path,damaged);offer=store.InspectRecovery();var laterBackup=decode(backup);laterBackup.revision=7;File.WriteAllText(path+".bak",encode(laterBackup));
            reject(()=>store.RestoreConfirmed(offer,out _),"Changed backup invalidates confirmation");check(File.ReadAllBytes(path).SequenceEqual(damaged),"Stale backup offer leaves primary untouched");
            File.WriteAllText(path+".bak",backup);offer=store.InspectRecovery();var future=decode(backup);future.version=2;File.WriteAllText(path,encode(future));
            check(store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Future primary never offers older backup");reject(()=>store.RestoreConfirmed(offer,out _),"Future primary appearing after preview blocks restore");check(File.ReadAllText(path)==encode(future),"Future primary preserved");
            future.version=1;future.growth.version=3;File.WriteAllText(path,encode(future));check(store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Future embedded growth protected");
            future=decode(backup);future.saveId="alien";File.WriteAllText(path,encode(future));check(store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Foreign primary identity protected");
            future=decode(backup);future.world.version=3;File.WriteAllText(path,encode(future));check(store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Future embedded world protected");
            File.WriteAllText(path,encode(future),System.Text.Encoding.Unicode);check(store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"UTF16 BOM future primary recognized not treated as corrupt");
            File.WriteAllText(path,"{\"version\":2,\"saveId\":\"newaster.formal-campaign\",\"world\":\"future-type\",\"growth\":\"future-type\"}");
            check(store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported && store.Load(out _)==FormalLoadResult.Blocked,"Stable future header checked before incompatible future payload types");
            File.WriteAllBytes(path,damaged);File.WriteAllText(path+".bak","broken");check(store.InspectRecovery().Status==FormalRecoveryStatus.NoValidBackup,"Corrupt backup disables action");
            File.WriteAllText(path+".bak",encode(future));check(store.InspectRecovery().Status==FormalRecoveryStatus.NoValidBackup,"Unsupported backup disables action");
            File.Delete(path+".bak");check(store.InspectRecovery().Status==FormalRecoveryStatus.NoValidBackup,"Missing backup with damaged current disables action");
            File.Delete(path);File.WriteAllText(path+".bak",backup);check(store.Load(out _)==FormalLoadResult.Blocked,"Orphan backup does not automatically start new game");
            offer=store.InspectRecovery();check(offer.CanRestore,"Orphan valid backup requires explicit recovery");
            restored=store.RestoreConfirmed(offer,out retained);check(retained==null && store.Load(out loaded)==FormalLoadResult.Loaded && encode(loaded)==encode(seed),"Missing primary restored without pretending to preserve a missing file");
            File.WriteAllBytes(path,damaged);offer=store.InspectRecovery();var other=new FormalCampaignStore(Path.Combine(dir,"other.json"),encode,decode,header);reject(()=>other.RestoreConfirmed(offer,out _),"Recovery offer bound to inspected save path");
            using(var locked=new FileStream(path+".write.lock",FileMode.Open,FileAccess.ReadWrite,FileShare.None)){
                reject(()=>store.RestoreConfirmed(offer,out _),"Concurrent write lock blocks recovery");reject(()=>store.Save(seed),"Concurrent write lock blocks normal write");
            }
            check(File.ReadAllBytes(path).SequenceEqual(damaged),"Concurrent rejection does not touch primary");
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)){
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))reject(()=>store.RestoreConfirmed(offer,out _),"Replacement failure retains damaged original and valid backup");
                check(File.ReadAllBytes(path).SequenceEqual(damaged) && File.ReadAllText(path+".bak")==backup,"Failed replacement keeps both input files");
                store.RestoreConfirmed(offer,out retained);check(store.Load(out loaded)==FormalLoadResult.Loaded,"Replacement failure retry completes safely");
            }
        }finally{foreach(var file in Directory.GetFiles(dir))File.Delete(file);Directory.Delete(dir);}
    }
    private static void RunSplit(Action<bool,string> check,JsonSerializerOptions options,Func<FormalCampaignSave,string> encode,Func<string,FormalCampaignSave> decode,Func<string,FormalCampaignHeader> header,FormalCampaignSave seed)
    {
        string dir=Path.Combine(Path.GetTempPath(),"newaster-split-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        string path=Path.Combine(dir,"unified.json"),growth=Path.Combine(dir,"growth.json"),world=Path.Combine(dir,"world.json");
        var store=new FormalCampaignStore(path,encode,decode,header);
        Func<string,T> codec<T>()=>t=>{try{return JsonSerializer.Deserialize<T>(t,options);}catch(JsonException e){throw new ArgumentException("JSON",e);}};
        Func<FormalRecoveryOffer> inspect=()=>store.InspectSplitRecovery(growth,world,codec<FormalGrowthHeader>(),codec<FormalGrowthSave>(),codec<FormalWorldHeader>(),codec<CampaignSaveV2>(),seed.growth,seed.world);
        Action<Action,string> reject=(action,name)=>{bool threw=false;try{action();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is IOException){threw=true;}check(threw,name);};
        try {
            File.WriteAllText(growth,"damaged");string g=JsonSerializer.Serialize(seed.growth,options),w=JsonSerializer.Serialize(seed.world,options);File.WriteAllText(growth+".bak",g);File.WriteAllText(world,w);
            var offer=inspect();check(offer.CanRestore,"Damaged split growth offers backup with healthy world");
            var preview=store.RecoveryPreview(offer);check(preview.growth.stones==888 && preview.growth.heroines[0].level==40,"Split preview preserves existing state");preview.growth.stones=0;
            check(!File.Exists(path) && File.ReadAllText(growth)=="damaged","Split inspection and preview are read-only");
            File.WriteAllText(world,w+" ");reject(()=>store.RestoreSplitConfirmed(offer,growth,world),"Changed healthy source invalidates split offer");check(!File.Exists(path),"Stale split offer creates no unified file");File.WriteAllText(world,w);
            offer=inspect();var restored=store.RestoreSplitConfirmed(offer,growth,world);check(encode(restored)==encode(seed),"Confirmed split recovery preserves both complete payloads");
            check(File.ReadAllText(growth)=="damaged" && File.ReadAllText(growth+".bak")==g && File.ReadAllText(world)==w,"Every split source retained byte-for-byte");
            check(encode(store.RestoreSplitConfirmed(offer,growth,world))==encode(seed),"Repeated split confirmation adds no grants");
            var journal=new FormalCampaignJournal(restored,encode,decode);var next=journal.Snapshot.world;next.materials++;journal.CommitWorld(next,store.Save);reject(()=>store.RestoreSplitConfirmed(offer,growth,world),"Old split offer never rolls back new unified progress");
            File.Delete(path);File.Delete(path+".bak");
            File.WriteAllText(growth,"{\"version\":3,\"saveId\":\"newaster.formal-growth\",\"contentVersion\":\"growth-2026-10-02\",\"heroines\":\"future-type\"}");check(inspect().Status==FormalRecoveryStatus.Unsupported,"Future split header protects incompatible payload");
            File.WriteAllText(growth,"damaged");File.WriteAllText(world,"{\"version\":3,\"materials\":\"future-type\"}");File.WriteAllText(world+".bak",w);check(inspect().Status==FormalRecoveryStatus.Unsupported,"Future world does not fall back to old backup");
            File.Delete(world);File.Delete(growth);offer=inspect();check(offer.CanRestore,"Missing split primaries offer explicit orphan backups");
            restored=store.RestoreSplitConfirmed(offer,growth,world);check(encode(restored)==encode(seed),"Orphan split backup restores without new grants");File.Delete(path);
            File.WriteAllText(growth+".bak","broken");check(inspect().Status==FormalRecoveryStatus.NoValidBackup,"Invalid split backup refuses initialization");
            File.WriteAllText(growth+".bak",g);offer=inspect();File.WriteAllText(path+".bak",encode(seed));reject(()=>store.RestoreSplitConfirmed(offer,growth,world),"Appearing unified orphan backup prevents split initialization");File.Delete(path+".bak");
            offer=inspect();using(var lease=new FileStream(path+".write.lock",FileMode.Open,FileAccess.ReadWrite,FileShare.None))reject(()=>store.RestoreSplitConfirmed(offer,growth,world),"Split recovery shares exclusive writer lease");
            File.Delete(growth+".bak");File.Delete(world+".bak");check(inspect().Status==FormalRecoveryStatus.Healthy,"Truly empty split sources have no recovery action");
        }finally{foreach(var file in Directory.GetFiles(dir))File.Delete(file);Directory.Delete(dir);}
    }
}
