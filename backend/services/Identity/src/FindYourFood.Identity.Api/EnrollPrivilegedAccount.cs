using System.Text;
using FindYourFood.Identity.Application;

namespace FindYourFood.Identity.Api;

internal static class EnrollPrivilegedAccount
{
    public static async Task<int> RunAsync(IServiceProvider services, bool owner)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("Enrollment requires an interactive terminal.");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<PrivilegedAccounts>();
        var authenticator = scope.ServiceProvider.GetRequiredService<IAuthenticatorEnrollment>();
        Console.Write(owner ? "Owner email: " : "Staff email: ");
        var email = Console.ReadLine();
        var password = ReadHidden("New password (12–128 characters): ", 128);
        if (!string.Equals(password, ReadHidden("Confirm password: ", 128), StringComparison.Ordinal))
        {
            Console.Error.WriteLine("The passwords differ.");
            return 1;
        }

        var secret = authenticator.CreateSecret();
        Console.WriteLine("Add a time-based FindYourFood account in your authenticator using this setup key.");
        Console.WriteLine(secret);
        var code = ReadHidden("Enter the current six-digit code: ", 6);
        var account = await accounts.EnrollAsync(email, password, secret, code, owner);
        if (account is null)
        {
            Console.Error.WriteLine("Enrollment failed. Check the email and authenticator code, and confirm the account is available.");
            return 1;
        }

        Console.WriteLine($"Enrolled account {account.Id:D}. Wait for the next authenticator code before signing in.");
        return 0;
    }

    private static string ReadHidden(string prompt, int maxLength)
    {
        Console.Write(prompt);
        var input = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input.ToString();
            }

            if (key.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input.Length--;
            }
            else if (!char.IsControl(key.KeyChar) && input.Length < maxLength + 1)
            {
                input.Append(key.KeyChar);
            }
        }
    }
}
