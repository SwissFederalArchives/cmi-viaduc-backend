using System;
using System.Threading.Tasks;
using System.Web.Http.Results;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Utilities.ProxyClients.Order;
using CMI.Web.Frontend.api.Controllers;
using CMI.Web.Frontend.api.Dto;
using FluentAssertions;
using MassTransit;
using MassTransit.TestFramework;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.Controller
{
    [TestFixture]
    public class OrderControllerTest : InMemoryTestFixture
    {
        public OrderControllerTest() : base(true)
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromMinutes(5);
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint(
                string.Format(BusConstants.OrderManagagerRequestBase, nameof(AddToBasketCustomRequest)),
                ec => { ec.Consumer(() => new AddBasketCustomConsumer()); });
        }

        [Test]
        public async Task Adding_manual_order_with_no_period_results_in_keine_Angabe()
        {
            // Arrange
            var orderManager = new OrderManagerClient(Bus);
            var controller = new OrderController(orderManager, null, null, null, null, null);

            // Act
            var result = await controller.AddToBasket(new FormularBestellungParams
            {
                Title = "Test",
                Period = ""
            });

            var contentResult = result as NegotiatedContentResult<OrderItemDto>;

            // Assert
            contentResult.Should().NotBeNull();
            contentResult.Content.Period.Should().Be("keine Angabe");
        }

        [Test]
        public async Task Adding_manual_order_with_single_year_period_results_in_JJJJ_JJJJ()
        {
            // Arrange
            var orderManager = new OrderManagerClient(Bus);
            var controller = new OrderController(orderManager, null, null, null, null, null);

            // Act
            var result = await controller.AddToBasket(new FormularBestellungParams
            {
                Title = "Test",
                Period = "1950"
            });

            var contentResult = result as NegotiatedContentResult<OrderItemDto>;

            // Assert
            contentResult.Should().NotBeNull();
            contentResult.Content.Period.Should().Be("1950-1950");
        }

        [Test]
        public async Task Adding_manual_order_with_single_date_period_results_in_DD_MM_JJJJ_DD_MM_JJJJ()
        {
            // Arrange
            var orderManager = new OrderManagerClient(Bus);
            var controller = new OrderController(orderManager, null, null, null, null, null);

            // Act
            var result = await controller.AddToBasket(new FormularBestellungParams
            {
                Title = "Test",
                Period = "24.12.1950"
            });

            var contentResult = result as NegotiatedContentResult<OrderItemDto>;

            // Assert
            contentResult.Should().NotBeNull();
            contentResult.Content.Period.Should().Be("24.12.1950-24.12.1950");
        }

        [Test]
        public async Task Adding_manual_order_with_two_date_period_is_not_changed()
        {
            // Arrange
            var orderManager = new OrderManagerClient(Bus);
            var controller = new OrderController(orderManager, null, null, null, null, null);

            // Act
            var result = await controller.AddToBasket(new FormularBestellungParams
            {
                Title = "Test",
                Period = "24.12.1950-14.01.1960"
            });

            var contentResult = result as NegotiatedContentResult<OrderItemDto>;

            // Assert
            contentResult.Should().NotBeNull();
            contentResult.Content.Period.Should().Be("24.12.1950-14.01.1960");
        }

        internal class AddBasketCustomConsumer : IConsumer<AddToBasketCustomRequest>
        {
            public async Task Consume(ConsumeContext<AddToBasketCustomRequest> context)
            {
                var retVal = new AddToBasketCustomResponse
                {
                    OrderItem = new OrderItem
                    {
                        ZeitraumDossier = context.Message.ZeitraumDossier
                    }
                };
                await context.RespondAsync(retVal);
            }
        }
    }
}