using System;
using System.Linq;
namespace NewAster.Core
{
    // Arrays are the wire representation because Unity JsonUtility does not serialize dictionaries.
    [Serializable] public sealed class BookNavigationSave
    {
        public string lastBookmarkId;
        public BookPageState[] pages=Array.Empty<BookPageState>();
        public bool shortTransitions;
    }
    [Serializable] public sealed class BookPageState
    {
        public string bookmarkId,selectedSubjectId,selectedFaceId,searchQuery="",sortMode="default";
        public string[] filterIds=Array.Empty<string>();
        public int selectedColossusLevel=1;
    }
    public sealed partial class FormalCampaignJournal
    {
        public bool CommitBookNavigation(BookNavigationSave state,Func<FormalCampaignSave,bool> save)
        {Ready();var next=Snapshot;next.bookNavigation=state;next.revision=checked(next.revision+1);return Persist(next,save);}
    }
}
