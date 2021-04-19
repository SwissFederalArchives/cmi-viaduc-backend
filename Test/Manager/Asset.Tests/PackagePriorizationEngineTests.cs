using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Engine.Asset;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class PackagePriorizationEngineTests
    {
        private ChannelAssignmentDefinition channelAssignmentDefinition = new ChannelAssignmentDefinition
        {
            Channel1 = "1,2,3",
            Channel2 = "1,2,3,4,5,6,7",
            Channel3 = "1,2,3,4,5,6,7,8,9",
            Channel4 = "6,7,8,9,1,2,3,4,5"
        };

        [Test]
        public void A_prefetch_count_of_4_should_result_in_one_job_per_channel()
        {
            var engine = new PackagePriorizationEngine(null, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 4, DownloadQueuePrefetchCount = 4});

            engine.MaxJobCountPerChannelForDownload[1].Should().Be(1);
            engine.MaxJobCountPerChannelForDownload[2].Should().Be(1);
            engine.MaxJobCountPerChannelForDownload[3].Should().Be(1);
            engine.MaxJobCountPerChannelForDownload[4].Should().Be(1);

            engine.MaxJobCountPerChannelForSync[1].Should().Be(1);
            engine.MaxJobCountPerChannelForSync[2].Should().Be(1);
            engine.MaxJobCountPerChannelForSync[3].Should().Be(1);
            engine.MaxJobCountPerChannelForSync[4].Should().Be(1);
        }

        [Test]
        public void A_prefetch_count_of_8_should_result_in_two_jobs_per_channel()
        {
            var engine = new PackagePriorizationEngine(null, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 8, DownloadQueuePrefetchCount = 8});

            engine.MaxJobCountPerChannelForDownload[1].Should().Be(2);
            engine.MaxJobCountPerChannelForDownload[2].Should().Be(2);
            engine.MaxJobCountPerChannelForDownload[3].Should().Be(2);
            engine.MaxJobCountPerChannelForDownload[4].Should().Be(2);

            engine.MaxJobCountPerChannelForSync[1].Should().Be(2);
            engine.MaxJobCountPerChannelForSync[2].Should().Be(2);
            engine.MaxJobCountPerChannelForSync[3].Should().Be(2);
            engine.MaxJobCountPerChannelForSync[4].Should().Be(2);
        }

        [Test]
        public void A_prefetch_count_of_6_should_result_in_two_jobs_for_the_first_two_channels_and_one_job_per_channel_for_the_rest()
        {
            var engine = new PackagePriorizationEngine(null, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 6});

            engine.MaxJobCountPerChannelForDownload[1].Should().Be(2);
            engine.MaxJobCountPerChannelForDownload[2].Should().Be(2);
            engine.MaxJobCountPerChannelForDownload[3].Should().Be(1);
            engine.MaxJobCountPerChannelForDownload[4].Should().Be(1);

            engine.MaxJobCountPerChannelForSync[1].Should().Be(2);
            engine.MaxJobCountPerChannelForSync[2].Should().Be(2);
            engine.MaxJobCountPerChannelForSync[3].Should().Be(1);
            engine.MaxJobCountPerChannelForSync[4].Should().Be(1);
        }


        [Test]
        public void A_prefetch_count_of_6_should_result_in_two_jobs_for_the_first_two_channels_and_one_job_per_channel_for_the_rest_for_sync()
        {
            var engine = new PackagePriorizationEngine(null, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 4});

            engine.MaxJobCountPerChannelForDownload[1].Should().Be(1);
            engine.MaxJobCountPerChannelForDownload[2].Should().Be(1);
            engine.MaxJobCountPerChannelForDownload[3].Should().Be(1);
            engine.MaxJobCountPerChannelForDownload[4].Should().Be(1);

            engine.MaxJobCountPerChannelForSync[1].Should().Be(2);
            engine.MaxJobCountPerChannelForSync[2].Should().Be(2);
            engine.MaxJobCountPerChannelForSync[3].Should().Be(1);
            engine.MaxJobCountPerChannelForSync[4].Should().Be(1);
        }

        [Test]
        public void A_ChannelAssignementDefinition_is_correctly_split_in_ranges()
        {
            channelAssignmentDefinition = new ChannelAssignmentDefinition
            {
                Channel1 = "1,2,3",
                Channel2 = "1,2,3,4,5,6,7",
                Channel3 = "1,2,3,4,5,6,7,8,9",
                Channel4 = "6,7,8,9,1,2,3,4,5"
            };

            var engine = new PackagePriorizationEngine(null, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 4});

            engine.KategorieRangesPerChannel[1].Should().HaveCount(1);
            engine.KategorieRangesPerChannel[2].Should().HaveCount(1);
            engine.KategorieRangesPerChannel[3].Should().HaveCount(1);
            engine.KategorieRangesPerChannel[4].Should().HaveCount(2);

            engine.KategorieRangesPerChannel[1].Should().BeEquivalentTo(new List<List<int>> {new List<int> {1, 2, 3}});
            engine.KategorieRangesPerChannel[2].Should().BeEquivalentTo(new List<List<int>> {new List<int> {1, 2, 3, 4, 5, 6, 7}});
            engine.KategorieRangesPerChannel[3].Should().BeEquivalentTo(new List<List<int>> {new List<int> {1, 2, 3, 4, 5, 6, 7, 8, 9}});
            engine.KategorieRangesPerChannel[4].Should()
                .BeEquivalentTo(new List<List<int>> {new List<int> {6, 7, 8, 9}, new List<int> {1, 2, 3, 4, 5}});
        }

        [Test]
        public void A_ChannelAssignementDefinition_is_correctly_split_in_ranges_2()
        {
            channelAssignmentDefinition = new ChannelAssignmentDefinition
            {
                Channel1 = "3,2,1",
                Channel2 = "1,2,3,4,5,6,7",
                Channel3 = "1,2,3,4,5,6,7,8,9",
                Channel4 = "6,7,8,9,4,5,1,2,3"
            };

            var engine = new PackagePriorizationEngine(null, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 4});

            engine.KategorieRangesPerChannel[1].Should().HaveCount(3);
            engine.KategorieRangesPerChannel[2].Should().HaveCount(1);
            engine.KategorieRangesPerChannel[3].Should().HaveCount(1);
            engine.KategorieRangesPerChannel[4].Should().HaveCount(3);

            engine.KategorieRangesPerChannel[1].Should()
                .BeEquivalentTo(new List<List<int>> {new List<int> {3}, new List<int> {2}, new List<int> {1}});
            engine.KategorieRangesPerChannel[2].Should().BeEquivalentTo(new List<List<int>> {new List<int> {1, 2, 3, 4, 5, 6, 7}});
            engine.KategorieRangesPerChannel[3].Should().BeEquivalentTo(new List<List<int>> {new List<int> {1, 2, 3, 4, 5, 6, 7, 8, 9}});
            engine.KategorieRangesPerChannel[4].Should().BeEquivalentTo(new List<List<int>>
                {new List<int> {6, 7, 8, 9}, new List<int> {4, 5}, new List<int> {1, 2, 3}});
        }

        [Test]
        public async Task No_empty_slots_from_db_results_in_no_new_jobs()
        {
            var db = new Mock<IPrimaerdatenAuftragAccess>();
            // 1 job is running on all channels --> channels full
            db.Setup(d => d.GetCurrentWorkload(AufbereitungsArtEnum.Download))
                .Returns(Task.FromResult(new Dictionary<int, int> {{1, 1}, {2, 1}, {3, 1}, {4, 1}}));
            var engine = new PackagePriorizationEngine(db.Object, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 4});

            var newJobs = await engine.GetNextJobsForExecution(AufbereitungsArtEnum.Download);

            newJobs.Count.Should().Be(0);
        }

        [Test]
        public async Task Empty_slots_in_all_channels_from_db_results_in_new_jobs()
        {
            var channelAssignmentDefinition = new ChannelAssignmentDefinition
            {
                Channel1 = "1,2,3",
                Channel2 = "1,2,3,4,5,6,7",
                Channel3 = "1,2,3,4,5,6,7,8,9",
                Channel4 = "6,7,8,9,1,2,3,4,5"
            };
            var db = new Mock<IPrimaerdatenAuftragAccess>();
            // 0 job is running on channels
            db.Setup(d => d.GetCurrentWorkload(AufbereitungsArtEnum.Download))
                .Returns(Task.FromResult(new Dictionary<int, int> {{1, 0}, {2, 0}, {3, 0}, {4, 0}}));
            // return primaerdatenAuftragId for next possible job
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {100}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5, 6, 7}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {200}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5, 6, 7, 8, 9}, It.IsAny<int>(),
                It.IsAny<int[]>())).Returns(Task.FromResult(new List<int> {300}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {6, 7, 8, 9}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {400}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {500}));
            var engine = new PackagePriorizationEngine(db.Object, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 4});

            var newJobs = await engine.GetNextJobsForExecution(AufbereitungsArtEnum.Download);

            newJobs.Count.Should().Be(4);
            newJobs[1].Should().BeEquivalentTo(new[] {100});
            newJobs[2].Should().BeEquivalentTo(new[] {200});
            newJobs[3].Should().BeEquivalentTo(new[] {300});
            newJobs[4].Should().BeEquivalentTo(new[] {400});
        }

        [Test]
        public async Task Empty_slots_in_channels_from_db_results_in_new_jobs_with_2_possible_jobs_per_channel()
        {
            var channelAssignmentDefinition = new ChannelAssignmentDefinition
            {
                Channel1 = "1,2,3",
                Channel2 = "1,2,3,4,5,6,7",
                Channel3 = "1,2,3,4,5,6,7,8,9",
                Channel4 = "6,7,8,9,1,2,3,4,5"
            };
            var db = new Mock<IPrimaerdatenAuftragAccess>();
            // 0 job is running on channel 1, 1 job on channel 2 and zero on 3 and 4
            db.Setup(d => d.GetCurrentWorkload(AufbereitungsArtEnum.Download))
                .Returns(Task.FromResult(new Dictionary<int, int> {{1, 0}, {2, 1}, {3, 0}, {4, 0}}));
            // return primaerdatenAuftragId for next possible job
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {100, 110}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5, 6, 7}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {200, 210}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5, 6, 7, 8, 9}, It.IsAny<int>(),
                It.IsAny<int[]>())).Returns(Task.FromResult(new List<int> {300, 310}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {6, 7, 8, 9}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {400, 410}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {500, 510}));
            // Prefetch count 8 results in two possible jobs per channel
            var engine = new PackagePriorizationEngine(db.Object, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 8});

            var newJobs = await engine.GetNextJobsForExecution(AufbereitungsArtEnum.Download);

            newJobs.Count.Should().Be(4);
            newJobs[1].Should().BeEquivalentTo(new[] {100, 110});
            newJobs[2].Should().BeEquivalentTo(new[] {200});
            newJobs[3].Should().BeEquivalentTo(new[] {300, 310});
            newJobs[4].Should().BeEquivalentTo(new[] {400, 410});
        }

        [Test]
        public async Task Empty_slots_in_channels_from_db_results_in_new_jobs_with_2_possible_jobs_per_channel_but_queue_4_only_one_large_job()
        {
            var channelAssignmentDefinition = new ChannelAssignmentDefinition
            {
                Channel1 = "1,2,3",
                Channel2 = "1,2,3,4,5,6,7",
                Channel3 = "1,2,3,4,5,6,7,8,9",
                Channel4 = "6,7,8,9,1,2,3,4,5"
            };
            var db = new Mock<IPrimaerdatenAuftragAccess>();
            // 0 job is running on channel 1, 1 job on channel 2 and zero on 3 and 4
            db.Setup(d => d.GetCurrentWorkload(AufbereitungsArtEnum.Download))
                .Returns(Task.FromResult(new Dictionary<int, int> {{1, 0}, {2, 1}, {3, 0}, {4, 0}}));
            // return primaerdatenAuftragId for next possible job
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {100, 110}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5, 6, 7}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {200, 210}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5, 6, 7, 8, 9}, It.IsAny<int>(),
                It.IsAny<int[]>())).Returns(Task.FromResult(new List<int> {300, 310}));
            // DB returns only 1 job for categories 6,7,8,9, so when requesting two jobs a job from 1,2,3,4,5 can be used.
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {6, 7, 8, 9}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {400}));
            db.Setup(d => d.GetNextJobsForChannel(AufbereitungsArtEnum.Download, new[] {1, 2, 3, 4, 5}, It.IsAny<int>(), It.IsAny<int[]>()))
                .Returns(Task.FromResult(new List<int> {500, 510}));
            // Prefetch count 8 results in two possible jobs per channel
            var engine = new PackagePriorizationEngine(db.Object, channelAssignmentDefinition,
                new RepositoryQueuesPrefetchCount {SyncQueuePrefetchCount = 6, DownloadQueuePrefetchCount = 8});

            var newJobs = await engine.GetNextJobsForExecution(AufbereitungsArtEnum.Download);

            newJobs.Count.Should().Be(4);
            newJobs[1].Should().BeEquivalentTo(new[] {100, 110});
            newJobs[2].Should().BeEquivalentTo(new[] {200});
            newJobs[3].Should().BeEquivalentTo(new[] {300, 310});
            newJobs[4].Should().BeEquivalentTo(new[] {400, 500});
        }
    }
}