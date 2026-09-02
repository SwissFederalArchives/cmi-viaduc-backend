using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Sinks.PeriodicBatching;

namespace CMI.Utilities.Logging.Configurator
{
    public enum RabbitMQDeliveryMode : byte
    {
        Transient = 1,
        Durable = 2
    }

    public sealed class RabbitMQClientConfiguration
    {
        public RabbitMQClientConfiguration()
        {
            Hostnames = new List<string>();
        }

        public string Username { get; set; }
        public string Password { get; set; }
        public string VHost { get; set; }
        public string Exchange { get; set; }
        public string ExchangeType { get; set; }
        public RabbitMQDeliveryMode DeliveryMode { get; set; } = RabbitMQDeliveryMode.Durable;
        public int Port { get; set; } = 5672;
        public ushort Heartbeat { get; set; } = 60;
        public IList<string> Hostnames { get; }
    }

    public sealed class RabbitMQSinkConfiguration
    {
        public ITextFormatter TextFormatter { get; set; } = new JsonFormatter();
        public int BatchPostingLimit { get; set; } = 50;
        public TimeSpan BufferingTimeLimit { get; set; } = TimeSpan.FromSeconds(2);
        public int? QueueLimit { get; set; } = 100000;
    }

    internal sealed class RabbitMqBatchedSink : Serilog.Sinks.PeriodicBatching.IBatchedLogEventSink, IDisposable
    {
        private readonly RabbitMQClientConfiguration clientConfiguration;
        private readonly RabbitMQSinkConfiguration sinkConfiguration;
        private readonly SemaphoreSlim syncRoot = new SemaphoreSlim(1, 1);
        private IConnection connection;
        private IChannel channel;

        public RabbitMqBatchedSink(RabbitMQClientConfiguration clientConfiguration, RabbitMQSinkConfiguration sinkConfiguration)
        {
            this.clientConfiguration = clientConfiguration ?? throw new ArgumentNullException(nameof(clientConfiguration));
            this.sinkConfiguration = sinkConfiguration ?? throw new ArgumentNullException(nameof(sinkConfiguration));
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            try
            {
                var openChannel = await EnsureChannelAsync().ConfigureAwait(false);

                foreach (var logEvent in batch)
                {
                    byte[] body;
                    using (var writer = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        sinkConfiguration.TextFormatter.Format(logEvent, writer);
                        body = Encoding.UTF8.GetBytes(writer.ToString());
                    }

                    var properties = new BasicProperties
                    {
                        ContentType = "application/json",
                        Persistent = clientConfiguration.DeliveryMode == RabbitMQDeliveryMode.Durable
                    };

                    await openChannel.BasicPublishAsync(
                            clientConfiguration.Exchange,
                            string.Empty,
                            false,
                            properties,
                            body,
                            CancellationToken.None)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ResetConnection();
                Console.WriteLine("Exception while sending log events to RabbitMQ. Exception: {0}", ex);
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            ResetConnection();
            syncRoot.Dispose();
        }

        private async Task<IChannel> EnsureChannelAsync()
        {
            if (channel != null && channel.IsOpen)
            {
                return channel;
            }

            await syncRoot.WaitAsync().ConfigureAwait(false);
            try
            {
                if (channel != null && channel.IsOpen)
                {
                    return channel;
                }

                ResetConnection();

                Exception lastException = null;
                foreach (var hostname in clientConfiguration.Hostnames.Where(h => !string.IsNullOrWhiteSpace(h)))
                {
                    try
                    {
                        var factory = new ConnectionFactory
                        {
                            HostName = hostname,
                            VirtualHost = clientConfiguration.VHost,
                            UserName = clientConfiguration.Username,
                            Password = clientConfiguration.Password,
                            Port = clientConfiguration.Port,
                            RequestedHeartbeat = TimeSpan.FromSeconds(clientConfiguration.Heartbeat)
                        };

                        connection = await factory.CreateConnectionAsync().ConfigureAwait(false);
                        channel = await connection.CreateChannelAsync().ConfigureAwait(false);
                        return channel;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                    }
                }

                throw new InvalidOperationException("Could not connect to any configured RabbitMQ host.", lastException);
            }
            finally
            {
                syncRoot.Release();
            }
        }

        private void ResetConnection()
        {
            try
            {
                channel?.Dispose();
            }
            catch
            {
            }
            finally
            {
                channel = null;
            }

            try
            {
                connection?.Dispose();
            }
            catch
            {
            }
            finally
            {
                connection = null;
            }
        }
    }

    public static class RabbitMqSinkExtensions
    {
        public static LoggerConfiguration RabbitMQ(this LoggerSinkConfiguration loggerConfiguration,
            RabbitMQClientConfiguration clientConfiguration,
            RabbitMQSinkConfiguration sinkConfiguration)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }

            if (clientConfiguration == null)
            {
                throw new ArgumentNullException(nameof(clientConfiguration));
            }

            if (sinkConfiguration == null)
            {
                throw new ArgumentNullException(nameof(sinkConfiguration));
            }

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = sinkConfiguration.BatchPostingLimit,
                Period = sinkConfiguration.BufferingTimeLimit,
                QueueLimit = sinkConfiguration.QueueLimit
            };

            return loggerConfiguration.Sink(new PeriodicBatchingSink(new RabbitMqBatchedSink(clientConfiguration, sinkConfiguration), batchingOptions));
        }
    }
}
