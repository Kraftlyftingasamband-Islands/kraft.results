using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Photos;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetPhotos;

internal sealed class GetMeetPhotosHandler(ResultsDbContext dbContext)
{
    public async Task<MeetPhotos?> Handle(string slug, CancellationToken cancellationToken)
    {
        string? meetTitle = await dbContext.Set<Meet>()
            .Where(m => m.Slug == slug)
            .Select(m => m.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (meetTitle is null)
        {
            return null;
        }

        List<PhotoSummary> photos = await dbContext.Set<Photo>()
            .Where(p => p.Meet!.Slug == slug)
            .Where(p => p.ImageFilename != null)
            .Where(p => p.ImageFilename != string.Empty)
            .OrderBy(p => p.CreatedOn)
            .Select(p => new PhotoSummary(p.PhotoId, p.ImageFilename!, p.Photographer))
            .ToListAsync(cancellationToken);

        return new MeetPhotos(meetTitle, photos);
    }
}
