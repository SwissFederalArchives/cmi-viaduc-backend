# First steps

## Start up

Clone all necessary projects:

* cmi-viaduc-backend (this project)
* cmi-viaduc-web-frontend
* cmi-viaduc-web-management
* cmi-viaduc-web-core

Build the Angular projects according to their respective read-me.

### Link the Angular dist directories

In order for the Viaduc application to deliver the web sources, these directories must be included.
To do this, link the output directories of both Angular applications to the correct directory of the web applications in the cmi-viaduc-backend project.

1. Clone the current master branch with your Git client into a local folder (ex. C:\Viaduc).

2. Open the command line (cmd) as administrator, change the directory to your local "backend" clone.
Type the following:
    * cd "CMI\Web\CMI.Web.Frontend"
    * mklink /J client "{INSERT-PATH-TO-cmi-viaduc-web-frontend}\dist".

3. Change the directory to your local "backend" clone.
Type the following:
    * cd "CMI\Web\CMI.Web.Management"
    * mklink /J client "{INSERT-PATH-TO-cmi-viaduc-web-management}\dist"

4. Create a database in your SQL server. Customize the connection string and passwords.
    * Create an empty database with the name 'Viaduc'.

5. Authentication<br/>
For the Federal Archive, EIAM is used as the authentication provider.
For running the frontends (especially the management client) a connection to an authentication provider is absolutely necessary. The connection is done via `SAML2` or former `Kentor` [Library](https://github.com/Sustainsys/Saml2).

The EIAM-team provided us with a test system for this purpose for a short time, but it is no longer accessible. It is therefore necessary to implement an alternative. To rebuild an own SAML2-IDP (e.g., for development purposes) [Stainsys.Saml2](https://github.com/Sustainsys/Saml2/tree/develop/Sustainsys.Saml2.StubIdp) can be recommended as a reference.

If such a SAML2-IDP is in use, it is important that the associated certificate is installed on the host machine of the API projects.

6. Secrets file<br/>
  In the directory above (outside) the solution, a file named `Credentials for develop.json` has to be created.
  This file contains passwords, serial numbers and other sensitive data. Placeholders are defined in the configuration files of the services and applications of the solutions. Via a post-build event, these placeholders are replaced with the respective value of the placeholder.
  This file must contain the following entries.
  ```
  {
    "@@CMI.Manager.Viaduc.Properties.DbConnectionSetting.ConnectionString_Credentials@@": "Trusted_Connection=True;",
    "@@CMI.Manager.Onboarding.Properties.DbConnectionSetting.ConnectionString_Credentials@@": "Trusted_Connection=True;",
    "@@CMI.Manager.Order.Properties.DbConnectionSetting.ConnectionString_Credentials@@": "Trusted_Connection=True;",
    "@@CMI.Manager.Asset.Properties.DbConnectionSetting.ConnectionString_Credentials@@": "Trusted_Connection=True;",
    "@@cmiSettings.sqlConnectionString_Credentials@@": "Trusted_Connection=True;",

    "@@CMI.Access.Harvest.Properties.Settings.OracleUser@@": "xxxx",
    "@@CMI.Access.Harvest.Properties.Settings.OraclePassword@@": "xxxx",
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.Settings.OracleUser@@": "xxxx",
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.Settings.OraclePassword@@": "xxxx",
    "@@CMI.Utilities.FormTemplate.Helper.Properties.Settings.DefaultConnection.connectionString_Credentials@@": "User Id=xxxx;Password=xxxx",

    "@@CMI.Utilities.Bus.Configuration.Properties.Settings.RabbitMqUri@@": "rabbitmq://computername/viaduc/",
    "@@CMI.Utilities.Bus.Configuration.Properties.Settings.RabbitMqUserName@@": "viaduc",
    "@@CMI.Utilities.Bus.Configuration.Properties.Settings.RabbitMqPassword@@": "xxxx",

    "@@CMI.Manager.Repository.Properties.Settings.SFTPUser@@": "xxxx",
    "@@CMI.Manager.Repository.Properties.Settings.SFTPPassword@@": "xxxx",

    "@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpPassword@@": "xxxx",

    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.Settings.AlfrescoUser@@": "admin",
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.Settings.AlfrescoPassword@@": "xxxxxx",
    "@@CMI.Access.Repository.Properties.Settings.RepositoryUser@@": "admin",
    "@@CMI.Access.Repository.Properties.Settings.RepositoryPassword@@": "xxxx",

    "@@CMI.Manager.Notification.Properties.NotificationSettings.UserName@@": "xxxx",
    "@@CMI.Manager.Notification.Properties.NotificationSettings.Password@@": "xxxx",

    "@@CMI.Manager.Asset.Properties.Settings.PasswordSeed@@": "a very long string",

    "@@CMI.Engine.MailTemplate.Properties.Settings.PublicClientUrl@@": "localhost/viaduc",
    "@@CMI.Engine.MailTemplate.Properties.Settings.ManagementClientUrl@@": "localhost/management",
    
    "@@CMI.Contract.Parameter.Properties.ParameterSettings.Path@@": "C:\Temp\Viaduc\Parameter",
    
    "@@CMI.Manager.Cache.Properties.CacheSettings.SftpLicenseKey@@": "xxxx",
    "@@CMI.Manager.Onboarding.Properties.SftpSetting.SftpLicenseKey@@": "xxxx",
    "@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpLicenseKey@@": "xxxx",
    "@@CMI.Manager.DocumentConverter.Properties.DocumentConverterSettings.SftpLicenseKey@@": "xxxx",
    "@@CMI.Tools.DocumentConverter.Properties.DocumentConverterSettings.SftpLicenseKey@@": "xxxx",
    "@@CMI.Manager.Asset.Properties.Settings.SftpLicenseKey@@": "xxxx",
    "@@CMI.Manager.Order.Properties.Settings@@": "xxxx",
    "@@cmiSettings.sftpLicenseKey@@": "xxxx",
 
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.AlfrescoServiceUrl@@": "xxx",
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.OracleServiceName@@": "xxx",
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.OracleHost@@": "xxx",
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.OracleSchemaName@@": "xxx",
    "@@CMI.Utilities.DigitalRepository.CreateTestDataHelper.Properties.OraclePort@@": "1521",
    "@@CMI.Manager.Vecteur.Properties.VecteurSettings.Address@@": "xxx",
    "@@CMI.Manager.Vecteur.Properties.VecteurSettings.ApiKey@@": "xxx",

    "@@CMI.Web.Frontend.AppData_Template.ConfigJson.ElasticUrl@@": "xxx",
    "@@CMI.Web.Frontend.AppData_Template.IdpMetadata.Url@@": "xxx",
    "@@CMI.Web.Frontend.AppData_Template.IdpMetadata.DigestValue@@": "xxx",
    "@@CMI.Web.Frontend.AppData_Template.IdpMetadata.SignatureValue@@": "xxx",
    "@@CMI.Web.Frontend.AppData_Template.IdpMetadata.X509Certificate@@": "xxx",
    "@@CMI.Manager.Cache.Properties.CacheSettings.SftpPrivateCertKey@@": "xxx",
    "@@CMI.Manager.Cache.Properties.CacheSettings.SftpPrivateCertPassword@@": "xxx",
    "@@CMI.Manager.DocumentConverter.Properties.DocumentConverterSettings.SftpPrivateCertKey@@": "xxx",
    "@@CMI.Manager.DocumentConverter.Properties.DocumentConverterSettings.SftpPrivateCertPassword@@": "xxx",
    "@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpPrivateCertKey@@": "xxx",
    "@@CMI.Manager.Vecteur.Properties.VecteurSettings.SftpPrivateCertPassword@@": "xxx"
}
```

8. RabbitMq
    1. Install RabbitMq
    2. Log in to the management client at http://localhost:15672/. By default with `user` and password `guest`. 
    3. Create a new virtual host `Viaduc`.
    4. Create a new user with the name `Viaduc` and administrator privileges.
    5. Allow the new user to access the new virtual host.

7. Start the project
    1. Configure several startup projects
    2. All host projects must be started
    3. Start both or the desired web project

## Conventions for namespaces

### Basic policy

The policy for namespaces is

`<Company>.<Concept>.<Subsystem>`

* Company name should be unequivocal.  In the present case, it is **CMI**.
* Concept can be one of the following words `Contract, Access, Engine, Manager, Proxy, Host`.
* The subsystem depends on the architecture. In the context of the project, we have identified the following subsystems `Harvest, Repository, Asset, Index, Order`. This list is not exhaustive. Parts of the code that cannot be assigned to any of these subsystems should use `Common` as the name of the subsystem.

### Exceptions

Because we have "standard" reusable services that are or can be used in other projects, there are exceptions to the above. Namely, we have

* Utilities
* iFX (Infrastucture)

## Folder structure

The folder structure you use corresponds to the namespace. At each point in the namespace, you create a folder in Explorer.

## Folders in the Solutions

Use folders within the Visual Studio Solution to structure your project. This helps you keep track of the various projects.

