using System.Data;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace YMes.Api.Wip;

public sealed class Wp802ReadOnlyDetailRepository(NpgsqlDataSource dataSource)
{
    private const int MaxRowsPerSection = 500;
    private const string Basis = "\"YMES\".tblwip8dbasis_factory";
    private const string TeamMember = "\"YMES\".tblwip8d_teammember";
    private const string Question = "\"YMES\".tblwip8d_question";
    private const string Action = "\"YMES\".tblwip8d_action";
    private const string ActionDetail = "\"YMES\".tblwip8d_actiondetail";
    private const string RootCause = "\"YMES\".tblwip8d_rootcause";
    private const string Disposition = "\"YMES\".tblwip8d_disposition";
    private const string Qa = "\"YMES\".tblwip8d_qa";
    private const string Prevention = "\"YMES\".tblwip8d_prevention";
    private const string PreventionTrack = "\"YMES\".tblwip8d_preventiontrack";
    private const string AttachFile = "\"YMES\".tblwip8d_attachfile";
    private const string Release = "\"YMES\".tblwip8d_release";
    private const string ReleaseLog = "\"YMES\".tblwip8d_releaselog";

    public Wp802ReadOnlyDetail Detail(string qrsNo)
    {
        var key = Key(qrsNo);
        using var connection = dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead);
        using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY;", connection, transaction))
            readOnly.ExecuteNonQuery();

        var basis = Single(connection, transaction, Basis,
            "qrsno,qrsclass,qrssummary,lotno,lotqty,sampleqty,errorqty,factoryno,areano,mono,customerno,productno,productversion,rono,opno,equipmentno,erfno,holddescription,description,projectdepartment,projectmanager,projectmanagername,curstage,planclosedate,lastupdatedate,summarydescription,disphour,dispcost,summarydepartment,summaryuserno,summaryusername,closetype,closedescription,closedepartment,closeuserno,closeusername,closedate,issuestate,creator,createdate,datastamp,reviser,revisedate",
            key);
        if (basis is null) throw new Wp802ReadOnlyDetailRuleException(404, "找不到指定的 8D 主單。");

        var result = new Wp802ReadOnlyDetail(
            basis,
            Rows(connection, transaction, TeamMember,
                "qrsno,membertype,departmentno,customerno,vendorno,memberno,membername,telno,email,taskdesc,description,pmflag,creator,createdate",
                "membertype,memberno COLLATE \"C\"", key),
            Rows(connection, transaction, Question,
                "qrsno,sequenceno,questiondesc,creator,createdate", "sequenceno", key),
            Rows(connection, transaction, Action,
                "qrsno,mainsn,planaction,dutydepartment,dutymember,dutymembername,planfinishdate,description,actualfinishdate,actionresult,qauserno,qausername,qamemo,creator,createdate",
                "mainsn", key),
            Rows(connection, transaction, ActionDetail,
                "qrsno,mainsn,sequenceno,subaction,dutydepartment,dutymember,dutymembername,planfinishdate,description,actualfinishdate,actionresult,qauserno,qausername,qamemo,creator,createdate",
                "mainsn,sequenceno", key),
            Rows(connection, transaction, RootCause,
                "qrsno,sequenceno,reasonno,frequency,description,creator,createdate", "sequenceno,reasonno COLLATE \"C\"", key),
            Rows(connection, transaction, Disposition,
                "qrsno,sequenceno,reasonno,disposition,dutydepartment,dutymember,dutymembername,planfinishdate,description,actualfinishdate,qadescription,qaresult,qamember,qamembername,creator,createdate",
                "sequenceno,reasonno COLLATE \"C\"", key),
            Rows(connection, transaction, Qa,
                "qrsno,sequenceno,reasonno,dispositionsn,qadescription,qaresult,qadepartment,verifier,verifiername,verifydate,lotno,ipqcformno,disphours,consumecost,memo,creator,createdate",
                "sequenceno,reasonno COLLATE \"C\",dispositionsn", key),
            Rows(connection, transaction, Prevention,
                "qrsno,sequenceno,preventionitem,description,maindepartment,director,directorname,planfinishdate,creator,createdate",
                "sequenceno", key),
            Rows(connection, transaction, PreventionTrack,
                "qrsno,mainsn,sequenceno,finishdate,trackdepartment,tracker,trackername,tracedescription,traceresult,creator,createdate",
                "mainsn,sequenceno", key),
            Rows(connection, transaction, AttachFile,
                "qrsno,stageno,attachfilename,creator,createdate", "stageno COLLATE \"C\",attachfilename COLLATE \"C\"", key),
            Single(connection, transaction, Release,
                "qrsno,releasememo,releasedepartment,releaser,releasername,releasedate,description,issuestate,creator,createdate", key),
            Single(connection, transaction, ReleaseLog,
                "qrsno,releasetype,releasememo,releasedepartment,releaser,releasername,releasedate,description,creator,createdate", key));

        transaction.Commit();
        return result;
    }

    private static IReadOnlyDictionary<string, object?>? Single(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string relation, string columns, string qrsNo)
    {
        using var command = Command(connection, transaction,
            $"SELECT {columns} FROM {relation} WHERE qrsno=@qrsNo LIMIT 2;", qrsNo);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var row = Read(reader);
        if (reader.Read()) throw new InvalidOperationException("A WP802 single-row source exceeded its verified key.");
        return row;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows(NpgsqlConnection connection,
        NpgsqlTransaction transaction, string relation, string columns, string orderBy, string qrsNo)
    {
        using var command = Command(connection, transaction,
            $"SELECT {columns} FROM {relation} WHERE qrsno=@qrsNo ORDER BY {orderBy} LIMIT @maxRowsPlusOne;", qrsNo);
        command.Parameters.Add("maxRowsPlusOne", NpgsqlDbType.Integer).Value = MaxRowsPerSection + 1;
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (rows.Count == MaxRowsPerSection)
                throw new Wp802ReadOnlyDetailRuleException(413, $"8D 明細單一頁籤超過 {MaxRowsPerSection} 筆，請縮小查詢範圍後重試。");
            rows.Add(Read(reader));
        }
        return rows;
    }

    private static NpgsqlCommand Command(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string sql, string qrsNo)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add("qrsNo", NpgsqlDbType.Varchar).Value = qrsNo;
        return command;
    }

    private static IReadOnlyDictionary<string, object?> Read(NpgsqlDataReader reader)
    {
        var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.Ordinal);
        for (var i = 0; i < reader.FieldCount; i++)
            row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
        return row;
    }

    private static string Key(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw Bad("QRSNo 不可空白。");
        var key = value.Trim();
        try { _ = new UTF8Encoding(false, true).GetByteCount(key); }
        catch (EncoderFallbackException) { throw Bad("QRSNo 含無效 Unicode。"); }
        if (key.EnumerateRunes().Count() > 30 || key.Any(char.IsControl) || key.Contains('\0'))
            throw Bad("QRSNo 長度或字元不正確（最多 30 字元）。");
        return key;
    }

    private static Wp802ReadOnlyDetailRuleException Bad(string message) => new(400, message);
}

public sealed record Wp802ReadOnlyDetail(
    IReadOnlyDictionary<string, object?> Basis,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> TeamMembers,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Questions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Actions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> ActionDetails,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> RootCauses,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Dispositions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> QAs,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Preventions,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> PreventionTracks,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Attachments,
    IReadOnlyDictionary<string, object?>? Release,
    IReadOnlyDictionary<string, object?>? ReleaseLog);

public sealed class Wp802ReadOnlyDetailRuleException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
