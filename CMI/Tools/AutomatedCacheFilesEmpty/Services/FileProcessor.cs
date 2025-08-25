using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using CMI.Access.Common;
using CMI.Contract.Common;
using CMI.Tools.AutomatedCacheFilesEmpty.Models;
// ReSharper disable LocalizableElement

namespace CMI.Tools.AutomatedCacheFilesEmpty.Services
{
    public class FileProcessor
    {
        private readonly ISearchIndexDataAccess searchIndexDataAccess;
        private readonly string connectionString;

        public FileProcessor(ISearchIndexDataAccess searchIndexDataAccess, string connectionString)
        {
            this.searchIndexDataAccess = searchIndexDataAccess ?? throw new ArgumentNullException(nameof(searchIndexDataAccess));
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Processes zip files and analyzes metadata.
        /// </summary>
        /// <param name="directoryPath">Path to the directory containing zip files.</param>
        /// <param name="xDays">Number of days to filter by last download date.</param>
        /// <returns>List of CacheCheckResult objects containing analysis results.</returns>
        public List<CacheCheckResult> ProcessFiles(string directoryPath, int xDays)
        {
            var results = new List<CacheCheckResult>();
            var files = Directory.GetFiles(directoryPath, "*.*");

            foreach (var file in files)
            {
                try
                {
                    string archiveRecordId = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrEmpty(archiveRecordId))
                    {
                        Console.WriteLine($"Skipping file {file}: Invalid archiveRecordId.");
                        continue;
                    }

                    Console.WriteLine($"Querying archive record for ID: {archiveRecordId}");
                    var archiveRecord = searchIndexDataAccess.FindDocument(archiveRecordId, MetadataToExclude.OCRContentAndFiles);

                    if (archiveRecord == null || string.IsNullOrEmpty(archiveRecord.ManifestLink))
                    {
                        Console.WriteLine($"Archive record not found or manifest link is empty for ID: {archiveRecordId}");
                        results.Add(new CacheCheckResult
                        {
                            FilePath = file,
                            ArchiveRecordId = archiveRecordId,
                            ReferenceCode = "",
                            FileSizeInMb = new FileInfo(file).Length / (1024.0 * 1024.0),
                            ToBeDeleted = false,
                            HasNoViewerManifest = true
                        });

                        continue;
                    }

                    string referenceCode = archiveRecord.ReferenceCode;
                    var latestLog = GetLatestDownloadLog(referenceCode);

                    var datumErstellungToken = latestLog?.DatumErstellungToken ?? new DateTime(1900, 1, 1);
                    results.Add(new CacheCheckResult
                    {
                        FilePath = file,
                        ArchiveRecordId = archiveRecordId,
                        ReferenceCode = referenceCode,
                        FileSizeInMb = new FileInfo(file).Length / (1024.0 * 1024.0),
                        DatumErstellungToken = datumErstellungToken,
                        ToBeDeleted = datumErstellungToken < DateTime.Today.AddDays(xDays * -1),
                        HasNoViewerManifest = false
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            }

            return results;
        }

        private DownloadLogEntry GetLatestDownloadLog(string referenceCode)
        {
            DownloadLogEntry latestLog = null;
            string query = @"
                SELECT TOP 1 * 
                FROM downloadLog 
                WHERE signatur = @referenceCode 
                AND Vorgang = 'Download' 
                ORDER BY [DatumErstellungToken] DESC";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@referenceCode", referenceCode);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                latestLog = new DownloadLogEntry
                                {
                                    ReferenceCode = reader["signatur"].ToString(),
                                    Vorgang = reader["Vorgang"].ToString(),
                                    DatumErstellungToken = Convert.ToDateTime(reader["DatumErstellungToken"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying latest download log: {ex.Message}");
            }

            return latestLog;
        }
    }
}
