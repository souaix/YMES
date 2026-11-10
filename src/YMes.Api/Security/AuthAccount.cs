namespace YMes.Api.Security;

public sealed record AuthAccount(
    string UserNo,
    string DisplayName,
    string PasswordHash,
    bool IsActive,
    bool MustChangePassword,
    short FailedLoginAttempts,
    DateTime? LockedUntilUtc);
