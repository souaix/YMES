using System.Text;

namespace YMes.Api.Security;

public static class PasswordPolicy
{
    public const int MinimumLength = 4;
    public const int MaximumLength = 128;

    private static readonly HashSet<string> BlockedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "passwordpassword",
        "123456789012345",
        "1234567890123456",
        "111111111111111",
        "aaaaaaaaaaaaaaa",
        "qwertyuiopasdf",
        "qwertyuiopasdfghjkl",
        "qwerty123456789",
        "adminadminadmin",
        "ymesymesymesymes",
        "welcome12345678",
        "welcome123456789",
        "changeme1234567",
        "changemechangeme",
        "letmeinletmeinletmein",
        "iloveyouiloveyou"
    };

    public static string NormalizeForHash(string password) => password.Normalize(NormalizationForm.FormC);

    public static int CountCodePoints(string value)
    {
        var count = 0;
        foreach (var _ in value.EnumerateRunes())
        {
            count++;
        }

        return count;
    }

    public static string? GetValidationError(string password, string? userNo = null)
    {
        var normalizedPassword = NormalizeForHash(password);
        var length = CountCodePoints(normalizedPassword);
        if (length < MinimumLength)
        {
            return "密碼至少需要 4 個字元。";
        }

        if (length > MaximumLength)
        {
            return "密碼最多可輸入 128 個字元。";
        }

        var comparablePassword = normalizedPassword.Normalize(NormalizationForm.FormKC);
        var containsAppName = comparablePassword.Contains("ymes", StringComparison.OrdinalIgnoreCase);
        var normalizedUserNo = userNo?.Normalize(NormalizationForm.FormKC);
        var containsUserNo = normalizedUserNo is { Length: >= 3 } &&
                             comparablePassword.Contains(normalizedUserNo, StringComparison.OrdinalIgnoreCase);
        if (BlockedPasswords.Contains(comparablePassword) || containsAppName || containsUserNo)
        {
            return "請勿使用常見密碼，也請避免在密碼中包含 YMES 或帳號。";
        }

        return null;
    }
}
