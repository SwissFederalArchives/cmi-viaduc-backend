using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator.Properties;
using MassTransit;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace CMI.Utilities.Logging.Configurator
{
    /// <summary>
    ///     Die Klasse erlaubt es, Logmeldungen via E-Mail zu versenden.
    ///     Weitere Informationen finden sich beim Setting SendMailOnError.
    /// </summary>
    public class MailErrorSink : ILogEventSink
    {
        private readonly IBusControl bus;
        private readonly List<ConfigEntry> config = new List<ConfigEntry>();
        private readonly bool initOk;
        private readonly MessageTemplateTextFormatter messageTemplateTextFormatter;


        public MailErrorSink(string outputTemplate, IFormatProvider formatProvider)
        {
            try
            {
                messageTemplateTextFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
                bus = BusConfigurator.ConfigureBus(MonitoredServices.NotMonitored, (cfg, host) => { });
                bus.Start();
                ReadConfig();

                initOk = true;
            }
            catch (Exception exception)
            {
                // Da der Logger noch nicht bereit ist auf die Console schreiben 
                Console.WriteLine($" ***** Exception beim initialisieren der Klasse MailErrorSink *****\nException: {exception}");
            }
        }

        public async void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < LogEventLevel.Error ||
                !initOk)
            {
                return;
            }

            var output = new StringWriter();
            messageTemplateTextFormatter.Format(logEvent, output);
            var message = output.ToString();

            foreach (var entry in config)
            {
                if (!message.Contains(entry.LogMessagePart))
                {
                    continue;
                }

                try
                {
                    var endpoint = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.NotificationManagerMessageQueue));
                    var emailMessage = new EmailMessage
                    {
                        To = entry.MailAddresses,
                        Subject = entry.MailTitle,
                        Body = WebUtility.HtmlEncode(message).Replace("\n", "<br>\n")
                    };

                    endpoint.Send<IEmailMessage>(emailMessage).Wait();

                    Log.Information("Fehlermeldung wird versandt an {MailAddresses}.", entry.MailAddresses);
                }
                catch (Exception ex)
                {
                    // Als Warnung anstatt Error damit sicher kein Loop entsteht
                    Log.Warning(ex, "Fehler beim Versenden der Fehlermeldung an {MailAddresses}", entry.MailAddresses);
                }
            }
        }

        private void ReadConfig()
        {
            if (LogSettings.Default.SendMailOnError == null)
            {
                return;
            }

            foreach (var line in LogSettings.Default.SendMailOnError.Replace("\r", "").Split('\n'))
            {
                var parts = line.Split(new[] {"->"}, StringSplitOptions.None);
                var ok = false;

                if (parts.Length == 3)
                {
                    var logMessagePart = parts[0].Trim();
                    var mailTitle = parts[1].Trim();
                    var mailAddresses = parts[2].Trim();

                    if (mailAddresses != string.Empty)
                    {
                        ok = true;
                        config.Add(new ConfigEntry {LogMessagePart = logMessagePart, MailTitle = mailTitle, MailAddresses = mailAddresses});
                    }
                }

                if (!ok && !string.IsNullOrWhiteSpace(line))
                    // Da der Logger noch nicht bereit ist auf die Console schreiben 
                {
                    Console.WriteLine($"Der Parameter SendMailOnError enthält die folgende ungültige Zeile: {line}");
                }
            }
        }


        private struct ConfigEntry
        {
            public string LogMessagePart { get; set; }
            public string MailTitle { get; set; }
            public string MailAddresses { get; set; }
        }
    }


    public static class MailErrorSinkExtension
    {
        public static LoggerConfiguration MailError(this LoggerSinkConfiguration loggerConfiguration,
            string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}",
            IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new MailErrorSink(outputTemplate, formatProvider));
        }
    }
}