using System;

namespace CMI.Contract.Parameter.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ValidationAttribute : Attribute
    {
        public ValidationAttribute(string regex, string message)
        {
            Regex = regex;
            Message = message;
        }

        public string Regex { get; set; }
        public string Message { get; set; }
    }
}