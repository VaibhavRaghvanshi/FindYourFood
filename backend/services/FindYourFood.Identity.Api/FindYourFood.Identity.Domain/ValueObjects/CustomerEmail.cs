using System.ComponentModel.DataAnnotations;

namespace FindYourFood.Identity.Domain;

public sealed record CustomerEmail
{
    private CustomerEmail(string value) => Value = value;

    public string Value { get; }

    public static CustomerEmail? Parse(string? input)
    {
        var value = input?.Trim();
        return value is { Length: > 0 and <= 254 } &&
               !value.Any(char.IsWhiteSpace) &&
               new EmailAddressAttribute().IsValid(value)
            ? new CustomerEmail(value)
            : null;
    }
}
