using System;
using System.Linq;
using NewAster.Core;
internal static class HeroineRosterTests
{
    public static void Run(Action<bool,string> check)
    {
        var entries=Enumerable.Range(0,256).Select(i=>new HeroineRosterEntry{id="heroine.future."+i.ToString("D4"),name="制作試験"+i,jobId=i<220?"job.fighter":"job.healer",stage=i<220?"available":"planned",originalStats=true,originalSkills=true}).ToArray();
        var roster=new HeroineRoster(entries,new[]{"job.fighter","job.healer"},new[]{"job.fighter"});
        check(roster.Count==256,"Roster holds more than 200 independent stable identities");
        entries[0].name="mutated";check(roster.Get(entries[0].id).name!="mutated","Authoring input cannot mutate the catalog");
        var pages=Enumerable.Range(0,11).SelectMany(i=>roster.Page(i*20,20)).ToArray();
        check(pages.Length==220 && pages.Select(e=>e.id).Distinct().Count()==220,"Stable pagination neither omits nor repeats available heroines");
        check(roster.Page(0,100,"job.healer").Length==0 && roster.Page(0,100,"job.healer",false).Length==36,"Planned jobs stay out of available inventory");
        var owned=pages.Where((e,i)=>i%3==0).Select(e=>e.id).ToArray();
        check(roster.Page(0,100,ownedIds:owned).Length==owned.Length,"Owned inventory filters across a large roster");
        check(roster.Formation(owned.Take(5),owned).SequenceEqual(owned.Take(5)),"Five selected actors remain independent of total roster size");
        Action<Action,string> reject=(action,name)=>{bool failed=false;try{action();}catch(ArgumentException){failed=true;}check(failed,name);};
        reject(()=>roster.Formation(owned.Take(4),owned),"Incomplete formation is rejected");
        reject(()=>roster.Formation(Enumerable.Repeat(owned[0],5),owned),"Duplicate formation is rejected");
        reject(()=>roster.Formation(pages.Take(5).Select(e=>e.id),owned),"Unowned members cannot enter formation");
        reject(()=>roster.Formation(entries.Skip(220).Take(5).Select(e=>e.id),entries.Select(e=>e.id)),"Unimplemented jobs cannot enter combat");
        entries[220].stage="available";reject(()=>new HeroineRoster(entries,new[]{"job.fighter","job.healer"},new[]{"job.fighter"}),"A future job cannot be marked available early");
        entries[220].stage="planned";entries[0].unresolved=new[]{"skill evidence missing"};reject(()=>new HeroineRoster(entries,new[]{"job.fighter","job.healer"},new[]{"job.fighter"}),"Unresolved evidence blocks completed authoring status");
    }
}
