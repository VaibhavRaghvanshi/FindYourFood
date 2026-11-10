using FindYourFood.Experience.Application.DTOs;
using FindYourFood.Experience.Application.Interfaces;

namespace FindYourFood.Experience.Infrastructure.Caching;

internal sealed class PublishedExperienceCache(TimeProvider clock) : IPublishedExperienceCache, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1);
    private long _generation;
    private Entry? _entry;

    public async Task<PublishedExperience?> GetAsync(
        Func<CancellationToken, Task<PublishedExperience?>> load, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var generation = Volatile.Read(ref _generation);
            if (_entry is { } current && current.Generation == generation && clock.GetUtcNow() < current.ExpiresAt)
            {
                return current.Publication;
            }

            var publication = await load(cancellationToken);
            _entry = new Entry(generation, clock.GetUtcNow().AddSeconds(30), publication);
            return publication;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => Interlocked.Increment(ref _generation);

    public void Dispose() => _gate.Dispose();

    private sealed record Entry(long Generation, DateTimeOffset ExpiresAt, PublishedExperience? Publication);
}
