namespace YMes.Api.Security;

public sealed record LoginRequest(string? Username, string? Password);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

public sealed record AuthenticatedUserResponse(
    string UserNo,
    string DisplayName,
    bool MustChangePassword);
