using CMI.Utilities.ActaPro.Properties;
using Serilog;
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace CMI.Utilities.ActaPro;

public class ActaProMappingProvider
{
    private readonly string connectionString;

    public ActaProMappingProvider() : this(Settings.Default.MappingTableDirectory)
    {
    }

    public ActaProMappingProvider(string mappingTableDirectory)
    {
        var probe = Path.Combine(mappingTableDirectory, "mappingTable.db");
        if (File.Exists(probe))
        {
            connectionString = probe;
            Log.Debug("The configured table path {connectionString} was found", connectionString);
            return;
        }
        // Only for testing
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        Log.Warning("The configured table path {probe} was not found. Use testing mappingTable", probe);
        probe = Path.Combine(baseDirectory, "mappingTable.db");
        if (File.Exists(probe))
        {
            connectionString = probe;
            return;
        }

        probe = Path.Combine(baseDirectory, "bin", "mappingTable.db");
        if (File.Exists(probe))
        {
            connectionString = probe;
            return;
        }

        probe = Path.Combine(baseDirectory, "bin", "Debug", "mappingTable.db");

        if (File.Exists(probe))
        {
            connectionString = probe;
            return;
        }

        throw new ArgumentException("The database file mappingTable.db could not be found.");

    }
    public long GetScopeId(string id)
    {
        SQLitePCL.Batteries.Init();
        using var connection =  new SqliteConnection($"Data Source={connectionString};");
        
        connection.Open();
        var sql = "SELECT ScopeID FROM MappingTable WHERE ActaProId = $id";
        
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("$id", id);

        var result = cmd.ExecuteScalar();
        if (!long.TryParse(result?.ToString(), out var scopeId))
        {
            scopeId = -1L;
        }

        return scopeId;
    }

    public string GetActaProId(string scopeId)
    {
        var actaProId = string.Empty;

        if (long.TryParse(scopeId, out _))
        {
            SQLitePCL.Batteries.Init();
            using var connection = new SqliteConnection($"Data Source={connectionString};");
            connection.Open();
            var sql = "SELECT ActaProId FROM MappingTable WHERE ScopeID = $scopeId";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("$scopeId", scopeId);

            var result = cmd.ExecuteScalar();
            actaProId = result?.ToString();
        }

        return actaProId;
    }

    /// <summary>
    /// Returns either the docKey or the scopeId
    /// </summary>
    /// <param name="veId"></param>
    /// <returns>Will return -1 if there is no other key</returns>
    public string GetOtherId(string veId)
    {
        if (long.TryParse(veId, out _))
        {
            var docKey = GetActaProId(veId);
            return string.IsNullOrEmpty(docKey) ? "-1" : docKey;
        }

        return GetScopeId(veId).ToString();
    }
}
