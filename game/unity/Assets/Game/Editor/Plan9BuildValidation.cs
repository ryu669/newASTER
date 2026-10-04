using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
public static partial class PlayableBuild
{
    private static void ValidatePlan9Story()
    {
        var asset=Resources.Load<TextAsset>("Story/plan9-story-content");
        Check(asset!=null,"Unity production narrative resource exists");
        var content=JsonUtility.FromJson<ProductionStoryContent>(asset.text);
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);
        content.Validate(combat.formation,WorldCatalog.ColossusIds.ToArray());
        var copy=JsonUtility.FromJson<ProductionStoryContent>(JsonUtility.ToJson(content));
        copy.Validate(combat.formation,WorldCatalog.ColossusIds.ToArray());
        var collection=ProductionStoryCatalog.Collection(combat,copy);var home=ProductionStoryCatalog.Home(combat,copy);
        var serializedHome=JsonUtility.FromJson<HomeExperienceCatalog>(JsonUtility.ToJson(home));serializedHome.Validate();
        var serializedCollection=JsonUtility.FromJson<CollectionCatalog>(JsonUtility.ToJson(collection));serializedCollection.Validate();
        Check(home.scripts.Length==85 && collection.links.Length==90,"Unity production catalogs connect all scenes and authored correspondences");
        var old=new FormalCampaignSave{world=new CampaignState(WorldCatalog.ColossusIds).CreateSave(),growth=new FormalGrowthSave{saveId="newaster.formal-growth",heroines=combat.formation.Select(id=>new FormalHeroineGrowth{heroineId=id}).ToArray()}};
        old.world.unlockedStoryIds=new[]{content.chapters[0].id};old.world.readStoryIds=old.world.unlockedStoryIds.ToArray();
        var migrated=ProductionStoryMigration.Prepare(old,combat,copy,s=>NewAster.Presentation.UnityFormalCampaignJson.Decode(NewAster.Presentation.UnityFormalCampaignJson.Encode(s)));
        var saved=NewAster.Presentation.UnityFormalCampaignJson.Decode(NewAster.Presentation.UnityFormalCampaignJson.Encode(migrated));saved.Validate();saved.home.ValidateContent(serializedHome,saved);
        Check(old.world.readStoryIds.Length==1 && saved.world.readStoryIds.Length==0 && saved.previousNarrative.readStoryIds.Length==1 && saved.previousNarrative.homeVersion=="none","Unity migration clone preserves prior read history without granting authored read completion");
        Check(copy.chapters.Length==60 && copy.chapters.Sum(c=>c.poems.Length)==450 && copy.events.Length==25,"Unity preserves the complete production narrative through JSON roundtrip");
        foreach(string path in content.chapters.Select(c=>c.backgroundResourcePath).Concat(content.events.Select(e=>e.backgroundResourcePath)).Concat(content.events.Select(e=>e.cgResourcePath)).Distinct())
            Check(Resources.Load<Texture2D>(path)!=null,"Unity production story background and CG binding exists");
        Debug.Log("PLAN9_STORY_SOURCE_PASS full authored resource and catalog adapter; gameplay bootstrap connection still pending");
    }
    private static void ValidatePlan9Colossi()
    {
        foreach(string id in ColossusCombatCatalog.AuthoredIds){
            var definition=JsonUtility.FromJson<ColossusCombatDef>(JsonUtility.ToJson(ColossusCombatCatalog.Get(id)));definition.Validate();
            Check(definition.id==id,"Unity authored enemy retains ID through serialization");
            var source=Resources.Load<TextAsset>(ColossusCombatCatalog.IllustrationResource(id));Check(source!=null,"Unity authored enemy owns an art manifest");
            var art=JsonUtility.FromJson<BattleIllustrationManifest>(source.text);art.Validate();
            Check(art.parts.Select(p=>p.partId).SequenceEqual(definition.parts.Select(p=>p.id)),"Unity enemy art and combat part IDs match in order");
            var body=Resources.Load<Texture2D>(art.bodyResourcePath);Check(body!=null,"Unity enemy body texture exists");
            foreach(var part in art.parts){var layer=Resources.Load<Texture2D>(part.resourcePath);Check(layer!=null && layer.width==body.width && layer.height==body.height,"Unity independent enemy part shares native canvas");}
            Check(Resources.Load<Texture2D>(art.enemyMajorResourcePath)!=null && Resources.Load<Texture2D>(art.backgroundResourcePath)!=null,"Unity own enemy major and background exist");
            if(definition.contentVersion==ColossusCombatDef.Plan9Version)foreach(int level in new[]{44,45,49,50}){
                var battle=new PlayableBattle(level,new PlayableProgress(),75,colossusDefinition:definition);
                battle.State.AdvanceBossGauge(definition.gaugeMax-1);
                Check(battle.NextEnemyAction==(level>=45?definition.ultimateAction:definition.majorAction),"Unity authored major and ultimate level boundary");
            }
        }
    }
}
