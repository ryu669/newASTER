using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    /// <summary>Contract fixtures only: no story text, accepted balance or authored correspondences.</summary>
    public static class CollectionContractFixture
    {
        public static CollectionCatalog Create(CombatDefinitionCatalog combat)
        {
            combat.Validate();var result=Create(combat.HeroineIds);
            foreach(var id in combat.HeroineIds){
                var hero=combat.Hero(id);var owner=result.owners.Single(o=>o.id==id);
                if(!hero.poemChapters.SequenceEqual(owner.chapterIds) || hero.poemLinks.Except(result.links.Where(l=>l.ownerId==id).Select(l=>l.id)).Any())throw new ArgumentException("Collection definitions do not resolve heroine references.");
            }
            return result;
        }
        public static CollectionCatalog Create(IEnumerable<string> heroineIds)
        {
            var owners=new List<CollectionOwnerDef>();var poems=new List<CollectionPoemDef>();
            var chapters=new List<CollectionChapterDef>();var resources=new List<CollectionResourceDef>();
            var relics=new List<CollectionRelicDef>();var bands=new List<CollectionRewardBandDef>();
            Action<string,string,string,bool,string[]> add=(id,kind,previous,integration,environments)=>{
                int count=kind=="colossus"?24:18,per=count/3;
                var chapterIds=Enumerable.Range(1,3).Select(i=>id+(kind=="heroine"?".poem-chapter.":".collection.chapter.")+i).ToArray();
                var poemIds=Enumerable.Range(1,count).Select(i=>id+".collection.poem."+i.ToString("D2")).ToArray();
                var material=id+".collection.material";
                owners.Add(new CollectionOwnerDef {id=id,kind=kind,previousOwnerId=previous,integration=integration,poemIds=poemIds,chapterIds=chapterIds,environmentIds=environments,materialIds=new[]{material}});
                resources.Add(new CollectionResourceDef {id=material,kind="material",ownerId=id});
                foreach(var environment in environments)resources.Add(new CollectionResourceDef {id=environment,kind="environment",ownerId=id});
                for(int i=0;i<count;i++)poems.Add(new CollectionPoemDef {id=poemIds[i],ownerId=id,chapterId=chapterIds[i/per]});
                for(int i=0;i<3;i++)chapters.Add(new CollectionChapterDef {id=chapterIds[i],ownerId=id,poemIds=poemIds.Skip(i*per).Take(per).ToArray()});
            };
            foreach(var c in WorldCatalog.Colossi){
                add(c.Id,"colossus",c.PrerequisiteColossusId,c.IsIntegrationBoss,c.EnvironmentTags.Select((tag,i)=>c.Id+".collection.environment."+i).ToArray());
                var relic=c.Id+".collection.relic";
                relics.Add(new CollectionRelicDef {id=relic,abilityId="ability.fixture.attack",materialIds=new[]{c.Id+".collection.material"}});
                for(int tier=0;tier<5;tier++)bands.Add(new CollectionRewardBandDef {ownerId=c.Id,minLevel=tier==0?1:tier*10,maxLevel=tier==4?50:tier*10+9,draws=tier+1,terraforming=4+tier,allowEmpty=true,relicIds=new[]{relic}});
            }
            foreach(var id in heroineIds)add(id,"heroine",null,false,Array.Empty<string>());
            var result=new CollectionCatalog {owners=owners.ToArray(),poems=poems.ToArray(),chapters=chapters.ToArray(),resources=resources.ToArray(),relics=relics.ToArray(),rewardBands=bands.ToArray(),links=owners.Where(o=>o.kind=="heroine").SelectMany(o=>Enumerable.Range(0,18).Select(i=>new CollectionLinkDef {id=o.id+".poem-link."+(i+1),ownerId=o.id,sourcePoemId=owners[0].poemIds[i],targetPoemId=o.poemIds[i]})).ToArray()};
            result.weaponNodes=owners.Where(o=>o.kind=="heroine").SelectMany(o=>Enumerable.Range(1,3).Select(i=>new CollectionWeaponNodeDef {id=o.id+".collection.weapon-node."+i,ownerId=o.id,materialIds=new[]{owners[0].materialIds[0]},prerequisiteIds=i==1?Array.Empty<string>():new[]{o.id+".collection.weapon-node."+(i-1)}})).ToArray();
            result.Validate();return result;
        }
    }
}
