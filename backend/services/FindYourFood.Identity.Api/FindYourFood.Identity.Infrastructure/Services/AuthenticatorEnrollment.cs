using FindYourFood.Identity.Application.Interfaces;
using OtpNet;

namespace FindYourFood.Identity.Infrastructure.Services;

internal sealed class AuthenticatorEnrollment : IAuthenticatorEnrollment
{
    public string CreateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
}
