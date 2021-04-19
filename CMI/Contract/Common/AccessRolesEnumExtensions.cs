using System;

namespace CMI.Contract.Common
{
    public static class AccessRolesEnumExtensions
    {
        public static AccessRolesEnum GetRolePublicClientEnum(this string rolePublicClient)
        {
            return string.IsNullOrWhiteSpace(rolePublicClient)
                ? AccessRolesEnum.Ö1
                : (AccessRolesEnum) Enum.Parse(typeof(AccessRolesEnum), rolePublicClient);
        }
    }
}