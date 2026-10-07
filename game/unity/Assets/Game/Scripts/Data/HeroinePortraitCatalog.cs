using System;
using System.Linq;

namespace NewAster.Data
{
    [Serializable]
    public sealed class HeroinePortraitCatalog
    {
        public const int MinimumWidth = 1536, MinimumHeight = 1024, MinimumFacePixels = 128;
        public const float SourceAspect = 1.5f, MaximumDisplayScale = 1f;
        public int schemaVersion;
        public float faceHeightRatio, faceCenterYRatio;
        public HeroinePortraitDef[] entries;
        // These five existing forms keep their previous art pending a separately requested replacement.
        // A newly added heroine cannot opt out of the landscape portrait contract.
        private static readonly string[] LegacyIds = { "heroine.nighthawk", "heroine.slayer-swim", "heroine.arcane", "heroine.arcane-academy", "heroine.shangrila" };

        public void Validate()
        {
            if (schemaVersion != 2 || entries == null || entries.Length == 0 ||
                !Finite(faceHeightRatio) || faceHeightRatio <= 0 || faceHeightRatio >= 1 ||
                !Finite(faceCenterYRatio) || faceCenterYRatio <= 0 || faceCenterYRatio >= 1 ||
                entries.Any(e => e == null) || entries.Select(e => e.heroineId).Distinct().Count() != entries.Length)
                throw new ArgumentException("Invalid heroine portrait catalog.");
            foreach (var e in entries)
                if (string.IsNullOrEmpty(e.heroineId) || string.IsNullOrEmpty(e.resourcePath) ||
                    !e.resourcePath.StartsWith("Illustrations/", StringComparison.Ordinal) || !e.resourcePath.Contains("-portrait-") || e.resourcePath.Contains("..") ||
                    !Finite(e.faceHeight) || e.faceHeight <= 0 || e.faceHeight > 1 ||
                    !Finite(e.faceCenterX) || e.faceCenterX <= 0 || e.faceCenterX >= 1 ||
                    !Finite(e.faceCenterY) || e.faceCenterY <= 0 || e.faceCenterY >= 1 ||
                    (e.legacyPortrait && !LegacyIds.Contains(e.heroineId)))
                    throw new ArgumentException("Invalid heroine portrait definition: " + e.heroineId);
        }
        public HeroinePortraitDef Entry(string id) => entries.SingleOrDefault(e => e.heroineId == id)
            ?? throw new ArgumentException("Missing selection portrait: " + id);
        public void ValidateSource(HeroinePortraitDef e, int width, int height)
        {
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
                throw new ArgumentException("Portrait exceeds native import limits: " + e.heroineId);
            if (e.legacyPortrait) return;
            if (width < MinimumWidth || height < MinimumHeight || Math.Abs(width / (float)height - SourceAspect) > .001f ||
                height * e.faceHeight < MinimumFacePixels)
                throw new ArgumentException("Portrait requires native 3:2, at least 1536x1024 and 128 face pixels: " + e.heroineId);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
    [Serializable]
    public sealed class HeroinePortraitDef
    {
        public string heroineId, resourcePath;
        public float faceCenterX, faceCenterY, faceHeight;
        public bool legacyPortrait;
    }
}
