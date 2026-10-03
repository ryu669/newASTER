using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan6HomeTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);
        Func<string,FormalCampaignSave> decode=s=>{try{return JsonSerializer.Deserialize<FormalCampaignSave>(s,options);}catch(JsonException e){throw new ArgumentException("JSON",e);}};
        Func<string,FormalCampaignHeader> header=s=>{try{return JsonSerializer.Deserialize<FormalCampaignHeader>(s,options);}catch(JsonException e){throw new ArgumentException("Header JSON",e);}};
        Func<HomeExperienceCatalog> fixture=()=>HomeExperienceFixture.Create(combat);
        Func<FormalCampaignSave> fresh=()=>new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",stones=321,heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()}};
        Action<Action,string> reject=(action,name)=>{bool rejected=false;try{action();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is OverflowException){rejected=true;}check(rejected,name);};
        Action<Action<HomeExperienceCatalog>,string,string> invalid=(mutate,code,name)=>{var c=fixture();mutate(c);HomeDiagnostic diagnostic=null;try{c.Validate();}catch(HomeDefinitionException e){diagnostic=e.Diagnostic;}check(diagnostic!=null && diagnostic.Code==code && diagnostic.DocumentId==c.contentVersion && diagnostic.JsonPointer.StartsWith("/") && !string.IsNullOrWhiteSpace(diagnostic.Message),name);};
        var catalog=fixture();catalog.Validate();check(catalog.events.Length==25 && catalog.weaponNodes.Length==20 && catalog.chapters.Length==60,"Plan6 fixture covers 5 heroes and 3+2 events without authored content");
        check(catalog.gardens.Length==9 && catalog.gardens.Count(g=>g.unmade)==7 && catalog.gardens.Where(g=>g.unmade).All(g=>g.zones.Length==0),"All nine garden pages exist without fabricating seven placement layouts");
        var restored=JsonSerializer.Deserialize<HomeExperienceCatalog>(JsonSerializer.Serialize(catalog,options),options);restored.Validate();check(restored.scripts[0].commands[2].textId=="text.fixture.line","Full home pack survives nested JSON roundtrip");
        reject(()=>catalog.Validate(true),"Fixture rejected by release gate");
        invalid(c=>c.schemaVersion=2,"UNKNOWN_SCHEMA","Future home pack rejected");
        invalid(c=>c.subjects[1].pageOrder=0,"DUPLICATE_ID","Duplicate page order diagnosed");
        invalid(c=>c.subjects[0].subjectId="missing","MISSING_REFERENCE","Unknown book subject diagnosed");
        invalid(c=>c.weaponNodes[1].parentIds=new[]{c.weaponNodes[4].id},"MISSING_REFERENCE","Cross-hero parent rejected");
        invalid(c=>c.weaponNodes[1].parentIds=new[]{c.weaponNodes[3].id},"DEPENDENCY_CYCLE","Weapon cycle rejected");
        invalid(c=>c.weaponNodes[1].costs=null,"UNRESOLVED_RULE","Unset weapon cost rejected");
        invalid(c=>c.weaponNodes[1].costs[0].amount=-1,"INVALID_RANGE","Negative cost rejected");
        invalid(c=>c.weaponNodes[1].costs[0].resourceId="resource.materials","MISSING_REFERENCE","Weapon cost must resolve to an explicit colossus material");
        invalid(c=>c.weaponNodes[1].costs=new[]{new HomeCost{resourceId=c.resourceIds[0],amount=int.MaxValue},new HomeCost{resourceId=c.resourceIds[0],amount=1}},"INVALID_RANGE","Summed cost overflow rejected");
        invalid(c=>c.gardens[0].zones[0].bounds.width=float.NaN,"INVALID_RANGE","Nonfinite zone rejected");
        invalid(c=>c.furniture[0].size01.x=0,"INVALID_RANGE","Zero furniture size rejected");
        invalid(c=>c.gardens[2].zones=c.gardens[0].zones,"INVALID_RANGE","Unmade garden cannot silently reuse another layout");
        invalid(c=>c.furniture[0].footprint.x=.8f,"INVALID_RANGE","Out-of-bounds furniture footprint rejected");
        invalid(c=>c.displays[0].expressions=Array.Empty<HomeDisplayVariant>(),"MISSING_REFERENCE","Missing normal expression rejected");
        invalid(c=>c.assets.Single(a=>a.id=="art.candidate.slayer.expression.joy.v1").overlayRegion01.width=float.NaN,"INVALID_RANGE","Nonfinite expression patch rejected");
        invalid(c=>c.assets.Single(a=>a.id=="art.candidate.slayer.expression.joy.v1").overlayRegion01.x=.99f,"INVALID_RANGE","Expression patch outside source rejected");
        invalid(c=>c.assets.Single(a=>a.id=="art.candidate.slayer.expression.joy.v1").fullFrame=true,"INVALID_RANGE","Ambiguous full-frame and regional expression rejected");
        invalid(c=>c.assets.Single(a=>a.id=="art.candidate.slayer.expression.joy.v1").kind="standing","INVALID_RANGE","Regional overlay restricted to expression assets");
        invalid(c=>c.events[0].unlockCondition=null,"UNRESOLVED_RULE","Undefined event condition not replaced by always");
        invalid(c=>c.events[0].unlockCondition=new HomeCondition{kind="all"},"INVALID_RANGE","Empty all condition rejected");
        invalid(c=>c.events[0].unlockCondition=new HomeCondition{kind="atLeast",domain="affection",value=1},"MISSING_REFERENCE","Affection condition needs owner");
        invalid(c=>c.events[0].unlockCondition=new HomeCondition{kind="flag",domain="eventRead",id=c.events[1].id},"DEPENDENCY_CYCLE","Read-event dependency cycle rejected");
        invalid(c=>c.events[2].unlockCondition=new HomeCondition{kind="flag",domain="lover",id=c.heroineIds[0]},"DEPENDENCY_CYCLE","Lover producer cycle rejected");
        invalid(c=>c.scripts[0].commands[0].assetId="missing","MISSING_REFERENCE","Background reference rejected");
        invalid(c=>c.scripts[0].commands[2].kind="choice","INVALID_RANGE","Script without required line rejected");
        invalid(c=>c.scripts[0].commands[1].kind="choice","UNRESOLVED_RULE","Unknown script command rejected");
        invalid(c=>c.texts[0].text=" ","INVALID_RANGE","Empty script text rejected");
        invalid(c=>c.scripts[0].commands[3].kind="line","INVALID_RANGE","Missing end rejected");
        invalid(c=>{c.status="release";},"PLACEHOLDER_IN_RELEASE","Placeholder asset in release rejected");
        var old=fresh();var oldText=encode(old);var withoutHome=oldText.Replace(",\"home\":null","");
        check(FormalCampaignJsonShape.WithNullRootMember("{\"nested\":{\"home\":1},\"home\":{\"label\":\"}\\\"\",\"items\":[{}]},\"tail\":7}","home")=="{\"nested\":{\"home\":1},\"home\":null,\"tail\":7}","Null normalization respects nested arrays and escaped strings");
        check(FormalCampaignJsonShape.WithNullRootMember("{\"ho\\u006de\":{},\"tail\":7}","home")=="{\"ho\\u006de\":null,\"tail\":7}","Null normalization resolves escaped root member name");
        check(FormalCampaignJsonShape.WithNullRootMember("{\"home\":null,\"tail\":7}","home")=="{\"home\":null,\"tail\":7}","Null normalization is idempotent");
        check(decode(withoutHome).home==null && decode(oldText).home==null,"Omitted and explicit null home stay uninitialized");
        var explicitHome=fresh();explicitHome.home=FormalHomeProgress.Empty(catalog.contentVersion);explicitHome.Validate();
        check(decode(encode(explicitHome)).home.furnitureInstances.Length==0,"Explicit empty home is distinct from omitted state");
        reject(()=>{var bad=decode(encode(explicitHome));bad.home.furnitureInstances=null;bad.Validate();},"Missing home list rejected");
        reject(()=>{var bad=decode(encode(explicitHome));bad.home.loverHeroineIds=new[]{combat.FormationIds[0]};bad.home.ValidateContent(catalog,bad);},"Lover state needs completed explicit event");
        reject(()=>{var bad=decode(encode(explicitHome));bad.home.readEventIds=new[]{catalog.events[0].id};bad.Validate();},"Read event without unlock rejected");
        reject(()=>{var bad=decode(encode(explicitHome));bad.home.claimedRewardIds=new[]{"unknown.reward"};bad.home.ValidateContent(catalog,bad);},"First reward claim requires an explicit completed definition");
        var journal=new FormalCampaignJournal(old,encode,decode);string original=encode(journal.Snapshot);int builds=0;
        Func<FormalHomeProgress,FormalHomeProgress> build=p=>{builds++;p.affections=new[]{new HomeAffection{heroineId=combat.FormationIds[0],value=7}};return p;};
        check(journal.CommitHome(new FormalHomeRequest("home.test","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),catalog,build,s=>false)==GrowthCommitResult.SaveFailed,"Failed home save reported");
        check(journal.HasPending && encode(journal.Snapshot)==original,"Failed home save preserves world growth and all old arrays");
        reject(()=>journal.CommitWorld(journal.Snapshot.world,s=>true),"Home pending excludes world edits");
        reject(()=>journal.CommitGrowth(journal.Snapshot.growth,s=>true),"Home pending excludes growth edits");
        reject(()=>journal.CommitHome(new FormalHomeRequest("different","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),catalog,build,s=>true),"Other home transaction cannot replace pending");
        reject(()=>journal.CommitHome(new FormalHomeRequest("home.test","homeInit",0,catalog.contentVersion,"affection.slayer.8"),catalog,build,s=>true),"Same pending ID with different operation key rejected");
        bool io=false;try{journal.CommitHome(new FormalHomeRequest("home.test","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),null,null,s=>throw new IOException("simulated"));}catch(IOException){io=true;}check(io && journal.HasPending,"Home IO failure retains pending");
        check(journal.CommitHome(new FormalHomeRequest("home.test","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),null,null,s=>{s.home.affections[0].value=999;return false;})==GrowthCommitResult.SaveFailed,"Writer mutation cannot alter frozen home candidate");
        check(journal.CommitHome(new FormalHomeRequest("home.test","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),null,null,s=>true)==GrowthCommitResult.Committed && builds==1 && !journal.HasPending,"Home retry commits original candidate once");
        var result=journal.Snapshot;check(result.home.affections[0].value==7 && result.revision==1 && result.growth.stones==321 && encode(new FormalCampaignSave{world=result.world,growth=result.growth})==oldText,"Home commit preserves old formal payload and increments revision once");
        check(journal.CommitHome(new FormalHomeRequest("home.test","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),null,null,s=>throw new Exception("duplicate write"))==GrowthCommitResult.AlreadyCommitted,"Home repeated receipt does not save again");
        reject(()=>journal.CommitHome(new FormalHomeRequest("home.test","garden",1,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),catalog,build,s=>true),"Receipt kind cannot be reused");
        reject(()=>journal.CommitHome(new FormalHomeRequest("home.test","homeInit",1,catalog.contentVersion,"affection.slayer.8"),catalog,build,s=>true),"Committed ID cannot be reused for a different operation key");
        reject(()=>journal.CommitHome(new FormalHomeRequest("home.stale","garden",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),catalog,build,s=>true),"Stale home revision rejected");
        reject(()=>journal.CommitHome(new FormalHomeRequest("home.reentrant","garden",1,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),catalog,p=>{journal.CommitWorld(journal.Snapshot.world,s=>true);return p;},s=>true),"Reentrant callback rejected");
        check(!journal.HasPending && journal.Snapshot.revision==1,"Rejected home builder leaves no pending");
        reject(()=>journal.CommitHome(new FormalHomeRequest("home.remove","garden",1,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),catalog,p=>{p.receipts=Array.Empty<HomeReceipt>();return p;},s=>true),"Home receipt removal rejected");
        string path=Path.Combine(Path.GetTempPath(),"plan6-home-"+Guid.NewGuid().ToString("N")+".json");
        try{
            var store=new FormalCampaignStore(path,encode,decode,header);check(store.Save(old),"Initial actual file save");
            File.WriteAllText(path,withoutHome);check(store.Load(out var loaded)==FormalLoadResult.Loaded && loaded.home==null && loaded.growth.stones==321,"Old actual file without home loads");
            check(store.Save(result) && store.Load(out loaded)==FormalLoadResult.Loaded && loaded.home.affections[0].value==7,"Actual home write and reload");
            var future=withoutHome.Substring(0,withoutHome.Length-1)+",\"home\":{\"version\":2,\"contentVersion\":\"future\",\"affections\":\"different-shape\"}}";File.WriteAllText(path,future);
            check(store.Load(out _) == FormalLoadResult.Blocked && store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Future home shape blocks before payload decoding or backup");
            foreach(var futurePayload in new[]{"{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"world\":{\"version\":3,\"affections\":\"future-shape\"}}","{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"growth\":{\"version\":3,\"heroines\":\"future-shape\"}}","{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"engagement\":{\"version\":2,\"claimedDates\":\"future-shape\"}}"}){
                File.WriteAllText(path,futurePayload);check(store.Load(out _)==FormalLoadResult.Blocked && store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"All envelope payload headers stop future internal shapes before fallback");
            }
            File.WriteAllText(path,"broken");check(store.InspectRecovery().CanRestore,"Corrupt primary exposes existing recovery confirmation");
            var restoredSave=store.RestoreConfirmed(store.InspectRecovery(),out string preserved);check(restoredSave.home==null && File.ReadAllText(preserved)=="broken","Recovery preserves damaged bytes and old home absence");File.Delete(preserved);
        }finally{foreach(var suffix in new[]{"",".bak",".tmp",".write.lock"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
}
