using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Logging.Configurator;

namespace CMI.Web.Common.Helpers
{
    public class CheckCustomAttributes
    {
        public static void CheckRequiredAttribute(User postData)
        {
            foreach (var propertyInfo in typeof(User).GetProperties())
            {
                var neuerWert = propertyInfo.GetValue(postData);

                if (propertyInfo.GetCustomAttributes(typeof(RequiredDtoField), false).Length == 0)
                {
                    continue;
                }

                if (neuerWert == null || neuerWert is string s && string.IsNullOrWhiteSpace(s))
                {
                    throw new BadRequestException($"Das Feld {propertyInfo.Name} ist ein Pflichtfeld");
                }
            }
        }

        public static void CheckEditNotAllowedAttribute(User originalUser, User postData)
        {
            foreach (var propertyInfo in typeof(User).GetProperties())
            {
                var originalWert = propertyInfo.GetValue(originalUser);
                var neuerWert = propertyInfo.GetValue(postData);

                if (originalWert.IsEquivalent(neuerWert))
                {
                    continue;
                }

                if (propertyInfo.GetCustomAttributes(typeof(EditNotAllowedAttribute), false).Length > 0)
                {
                    throw new ForbiddenException(
                        $"Es wurde versucht, das Feld {propertyInfo.Name} vom Wert {originalWert} zum Wert {neuerWert} zu aktualisieren. Diese Operation ist aber generell durch ein [EditNotAllowedAttribute] verboten.");
                }
            }
        }

        public static void CheckEditNotAllowedForAttribute(User originalUser, User postData)
        {
            foreach (var propertyInfo in typeof(User).GetProperties())
            {
                var originalWert = propertyInfo.GetValue(originalUser);
                var neuerWert = propertyInfo.GetValue(postData);


                if (originalWert is DateTime originalWertDate && neuerWert is DateTime neuerWertDate)
                {
                    if (Equals(originalWertDate, neuerWertDate))
                    {
                        continue;
                    }
                }
                else if (originalWert.IsEquivalent(neuerWert))
                {
                    continue;
                }


                var editNotAllowedForAttribute = propertyInfo.GetCustomAttributes(typeof(EditNotAllowedForAttribute), false)
                    .OfType<EditNotAllowedForAttribute>().FirstOrDefault();

                if (editNotAllowedForAttribute != null &&
                    editNotAllowedForAttribute.DisallowedRolesEnum.Any(r => r.ToString() == originalUser.RolePublicClient))
                {
                    throw new ForbiddenException(
                        $"Es wurde versucht, das Feld {propertyInfo.Name} vom Wert {originalWert} zum Wert {neuerWert} zu aktualisieren. Diese Operation auf Benutzern mit der Rolle-Public-Client '{originalUser.RolePublicClient}' durch ein [EditNotAllowedForAttribute] verboten.");
                }
            }
        }

        /// <summary>
        ///     EditRequiresFeatureAttribute prüfen
        /// </summary>
        /// <param name="logedInUserApplicationFeatureList">Applications Features List des eingeloggten Benuters</param>
        /// <param name="originalUser"></param>
        /// <param name="postData"></param>
        public static void CheckEditRequiresFeatureAttribute(IList<ApplicationFeature> logedInUserApplicationFeatureList, User originalUser,
            User postData)
        {
            foreach (var propertyInfo in typeof(User).GetProperties())
            {
                var originalWert = propertyInfo.GetValue(originalUser);
                var neuerWert = propertyInfo.GetValue(postData);

                if (originalWert.IsEquivalent(neuerWert))
                {
                    continue;
                }


                var requiredFeatureAttribute = propertyInfo.GetCustomAttributes(typeof(EditRequiresFeatureAttribute), false)
                    .OfType<EditRequiresFeatureAttribute>().FirstOrDefault();

                if (requiredFeatureAttribute == null)
                {
                    continue;
                }

                if (!logedInUserApplicationFeatureList.Intersect(requiredFeatureAttribute.RequiredFeatures).Any())
                {
                    throw new ForbiddenException(
                        $"Es wurde versucht, das Feld {propertyInfo.Name} vom Wert {originalWert} zum Wert {neuerWert} zu aktualisieren. Dafür wird das Feature {string.Join(" ", requiredFeatureAttribute.RequiredFeatures)} benötigt, welches Sie nicht haben.");
                }
            }
        }
    }
}