namespace FindYourFood.Customer.Domain;

public sealed class CustomerProfile(Guid customerId)
{
    public Guid CustomerId { get; private set; } = customerId;
    public long Revision { get; private set; }
    public string SettingsJson { get; private set; } = "";

    public void Save(string settingsJson)
    {
        SettingsJson = settingsJson;
        Revision++;
    }
}
