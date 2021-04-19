using System;

namespace CMI.Contract.Common
{
    /// <summary>
    ///     Zeigt an, dass diese Property nicht mit einer Save-Operation gespeichert werden kann.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EditNotAllowedAttribute : Attribute
    {
    }
}