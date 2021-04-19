using System.Web;
using System.Web.SessionState;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;

namespace CMI.Web.Common.Auth
{
    public static class UserAccessExtensions
    {
        private const string SessionFeaturesInfo = "FeaturesInfo";

        public static void SetApplicationUser(this HttpSessionState session, User info)
        {
            if (session != null)
            {
                session[SessionFeaturesInfo] = info;
            }
        }

        public static bool HasFeatureStaticContentEdit(this HttpSessionState session)
        {
            var featuresInfo = session?[SessionFeaturesInfo] as User;
            var features = featuresInfo?.Features;
            return features.HasFeature(ApplicationFeature.PublicClientVerwaltenStaticContentEdit);
        }

        public static bool HasFeatureStaticContentEdit(this HttpSessionStateBase session)
        {
            var featuresInfo = session?[SessionFeaturesInfo] as User;
            var features = featuresInfo?.Features;
            return features.HasFeature(ApplicationFeature.PublicClientVerwaltenStaticContentEdit);
        }
    }
}