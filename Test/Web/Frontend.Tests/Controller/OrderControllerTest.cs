using System;
using System.Threading.Tasks;
using System.Web.Http.Results;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Web.Frontend.api.Controllers;
using CMI.Web.Frontend.api.Dto;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Moq;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.Controller
{
    [TestFixture]
    public class OrderControllerTest
    {
        private InMemoryTestHarness harness;

        [SetUp]
        public void Setup()
        {
            harness = new InMemoryTestHarness();
            harness.TestTimeout = TimeSpan.FromMinutes(5);
            harness.Start();
        }

        [Test]
        public async Task Adding_manual_order_with_no_period_results_in_keine_Angabe()
        {
            // Arrange
            var orderManager = new Mock<IPublicOrder>();
            orderManager.Setup(o => o.AddToBasketCustom(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(AddToBasketCustom);
            
            var controller = new OrderController(orderManager.Object, null, null, null, null, null, null, null);

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
            var orderManager = new Mock<IPublicOrder>();
            orderManager.Setup(o => o.AddToBasketCustom(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(AddToBasketCustom);

            var controller = new OrderController(orderManager.Object, null, null, null, null, null, null, null);

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
            var orderManager = new Mock<IPublicOrder>();
            orderManager.Setup(o => o.AddToBasketCustom(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(AddToBasketCustom);

            var controller = new OrderController(orderManager.Object, null, null, null, null, null, null, null);

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
            var orderManager = new Mock<IPublicOrder>();
            orderManager.Setup(o => o.AddToBasketCustom(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(AddToBasketCustom);

            var controller = new OrderController(orderManager.Object, null, null, null, null, null, null, null);

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


        public Task<OrderItem> AddToBasketCustom(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer, string aktenzeichen,
            string dossiertitel, string zeitraumDossier, string userId)
        {
            return Task.FromResult(new OrderItem
            {
                Bestand = bestand,
                Ablieferung = ablieferung,
                BehaeltnisNummer = behaeltnisNummer,
                ArchivNummer = archivNummer,
                Aktenzeichen = aktenzeichen,
                Dossiertitel = dossiertitel,
                ZeitraumDossier = zeitraumDossier
            });
        }
    }
}