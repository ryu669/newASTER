using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
public static partial class PlayableBuild
{
    private static void ValidatePlan9Story()
    {
        foreach(var accepted in ProductionAssetAcceptance.Hashes){
            string png="Assets/Game/Resources/"+accepted.Key+".png",wav="Assets/Game/Resources/"+accepted.Key+".wav";
            string file=System.IO.File.Exists(png)?png:wav;
            Check(System.IO.File.Exists(file),"Individually accepted production asset exists");
            using(var sha=System.Security.Cryptography.SHA256.Create())Check(System.BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(file))).Replace("-","").ToLowerInvariant()==accepted.Value,"Individually accepted production asset matches adopted bytes");
        }
        var asset=Resources.Load<TextAsset>("Story/plan9-story-content");
        Check(asset!=null,"Unity production narrative resource exists");
        var content=JsonUtility.FromJson<ProductionStoryContent>(asset.text);
        var combat=JsonUtility.FromJson<CombatDefinitionCatalog>(Resources.Load<TextAsset>("Combat/battle-formal").text);
        content.Validate(combat.formation,WorldCatalog.ColossusIds.ToArray());
        var copy=JsonUtility.FromJson<ProductionStoryContent>(JsonUtility.ToJson(content));
        copy.Validate(combat.formation,WorldCatalog.ColossusIds.ToArray());
        var collection=ProductionStoryCatalog.Collection(combat,copy);var home=ProductionStoryCatalog.Home(combat,copy);
        var serializedHome=JsonUtility.FromJson<HomeExperienceCatalog>(JsonUtility.ToJson(home));serializedHome.Validate(true);
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
        Check(home.gardens.Length==9 && home.gardens.All(g=>!g.unmade && g.middleAssetIds.Length==1 && g.foregroundAssetIds.Length==1) && home.furniture.Length==10,"Unity production gardens and furniture cover all layouts");
        foreach(string path in home.gardens.Select(g=>g.backgroundAssetId).Concat(home.gardens.SelectMany(g=>g.middleAssetIds.Concat(g.foregroundAssetIds))).Concat(home.furniture.Select(f=>f.assetId)).Distinct().Select(id=>home.assets.Single(a=>a.id==id).resourcePath))Check(Resources.Load<Texture2D>(path)!=null,"Unity production garden layers and furniture exist");
        Debug.Log("PLAN9_STORY_SOURCE_PASS full authored resource and catalog adapter; normal bootstrap connected and isolated player journey validated separately");
        foreach(string name in new[]{"bgm-title","bgm-battle","bgm-garden","bgm-adv","se-confirm","se-cancel","se-page","se-unlock"}){
            var clip=Resources.Load<AudioClip>("Audio/plan9-"+name+"-candidate-v1");Check(clip!=null && clip.channels==2 && clip.frequency==44100 && clip.samples>1000,"Unity original scene music and UI sound clip is valid stereo PCM");
        }
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
