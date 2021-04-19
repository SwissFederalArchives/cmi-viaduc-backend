using System.Collections.Generic;
using System.Linq;
using CMI.Web.Frontend.api;
using CMI.Web.Frontend.api.Interfaces;
using Moq;

namespace CMI.Web.Frontend.API.Tests.api
{
    public class MockIWoerterbuch
    {
        private readonly List<SynonymGroup> groups;
        private readonly Mock<IWoerterbuch> mock;
        private readonly string SearchTerm;
        private Source source;


        public MockIWoerterbuch(string searchTerm)
        {
            SearchTerm = searchTerm;
            mock = new Mock<IWoerterbuch>();
            groups = new List<SynonymGroup>();
            source = new Source("TEST\tder Test\tle Test\til Test\tthe Test");
        }


        public MockIWoerterbuch(string searchTerm, string fileName, string lng_de, string lng_fr, string lng_it, string lng_en)
        {
            SearchTerm = searchTerm;
            mock = new Mock<IWoerterbuch>();
            groups = new List<SynonymGroup>();
            source = new Source($"{fileName}\t{lng_de}\t{lng_fr}\t{lng_it}\t{lng_en}");
        }

        public MockIWoerterbuch AddSynonymGroup(Source externalSource, params string[] lines)
        {
            groups.Add(new SynonymGroup(lines.ToList(), externalSource));
            return this;
        }

        public MockIWoerterbuch AddSynonymGroup(params string[] lines)
        {
            return AddSynonymGroup(source, lines);
        }

        public MockIWoerterbuch AddSynonymGroup(string fileName, string lng_de, string lng_fr, string lng_it, string lng_en, params string[] lines)
        {
            return AddSynonymGroup(new Source($"{fileName}\t{lng_de}\t{lng_fr}\t{lng_it}\t{lng_en}"),
                lines);
        }


        public MockIWoerterbuch SetDefaultSource(Source newSource)
        {
            source = newSource;
            return this;
        }

        public MockIWoerterbuch SetDefaultSource(string fileName, string lng_de, string lng_fr, string lng_it, string lng_en)
        {
            return SetDefaultSource(new Source($"{fileName}\t{lng_de}\t{lng_fr}\t{lng_it}\t{lng_en}"));
        }

        public MockIWoerterbuch SetDefaultSource(string sourceString)
        {
            return SetDefaultSource(new Source(sourceString));
        }


        public Mock<IWoerterbuch> SetupAndReturn()
        {
            mock.Setup(foo => foo.FindGroups(SearchTerm)).Returns(groups);
            return mock;
        }
    }
}