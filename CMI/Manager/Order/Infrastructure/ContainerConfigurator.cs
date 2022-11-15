using System;
using System.Reflection;
using Autofac;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.Template;
using MassTransit;

namespace CMI.Manager.Order.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SearchIndexDataAccess>()
                .As<ISearchIndexDataAccess>()
                .WithParameter("address", new Uri(ElasticConnectionSetting.Default.ConnectionString));

            builder.RegisterType<OrderManager>().As<IPublicOrder>();
            builder.RegisterType<OrderDataAccess>().As<IOrderDataAccess>().WithParameter("connectionString", DbConnectionSetting.Default.ConnectionString);
            builder.RegisterType<UserDataAccess>().As<IUserDataAccess>().WithParameter("connectionString", DbConnectionSetting.Default.ConnectionString);
            builder.RegisterType<CacheHelper>().As<ICacheHelper>().WithParameter("sftpLicenseKey", Settings.Default.SftpLicenseKey);

            builder.RegisterType<StatusWechsler>().AsSelf();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();
            builder.RegisterType<MailHelper>().As<IMailHelper>();
            builder.RegisterType<DataBuilder>().As<IDataBuilder>();

            // SimpleConsumers
            builder.RegisterType(typeof(SimpleConsumer<AddToBasketRequest, AddToBasketResponse, IPublicOrder>)).As(typeof(IConsumer<AddToBasketRequest>));
            builder.RegisterType(typeof(SimpleConsumer<AddToBasketCustomRequest, AddToBasketCustomResponse, IPublicOrder>)).As(typeof(IConsumer<AddToBasketCustomRequest>));
            builder.RegisterType(typeof(SimpleConsumer<RemoveFromBasketRequest, RemoveFromBasketResponse, IPublicOrder>)).As(typeof(IConsumer<RemoveFromBasketRequest>));
            builder.RegisterType(typeof(SimpleConsumer<UpdateCommentRequest, UpdateCommentResponse, IPublicOrder>)).As(typeof(IConsumer<UpdateCommentRequest>));
            builder.RegisterType(typeof(SimpleConsumer<UpdateBenutzungskopieRequest, UpdateBenutzungskopieResponse, IPublicOrder>)).As(typeof(IConsumer<UpdateBenutzungskopieRequest>));
            builder.RegisterType(typeof(SimpleConsumer<UpdateBewilligungsDatumRequest, UpdateBewilligungsDatumResponse, IPublicOrder>)).As(typeof(IConsumer<UpdateBewilligungsDatumRequest>));
            builder.RegisterType(typeof(SimpleConsumer<UpdateBenutzungskopieRequest, UpdateBenutzungskopieResponse, IPublicOrder>)).As(typeof(IConsumer<UpdateBenutzungskopieRequest>));
            builder.RegisterType(typeof(SimpleConsumer<UpdateReasonRequest, UpdateReasonResponse, IPublicOrder>)).As(typeof(IConsumer<UpdateReasonRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetBasketRequest, GetBasketResponse, IPublicOrder>)).As(typeof(IConsumer<GetBasketRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetOrderingsRequest, GetOrderingsResponse, IPublicOrder>)).As(typeof(IConsumer<GetOrderingsRequest>));
            builder.RegisterType(typeof(SimpleConsumer<IsUniqueVeInBasketRequest, IsUniqueVeInBasketResponse, IPublicOrder>)).As(typeof(IConsumer<IsUniqueVeInBasketRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetDigipoolRequest, GetDigipoolResponse, IPublicOrder>)).As(typeof(IConsumer<GetDigipoolRequest>));
            builder.RegisterType(typeof(SimpleConsumer<UpdateDigipoolRequest, UpdateDigipoolResponse, IPublicOrder>)).As(typeof(IConsumer<UpdateDigipoolRequest>));
            builder.RegisterType(typeof(SimpleConsumer<MarkOrderAsFaultedRequest, MarkOrderAsFaultedResponse, IPublicOrder>)).As(typeof(IConsumer<MarkOrderAsFaultedRequest>));
            builder.RegisterType(typeof(SimpleConsumer<ResetAufbereitungsfehlerRequest, ResetAufbereitungsfehlerResponse, IPublicOrder>)).As(typeof(IConsumer<ResetAufbereitungsfehlerRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetPrimaerdatenReportRecordsRequest, GetPrimaerdatenReportRecordsResponse, IPublicOrder>)).As(typeof(IConsumer<GetPrimaerdatenReportRecordsRequest>));
            builder.RegisterType(typeof(SimpleConsumer<UpdateOrderItemRequest, UpdateOrderItemResponse, IPublicOrder>)).As(typeof(IConsumer<UpdateOrderItemRequest>));
            // just register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }
    }
}