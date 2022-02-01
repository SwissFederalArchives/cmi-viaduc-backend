using System;
using System.Text;
using System.Windows.Forms;
using Autofac;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Utilities.Bus.Configuration;
using MassTransit;
using Newtonsoft.Json;

namespace CMI.Manager.ExternalContent.WinFormsTestClient
{
    public partial class MainForm : Form
    {
        private static IBusControl bus;
        private readonly IRequestClient<GetDigitizationOrderData> orderClient;

        public MainForm()
        {
            InitializeComponent();
            LoadBus();
            orderClient = GetOrderClient();
        }


        public static IRequestClient<GetDigitizationOrderData> GetOrderClient()
        {
            // ReSharper disable once RedundantAssignment
            var requestTimeout = TimeSpan.FromMinutes(1);
            #if DEBUG
                requestTimeout = TimeSpan.FromMinutes(10);
            #endif

            return bus.CreateRequestClient<GetDigitizationOrderData>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.ManagementApiGetDigitizationOrderData), requestTimeout);
        }

        private static void LoadBus()
        {
            // Configure Bus
            var containerBuilder = new ContainerBuilder();
            BusConfigurator.ConfigureBus(containerBuilder);
            var container = containerBuilder.Build();

            bus = container.Resolve<IBusControl>();
            bus.Start();
        }

        private async void cmdGetDigitizationData_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var text = txtArchiveRecordId.Text;
                var archiveRecordId = Convert.ToInt32(text.Contains("(") ? text.Substring(0, text.IndexOf("(", StringComparison.Ordinal)) : text);
                var result = (await orderClient.GetResponse<GetDigitizationOrderDataResponse>(new GetDigitizationOrderData {ArchiveRecordId = archiveRecordId.ToString()})).Message;
                if (result.Result.Success)
                {
                    txtResult.Text = JsonConvert.SerializeObject(result.Result.DigitizationOrder, Formatting.Indented);
                }
                else
                {
                    txtResult.Text = "FEHLER" + Environment.NewLine +
                                     "======" + Environment.NewLine + Environment.NewLine + result.Result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                txtResult.Text = ex.Message;
            }

            Cursor = Cursors.Default;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            bus.Stop();
        }

        private void cmdTestIsUsageCopy_Click(object sender, EventArgs e)
        {
            var orderData = DigitalisierungsAuftrag.LoadFromFile(@"C:\Temp\DigiOrder_4891626.xml", Encoding.UTF8);
            var result = DigitizationOrderBuilder.IsUsageCopy(orderData);
            MessageBox.Show(result.ToString());
        }
    }
}