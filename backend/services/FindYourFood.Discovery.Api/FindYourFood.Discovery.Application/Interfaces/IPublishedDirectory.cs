using FindYourFood.Discovery.Domain;

namespace FindYourFood.Discovery.Application.Interfaces;

public interface IPublishedDirectory
{
    Task<SearchDocument[]> FetchAsync(string city, CancellationToken ct);
}
