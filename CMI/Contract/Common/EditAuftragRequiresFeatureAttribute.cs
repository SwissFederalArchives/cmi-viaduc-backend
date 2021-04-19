using System;

namespace CMI.Contract.Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EditAuftragRequiresFeatureAttribute : EditRequiresFeatureAttribute
    {
        public EditAuftragRequiresFeatureAttribute(params ApplicationFeature[] requiredReatures) : base(requiredReatures)
        {
        }
    }
}