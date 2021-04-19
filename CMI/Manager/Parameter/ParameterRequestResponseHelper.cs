using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Parameter.GetParameter;
using Serilog;

namespace CMI.Manager.Parameter
{
    public class ParameterRequestResponseHelper
    {
        private static ParameterRequestResponseHelper instance;

        private readonly object locker = new object();


        public static ParameterRequestResponseHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    Start();
                }

                return instance;
            }
        }

        private List<Contract.Parameter.Parameter> Parameters { get; set; } = new List<Contract.Parameter.Parameter>();
        private List<Contract.Parameter.Parameter> ParametersInProgress { get; set; } = new List<Contract.Parameter.Parameter>();

        private List<string> ErrorMessages { get; set; } = new List<string>();

        public static void Start()
        {
            if (instance == null)
            {
                instance = new ParameterRequestResponseHelper();
            }
        }

        private async Task LoadParameters()
        {
            // Todo: Anstatt mit Publish eine Parameterabfrage zu starten, wo dann alle über einen Rückruf
            // den Status zurückmelden, müsste eine elegantere Methode gefunden werden.
            await ParameterService.ParameterBus.Publish(new GetParameterEvent());

            Log.Verbose($"{DateTime.Now} ********************* Get Event started *****************************");
            await Task.Delay(10000);

            Log.Verbose($"{DateTime.Now} ********************* Get Event ENDED *****************************");

            Parameters = new List<Contract.Parameter.Parameter>();
            Parameters.AddRange(ParametersInProgress);
            ParametersInProgress = new List<Contract.Parameter.Parameter>();
        }

        public void AppendParam(Contract.Parameter.Parameter[] p)
        {
            lock (locker)
            {
                foreach (var param in p)
                {
                    ParametersInProgress.Add(param);
                }
            }
        }

        public async Task<Contract.Parameter.Parameter[]> GetParameters()
        {
            await instance.LoadParameters();
            return Parameters.ToArray();
        }

        public void AppendErrorMessage(string s)
        {
            ErrorMessages.Add(s);
        }

        public string[] GetErrorMessages()
        {
            return ErrorMessages.ToArray();
        }

        public void ClearErrorMessages()
        {
            ErrorMessages = new List<string>();
        }
    }
}