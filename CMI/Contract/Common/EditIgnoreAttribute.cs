using System;

namespace CMI.Contract.Common
{
    /// <summary>
    ///     Zeigt an, dass diese Property beim Speichern ignoriert wird.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EditIgnoreAttribute : Attribute
    {
    }
}