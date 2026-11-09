namespace FindYourFood.Identity.Application.Interfaces;

public interface IAuthenticatorEnrollment
{
    string CreateSecret();
}
