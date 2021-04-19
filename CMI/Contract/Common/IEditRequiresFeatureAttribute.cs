using System.Collections.Generic;

namespace CMI.Contract.Common
{
    public interface IEditRequiresFeatureAttribute
    {
        List<ApplicationFeature> RequiredFeatures { get; }
    }
}