using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using NewAster.Presentation;
using UnityEngine;

public static partial class PlayableBuild
{
    private static void ValidatePlan6()
    {
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);
        var catalog=HomeExperienceFixture.Create(combat);
        var book=new BookNavigationState(catalog.subjects.Select(s=>new BookOrderedSubject(s.bookmarkId=="colossi"?BookBookmark.Colossi:s.bookmarkId=="heroines"?BookBookmark.Heroines:s.bookmarkId=="gardens"?BookBookmark.Gardens:BookBookmark.Stories,s.subjectId,s.pageOrder)));
        Check(book.SubjectCount==15 && !book.CanTurnPrevious,"Unity book explicit first boundary");
        Check(book.RequestTurn(1) && !book.RequestTurn(1) && book.SubjectIndex==1,"Unity book transition suppresses duplicate input");
        Check(book.CompleteTransition() && !book.CompleteTransition(),"Unity book skip sync once");
        for(int i=0;i<30;i++)book.TurnPage(1);Check(book.SubjectIndex==14 && !book.CanTurnNext,"Unity book last boundary without wrap");
        book.ChangeBookmark(BookBookmark.Stories);Check(book.BeginReading("chapter.fixture",2) && book.Reading.Move(1) && book.SubjectId==book.Reading.OwnerId,"Unity text page preserves story owner");
        book.Close();book.Reenter();Check(book.SubjectIndex==0 && book.Bookmark==BookBookmark.Colossi,"Unity book closes and reenters independently of progress");
        var emptyBook=new BookNavigationState(Array.Empty<BookOrderedSubject>());Check(emptyBook.SubjectId==null && !emptyBook.CanFlip,"Unity empty book category");
        var pack=JsonUtility.FromJson<HomeExperienceCatalog>(JsonUtility.ToJson(catalog));pack.Validate();
        Check(pack.weaponNodes.Length==20 && pack.events.Length==25 && pack.scripts[0].commands[2].textId=="text.fixture.line","Unity complete home definition pack roundtrip");
        Func<FormalCampaignSave,string> encode=NewAster.Presentation.UnityFormalCampaignJson.Encode;
        Func<string,FormalCampaignSave> decode=UnityFormalCampaignJson.Decode;
        var old=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",stones=321,heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()}};
        string oldJson="{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"revision\":0,\"world\":"+JsonUtility.ToJson(old.world)+",\"growth\":"+JsonUtility.ToJson(old.growth)+"}";
        var missing=decode(oldJson);missing.Validate();Check(missing.home==null && UnityFormalCampaignJson.DecodeHeader(oldJson).home==null,"Unity absent home does not materialize progress");
        var encodedOld=encode(missing);Check(FormalCampaignJsonShape.RootMemberIsNull(encodedOld,"home") && decode(encodedOld).home==null,"Unity writer preserves absent inline home instead of manufacturing a default object");
        string nullJson=oldJson.Substring(0,oldJson.Length-1)+",\"home\":null}";
        var nullHome=decode(nullJson);nullHome.Validate();Check(nullHome.home==null && UnityFormalCampaignJson.DecodeHeader(nullJson).home==null,"Unity explicit null home remains uninitialized");
        old.home=FormalHomeProgress.Empty(catalog.contentVersion);
        var empty=decode(encode(old));empty.home.ValidateContent(pack,empty);Check(empty.home!=null && empty.home.occupants.Length==0 && empty.growth.stones==321,"Unity explicit empty home differs from missing state");
        var additive=decode(JsonUtility.ToJson(old).Replace(",\"weaponEquipment\":[]",""));additive.home.ValidateContent(pack,additive);Check(additive.home.weaponEquipment.Length==0,"Unity old home without equipment field defaults empty");
        var journal=new FormalCampaignJournal(missing,encode,decode);string original=encode(journal.Snapshot);int builds=0;
        Func<FormalHomeProgress,FormalHomeProgress> build=p=>{builds++;p.affections=new[]{new HomeAffection{heroineId=combat.FormationIds[0],value=7}};return p;};
        Check(journal.CommitHome(new FormalHomeRequest("unity.home","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),pack,build,s=>false)==GrowthCommitResult.SaveFailed && journal.HasPending && encode(journal.Snapshot)==original,"Unity home transaction failure leaves original save untouched");
        Check(journal.CommitHome(new FormalHomeRequest("unity.home","homeInit",0,HomeExperienceCatalog.FixtureVersion,"affection.slayer.7"),null,null,s=>{decode(encode(s)).Validate();return true;})==GrowthCommitResult.Committed && builds==1,"Unity home retry does not rebuild");
        var final=decode(encode(journal.Snapshot));final.home.ValidateContent(pack,final);Check(final.home.affections[0].value==7 && final.home.receipts.Length==1 && final.revision==1 && final.growth.stones==321,"Unity nested home state and receipt persist");
        var operation=new HomeOperation("weapon",pack.weaponNodes[0].id);var homeRequest=new FormalHomeRequest("unity.weapon","weapon",journal.Snapshot.revision,pack.contentVersion,operation.Key);
        Check(journal.CommitHomeOperation(homeRequest,pack,operation,n=>true)==GrowthCommitResult.Committed,"Unity weapon node acquisition");
        var equip=new HomeOperation("equip",pack.weaponNodes[0].id,combat.FormationIds[0]);Check(journal.CommitHomeOperation(new FormalHomeRequest("unity.equip","weapon",journal.Snapshot.revision,pack.contentVersion,equip.Key),pack,equip,n=>true)==GrowthCommitResult.Committed,"Unity separate equipment field");
        var roundtrip=decode(encode(journal.Snapshot));Check(roundtrip.home.weaponEquipment.Length==1 && roundtrip.home.weaponEquipment[0].nodeId==pack.weaponNodes[0].id,"Unity equipment survives additive JSON");
        var scene=new AdvSession(pack,pack.events[0].sceneId,pack.events[0].id,false,roundtrip.home.readLineKeys);scene.Tick(.3);scene.Advance();Check(scene.FullyVisible && scene.NewlyRead.Count==0,"Unity first input reveals without reading");scene.Advance();Check(scene.CgId!=null && scene.HideActors && scene.NewlyRead.Count==1,"Unity interpreter applies CG after advanced line");scene.Advance();scene.Advance();Check(scene.CgId==null && scene.Actors.Count==1,"Unity CG restores and actor update is unique");scene.Pause();string visible=scene.VisibleText;scene.Tick(1);Check(scene.VisibleText==visible && !scene.Auto,"Unity ADV pause freezes timers");
        string path=Path.Combine(Application.temporaryCachePath,"plan6-unity-"+Guid.NewGuid().ToString("N")+".json");
        try{
            File.WriteAllText(path,oldJson);var store=new FormalCampaignStore(path,encode,decode,UnityFormalCampaignJson.DecodeHeader);
            Check(store.Load(out var loaded)==FormalLoadResult.Loaded && loaded.home==null,"Unity actual old file loads without home");
            Check(store.Save(final) && store.Load(out loaded)==FormalLoadResult.Loaded && loaded.home.receipts.Length==1,"Unity actual file home write reload");
            string future=oldJson.Substring(0,oldJson.Length-1)+",\"home\":{\"version\":2,\"contentVersion\":\"future\",\"furnitureInstances\":\"future-shape\"}}";
            File.WriteAllText(path,future);Check(store.Load(out _)==FormalLoadResult.Blocked && store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Unity future home shape prevents old backup fallback");
            Check(File.ReadAllText(path)==future,"Unity future file retained without rewrite");
            foreach(var futurePayload in new[]{"{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"world\":{\"version\":3,\"affections\":\"future-shape\"}}","{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"growth\":{\"version\":3,\"heroines\":\"future-shape\"}}","{\"version\":1,\"saveId\":\"newaster.formal-campaign\",\"engagement\":{\"version\":2,\"claimedDates\":\"future-shape\"}}"}){
                File.WriteAllText(path,futurePayload);Check(store.Load(out _)==FormalLoadResult.Blocked && store.InspectRecovery().Status==FormalRecoveryStatus.Unsupported,"Unity all envelope future payload headers prevent backup fallback");
            }
        }finally{foreach(var suffix in new[]{"",".bak",".tmp",".write.lock"})if(File.Exists(path+suffix))File.Delete(path+suffix);}
    }
}
