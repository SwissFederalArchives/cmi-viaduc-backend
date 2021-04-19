using System;
using System.Collections.Generic;
using CMI.Access.Repository.Properties;
using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using Serilog;

namespace CMI.Access.Repository
{
    public class RepositoryConnectionFactory : IRepositoryConnectionFactory
    {
        private readonly string connectionMode;
        private readonly string password;
        private readonly string serviceUrl;
        private readonly string user;

        public RepositoryConnectionFactory()
        {
            user = Settings.Default.RepositoryUser;
            password = Settings.Default.RepositoryPassword;
            serviceUrl = Settings.Default.RepositoryServiceUrl;
            connectionMode = Settings.Default.ConnectionMode;
        }

        public ISession ConnectToFirstRepository()
        {
            var parameters = new Dictionary<string, string>();

            switch (connectionMode.ToLowerInvariant())
            {
                case "wcf":
                    parameters = new Dictionary<string, string>
                    {
                        [SessionParameter.BindingType] = BindingType.WebServices,
                        [SessionParameter.WebServicesRepositoryService] = serviceUrl,
                        [SessionParameter.WebServicesAclService] = serviceUrl,
                        [SessionParameter.WebServicesDiscoveryService] = serviceUrl,
                        [SessionParameter.WebServicesMultifilingService] = serviceUrl,
                        [SessionParameter.WebServicesNavigationService] = serviceUrl,
                        [SessionParameter.WebServicesObjectService] = serviceUrl,
                        [SessionParameter.WebServicesPolicyService] = serviceUrl,
                        [SessionParameter.WebServicesRelationshipService] = serviceUrl,
                        [SessionParameter.WebServicesVersioningService] = serviceUrl,
                        [SessionParameter.User] = user,
                        [SessionParameter.Password] = password
                    };
                    break;
                case "atom":
                    parameters = new Dictionary<string, string>
                    {
                        [SessionParameter.BindingType] = BindingType.AtomPub,
                        [SessionParameter.AtomPubUrl] = serviceUrl,
                        [SessionParameter.User] = user,
                        [SessionParameter.Password] = password,
                        [SessionParameter.ConnectTimeout] = "360000", // 360 Seconds
                        [SessionParameter.ReadTimeout] = "360000"
                    };
                    break;
            }

            try
            {
                var factory = SessionFactory.NewInstance();
                var repositories = factory.GetRepositories(parameters);
                var session = repositories[0].CreateSession();
                return session;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unknown error while getting session to CMIS repository. Error message is: {Message}", ex.Message);
                throw new RepositoryConnectionException(ex);
            }
        }
    }
}