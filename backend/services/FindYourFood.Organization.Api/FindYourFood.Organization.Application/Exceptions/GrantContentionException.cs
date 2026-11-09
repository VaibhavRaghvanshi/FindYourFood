namespace FindYourFood.Organization.Application;

public sealed class GrantContentionException(Exception inner) : Exception("Permission records are busy.", inner);
