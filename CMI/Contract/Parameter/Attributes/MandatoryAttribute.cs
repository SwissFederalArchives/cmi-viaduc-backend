using System;

namespace CMI.Contract.Parameter.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MandatoryAttribute : Attribute
    {
        public MandatoryAttribute()
        {
            IsMandatory = true;
        }

        public MandatoryAttribute(bool isManatory)
        {
            IsMandatory = isManatory;
        }

        public bool IsMandatory { get; }
    }
}