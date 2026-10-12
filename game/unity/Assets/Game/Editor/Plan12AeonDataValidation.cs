using System;
using System.IO;
using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEditor;
using UnityEngine;

public static class Plan12AeonDataValidation
{
    [MenuItem("Tools/newASTER/Validate Aeon Development Data")]
    public static void Validate()
    {
        string folder=Path.Combine(Application.dataPath,"Game/ContentDrafts/Aeon");
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(File.ReadAllText(Path.Combine(folder,"combat.json")));
        var story=JsonUtility.FromJson<ProductionStoryContent>(File.ReadAllText(Path.Combine(folder,"story.json")));
        combat.Validate();story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var restoredCombat=JsonUtility.FromJson<CombatDefinitionCatalog>(JsonUtility.ToJson(combat));
        var restoredStory=JsonUtility.FromJson<ProductionStoryContent>(JsonUtility.ToJson(story));
        restoredCombat.Validate();restoredStory.Validate(restoredCombat.HeroineIds,WorldCatalog.ColossusIds.ToArray());
        var pages=story.chapters.Where(c=>c.ownerId=="heroine.aeon").SelectMany(c=>c.pages).ToArray();
        var restoredPages=restoredStory.chapters.Where(c=>c.ownerId=="heroine.aeon").SelectMany(c=>c.pages).ToArray();
        if(pages.Length!=30 || !pages.SequenceEqual(restoredPages))throw new Exception("Aeon chapter pages lost during Unity serialization.");
        ProductionStoryCatalog.Collection(restoredCombat,restoredStory).Validate();
        if(Resources.Load<TextAsset>("Combat/battle-plan11-7").text.Contains("heroine.aeon"))throw new Exception("Unaccepted Aeon content entered the live roster.");
        Debug.Log("PLAN12_AEON_UNITY_DATA_PASS / 30 pages / combat and story JsonUtility roundtrip / collection / live roster preserved; art and Player checks pending");
    }
}
