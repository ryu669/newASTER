using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;
internal static class Plan10FormContinuityTests {
 public static void Run(Action<bool,string> check,CombatDefinitionCatalog c,ProductionStoryContent story,JsonSerializerOptions options){
  var home=ProductionStoryCatalog.Home(c,story);var collection=ProductionStoryCatalog.Collection(c,story);
  Func<FormalCampaignSave> fresh=()=>new FormalCampaignSave{growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=4321,stones=1234,heroines=c.HeroineIds.Select(id=>new FormalHeroineGrowth{heroineId=id,level=20}).ToArray()},world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),home=FormalHomeProgress.Empty(home.contentVersion),collection=new FormalCollectionLedger{contentVersion=collection.contentVersion}};
  Func<FormalCampaignSave,string> encode=s=>JsonSerializer.Serialize(s,options);Func<string,FormalCampaignSave> decode=s=>JsonSerializer.Deserialize<FormalCampaignSave>(s,options);
  foreach(var pair in new[]{new[]{"heroine.slayer","heroine.slayer-swim"},new[]{"heroine.arcane","heroine.arcane-academy"},new[]{"heroine.arcane-academy","heroine.arcane"}}){
   var save=fresh();save.growth.heroines=save.growth.heroines.Where(h=>h.heroineId!=pair[1]).ToArray();save.home.affections=new[]{new HomeAffection{heroineId=pair[0],value=20}};save.home.loverHeroineIds=new[]{pair[0]};save.home.readEventIds=new[]{pair[0]+".event.2"};save.home.unlockedEventIds=new[]{pair[0]+".event.2"};HomeConditions.Refresh(save,home);save.Validate();save.home.ValidateContent(home,save);
   var journal=new FormalCampaignJournal(save,encode,decode);var progress=new FormalProgression(save.growth,c.HeroineIds);var join=new GrowthRequest("join."+pair[1],pair[1],save.growth.revision,GrowthOperation.ReceiveHeroine);
   check(progress.Commit(join,g=>journal.CommitGrowth(g,s=>false,home))==GrowthCommitResult.SaveFailed && !journal.Snapshot.home.loverHeroineIds.Contains(pair[1]),"Failed alternate join keeps relationship unpublished: "+pair[1]);
   check(progress.Commit(join,g=>journal.CommitGrowth(g,s=>true,home))==GrowthCommitResult.Committed && journal.Snapshot.home.affections.Single(a=>a.heroineId==pair[1]).value==20 && journal.Snapshot.home.loverHeroineIds.Contains(pair[1]),"Durable join inherits existing relationship: "+pair[1]);
   check(progress.Commit(join,g=>throw new Exception("duplicate save"))==GrowthCommitResult.AlreadyCommitted && journal.Snapshot.growth.nectar==4321 && journal.Snapshot.growth.stones==1234,"Free join once without balance changes");
   var restored=decode(encode(journal.Snapshot));restored.Validate();restored.home.ValidateContent(home,restored);check(!restored.home.readEventIds.Contains(pair[1]+".event.2") && restored.growth.heroines.Single(h=>h.heroineId==pair[1]).SkillLevel(0)==1,"Form reading and growth stay independent after inherited relationship");
  }
  var roleSave=fresh();HomeConditions.Refresh(roleSave,home);roleSave.home.formationIds=new[]{"heroine.shangrila","heroine.slayer-swim","heroine.r","heroine.iconoclast","heroine.undermine"};var roles=new FormalCampaignJournal(roleSave,encode,decode);
  foreach(var operation in new[]{new HomeOperation("commander","heroine.slayer-swim"),new HomeOperation("sniper-support","heroine.shangrila","heroine.r")}){
   var request=new FormalHomeRequest("role."+operation.Kind,operation.Kind,roles.Snapshot.revision,home.contentVersion,operation.Key);string before=encode(roles.Snapshot);
   check(roles.CommitHomeOperation(request,home,operation,s=>false)==GrowthCommitResult.SaveFailed && encode(roles.Snapshot)==before,"Failed role save preserves complete campaign");
   check(roles.CommitHomeOperation(request,home,operation,s=>true)==GrowthCommitResult.Committed && roles.CommitHomeOperation(request,home,operation,s=>throw new Exception("duplicate role save"))==GrowthCommitResult.AlreadyCommitted,"Role retry saves exactly once");
  }
  var restoredRoles=decode(encode(roles.Snapshot));restoredRoles.Validate();restoredRoles.home.ValidateContent(home,restoredRoles);var deployed=restoredRoles.home.Deployment(restoredRoles.home.formationIds);check(deployed.CommanderId=="heroine.slayer-swim" && deployed.SniperSupports.Single().targetId=="heroine.r" && restoredRoles.growth.nectar==4321,"Commander and selected support survive save roundtrip without cost");
  var old=decode(encode(roleSave));old.home.commanderHeroineId=null;old.home.sniperSupports=null;old.Validate();old.home.ValidateContent(home,old);check(old.home.Deployment(c.FormationIds).CommanderId==null && old.home.Deployment(c.FormationIds).SniperSupports.Length==0,"Absent old-save role fields use automatic defaults");
  foreach(var invalid in new[]{new HomeOperation("commander","heroine.r"),new HomeOperation("sniper-support","heroine.r","heroine.iconoclast"),new HomeOperation("sniper-support","heroine.shangrila","heroine.shangrila")}){bool rejected=false;try{HomeRules.Apply(decode(encode(roleSave)),home,invalid);}catch(ArgumentException){rejected=true;}check(rejected,"Invalid role owner/job/self-target rejected");}
  check(restoredRoles.home.Deployment(c.FormationIds).CommanderId==null && restoredRoles.home.Deployment(c.FormationIds).SniperSupports.Length==0,"Removing deployed role owners falls back to auto without invalid deployment");
  Console.WriteLine("PLAN10_FORM_CONTINUITY_PASS alternate-first recruitment / shared relationships / independent reading / atomic roles / old saves");
 }
}
