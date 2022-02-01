using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using CMI.Contract.DocumentConverter;

namespace CMI.Web.Management.api.Data
{
    public class AbbyyProgressInfo
    {
        public AbbyyProgressInfo()
        {
            ProgressDetails = new ConcurrentDictionary<string, ProgressDetail>();
        }

        public ConcurrentDictionary<string, ProgressDetail> ProgressDetails { get; }

        public void AddOrUpdateProgressInfo(AbbyyProgressEvent eventData)
        {
            // Make sure we have valid data
            if (eventData.File == null)
            {
                return;
            }

            var file = new FileInfo(eventData.File);

            if (ProgressDetails.TryGetValue(eventData.File, out var comparisonValue))
            {
                // Clone the current value
                var detail = new ProgressDetail
                {
                    DetailId = comparisonValue.DetailId,
                    FileName = comparisonValue.FileName,
                    LastUpdate = comparisonValue.LastUpdate,
                    Page = comparisonValue.Page,
                    TotalPages = comparisonValue.TotalPages,
                    Percentage = comparisonValue.Percentage,
                    StartedOn = comparisonValue.StartedOn,
                    ProcessType = comparisonValue.ProcessType,
                    Completed = comparisonValue.Completed,
                    Failed = comparisonValue.Failed,
                    Context = comparisonValue.Context
                };

                // Update the clone with the new values
                if (eventData.EventType == AbbyyEventType.AbbyyOnProgressEvent)
                {
                    // Sometime the event are not processed in order, so 96% can come after 100%
                    detail.Percentage = eventData.Percentage > detail.Percentage ? eventData.Percentage : detail.Percentage;
                }
                else
                {
                    // When export phase starts, pages are reported again. But we keep the highest reported number
                    detail.Page = eventData.Page > detail.Page ? eventData.Page : detail.Page;
                    detail.TotalPages = eventData.TotalPages;
                }

                if (eventData.IsComplete)
                {
                    detail.Completed = true;
                }

                if (eventData.HasFailed)
                {
                    detail.Failed = true;
                }

                // In any case update the last update date.
                detail.LastUpdate = DateTime.Now;

                ProgressDetails.TryUpdate(eventData.File, detail, comparisonValue);
            }
            else
            {
                // If it is a new file, store all possible details
                var detail = new ProgressDetail
                {
                    DetailId = file.FullName,
                    FileName = file.Name,
                    LastUpdate = DateTime.Now,
                    Page = eventData.Page,
                    TotalPages = eventData.TotalPages,
                    Percentage = eventData.Percentage,
                    StartedOn = DateTime.Now,
                    ProcessType = eventData.Process.ToString(),
                    Completed = eventData.IsComplete,
                    Failed = eventData.HasFailed,
                    Context = eventData.Context
                };
                ProgressDetails.TryAdd(eventData.File, detail);
            }

            // As a safety measure we remove all completed values if we have more than 100
            // of those. This is required as the RemoveCompleted function is only called
            // if the Abbyy Activity is watched in the M-C client.
            if (ProgressDetails.Values.Count(d => d.Completed) > 100)
            {
                RemoveCompleted();
            }
        }

        public void RemoveCompleted()
        {
            // Let's delete all detail items that were completed more than 30 seconds ago
            var items = ProgressDetails.Where(d =>
                d.Value.Completed &&
                d.Value.LastUpdate <= DateTime.Now.AddSeconds(-30));

            foreach (var item in items)
            {
                ProgressDetails.TryRemove(item.Key, out _);
            }
        }

        public void RemoveAll()
        {
            ProgressDetails.Clear();
        }

        public void RemoveItem(string key)
        {
            ProgressDetails.TryRemove(key, out _);
        }

        /// <summary>
        ///     Calculates a state for all of the processes.
        ///     If nothing happened for the last 15 minutes, this is a warning sign.
        ///     After 30 minutes, this is clearly a problem and may indicate a serious problem with Abbyy
        /// </summary>
        public void CalculateState()
        {
            foreach (var detail in ProgressDetails)
            {
                var lastUpdated = TimeSpan.FromTicks(DateTime.Now.Ticks - detail.Value.LastUpdate.Ticks);
                detail.Value.ProcessState = "normal";
                if (lastUpdated.TotalMinutes > 15)
                {
                    detail.Value.ProcessState = "warning";
                }

                if (lastUpdated.TotalMinutes > 30)
                {
                    detail.Value.ProcessState = "danger";
                }
            }
        }
    }
}
