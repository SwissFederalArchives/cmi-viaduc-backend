using System;
using FREngine;
using Serilog;

namespace CMI.Manager.DocumentConverter.Abbyy
{
    public class AbbyyLicense
    {
        private readonly IEnginesPool enginesPool;

        public AbbyyLicense(IEnginesPool enginesPool)
        {
            this.enginesPool = enginesPool;
        }

        public int? GetRemainingPages()
        {
            var engine = enginesPool.GetEngine();
            var isRecycleRequired = false;
            try
            {
                var remainingPages = engine.CurrentLicense.VolumeRemaining[LicenseCounterTypeEnum.LCT_Pages];
                return remainingPages;
            }
            catch (Exception e)
            {
                isRecycleRequired = enginesPool.ShouldRestartEngine(e);
                Log.Error(e, "Unable to query the remaining pages");
                return null;
            }
            finally
            {
                enginesPool.ReleaseEngine(engine, isRecycleRequired);
            }
        }
    }
}