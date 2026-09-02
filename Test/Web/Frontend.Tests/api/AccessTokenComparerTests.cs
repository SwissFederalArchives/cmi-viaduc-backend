using CMI.Contract.Common;
using CMI.Web.Frontend.Helpers;
using Shouldly;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class AccessTokenComparerTests
    {
        [Test]
        public void TokensMatch_AllFieldsMatch_ReturnsTrue()
        {
            var a = new AccessTokens
            {
                MetadataAccessTokens = "BAR, AS, BVW, Ö3, Ö2, Ö1",
                FulltextAccessTokens = "BAR, AS, BVW, Ö3, Ö2, Ö1",
                DownloadAccessTokens = "BAR, AS, BVW, Ö3, Ö2, Ö1",
                FieldAccessTokens = "BAR, AS, BVW, Ö3, Ö2, Ö1"
            };

            var b = new AccessTokens
            {
                MetadataAccessTokens = "Ö1, Ö2, Ö3, BVW, AS, BAR",
                FulltextAccessTokens = "Ö1, Ö2, Ö3, BVW, AS, BAR",
                DownloadAccessTokens = "Ö1, Ö2, Ö3, BVW, AS, BAR",
                FieldAccessTokens = "Ö1, Ö2, Ö3, BVW, AS, BAR"
            };

            AccessTokenComparer.TokensMatch(a, b).ShouldBeTrue();
        }

        [Test]
        public void TokensMatch_FieldAccessDifferent_ReturnsFalse()
        {
            var a = new AccessTokens { FieldAccessTokens = "BAR, AS, F1" };
            var b = new AccessTokens { FieldAccessTokens = "BAR, AS, F2" };

            AccessTokenComparer.TokensMatch(a, b).ShouldBeFalse();
        }

        [Test]
        public void TokensMatch_IgnoreEGandFGTokens_ReturnsTrue()
        {
            var a = new AccessTokens { MetadataAccessTokens = "BAR, EG_123, FG_456" };
            var b = new AccessTokens { MetadataAccessTokens = "BAR" };

            AccessTokenComparer.TokensMatch(a, b).ShouldBeTrue();
        }

        [Test]
        public void TokensMatch_MetadataDifferentTokens_ReturnsFalse()
        {
            var a = new AccessTokens { MetadataAccessTokens = "BAR" };
            var b = new AccessTokens { MetadataAccessTokens = "AS" };

            AccessTokenComparer.TokensMatch(a, b).ShouldBeFalse();
        }

        [Test]
        public void TokensMatch_EmptyFields_ReturnsTrue()
        {
            var a = new AccessTokens();
            var b = new AccessTokens();

            AccessTokenComparer.TokensMatch(a, b).ShouldBeTrue();
        }

        [Test]
        public void TokensMatch_OnlyOneFieldDiffersByEGorFG_ReturnsFalse()
        {
            var a = new AccessTokens
            {
                MetadataAccessTokens = "BAR, AS",
                FulltextAccessTokens = "BAR, AS",
                DownloadAccessTokens = "BAR, AS",
                FieldAccessTokens = "BAR, AS, FG_999"
            };

            var b = new AccessTokens
            {
                MetadataAccessTokens = "AS, BAR",
                FulltextAccessTokens = "AS, BAR",
                DownloadAccessTokens = "AS, BAR",
                FieldAccessTokens = "AS" // BAR missing, FG_999 ignored
            };

            AccessTokenComparer.TokensMatch(a, b).ShouldBeFalse("BAR is missing in b and should not be ignored");
        }

        [Test]
        public void TokensMatch_OnlyFGPrefixDiffersInOneField_ReturnsTrue()
        {
            var a = new AccessTokens
            {
                MetadataAccessTokens = "BAR, AS",
                FulltextAccessTokens = "BAR, AS",
                DownloadAccessTokens = "BAR, AS",
                FieldAccessTokens = "BAR, AS, FG_999"
            };

            var b = new AccessTokens
            {
                MetadataAccessTokens = "BAR, AS",
                FulltextAccessTokens = "BAR, AS",
                DownloadAccessTokens = "BAR, AS",
                FieldAccessTokens = "BAR, AS"
            };

            AccessTokenComparer.TokensMatch(a, b).ShouldBeTrue("FG_999 should be ignored");
        }
    }
}
