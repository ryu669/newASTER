using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class BookRedesignTests
{
    private static CampaignSaveV2 Claim(CampaignSaveV2 world,string id){world.claimedBattleIds=world.claimedBattleIds.Concat(new[]{id}).ToArray();return world;}
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat,CollectionCatalog catalog,HomeExperienceCatalog home)
    {
        foreach(string hero in combat.HeroineIds)foreach(string route in new[]{"alpha","beta","gamma"}){
            var previous=home.weaponNodes.Single(n=>n.id==hero+".weapon."+route);
            for(int tier=2;tier<=4;tier++){
                var next=home.weaponNodes.Single(n=>n.id==hero+".weapon."+route+".tier"+tier);var beforeEffects=WeaponGrowthRules.EffectKinds(previous);var afterEffects=WeaponGrowthRules.EffectKinds(next);
                check(beforeEffects.All(afterEffects.Contains) && afterEffects.Length>beforeEffects.Length,"Every advancing branch retains earlier effects and adds a new effect category: "+next.id);
                previous=next;
            }
        }
        var added=catalog.relics.Where(r=>r.id.StartsWith("relic.",StringComparison.Ordinal)).ToArray();
        check(added.Length==18 && added.Count(r=>r.jobStatPercent>0)==13 && added.Count(r=>r.jobId==null)==5,"Relics add thirteen job emblems plus five independent special items, without a job/type product");
        check(added.All(r=>new[]{r.jobStatPercent,r.attackPercent,r.defensePercent,r.speedPercent,r.turnEffect==null?0:1}.Count(n=>n>0)==1),"Every additional relic has exactly one independent effect category");
        foreach(var d in added.Where(r=>r.jobStatPercent>0)){check(FormalRelicRules.JobBonus(d,d.jobId)==30 && FormalRelicRules.JobBonus(d,"job.unknown")==0,"Job affinity only rewards its matching job");}
        var copy=catalog.Copy();check(copy.relics.Where(r=>r.id.StartsWith("relic.",StringComparison.Ordinal)).Select(r=>r.name+"|"+r.jobId+"|"+r.turnEffect+"|"+r.jobStatPercent+"|"+r.defensePercent+"|"+r.speedPercent).SequenceEqual(added.Select(r=>r.name+"|"+r.jobId+"|"+r.turnEffect+"|"+r.jobStatPercent+"|"+r.defensePercent+"|"+r.speedPercent)),"Session catalog copies preserve all new effects");
        var ramp=added.Single(r=>r.turnEffect=="ramp");var wane=added.Single(r=>r.turnEffect=="wane");
        check(FormalRelicRules.TurnBonus(ramp,1)==0 && FormalRelicRules.TurnBonus(ramp,2)==5 && FormalRelicRules.TurnBonus(ramp,9)==40 && FormalRelicRules.TurnBonus(ramp,int.MaxValue)==40,"Growing relic starts at zero and caps after eight elapsed turns");
        check(FormalRelicRules.TurnBonus(wane,1)==50 && FormalRelicRules.TurnBonus(wane,2)==40 && FormalRelicRules.TurnBonus(wane,6)==0 && FormalRelicRules.TurnBonus(wane,100)==0,"Fading relic never falls below zero");
        var growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=30}).ToArray()};
        foreach(var d in added){
            var inventory=new FormalCollectionLedger{contentVersion=catalog.contentVersion,relics=new[]{new CollectionRelic{id=d.id,contentVersion=catalog.contentVersion}},equipment=new[]{new CollectionEquipment{heroineId=combat.FormationIds[0],relicId=d.id}}};
            var baseline=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:growth);
            var b=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:growth,collectionGrowth:inventory,relicCatalog:catalog);
            check(b.ChainRate(0)==baseline.ChainRate(0),"Relic does not alter chain probability");
            check(b.State.Heroes[0].RelicAttackPercent==d.attackPercent+FormalRelicRules.JobBonus(d,combat.Hero(combat.FormationIds[0]).jobId)+FormalRelicRules.TurnBonus(d,1),"Relic percentage contributions are added, without multiplying effect factors");
            if(d.defensePercent>0)check(b.State.Heroes[0].PhysicalDefense>baseline.State.Heroes[0].PhysicalDefense && b.State.Heroes[0].MagicDefense>baseline.State.Heroes[0].MagicDefense,"Defense item boosts both actual defenses");
            if(d.speedPercent>0)check(b.State.Heroes[0].Speed>baseline.State.Heroes[0].Speed,"Speed item affects actual action timing speed");
            if(d.turnEffect!=null){int first=b.State.Heroes[0].Attack,guard=0;while(b.Turn==1 && guard++<500)b.Pass();check(b.Turn==2 && (d.turnEffect=="ramp"?b.State.Heroes[0].Attack>first:b.State.Heroes[0].Attack<first),"Elapsed enemy turns change equipped relic attack in live combat");}
        }
        var options=new JsonSerializerOptions{IncludeFields=true};Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        Func<FormalCampaignSave> fresh=()=>new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=growth.Copy(),collection=new FormalCollectionLedger{contentVersion=catalog.contentVersion}};
        string enemy=catalog.owners.First(o=>o.kind=="colossus").id;
        Func<FormalCampaignJournal,int,bool,FormalBattleEndRequest> request=(journal,seed,hunt)=>new FormalBattleEndRequest(new BattleCollectionSession(catalog,"book.drop."+seed,enemy,50,journal.Snapshot.revision,combat.FormationIds,seed,ColossusCombatCatalog.Get(enemy).contentVersion,hunt).Finish(BattleEndReason.Victory,Array.Empty<string>(),Array.Empty<string>()),journal.Snapshot.revision);
        int normalDrops=0,huntDrops=0;
        for(int seed=0;seed<100;seed++)foreach(bool hunt in new[]{false,true}){
            var journal=new FormalCampaignJournal(fresh(),encode,decode);var r=request(journal,seed,hunt);journal.CommitBattleEnd(r,catalog,w=>Claim(w,r.Id),s=>true);
            var receipt=journal.Snapshot.collection.receipts.Single();check(receipt.relicDrawCount==(hunt?5:1) && receipt.battle.relicHunt==hunt,"Normal and hunt rewards retain distinct saved source and draw counts");
            if(hunt)huntDrops+=receipt.relicDrops.Length;else normalDrops+=receipt.relicDrops.Length;
        }
        check(normalDrops>0 && normalDrops<15 && huntDrops>100 && huntDrops<250,"Deterministic seed sample verifies low normal drops and richer hunt rewards");
        var retry=new FormalCampaignJournal(fresh(),encode,decode);var retryRequest=request(retry,119,true);string before=encode(retry.Snapshot);
        check(retry.CommitBattleEnd(retryRequest,catalog,w=>Claim(w,retryRequest.Id),s=>false)==GrowthCommitResult.SaveFailed && encode(retry.Snapshot)==before,"Failed hunt save publishes no inventory or world change");
        check(retry.CommitBattleEnd(retryRequest,catalog,w=>Claim(w,retryRequest.Id),s=>true)==GrowthCommitResult.Committed,"Hunt retry commits the pending candidate");
        var direct=new FormalCampaignJournal(fresh(),encode,decode);direct.CommitBattleEnd(request(direct,119,true),catalog,w=>Claim(w,"book.drop.119"),s=>true);
        check(encode(retry.Snapshot)==encode(direct.Snapshot),"Hunt retry preserves the original rolls");
        var loaded=new FormalCampaignJournal(decode(encode(retry.Snapshot)),encode,decode);
        check(loaded.CommitBattleEnd(retryRequest,catalog,w=>Claim(w,retryRequest.Id),s=>throw new Exception("duplicate write"))==GrowthCommitResult.AlreadyCommitted,"Restart cannot duplicate a recorded hunt reward");
        check((int)BookBookmark.NewWorld==4 && (int)BookBookmark.PossibleWorlds==5,"New page bookmarks preserve existing enum values");
    }
}
