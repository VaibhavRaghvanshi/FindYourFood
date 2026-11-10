using System.Text.Json;
using FindYourFood.Customer.Application.DTOs;
using FindYourFood.Customer.Application.Interfaces;
using FindYourFood.Customer.Domain;

namespace FindYourFood.Customer.Application.Services;

public sealed class CustomerProfiles(IProfileRepository profiles, IUnitOfWork unitOfWork)
{
    public async Task<CustomerSnapshot> GetAsync(Guid subject, CancellationToken ct)
    {
        var profile = await profiles.FindAsync(subject, ct);
        return profile is null ? new(0, ProfileSettings.Empty)
            : new(profile.Revision, JsonSerializer.Deserialize<ProfileSettings>(profile.SettingsJson)!);
    }

    public async Task<ProfileSaveResult> SaveAsync(Guid subject, CustomerSnapshot input, CancellationToken ct)
    {
        if (input.Revision < 0 || input.Settings is null || !input.Settings.IsValid())
        {
            return new(ProfileSaveStatus.Invalid);
        }

        var profile = await profiles.FindAsync(subject, ct);
        if ((profile?.Revision ?? 0) != input.Revision)
        {
            return new(ProfileSaveStatus.Conflict);
        }

        if (profile is null)
        {
            profile = new CustomerProfile(subject);
            profiles.Add(profile);
        }

        profile.Save(JsonSerializer.Serialize(input.Settings));
        return await unitOfWork.TrySaveAsync(ct)
            ? new(ProfileSaveStatus.Saved, new(profile.Revision, input.Settings))
            : new(ProfileSaveStatus.Conflict);
    }
}
