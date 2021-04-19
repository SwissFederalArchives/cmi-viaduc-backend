using System.Dynamic;
using System.Threading.Tasks;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Utilities.Template;
using MassTransit;

namespace CMI.Manager.Order
{
    /// <summary>
    ///     Repräsentiert eine später zu versendende EMail
    /// </summary>
    internal class EMail<T> : ISendable where T : EmailTemplate, new()
    {
        public EMail(ExpandoObject expandoObject, string key)
        {
            ExpandoObject = expandoObject;
            Key = key;
        }

        public ExpandoObject ExpandoObject { get; }

        public string Key { get; }


        async Task ISendable.Send(IBus bus)
        {
            var mailHelper = new MailHelper();
            var paramHelper = new ParameterHelper();
            var templateSetting = (EmailTemplate) paramHelper.GetSetting<T>();
            await mailHelper.SendEmail(bus, templateSetting, ExpandoObject);
        }
    }
}