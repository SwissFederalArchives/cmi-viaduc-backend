using System.Threading.Tasks;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using MassTransit;

namespace CMI.Utilities.Template
{
    public interface IMailHelper
    {
        Task SendEmail(IBus bus, EmailTemplate templateSetting, object dataContext, bool logAllowed = true);

        string TransformToHtml(string template, object dataSource, string language = "de");
        string TransformToText(string template, object dataSource, string language = "de");
    }
}