using System;
using System.Collections.Generic;
using System.IO;
using CMI.Contract.DocumentConverter;
using CMI.Contract.Messaging;
using CMI.Tools.DocumentConverter.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Rebex;
using Rebex.Net;

namespace CMI.Tools.DocumentConverter
{
    public class DocumentConverterTester : IDisposable
    {
        private readonly IBusControl bus;

        public DocumentConverterTester()
        {
            bus = BusConfigurator.ConfigureBus();
            ;
            bus.Start();
        }

        public void Dispose()
        {
            bus.Stop();
        }

        public void Test(string sourceFolder)
        {
            try
            {
                LogConfigurator.ConfigureForService();

                var initClient = CreateJobInitRequestClient();
                var extractionClient = CreateDocumentExtractionRequestClient();
                var conversionClient = CreateDocumentConversionRequestClient();
                var supportedFileTypesClient = CreateSupportedFileTypesRequestClient();

                // Create the target ouptut folder as subdirectory. Make sure it is empty
                var targetFolder = new DirectoryInfo(Path.Combine(sourceFolder, "Output"));
                if (targetFolder.Exists)
                {
                    targetFolder.Delete(true);
                }

                targetFolder.Create();

                var sourceFiles = new DirectoryInfo(sourceFolder).GetFiles("*.*", SearchOption.AllDirectories);
                var jobs = new List<JobInitRequest>();

                foreach (var sourceFile in sourceFiles)
                {
                    var extractionJob = new JobInitRequest
                    {
                        FileNameWithExtension = sourceFile.FullName,
                        RequestedProcessType = ProcessType.TextExtraction
                    };

                    var conversionJob = new JobInitRequest
                    {
                        FileNameWithExtension = sourceFile.FullName,
                        RequestedProcessType = ProcessType.Rendering
                    };

                    jobs.Add(extractionJob);
                    jobs.Add(conversionJob);
                }

                // Print out supported file types
                var conversionSupportedFileTypes = supportedFileTypesClient
                    .Request(new SupportedFileTypesRequest {ProcessType = ProcessType.Rendering}).GetAwaiter().GetResult();
                var extractionSupportedFileTypes = supportedFileTypesClient
                    .Request(new SupportedFileTypesRequest {ProcessType = ProcessType.TextExtraction}).GetAwaiter().GetResult();
                Console.WriteLine($"Supported types for conversion: {string.Join(", ", conversionSupportedFileTypes.SupportedFileTypes)}");
                Console.WriteLine($"Supported types for text extraction: {string.Join(", ", extractionSupportedFileTypes.SupportedFileTypes)}");
                Console.WriteLine("");

                foreach (var job in jobs)
                {
                    // Init Job
                    var jobInitResult = initClient.Request(job).GetAwaiter().GetResult();

                    if (!jobInitResult.IsInvalid)
                    {
                        // Upload File
                        UploadFile(jobInitResult, new FileInfo(job.FileNameWithExtension));

                        // Start actual job, based on request type
                        switch (job.RequestedProcessType)
                        {
                            case ProcessType.TextExtraction:
                                var extractedText = extractionClient.Request(new ExtractionStartRequest {JobGuid = jobInitResult.JobGuid})
                                    .GetAwaiter().GetResult();
                                SaveTextExtractionResult(new FileInfo(job.FileNameWithExtension), extractedText.Text, targetFolder);
                                break;
                            case ProcessType.Rendering:
                                var conversionResult = conversionClient.Request(new ConversionStartRequest
                                    {
                                        JobGuid = jobInitResult.JobGuid,
                                        DestinationExtension = GetDestinationBasedOnInput(new FileInfo(job.FileNameWithExtension).Extension)
                                    })
                                    .GetAwaiter()
                                    .GetResult();
                                if (!conversionResult.IsInvalid)
                                {
                                    DownloadAndStoreFile(conversionResult, targetFolder);
                                }
                                else
                                {
                                    Console.WriteLine($"Unable to convert file. Error: {conversionResult.ErrorMessage}");
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        private string GetDestinationBasedOnInput(string extension)
        {
            if (extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
            {
                return ".pdf";
            }

            if (extension.Equals(".wav", StringComparison.InvariantCultureIgnoreCase))
            {
                return ".mp3";
            }

            if (extension.Equals(".mp4", StringComparison.InvariantCultureIgnoreCase))
            {
                return ".mp4";
            }

            if (extension.Equals(".tif", StringComparison.InvariantCultureIgnoreCase))
            {
                return ".pdf";
            }

            if (extension.Equals(".tiff", StringComparison.InvariantCultureIgnoreCase))
            {
                return ".pdf";
            }

            Console.WriteLine($"Not supported extension {extension}");
            return "";
        }

        private void SaveTextExtractionResult(FileInfo sourceFile, string extractedText, DirectoryInfo targetFolder)
        {
            var targetFile = new FileInfo(Path.Combine(targetFolder.FullName, sourceFile.Name.Replace(sourceFile.Extension, ".txt")));
            File.WriteAllText(targetFile.FullName, extractedText);
            Console.WriteLine("Saved extracted text to disk...");
        }


        private void UploadFile(JobInitResult jobResult, FileInfo toBeConverted)
        {
            Licensing.Key = DocumentConverterSettings.Default.SftpLicenseKey;
            using (var client = new Sftp())
            {
                client.Connect(jobResult.UploadUrl, jobResult.Port);
                client.Login(jobResult.User, jobResult.Password);
                client.PutFile(toBeConverted.FullName, $"{client.GetCurrentDirectory()}{toBeConverted.Name}");
            }
        }

        private void DownloadAndStoreFile(ConversionStartResult conversionResult, DirectoryInfo targetFolder)
        {
            Licensing.Key = DocumentConverterSettings.Default.SftpLicenseKey;

            try
            {
                using (var client = new Sftp())
                {
                    client.Connect(conversionResult.UploadUrl, conversionResult.Port);
                    client.Login(conversionResult.User, conversionResult.Password);

                    var remotePath = $"{client.GetCurrentDirectory()}{conversionResult.ConvertedFileName}";
                    var targetPath = Path.Combine(targetFolder.FullName, $"{conversionResult.ConvertedFileName}");

                    if (!client.FileExists(remotePath))
                    {
                        Console.WriteLine($"ERROR: File {conversionResult.ConvertedFileName} not found on SFTP server.");
                    }

                    var length = client.GetFile(remotePath, targetPath);
                    Console.WriteLine($"Converted file {targetPath} and downloaded {length} bytes");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        private IRequestClient<JobInitRequest, JobInitResult> CreateJobInitRequestClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterJobInitRequestQueue);
            var client = new MessageRequestClient<JobInitRequest, JobInitResult>(bus, busUri, requestTimeout);

            return client;
        }

        private IRequestClient<SupportedFileTypesRequest, SupportedFileTypesResponse> CreateSupportedFileTypesRequestClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterSupportedFileTypesRequestQueue);
            var client = new MessageRequestClient<SupportedFileTypesRequest, SupportedFileTypesResponse>(bus, busUri, requestTimeout);

            return client;
        }

        private IRequestClient<ConversionStartRequest, ConversionStartResult> CreateDocumentConversionRequestClient()
        {
            // Very large files could take a very long time to convert
            var requestTimeout = TimeSpan.FromHours(12);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterConversionStartRequestQueue);
            var client = new MessageRequestClient<ConversionStartRequest, ConversionStartResult>(bus, busUri, requestTimeout);

            return client;
        }

        private IRequestClient<ExtractionStartRequest, ExtractionStartResult> CreateDocumentExtractionRequestClient()
        {
            // Ocr of a large pdf can take some time
            var requestTimeout = TimeSpan.FromHours(12);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterExtractionStartRequestQueue);
            var client = new MessageRequestClient<ExtractionStartRequest, ExtractionStartResult>(bus, busUri, requestTimeout);

            return client;
        }
    }
}