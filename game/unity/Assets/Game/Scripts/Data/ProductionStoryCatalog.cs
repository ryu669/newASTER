using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;

namespace NewAster.Data
{
    // Initial-five production narrative, authored economy and individually adopted art.
    public static class ProductionStoryCatalog
    {
        public static CollectionCatalog Collection(CombatDefinitionCatalog combat,ProductionStoryContent story)
        {
            story.Validate(combat.FormationIds,WorldCatalog.ColossusIds.ToArray());
            var catalog=CollectionContractFixture.Create(combat);
            catalog.contentVersion=CollectionCatalog.ProductionVersion;catalog.status="release";
            foreach(var chapter in story.chapters){
                var target=catalog.chapters.Single(c=>c.id==chapter.id);
                if(!target.poemIds.SequenceEqual(chapter.poems.Select(p=>p.id)))throw new ArgumentException("Production poem membership differs from preserved IDs.");
                target.textId="text.production."+chapter.id+".intro";
            }
            catalog.links=story.chapters.Where(c=>combat.FormationIds.Contains(c.ownerId)).SelectMany(c=>c.poems.Select(p=>new CollectionLinkDef{
                id="link.production."+p.id,ownerId=c.ownerId,sourcePoemId=p.sourcePoemId,targetPoemId=p.id})).ToArray();
            ProductionEconomyCatalog.ApplyCollection(catalog);catalog.Validate();return catalog;
        }
        public static HomeExperienceCatalog Home(CombatDefinitionCatalog combat,ProductionStoryContent story)
        {
            var collection=Collection(combat,story);
            // Reuse candidate art layout contracts, replace every fixture narrative and scene.
            var home=HomeExperienceFixture.Create(combat);
            home.contentVersion=HomeExperienceCatalog.ProductionVersion;home.status="release";
            home.texts=Array.Empty<HomeTextDef>();home.scripts=Array.Empty<HomeAdvScript>();
            var assets=home.assets.ToList();var texts=new List<HomeTextDef>();var scripts=new List<HomeAdvScript>();
            foreach(var chapter in story.chapters){
                var target=home.chapters.Single(c=>c.id==chapter.id);target.sceneId="scene.production."+chapter.id;
                target.requiredPoemIds=chapter.poems.Select(p=>p.id).ToArray();
                AddScript(target.sceneId,combat.FormationIds.Contains(chapter.ownerId)?chapter.ownerId:null,chapter.backgroundResourcePath,null,
                    new[]{chapter.introduction}.Concat(chapter.poems.Select(p=>p.body)).Concat(new[]{chapter.conclusion}).ToArray(),null,assets,texts,scripts);
            }
            foreach(var authored in story.events){
                var target=home.events.Single(e=>e.id==authored.id);int index=int.Parse(authored.id.Substring(authored.id.LastIndexOf('.')+1));
                target.sceneId="scene.production."+authored.id;target.kind=index<3?"affinity":"lover";target.establishesLover=authored.establishesLover;
                var conditions=new List<HomeCondition>{new HomeCondition{kind="atLeast",domain="affection",ownerId=authored.ownerId,value=authored.affectionRequired}};
                if(index>0)conditions.Add(new HomeCondition{kind="flag",domain="eventRead",id=authored.ownerId+".event."+(index-1)});
                if(index>2)conditions.Add(new HomeCondition{kind="flag",domain="lover",id=authored.ownerId});
                target.unlockCondition=new HomeCondition{kind="all",items=conditions.ToArray()};
                AddScript(target.sceneId,authored.ownerId,authored.backgroundResourcePath,authored.cgResourcePath,authored.paragraphs,authored.expressions,assets,texts,scripts);
            }
            home.assets=assets.ToArray();home.texts=texts.ToArray();home.scripts=scripts.ToArray();
            ProductionGardenCatalog.Apply(home,combat,collection);
            ProductionEconomyCatalog.ApplyHome(home,combat,collection);
            foreach(var display in home.displays){
                var standing=home.assets.Single(a=>a.id==display.standingAssetId);
                string normal="art.production."+display.heroineId+".normal",pose="art.production."+display.heroineId+".idle";
                home.assets=home.assets.Concat(new[]{new HomeAssetDef{id=normal,kind="expression",resourcePath=standing.resourcePath,fullFrame=true,placeholder=true},new HomeAssetDef{id=pose,kind="pose",resourcePath=standing.resourcePath,fullFrame=true,placeholder=true}}).ToArray();
                display.expressions.Single(e=>e.id=="expression.normal").assetId=normal;display.poses.Single(p=>p.id=="pose.idle").assetId=pose;
            }
            var used=new HashSet<string>(home.displays.SelectMany(d=>new[]{d.standingAssetId}.Concat(d.expressions.Select(e=>e.assetId)).Concat(d.poses.Select(p=>p.assetId)))
                .Concat(home.gardens.SelectMany(g=>new[]{g.backgroundAssetId}.Concat(g.middleAssetIds).Concat(g.foregroundAssetIds)))
                .Concat(home.furniture.Select(f=>f.assetId)).Concat(home.scripts.SelectMany(s=>s.commands).SelectMany(c=>new[]{c.assetId,c.audioId}).Where(id=>id!=null)));
            home.assets=home.assets.Where(a=>used.Contains(a.id)).ToArray();
            ProductionAssetAcceptance.Adopt(home);home.Validate(true);return home;
        }
        private static void AddScript(string scene,string hero,string background,string cg,string[] paragraphs,string[] expressions,List<HomeAssetDef> assets,List<HomeTextDef> texts,List<HomeAdvScript> scripts)
        {
            Func<string,string,string> asset=(kind,path)=>{
                string id="art.production."+kind+"."+path.Replace('/','.');
                if(!assets.Any(a=>a.id==id))assets.Add(new HomeAssetDef{id=id,kind=kind,resourcePath=path,placeholder=true});
                return id;
            };
            var commands=new List<HomeAdvCommand>{new HomeAdvCommand{commandId="background",kind="background",assetId=asset("background",background),transition="instant"}};
            for(int i=0;i<paragraphs.Length;i++){
                if(hero!=null)commands.Add(new HomeAdvCommand{commandId="actor."+i,kind="actor",heroineId=hero,slotId="slot.center",outfitId="outfit.fixture",expressionId="expression."+(expressions==null?"normal":expressions[i]),poseId="pose.idle"});
                if(cg!=null && i==2)commands.Add(new HomeAdvCommand{commandId="cg",kind="cg",assetId=asset("cg",cg),hideActors=true});
                if(cg!=null && i==paragraphs.Length-1)commands.Add(new HomeAdvCommand{commandId="hide-cg",kind="hideCg"});
                string key=scene.Substring("scene.".Length),text="text."+key+"."+(i==0?"intro":i.ToString());
                texts.Add(new HomeTextDef{id=text,text=paragraphs[i]});
                commands.Add(new HomeAdvCommand{commandId="line."+i,kind="line",lineId="line."+i,textId=text});
            }
            commands.Add(new HomeAdvCommand{commandId="end",kind="end"});
            scripts.Add(new HomeAdvScript{id=scene,schemaVersion=1,scriptVersion=1,commands=commands.ToArray()});
        }
    }
}
