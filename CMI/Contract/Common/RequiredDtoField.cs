using System;

namespace CMI.Contract.Common
{
    /// <summary>
    ///     Zeigt an, dass dieses Property ein Pflichtfeld ist
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RequiredDtoField : Attribute
    {
    }
}