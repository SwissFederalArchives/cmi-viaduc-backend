using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Utilities.Security;
using CMI.Web.Common.Auth;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Web.Common.Helpers
{
    public class MockAuthenticationHelper
    {
        public MockAuthenticationHelper()
        {
            InitMockIdentities();
        }

        public List<MockIdentity> Identities { get; set; }

        public void InitMockIdentities()
        {
            Identities = new List<MockIdentity>();

            try
            {
                var appDataPath = DirectoryHelper.Instance.ConfigDirectory;
                var identitiesPath = Path.Combine(appDataPath, "identities");

                var files = Directory.GetFiles(identitiesPath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var jsonString = File.ReadAllText(file);
                        var mockIdentity = JsonConvert.DeserializeObject<MockIdentity>(jsonString);
                        Identities.Add(mockIdentity);
                    }
                    catch (Exception ey)
                    {
                        Log.Error("Error in InitMockIdentities for {FILE}: {ERROR}", Path.GetFileNameWithoutExtension(file), ey);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in InitMockIdentities: {ERROR}", ex);
            }
        }

        public ClaimInfo[] GetClaimsForUser(string username, string passwordAsCleartext)
        {
            var hashedPassword = HashUtilities.GetMd5Hash(passwordAsCleartext).Replace("-", "");
            var identity = Identities.FirstOrDefault(i =>
                string.Compare(username, i.Username, StringComparison.OrdinalIgnoreCase) == 0
                && string.Compare(hashedPassword, i.Password, StringComparison.OrdinalIgnoreCase) == 0);

            return identity?.Claims;
        }
    }

    public class MockIdentity
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public ClaimInfo[] Claims { get; set; }
    }

    public class MockAuthenticationRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}