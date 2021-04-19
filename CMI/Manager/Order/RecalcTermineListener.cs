using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using Serilog;

namespace CMI.Manager.Order
{
    public class RecalcTermineListener
    {
        private readonly DigitalisierungsTerminManager terminManager;

        public RecalcTermineListener(IOrderDataAccess orderDataAccess)
        {
            terminManager = new DigitalisierungsTerminManager(orderDataAccess);
        }

        public void Start()
        {
            ParameterHelper.ParameterSaved += HelperOnParameterSaved;
        }

        public void Stop()
        {
            ParameterHelper.ParameterSaved -= HelperOnParameterSaved;
        }

        private async void HelperOnParameterSaved(object sender, ParameterSavedEventArgs e)
        {
            await RecalcTermine(e);
        }

        private async Task RecalcTermine(ParameterSavedEventArgs e)
        {
            try
            {
                if (e.SettingType != typeof(KontingentDigitalisierungsauftraegeSetting))
                {
                    return;
                }

                DigitalisierungsKategorie kategorie;
                var newKontingent = e.Parameter.Value.ToString();
                var name = e.Parameter.Name
                    .Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault();

                switch (name)
                {
                    case nameof(KontingentDigitalisierungsauftraegeSetting.Oeffentlichkeit):
                        kategorie = DigitalisierungsKategorie.Oeffentlichkeit;
                        break;
                    case nameof(KontingentDigitalisierungsauftraegeSetting.Intern):
                        kategorie = DigitalisierungsKategorie.Intern;
                        break;
                    case nameof(KontingentDigitalisierungsauftraegeSetting.Amt):
                        kategorie = DigitalisierungsKategorie.Amt;
                        break;
                    case nameof(KontingentDigitalisierungsauftraegeSetting.DDS):
                        kategorie = DigitalisierungsKategorie.Forschungsgruppe;
                        break;
                    case nameof(KontingentDigitalisierungsauftraegeSetting.Gesuche):
                        kategorie = DigitalisierungsKategorie.Gesuch;
                        break;
                    default:
                        Log.Warning("Unbekannter Kontingentparameter {PARAMETER}", e.Parameter.Name);
                        return;
                }

                var parser = new DigitalisierungsKontingentParser();
                var kontingent = parser.Parse(newKontingent);

                Log.Information(
                    "Digitalisierungstermine für Kategorie {KATEGORIE} werden neu berechnet, da sich der Parameter für das Kontingent geändert hat.",
                    kategorie.ToString());
                await terminManager.RecalcTermine(kategorie, kontingent);
                Log.Information("Neuberechnung für Digitalisierungstermine der Kategorie {KATEGORIE} abgeschlossen.", kategorie.ToString());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fehler bei der Neuberechnung der Digitalisierungstermine der Kategorie {KATEGORIE}.", e?.Parameter?.Value);
            }
        }
    }
}