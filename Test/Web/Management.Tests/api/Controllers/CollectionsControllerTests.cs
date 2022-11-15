using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Results;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Management.api.Controllers;
using CMI.Web.Management.Auth;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Web.Management.Tests.api.Controllers
{
    [TestFixture]
    public class CollectionsControllerTests
    {
        private Mock<ICollectionManager> collectionClientMock;

        [SetUp]
        public void SetupDefaultMocks()
        {
            collectionClientMock = new Mock<ICollectionManager>();
        }

        [Test]
        public async Task GetReturnsCollection()
        {
            // Arrange
            var controller = InitializeMocksWithUserRights();
            // Act
           await controller.Get(10);
           // Assert
           collectionClientMock.Verify(c => c.GetCollection(10), Times.Once);
           collectionClientMock.Verify(c => c.GetAllCollections(false), Times.Never);
        }

        [Test]
        public async Task GetReturnsAllCollection()
        {
            // Arrange
            var controller = InitializeMocksWithUserRights();
            // Act
            await controller.GetAll();
            // Assert
            collectionClientMock.Verify(c => c.GetCollection(10), Times.Never);
            collectionClientMock.Verify(c => c.GetAllCollections(false), Times.Once);
        }

        [Test]
        public async Task Delete_successfull_when_user_has_rights()
        {
            // Arrange
            var controller = InitializeMocksWithUserRights();
            // Act
            await controller.Delete(12);

            // Assert
            collectionClientMock.Verify(c => c.GetCollection(10), Times.Never);
            collectionClientMock.Verify(c => c.GetAllCollections(false), Times.Never);
            collectionClientMock.Verify(c => c.DeleteCollection(12), Times.Once);
        }
        
        [Test] 
        public async Task Update_fails_if_id_of_item_and_passed_id_are_not_the_same()
        {
            // Arrange
            var controller = InitializeMocksWithUserRights();
            var collection = new CollectionDto { CollectionId = 10 };
            // Act
            var result = await controller.Update(12, collection);

            // Assert
            result.Should().BeOfType(typeof(BadRequestResult));
            collectionClientMock.Verify(c => c.GetCollection(10), Times.Never);
            collectionClientMock.Verify(c => c.GetAllCollections(false), Times.Never);
            collectionClientMock.Verify(c => c.InsertOrUpdateCollection(collection, null), Times.Never);
        }

        [Test]
        public async Task Update_fails_when_user_has_no_rights()
        {
            try
            {
                // Arrange
                var controller = InitializeMocks();
                // Act
                await controller.Update(10, new CollectionDto { CollectionId = 10 });
                throw new Exception("Darf hier nicht ankommen");
            }
            catch (Exception e)
            {
                e.Should().BeOfType(typeof(ForbiddenException));
                e.Message.Should().BeEquivalentTo("Die von Ihnen gewünschte Operation kann nicht ausgeführt werden. Ihnen fehlt das Recht 'CMI.Contract.Common.ApplicationFeature[]' um die Operation durchzuführen.");
            }
        }

        [Test]
        public async Task Update_successfull_when_user_has_rights()
        {
            // Arrange
            var controller = InitializeMocksWithUserRights();
            var collection = new CollectionDto { CollectionId = 12 };
            // Act
            var result = await controller.Update(12, collection);

            // Assert
            collectionClientMock.Verify(c => c.GetCollection(10), Times.Never);
            collectionClientMock.Verify(c => c.GetAllCollections(false), Times.Never);
            collectionClientMock.Verify(c => c.InsertOrUpdateCollection(collection, null), Times.Once);
            result.Should().BeOfType(typeof(OkNegotiatedContentResult<CollectionDto>));
        }
        
        private CollectionsController InitializeMocks()
        {
            var userAccessMock = new Mock<IManagementUserAccess>();
            userAccessMock.SetupGet(p => p.ApplicationFeatures).Returns(new List<ApplicationFeature>());
            var helperMock = new Mock<IManagementControllerHelper>();
            helperMock.Setup(h => h.GetUserAccess(null)).Returns(userAccessMock.Object);
            var controller = new CollectionsController(collectionClientMock.Object);
            controller.ManagementControllerHelper = helperMock.Object;
            return controller;
        }

        private CollectionsController InitializeMocksWithUserRights()
        {
            var testList = new List<ApplicationFeature>();
            testList.Add(ApplicationFeature.AdministrationSammlungenEinsehen);
            testList.Add(ApplicationFeature.AdministrationSammlungenBearbeiten);
            var userAccessMock = new Mock<IManagementUserAccess>();
            userAccessMock.SetupGet(p => p.ApplicationFeatures).Returns(testList);
            var helperMock = new Mock<IManagementControllerHelper>();
            helperMock.Setup(h => h.GetUserAccess(null)).Returns(userAccessMock.Object);
            var controller = new CollectionsController(collectionClientMock.Object);
            controller.ManagementControllerHelper = helperMock.Object;
            return controller;
        }
    }
}
