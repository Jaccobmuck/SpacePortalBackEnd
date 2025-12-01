using System.Text.Json.Serialization;

namespace SpacePortalBackEnd.DTOs.Nasa
{
    public sealed class ApodResponseDto
    {
        public string date { get; set; } = null!;
        public string title { get; set; } = null!;
        public string? explanation { get; set; }
        public string media_type { get; set; } = null!;
        public string url { get; set; } = null!;
        public string? hdurl { get; set; }
        public string? thumbnail_url { get; set; }
        public string? copyright { get; set; }
    }
}
