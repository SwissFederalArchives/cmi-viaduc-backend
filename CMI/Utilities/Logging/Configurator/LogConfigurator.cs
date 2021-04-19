using System;
using System.Diagnostics;
using System.IO;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator.Properties;
using RabbitMQ.Client;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.RabbitMQ;
using Serilog.Sinks.RabbitMQ.Sinks.RabbitMQ;

namespace CMI.Utilities.Logging.Configurator
{
    public static class LogConfigurator
    {
        public static void ConfigureForWeb(string mainAssembly)
        {
            PrepareRabbitMq();

            Log.Logger = GetBasicLoggerConfiguration()
                .Enrich.WithProperty("MainAssembly", mainAssembly)
                .WriteTo.RabbitMQ(RabbitMqClientConfiguration(), RabbitMQSinkConfiguration(), new JsonFormatter())
                .WriteTo.MailError()
                .CreateLogger();
        }

        public static void ConfigureForService()
        {
            LoggerConfiguration config;

            PrepareRabbitMq();

            switch (LogSettings.Default.LogLevel?.ToLowerInvariant())
            {
                case "verbose":
                    config = GetBasicLoggerConfiguration().MinimumLevel.Verbose();
                    break;

                case "debug":
                    config = GetBasicLoggerConfiguration().MinimumLevel.Debug();
                    break;

                case "warning":
                    config = GetBasicLoggerConfiguration().MinimumLevel.Warning();
                    break;

                case "error":
                    config = GetBasicLoggerConfiguration().MinimumLevel.Error();
                    break;

                case "fatal":
                    config = GetBasicLoggerConfiguration().MinimumLevel.Fatal();
                    break;

                default:
                    config = GetBasicLoggerConfiguration().MinimumLevel.Information();
                    break;
            }

            var exeName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
            var pathFormat = Path.Combine(
                LogSettings.Default.OutputFolder.Replace("{exeName}", exeName),
                "log-{Date}.log");

            Log.Logger = config
                .Enrich.WithProperty("MainAssembly", exeName)
                .WriteTo.RollingFile(pathFormat,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}{Properties:j}{NewLine}")
                .WriteTo.RabbitMQ(RabbitMqClientConfiguration(), RabbitMQSinkConfiguration(), new JsonFormatter())
                .WriteTo.MailError()
                .CreateLogger();
        }


        private static LoggerConfiguration GetBasicLoggerConfiguration()
        {
            return new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithMachineName()
                .WriteTo.LiterateConsole()
                .ReadFrom.AppSettings();
        }

        private static RabbitMQClientConfiguration RabbitMqClientConfiguration()
        {
            var uri = new Uri(BusConfigurator.Uri);

            var rabbitConfiguration = new RabbitMQClientConfiguration
            {
                Username = BusConfigurator.UserName,
                Password = BusConfigurator.Password,
                VHost = uri.AbsolutePath.Replace("/", ""), // Remove the / at the begging and the end
                Exchange = "CMI.Logging",
                ExchangeType = ExchangeType.Fanout,
                DeliveryMode = RabbitMQDeliveryMode.Durable,
                Port = uri.IsDefaultPort ? 5672 : uri.Port,
                Heartbeat = 60
            };
            rabbitConfiguration.Hostnames.Add(uri.Host);
            return rabbitConfiguration;
        }

        private static RabbitMQSinkConfiguration RabbitMQSinkConfiguration()
        {
            var rabbitConfiguration = new RabbitMQSinkConfiguration();
            return rabbitConfiguration;
        }

        /// <summary>
        ///     Erstellt eine Queue und ein Exchange für die Logmessages
        /// </summary>
        private static void PrepareRabbitMq()
        {
            try
            {
                var uri = new Uri(BusConfigurator.Uri);

                var factory = new ConnectionFactory
                {
                    HostName = uri.Host,
                    VirtualHost = uri.AbsolutePath.Replace("/", ""),
                    UserName = BusConfigurator.UserName,
                    Password = BusConfigurator.Password,
                    Port = uri.IsDefaultPort ? 5672 : uri.Port
                };

                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("CMI.Logging", true, false, false);
                    channel.ExchangeDeclare("CMI.Logging", ExchangeType.Fanout, true);
                    channel.QueueBind("CMI.Logging", "CMI.Logging", string.Empty);
                }
            }
            catch (Exception ex)
            {
                // Da der Logger noch nicht bereit ist auf die Console schreiben
                Console.WriteLine("Exception bei der Erstellung von RabbitMQ Queue und Exchange 'CMI.Logging' für die Logmessages. Exception: {0}",
                    ex);
            }
        }
    }
}