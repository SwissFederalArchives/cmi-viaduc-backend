using CMI.Web.Common.Auth;
using Moq;
using NUnit.Framework;

namespace CMI.Web.Common.Tests.Auth
{
    [TestFixture]
    public class AuthServiceNotificationsTests
    {
        [SetUp]
        public void Init()
        {
            testObject = new Mock<AuthServiceNotifications>(null, null);
        }

        public Mock<AuthServiceNotifications> testObject;

        [Test]
        public void TestIsValidLoginTypeExternStatus()
        {
            // ARRANGE
            const string tanAuth = "urn:oasis:names:tc:SAML:2.0:ac:classes:NomadTelephony";
            const string benutzernameUndPasswordAuth =
                "urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport";

            // ACT
            var resultKerberosAuth = testObject.Object.IsValidLoginType(tanAuth);
            var resultSmartcartAuth = testObject.Object.IsValidLoginType(benutzernameUndPasswordAuth);

            // ASSERT
            Assert.IsTrue(resultKerberosAuth);
            Assert.IsTrue(resultSmartcartAuth);
        }

        [Test]
        public void TestIsValidLoginTypeInterneStatus()
        {
            // ARRANGE
            const string kerberosAuth = "urn:oasis:names:tc:SAML:2.0:ac:classes:Kerberos";
            const string smartcartAuth = "urn:oasis:names:tc:SAML:2.0:ac:classes:SmartcardPKI";

            // ACT
            var resultKerberosAuth = testObject.Object.IsValidLoginType(kerberosAuth);
            var resultSmartcartAuth = testObject.Object.IsValidLoginType(smartcartAuth);

            // ASSERT
            Assert.IsTrue(resultKerberosAuth);
            Assert.IsTrue(resultSmartcartAuth);
        }

        [Test]
        public void TestIsValidLoginTypeNichtGueltig()
        {
            // ARRANGE
            const string nichtGueltigAuth = "blabla:dasd";

            // ACT
            var resultEmpty = testObject.Object.IsValidLoginType(string.Empty);
            var resultWrongAuth = testObject.Object.IsValidLoginType(nichtGueltigAuth);

            // ASSERT
            Assert.IsFalse(resultEmpty);
            Assert.IsFalse(resultWrongAuth);
        }

        [Test]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:20")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:30")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:40")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:50")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:60")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:51")]
        public void TestIsValidLoginTypeQoA(string qoa)
        {
            // ARRANGE

            // ACT
            var result = testObject.Object.IsValidLoginType(qoa);

            // ASSERT
            Assert.IsTrue(result);
        }
    }
}