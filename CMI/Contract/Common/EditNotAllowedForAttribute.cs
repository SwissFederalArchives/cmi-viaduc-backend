using System;
using System.Collections.Generic;
using System.Linq;

namespace CMI.Contract.Common
{
    /// <summary>
    ///     Zeigt an, dass diese Property nicht mit einer Save-Operation gespeichert werden kann, wenn der zu speichernde
    ///     Benutzer eine in der Liste genannte Rolle hat.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EditNotAllowedForAttribute : Attribute
    {
        public EditNotAllowedForAttribute(params AccessRolesEnum[] disallowedRolesEnum)
        {
            DisallowedRolesEnum = disallowedRolesEnum.ToList();
        }

        public List<AccessRolesEnum> DisallowedRolesEnum { get; }
    }
}