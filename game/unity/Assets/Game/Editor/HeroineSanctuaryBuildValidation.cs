using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using NewAster.Presentation;
using UnityEngine;
public static partial class PlayableBuild
{
    private static void ValidateHeroineSanctuary()
    {
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);var story=JsonUtility.FromJson<ProductionStoryContent>(Resources.Load<TextAsset>("Story/plan9-story-content").text);var home=ProductionStoryCatalog.Home(combat,story);
        var save=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",nectar=5000,heroines=combat.FormationIds.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()},home=FormalHomeProgress.Empty(home.contentVersion)};
        // Remove skill arrays by syntax rather than relying on indentation of JsonUtility.
        var legacy=System.Text.RegularExpressions.Regex.Replace(UnityFormalCampaignJson.Encode(save),",\\s*\\\"skillLevels\\\"\\s*:\\s*\\[[^\\]]*\\]","");
        var decoded=UnityFormalCampaignJson.Decode(legacy);decoded.Validate();Check(decoded.growth.heroines.All(h=>h.SkillLevel(0)==1 && h.SkillLevel(2)==1),"Unity legacy absent skills mean Lv1");
        var progression=new FormalProgression(save.growth,combat.FormationIds);var request=new GrowthRequest("unity.sanctuary.skill",combat.FormationIds[0],0,GrowthOperation.Skill,2,skillSlot:0);FormalGrowthSave disk=null;
        Check(progression.Commit(request,s=>false)==GrowthCommitResult.SaveFailed && progression.Snapshot.nectar==5000,"Unity skill upgrade failure leaves balance intact");
        Check(progression.Commit(request,s=>{disk=JsonUtility.FromJson<FormalGrowthSave>(JsonUtility.ToJson(s));disk.Validate();return true;})==GrowthCommitResult.Committed && disk.heroines[0].SkillLevel(0)==2 && disk.nectar==4940,"Unity skill candidate saves and roundtrips once");
        Check(new FormalProgression(disk,combat.FormationIds).Commit(request,s=>false)==GrowthCommitResult.AlreadyCommitted,"Unity skill receipt survives restart");
        foreach(string id in combat.FormationIds){Check(home.weaponNodes.Count(n=>n.heroineId==id)==13,"Unity heroine has thirteen stable weapon nodes");var g=new FormalHeroineGrowth{heroineId=id,skillLevels=new[]{7,7,7}};Check(HeroineIdentityCatalog.Traits(combat.Hero(id),g).All(t=>t.active),"Unity individual mastery trait is defined and active at allLv7");}
        save.home.weaponNodeIds=new[]{combat.FormationIds[0]+".weapon.root"};save.home.weaponLevels=new[]{new HomeWeaponLevel{nodeId=save.home.weaponNodeIds[0],level=7}};var copy=UnityFormalCampaignJson.Decode(UnityFormalCampaignJson.Encode(save));copy.Validate();Check(copy.home.WeaponLevel(save.home.weaponNodeIds[0])==7,"Unity weapon level survives unified save roundtrip");
        Debug.Log("HEROINE_SANCTUARY_BUILD_PASS skill-levels tree-nodes traits legacy-roundtrip");
        foreach(string id in combat.FormationIds)Check(Resources.Load<Texture2D>("Illustrations/"+id.Replace("heroine.","")+"-equipment-tree-v1")!=null,"Unity original botanical tree asset exists");
    }
}
