using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Web.Frontend.ParameterSettings;

namespace CMI.Web.Frontend.api
{
    public class KontingentBestimmer
    {
        private readonly DigitalisierungsbeschraenkungSettings setting;

        public KontingentBestimmer(DigitalisierungsbeschraenkungSettings setting)
        {
            this.setting = setting;
        }

        public KontingentResult BestimmeKontingent(IEnumerable<Ordering> userOrderings, User user)
        {
            if (user.DigitalisierungsbeschraenkungAufgehobenBis.HasValue &&
                user.DigitalisierungsbeschraenkungAufgehobenBis.Value >= DateTime.Now)
            {
                return new KontingentResult
                {
                    AktiveDigitalisierungsauftraege = 0,
                    Digitalisierungesbeschraenkung = int.MaxValue
                };
            }

            var digitalisierungsbeschraenkung = 0;

            switch (user.Access.RolePublicClient.GetRolePublicClientEnum())
            {
                case AccessRolesEnum.Ö2:
                    digitalisierungsbeschraenkung = setting.DigitalisierungsbeschraenkungOe2;
                    break;
                case AccessRolesEnum.Ö3:
                    digitalisierungsbeschraenkung = setting.DigitalisierungsbeschraenkungOe3;
                    break;
                case AccessRolesEnum.AS:
                    digitalisierungsbeschraenkung = setting.DigitalisierungsbeschraenkungAs;
                    break;
                case AccessRolesEnum.BVW:
                    digitalisierungsbeschraenkung = setting.DigitalisierungsbeschraenkungBvw;
                    break;
                case AccessRolesEnum.BAR:
                    digitalisierungsbeschraenkung = setting.DigitalisierungsbeschraenkungBar;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(user),
                        $"Die Rolle: {user.Access.RolePublicClient} ist nicht vorgesehen für die Kontingentberechnung");
            }

            var aktiveDigitalisierungsAuftraege = userOrderings
                .Where(o => o.Type == OrderType.Digitalisierungsauftrag)
                .SelectMany(i => i.Items)
                .Count(i => i.Status != OrderStatesInternal.Abgebrochen &&
                            i.Status != OrderStatesInternal.DigitalisierungAbgebrochen &&
                            i.Status != OrderStatesInternal.ZumReponierenBereit &&
                            i.Status != OrderStatesInternal.Abgeschlossen);

            return new KontingentResult
            {
                AktiveDigitalisierungsauftraege = aktiveDigitalisierungsAuftraege,
                Digitalisierungesbeschraenkung = digitalisierungsbeschraenkung
            };
        }
    }
}