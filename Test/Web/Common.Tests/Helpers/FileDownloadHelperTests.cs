using CMI.Web.Common.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Common.Tests.Helpers
{
    [TestFixture]
    public class FileDownloadHelperTests
    {
        [Test]
        public void Are_download_tokens_unique()
        {
            // Arrange
            var helper = new FileDownloadHelper(null);

            // Act
            var t1 = helper.CreateDownloadToken();
            var t2 = helper.CreateDownloadToken();

            // Assert
            t1.Should().NotBe(t2);
        }

        [Test]
        public void Get_correct_configured_token_expiry_time()
        {
            // Arrange
            var settings = new CmiSettings {["tokenValidTime"] = "123"};
            var helper = new FileDownloadHelper(settings);

            // Act
            var result = helper.GetConfigValueTokenValidTime();

            // Assert
            result.Should().Be(123);
        }

        [Test]
        public void Get_default_token_expiry_time_if_no_setting_is_available()
        {
            // Arrange
            var settings = new CmiSettings();
            var defaultExpiryTime = 10;
            var helper = new FileDownloadHelper(settings);

            // Act
            var result = helper.GetConfigValueTokenValidTime();

            // Assert
            result.Should().Be(defaultExpiryTime);
        }
    }
}