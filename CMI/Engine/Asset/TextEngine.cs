using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using MassTransit;
using Rebex;
using Rebex.Net;
using Serilog;
using JobContext = CMI.Contract.DocumentConverter.JobContext;

namespace CMI.Engine.Asset
{
    public class TextEngine : ITextEngine
    {
        private readonly IRequestClient<ExtractionStartRequest> extractionRequestClient;
        private readonly IRequestClient<JobInitRequest> jobRequestClient;
        private readonly IRequestClient<JobEndRequest> jobEndRequestClient;
        private readonly IRequestClient<SupportedFileTypesRequest> supportedFileTypesRequestClient;
        private readonly string sftpLicenseKey;
        private string[] supportedFileTypes;

        public TextEngine(IRequestClient<JobInitRequest> jobRequestClient,
            IRequestClient<JobEndRequest> jobEndRequestClient,
            IRequestClient<ExtractionStartRequest> extractionRequestClient,
            IRequestClient<SupportedFileTypesRequest> supportedFileTypesRequestClient,
            string sftpLicenseKey)
        {
            this.jobRequestClient = jobRequestClient;
            this.jobEndRequestClient = jobEndRequestClient;
            this.extractionRequestClient = extractionRequestClient;
            this.supportedFileTypesRequestClient = supportedFileTypesRequestClient;
            this.sftpLicenseKey = sftpLicenseKey;
        }

        public async Task<string> ExtractText(string file, JobContext context)
        {
            var fi = new FileInfo(file);

            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var conversionSettings = new JobInitRequest
                {
                    FileNameWithExtension = fi.Name,
                    RequestedProcessType = ProcessType.TextExtraction,
                    Context = context
                };

                var registrationResponse = (await jobRequestClient.GetResponse<JobInitResult>(conversionSettings)).Message;
                Log.Information("Successfully registered job for text extraction of file {Name}. Got job id {JobId}", fi.Name,
                    registrationResponse.JobGuid);

                await UploadFile(registrationResponse, fi);
                Log.Information("File '{Name}' uploaded", fi.Name);

                var requestSettings = new ExtractionStartRequest
                {
                    JobGuid = registrationResponse.JobGuid
                };
                Log.Information("Sent actual text extraction request for job id {jobGuid}", registrationResponse.JobGuid);
                
                var extractionResult = (await extractionRequestClient.GetResponse<ExtractionStartResult>(requestSettings)).Message;
                if (extractionResult.IsInvalid)
                {
                    throw new Exception(extractionResult.ErrorMessage);
                }

                var lengthInBytes = string.IsNullOrEmpty(extractionResult.Text) ? 0 : extractionResult.Text.Length;
                Log.Information("Retrieved content for file {Name} in {ElapsedMilliseconds} ms. Length of content is {lengthInBytes} bytes.", fi.Name,
                    stopWatch.ElapsedMilliseconds,
                    lengthInBytes);

                // Save the files from the ocr process
                if (extractionResult.IsOcrResult)
                {
                    await DownloadAndStoreFiles(extractionResult, fi);
                }

                // Remove the job
                await jobEndRequestClient.GetResponse<JobEndResult>(new JobEndRequest {JobGuid = extractionResult.JobGuid});
                Log.Information($"Removed the job with the id {extractionResult.JobGuid}.");

                return extractionResult.Text;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while extracting text for file {FullName}", fi.FullName);
                throw;
            }
        }


        public async Task<string[]> GetSupportedFileTypes()
        {
            if (supportedFileTypes != null && supportedFileTypes.Length > 0)
            {
                return supportedFileTypes;
            }

            var request = new SupportedFileTypesRequest
            {
                ProcessType = ProcessType.TextExtraction
            };

            var response = (await supportedFileTypesRequestClient.GetResponse<SupportedFileTypesResponse>(request)).Message;
            supportedFileTypes = response.SupportedFileTypes;
            return supportedFileTypes;
        }

        private async Task UploadFile(JobInitResult jobInitResult, FileInfo toBeConverted)
        {
            Licensing.Key = sftpLicenseKey;
            using (var client = new Sftp())
            {
                await client.ConnectAsync(jobInitResult.UploadUrl, jobInitResult.Port);
                await client.LoginAsync(jobInitResult.User, jobInitResult.Password);

                var uploadName = $"{client.GetCurrentDirectory()}{toBeConverted.Name}";
                Log.Verbose("Uploading file {FullName} to {UploadName}", toBeConverted.FullName, uploadName);
                if (toBeConverted.Exists)
                {
                    await client.PutFileAsync(toBeConverted.FullName, uploadName);
                    Log.Verbose("Upload successfull");
                }
                else
                {
                    Log.Verbose("File {FullName} does not exist, or cannot be accessed.", toBeConverted.FullName);
                    throw new FileNotFoundException("File could not be uploaded, because source file is not accessible or cannot be found",
                        toBeConverted.FullName);
                }
            }
        }

        private async Task<bool> DownloadAndStoreFiles(ExtractionStartResult documentExtractionResult, FileInfo originalFile)
        {
            Licensing.Key = sftpLicenseKey;
            var targetFolder = originalFile.Directory;
            try
            {
                foreach (var createdOcrFile in documentExtractionResult.CreatedOcrFiles)
                {
                    using (var client = new Sftp())
                    {
                        await client.ConnectAsync(documentExtractionResult.UploadUrl, documentExtractionResult.Port);
                        await client.LoginAsync(documentExtractionResult.User, documentExtractionResult.Password);

                        var remotePath = $"{client.GetCurrentDirectory()}{createdOcrFile.Value}";
                        var targetPath = Path.Combine(targetFolder!.FullName, HandlePdfFiles(createdOcrFile, originalFile));

                        if (!await client.FileExistsAsync(remotePath))
                        {
                            throw new FileNotFoundException($"File '{createdOcrFile.Value}' not found on SFTP server");
                        }

                        await client.GetFileAsync(remotePath, targetPath);

                        if (!File.Exists(targetPath))
                        {
                            throw new InvalidOperationException($"Was unable to download file  {targetPath} from sftp server");
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                throw;
            }
        }

        /// <summary>
        /// PDF files that are recognized are re-saved as PDF under a temporary file name
        /// Here we take care and apply the old original name
        /// </summary>
        private static string HandlePdfFiles(KeyValuePair<OcrResultType, string> createdOcrFile, FileInfo originalFile)
        {
            if (createdOcrFile.Key == OcrResultType.Pdf)
            {
                return originalFile.Name;
            }

            return createdOcrFile.Value;
        }
    }
}