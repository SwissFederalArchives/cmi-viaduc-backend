using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using Newtonsoft.Json;

namespace CMI.Web.Frontend.Helpers
{
    /// <summary>
    ///     Aufgabe der Klasse: Ermittlung ob ein vorgegebenes Limit überschritten wurde
    /// </summary>
    public class UsageAnalyzer : IUsageAnalyzer
    {
        private readonly IUsageSettings settings;
        private readonly Dictionary<string, UserUsageStatistic> usageByUser = new Dictionary<string, UserUsageStatistic>();
        private readonly UserUsageStatisticAccess userUsageStatisticAccess;


        public UsageAnalyzer(IUsageSettings settings, UsageType usageType)
        {
            this.settings = settings;
            userUsageStatisticAccess = new UserUsageStatisticAccess(WebHelper.Settings["sqlConnectionString"], usageType);
        }


        public Threshold? GetExceededThreshold(string userId, HttpRequestMessage request)
        {
            var anonymous = string.IsNullOrEmpty(userId);
            if (anonymous && !settings.TrackAnonymous.GetValueOrDefault())
            {
                return null;
            }

            var usage = GetUsageStatistic(userId, request);

            return usage.ExceededThreshold;
        }

        /// <param name="userId"></param>
        /// <param name="request"></param>
        /// <param name="usages">Anzahl Verwendungen</param>
        public void UpdateUsageStatistic(string userId, HttpRequestMessage request, int usages)
        {
            var anonymous = string.IsNullOrEmpty(userId);
            if (anonymous && !settings.TrackAnonymous.GetValueOrDefault())
            {
                return;
            }

            var usage = GetUsageStatistic(userId, request);
            usage.Update(DateTime.Now, usages);

            if (!string.IsNullOrEmpty(userId))
            {
                userUsageStatisticAccess.InsertOrUpdateUserUsage(userId, usage.UsageByInterval);
            }
        }

        public void Reset(string userId, HttpRequestMessage request)
        {
            var anonymous = string.IsNullOrEmpty(userId);
            if (anonymous && !settings.TrackAnonymous.GetValueOrDefault())
            {
                return;
            }

            var usage = GetUsageStatistic(userId, request);
            usage.ResetExceededThresholds();

            if (!string.IsNullOrEmpty(userId))
            {
                userUsageStatisticAccess.InsertOrUpdateUserUsage(userId, usage.UsageByInterval);
            }
        }

        public string GetText(TimeSpan span, string language)
        {
            var translationSettings = FrontendSettingsViaduc.Instance;
            var sb = new StringBuilder();

            if (span.Days > 0)
            {
                sb.AppendFormat("{0} {1} ", span.Days,
                    span.Days > 1
                        ? translationSettings.GetTranslation(language, "general.days", "Tagen")
                        : translationSettings.GetTranslation(language, "general.day", "Tag"));
            }

            if (span.Hours > 0)
            {
                sb.AppendFormat("{0} {1} ", span.Hours,
                    span.Hours > 1
                        ? translationSettings.GetTranslation(language, "general.hours", "Stunden")
                        : translationSettings.GetTranslation(language, "general.hour", "Stunde"));
            }

            if (span.Minutes > 0)
            {
                sb.AppendFormat("{0} {1} ", span.Minutes,
                    span.Minutes > 1
                        ? translationSettings.GetTranslation(language, "general.minutes", "Minuten")
                        : translationSettings.GetTranslation(language, "general.minute", "Minute"));
            }

            if (span.Seconds > 0)
            {
                sb.AppendFormat("{0} {1} ", span.Seconds,
                    span.Seconds > 1
                        ? translationSettings.GetTranslation(language, "general.seconds", "Sekunden")
                        : translationSettings.GetTranslation(language, "general.second", "Sekunde"));
            }

            return sb.ToString().Trim();
        }


        private UserUsageStatistic GetUsageStatistic(string userId, HttpRequestMessage request)
        {
            var anonymous = string.IsNullOrEmpty(userId);
            var usageId = GetUsageUserId(userId, request);
            if (!usageByUser.ContainsKey(usageId))
            {
                var usage = new UserUsageStatistic(settings);
                usage.UsageByInterval = (!anonymous ? userUsageStatisticAccess.GetUserUsage(userId) : null) ?? new UserUsageStatisticData();
                usageByUser.Add(usageId, usage);
            }

            return usageByUser[usageId];
        }

        private static string GetUsageUserId(string userId, HttpRequestMessage request)
        {
            return !string.IsNullOrEmpty(userId) ? userId : GetAnonymousClientId(request);
        }

        private static string GetAnonymousClientId(HttpRequestMessage request)
        {
            return $"Anonymous_{StringHelper.ToIdentifier(SecurityHelper.GetClientIp(request))}";
        }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum UsageInterval
    {
        Threshold5sec = 5,
        Threshold1min = 1 * 60,
        Threshold10min = 10 * 60,
        Threshold1h = 60 * 60,
        Threshold8h = 8 * 60 * 60,
        Threshold7d = 7 * 24 * 60 * 60,
        Threshold30d = 30 * 24 * 60 * 60,
        Threshold365d = 365 * 24 * 60 * 60
    }


    public interface IUsageSettings
    {
        Dictionary<UsageInterval, int> IntervalThresholds { get; }

        bool? TrackAnonymous { get; }
    }

    public class SearchUsageSettings : IUsageSettings
    {
        [JsonIgnore] public Dictionary<UsageInterval, int> IntervalThresholds { get; } = new Dictionary<UsageInterval, int>();

        public bool? TrackAnonymous { get; set; } = false;

        public void Update()
        {
            IntervalThresholds.Clear();
            IntervalThresholds.Add(UsageInterval.Threshold1min, DisplayThreshold1min);
            IntervalThresholds.Add(UsageInterval.Threshold10min, DisplayThreshold10min);
            IntervalThresholds.Add(UsageInterval.Threshold1h, DisplayThreshold1h);
            IntervalThresholds.Add(UsageInterval.Threshold8h, DisplayThreshold8h);
            IntervalThresholds.Add(UsageInterval.Threshold7d, DisplayThreshold7d);
            IntervalThresholds.Add(UsageInterval.Threshold30d, DisplayThreshold30d);
            IntervalThresholds.Add(UsageInterval.Threshold365d, DisplayThreshold365d);
        }

        // ReSharper disable InconsistentNaming
        public int DisplayThreshold1min { get; set; } = 10000;
        public int DisplayThreshold10min { get; set; } = 20000;
        public int DisplayThreshold1h { get; set; } = 50000;
        public int DisplayThreshold8h { get; set; } = 100000;
        public int DisplayThreshold7d { get; set; } = 200000;
        public int DisplayThreshold30d { get; set; } = 300000;

        public int DisplayThreshold365d { get; set; } = 1000000;
        // ReSharper restore InconsistentNaming
    }

    public class DownloadUsageSettings : IUsageSettings
    {
        [JsonIgnore] public Dictionary<UsageInterval, int> IntervalThresholds { get; } = new Dictionary<UsageInterval, int>();

        public bool? TrackAnonymous { get; set; } = false;

        public void Update()
        {
            IntervalThresholds.Clear();
            IntervalThresholds.Add(UsageInterval.Threshold5sec, Threshold5sec);
            IntervalThresholds.Add(UsageInterval.Threshold1min, Threshold1min);
            IntervalThresholds.Add(UsageInterval.Threshold10min, Threshold10min);
            IntervalThresholds.Add(UsageInterval.Threshold1h, Threshold1h);
            IntervalThresholds.Add(UsageInterval.Threshold8h, Threshold8h);
            IntervalThresholds.Add(UsageInterval.Threshold7d, Threshold7d);
            IntervalThresholds.Add(UsageInterval.Threshold30d, Threshold30d);
            IntervalThresholds.Add(UsageInterval.Threshold365d, Threshold365d);
        }

        // ReSharper disable InconsistentNaming
        public int Threshold5sec { get; set; } = 3;
        public int Threshold1min { get; set; } = 10;
        public int Threshold10min { get; set; } = 30;
        public int Threshold1h { get; set; } = 60;
        public int Threshold8h { get; set; } = 100;
        public int Threshold7d { get; set; } = 500;
        public int Threshold30d { get; set; } = 1000;

        public int Threshold365d { get; set; } = 5000;
        // ReSharper restore InconsistentNaming
    }


    public struct Threshold
    {
        public TimeSpan UsageInterval { get; set; }
        public int Usages { get; set; }
        public TimeSpan IsEndingIn { get; set; }
    }


    public class UserUsageStatistic
    {
        private readonly IUsageSettings settings;


        public UserUsageStatistic(IUsageSettings settings)
        {
            this.settings = settings;
        }


        public UserUsageStatisticData UsageByInterval { get; set; }

        public Threshold? ExceededThreshold { get; private set; }


        public void Update(DateTime time, int usages)
        {
            ExceededThreshold = null;

            foreach (var intervalThreshold in settings.IntervalThresholds)
            {
                var interval = (int) intervalThreshold.Key;
                var threshold = intervalThreshold.Value;

                if (!UsageByInterval.ContainsKey(interval))
                {
                    UsageByInterval.Add(interval, new UsageBeginAndCount(time));
                }

                var current = UsageByInterval[interval];
                var elapsed = time.Subtract(current.Begin).TotalSeconds;

                if (elapsed < interval)
                {
                    current.Count += usages;
                }
                else
                {
                    current.Begin = time;
                    current.Count = usages;
                }

                if (current.Count > threshold)
                {
                    ExceededThreshold = new Threshold
                    {
                        UsageInterval = TimeSpan.FromSeconds(interval),
                        Usages = threshold,
                        IsEndingIn = current.Begin + TimeSpan.FromSeconds(interval) - DateTime.Now
                    };
                }
            }
        }

        public void ResetExceededThresholds()
        {
            foreach (var intervalThreshold in settings.IntervalThresholds)
            {
                var interval = (int) intervalThreshold.Key;
                var threshold = intervalThreshold.Value;
                var current = UsageByInterval.ContainsKey(interval) ? UsageByInterval[interval] : null;
                if (current != null && current.Count > threshold)
                {
                    current.Count = 0;
                }
            }

            ExceededThreshold = null;
        }
    }
}