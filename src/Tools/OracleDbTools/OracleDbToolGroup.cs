using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Text.Json;
using Micube.MCP.SDK.Abstracts;
using Micube.MCP.SDK.Attributes;
using Micube.MCP.SDK.Interfaces;
using Micube.MCP.SDK.Models;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Types;
using System.Reflection;

namespace OracleDbTools;

[McpToolGroup("OracleDbTools", "oracle.json", "Oracle database access tools")]
public class OracleDbToolGroup : BaseToolGroup
{
    public override string GroupName => "OracleDbTools";

    private string? _connectionString;
    private int _commandTimeoutSeconds = 30;
    private int _maxResultRows = 10000;

    public OracleDbToolGroup(IMcpLogger logger) : base(logger)
    {
        // OracleConfiguration.TnsAdmin 설정
        // TNS_ADMIN 경로는 DLL이 위치한 디렉토리로 설정 - 이렇게 해야 같은 경로의 tnsnames.ora 파일을 사용할 수 있습니다.
        var dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        OracleConfiguration.TnsAdmin = dllDirectory;
        Logger.LogInfo("[OracleTools] Configuration initialized with TNS_ADMIN: " + OracleConfiguration.TnsAdmin);
    }

    protected override void OnConfigure(JsonElement? config)
    {
        if (!config.HasValue) return;

        // Connection string 설정
        if (config.Value.TryGetProperty("connectionString", out var conn))
        {
            _connectionString = conn.GetString();
            Logger.LogInfo("[OracleTools] Connection string configured.");
        }

        // Command timeout 설정
        if (config.Value.TryGetProperty("commandTimeoutSeconds", out var timeout))
        {
            _commandTimeoutSeconds = timeout.GetInt32();
            Logger.LogInfo($"[OracleTools] Command timeout set to {_commandTimeoutSeconds} seconds.");
        }

        // 최대 결과 행 수 설정
        if (config.Value.TryGetProperty("maxResultRows", out var maxRows))
        {
            _maxResultRows = maxRows.GetInt32();
            Logger.LogInfo($"[OracleTools] Max result rows set to {_maxResultRows}.");
        }
    }

    /// <summary>
    /// SQL SELECT 쿼리를 실행하고 결과를 반환합니다.
    /// </summary>
    [McpTool("Query")]
    public async Task<object> QueryAsync(Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(_connectionString))
            return ToolCallResult.Fail("Connection string not configured.");

        if (!parameters.TryGetValue("sql", out var sqlObj) || sqlObj is not string sql || string.IsNullOrWhiteSpace(sql))
            return ToolCallResult.Fail("Parameter 'sql' is required and must be a non-empty string.");

        // 보안을 위한 기본적인 SQL 검증
        if (!IsSelectQuery(sql))
            return ToolCallResult.Fail("Only SELECT queries are allowed for Query method. Use Command method for DML/DDL operations.");

        try
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new OracleCommand(sql, conn)
            {
                CommandTimeout = _commandTimeoutSeconds
            };

            using var reader = await cmd.ExecuteReaderAsync();
            var results = new List<Dictionary<string, object>>();
            var rowCount = 0;

            while (await reader.ReadAsync() && rowCount < _maxResultRows)
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    // Oracle 특정 타입들을 JSON 직렬화 가능한 타입으로 변환
                    row[reader.GetName(i)] = ConvertOracleValue(value);
                }
                results.Add(row);
                rowCount++;
            }

            var metadata = new
            {
                rowCount = results.Count,
                columnCount = reader.FieldCount,
                columns = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => new
                    {
                        name = reader.GetName(i),
                        type = reader.GetDataTypeName(i),
                        fieldType = reader.GetFieldType(i).Name
                    }).ToArray(),
                truncated = rowCount >= _maxResultRows
            };

            Logger.LogInfo($"[OracleTools] Query executed successfully. Returned {results.Count} rows.");

            return new { data = results, metadata };
        }
        catch (OracleException ex)
        {
            Logger.LogError($"[OracleTools] Oracle error: {ex.Message} (Code: {ex.Number})", ex);
            return ToolCallResult.Fail($"Oracle error {ex.Number}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError("[OracleTools] Query execution failed", ex);
            return ToolCallResult.Fail($"Query failed: {ex.Message}");
        }
    }

    /// <summary>
    /// SQL 명령을 실행합니다 (CREATE TABLE, INSERT, UPDATE, DELETE 등).
    /// </summary>
    [McpTool("Command")]
    public async Task<ToolCallResult> CommandAsync(Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(_connectionString))
            return ToolCallResult.Fail("Connection string not configured.");

        if (!parameters.TryGetValue("sql", out var sqlObj) || sqlObj is not string sql || string.IsNullOrWhiteSpace(sql))
            return ToolCallResult.Fail("Parameter 'sql' is required and must be a non-empty string.");

        // 위험한 명령어 차단
        if (IsDangerousCommand(sql))
            return ToolCallResult.Fail("Dangerous commands (DROP DATABASE, TRUNCATE, etc.) are not allowed for safety.");

        try
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new OracleCommand(sql, conn)
            {
                CommandTimeout = _commandTimeoutSeconds
            };

            var affected = await cmd.ExecuteNonQueryAsync();

            Logger.LogInfo($"[OracleTools] Command executed successfully: {sql.Substring(0, Math.Min(50, sql.Length))}...");
            return ToolCallResult.Success($"Command executed successfully. Rows affected: {affected}");
        }
        catch (OracleException ex)
        {
            Logger.LogError($"[OracleTools] Oracle command error: {ex.Message} (Code: {ex.Number})", ex);
            return ToolCallResult.Fail($"Oracle error {ex.Number}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError("[OracleTools] Command execution failed", ex);
            return ToolCallResult.Fail($"Command failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 데이터베이스 스키마 정보를 조회합니다 (테이블, 컬럼 등).
    /// </summary>
    [McpTool("GetDatabaseInfo")]
    public async Task<object> GetDatabaseInfoAsync(Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(_connectionString))
            return ToolCallResult.Fail("Connection string not configured.");

        var dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string filePath = Path.Combine(dllDirectory, "sample-database-info.json");
        var readLines = await File.ReadAllLinesAsync(filePath);

        return new { readLines };

        // 아래는 전체 DB를 가져오는 것이지만, 실제의 경우에는 반드시 사용하는 Table에 대한 정보만 가져와야 합니다.
        // 그래서 위와 같이 Table 정보는 json으로 작성하도록 합니다. 

        try
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            // 테이블 목록 조회
            var tables = new List<string>();
            using (var cmd = new OracleCommand("SELECT table_name FROM user_tables ORDER BY table_name", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    tables.Add(reader.GetString(0));
            }

            // 컬럼 정보 조회
            var columns = new List<object>();
            using (var cmd = new OracleCommand(@"
                SELECT table_name, column_name, data_type, 
                       COALESCE(data_length, data_precision, 0) as length,
                       nullable, column_id
                FROM user_tab_columns 
                ORDER BY table_name, column_id", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    columns.Add(new
                    {
                        table = reader.GetString(0),
                        column = reader.GetString(1),
                        type = reader.GetString(2),
                        length = reader.GetDecimal(3),
                        nullable = reader.GetString(4) == "Y",
                        position = reader.GetInt32(5)
                    });
                }
            }

            // 인덱스 정보 조회
            var indexes = new List<object>();
            using (var cmd = new OracleCommand(@"
                SELECT index_name, table_name, uniqueness, status
                FROM user_indexes 
                WHERE table_name IN (SELECT table_name FROM user_tables)
                ORDER BY table_name, index_name", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    indexes.Add(new
                    {
                        indexName = reader.GetString(0),
                        tableName = reader.GetString(1),
                        unique = reader.GetString(2) == "UNIQUE",
                        status = reader.GetString(3)
                    });
                }
            }

            // 데이터베이스 기본 정보
            var dbInfo = new Dictionary<string, object>();
            using (var cmd = new OracleCommand(@"
                SELECT 
                    (SELECT name FROM v$database) as db_name,
                    (SELECT version FROM v$instance) as db_version,
                    (SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM dual) as current_schema,
                    (SELECT SYS_CONTEXT('USERENV', 'SESSION_USER') FROM dual) as session_user
                FROM dual", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        dbInfo[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                }
            }

            Logger.LogInfo($"[OracleTools] Retrieved database schema info: {tables.Count} tables, {columns.Count} columns, {indexes.Count} indexes.");

            return new
            {
                databaseInfo = dbInfo,
                tables = tables,
                tableCount = tables.Count,
                columnCount = columns.Count,
                indexCount = indexes.Count,
                columns = columns,
                indexes = indexes
            };
        }
        catch (OracleException ex)
        {
            Logger.LogError($"[OracleTools] Oracle error while retrieving DB info: {ex.Message} (Code: {ex.Number})", ex);
            return ToolCallResult.Fail($"Oracle error {ex.Number}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError("[OracleTools] Failed to retrieve database schema info", ex);
            return ToolCallResult.Fail($"Schema info retrieval failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 연결 테스트를 수행합니다.
    /// </summary>
    [McpTool("TestConnection")]
    public async Task<ToolCallResult> TestConnectionAsync(Dictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(_connectionString))
            return ToolCallResult.Fail("Connection string not configured.");

        try
        {
            using var conn = new OracleConnection(_connectionString);
            var startTime = DateTime.Now;
            await conn.OpenAsync();
            var connectionTime = DateTime.Now - startTime;

            // 간단한 쿼리로 연결 확인
            using var cmd = new OracleCommand("SELECT 1 FROM dual", conn);
            await cmd.ExecuteScalarAsync();

            Logger.LogInfo("[OracleTools] Connection test successful.");
            return ToolCallResult.Success($"Connection successful. Time: {connectionTime.TotalMilliseconds:F0}ms, State: {conn.State}");
        }
        catch (OracleException ex)
        {
            Logger.LogError($"[OracleTools] Oracle connection error: {ex.Message} (Code: {ex.Number})", ex);
            return ToolCallResult.Fail($"Oracle connection error {ex.Number}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError("[OracleTools] Connection test failed", ex);
            return ToolCallResult.Fail($"Connection failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// SQL이 SELECT 쿼리인지 확인합니다.
    /// </summary>
    private static bool IsSelectQuery(string sql)
    {
        var trimmed = sql.Trim().ToUpperInvariant();
        return trimmed.StartsWith("SELECT") || trimmed.StartsWith("WITH");
    }

    /// <summary>
    /// 위험한 SQL 명령어인지 확인합니다.
    /// </summary>
    private static bool IsDangerousCommand(string sql)
    {
        var dangerous = new[]
        {
            "DROP DATABASE", "DROP SCHEMA", "DROP USER", "TRUNCATE",
            "SHUTDOWN", "STARTUP", "ALTER SYSTEM", "GRANT SYSDBA", "REVOKE SYSDBA"
        };

        var upperSql = sql.ToUpperInvariant();
        return dangerous.Any(cmd => upperSql.Contains(cmd));
    }

    /// <summary>
    /// Oracle 특정 데이터 타입을 JSON 직렬화 가능한 타입으로 변환합니다.
    /// </summary>
    private static object? ConvertOracleValue(object? value)
    {
        return value switch
        {
            DBNull => null,
            OracleDecimal oracleDecimal => oracleDecimal.IsNull ? null : (object)oracleDecimal.Value,
            OracleString oracleString => oracleString.IsNull ? null : oracleString.Value,
            OracleDate oracleDate => oracleDate.IsNull ? null : oracleDate.Value,
            OracleTimeStamp oracleTimeStamp => oracleTimeStamp.IsNull ? null : oracleTimeStamp.Value,
            OracleBlob oracleBlob => oracleBlob.IsNull ? null : "<BLOB data>",
            OracleClob oracleClob => oracleClob.IsNull ? null : "<CLOB data>",
            _ => value
        };
    }

    #endregion
}