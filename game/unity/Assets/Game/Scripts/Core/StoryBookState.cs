using System;
using System.Collections.Generic;
using System.Linq;

namespace NewAster.Core
{
    public sealed class StoryBookChapter
    {
        public string StoryId { get; }
        public IReadOnlyList<string> PageTextIds { get; }

        public StoryBookChapter(string storyId, IEnumerable<string> pageTextIds)
        {
            if (string.IsNullOrWhiteSpace(storyId)) throw new ArgumentException("A story id is required.", nameof(storyId));
            PageTextIds = (pageTextIds ?? throw new ArgumentNullException(nameof(pageTextIds))).Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
            if (PageTextIds.Count == 0) throw new ArgumentException("A chapter needs at least one page.", nameof(pageTextIds));
            StoryId = storyId;
        }
    }

    /// <summary>本文を本としてめくる状態。万物の書のしおり／対象／情報面とは独立した文章ページ遷移。</summary>
    public sealed class StoryBookState
    {
        private StoryBookChapter _chapter;
        public string StoryId => _chapter?.StoryId;
        public int PageIndex { get; private set; }
        public string CurrentPageTextId => _chapter == null ? null : _chapter.PageTextIds[PageIndex];
        public bool IsAtLastPage => _chapter != null && PageIndex == _chapter.PageTextIds.Count - 1;

        public bool TryOpen(ProgressState progress, StoryBookChapter chapter)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (chapter == null) throw new ArgumentNullException(nameof(chapter));
            if (!progress.UnlockedStoryIds.Contains(chapter.StoryId)) return false;
            _chapter = chapter;
            PageIndex = 0;
            return true;
        }

        public bool TurnPage(int direction)
        {
            if (_chapter == null || direction == 0) return false;
            var nextPage = Math.Max(0, Math.Min(_chapter.PageTextIds.Count - 1, PageIndex + Math.Sign(direction)));
            if (nextPage == PageIndex) return false;
            PageIndex = nextPage;
            return true;
        }

        public bool TryMarkRead(ProgressState progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            return IsAtLastPage && progress.MarkStoryRead(StoryId);
        }
    }
}
