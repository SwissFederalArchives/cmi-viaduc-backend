using System;
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
    public class RenderEngine : IRenderEngine
    {
        private readonly IRequestClient<ConversionStartRequest> conversionRequestClient;
        private readonly IRequestClient<JobInitRequest> jobInitRequestClient;
        private readonly IRequestClient<SupportedFileTypesRequest> supportedFileTypesRequestClient;
        private readonly string sftpLicenseKey;
        private string[] supportedFileTypes;

        public RenderEngine(IRequestClient<JobInitRequest> jobInitRequestClient,
            IRequestClient<ConversionStartRequest> conversionRequestClient,
            IRequestClient<SupportedFileTypesRequest> supportedFileTypesRequestClient,
            string sftpLicenseKey)
        {
            this.jobInitRequestClient = jobInitRequestClient;
            this.conversionRequestClient = conversionRequestClient;
            this.supportedFileTypesRequestClient = supportedFileTypesRequestClient;
            this.sftpLicenseKey = sftpLicenseKey;
        }

        public async Task<string[]> GetSupportedFileTypes()
        {
            if (supportedFileTypes != null && supportedFileTypes.Any())
            {
                return supportedFileTypes;
            }

            var request = new SupportedFileTypesRequest
            {
                ProcessType = ProcessType.Rendering
            };

            var response = await supportedFileTypesRequestClient.GetResponse<SupportedFileTypesResponse>(request);
            supportedFileTypes = response.Message?.SupportedFileTypes;
            return supportedFileTypes;
        }

        public async Task<string> ConvertFile(string file, string destinationExtension, JobContext context)
        {
            var fi = new FileInfo(file);

            Log.Information("Start converting file: {FullName}", fi.FullName);

            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var conversionSettings = new JobInitRequest
                {
                    FileNameWithExtension = fi.Name,
                    RequestedProcessType = ProcessType.Rendering,
                    Context = context
                };

                var registrationResponse = (await jobInitRequestClient.GetResponse<JobInitResult>(conversionSettings)).Message;
                if (registrationResponse.IsInvalid)
                {
                    throw new Exception($"JobInit request was not valid. Error message: {registrationResponse.ErrorMessage}");
                }

                Log.Information("Successfully registered job for conversion of file {Name}. Got job id {JobId}", fi.Name,
                    registrationResponse.JobGuid);

                await UploadFile(registrationResponse, fi);
                Log.Information("File '{Name}' uploaded", fi.Name);

                var requestSettings = new ConversionStartRequest
                {
                    JobGuid = registrationResponse.JobGuid,
                    DestinationExtension = destinationExtension
                };

                Log.Information("Sent actual conversion request for job id {jobGuid}", registrationResponse.JobGuid);
                var convertionResponse = (await conversionRequestClient.GetResponse<ConversionStartResult>(requestSettings)).Message;
                if (convertionResponse.IsInvalid)
                {
                    throw new Exception(convertionResponse.ErrorMessage);
                }

                var result = await DownloadAndStoreFile(convertionResponse, fi.Directory);
                Log.Information(
                    $"Retrieved conversion result for file {fi.FullName} in {stopWatch.ElapsedMilliseconds} ms. Length of content is {result.LengthOfContent} bytes.");

                return result.TargetPath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while converting file {FullName}", fi.FullName);
                throw;
            }
        }

        private async Task UploadFile(JobInitResult jobInitResult, FileInfo toBeConverted)
        {
            Licensing.Key = sftpLicenseKey;
            using (var client = new Sftp())
            {
                await client.ConnectAsync(jobInitResult.UploadUrl, jobInitResult.Port);
                await client.LoginAsync(jobInitResult.User, jobInitResult.Password);
                await client.PutFileAsync(toBeConverted.FullName, $"{client.GetCurrentDirectory()}{toBeConverted.Name}");
            }
        }

        private async Task<DownloadAndStoreFileResult> DownloadAndStoreFile(ConversionStartResult documentConversionResult,
            FileSystemInfo targetFolder)
        {
            Licensing.Key = sftpLicenseKey;

            try
            {
                using (var client = new Sftp())
                {
                    await client.ConnectAsync(documentConversionResult.UploadUrl, documentConversionResult.Port);
                    await client.LoginAsync(documentConversionResult.User, documentConversionResult.Password);

                    var remotePath = $"{client.GetCurrentDirectory()}{documentConversionResult.ConvertedFileName}";
                    var targetPath = Path.Combine(targetFolder.FullName, $"{documentConversionResult.ConvertedFileName}");

                    if (!await client.FileExistsAsync(remotePath))
                    {
                        throw new FileNotFoundException($"File '{documentConversionResult.ConvertedFileName}' not found on SFTP server");
                    }

                    var lengthOfContent = await client.GetFileAsync(remotePath, targetPath);

                    if (!File.Exists(targetPath))
                    {
                        throw new InvalidOperationException($"Was unable to download file  {targetPath} from sftp server");
                    }

                    return new DownloadAndStoreFileResult {LengthOfContent = lengthOfContent, TargetPath = targetPath};
                }
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                throw;
            }
        }
    }
}