using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Results;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Utilities.Template;
using FluentAssertions;
using MassTransit;
using MassTransit.Events;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Vecteur.Tests
{
    [TestFixture]
    public class VecteurControllerTests
    {
        [Test]
        public async Task GetNextDigitalisierungsauftrag_VE_in_protection_with_approve_status_FreigegebenDurchSystem_returns_true()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenDurchSystem};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(9999, 12, 31), Year = 9999}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeTrue();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_VE_in_protection_with_approve_status_FreigegebenAusserhalbSchutzfrist_returns_false()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(9999, 12, 31), Year = 9999}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeFalse();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_VE_in_protection_with_approve_status_FreigegebenInSchutzfrist_returns_true()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(9999, 12, 31), Year = 9999}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeTrue();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_VE_not_in_protection_with_approve_status_FreigegebenDurchSystem_returns_false()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenDurchSystem};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeFalse();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_VE_not_in_protection_with_approve_status_FreigegebenAusserhalbSchutzfrist_returns_false()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeFalse();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_VE_not_in_protection_with_approve_status_FreigegebenInSchutzfrist_returns_true()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeTrue();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_no_VE_with_approve_status_FreigegebenDurchSystem_returns_true()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = null, ApproveStatus = ApproveStatus.FreigegebenDurchSystem};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeTrue();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_no_VE_with_approve_status_FreigegebenAusserhalbSchutzfrist_returns_false()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = null, ApproveStatus = ApproveStatus.FreigegebenAusserhalbSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeFalse();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_no_VE_with_approve_status_FreigegebenInSchutzfrist_returns_true()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = null, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = (OkNegotiatedContentResult<DigitalisierungsAuftrag>) await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Content.Dossier.InSchutzfrist.Should().BeTrue();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_call_with_no_or_wrong_API_key_results_in_Unauthorized()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = null, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);
            // Now change api key
            ApiKeyChecker.Key = "Change it";

            // Act
            var auftrag = await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Should().BeOfType<UnauthorizedResult>();
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_if_digipool_is_empty_NoContent_is_returned()
        {
            // Arrange
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(Array.Empty<DigipoolEntry>(), archiveRecord);

            // Act
            var auftrag = await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Should().BeOfType<StatusCodeResult>();
            ((StatusCodeResult) auftrag).StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_if_ve_not_found_should_return_RequestEntityTooLarge()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, null);

            // Act
            var auftrag = await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_if_ve_has_no_protectionEndDate_should_return_RequestEntityTooLarge()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord {ArchiveRecordId = "1", ProtectionEndDate = null};

            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }


        [Test]
        public async Task GetNextDigitalisierungsauftrag_Exception_in_method_with_digipoolEntry_is_null_should_return_InternalServerError()
        {
            // Arrange
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(null, null);

            // Act
            var auftrag = await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Should().BeOfType<ExceptionResult>();
        }


        [Test]
        public async Task GetNextDigitalisierungsauftrag_Exception_in_method_with_digipoolEntry_not_null_should_return_413_return_code()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist, OrderItemId = 999};
            var orderManagerMock = new Mock<IPublicOrder>();
            orderManagerMock.Setup(x => x.GetDigipool(It.IsAny<int>())).ReturnsAsync(new[] {digiPoolEntry});
            var mailHelperMock = new Mock<IMailHelper>();

            var controller =
                ArrangeControllerForGetNextDigitalisierungsauftragWithOrderMock(orderManagerMock.Object, mailHelperMock.Object,
                    null /*provokes error*/);

            // Act
            var auftrag = await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
            // Verify that the order was marked a s faulted
            orderManagerMock.Verify(x => x.MarkOrderAsFaulted(999), Times.Once);
            mailHelperMock.Verify(
                x => x.SendEmail(It.IsAny<IBus>(), It.IsAny<DigipoolAufbereitungFehlgeschlagen>(), It.IsAny<object>(), It.IsAny<bool>()), Times.Once);
        }

        [Test]
        public void GetNextDigitalisierungsauftrag_Digipool_returns_only_faulted_items_should_return_403_return_code()
        {
            // Arrange
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist, HasAufbereitungsfehler = true};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = controller.GetNextDigitalisierungsauftrag().GetAwaiter().GetResult();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Test]
        public void GetNextDigitalisierungsauftrag_returns_service_not_available_if_order_manager_is_not_running()
        {
            // Arrange
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForGetNextDigitalisierungsauftragWithDigipoolException(new RequestTimeoutException(), archiveRecord);

            // Act
            var auftrag = controller.GetNextDigitalisierungsauftrag().GetAwaiter().GetResult();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void GetNextDigitalisierungsauftrag_returns_service_not_available_if_index_manager_is_not_running()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var controller =
                ArrangeControllerForGetNextDigitalisierungsauftragWithIndexManagerException(new[] {digiPoolEntry}, new RequestTimeoutException());

            // Act
            var auftrag = controller.GetNextDigitalisierungsauftrag().GetAwaiter().GetResult();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void GetNextDigitalisierungsauftrag_SQL_Server_not_available_returns_service_not_available()
        {
            // Arrange
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            // Setup specific exception that signals that database is down
            var fe = new FaultEvent<OrderDatabaseNotFoundOrNotRunningException>(new OrderDatabaseNotFoundOrNotRunningException(), new Guid(), null,
                new OrderDatabaseNotFoundOrNotRunningException(), null);
            var ex = new RequestFaultException("", fe);

            var controller = ArrangeControllerForGetNextDigitalisierungsauftragWithDigipoolException(ex, archiveRecord);

            // Act
            var auftrag = controller.GetNextDigitalisierungsauftrag().GetAwaiter().GetResult();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public void GetNextDigitalisierungsauftrag_No_protection_end_date_returns_error()
        {
            // Arrange
            var archiveRecord = new ElasticArchiveRecord {ProtectionEndDate = null};
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist, HasAufbereitungsfehler = false};

            var controller = ArrangeControllerForGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = controller.GetNextDigitalisierungsauftrag().GetAwaiter().GetResult();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }

        [Test]
        public async Task GetNextDigitalisierungsauftrag_with_missing_must_fields_returns_internal_exception()
        {
            // Arrange
            var digiPoolEntry = new DigipoolEntry {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenInSchutzfrist};
            var archiveRecord = new ElasticArchiveRecord
                {ProtectionEndDate = new ElasticDateWithYear {Date = new DateTime(1900, 12, 31), Year = 1900}};
            var controller = ArrangeControllerForInvalidGetNextDigitalisierungsauftrag(new[] {digiPoolEntry}, archiveRecord);

            // Act
            var auftrag = await controller.GetNextDigitalisierungsauftrag();

            // Assert
            auftrag.Should().BeOfType<NegotiatedContentResult<string>>();
            ((NegotiatedContentResult<string>) auftrag).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }

        [Test]
        public async Task GetStatus_Exception_in_method_should_return_InternalServerError()
        {
            // Arrange
            var controller = ArrangeControllerForGetStatus(null /*provokes error*/);

            // Act
            var result = await controller.GetStatus(1);

            // Assert
            result.Should().BeOfType<ExceptionResult>();
        }

        [Test]
        public async Task GetStatus_if_orderId_returns_no_result_NotFound_is_returned()
        {
            // Arrange
            var controller = ArrangeControllerForGetStatus(Array.Empty<OrderItem>());

            // Act
            var result = await controller.GetStatus(1);

            // Assert
            result.Should().BeOfType<StatusCodeResult>();
            ((StatusCodeResult) result).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Test]
        public async Task GetStatus_call_with_no_or_wrong_API_key_results_in_Unauthorized()
        {
            // Arrange
            var controller = ArrangeControllerForGetStatus(Array.Empty<OrderItem>());
            // Now change api key
            ApiKeyChecker.Key = "Change it";

            // Act
            var result = await controller.GetStatus(1);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Test]
        public async Task GetStatus_with_with_valid_id_returns_content()
        {
            // Arrange
            var orderItem = new OrderItem {VeId = 1, ApproveStatus = ApproveStatus.FreigegebenDurchSystem, Status = OrderStatesInternal.Ausgeliehen};
            var controller = ArrangeControllerForGetStatus(new[] {orderItem});

            // Act
            var result = await controller.GetStatus(1);

            // Assert
            result.Should().BeOfType<OkNegotiatedContentResult<string>>();
            ((OkNegotiatedContentResult<string>) result).Content.Should().Be(OrderStatesInternal.Ausgeliehen.ToString());
        }


        [Test]
        public async Task SetStatusAushebungBereit_call_with_no_or_wrong_API_key_results_in_Unauthorized()
        {
            // Arrange
            var controller = ArrangeControllerGeneric();
            // Now change api key
            ApiKeyChecker.Key = "Change it";

            // Act
            var result = await controller.SetStatusAushebungBereit(1);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Test]
        public async Task SetStatusDigitalisierungAbgebrochen_call_with_no_or_wrong_API_key_results_in_Unauthorized()
        {
            // Arrange
            var controller = ArrangeControllerGeneric();
            // Now change api key
            ApiKeyChecker.Key = "Change it";

            // Act
            var result = await controller.SetStatusDigitalisierungAbgebrochen(1, string.Empty);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }


        [Test]
        public async Task SetStatusDigitalisierungExtern_call_with_no_or_wrong_API_key_results_in_Unauthorized()
        {
            // Arrange
            var controller = ArrangeControllerGeneric();
            // Now change api key
            ApiKeyChecker.Key = "Change it";

            // Act
            var result = await controller.SetStatusDigitalisierungExtern(1);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Test]
        public async Task SetStatusZumReponierenBereit_call_with_no_or_wrong_API_key_results_in_Unauthorized()
        {
            // Arrange
            var controller = ArrangeControllerGeneric();
            // Now change api key
            ApiKeyChecker.Key = "Change it";

            // Act
            var result = await controller.SetStatusZumReponierenBereit(1);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }


        private VecteurController ArrangeControllerGeneric()
        {
            var actionClientMock = Mock.Of<IVecteurActions>();
            var orderManagerMock = Mock.Of<IPublicOrder>();
            var digitizationHelperMock = Mock.Of<IDigitizationHelper>();
            var busHelper = Mock.Of<IMessageBusCallHelper>();

            return SetupVecteurController(actionClientMock, orderManagerMock, digitizationHelperMock, busHelper, Mock.Of<IMailHelper>());
        }

        /// <summary>Arranges the controller for calls to GetStatus</summary>
        /// <param name="findOrderItemsResult">The result for a call to FindOrderItems</param>
        private VecteurController ArrangeControllerForGetStatus(OrderItem[] findOrderItemsResult)
        {
            var actionClientMock = Mock.Of<IVecteurActions>();
            var orderManagerMock = Mock.Of<IPublicOrder>();
            var digitizationHelperMock = Mock.Of<IDigitizationHelper>();
            var busHelper = Mock.Of<IMessageBusCallHelper>(x => x.FindOrderItems(It.IsAny<int[]>()) == Task.FromResult(findOrderItemsResult));

            return SetupVecteurController(actionClientMock, orderManagerMock, digitizationHelperMock, busHelper, Mock.Of<IMailHelper>());
        }


        /// <summary>Arranges the Vecteur controller</summary>
        /// <param name="digipoolResult">The result that should be returned from the call to GetDigipool</param>
        /// <param name="archiveRecordResult">The result that should be returned from the Elastic DB. It is the archive record</param>
        private VecteurController ArrangeControllerForGetNextDigitalisierungsauftrag(DigipoolEntry[] digipoolResult,
            ElasticArchiveRecord archiveRecordResult)
        {
            var actionClientMock = Mock.Of<IVecteurActions>();
            var orderManagerMock = Mock.Of<IPublicOrder>(x => x.GetDigipool(It.IsAny<int>()) == Task.FromResult(digipoolResult));
            var digitizationHelperMock = Mock.Of<IDigitizationHelper>(x =>
                x.GetDigitalisierungsAuftrag(It.IsAny<string>()) == GetEmptyDigitalisierungsauftrag() &&
                x.GetManualDigitalisierungsAuftrag(It.IsAny<DigipoolEntry>()) == GetEmptyDigitalisierungsauftrag());
            var busHelper =
                Mock.Of<IMessageBusCallHelper>(x => x.GetElasticArchiveRecord(It.IsAny<string>()) == Task.FromResult(archiveRecordResult));

            return SetupVecteurController(actionClientMock, orderManagerMock, digitizationHelperMock, busHelper, Mock.Of<IMailHelper>());
        }

        /// <summary>Arranges the Vecteur controller for an invalid result</summary>
        /// <param name="digipoolResult">The result that should be returned from the call to GetDigipool</param>
        /// <param name="archiveRecordResult">The result that should be returned from the Elastic DB. It is the archive record</param>
        private VecteurController ArrangeControllerForInvalidGetNextDigitalisierungsauftrag(DigipoolEntry[] digipoolResult,
            ElasticArchiveRecord archiveRecordResult)
        {
            var actionClientMock = Mock.Of<IVecteurActions>();
            var orderManagerMock = Mock.Of<IPublicOrder>(x => x.GetDigipool(It.IsAny<int>()) == Task.FromResult(digipoolResult));
            var digitizationHelperMock = Mock.Of<IDigitizationHelper>(x =>
                x.GetDigitalisierungsAuftrag(It.IsAny<string>()) == GetInvalidEmptyDigitalisierungsauftrag() &&
                x.GetManualDigitalisierungsAuftrag(It.IsAny<DigipoolEntry>()) == GetInvalidEmptyDigitalisierungsauftrag());
            var busHelper =
                Mock.Of<IMessageBusCallHelper>(x => x.GetElasticArchiveRecord(It.IsAny<string>()) == Task.FromResult(archiveRecordResult));

            return SetupVecteurController(actionClientMock, orderManagerMock, digitizationHelperMock, busHelper, Mock.Of<IMailHelper>());
        }

        /// <summary>Arranges the Vecteur controller</summary>
        /// <param name="orderClient">A mock of the IPublicOrder interface</param>
        /// <param name="archiveRecordResult">The result that should be returned from the Elastic DB. It is the archive record</param>
        private VecteurController ArrangeControllerForGetNextDigitalisierungsauftragWithOrderMock(IPublicOrder orderClient, IMailHelper mailHelper,
            ElasticArchiveRecord archiveRecordResult)
        {
            var actionClientMock = Mock.Of<IVecteurActions>();
            var digitizationHelperMock = Mock.Of<IDigitizationHelper>(x =>
                x.GetDigitalisierungsAuftrag(It.IsAny<string>()) == GetEmptyDigitalisierungsauftrag() &&
                x.GetManualDigitalisierungsAuftrag(It.IsAny<DigipoolEntry>()) == GetEmptyDigitalisierungsauftrag());
            var busHelper =
                Mock.Of<IMessageBusCallHelper>(x => x.GetElasticArchiveRecord(It.IsAny<string>()) == Task.FromResult(archiveRecordResult));

            return SetupVecteurController(actionClientMock, orderClient, digitizationHelperMock, busHelper, mailHelper);
        }

        /// <summary>Arranges the Vecteur controller</summary>
        /// <param name="digipoolException">The exception that should be thrown when a call to GetDigipool is made.</param>
        /// <param name="archiveRecordResult">The result that should be returned from the Elastic DB. It is the archive record</param>
        private VecteurController ArrangeControllerForGetNextDigitalisierungsauftragWithDigipoolException(Exception digipoolException,
            ElasticArchiveRecord archiveRecordResult)
        {
            var actionClientMock = Mock.Of<IVecteurActions>();
            var orderManagerMock = new Mock<IPublicOrder>();
            orderManagerMock.Setup(x => x.GetDigipool(It.IsAny<int>())).Throws(digipoolException);

            var digitizationHelperMock = Mock.Of<IDigitizationHelper>(x =>
                x.GetDigitalisierungsAuftrag(It.IsAny<string>()) == GetEmptyDigitalisierungsauftrag() &&
                x.GetManualDigitalisierungsAuftrag(It.IsAny<DigipoolEntry>()) == GetEmptyDigitalisierungsauftrag());
            var busHelper =
                Mock.Of<IMessageBusCallHelper>(x => x.GetElasticArchiveRecord(It.IsAny<string>()) == Task.FromResult(archiveRecordResult));

            return SetupVecteurController(actionClientMock, orderManagerMock.Object, digitizationHelperMock, busHelper, Mock.Of<IMailHelper>());
        }

        /// <summary>Arranges the Vecteur controller</summary>
        /// <param name="digipoolResult">The exception that should be thrown when a call to GetDigipool is made.</param>
        /// <param name="archiveException">The exception to throw when calling GetElasticArchiveRecord</param>
        private VecteurController ArrangeControllerForGetNextDigitalisierungsauftragWithIndexManagerException(DigipoolEntry[] digipoolResult,
            Exception archiveException)
        {
            var actionClientMock = Mock.Of<IVecteurActions>();
            var orderManagerMock = Mock.Of<IPublicOrder>(x => x.GetDigipool(It.IsAny<int>()) == Task.FromResult(digipoolResult));

            var digitizationHelperMock = Mock.Of<IDigitizationHelper>(x =>
                x.GetDigitalisierungsAuftrag(It.IsAny<string>()) == GetEmptyDigitalisierungsauftrag() &&
                x.GetManualDigitalisierungsAuftrag(It.IsAny<DigipoolEntry>()) == GetEmptyDigitalisierungsauftrag());
            var busHelper = new Mock<IMessageBusCallHelper>();
            busHelper.Setup(x => x.GetElasticArchiveRecord(It.IsAny<string>())).Throws(archiveException);

            return SetupVecteurController(actionClientMock, orderManagerMock, digitizationHelperMock, busHelper.Object, Mock.Of<IMailHelper>());
        }

        private static VecteurController SetupVecteurController(IVecteurActions actionClientMock, IPublicOrder orderManagerMock,
            IDigitizationHelper digitizationHelperMock, IMessageBusCallHelper busHelper, IMailHelper mailHelperMock)
        {
            var bus = Mock.Of<IBus>();
            var parameterHelper = Mock.Of<IParameterHelper>();
            var user = new User();
            var dataBuilder = Mock.Of<IDataBuilder>(x => x.SetDataProtectionLevel(It.IsAny<DataBuilderProtectionStatus>()) == x &&
                                                         x.AddAuftraege(It.IsAny<IEnumerable<int>>()) == x &&
                                                         x.AddValue(It.IsAny<string>(), It.IsAny<object>()) == x &&
                                                         x.GetAuftraege(It.IsAny<IEnumerable<int>>()) == new List<Auftrag>
                                                         {
                                                             new Auftrag(new OrderItem(),
                                                                 new Ordering(), new BestellformularVe(new OrderItem()),
                                                                 new BestellformularVe(new OrderItem()), Person.FromUser(user))
                                                         });

            var controller = new VecteurController(actionClientMock, orderManagerMock, digitizationHelperMock, busHelper, mailHelperMock, bus,
                parameterHelper, dataBuilder);
            var controllerContext = new HttpControllerContext();
            var request = new HttpRequestMessage();
            request.Headers.Add("X-ApiKey", "myKey");
            ApiKeyChecker.Key = "myKey";

            // Don't forget these lines, if you do then the request will be null.
            controllerContext.Request = request;
            controller.ControllerContext = controllerContext;

            return controller;
        }

        private Task<DigitalisierungsAuftrag> GetEmptyDigitalisierungsauftrag()
        {
            const string noDataAvailable = "keine Angabe";
            var t = Task<DigitalisierungsAuftrag>.Factory.StartNew(() => new DigitalisierungsAuftrag
            {
                Ablieferung = new AblieferungType
                    {AblieferndeStelle = noDataAvailable, Ablieferungsnummer = noDataAvailable, AktenbildnerName = noDataAvailable},
                OrdnungsSystem = new OrdnungsSystemType {Name = noDataAvailable, Signatur = noDataAvailable, Stufe = noDataAvailable},
                Dossier = new VerzEinheitType
                    {Titel = noDataAvailable, Signatur = noDataAvailable, Entstehungszeitraum = noDataAvailable, Stufe = noDataAvailable},
                Auftragsdaten = new AuftragsdatenType {Benutzungskopie = true, BestelleinheitId = "-1"}
            });
            return t;
        }

        private Task<DigitalisierungsAuftrag> GetInvalidEmptyDigitalisierungsauftrag()
        {
            const string noDataAvailable = "keine Angabe";
            var t = Task<DigitalisierungsAuftrag>.Factory.StartNew(() => new DigitalisierungsAuftrag
            {
                Ablieferung = new AblieferungType
                    {AblieferndeStelle = noDataAvailable, Ablieferungsnummer = noDataAvailable, AktenbildnerName = noDataAvailable},
                OrdnungsSystem = new OrdnungsSystemType {Name = noDataAvailable, Signatur = noDataAvailable, Stufe = noDataAvailable},
                Dossier = new VerzEinheitType
                    {Titel = noDataAvailable, Signatur = noDataAvailable, Entstehungszeitraum = noDataAvailable, Stufe = noDataAvailable},
                Auftragsdaten = new AuftragsdatenType {Benutzungskopie = true} // Missing BestelleinheitId
            });
            return t;
        }
    }
}