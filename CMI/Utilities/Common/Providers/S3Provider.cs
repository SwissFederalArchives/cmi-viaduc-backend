using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using Serilog;

namespace CMI.Utilities.Common.Providers
{
    public class S3Provider : IStorageProvider
    {
        private readonly StorageProviderSettings storageProviderSettings;
        private readonly AmazonS3Client s3Client;

        public S3Provider(StorageProviderSettings storageProviderSettings)
        {
            this.storageProviderSettings = storageProviderSettings;

            try
            {
                if (!string.IsNullOrEmpty(storageProviderSettings.ServiceUrl))
                {
                    var option = new AmazonS3Config
                    {
                        ServiceURL = storageProviderSettings.ServiceUrl,
                        ForcePathStyle = true
                    };
                    s3Client = new AmazonS3Client(new BasicAWSCredentials(storageProviderSettings.AccessKey, storageProviderSettings.SecretAccessKey), option);
                }
                else
                {
                    var regionEndpoint = RegionEndpoint.GetBySystemName(storageProviderSettings.Region);
                    s3Client = new AmazonS3Client(new BasicAWSCredentials(storageProviderSettings.AccessKey, storageProviderSettings.SecretAccessKey), regionEndpoint);
                }

                Log.Information("Successfully connected to S3 service.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected exception while connecting to S3 service");
                throw;
            }
        }

        public async Task CopyFileAsync(FileInfo sourceFile, string relPath, string extension, string targetDirectory)
        {
            var dic = new DirectoryInfo(targetDirectory);
            var uploadFile = new FileInfo(Path.ChangeExtension(sourceFile.FullName, extension));
            if (uploadFile.Exists)
            {
                var key = Path.Combine(dic.Name, relPath);
                await UploadFileAsync(uploadFile, key);
            }
        }

        public async Task<MemoryStream> ReadFileAsync(Uri fileUri)
        {
            var key = fileUri.ToString(); 
            var exists = DoesS3ObjectExist(key);
            if (exists)
            {
                var result = await LoadFileFromStorageAsync(key);
                return result?.MemoryStream;
            }

            return null;
        }

        private async Task UploadFileAsync(FileInfo file, string relativeToPath = "")
        {
            try
            {
                using var fileTransferUtility = new TransferUtility(s3Client);
                var sw = new Stopwatch();
                sw.Start();

                // Generate key
                var uri = new Uri(relativeToPath + "\\" + file.Name, UriKind.Relative);
                var key = PathHelper.CreateShortValidUrlName(uri.OriginalString, true).Replace("\\", "/");
          
                // Check if exists
                var exists = DoesS3ObjectExist(key);
                if (exists)
                {
                    await s3Client.DeleteObjectAsync(storageProviderSettings.BucketName, key, CancellationToken.None);
                }

                // Upload the file
                await fileTransferUtility.UploadAsync(file.FullName, storageProviderSettings.BucketName, key, CancellationToken.None);
                Log.Debug($"Upload for file {key} completed in ms {sw.Elapsed.Milliseconds}");
            }
            catch (AmazonS3Exception e)
            {
                Log.Error("Amazon error encountered on server. Message:'{message}' when writing file {file}", e.Message, file.FullName);
                throw;
            }
            catch (Exception e)
            {
                Log.Error("Unknown encountered on server. Message:'{message}' when writing file {file}", e.Message, file.FullName);
                throw;
            }
        }
        
        private bool DoesS3ObjectExist(string key)
        {
            try
            {
                Log.Debug("Checking if file exists on S3-storage with key {key}", key);

                Amazon.S3.IO.S3FileInfo s3FileInfo = new Amazon.S3.IO.S3FileInfo(s3Client,
                    storageProviderSettings.BucketName, key);
                return s3FileInfo.Exists;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DoesS3ObjectExistAsync Error: {Message} ", ex.Message);
                return false;
            }
        }
       
        public async Task<DownloadFileResult> LoadFileFromStorageAsync(string key)
        {
            try
            {
                Log.Information("Load file {key} from {bucket}", key, storageProviderSettings.BucketName);
                // Create a GetObject request
                var request = new GetObjectRequest
                {
                    BucketName = storageProviderSettings.BucketName,
                    Key = key
                };

                // Issue request and remember to dispose of the response
                using var response = await s3Client.GetObjectAsync(request);

                // File loaded
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {

                    var ms = new MemoryStream();
                    await response.ResponseStream.CopyToAsync(ms);
                    ms.Position = 0;
                    Log.Debug("Load file {key} from {bucket} OK", key, storageProviderSettings.BucketName);
                    return new DownloadFileResult
                    {
                        MemoryStream = ms,
                        ContentType = response.Headers.ContentType,
                        Size = response.ContentLength
                    };

                }
                Log.Error("Load file {key} from {BucketName} NOT OK", key, storageProviderSettings.BucketName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting {key} from {bucket}: {message}", key, storageProviderSettings.BucketName, ex.Message);
            }

            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A boolean value that represents the success or failure of
        /// deleting all of the objects in the bucket.</returns>
        private async Task<ListObjectsV2Response> DeleteObject(ListObjectsV2Request request, string key, CancellationToken cancellationToken)
        {
            ListObjectsV2Response response;
            do
            {
                response = await s3Client.ListObjectsV2Async(request);
                // If the response is truncated, set the request ContinuationToken
                // from the NextContinuationToken property of the response.
                request.ContinuationToken = response.NextContinuationToken;

                if (cancellationToken.IsCancellationRequested) break;

                response.S3Objects
                    .ForEach(async obj => await s3Client.DeleteObjectAsync(storageProviderSettings.BucketName, key, cancellationToken));

            } while (response.IsTruncated);

            return response;
        }
    }

    public class DownloadFileResult
    {
        public MemoryStream MemoryStream { get; set; } = new();
        public string ContentType { get; set; }
        public long Size { get; set; }
    }

    public class S3FileInfo
    {
        public string Key { get; set; }
        public DateTime LastModified { get; set; }
        public string ETag { get; set; }
        public long Size { get; set; }
    }

}
