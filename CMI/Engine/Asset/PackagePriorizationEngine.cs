using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Engine.Asset
{
    public class PackagePriorizationEngine : IPackagePriorizationEngine
    {
        private readonly IPrimaerdatenAuftragAccess primaerdatenDb;

        /// <summary>
        ///     Die Priorisierungsengine liefert den nächsten Job für die verschiedenen Kanäle.
        ///     Dazu wird geschaut wieviele Aufträge aktuell im Repository Service am laufen sind und in welchen Kanälen.
        /// 
        ///     Wenn es in einem Kanal Platz hat, wird der nächste pendente Auftrag aus der Datenbank abgerufen.
        /// 
        ///     Es wird mit 4 Kanälen gerechnet, d.h. alle Aufträge werden anhand ihrer Grösse in eine Priorisierungskategorie
        ///     eingeteilt.
        ///     Die Auftäge werden dann anhand der Priorisierungskategorie einem der 4 Kanäle zugeordnet.
        /// </summary>
        /// <param name="primaerdatenDb">Der Datenbankzugriff auf die Primärdaten Jobs.</param>
        /// <param name="channelAssignmentDefinition">
        ///     Die Konfiguration welche Priorisierungskategorien in welchen Kanälen
        ///     zugeordnet werden können
        /// </param>
        /// <param name="prefetchCount">Einstellungen wieviele parallele Jobs die Repository Queue (RabbitMq) verarbeiten kann.</param>
        public PackagePriorizationEngine(IPrimaerdatenAuftragAccess primaerdatenDb, ChannelAssignmentDefinition channelAssignmentDefinition,
            RepositoryQueuesPrefetchCount prefetchCount)
        {
            this.primaerdatenDb = primaerdatenDb;

            // Fülle in einen Dictionary wieviele Jobs pro Kanal verfügbar sind.
            MaxJobCountPerChannelForSync = GetMaxJobCountPerChannel(prefetchCount.SyncQueuePrefetchCount);
            MaxJobCountPerChannelForDownload = GetMaxJobCountPerChannel(prefetchCount.DownloadQueuePrefetchCount);

            // Definieren die KategorieRanges pro Kanal
            KategorieRangesPerChannel = new Dictionary<int, List<List<int>>>
            {
                {1, SplitKategorienInRanges(channelAssignmentDefinition.GetPrioritiesForChannel(1))},
                {2, SplitKategorienInRanges(channelAssignmentDefinition.GetPrioritiesForChannel(2))},
                {3, SplitKategorienInRanges(channelAssignmentDefinition.GetPrioritiesForChannel(3))},
                {4, SplitKategorienInRanges(channelAssignmentDefinition.GetPrioritiesForChannel(4))}
            };

            Log.Debug(
                "Initialized Package Priorization Engine with the following settings: Max job count for sync: {MaxJobCountPerChannelForSync}. " +
                "Max job count for download: {MaxJobCountPerChannelForDownload}", MaxJobCountPerChannelForSync, MaxJobCountPerChannelForDownload);
        }

        public Dictionary<int, int> MaxJobCountPerChannelForSync { get; }
        public Dictionary<int, int> MaxJobCountPerChannelForDownload { get; }
        public Dictionary<int, List<List<int>>> KategorieRangesPerChannel { get; }

        public async Task<Dictionary<int, int[]>> GetNextJobsForExecution(AufbereitungsArtEnum aufbereitungsArt)
        {
            Log.Debug("Fetching next jobs for execution from priorization engine.");
            var newJobsPerChannel = new Dictionary<int, int>();

            // Hole die aktuelle Auslastung von der Datenbank
            // die im Repository Service in der Arbeit sind
            var workload = await primaerdatenDb.GetCurrentWorkload(aufbereitungsArt);
            Log.Debug("The current workload is: {workload}", JsonConvert.SerializeObject(workload));

            // Schaue ob alle channels gefüllt sind
            foreach (var channelWorkload in workload)
            {
                // Hole die Anzahl der möglichen Jobs in der Queue für die Aufbereitungsart
                var possibleJobs = GetPossibleJobs(aufbereitungsArt, channelWorkload.Key);

                // Anzahl der zu startenden neuen Jobs ist mögliche Anzahl minus der aktuellen Auslastung
                var newJobsToStart = possibleJobs - workload[channelWorkload.Key];
                if (newJobsToStart > 0)
                {
                    newJobsPerChannel.Add(channelWorkload.Key, newJobsToStart);
                }
            }

            // Hole die nächsten Jobs anhand der möglichen neuen Jobs
            Log.Debug("We are able to fetch pending jobs as follows: {newJobsPerChannel}", JsonConvert.SerializeObject(newJobsPerChannel));
            var retVal = await FetchPendingJobsFromDatabase(aufbereitungsArt, newJobsPerChannel);
            Log.Debug("The following jobs can be started: {retVal}", JsonConvert.SerializeObject(retVal));

            return retVal;
        }

        private async Task<Dictionary<int, int[]>> FetchPendingJobsFromDatabase(AufbereitungsArtEnum aufbereitungsArt,
            Dictionary<int, int> newJobsPerChannel)
        {
            var retVal = new Dictionary<int, int[]>();
            foreach (var channel in newJobsPerChannel)
            {
                var jobIds = await FetchNextJobForChannel(aufbereitungsArt, channel.Key, channel.Value, retVal.SelectMany(r => r.Value).ToArray());
                Log.Debug("Found the following jobs for channel {Key}: {jobIds}", channel.Key, string.Join(", ", jobIds));
                if (jobIds.Count > 0)
                {
                    retVal.Add(channel.Key, jobIds.ToArray());
                }
            }

            return retVal;
        }

        private async Task<List<int>> FetchNextJobForChannel(AufbereitungsArtEnum aufbereitungsArt, int channel, int anzahlJobs, int[] idsToExclude)
        {
            var nextJobs = new List<int>();
            // Spezialfall: Im Normalfall sollten die Kategorien fortlaufend nummeriert sein. Aber es gibt den Fall für Kanal 4 
            // der definiert dass Prioritäten zuerst 6-9 verarbeitet werden sollen, und wenn es dann nichts mehr hat die Prioritäten 1-5
            // Daher müssen wir die Kategorien anschauen und prüfen ob es einen "Bruch" gibt. Wenn ja, dann müssen wir ggf. 2 Db Abfragen
            // machen.
            var kategorienRanges = KategorieRangesPerChannel[channel];
            foreach (var kategorienRange in kategorienRanges)
            {
                nextJobs.AddRange(await primaerdatenDb.GetNextJobsForChannel(aufbereitungsArt, kategorienRange.ToArray(), anzahlJobs, idsToExclude));
                if (nextJobs.Count >= anzahlJobs)
                    // Liefere die Jobs zurück, aber maximal soviele wie verlangt wurden.
                {
                    return nextJobs.GetRange(0, anzahlJobs);
                }
            }

            return nextJobs;
        }

        /// <summary>
        ///     Methode untersucht ob die Kategorien in fortlaufender Priorisierung verlaufen.
        ///     Wenn nicht, werden "Pakete" mit fortlaufenden Priorisierungen gemacht.
        ///     Bsp:
        ///     Input ist "6,7,8,9,1,2,3,4,5"
        ///     Output ist eine Liste mit zwei Einträgen. Der Eintrag ist wiederum eine Liste von Zahlen.
        ///     6,7,8,9
        ///     1,2,3,4,5
        /// </summary>
        /// <param name="kategorien"></param>
        /// <returns></returns>
        private static List<List<int>> SplitKategorienInRanges(int[] kategorien)
        {
            var splitKategorien = new List<List<int>>();
            var rangeKategorien = new List<int>();
            var lastKategorie = 0;
            foreach (var kategorie in kategorien)
            {
                if (kategorie > lastKategorie)
                {
                    rangeKategorien.Add(kategorie);
                }
                else
                {
                    splitKategorien.Add(rangeKategorien);
                    rangeKategorien = new List<int> {kategorie};
                }

                lastKategorie = kategorie;
            }

            splitKategorien.Add(rangeKategorien);
            return splitKategorien;
        }

        private int GetPossibleJobs(AufbereitungsArtEnum aufbereitungsArt, int channel)
        {
            switch (aufbereitungsArt)
            {
                case AufbereitungsArtEnum.Sync:
                    return MaxJobCountPerChannelForSync[channel];
                case AufbereitungsArtEnum.Download:
                    return MaxJobCountPerChannelForDownload[channel];
                default:
                    throw new ArgumentOutOfRangeException(nameof(aufbereitungsArt), aufbereitungsArt, null);
            }
        }


        private static Dictionary<int, int> GetMaxJobCountPerChannel(int prefetchCount)
        {
            var maxJobCountPerChannelForSync = new Dictionary<int, int>
            {
                {1, 0},
                {2, 0},
                {3, 0},
                {4, 0}
            };
            for (var i = 1; i <= prefetchCount; i++)
            {
                switch (i % 4)
                {
                    case 1:
                        maxJobCountPerChannelForSync[1]++;
                        break;
                    case 2:
                        maxJobCountPerChannelForSync[2]++;
                        break;
                    case 3:
                        maxJobCountPerChannelForSync[3]++;
                        break;
                    case 0:
                        maxJobCountPerChannelForSync[4]++;
                        break;
                }
            }

            return maxJobCountPerChannelForSync;
        }
    }

    public interface IPackagePriorizationEngine
    {
        Task<Dictionary<int, int[]>> GetNextJobsForExecution(AufbereitungsArtEnum aufbereitungsArt);
    }
}