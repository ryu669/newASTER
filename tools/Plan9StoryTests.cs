using System;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
using NewAster.Data;

public static class Plan9StoryTests
{
    public static readonly string[] Heroes={"heroine.slayer","heroine.iconoclast","heroine.undermine","heroine.echidna","heroine.excalipan"};
    public static void Run(Action<bool,string> check,string json)
    {
        Func<ProductionStoryContent> fresh=()=>JsonSerializer.Deserialize<ProductionStoryContent>(json,new JsonSerializerOptions{IncludeFields=true});
        var pack=fresh();pack.Validate(Heroes,WorldCatalog.ColossusIds.ToArray());
        check(pack.chapters.Length==60 && pack.chapters.Sum(c=>c.poems.Length)==450 && pack.events.Length==25,"Complete authored production story passes core validation");
        check(pack.chapters.All(c=>!c.id.StartsWith("trial.")) && pack.events.All(e=>!e.id.StartsWith("trial.")),"Production identities remain separate from diagnostic profiles");
        foreach(var hero in Heroes){
            check(pack.chapters.Where(c=>c.ownerId==hero).SelectMany(c=>c.poems).Select(p=>p.sourcePoemId).Distinct().Count()==18,"Each heroine has eighteen individually selected correspondences");
            check(pack.events.Count(e=>e.ownerId==hero && e.establishesLover)==1 && pack.events.Single(e=>e.ownerId==hero && e.establishesLover).id==hero+".event.2","Only the mutual confession establishes the lover relationship");
        }
        Action<Action<ProductionStoryContent>> reject=mutate=>{var value=fresh();mutate(value);bool failed=false;try{value.Validate(Heroes,WorldCatalog.ColossusIds.ToArray());}catch(ArgumentException){failed=true;}check(failed,"Malformed production narrative is rejected before use");};
        reject(p=>p.schemaVersion=2);reject(p=>p.contentVersion="future");reject(p=>p.provenance="unknown");
        reject(p=>p.chapters=null);reject(p=>p.chapters=p.chapters.Take(59).ToArray());reject(p=>p.chapters[0]=null);
        reject(p=>p.chapters[0].id=p.chapters[1].id);reject(p=>p.chapters[0].ownerId="unknown");
        reject(p=>p.chapters[0].poems=null);reject(p=>p.chapters[0].poems[0]=null);
        reject(p=>p.chapters[0].poems[0].body="No quoted poem");reject(p=>p.chapters[0].introduction="【動作検証用】");
        reject(p=>p.chapters[0].poems[0].id="foreign.poem");
        reject(p=>p.chapters.First(c=>c.ownerId==Heroes[0]).poems[0].sourcePoemId="missing");
        reject(p=>p.chapters.First(c=>c.ownerId==Heroes[0]).poems[0].reason="");
        reject(p=>{var c=p.chapters.First(v=>v.ownerId==Heroes[0]);c.poems[1].sourcePoemId=c.poems[0].sourcePoemId;});
        reject(p=>{var c=p.chapters.First(v=>v.ownerId==WorldCatalog.ColossusIds[0]);c.poems[0].sourcePoemId="unexpected";});
        reject(p=>p.events=null);reject(p=>p.events=p.events.Take(24).ToArray());reject(p=>p.events[0]=null);
        reject(p=>p.events[0].id=p.events[1].id);reject(p=>p.events[0].ownerId="unknown");
        reject(p=>p.events[0].affectionRequired=999);reject(p=>p.events[0].establishesLover=!p.events[0].establishesLover);
        reject(p=>p.events[0].paragraphs=new[]{"too short"});reject(p=>p.events[0].paragraphs=null);
        reject(p=>p.events[0].paragraphs[0]=p.events[1].paragraphs[0]);
        reject(p=>p.events[0].expressions=new[]{"normal"});reject(p=>p.events[0].expressions[0]="unknown");
        reject(p=>p.events[0].cgResourcePath=p.events[1].cgResourcePath);reject(p=>p.events[0].backgroundResourcePath="");
        bool invalidOwners=false;try{pack.Validate(new[]{Heroes[0],Heroes[0],Heroes[2],Heroes[3],Heroes[4]},WorldCatalog.ColossusIds.ToArray());}catch(ArgumentException){invalidOwners=true;}
        check(invalidOwners,"Duplicated production roster is rejected");
    }
}
