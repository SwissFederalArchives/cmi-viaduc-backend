using CMI.Utilities.Transkribus.Properties;
using System;
using System.IO;
using System.Threading.Tasks;
using Rebex;
using Rebex.Net;
using Serilog;

namespace CMI.Utilities.Transkribus;

public class TranskribusSftpServer
{

    internal async Task UploadFile(FileInfo toBeUpload)
    {
        var host = TranskribusSettings.Default.TranskribusSftpHost;
        var port = TranskribusSettings.Default.TranskribusSftpPort;
        var user = TranskribusSettings.Default.TrankribusSFTPUser;
        var pwd = TranskribusSettings.Default.TranskribusSftpPassword;
        Licensing.Key = TranskribusSettings.Default.TranskribusSFTPKeyFile;
        using (var client = new Sftp())
        {
            await client.ConnectAsync(host, port);
            await client.LoginAsync(user, pwd);
            await client.PutFileAsync(toBeUpload.FullName, $"{client.GetCurrentDirectory()}{toBeUpload.Name}");
        }
    }

    internal async Task<bool> DownloadAndStoreFile()
    {
        var host = TranskribusSettings.Default.TranskribusSftpHost;
        var port = TranskribusSettings.Default.TranskribusSftpPort;
        var user = TranskribusSettings.Default.TrankribusSFTPUser;
        var pwd = TranskribusSettings.Default.TranskribusSftpPassword;
        Licensing.Key = TranskribusSettings.Default.TranskribusSFTPKeyFile;

        try
        {
            using (var client = new Sftp())
            {
                await client.ConnectAsync(host, port);
                await client.LoginAsync(user, pwd);

                var directoryInfo = new DirectoryInfo(client.GetCurrentDirectory() + "OutputHTR");

                foreach (var file in directoryInfo.GetFiles())
                {
                    if (!await client.FileExistsAsync(file.FullName))
                    {
                        throw new FileNotFoundException($"File '{file.FullName}' not found on SFTP server");
                    }

                    var lengthOfContent = await client.GetFileAsync(file.FullName, Path.Combine(TranskribusSettings.Default.BaseDirectory, file.Name));

                    if (!File.Exists(Path.Combine(TranskribusSettings.Default.BaseDirectory, file.Name)))
                    {
                        throw new InvalidOperationException($"Was unable to download file  {Path.Combine(TranskribusSettings.Default.BaseDirectory, file.Name)} from sftp server");
                    }
                }

                return true;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
            throw;
        }
    }
}