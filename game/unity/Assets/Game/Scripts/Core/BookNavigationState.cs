using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public enum BookBookmark { Colossi, Heroines, Gardens, Stories, NewWorld, PossibleWorlds, Summoning, Items, RelicHunt }
    public enum BookFace { Overview, Details }
    public sealed class BookOrderedSubject
    {
        public BookBookmark Bookmark {get;} public string SubjectId {get;} public int PageOrder {get;}
        public BookOrderedSubject(BookBookmark bookmark,string subjectId,int pageOrder)
        {if(!Enum.IsDefined(typeof(BookBookmark),bookmark) || !CollectionCatalog.ValidId(subjectId) || pageOrder<0)throw new ArgumentException("Invalid book subject.");Bookmark=bookmark;SubjectId=subjectId;PageOrder=pageOrder;}
    }
    public sealed class BookReadingCursor
    {
        public string OwnerId {get;} public string ChapterId {get;} public int PageCount {get;} public int PageIndex {get;private set;}
        public BookReadingCursor(string ownerId,string chapterId,int pageCount)
        {if(!CollectionCatalog.ValidId(ownerId) || !CollectionCatalog.ValidId(chapterId) || pageCount<1)throw new ArgumentException("Invalid book reading cursor.");OwnerId=ownerId;ChapterId=chapterId;PageCount=pageCount;}
        public bool Move(int direction){int next=PageIndex+Math.Sign(direction);if(direction==0 || next<0 || next>=PageCount)return false;PageIndex=next;return true;}
    }
    /// <summary>Presentation transitions never queue input or change durable progression.</summary>
    public sealed class BookNavigationState
    {
        private readonly Dictionary<BookBookmark,IReadOnlyList<string>> subjects;
        private readonly BookBookmark initialBookmark;
        public BookBookmark Bookmark {get;private set;}
        public int SubjectIndex {get;private set;}
        public BookFace Face {get;private set;}
        public int SubjectCount=>subjects[Bookmark].Count;
        public bool HasSubject=>SubjectCount>0;
        public string SubjectId=>HasSubject?subjects[Bookmark][SubjectIndex]:null;
        public bool IsTransitioning {get;private set;}
        public bool IsOpen {get;private set;}=true;
        public BookReadingCursor Reading {get;private set;}
        public bool CanTurnPrevious=>IsOpen && !IsTransitioning && Reading==null && SubjectIndex>0;
        public bool CanTurnNext=>IsOpen && !IsTransitioning && Reading==null && SubjectIndex+1<SubjectCount;
        public bool CanFlip=>IsOpen && !IsTransitioning && Reading==null && HasSubject;
        public event Action DestinationChanged;
        public event Action TransitionCompleted;
        public BookNavigationState(IEnumerable<BookOrderedSubject> ordered,BookBookmark initialBookmark=BookBookmark.Colossi)
        {
            if(ordered==null || !Enum.IsDefined(typeof(BookBookmark),initialBookmark))throw new ArgumentException("Invalid book catalog.");
            var entries=ordered.ToArray();if(entries.Any(s=>s==null))throw new ArgumentException("Missing book subject.");
            foreach(var g in entries.GroupBy(s=>s.Bookmark))if(g.Select(s=>s.PageOrder).Distinct().Count()!=g.Count() || g.Select(s=>s.SubjectId).Distinct().Count()!=g.Count())throw new ArgumentException("Duplicate book subject or page order.");
            subjects=Enum.GetValues(typeof(BookBookmark)).Cast<BookBookmark>().ToDictionary(b=>b,b=>(IReadOnlyList<string>)entries.Where(s=>s.Bookmark==b).OrderBy(s=>s.PageOrder).Select(s=>s.SubjectId).ToArray());
            this.initialBookmark=initialBookmark;Bookmark=initialBookmark;Face=BookFace.Overview;
        }
        // Existing catalogs encode their explicit pageOrder as list index.
        public BookNavigationState(IReadOnlyDictionary<BookBookmark,IReadOnlyList<string>> lists,BookBookmark initialBookmark=BookBookmark.Colossi)
            :this(Expand(lists),initialBookmark){}
        private static IEnumerable<BookOrderedSubject> Expand(IReadOnlyDictionary<BookBookmark,IReadOnlyList<string>> lists)
        {if(lists==null)throw new ArgumentNullException(nameof(lists));if(lists.Any(p=>p.Value==null))throw new ArgumentException("Missing book subject list.");return lists.SelectMany(p=>p.Value.Select((id,i)=>new BookOrderedSubject(p.Key,id,i)));}
        private bool Ready=>IsOpen && !IsTransitioning && Reading==null;
        private bool Changed(bool animate)
        {IsTransitioning=animate;DestinationChanged?.Invoke();return true;}
        private bool BookmarkChange(BookBookmark bookmark,bool animate)
        {
            if(!Enum.IsDefined(typeof(BookBookmark),bookmark))throw new ArgumentException("Unknown bookmark.");if(!Ready)return false;
            bool differs=Bookmark!=bookmark || SubjectIndex!=0 || Face!=BookFace.Overview;
            Bookmark=bookmark;SubjectIndex=0;Face=BookFace.Overview;return differs && Changed(animate);
        }
        private bool Turn(int direction,bool animate)
        {if(!Ready || direction==0)return false;int next=SubjectIndex+Math.Sign(direction);if(next<0 || next>=SubjectCount)return false;SubjectIndex=next;Face=BookFace.Overview;return Changed(animate);}
        private bool Flip(bool animate)
        {if(!CanFlip)return false;Face=Face==BookFace.Overview?BookFace.Details:BookFace.Overview;return Changed(animate);}
        public void ChangeBookmark(BookBookmark bookmark)=>BookmarkChange(bookmark,false);
        public void TurnPage(int direction)=>Turn(direction,false);
        public void FlipPage()=>Flip(false);
        public bool RequestBookmark(BookBookmark bookmark)=>BookmarkChange(bookmark,true);
        public bool RequestTurn(int direction)=>Turn(direction,true);
        public bool RequestFlip()=>Flip(true);
        public bool RequestSubject(BookBookmark bookmark,string subjectId)
        {
            if(!Ready)return false;if(!subjects.ContainsKey(bookmark))throw new ArgumentException("Unknown bookmark.");int index=Array.IndexOf(subjects[bookmark].ToArray(),subjectId);
            if(index<0)throw new ArgumentException("Unknown book subject.");bool changed=Bookmark!=bookmark || SubjectIndex!=index || Face!=BookFace.Overview;
            Bookmark=bookmark;SubjectIndex=index;Face=BookFace.Overview;return changed && Changed(true);
        }
        public bool CompleteTransition()
        {if(!IsTransitioning)return false;IsTransitioning=false;TransitionCompleted?.Invoke();return true;}
        public bool BeginReading(string chapterId,int pageCount)
        {if(!Ready || Bookmark!=BookBookmark.Stories || !HasSubject)return false;Reading=new BookReadingCursor(SubjectId,chapterId,pageCount);return true;}
        public void EndReading()=>Reading=null;
        public void Close(){IsTransitioning=false;Reading=null;IsOpen=false;}
        public void Reenter(){IsTransitioning=false;Reading=null;IsOpen=true;Bookmark=initialBookmark;SubjectIndex=0;Face=BookFace.Overview;}
    }
}
