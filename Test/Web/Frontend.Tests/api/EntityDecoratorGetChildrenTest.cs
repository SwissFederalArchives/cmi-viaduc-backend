using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.ActaPro;
using CMI.Web.Common.api;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using Shouldly;
using Moq;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class EntityDecoratorGetChildrenTest
    {
        #region Fields

        private Mock<IElasticSettings> elasticSettings;
        private Mock<IElasticService> elasticService;
        private Mock<IEntityProvider> entityProvider;
        private EntityDecorator<TreeRecord> entityDecorator;

        #endregion

        #region Tests
        [Test]
        public async Task When_tree_record_is_leaf_return_null()
        {
            // Given
            this.InitializeMocksAndTestClass();
            // when
            var treeRecord = new TreeRecord { IsLeaf = true };
            var result = await entityDecorator.GetChildren(treeRecord, 22, null, null);

            // Verify
            result.ShouldNotBe(null);
            result.Items.Count.ShouldBe(0);
            result.Paging.ShouldNotBe(null);
            result.Paging.Total.ShouldBe(null);
            result.Paging.SortOrder.ShouldBe("Ascending");
            result.Paging.OrderBy.ShouldBe("treeSequence");
        }

        [Test]
        public async Task When_paging_is_null_add_correct_tree_sort_order()
        {
            // Given
            this.InitializeMocksAndTestClass();
            this.SetupServiceMock();

            // when
            var treeRecord = new TreeRecord { ArchiveRecordId = "28", IsLeaf = false, TreeSequence = 12 };
            var userAccess = new UserAccess("id1", string.Empty, string.Empty, new[] { "1", "2" }, true);
            var result = await entityDecorator.GetChildren(treeRecord, 11, userAccess, null);

            // Verify
            result.ShouldNotBe(null);
            result.Items.Count.ShouldBe(0);
            result.Paging.ShouldNotBe(null);
            result.Paging.Total.ShouldBe(0);
            result.Paging.SortOrder.ShouldBe("Ascending");
            result.Paging.OrderBy.ShouldBe("treeSequence");
        }

        [Test]
        public async Task When_paging_is_not_null_not_change_tree_sort_order()
        {
            // Given
            this.InitializeMocksAndTestClass();
            this.SetupServiceMock();

            // when
            var treeRecord = new TreeRecord { ArchiveRecordId = "28", IsLeaf = false, TreeSequence = 3 };
            var userAccess = new UserAccess("id1", string.Empty, string.Empty, new[] { "1", "2" }, true);
            var paging = new Paging { Skip = 22, OrderBy = "me", Total = 0 };
            var result = await entityDecorator.GetChildren(treeRecord, 11, userAccess, paging);

            // Verify
            result.ShouldNotBe(null);
            result.Paging.ShouldBe(paging);
        }

        [Test]
        public async Task When_paging_is_null_change_tree_sort_order()
        {
            // Given
            this.InitializeMocksAndTestClass();
            this.SetupMocksWithResults();

            // when
            var treeRecord = new TreeRecord { ArchiveRecordId = "28", IsLeaf = false, TreeSequence = 3 };
            var userAccess = new UserAccess("id1", string.Empty, string.Empty, new[] { "1", "2" }, true);
            var result = await entityDecorator.GetChildren(treeRecord, 11, userAccess, null);

            // Verify
            result.ShouldNotBe(null);
            result.Items.Count.ShouldBe(1);
            result.Paging.ShouldNotBe(null);
            result.Paging.Total.ShouldBe(1);
            result.Paging.SortOrder.ShouldBe("Ascending");
            result.Paging.OrderBy.ShouldBe("treeSequence");
        }

        #endregion

        #region private Methods

        private void InitializeMocksAndTestClass()
        {
            elasticSettings = new Mock<IElasticSettings>();
            elasticService = new Mock<IElasticService>();
            entityProvider = new Mock<IEntityProvider>();
            entityDecorator = new EntityDecorator<TreeRecord>(elasticService.Object, elasticSettings.Object, entityProvider.Object, null);
        }

        private void SetupServiceMock()
        {
            elasticService.Setup(e => e.RunQuery<TreeRecord>(It.IsAny<ElasticQuery>(), It.IsAny<UserAccess>(), true))
                .Returns(Task.FromResult(new ElasticQueryResult<TreeRecord>()));

            elasticService.Setup(e => e.ActaProMappingProvider).Returns(new ActaProMappingProvider());
            elasticSettings.Setup(e => e.ParentIdField).Returns("parentArchiveRecordId");
            elasticSettings.Setup(e => e.IdField).Returns("archiveRecordId");
        }

        private void SetupMocksWithResults()
        {
            var items = new List<Entity<TreeRecord>>();
            items.Add(new Entity<TreeRecord> { Data = new TreeRecord { TreeSequence = 4 } });
            elasticService.Setup(e => e.RunQuery<TreeRecord>(It.IsAny<ElasticQuery>(), It.IsAny<UserAccess>(), true)).Returns(
                Task.FromResult(new ElasticQueryResult<TreeRecord> { Data = new EntityResult<TreeRecord> { Items = items }, TotalNumberOfHits = 1 }));
            entityProvider.Setup(e =>
                    e.GetResultAsEntities(It.IsAny<UserAccess>(), It.IsAny<ElasticQueryResult<TreeRecord>>(), It.IsAny<EntityMetaOptions>()))
                .Returns(Task.FromResult(items));
            elasticService.Setup(e => e.ActaProMappingProvider).Returns(new ActaProMappingProvider());
            elasticSettings.Setup(e => e.ParentIdField).Returns("parentArchiveRecordId");
            elasticSettings.Setup(e => e.IdField).Returns("archiveRecordId");
        }

        #endregion
    }
}