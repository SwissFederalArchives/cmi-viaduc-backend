using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Contract.Common;
using Serilog;

namespace CMI.Engine.Asset
{
    public class PreparationTimeCalculator : IPreparationTimeCalculator
    {
        /// <summary>
        ///     Schätzt die Gesamtdauer, welche benötigt wird, um die Gebrauchskopie zu erstellen
        /// </summary>
        public TimeSpan EstimatePreparationDuration(List<ElasticArchiveRecordPackage> primaryData, int conversionRateAudioInKbs,
            int conversionRateVideoInKbs)
        {
            var duration = new TimeSpan(0);

            foreach (var package in primaryData)
            {
                if (package.RepositoryExtractionDuration.HasValue &&
                    package.FulltextExtractionDuration.HasValue)
                {
                    duration += package.RepositoryExtractionDuration.Value;
                    duration += package.FulltextExtractionDuration.Value;
                }
                else
                {
                    Log.Warning(
                        "Data missing in elastic for estimating 'Aufbereitungszeit für Gebrauchskopie'. Missing fields: repositoryExtractionDuration and/or fulltextExtractionDuration");
                }

                foreach (var item in package.Items.Where(item => item.Type == ElasticRepositoryObjectType.File))
                {
                    var extension = Path.GetExtension(item.Name);

                    if (".mp4".Equals(extension, StringComparison.InvariantCultureIgnoreCase))
                        // ReSharper disable once PossibleLossOfFraction
                    {
                        duration += TimeSpan.FromSeconds(item.SizeInBytes / 1000 / conversionRateVideoInKbs);
                    }

                    if (".wav".Equals(extension, StringComparison.InvariantCultureIgnoreCase))
                        // ReSharper disable once PossibleLossOfFraction
                    {
                        duration += TimeSpan.FromSeconds(item.SizeInBytes / 1000 / conversionRateAudioInKbs);
                    }
                }
            }

            return duration;
        }
    }
}