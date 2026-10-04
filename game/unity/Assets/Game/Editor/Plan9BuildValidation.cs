using System.Linq;
using NewAster.Core;
using NewAster.Data;
using UnityEngine;
public static partial class PlayableBuild
{
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
