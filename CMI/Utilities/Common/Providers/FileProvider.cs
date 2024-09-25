using System;
using System.IO;
using System.Threading.Tasks;
using CMI.Utilities.Common.Helpers;
using Serilog;
using Path = System.IO.Path;

namespace CMI.Utilities.Common.Providers
{
    public class FileProvider : IStorageProvider
    {
        public async Task CopyFileAsync(FileInfo sourceFile, string relPath, string extension, string targetDirectory)
        {
            Log.Information("FileProvider CopyFileAsync {relPath} and {fullName}", relPath, sourceFile.FullName);
            var file = new FileInfo(Path.ChangeExtension(sourceFile.FullName, extension));
            if (file.Exists)
            {
                var targetFile = new FileInfo(Path.Combine(targetDirectory, PathHelper.CreateShortValidUrlName(relPath, false), PathHelper.CreateShortValidUrlName(file.Name, true)));
                await CopyFileInternal(targetFile, file);
            }
        }

        public async Task<MemoryStream> ReadFileAsync(Uri fileUir)
        {
            Log.Information("FileProvider ReadFileAsync AbsolutePath: {AbsolutePath}, OriginalString: {OriginalString}", fileUir.AbsolutePath, fileUir.OriginalString);

            var memoryStream = new MemoryStream();
            try
            {
                if (File.Exists(fileUir.AbsolutePath))
                {
                    using (var stream = File.Open(fileUir.AbsolutePath, FileMode.Open))
                    {
                        var result = new byte[stream.Length];
                        await stream.ReadAsync(result, 0, (int) stream.Length);

                        stream.Position = 0;
                        await stream.CopyToAsync(memoryStream);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "ReadFileAsync an error has occurred");
                await ReadFileFromNetworkDrive(fileUir, memoryStream);
            }

            if (memoryStream.Position == 0)
            {
                await ReadFileFromNetworkDrive(fileUir, memoryStream);
            }

            return await Task.FromResult(memoryStream);
        }

        private static async Task ReadFileFromNetworkDrive(Uri fileUir, MemoryStream memoryStream)
        {
            var networkFile = fileUir.LocalPath;
            Log.Information("FileProvider ReadFileFromNetworkDrive {networkFile}", networkFile);
            try
            {
                using var stream = File.OpenRead(networkFile);
                var result = new byte[stream.Length];
                await stream.ReadAsync(result, 0, (int) stream.Length);

                stream.Position = 0;
                await stream.CopyToAsync(memoryStream);
            }
            catch (Exception e)
            {
                Log.Error(e, "FileRead Exception {networkFile}", networkFile);
                throw e;
            }
        }

        private async Task CopyFileInternal(FileInfo targetFile, FileInfo sourceFile)
        {
            // Delete existing file
            if (targetFile.Exists)
            {
                targetFile.Delete();
            }

            if (!targetFile.Directory!.Exists)
            {
                targetFile.Directory.Create();
            }

            using var sourceStream = File.Open(sourceFile.FullName, FileMode.Open);
            {
                using var destinationStream = File.Create(targetFile.FullName);
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }
        }
    }
}
