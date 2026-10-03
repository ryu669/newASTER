using System;
using System.Collections.Generic;
using NewAster.Core;

public static class Plan6BookTests
{
    public static void Run(Action<bool,string> check)
    {
        var subjects=new List<BookOrderedSubject>{new BookOrderedSubject(BookBookmark.Colossi,"enemy.second",20),new BookOrderedSubject(BookBookmark.Colossi,"enemy.first",10),new BookOrderedSubject(BookBookmark.Heroines,"hero.first",1),new BookOrderedSubject(BookBookmark.Stories,"owner.first",0)};
        var book=new BookNavigationState(subjects);subjects.Clear();int destinations=0,completions=0;book.DestinationChanged+=()=>destinations++;book.TransitionCompleted+=()=>completions++;
        check(book.SubjectId=="enemy.first" && book.SubjectCount==2 && !book.CanTurnPrevious && book.CanTurnNext,"Explicit order wins over enumeration and source mutation");
        book.TurnPage(-1);check(book.SubjectId=="enemy.first" && destinations==0,"First page never wraps");
        book.FlipPage();check(book.Face==BookFace.Details && book.SubjectId=="enemy.first","Flip preserves subject");
        check(book.RequestTurn(1) && book.SubjectId=="enemy.second" && book.Face==BookFace.Overview && book.IsTransitioning,"One turn freezes destination and resets face");
        for(int i=0;i<100;i++){book.RequestTurn(-1);book.RequestBookmark(BookBookmark.Heroines);book.RequestFlip();}
        check(book.SubjectId=="enemy.second" && book.Bookmark==BookBookmark.Colossi && destinations==2,"Extra transition inputs are ignored without queue");
        check(book.CompleteTransition() && !book.CompleteTransition() && completions==1,"Transition skip syncs exactly once");
        book.TurnPage(1);check(book.SubjectId=="enemy.second" && !book.CanTurnNext,"Last page never wraps");
        book.ChangeBookmark(BookBookmark.Gardens);check(book.SubjectId==null && book.SubjectCount==0 && !book.CanFlip && !book.CanTurnNext && !book.CanTurnPrevious,"Empty category has no synthetic subject");
        book.FlipPage();book.TurnPage(1);check(book.Face==BookFace.Overview && !book.BeginReading("chapter.first",1),"Empty page refuses flip reading and turns");
        book.ChangeBookmark(BookBookmark.Heroines);check(book.SubjectId=="hero.first" && !book.CanTurnNext && !book.CanTurnPrevious,"Single subject has both directions disabled");
        book.FlipPage();book.ChangeBookmark(BookBookmark.Colossi);check(book.SubjectId=="enemy.first" && book.Face==BookFace.Overview,"Bookmark returns to category first overview");
        book.ChangeBookmark(BookBookmark.Stories);check(book.BeginReading("chapter.first",3),"Reading cursor starts on selected story owner");
        var reader=book.Reading;check(reader.Move(1) && reader.PageIndex==1 && book.SubjectId=="owner.first" && reader.ChapterId=="chapter.first","Text page advance preserves owner and chapter");
        book.ChangeBookmark(BookBookmark.Colossi);book.FlipPage();book.TurnPage(1);check(book.Bookmark==BookBookmark.Stories && book.SubjectId==reader.OwnerId,"Book subject cannot change beneath active reader");
        reader.Move(1);check(!reader.Move(1) && reader.PageIndex==2,"Reading page ends do not wrap");book.EndReading();
        check(book.RequestBookmark(BookBookmark.Colossi),"Reader close permits bookmark transition");book.Close();check(!book.IsTransitioning && !book.IsOpen && !book.RequestTurn(1),"Close discards animation without later queued input");
        book.Reenter();check(book.IsOpen && book.SubjectId=="enemy.first" && book.Face==BookFace.Overview && book.Reading==null,"Reentry restores specified initial view");
        check(book.RequestSubject(BookBookmark.Colossi,"enemy.second") && destinations==9,"Direct owner navigation commits a single destination");book.CompleteTransition();
        bool duplicate=false;try{new BookNavigationState(new[]{new BookOrderedSubject(BookBookmark.Colossi,"a",1),new BookOrderedSubject(BookBookmark.Colossi,"b",1)});}catch(ArgumentException){duplicate=true;}check(duplicate,"Duplicate order rejected before navigation");
        var empty=new BookNavigationState(Array.Empty<BookOrderedSubject>());check(!empty.HasSubject && empty.SubjectId==null,"Entirely empty book is valid");
        bool invalid=false;try{new BookOrderedSubject((BookBookmark)99,"a",1);}catch(ArgumentException){invalid=true;}check(invalid,"Unknown bookmark rejected");
    }
}
