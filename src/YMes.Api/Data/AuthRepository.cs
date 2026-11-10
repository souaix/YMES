using Npgsql;
using YMes.Api.Security;

namespace YMes.Api.Data;

public sealed class AuthRepository(NpgsqlDataSource dataSource)
{
    public async Task<AuthAccount?> FindByUserNoAsync(string userNo, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.userno, u.username, u.password,
                   u.issuestate = 1,
                   u.resetpassword = 1,
                   COALESCE(s.failed_login_attempts, 0::smallint),
                   s.locked_until
            FROM "YMES".tblusruserbasis AS u
            LEFT JOIN "YMES".ym_auth_login_state AS s
                ON lower(s.user_no) = lower(u.userno)
            WHERE lower(u.userno) = lower(@userNo)
            LIMIT 1;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("userNo", userNo);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthAccount(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetBoolean(3),
            reader.GetBoolean(4),
            reader.GetInt16(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6));
    }

    public async Task<bool> IsDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT to_regclass('"YMES".tblusruserbasis') IS NOT NULL
               AND to_regclass('"YMES".ym_auth_login_state') IS NOT NULL;
            """);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task RecordLoginFailureAsync(string userNo, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO "YMES".ym_auth_login_state AS s
                (user_no, failed_login_attempts, locked_until)
            SELECT u.userno, 1, NULL
            FROM "YMES".tblusruserbasis AS u
            WHERE lower(u.userno) = lower(@userNo)
            LIMIT 1
            ON CONFLICT (user_no) DO UPDATE
            SET failed_login_attempts =
                    CASE WHEN s.failed_login_attempts + 1 >= 5 THEN 0
                         ELSE s.failed_login_attempts + 1 END,
                locked_until =
                    CASE WHEN s.failed_login_attempts + 1 >= 5
                         THEN now() + interval '15 minutes'
                         ELSE s.locked_until END,
                updated_at = now();
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("userNo", userNo);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RecordSuccessfulLoginAsync(string userNo, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO "YMES".ym_auth_login_state
                (user_no, failed_login_attempts, locked_until, last_login_at)
            SELECT u.userno, 0, NULL, now()
            FROM "YMES".tblusruserbasis AS u
            WHERE lower(u.userno) = lower(@userNo)
            LIMIT 1
            ON CONFLICT (user_no) DO UPDATE
            SET failed_login_attempts = 0,
                locked_until = NULL,
                last_login_at = now(),
                updated_at = now();
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("userNo", userNo);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        string userNo,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var credentialCommand = new NpgsqlCommand("""
            UPDATE "YMES".tblusruserbasis
            SET password = @passwordHash,
                changedate = now(),
                resetpassword = 0
            WHERE lower(userno) = lower(@userNo);
            """, connection, transaction))
        {
            credentialCommand.Parameters.AddWithValue("userNo", userNo);
            credentialCommand.Parameters.AddWithValue("passwordHash", passwordHash);
            await credentialCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var stateCommand = new NpgsqlCommand("""
            UPDATE "YMES".ym_auth_login_state
            SET failed_login_attempts = 0,
                locked_until = NULL,
                updated_at = now()
            WHERE lower(user_no) = lower(@userNo);
            """, connection, transaction))
        {
            stateCommand.Parameters.AddWithValue("userNo", userNo);
            await stateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RehashPasswordAsync(
        string userNo,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE "YMES".tblusruserbasis
            SET password = @passwordHash,
                changedate = now()
            WHERE lower(userno) = lower(@userNo);
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("userNo", userNo);
        command.Parameters.AddWithValue("passwordHash", passwordHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task WriteAuditEventAsync(
        string? userNo,
        string eventType,
        bool succeeded,
        string environment,
        string? clientIp,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO "YMES".ym_auth_audit_events
                (user_no, event_type, succeeded, environment_name, client_ip, user_agent)
            VALUES
                (@userNo, @eventType, @succeeded, @environment, @clientIp, @userAgent);
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.Add("userNo", NpgsqlTypes.NpgsqlDbType.Varchar).Value =
            (object?)userNo ?? DBNull.Value;
        command.Parameters.AddWithValue("eventType", eventType);
        command.Parameters.AddWithValue("succeeded", succeeded);
        command.Parameters.AddWithValue("environment", environment);
        command.Parameters.Add("clientIp", NpgsqlTypes.NpgsqlDbType.Varchar).Value =
            (object?)clientIp ?? DBNull.Value;
        command.Parameters.Add("userAgent", NpgsqlTypes.NpgsqlDbType.Varchar).Value =
            (object?)userAgent ?? DBNull.Value;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
