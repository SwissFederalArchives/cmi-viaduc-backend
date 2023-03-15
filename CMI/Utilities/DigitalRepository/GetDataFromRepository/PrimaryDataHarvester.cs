using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Repository;
using CMI.Utilities.DigitalRepository.PrimaryDataHarvester.Properties;
using MassTransit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace CMI.Utilities.DigitalRepository.PrimaryDataHarvester
{
    internal class PrimaryDataHarvester
    {
        private readonly IRepositoryManager manager;
        private readonly IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient;
        private event EventHandler Started = delegate { };

        public PrimaryDataHarvester(IRepositoryManager manager, IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient)
        {
            this.manager = manager;
            this.findArchiveRecordClient = findArchiveRecordClient;
        }



        /// <summary>
        ///     Starts the Harvest Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start(string veId)
        {
            // As the real start method is async and we can not have async start methods in 
            // TopShelf we create an event that is called during startup. 
            Started += OnStarted;
            Started(this, new EventArgsVeId(veId));
        }
        

        private async void OnStarted(object sender, EventArgs args)
        {
            Started -= OnStarted;
            if (args is EventArgsVeId veId)
            {
                await SavePrimaryData(veId.veId);
            }
            Log.Information("Daten geholt!!!");
        }

        public async Task SavePrimaryData(string veId)
        {
            try
            {
                var archiveRecord = (await findArchiveRecordClient.GetResponse<FindArchiveRecordResponse>(
                    new FindArchiveRecordRequest
                    {
                        ArchiveRecordId = veId, 
                        IncludeFulltextContent = false,
                        UseUnanonymizedData = false
                    })).Message;
                if (archiveRecord.ArchiveRecordId == veId)
                {
                    var record = new ArchiveRecord
                    {
                        ArchiveRecordId = archiveRecord.ArchiveRecordId,
                        Metadata = new ArchiveRecordMetadata
                        {
                            PrimaryDataLink = archiveRecord.ElasticArchiveRecord.PrimaryDataLink
                        }
                    };

                    Log.Information($"Daten von folgender VeId: {veId} werden geholt, mit Datenlink {record.Metadata.PrimaryDataLink}");
                    var packageResultAsync = await manager.GetPackage(record.Metadata.PrimaryDataLink, record.ArchiveRecordId, 0);

                    if (packageResultAsync.Success)
                    {
                        Log.Information("packageResult Valid: " + packageResultAsync.Valid + " Success: " + packageResultAsync.Success);
                    }
                    else
                    {
                        Log.Warning("packageResult not Success: " + packageResultAsync.ErrorMessage);
                    }

                    var outputFile = Path.Combine(Settings.Default.FileCopyDestinationPath, archiveRecord.ArchiveRecordId + ".zip");
                    if (File.Exists(outputFile))
                    {
                        Log.Warning("outputFile schon vorhanden: " + outputFile);
                    }
                    else
                    {
                        File.Move(Path.Combine(Settings.Default.FileCopyDestinationPath, packageResultAsync.PackageDetails.PackageFileName),
                            outputFile);
                    }

                }
                else
                {
                    Log.Warning("Record not found");
                }
            }
            catch (Exception e)
            {
                Log.Warning("Error veId {veId}: " + e, veId);
            }

        }
    }

    public class ArchiveRecordIdOrSignature
    {
        public List<string> RecordIdOrSig { get; set; }
    }

    public class EventArgsVeId : EventArgs
    {
        public readonly string veId;

        public EventArgsVeId(string veId)
        {
            this.veId = veId;
        }
    }
}
