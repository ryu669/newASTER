using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan8StoryTests
{
    public static void Run(Action<bool,string> check,CombatDefinitionCatalog combat,string json)
    {
        Func<TrialStoryContent> fresh=()=>JsonSerializer.Deserialize<TrialStoryContent>(json,new JsonSerializerOptions{IncludeFields=true});
        var pack=fresh();pack.Validate(combat,WorldCatalog.ColossusIds[0]);
        check(pack.chapters.Length==8 && pack.chapters.Sum(c=>c.poems.Length)==54,"Original trial content contains eight chapters and 54 poem excerpts");
        check(pack.chapters.Skip(3).All(c=>c.id.EndsWith(".poem-chapter.1")),"Heroine chapters two and three stay outside authored trial content");
        check(pack.chapters.Skip(3).Select(c=>string.Join("/",c.poems.Select(p=>p.sourcePoemId))).Distinct().Count()==5,"Five authored correspondences use different source mappings");
        Action<Action<TrialStoryContent>> reject=mutate=>{var value=fresh();mutate(value);bool failed=false;try{value.Validate(combat,WorldCatalog.ColossusIds[0]);}catch(ArgumentException){failed=true;}check(failed,"Invalid authored trial pack is rejected");};
        reject(p=>p.status="release");reject(p=>p.provenance="video-transcript");reject(p=>p.chapters=p.chapters.Take(7).ToArray());
        reject(p=>p.chapters[0].poems=null);reject(p=>p.chapters[3].poems[0].sourcePoemId="unknown");
        reject(p=>p.chapters[3].poems[0].reason="");reject(p=>p.chapters[0].poems[0].body="not present");
        reject(p=>p.chapters[3].id="heroine.slayer.poem-chapter.2");reject(p=>p.chapters[3].poems[1].sourcePoemId=p.chapters[3].poems[0].sourcePoemId);
        reject(p=>p.events[0].establishesLover=true);reject(p=>p.events[0].affectionRequired=0);reject(p=>p.events[0].paragraphs=new[]{"one"});
        reject(p=>p.chapters[0].ownerId="colossus.other");
    }
}
