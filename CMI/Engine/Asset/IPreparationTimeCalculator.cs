using System;
using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Engine.Asset
{
    public interface IPreparationTimeCalculator
    {
        /// <summary>
        ///     Schätzt die Gesamtdauer, welche benötigt wird, um die Gebrauchskopie zu erstellen
        /// </summary>
        TimeSpan EstimatePreparationDuration(List<ElasticArchiveRecordPackage> primaryData, int conversionRateAudioInKbs,
            int conversionRateVideoInKbs);
    }
}