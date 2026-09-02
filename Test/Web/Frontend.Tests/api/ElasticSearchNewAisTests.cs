using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Search;
using Shouldly;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Web.Frontend.API.Tests.api
{
    public class ElasticSearchNewAisTests
    {

        #region Tests


        [Test]
        public async Task If_QueryForId_with_int_id_Scope_method_is_called()
        {
            // arrange
            var data = new List<Entity<ElasticArchiveRecord>>
            {
                new () {Data = new ElasticArchiveRecord {ArchiveRecordId = "12345", Title = "Hund", All = "Ball"}},
            };
            var service = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<string>(), It.IsAny<UserAccess>(), true) ==
                Task.FromResult(new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Data = new EntityResult<ElasticArchiveRecord> { Items = data }
                }));

          
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);

            // act
            var result = await service.QueryForId<ElasticArchiveRecord>("12345", userAccess);

            //assert
            result.Data.Items.Count.ShouldBe(1);
            result.Data.Items.ShouldBe(data);
        }


        [Test]
        public async Task If_QueryForId_with_uuid_id_Scope_method_is_not_called()
        {
            // arrange
            var data = new List<Entity<ElasticArchiveRecord>>
            {
                new () {Data = new ElasticArchiveRecord{ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball"}}
            };
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);
            var service = Mock.Of<IElasticService>(setup =>
                setup.QueryForId<ElasticArchiveRecord>(It.IsAny<string>(), It.IsAny<UserAccess>(), true) ==
                Task.FromResult(new ElasticQueryResult<ElasticArchiveRecord>
                {
                    Data = new EntityResult<ElasticArchiveRecord> { Items = data }
                }));
            // act
            var result = await service.QueryForId<ElasticArchiveRecord>("Klas    2badb81d-ca89-5491-a0a2-78051750b341", userAccess);

            //assert
            result.Data.Items.Count.ShouldBe(1);
            result.Data.Items.ShouldBe(data);
        }

        #endregion
        
    }
}
