using System.Collections.Generic;
using System.Linq;
using System.Security;
using CMI.Contract.Common;
using CMI.Utilities.Logging.Configurator;

namespace CMI.Access.Sql.Viaduc
{
    public static class ApplicationAccessExtensions
    {
        public static bool HasFeature(this IEnumerable<ApplicationFeature> features, params ApplicationFeature[] feature)
        {
            return features != null && features.Any(feature.Contains);
        }

        public static bool HasFeature(this IApplicationFeaturesAccess access, params ApplicationFeature[] feature)
        {
            return access.ApplicationFeatures.HasFeature(feature);
        }

        public static void AssertFeatureOrThrow(this IEnumerable<ApplicationFeature> features, ApplicationFeature[] feature)
        {
            if (!features.HasFeature(feature))
            {
                throw new ForbiddenException(
                    $"Die von Ihnen gewünschte Operation kann nicht ausgeführt werden. Ihnen fehlt das Recht '{feature}' um die Operation durchzuführen.");
            }
        }

        public static void AssertFeatureOrThrow(this IApplicationFeaturesAccess access, params ApplicationFeature[] feature)
        {
            access.ApplicationFeatures.AssertFeatureOrThrow(feature);
        }

        public static bool HasRole(this IEnumerable<ApplicationRole> roles, string roleIdentifier)
        {
            roleIdentifier = roleIdentifier.ToUpperInvariant();
            return roles != null && roles.Any(role => roleIdentifier.Equals(role.Identifier.ToUpperInvariant()));
        }

        public static bool HasRole(this IApplicationRolesAccess access, string roleIdentifier)
        {
            return access.ApplicationRoles.HasRole(roleIdentifier);
        }

        public static void AssertRoleOrThrow(this IEnumerable<ApplicationRole> roles, string roleIdentifier)
        {
            if (!roles.HasRole(roleIdentifier))
            {
                throw new SecurityException($"{roleIdentifier} missing");
            }
        }

        public static void AssertRoleOrThrow(this IApplicationRolesAccess access, string roleIdentifier)
        {
            access.ApplicationRoles.AssertRoleOrThrow(roleIdentifier);
        }
    }

    public interface IApplicationAccess
    {
        string UserId { get; }
    }

    public interface IApplicationFeaturesAccess : IApplicationAccess
    {
        IList<ApplicationFeature> ApplicationFeatures { get; }
    }

    public interface IApplicationRolesAccess : IApplicationAccess
    {
        IList<ApplicationRole> ApplicationRoles { get; }
    }
}