using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using CMI.Tools.ElasticTreeSequenceUpdater.Models;
using CMI.Tools.ElasticTreeSequenceUpdater.Properties;

namespace CMI.Tools.ElasticTreeSequenceUpdater.Services
{
    public static class SQLiteHelper
    {
        private static string DbFile => Settings.Default.SQLiteDbPath;

        /// <summary>
        /// Initialisiert die SQLite-DB.
        /// Wenn recreate = true, wird die alte Datei gelöscht und neu angelegt.
        /// </summary>
        public static void Initialize(bool recreate)
        {
            if (recreate && System.IO.File.Exists(DbFile))
            {
                System.IO.File.Delete(DbFile);
            }

            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                @"CREATE TABLE IF NOT EXISTS TreeSequenceUpdates (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ScopeId TEXT,
                    ActaProId TEXT,
                    TreeSequence INTEGER,
                    Processed INTEGER DEFAULT 0
                  );";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Batch-Einfügen in SQLite.
        /// </summary>
        public static void InsertBatch(IEnumerable<TreeSequenceRow> rows)
        {
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();

            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = "PRAGMA synchronous = OFF; PRAGMA journal_mode = MEMORY;";
                pragmaCmd.ExecuteNonQuery();
            }

            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO TreeSequenceUpdates (ScopeId, ActaProId, TreeSequence, Processed) " +
                "VALUES ($scope, $acta, $tree, 0)";

            var scopeParam = cmd.CreateParameter();
            scopeParam.ParameterName = "$scope";
            cmd.Parameters.Add(scopeParam);

            var actaParam = cmd.CreateParameter();
            actaParam.ParameterName = "$acta";
            cmd.Parameters.Add(actaParam);

            var treeParam = cmd.CreateParameter();
            treeParam.ParameterName = "$tree";
            cmd.Parameters.Add(treeParam);

            foreach (var row in rows)
            {
                scopeParam.Value = row.ScopeId ?? "";
                actaParam.Value = row.ActaProId ?? "";
                treeParam.Value = row.TreeSequence;
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        /// <summary>
        /// Holt einen Batch von unprocessed Records.
        /// </summary>
        public static List<UpdateEntry> GetBatch(int size)
        {
            var result = new List<UpdateEntry>();
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT Id, ScopeId, ActaProId, TreeSequence FROM TreeSequenceUpdates WHERE Processed = 0 LIMIT $size";
            cmd.Parameters.AddWithValue("$size", size);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new UpdateEntry
                {
                    DbId = reader.GetInt64(0),
                    ScopeId = reader.IsDBNull(1) ? null : reader.GetString(1),
                    ActaProId = reader.IsDBNull(2) ? null : reader.GetString(2),
                    TreeSeq = reader.GetInt32(3)
                });
            }
            return result;
        }

        /// <summary>
        /// Markiert Records als verarbeitet.
        /// </summary>
        public static void MarkProcessed(IEnumerable<long> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any()) return;

            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            using var tx = conn.BeginTransaction();

            var placeholders = string.Join(",", idList.Select((_, i) => $"$id{i}"));
            var cmd = conn.CreateCommand();
            cmd.CommandText = $"UPDATE TreeSequenceUpdates SET Processed = 1 WHERE Id IN ({placeholders})";

            for (int i = 0; i < idList.Count; i++)
            {
                cmd.Parameters.AddWithValue($"$id{i}", idList[i]);
            }

            cmd.ExecuteNonQuery();
            tx.Commit();
        }

        /// <summary>
        /// Gibt die Anzahl der noch nicht verarbeiteten Records zurück.
        /// </summary>
        public static long CountUnprocessed()
        {
            using var conn = new SqliteConnection($"Data Source={DbFile}");
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM TreeSequenceUpdates WHERE Processed = 0";
            return (long) cmd.ExecuteScalar();
        }
    }
}
