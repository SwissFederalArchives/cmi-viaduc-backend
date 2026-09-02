using CMI.Tools.ElasticTreeSequenceUpdater.Models;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AccessElasticFactory = CMI.Tools.ElasticTreeSequenceUpdater.Services.ElasticClientFactory;

namespace CMI.Tools.ElasticTreeSequenceUpdater.Services
{
    public class SynchronisationService
    {
        public async Task RunAsync(string[] args)
        {
            var inputFile = args.Length > 0 ? args[0].Trim() : "treeSequenceUpdates.csv";
            int mode = 0; // default = sqlite-new

            if (args.Length > 1 && int.TryParse(args[1], out var parsed))
                mode = parsed;

            Log.Information("[Service] Input file: {File}", inputFile);
            Log.Information("[Service] Mode: {Mode} (0=new, 1=reuse, 2=csv-direct)", mode);

            if (!File.Exists(inputFile) && mode != 1)
            {
                Log.Error("[Service] Input file not found. Aborting.");
                return;
            }

            IEnumerable<TreeSequenceRow> rows = Enumerable.Empty<TreeSequenceRow>();
            var client = AccessElasticFactory.Create();
            var elastic = new TreeSequenceBulkUpdater(client, "archive");

            switch (mode)
            {
                case 0: // sqlite-new
                    SQLiteHelper.Initialize(true);
                    rows = LoadCsv(inputFile);
                    SQLiteHelper.InsertBatch(rows);
                    Log.Information("[Service] Inserted rows into NEW SQLite DB.");
                    break;

                case 1: // sqlite-reuse
                    SQLiteHelper.Initialize(false);
                    var unprocessed = SQLiteHelper.CountUnprocessed();
                    Log.Information("[Service] Using existing SQLite DB. {Count} unprocessed rows remain.", unprocessed);
                    break;

                case 2: // csv-direct
                    rows = LoadCsv(inputFile);
                    Log.Information("[Service] Processing directly from CSV (no SQLite).");
                    break;

                default:
                    Log.Error("[Service] Invalid mode {Mode}. Use 0=new, 1=reuse, 2=csv-direct.", mode);
                    return;
            }

            while (true)
            {
                List<UpdateEntry> batch;

                if (mode == 2) // csv-direct
                {
                    batch = rows.Take(5000)
                        .Select((r, i) => new UpdateEntry
                        {
                            DbId = i,
                            ScopeId = r.ScopeId,
                            ActaProId = r.ActaProId,
                            TreeSeq = r.TreeSequence
                        })
                        .ToList();

                    if (!batch.Any()) break;
                    rows = rows.Skip(5000);
                }
                else
                {
                    batch = SQLiteHelper.GetBatch(5000);
                    if (!batch.Any()) break;
                }

                var sw = Stopwatch.StartNew();
                await elastic.BulkUpdateTreeSequenceAsync(batch);
                sw.Stop();

                Log.Information("[Service] Processed {Count} rows in {Ms} ms ({Rate}/s)",
                    batch.Count, sw.ElapsedMilliseconds,
                    (batch.Count * 1000) / sw.ElapsedMilliseconds);

                if (mode != 2)
                {
                    SQLiteHelper.MarkProcessed(batch.Select(b => b.DbId));
                    Log.Information("[Service] Marked {Count} rows as processed in SQLite.", batch.Count);
                }
            }

            Log.Information("[Service] Completed all updates.");
        }

        private IEnumerable<TreeSequenceRow> LoadCsv(string path)
        {
            using var reader = new StreamReader(path);
            string? headerLine = reader.ReadLine();
            if (headerLine == null) yield break;

            int lineNo = 1;
            while (!reader.EndOfStream)
            {
                lineNo++;
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3)
                {
                    Log.Warning("[CSV] Line {Line} has less than 3 columns.", lineNo);
                    continue;
                }

                string actaProId = parts[0].Trim();
                string scopeId = parts[1].Trim();
                string treeSeqStr = parts[2].Trim();

                if (!int.TryParse(treeSeqStr, out var treeSeq))
                {
                    Log.Warning("[CSV] Invalid TreeSequence in line {Line}: '{TreeSeq}'", lineNo, treeSeqStr);
                    continue;
                }

                yield return new TreeSequenceRow
                {
                    ActaProId = actaProId,
                    ScopeId = scopeId,
                    TreeSequence = treeSeq
                };
            }
        }
               
    }
}
