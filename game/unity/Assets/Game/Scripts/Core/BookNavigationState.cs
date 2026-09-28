using System;
using System.Collections.Generic;

namespace NewAster.Core
{
    public enum BookBookmark { Colossi, Heroines, Gardens, Stories }
    public enum BookFace { Overview, Details }

    /// <summary>
    /// 万物の書の不変ルール：しおりは大分類、めくりは対象、裏返しは同一対象の情報面。
    /// Unity UIや演出から独立して扱う。
    /// </summary>
    public sealed class BookNavigationState
    {
        private readonly IReadOnlyDictionary<BookBookmark, IReadOnlyList<string>> subjects;

        public BookBookmark Bookmark { get; private set; }
        public int SubjectIndex { get; private set; }
        public BookFace Face { get; private set; }
        public string SubjectId => subjects[Bookmark][SubjectIndex];

        public BookNavigationState(IReadOnlyDictionary<BookBookmark, IReadOnlyList<string>> subjects,
            BookBookmark initialBookmark = BookBookmark.Colossi)
        {
            this.subjects = subjects ?? throw new ArgumentNullException(nameof(subjects));
            if (!subjects.ContainsKey(initialBookmark) || subjects[initialBookmark].Count == 0)
                throw new ArgumentException("Initial bookmark must contain a subject.", nameof(initialBookmark));
            Bookmark = initialBookmark;
            SubjectIndex = 0;
            Face = BookFace.Overview;
        }

        public void ChangeBookmark(BookBookmark bookmark)
        {
            if (!subjects.ContainsKey(bookmark) || subjects[bookmark].Count == 0)
                throw new ArgumentException("Bookmark must contain a subject.", nameof(bookmark));
            Bookmark = bookmark;
            SubjectIndex = 0;
            Face = BookFace.Overview;
        }

        public void TurnPage(int direction)
        {
            if (direction == 0) return;
            var count = subjects[Bookmark].Count;
            SubjectIndex = (SubjectIndex + Math.Sign(direction) + count) % count;
            Face = BookFace.Overview;
        }

        public void FlipPage() => Face = Face == BookFace.Overview ? BookFace.Details : BookFace.Overview;
    }
}
