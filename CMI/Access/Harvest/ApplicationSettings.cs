using System;
using CMI.Access.Harvest.Properties;
using Serilog;

namespace CMI.Access.Harvest
{
    public class ApplicationSettings
    {
        public ApplicationSettings()
        {
            try
            {
                DigitalRepositoryElementIdentifier = Settings.Default.DigitalRepositoryElementIdentifier;
                OutputSQLExecutionTimes = Settings.Default.OutputSQLExecutionTimes;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invalid setting in configuration file. Check the DigitalRepositoryIdentifier and OutputSQLExecutionTimes");
                DigitalRepositoryElementIdentifier = "-1";
                OutputSQLExecutionTimes = false;
            }
        }

        public string DigitalRepositoryElementIdentifier { get; set; }
        public bool OutputSQLExecutionTimes { get; set; }
    }
}