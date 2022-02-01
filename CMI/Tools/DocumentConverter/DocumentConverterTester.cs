using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
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
            var containerBuilder = new ContainerBuilder();
            BusConfigurator.ConfigureBus(containerBuilder);
            var container = containerBuilder.Build();

            bus = container.Resolve<IBusControl>();
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
                    .GetResponse<SupportedFileTypesResponse>(new SupportedFileTypesRequest {ProcessType = ProcessType.Rendering}).GetAwaiter().GetResult().Message;
                var extractionSupportedFileTypes = supportedFileTypesClient
                    .GetResponse<SupportedFileTypesResponse>(new SupportedFileTypesRequest {ProcessType = ProcessType.TextExtraction}).GetAwaiter().GetResult().Message;

                Console.WriteLine($"Supported types for conversion: {string.Join(", ", conversionSupportedFileTypes.SupportedFileTypes)}");
                Console.WriteLine($"Supported types for text extraction: {string.Join(", ", extractionSupportedFileTypes.SupportedFileTypes)}");
                Console.WriteLine("");

                foreach (var job in jobs)
                {
                    // Init Job
                    var jobInitResult = initClient.GetResponse<JobInitResult>(job).GetAwaiter().GetResult().Message;

                    if (!jobInitResult.IsInvalid)
                    {
                        // Upload File
                        UploadFile(jobInitResult, new FileInfo(job.FileNameWithExtension));

                        // Start actual job, based on request type
                        switch (job.RequestedProcessType)
                        {
                            case ProcessType.TextExtraction:
                                var extractedText = extractionClient.GetResponse<ExtractionStartResult>(new ExtractionStartRequest {JobGuid = jobInitResult.JobGuid})
                                    .GetAwaiter().GetResult().Message;
                                SaveTextExtractionResult(new FileInfo(job.FileNameWithExtension), extractedText.Text, targetFolder);
                                break;
                            case ProcessType.Rendering:
                                var conversionResult = conversionClient.GetResponse<ConversionStartResult>(new ConversionStartRequest
                                    {
                                        JobGuid = jobInitResult.JobGuid,
                                        DestinationExtension = GetDestinationBasedOnInput(new FileInfo(job.FileNameWithExtension).Extension)
                                    })
                                    .GetAwaiter()
                                    .GetResult()
                                    .Message;
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


        private IRequestClient<JobInitRequest> CreateJobInitRequestClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterJobInitRequestQueue);
            var client = bus.CreateRequestClient<JobInitRequest>(busUri, requestTimeout);

            return client;
        }

        private IRequestClient<SupportedFileTypesRequest> CreateSupportedFileTypesRequestClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterSupportedFileTypesRequestQueue);
            var client = bus.CreateRequestClient<SupportedFileTypesRequest>(busUri, requestTimeout);

            return client;
        }

        private IRequestClient<ConversionStartRequest> CreateDocumentConversionRequestClient()
        {
            // Very large files could take a very long time to convert
            var requestTimeout = TimeSpan.FromHours(12);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterConversionStartRequestQueue);
            var client = bus.CreateRequestClient<ConversionStartRequest>(busUri, requestTimeout);

            return client;
        }

        private IRequestClient<ExtractionStartRequest> CreateDocumentExtractionRequestClient()
        {
            // Ocr of a large pdf can take some time
            var requestTimeout = TimeSpan.FromHours(12);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterExtractionStartRequestQueue);
            var client = bus.CreateRequestClient<ExtractionStartRequest>(busUri, requestTimeout);

            return client;
        }
    }
}