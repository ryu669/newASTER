using System;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
public static partial class PlayableBuild
{
    private static void ValidateCombatExpansion()
    {
        foreach(string frame in new[]{"arrival","doors","disembark"}){
            var art=Resources.Load<Texture2D>("KinderTrain/"+frame);
            Check(art!=null && art.width>=1024 && art.height>=512,"Imported train keyframe is a runtime Texture2D: "+frame);
        }
        var catalog=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);
        catalog=JsonUtility.FromJson<CombatDefinitionCatalog>(JsonUtility.ToJson(catalog));catalog.Validate();
        Check(catalog.optionalResourceBoost && catalog.skills.All(s=>(s.attributes??Array.Empty<string>()).Length<=5),"Unity JSON preserves optional resource and attribute lists");
        foreach(var id in WorldCatalog.ColossusIds){var enemy=ProductionEconomyCatalog.Enemy(id);var copy=JsonUtility.FromJson<ColossusCombatDef>(JsonUtility.ToJson(enemy));copy.Validate();Check(copy.attributeResistances.Length==12 && copy.statusResistances.Length==10,"Unity JSON preserves beast weakness/resistance and ten statuses: "+id);}
        var battle=new PlayableBattle(1,new PlayableProgress(),341,combatDefinitions:catalog);
        int actor=battle.AvailableHero;battle.State.Heroes[actor].SpendResource(battle.State.Heroes[actor].JobResource);
        Check(battle.SkillResourceCost(actor,0)==0 && battle.Act(actor,0,"body"),"Unity current skill executes with no resource");
        var v2=new PlayableBattle(1,new PlayableProgress(),341,combatDefinitions:catalog,useJobRulesV2:true);
        Check(v2.UsesJobRulesV2 && v2.JobState(v2.AvailableHero)!=null,"Unity formal catalog binds v2 job state");
        long clock=v2.Clock;actor=v2.AvailableHero;int resource=v2.State.Heroes[actor].JobResource;
        v2.Pass();Check(v2.NextAt(actor)==clock+SkillTimingDefinition.Delay(v2.State.Heroes[actor].Speed,25),"Unity v2 PASS schedules WT 25");
        Check(v2.BattleTurn==v2.Clock/100,"Unity v2 Battle Turn uses logical clock");
    }
}
