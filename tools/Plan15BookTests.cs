using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using NewAster.Core;
public static class Plan15BookTests
{
    public static void Run(Action<bool,string> check)
    {
        var catalog=Enumerable.Range(0,256).Select(i=>new BookOrderedSubject(BookBookmark.Heroines,"hero."+i,i)).Concat(new[]{new BookOrderedSubject(BookBookmark.Colossi,"boss.a",0)}).ToArray();
        var b=new BookNavigationState(catalog);b.ChangeBookmark(BookBookmark.Heroines);b.RequestSubject(BookBookmark.Heroines,"hero.200");b.CompleteTransition();b.FlipPage();b.PageState.searchQuery="sample";b.PageState.filterIds=new[]{"owned"};b.PageState.selectedColossusLevel=32;
        b.ChangeBookmark(BookBookmark.Colossi);b.ChangeBookmark(BookBookmark.Heroines);check(b.SubjectId=="hero.200" && b.Face==BookFace.Details,"BK-01/BK-04 bookmark preserves ID and face");
        b.Close();b.Reenter();check(b.SubjectId=="hero.200" && b.Face==BookFace.Details,"BK-02 reopening keeps page");
        var options=new JsonSerializerOptions{IncludeFields=true};var saved=JsonSerializer.Serialize(b.Capture(),options);b=new BookNavigationState(catalog);b.Restore(JsonSerializer.Deserialize<BookNavigationSave>(saved,options));
        check(b.SubjectId=="hero.200" && b.Face==BookFace.Details && b.PageState.searchQuery=="sample" && b.PageState.filterIds.Single()=="owned","BK-03 persistent serialized state");
        b.SetResults(BookBookmark.Heroines,new[]{"hero.200","hero.222"});check(b.RequestTurn(1) && b.SubjectId=="hero.222" && !b.CanTurnNext,"BK-05 turns stay within results");b.CompleteTransition();
        b.SetResults(BookBookmark.Heroines,Array.Empty<string>());check(!b.HasSubject && !b.CanFlip && !b.RequestSubject(BookBookmark.Heroines,"unknown"),"BK-06 zero results and unknown ID are safe");
        b.SetResults(BookBookmark.Heroines,catalog.Where(x=>x.Bookmark==BookBookmark.Heroines).Select(x=>x.SubjectId));check(b.HasSubject,"BK-06 clearing conditions restores results");
        b.Restore(new BookNavigationSave{lastBookmarkId="unknown",pages=new[]{new BookPageState{bookmarkId="Heroines",selectedSubjectId="deleted",selectedFaceId="unknown"}}});check(b.SubjectId=="hero.0" && b.Face==BookFace.Overview,"BK-06 deleted subject and invalid face fallback");
        b.PageState.selectedColossusLevel=42;var before=b.Capture();b.RequestSubject(BookBookmark.Colossi,"boss.a");b.CompleteTransition();check(b.GoBack() && b.Bookmark==BookBookmark.Heroines && b.PageState.selectedColossusLevel==42,"BK-07/BK-08 transient return preserves selection");b.CompleteTransition();
        b.FlipPage();check(before.pages.Single(x=>x.bookmarkId=="Heroines").selectedFaceId=="Overview","BK-03 captured save is detached from later navigation");
        var watch=Stopwatch.StartNew();for(int i=0;i<1000;i++)b.SetResults(BookBookmark.Heroines,catalog.Where(x=>x.Bookmark==BookBookmark.Heroines).Select(x=>x.SubjectId));watch.Stop();check(watch.ElapsedMilliseconds<2000,"BK-13 256-person result refresh stays bounded");
        Console.WriteLine("BK13_NAVIGATION_1000_REFRESH_MS "+watch.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
        var roster=new HeroineRoster(Enumerable.Range(0,256).Select(i=>new HeroineRosterEntry{id="hero."+i,name="person"+i,jobId="job.fighter",stage="available",originalStats=true,originalSkills=true}),new[]{"job.fighter"},new[]{"job.fighter"});
        watch.Restart();for(int i=0;i<1000;i++)check(roster.Search("person2","job.fighter",null).Length==67,"BK-13 exact 256-person name and job result");watch.Stop();check(watch.ElapsedMilliseconds<2000,"BK-13 256-person search stays bounded");
        Console.WriteLine("BK13_SEARCH_1000_QUERY_MS "+watch.Elapsed.TotalMilliseconds.ToString("F2",System.Globalization.CultureInfo.InvariantCulture));
        var availability=new BookNavigationState(catalog);availability.Restore(new BookNavigationSave{lastBookmarkId="Heroines",pages=new[]{new BookPageState{bookmarkId="Heroines",selectedSubjectId="hero.200"}}},(bookmark,id)=>bookmark!=BookBookmark.Heroines || id=="hero.3");check(availability.SubjectId=="hero.3","BK-06 locked saved target falls back to first available ID");
        availability.Restore(availability.Capture(),(bookmark,id)=>bookmark!=BookBookmark.Heroines);check(!availability.HasSubject && !availability.CanFlip,"BK-06 no available targets has a safe empty page");
        var old=new BookNavigationState(catalog);old.Restore(null);check(old.SubjectId=="boss.a" && old.Face==BookFace.Overview,"BK-15 old save starts at initial page");
        Console.WriteLine("PLAN15_BOOK_PASS navigation / restore / results / history / 256 subjects");
    }
}
