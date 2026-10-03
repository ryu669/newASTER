using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
public static class Plan6ExperienceTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
        var c=HomeExperienceFixture.Create(combat);var material=c.materials[0];var s=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},collection=new FormalCollectionLedger{materials=new[]{new CollectionMaterial{id=material.id,sourceColossusId=material.colossusId,amount=100}}}};
        var journal=new FormalCampaignJournal(s,encode,decode);
        Func<HomeOperation,FormalHomeRequest> request=op=>new FormalHomeRequest(Guid.NewGuid().ToString("N"),op.Kind=="equip"?"weapon":op.Kind=="remove"?"place":op.Kind=="use"?"occupant":op.Kind,journal.Snapshot.revision,c.contentVersion,op.Key);
        Action<HomeOperation> commit=op=>check(journal.CommitHomeOperation(request(op),c,op,n=>true)==GrowthCommitResult.Committed,"home operation "+op.Kind);
        Action<Action,string> reject=(a,label)=>{bool failed=false;try{a();}catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is OverflowException){failed=true;}check(failed,label);};
        foreach(var hero in combat.FormationIds){var nodes=c.weaponNodes.Where(n=>n.heroineId==hero).ToArray();reject(()=>commit(new HomeOperation("weapon",nodes[3].id)),"all parents required");commit(new HomeOperation("weapon",nodes[0].id));commit(new HomeOperation("weapon",nodes[1].id));commit(new HomeOperation("weapon",nodes[2].id));commit(new HomeOperation("weapon",nodes[3].id));commit(new HomeOperation("equip",nodes[3].id,hero));}
        check(HomeRules.Balance(journal.Snapshot,material.id)==40,"all five trees spend 60 materials");
        var same=new HomeOperation("weapon",c.weaponNodes[3].id);commit(same);check(HomeRules.Balance(journal.Snapshot,material.id)==40,"acquired node costs nothing");
        var before=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:journal.Snapshot.growth);
        var after=new PlayableBattle(1,new PlayableProgress(),combatDefinitions:combat,formalGrowth:journal.Snapshot.growth,homeProgress:journal.Snapshot.home,homeCatalog:c);
        check(after.State.Heroes[0].Attack>before.State.Heroes[0].Attack && after.SkillName(0,0).Contains("γ"),"next battle applies equipped ability and skill");check(after.State.Heroes[0].Speed==before.State.Heroes[0].Speed && after.ChainRate(0)==before.ChainRate(0),"equipment preserves speed and chain");
        var unequip=new HomeOperation("equip","unequip",combat.FormationIds[0]);var retry=request(unequip);string old=encode(journal.Snapshot);check(journal.CommitHomeOperation(retry,c,unequip,n=>false)==GrowthCommitResult.SaveFailed && old==encode(journal.Snapshot),"weapon save failure leaves whole snapshot unchanged");reject(()=>commit(new HomeOperation("talk",combat.FormationIds[0])),"pending excludes different operation");check(journal.CommitHomeOperation(retry,c,unequip,n=>true)==GrowthCommitResult.Committed,"frozen weapon retry");check(journal.CommitHomeOperation(retry,c,unequip,n=>false)==GrowthCommitResult.AlreadyCommitted,"same receipt idempotent");
        // Unlock test gardens through the existing world boundary, without granting production progress.
        var world=journal.Snapshot.world;world.unlockedGardenIds=c.gardens.Take(2).Select(g=>g.id).ToArray();check(journal.CommitWorld(world,n=>true),"fixture garden unlock");
        commit(new HomeOperation("craft",c.furniture[0].id,"instance.a"));commit(new HomeOperation("craft",c.furniture[1].id,"instance.b"));string garden=c.gardens[0].id;
        commit(new HomeOperation("place","instance.a",garden:garden,zone:"zone.ground",x:.3f,y:.6f));
        reject(()=>commit(new HomeOperation("place","instance.b",garden:garden,zone:"zone.ground",x:.3f,y:.6f)),"positive area overlap rejected");
        commit(new HomeOperation("place","instance.b",garden:garden,zone:"zone.ground",x:.428f,y:.6f));check(journal.Snapshot.home.furniturePlacements.Length==2,"edge contact permitted");
        reject(()=>commit(new HomeOperation("place","instance.b",garden:garden,zone:"zone.ground",x:0,y:.6f)),"whole footprint must be in bounds");reject(()=>new HomeOperation("place","instance.b",x:float.NaN),"nonfinite coordinate rejected");
        commit(new HomeOperation("occupant",combat.FormationIds[0],garden:garden,x:.3f,y:.6f));commit(new HomeOperation("occupant",combat.FormationIds[0],garden:c.gardens[1].id,x:.5f,y:.7f));check(journal.Snapshot.home.occupants.Length==1 && journal.Snapshot.home.occupants[0].gardenId==c.gardens[1].id,"garden move retains one heroine across all gardens");
        commit(new HomeOperation("occupant",combat.FormationIds[0],garden:garden,x:.5f,y:.7f));commit(new HomeOperation("use",combat.FormationIds[0],"instance.a"));check(journal.Snapshot.home.occupants[0].furnitureInstanceId==null,"missing SD action returns idle");
        commit(new HomeOperation("remove","instance.a"));check(journal.Snapshot.home.furnitureInstances.Length==2 && journal.Snapshot.home.furniturePlacements.Length==1,"remove retains owned individual");
        var restored=decode(encode(journal.Snapshot));restored.home.ValidateContent(c,restored);check(restored.home.furniturePlacements[0].x==.428f,"placement survives JSON with same decision");
        commit(new HomeOperation("talk",combat.FormationIds[0]));check(journal.Snapshot.home.affections[0].value==1 && journal.Snapshot.home.unlockedEventIds.Contains(c.events[0].id) && !journal.Snapshot.home.loverHeroineIds.Any(),"affection unlock distinct from reading and lover");
    }
}
