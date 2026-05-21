using System.Diagnostics.CodeAnalysis;

namespace KRAFT.Results.Web.Client.Features.Meets;

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "URLs are pre-built strings for use in Razor src attributes")]
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "URLs are pre-built strings for use in Razor src attributes")]
public sealed record GalleryPhoto(string ThumbnailUrl, string DisplayUrl, string? Photographer);
