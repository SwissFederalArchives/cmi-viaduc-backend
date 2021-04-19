using System;
using System.Collections.Generic;
using System.Linq;

namespace CMI.Contract.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EditRequiresFeatureAttribute : Attribute, IEditRequiresFeatureAttribute
    {
        public EditRequiresFeatureAttribute(params ApplicationFeature[] requiredReatures)
        {
            RequiredFeatures = requiredReatures.ToList();
        }

        public List<ApplicationFeature> RequiredFeatures { get; }
    }
}