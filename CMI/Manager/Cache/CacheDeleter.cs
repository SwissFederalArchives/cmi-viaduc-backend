using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using CMI.Utilities.Common;
using Serilog;

namespace CMI.Manager.Cache
{
    public class CacheDeleter
    {
        private readonly IFileSystem fileSystem;
        private readonly IParameterHelper parameterHelper;
        private readonly ISleeper sleeper;

        public CacheDeleter(IParameterHelper parameterHelper, IFileSystem fileSystem, ISleeper sleeper)
        {
            this.parameterHelper = parameterHelper;
            this.fileSystem = fileSystem;
            this.sleeper = sleeper;
        }

        public Task Start(bool isLoop = true)
        {
            return Task.Run(() =>
            {
                do
                {
                    var setting = parameterHelper.GetSetting<CacheSettings>();

                    RetentionCategory[] categories =
                    {
                        new RetentionCategory(setting.RetentionSpanUsageCopyAb, CacheRetentionCategory.UsageCopyAB),
                        new RetentionCategory(setting.RetentionSpanUsageCopyBarOrAS, CacheRetentionCategory.UsageCopyBarOrAS),
                        new RetentionCategory(setting.RetentionSpanUsageCopyEb, CacheRetentionCategory.UsageCopyEB),
                        new RetentionCategory(setting.RetentionSpanUsageCopyPublic, CacheRetentionCategory.UsageCopyPublic),
                        new RetentionCategory(setting.RetentionSpanUsageCopyBenutzungskopie, CacheRetentionCategory.UsageCopyBenutzungskopie)
                    };

                    foreach (var category in categories.Where(c => c.RetentionSpan != TimeSpan.MaxValue))
                    {
                        var catDir = Path.Combine(Properties.CacheSettings.Default.BaseDirectory, category.CacheRetentionCategory.ToString());
                        try
                        {
                            foreach (var fileName in fileSystem.Directory.GetFiles(catDir))
                            {
                                var fi = fileSystem.FileInfo.FromFileName(fileName);
                                DateTime fileTime;

                                if (category.CacheRetentionCategory == CacheRetentionCategory.UsageCopyEB)
                                {
                                    fileTime = fi.LastAccessTime;
                                }
                                else
                                {
                                    fileTime = fi.CreationTime;
                                }

                                if (fileTime < DateTime.Now - category.RetentionSpan)
                                {
                                    try
                                    {
                                        fileSystem.File.Delete(fileName);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Warning(ex, "Can't delete File '" + fileName + "'");
                                    }
                                }
                            }
                        }
                        catch (DirectoryNotFoundException)
                        {
                            Log.Warning("Directory not found: " + catDir);
                        }
                    }

                    sleeper.Sleep(TimeSpan.FromMinutes(15));
                } while (isLoop);
            });
        }
    }
}