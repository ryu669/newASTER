using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
public static class FormalKinderTests
{
    public static void Run(Action<bool,string> check,FormalKinderBanner banner)
    {
        string[] ids=banner.heroineIds;banner.Validate(ids);
        Func<FormalGrowthSave> fresh=()=>new FormalGrowthSave {saveId="kinder.test",stones=30000,kinderPoints=200,heroines=ids.Select(id=>new FormalHeroineGrowth {heroineId=id}).ToArray()};
        Action<Action,string> reject=(action,label)=>{bool failed=false;try{action();}catch(ArgumentException){failed=true;}catch(InvalidOperationException){failed=true;}catch(OverflowException){failed=true;}check(failed,label);};
        var state=new FormalProgression(fresh(),ids);var request=new KinderRequest("kinder.ten",0,KinderOperation.StoneDraw,10);int calls=0;
        state.PreviewKinder(request,banner);check(state.Snapshot.revision==0 && calls==0,"Preview does not consume or roll");
        Func<int,int> rng=max=>{calls++;return max==10000?299:4;};
        check(state.CommitKinder(request,banner,rng,s=>false)==GrowthCommitResult.SaveFailed,"Gacha save failure remains pending");
        check(calls==20 && state.Snapshot.stones==30000 && state.Snapshot.kinderPoints==200 && state.Snapshot.heroines[4].fragments==0,"Failed ten draw is fully atomic");
        reject(()=>state.Commit(new GrowthRequest("growth.blocked",ids[0],0,GrowthOperation.Level,2),s=>true),"Growth blocked by pending gacha");
        check(state.CommitKinder(request,banner,max=>throw new Exception("Must not reroll"),s=>true)==GrowthCommitResult.Committed,"Retry keeps all ten outcomes");
        check(state.Snapshot.stones==27000 && state.Snapshot.kinderPoints==210 && state.Snapshot.totalKinderDraws==10 && state.Snapshot.heroines[4].fragments==1000,"Ten draw charges and all ten duplicate rewards commit together");
        var receipt=state.KinderReceipt(request.Id);check(receipt.kinderOutcomes.Length==10 && receipt.kinderOutcomes.All(x=>x.grantKind=="fragments"),"Receipt stores exact conversion results");
        receipt.kinderOutcomes[0].heroineId="tampered";check(state.KinderReceipt(request.Id).kinderOutcomes[0].heroineId==ids[4],"Receipt outcomes detached");
        check(state.CommitKinder(request,null,null,s=>throw new Exception("Must not save twice"))==GrowthCommitResult.AlreadyCommitted,"Committed request idempotent before reroll");
        reject(()=>state.CommitKinder(new KinderRequest(request.Id,0,KinderOperation.StoneDraw),banner,rng,s=>true),"Same ID different count rejected");
        var exchange=new KinderRequest("kinder.exchange",1,KinderOperation.Exchange,heroineId:ids[1]);
        state.CommitKinder(exchange,banner,null,s=>true);
        check(state.Snapshot.kinderPoints==110 && state.Snapshot.tickets.Single().count==1 && state.Snapshot.heroines[1].fragments==0,"Exchange consumes points and creates only a ticket");
        var ticket=new KinderRequest("kinder.ticket",2,KinderOperation.TicketDraw,heroineId:ids[1]);
        state.CommitKinder(ticket,banner,max=>throw new Exception("Ticket must not roll"),s=>true);
        check(state.Snapshot.tickets.Single().count==0 && state.Snapshot.heroines[1].fragments==100 && state.Snapshot.stones==27000 && state.Snapshot.kinderPoints==110,"Ticket guarantees selected hero, no stones or points");
        check(state.KinderReceipt(ticket.Id).kinderOutcomes.Single().heroineId==ids[1],"Ticket result persisted");
        reject(()=>state.PreviewKinder(new KinderRequest("kinder.empty-ticket",3,KinderOperation.TicketDraw,heroineId:ids[1]),banner),"Empty ticket rejected");
        reject(()=>state.PreviewKinder(new KinderRequest("kinder.stale",0,KinderOperation.StoneDraw),banner),"Stale revision rejected");
        var poor=fresh();poor.stones=299;poor.kinderPoints=99;var ps=new FormalProgression(poor,ids);
        reject(()=>ps.PreviewKinder(new KinderRequest("kinder.poor",0,KinderOperation.StoneDraw),banner),"299 stones cannot draw");
        reject(()=>ps.PreviewKinder(new KinderRequest("kinder.poor-exchange",0,KinderOperation.Exchange,heroineId:ids[0]),banner),"99 points cannot exchange");
        poor.stones=2999;ps=new FormalProgression(poor,ids);reject(()=>ps.PreviewKinder(new KinderRequest("kinder.poor-ten",0,KinderOperation.StoneDraw,10),banner),"2999 stones cannot partially ten draw");
        var intro=fresh();intro.stones=0;var introState=new FormalProgression(intro,ids);var grant=new KinderRequest("grant.plan4-kinder-introduction",0,KinderOperation.IntroGrant);
        introState.CommitKinder(grant,banner,null,s=>true);check(introState.Snapshot.stones==3000 && introState.Snapshot.totalKinderDraws==0,"One-time trial intro grant is not a draw");
        introState.CommitKinder(grant,banner,null,s=>true);check(introState.Snapshot.stones==3000,"Intro grant cannot double award");
        reject(()=>introState.PreviewKinder(new KinderRequest("grant.arbitrary",1,KinderOperation.IntroGrant),banner),"Arbitrary intro reward IDs rejected");
        var at300= new FormalProgression(fresh(),ids);at300.CommitKinder(new KinderRequest("kinder.boundary",0,KinderOperation.StoneDraw),banner,max=>max==10000?300:0,s=>true);
        check(at300.Snapshot.nectar==200 && at300.KinderReceipt("kinder.boundary").kinderOutcomes[0].kind=="nectar","300 boundary is material, not heroine");
        var crystal=new FormalProgression(fresh(),ids);crystal.CommitKinder(new KinderRequest("kinder.crystal",0,KinderOperation.StoneDraw),banner,max=>max==10000?9999:9000,s=>true);
        check(crystal.Snapshot.awakeningCrystals==2,"Conditional material weights resolve crystal");
        var invalid=new FormalProgression(fresh(),ids);reject(()=>invalid.CommitKinder(new KinderRequest("kinder.bad-roll",0,KinderOperation.StoneDraw),banner,max=>max,b=>true),"Out of range RNG rejected");
        check(!invalid.HasPending && invalid.Snapshot.stones==30000,"Invalid roll consumes nothing");
        var overflow=fresh();overflow.heroines[0].duplicateRank=5;var os=new FormalProgression(overflow,ids);
        os.CommitKinder(new KinderRequest("kinder.max",0,KinderOperation.StoneDraw),banner,max=>0,s=>true);
        check(os.Snapshot.overflow==100 && os.KinderReceipt("kinder.max").kinderOutcomes[0].grantKind=="overflow","Max duplicate converts to common material");
        var unowned=fresh();unowned.heroines=Array.Empty<FormalHeroineGrowth>();var us=new FormalProgression(unowned,ids);
        us.CommitKinder(new KinderRequest("kinder.first",0,KinderOperation.StoneDraw),banner,max=>0,s=>true);
        check(us.Snapshot.heroines.Single().fragments==0 && us.KinderReceipt("kinder.first").kinderOutcomes[0].grantKind=="owned","First acquisition grants ownership only");
        var clone=banner.Copy();var isolation=new FormalProgression(fresh(),ids);
        isolation.CommitKinder(new KinderRequest("kinder.isolation",0,KinderOperation.StoneDraw),clone,max=>{clone.heroineIds[0]="invalid";return 0;},s=>true);
        check(isolation.Snapshot.heroines[0].fragments==100,"In-progress draw isolates definition mutations");
        var legacy=fresh();legacy.version=1;legacy.stones=0;legacy.kinderPoints=0;legacy.totalKinderDraws=0;legacy.tickets=null;legacy.UpgradeFormalV1();legacy.Validate();
        check(legacy.version==2 && legacy.heroines.Length==5 && legacy.stones==0,"Formal V1 upgrade preserves ownership without economy compensation");
        var options=new JsonSerializerOptions {IncludeFields=true};var restored=JsonSerializer.Deserialize<FormalGrowthSave>(JsonSerializer.Serialize(state.Snapshot,options),options);restored.Validate();
        check(new FormalProgression(restored,ids).CommitKinder(ticket,null,null,s=>false)==GrowthCommitResult.AlreadyCommitted,"Gacha receipt roundtrip prevents duplicate after restart");
        var growthPending=new FormalProgression(new FormalGrowthSave {saveId="mixed.test",nectar=100,stones=300,heroines=fresh().heroines},ids);
        growthPending.Commit(new GrowthRequest("growth.pending",ids[0],0,GrowthOperation.Level,2),s=>false);
        reject(()=>growthPending.CommitKinder(new KinderRequest("kinder.blocked",0,KinderOperation.StoneDraw),banner,rng,s=>true),"Gacha blocked by pending growth");
        for(int i=0;i<100;i++){int value=KinderRandom.NextBelow(5);check(value>=0&&value<5,"Unbiased generator range");}
    }
}
