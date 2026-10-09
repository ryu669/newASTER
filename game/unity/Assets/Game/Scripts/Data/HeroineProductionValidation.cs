using System;
using System.Collections.Generic;
using System.Linq;
using NewAster.Core;
namespace NewAster.Data
{
    public static class HeroineProductionValidation
    {
        // Reuse domain validators, adding form/field context for production diagnostics.
        public static string[] Errors(CombatDefinitionCatalog combat,ProductionStoryContent story,HeroinePortraitCatalog portraits,HeroineWeaponTreeCatalog trees)
        {
            var errors=new List<string>();
            Action<string,string,Action> run=(id,field,action)=>{try{action();}catch(ArgumentException e){errors.Add(id+" / "+field+" / "+e.Message);}catch(InvalidOperationException e){errors.Add(id+" / "+field+" / "+e.Message);}};
            run("catalog","combat",()=>combat.Validate());
            run("catalog","story",()=>story.Validate(combat.HeroineIds,WorldCatalog.ColossusIds.ToArray()));
            run("catalog","portrait",()=>portraits.Validate());
            run("catalog","weapon-tree",()=>trees.Validate());
            foreach(var hero in combat.heroines){
                string id=hero.id;
                run(id,"personId",()=>{if(!combat.heroines.Any(h=>h.id==combat.PersonId(id)))throw new ArgumentException("Person must resolve to the canonical form.");});
                run(id,"traits",()=>{
                    var set=CombatTraitCatalog.Resolve(hero);
                    if(set.uniqueTraitIds.Length!=3 || set.interactionTraitIds.Length!=3 || set.VisibleIds.Length!=8)throw new ArgumentException("Require job traits 2, character traits 3, interaction traits 3 (eight total).");
                    for(int duplicateRank=0;duplicateRank<=5;duplicateRank++){
                        var cards=HeroineIdentityCatalog.Traits(hero,new FormalHeroineGrowth{heroineId=id,duplicateRank=duplicateRank});HeroineIdentityCatalog.ValidateTraits(cards);
                        if(!cards.Select(c=>c.id).SequenceEqual(set.VisibleIds))throw new ArgumentException("Display and battle trait order differ.");
                    }
                });
                run(id,"reactionStyle",()=>{var profile=string.IsNullOrEmpty(hero.reactionStyleMain)?DailyReactionProfiles.For(combat.PersonId(id)):new ReactionStyleProfile{main=hero.reactionStyleMain,sub=string.IsNullOrEmpty(hero.reactionStyleSub)?null:hero.reactionStyleSub};profile.Validate();});
                run(id,"skills",()=>{if(hero.skills.Length!=3 || hero.skills.Any(s=>!combat.skills.Any(d=>d.id==s && d.ownerId==id)))throw new ArgumentException("Three owned skills required.");});
                run(id,"portrait",()=>portraits.Entry(id));
                run(id,"weapon-tree",()=>{var tree=trees.Entry(id);if(tree.nodes.Length!=13)throw new ArgumentException("Thirteen nodes required.");});
                run(id,"story",()=>{var chapters=story.chapters.Where(c=>c.ownerId==id).ToArray();if(chapters.Length!=3 || chapters.Any(c=>c.pages==null || c.pages.Length!=10) || chapters.Sum(c=>c.poems.Length)!=18)throw new ArgumentException("Three ten-page chapters and eighteen poems required.");});
                run(id,"events",()=>{foreach(var e in story.events.Where(e=>e.ownerId==id))if(e.paragraphs==null || e.paragraphs.Length==0)throw new ArgumentException("Missing authored narrative: "+e.id);});
            }
            return errors.ToArray();
        }
    }
}
