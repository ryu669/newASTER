using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;

namespace NewAster.Data
{
    public static class TrialStoryCatalog
    {
        public static string Id(string original)=>"trial.plan8."+original;
        public static CollectionCatalog Collection(CombatDefinitionCatalog combat,TrialStoryContent story)
        {
            story.Validate(combat,WorldCatalog.ColossusIds[0]);
            var c=CollectionContractFixture.Create(combat);
            c.contentVersion=CollectionCatalog.TrialVersion;c.status="development-trial";
            foreach(var chapter in story.chapters){
                var ch=c.chapters.Single(x=>x.id==chapter.id);string old=ch.id;
                ch.id=Id(old);ch.textId="text."+ch.id+".intro";
                var owner=c.owners.Single(x=>x.id==chapter.ownerId);
                owner.chapterIds=owner.chapterIds.Select(id=>id==old?ch.id:id).ToArray();
                foreach(var poem in chapter.poems){
                    var p=c.poems.Single(x=>x.id==poem.id);string oldPoem=p.id;p.chapterId=ch.id;
                    if(chapter.ownerId!=story.colossusId){
                        p.id=Id(oldPoem);owner.poemIds=owner.poemIds.Select(id=>id==oldPoem?p.id:id).ToArray();
                        var link=c.links.Single(x=>x.targetPoemId==oldPoem);link.targetPoemId=p.id;link.sourcePoemId=poem.sourcePoemId;
                    }
                    ch.poemIds=ch.poemIds.Select(id=>id==oldPoem?p.id:id).ToArray();
                }
            }
            // Unauthored second/third chapters have no inferred correspondences.
            c.links=c.links.Where(l=>l.targetPoemId.StartsWith("trial.plan8.",StringComparison.Ordinal)).ToArray();
            c.Validate();return c;
        }

        public static HomeExperienceCatalog Home(CombatDefinitionCatalog combat,TrialStoryContent story)
        {
            var collection=Collection(combat,story);var home=HomeExperienceFixture.Create(combat);
            home.contentVersion=HomeExperienceCatalog.TrialVersion;home.status="development-trial";
            home.poemIds=collection.poems.Select(p=>p.id).ToArray();
            foreach(var original in story.chapters){
                var ch=home.chapters.Single(x=>x.id==original.id);ch.id=Id(original.id);ch.sceneId="scene."+ch.id;
                ch.requiredPoemIds=collection.chapters.Single(x=>x.id==ch.id).poemIds;
                AddScript(home,ch.sceneId,original.ownerId==story.colossusId?null:original.ownerId,
                    new[]{original.introduction}.Concat(original.poems.Select(p=>p.body)).Concat(new[]{original.conclusion}).ToArray());
            }
            foreach(var original in story.events){
                var e=new HomeEventDef{id=Id(original.id),heroineId=original.ownerId,kind="affinity",sceneId="scene."+Id(original.id),establishesLover=false,
                    unlockCondition=new HomeCondition{kind="atLeast",domain="affection",ownerId=original.ownerId,value=original.affectionRequired}};
                home.events=home.events.Concat(new[]{e}).ToArray();AddScript(home,e.sceneId,e.heroineId,original.paragraphs);
            }
            home.Validate();return home;
        }

        private static void AddScript(HomeExperienceCatalog home,string scene,string hero,string[] paragraphs)
        {
            var commands=new List<HomeAdvCommand>{new HomeAdvCommand{commandId="background",kind="background",assetId="asset.fixture.background",transition="instant"}};
            if(hero!=null)commands.Add(new HomeAdvCommand{commandId="actor",kind="actor",heroineId=hero,slotId="slot.center",outfitId="outfit.fixture",expressionId="expression.normal",poseId="pose.idle"});
            for(int i=0;i<paragraphs.Length;i++){
                string text="text."+scene.Substring("scene.".Length)+"."+(i==0?"intro":i.ToString());
                home.texts=home.texts.Concat(new[]{new HomeTextDef{id=text,text=paragraphs[i]}}).ToArray();
                commands.Add(new HomeAdvCommand{commandId="command."+i,kind="line",lineId="line."+i,textId=text,speakerId=hero});
            }
            commands.Add(new HomeAdvCommand{commandId="end",kind="end"});
            home.scripts=home.scripts.Concat(new[]{new HomeAdvScript{id=scene,schemaVersion=1,scriptVersion=1,commands=commands.ToArray()}}).ToArray();
        }
    }
}
