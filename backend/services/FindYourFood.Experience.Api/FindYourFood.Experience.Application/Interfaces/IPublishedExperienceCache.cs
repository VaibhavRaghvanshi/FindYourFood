using FindYourFood.Experience.Application.DTOs;

namespace FindYourFood.Experience.Application.Interfaces;

public interface IPublishedExperienceCache
{
    Task<PublishedExperience?> GetAsync(Func<CancellationToken, Task<PublishedExperience?>> load, CancellationToken cancellationToken);
    void Invalidate();
}
