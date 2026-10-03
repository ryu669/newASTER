using System;
using System.Linq;

namespace NewAster.Core
{
    [Serializable] public sealed class TrialStoryPoem
    { public string id,text,body,sourcePoemId,reason; }
    [Serializable] public sealed class TrialStoryChapter
    { public string id,ownerId,title,introduction,conclusion; public TrialStoryPoem[] poems; }
    [Serializable] public sealed class TrialStoryEvent
    { public string id,ownerId,title; public int affectionRequired; public bool establishesLover; public string[] paragraphs; }
    [Serializable] public sealed class TrialStoryContent
    {
        public int schemaVersion;
        public string contentVersion,status,provenance,colossusId;
        public TrialStoryChapter[] chapters;
        public TrialStoryEvent[] events;
        public void Validate(CombatDefinitionCatalog combat,string firstColossus)
        {
            if(schemaVersion!=1 || status!="development-trial" || provenance!="newaster-original" ||
                contentVersion!="trial-story-2026-10-04" || colossusId!=firstColossus || chapters==null ||
                chapters.Length!=8 || chapters.Any(c=>c==null || c.poems==null) || chapters.Select(c=>c.id).Distinct().Count()!=8)
                throw new ArgumentException("Invalid original trial story scope.");
            var heroes=combat.FormationIds;
            if(chapters.Count(c=>c.ownerId==colossusId)!=3 || chapters.Count(c=>heroes.Contains(c.ownerId))!=5)
                throw new ArgumentException("Trial requires three enemy and five first heroine chapters.");
            var poems=chapters.SelectMany(c=>c.poems??Array.Empty<TrialStoryPoem>()).ToArray();
            if(poems.Length!=54 || poems.Any(p=>p==null || string.IsNullOrWhiteSpace(p.text) || string.IsNullOrWhiteSpace(p.body) || !p.body.Contains(p.text)) || poems.Select(p=>p.id).Distinct().Count()!=54)
                throw new ArgumentException("Each of 54 unique poems must occur in its authored paragraph.");
            foreach(var c in chapters){
                bool enemy=c.ownerId==colossusId;
                if(!enemy && !heroes.Contains(c.ownerId) || string.IsNullOrWhiteSpace(c.title) || string.IsNullOrWhiteSpace(c.introduction) || string.IsNullOrWhiteSpace(c.conclusion) || c.poems==null || c.poems.Length!=(enemy?8:6))
                    throw new ArgumentException("Invalid trial chapter owner or text.");
                int index=enemy?Array.IndexOf(chapters.Where(ch=>ch.ownerId==colossusId).ToArray(),c)+1:1;
                string chapterId=c.ownerId+(enemy?".collection.chapter.":".poem-chapter.")+index;
                if(c.id!=chapterId)throw new ArgumentException("Trial chapter identity mismatch.");
                for(int i=0;i<c.poems.Length;i++){
                    var p=c.poems[i];
                    if(p.id!=c.ownerId+".collection.poem."+((enemy?(index-1)*8:0)+i+1).ToString("D2"))throw new ArgumentException("Poem does not belong to chapter.");
                    if(enemy){if(!string.IsNullOrEmpty(p.sourcePoemId) || !string.IsNullOrEmpty(p.reason))throw new ArgumentException("Enemy poem cannot have heroine correspondence.");}
                    else if(string.IsNullOrWhiteSpace(p.reason) || !chapters.Where(ch=>ch.ownerId==colossusId).SelectMany(ch=>ch.poems).Any(source=>source.id==p.sourcePoemId))throw new ArgumentException("Missing authored source correspondence.");
                }
            }
            foreach(var hero in heroes)if(chapters.Count(c=>c.ownerId==hero)!=1)throw new ArgumentException("Missing heroine first chapter.");
            if(chapters.Where(c=>heroes.Contains(c.ownerId)).Any(c=>c.poems.Select(p=>p.sourcePoemId).Distinct().Count()!=6))throw new ArgumentException("First heroine chapter requires six distinct source poems.");
            if(chapters.Where(c=>heroes.Contains(c.ownerId)).Select(c=>string.Join("/",c.poems.Select(p=>p.sourcePoemId))).Distinct().Count()!=5)
                throw new ArgumentException("Heroine correspondences cannot share one generated index mapping.");
            if(events==null || events.Length!=1 || events[0]==null || events[0].id!="heroine.slayer.event.0" || events[0].ownerId!="heroine.slayer" || events[0].establishesLover || events[0].affectionRequired!=1 || string.IsNullOrWhiteSpace(events[0].title) || events[0].paragraphs==null || events[0].paragraphs.Length<6 || events[0].paragraphs.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Trial requires one explicit affection event without lover status.");
        }
    }
}
