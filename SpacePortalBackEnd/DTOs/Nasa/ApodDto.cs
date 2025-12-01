using SpacePortalBackEnd.Models.Nasa;

namespace SpacePortalBackEnd.DTOs.Nasa
{
    public record ApodDto(
         DateTime Date,
         string Title,
         string? Explanation,
         string MediaType,
         string Url,
         string? HdUrl,
         string? ThumbUrl,
         string? LocalPath,
         string? Copyright)
    {
        public ApodDto(ApodEntry entity)
            : this(
                entity.Date,
                entity.Title,
                entity.Explanation,
                entity.MediaType,
                entity.Url,
                entity.HdUrl,
                entity.ThumbUrl,
                entity.LocalPath,
                entity.Copyright)
        {
        }
    }
}
