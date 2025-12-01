namespace SpacePortalBackEnd.Models.Nasa
{
    public class ApodEntry
    {
        public long ApodEntryId { get; set; }

        // APOD date (UTC, date-only)
        public DateTime Date { get; set; }

        public string Title { get; set; } = null!;
        public string? Explanation { get; set; }

        // "image" or "video"
        public string MediaType { get; set; } = null!;

        // For images: direct image URL; for videos: video URL (e.g. YouTube/embedded)
        public string Url { get; set; } = null!;

        public string? HdUrl { get; set; }

        // For videos: NASA thumbnail_url; for images: we default this to Url
        public string? ThumbUrl { get; set; }

        public string? Copyright { get; set; }

        // Optional local copy path (e.g. "/apod/2025/11/20251104.jpg")
        public string? LocalPath { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
