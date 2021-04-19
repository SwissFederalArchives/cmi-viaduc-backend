using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Manager.Onboarding.Properties;
using CMI.Utilities.Template;
using Rebex;
using Rebex.Net;
using Serilog;
using IBus = MassTransit.IBus;

namespace CMI.Manager.Onboarding
{
    public class ProcessSftp
    {
        private readonly IBus bus;
        private readonly UserDataAccess dataAccess;
        private readonly DataBuilder dataBuilder;
        private readonly IMailHelper mailHelper;
        private readonly IParameterHelper parameterHelper;


        public ProcessSftp(IBus bus)
        {
            this.bus = bus;

            dataAccess = new UserDataAccess(DbConnectionSetting.Default.ConnectionString);
            parameterHelper = new ParameterHelper();
            mailHelper = new MailHelper();
            dataBuilder = new DataBuilder(bus);

            Licensing.Key = SftpSetting.Default.SftpLicenseKey;
        }

        public void ProcessAll()
        {
            var parameter = parameterHelper.GetSetting<OnboardingSettings>();

            ThrowIfConfigEmpty(parameter.PgpPrivateKey, nameof(parameter.PgpPrivateKey));
            ThrowIfConfigEmpty(parameter.SshPrivateKey, nameof(parameter.SshPrivateKey));
            ThrowIfConfigEmpty(parameter.SshPrivateKeyPassword, nameof(parameter.SshPrivateKeyPassword));

            using (var sftp = new Sftp())
            {
                
                var sftpKeyStream = new MemoryStream(Encoding.UTF8.GetBytes(parameter.SshPrivateKey));
                var sftpKey = new SshPrivateKey(sftpKeyStream, parameter.SshPrivateKeyPassword);

                Log.Information("Connecting to SFTP...");

                sftp.Connect(parameter.SftpServerName);
                sftp.Login(parameter.SftpUserName, sftpKey);

                Log.Information("Login completed");

                foreach (var item in sftp.GetItems("*"))
                {
                    Log.Information("Found item {name}", item.Path);

                    if (item.Name.ToLower().EndsWith(".zip.gpg") &&
                        item.IsFile)
                    {
                        Log.Information("Process file {fileNameWithPath}...", item.Path);

                        var pgpKeyStream = new MemoryStream(Encoding.UTF8.GetBytes(parameter.PgpPrivateKey));
                        var sftpStream = new MemoryStream();
                        var decryptedStream = new MemoryStream();

                        sftp.GetFile(item.Path, sftpStream);
                        sftpStream.Position = 0;

                        Pgp.Decrypt(sftpStream, decryptedStream, pgpKeyStream, parameter.PgpPrivateKeyPassword);

                        ProcessZip(decryptedStream);

                        sftp.DeleteFile(item.Path);
                    }
                }
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void ThrowIfConfigEmpty(string value, string settingName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new Exception($"{nameof(OnboardingService)} kann nicht starten, weil der Parameter-Wert {settingName} fehlt!");
        }

        private void ProcessZip(Stream streamWithZip)
        {
            var identificationSuccessful = false;
            string userExtId = null;
            ZipArchiveEntry txtEntry = null;
            ZipArchiveEntry pdfEntry = null;

            using (var archive = new ZipArchive(streamWithZip))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        txtEntry = entry;

                        using (var txtReader = new StreamReader(entry.Open()))
                        {
                            string line;
                            while ((line = txtReader.ReadLine()) != null)
                            {
                                if (string.Equals(line.Trim(), "Identification: SUCCESSFUL", StringComparison.OrdinalIgnoreCase))
                                {
                                    identificationSuccessful = true;
                                }

                                if (line.StartsWith("Reference number:"))
                                {
                                    userExtId = GetContentFromTxt(line);
                                }
                            }
                        }
                    }

                    if (entry.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                        !entry.Name.EndsWith("_diff.pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        pdfEntry = entry;
                    }
                }

                if (txtEntry == null)
                {
                    throw new Exception("TXT file not found in ZIP file");
                }

                Console.WriteLine("Identification successful? " + identificationSuccessful);

                if (identificationSuccessful)
                {
                    if (userExtId == null)
                    {
                        throw new Exception("'Reference number' not found in TXT file.");
                    }

                    var user = dataAccess.GetUserWitExtId(userExtId);

                    if (user == null)
                    {
                        Log.Information("User not found in Viaduc");
                        return;
                    }

                    if (pdfEntry == null)
                    {
                        throw new Exception("PDF file not found in ZIP file");
                    }

                    UpdateUser(user, txtEntry, pdfEntry);

                    SendMail(user.Id);
                }
            }
        }

        private void UpdateUser(User user, ZipArchiveEntry txtEntry, ZipArchiveEntry pdfEntry)
        {
            using (var txtReader = new StreamReader(txtEntry.Open()))
            {
                string line;
                while ((line = txtReader.ReadLine()) != null)
                {
                    if (line.StartsWith("Given name:"))
                    {
                        user.FirstName = GetContentFromTxt(line);
                    }
                    else if (line.StartsWith("Surname:"))
                    {
                        user.FamilyName = GetContentFromTxt(line);
                    }
                    else if (line.StartsWith("Date of birth:"))
                    {
                        user.Birthday = DateTime.Parse(GetContentFromTxt(line), CultureInfo.GetCultureInfo("de"));
                    }

                    else if (line.StartsWith("Street nr:") && string.IsNullOrWhiteSpace(user.Street))
                    {
                        user.Street = GetContentFromTxt(line);
                    }
                    else if (line.StartsWith("Zip:") && string.IsNullOrWhiteSpace(user.ZipCode))
                    {
                        user.ZipCode = GetContentFromTxt(line);
                    }
                    else if (line.StartsWith("City:") && string.IsNullOrWhiteSpace(user.Town))
                    {
                        user.Town = GetContentFromTxt(line);
                    }
                    else if (line.StartsWith("Country:") && string.IsNullOrWhiteSpace(user.CountryCode))
                    {
                        user.CountryCode = GetContentFromTxt(line).ToUpper();
                    }
                    else if (line.StartsWith("Email:") && string.IsNullOrWhiteSpace(user.EmailAddress))
                    {
                        user.EmailAddress = GetContentFromTxt(line);
                    }
                    else if (line.StartsWith("Cellphone:") && string.IsNullOrWhiteSpace(user.MobileNumber))
                    {
                        user.MobileNumber = GetContentFromTxt(line);
                    }
                }
            }

            dataAccess.UpdateUserProfile(user.Id, user);

            dataAccess.UpdateRejectionReason(null, user.Id);

            var memoryStream = new MemoryStream();
            using (var pdfStream = pdfEntry.Open())
            {
                pdfStream.CopyTo(memoryStream);
                dataAccess.SetIdentifierDocument(user.Id, memoryStream.ToArray(), AccessRoles.RoleOe3);
            }
        }

        private void SendMail(string userId)
        {
            try
            {
                var template = parameterHelper.GetSetting<IdentifizierungErfolgreich>();
                var dataContext = dataBuilder
                    .AddUser(userId)
                    .Create();

                mailHelper.SendEmail(bus, template, dataContext)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }

        private static string GetContentFromTxt(string line)
        {
            var startIndex = line.IndexOf(':') + 1;

            return line.Substring(startIndex).Trim();
        }
    }
}