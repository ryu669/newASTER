using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class CollectionContractTests
{
    public static void Run(Action<bool,string> check)
    {
        var roster=new[]{"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan"};
        Func<CollectionCatalog> fresh=()=>CollectionContractFixture.Create(roster);
        var options=new JsonSerializerOptions {IncludeFields=true};
        Func<CollectionCatalog,CollectionCatalog> json=c=>JsonSerializer.Deserialize<CollectionCatalog>(JsonSerializer.Serialize(c,options),options);
        Action<Action,string> reject=(f,name)=>{bool failed=false;try{f();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException){failed=true;}check(failed,name);};
        var c=fresh();json(c).Validate();check(c.poems.Length==450 && c.chapters.Length==60,"15x24 and 5x18 poems, three chapters each");
        check(c.chapters.All(x=>x.textId==null) && c.status=="fixture","Fixtures never provide authored story text");
        Action<Action<CollectionCatalog>,string> bad=(mutate,name)=>{var d=fresh();mutate(d);reject(d.Validate,name);};
        bad(d=>d.schemaVersion=2,"Unknown catalog schema blocked");
        bad(d=>d.contentVersion="future","Unknown content blocked");
        bad(d=>d.status="formal","Fixture cannot claim formal status");
        bad(d=>d.owners[0].poemIds=d.owners[0].poemIds.Take(23).ToArray(),"Missing colossus poem blocked");
        bad(d=>d.owners[15].poemIds=d.owners[15].poemIds.Take(17).ToArray(),"Missing heroine poem blocked");
        bad(d=>d.poems[0].id=d.poems[1].id,"Duplicate global ID blocked");
        bad(d=>d.poems[0].ownerId=roster[0],"Wrong poem owner blocked");
        bad(d=>d.chapters[0].poemIds=d.chapters[1].poemIds,"Cross chapter membership blocked");
        bad(d=>d.chapters[0].textId="fixture prose","Unapproved fixture prose blocked");
        bad(d=>d.owners[0].previousOwnerId=d.owners[1].id,"Cyclic prerequisites blocked");
        bad(d=>d.owners[2].previousOwnerId=d.owners[0].id,"Branching unlock chain blocked");
        bad(d=>d.owners[0].previousOwnerId="unknown","Unknown prerequisite blocked");
        bad(d=>d.owners[0].environmentIds=new[]{"unknown"},"Missing environment blocked");
        bad(d=>d.resources[0].ownerId=roster[0],"Material ownership blocked");
        bad(d=>d.relics[0].maxLevel=121,"Relic level limit fixed");
        bad(d=>d.relics[0].abilityId="chain-rate","Unapproved ability blocked");
        bad(d=>d.rewardBands[0].minLevel=2,"Missing reward level blocked");
        bad(d=>d.rewardBands[0].relicIds=new[]{"unknown"},"Unknown relic pool blocked");
        bad(d=>d.links=new[]{new CollectionLinkDef {sourcePoemId=d.poems[0].id,targetPoemId=d.poems[1].id}},"Enemy to enemy correspondence blocked");
        bad(d=>d.links=new[]{new CollectionLinkDef {sourcePoemId="unknown",targetPoemId=d.owners[15].poemIds[0]}},"Missing source poem blocked");
        var source=c.owners[0].poemIds;var target=c.owners[15].poemIds;
        c.links=Enumerable.Range(0,18).Select(i=>new CollectionLinkDef {id=roster[0]+".test-link."+i,ownerId=roster[0],sourcePoemId=source[i],targetPoemId=target[i]}).ToArray();c.Validate();
        foreach(var reason in new[]{BattleEndReason.Victory,BattleEndReason.Defeat,BattleEndReason.Retreat}){
            var party=(string[])roster.Clone();
            var session=new BattleCollectionSession(c,"battle."+reason,c.owners[0].id,45,12,party);
            party[0]="changed";var detached=session.Snapshot;detached.formationIds[0]="changed";
            check(session.Snapshot.formationIds[0]==roster[0],"Starting party detached from external mutation");
            for(int i=0;i<24;i++)check(session.RecordCompletedSinging(source[i]),"Completed singing accepted without collection cap");
            check(!session.RecordCompletedSinging(source[0]),"Repeat singing is a set");
            var r=session.Finish(reason,source,Array.Empty<string>());
            check(r.acquiredPoemIds.Length==18 && r.acquiredPoemIds.All(target.Contains),"Previously owned enemy poems still collect corresponding heroine poems");
            check(r.unlockedChapterIds.Length==6,"All 8-poem and 6-poem chapters unlock for every end reason");
            r.acquiredPoemIds[0]="mutated";r.battle.formationIds[0]="mutated";
            check(session.Finish(reason,Array.Empty<string>(),Array.Empty<string>()).acquiredPoemIds.Length==18 && session.Snapshot.formationIds[0]==roster[0],"Frozen end result detached and retry stable");
            reject(()=>session.Finish(reason==BattleEndReason.Victory?BattleEndReason.Defeat:BattleEndReason.Victory,source,Array.Empty<string>()),"Same battle cannot change end reason");
            reject(()=>session.RecordCompletedSinging(source[0]),"No singing after battle termination");
        }
        var noSong=new BattleCollectionSession(c,"silent",c.owners[0].id,1,0,roster);
        check(noSong.Finish(BattleEndReason.Retreat,Array.Empty<string>(),Array.Empty<string>()).acquiredPoemIds.Length==0,"Incomplete or absent singing grants nothing");
        reject(()=>new BattleCollectionSession(c,"bad",c.owners[0].id,51,0,roster),"Level 51 blocked");
        reject(()=>new BattleCollectionSession(c,"bad",c.owners[0].id,1,0,roster.Take(4)),"Missing party member blocked");
        reject(()=>new BattleCollectionSession(c,"bad",c.owners[0].id,1,0,roster.Select(x=>"unknown")),"Unknown party blocked");
        var unknown=new BattleCollectionSession(c,"other",c.owners[0].id,1,0,roster);
        reject(()=>unknown.RecordCompletedSinging(c.owners[1].poemIds[0]),"Another enemy cannot sing current enemy poems");
        var mutable=fresh();var frozen=new BattleCollectionSession(mutable,"frozen",mutable.owners[0].id,1,0,roster);
        var song=mutable.owners[0].poemIds[0];mutable.poems[0].ownerId="mutated";
        check(frozen.RecordCompletedSinging(song),"Definitions frozen for active battle");
        for(int i=0;i<8;i++){
            var s=new BattleCollectionSession(c,"boundary."+i,c.owners[0].id,1,0,roster);
            for(int j=0;j<i;j++)s.RecordCompletedSinging(source[j]);
            var r=s.Finish(BattleEndReason.Defeat,Array.Empty<string>(),Array.Empty<string>());
            check(r.unlockedChapterIds.Contains(c.owners[15].chapterIds[0])==(i>=6),"Heroine chapter six poem boundary");
            check(!r.unlockedChapterIds.Contains(c.owners[0].chapterIds[0]),"Enemy chapter not unlocked before eight poems");
        }
        var envelope=new FormalCampaignSave {world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave {saveId="newaster.formal-growth",stones=765,nectar=432,receipts=new[]{new GrowthReceipt {transactionId="legacy",signature="legacy"}}}};
        var original=JsonSerializer.Serialize(envelope,options);
        var roundtrip=JsonSerializer.Deserialize<FormalCampaignSave>(original,options);roundtrip.Validate();
        check(roundtrip.collection==null && roundtrip.growth.stones==765 && roundtrip.growth.nectar==432 && roundtrip.growth.receipts[0].transactionId=="legacy","Old formal envelopes preserve wallet and receipts without inventing collection history");
        roundtrip.collection=new FormalCollectionLedger {version=2};reject(roundtrip.Validate,"Unknown collection save schema blocked");
        roundtrip.collection=new FormalCollectionLedger {receipts=new[]{new CollectionReceipt {battle=new BattleCollectionSession(c,"unsaved",c.owners[0].id,1,0,roster).Snapshot,reason=BattleEndReason.Defeat,acquiredPoemIds=Array.Empty<string>(),unlockedChapterIds=Array.Empty<string>()}}};
        reject(roundtrip.Validate,"Collection receipt must be in durable world");
        roundtrip.world.claimedBattleIds=new[]{"unsaved"};roundtrip.growth.receipts=roundtrip.growth.receipts.Concat(new[]{new GrowthReceipt {transactionId="unsaved",signature=new FormalBattleEndRequest(roundtrip.collection.receipts[0],0).Signature}}).ToArray();roundtrip.Validate();
        var restored=JsonSerializer.Deserialize<FormalCampaignSave>(JsonSerializer.Serialize(roundtrip,options),options);restored.Validate();
        check(restored.collection.receipts[0].battle.level==1 && restored.growth.stones==765,"New collection metadata JSON roundtrip preserves formal wallets");
        restored.collection.receipts[0].battle.contentVersion="future";reject(restored.Validate,"Unknown saved content blocked");
    }
}