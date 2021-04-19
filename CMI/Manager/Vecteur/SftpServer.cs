using System.IO;
using CMI.Manager.Vecteur.Properties;
using Rebex;
using Rebex.IO.FileSystem;
using Rebex.Net;
using Rebex.Net.Servers;
using Serilog;

namespace CMI.Manager.Vecteur
{
    public class SftpServer
    {
        private FileServer fileServer;

        public void Start()
        {
            Licensing.Key = VecteurSettings.Default.SftpLicenseKey;

            fileServer = new FileServer
            {
                LogWriter = new SerilogWriter()
            };

            var port = (int) VecteurSettings.Default.SftpPort;

            var rsaKey = ServerKey.GetServerPrivateKey();
            fileServer.Keys.Add(rsaKey);

            var dirInfo = new DirectoryInfo(VecteurSettings.Default.BaseDirectory);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            var fs = CreateFileSystem(dirInfo.FullName);

            var user = new FileServerUser("vecteur", VecteurSettings.Default.SftpPassword);
            fileServer.Users.Add(user);
            user.SetFileSystem(fs);

            fileServer.Settings.SshParameters.EncryptionModes &= ~SshEncryptionMode.CBC; // Disable CBC algorithm (security vulnerability)
            fileServer.Bind(port, FileServerProtocol.Sftp);
            fileServer.Start();
        }

        private LocalFileSystemProvider CreateFileSystem(string rootDir)
        {
            var localFileSystem = new LocalFileSystemProvider(rootDir);
            return localFileSystem;
        }

        public void Stop()
        {
            Log.Information("FileServer stopping");
            fileServer.Stop();
            Log.Information("FileServer stopped");
            Log.CloseAndFlush();
        }
    }
}