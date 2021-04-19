using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CMI.Contract.Parameter;
using Serilog;

namespace CMI.Manager.Cache
{
    public class CacheSizeWarner
    {
        private readonly IParameterHelper parameterHelper;


        public CacheSizeWarner(IParameterHelper parameterHelper)
        {
            this.parameterHelper = parameterHelper;
        }

        public event EventHandler<ThresholdExceededEventArgs> ThresholdExceeded;

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var setting = parameterHelper.GetSetting<CacheSettings>();
                    var actualSizeInBytes = CalculateFolderSize(Properties.CacheSettings.Default.BaseDirectory);
                    var thresholdInGb = setting.WarningThresholdCacheSize;
                    var thresholdInBytes = (ulong) thresholdInGb * 1000000000;
                    if (actualSizeInBytes > thresholdInBytes)
                    {
                        Log.Warning($"Disk Size Threshold Exceeded. Threshold={thresholdInBytes}, Actual={actualSizeInBytes}");
                        OnThresholdExceeded(new ThresholdExceededEventArgs(thresholdInBytes, actualSizeInBytes));
                    }
                    else
                    {
                        Log.Information(
                            $"Size of Directory is below threshold, which is good. Threshold={thresholdInBytes}, Actual={actualSizeInBytes}");
                    }

                    Thread.Sleep(TimeSpan.FromHours(24));
                }
            });
        }

        protected void OnThresholdExceeded(ThresholdExceededEventArgs ea)
        {
            if (ThresholdExceeded != null)
            {
                ThresholdExceeded(this, ea);
            }
        }

        protected static ulong CalculateFolderSize(string folder)
        {
            ulong folderSize = 0;
            try
            {
                // Checks if the path is valid or not
                if (!Directory.Exists(folder))
                {
                    return folderSize;
                }

                try
                {
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        if (File.Exists(file))
                        {
                            var finfo = new FileInfo(file);
                            folderSize += (ulong) finfo.Length;
                        }
                    }

                    foreach (var dir in Directory.GetDirectories(folder))
                    {
                        folderSize += CalculateFolderSize(dir);
                    }
                }
                catch (NotSupportedException e)
                {
                    Log.Error("Unable to calculate folder size: {0}", e.Message);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Log.Error("Unable to calculate folder size: {0}", e.Message);
            }

            return folderSize;
        }
    }
}