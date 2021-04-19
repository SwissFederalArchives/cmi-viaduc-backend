using System;

namespace CMI.Contract.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EditEinsichtsgesuchRequiresFeatureAttribute : EditRequiresFeatureAttribute
    {
        public EditEinsichtsgesuchRequiresFeatureAttribute(params ApplicationFeature[] requiredReatures) : base(requiredReatures)
        {
        }
    }
}