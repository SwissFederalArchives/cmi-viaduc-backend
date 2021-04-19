using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Principal;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Web.Common.Tests.Helpers
{
    [TestFixture]
    public class AuthControllerHelperTests
    {
        [Test]
        public void GetIdentity_For_User_Without_Role_Claim_Should_Throw_AuthenticationException()
        {
            // arrange
            var controllerHelperMock = Mock.Of<IControllerHelper>();
            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>());

            var sut = new AuthControllerHelper(null, null, controllerHelperMock, authenticationHelperMock.Object, null);

            // act
            var action = (Action) (() => { sut.GetIdentity(null, null, false); });

            // assert
            action.Should().Throw<AuthenticationException>().WithMessage("User hat noch keinen Antrag gestellt");
        }

        [Test]
        public void GetIdentity_For_InExisting_External_Public_Client_User_Should_Return_Valid_Identity()
        {
            // arrange
            var controllerHelperMock = Mock.Of<IControllerHelper>();
            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "Ö2"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock, authenticationHelperMock.Object, null);

            // act
            var result = sut.GetIdentity(null, null, true);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.NeuerBenutzer);
            result.Roles.Should().ContainInOrder(AccessRoles.RoleOe2);
            result.IssuedAccessTokens.Length.Should().Be(0);
            result.RedirectUrl.Should().BeEmpty();
        }

        [Test]
        public void GetIdentity_For_InExisting_Internal_Public_Client_User_Should_Return_Valid_Identity()
        {
            // arrange
            var controllerHelperMock = Mock.Of<IControllerHelper>(setup => setup.IsInternalUser());

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "BVW"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock, authenticationHelperMock.Object, null);

            // act
            var result = sut.GetIdentity(null, null, true);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.NeuerBenutzer);
            result.Roles.Should().ContainInOrder(AccessRoles.RoleBVW);
            result.IssuedAccessTokens.Length.Should().Be(0);
            result.RedirectUrl.Should().BeEmpty();
        }

        [Test]
        public void GetIdentity_For_InExisting_Internal_Management_Client_User_Should_Return_Valid_Identity()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "BVW"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.GetIdentity(null, null, false);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.NeuerBenutzer);
            result.Roles.Should().ContainInOrder("ALLOW");
            result.IssuedAccessTokens.Length.Should().Be(0);
            result.RedirectUrl.Should().Be("www.recherche.bar.admin.ch/recherche");
        }


        [Test]
        public void GetIdentity_For_Existing_User_Without_Role_Should_Throw_Exception()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "BVW"
                    }
                });

            var applicationRoleUserDataAccessMock = Mock.Of<IApplicationRoleUserDataAccess>();

            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(
                new User {Id = "1"});

            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock, mockUserDataAccess.Object, controllerHelperMock.Object,
                authenticationHelperMock.Object, webCmiConfigProviderMock.Object);

            // act
            var action = (Action) (() => { sut.GetIdentity(null, null, false); });

            // assert
            action.Should().Throw<AuthenticationException>()
                .Where(ex => ex.Message.Contains(
                    "Es wurde für den Benutzer keine Rolle definiert in der Datenbank oder Authentifikation hat fehlgeschlagen"));
        }

        [Test]
        public void GetIdentity_For_Existing_Public_Client_User_With_Role_And_Correct_AuthenticationMethod_Should_Return_Valid_Identity()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "Ö2"
                    }
                });

            var applicationRoleUserDataAccessMock = Mock.Of<IApplicationRoleUserDataAccess>();

            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(
                new User {Id = "1"});
            mockUserDataAccess.Setup(m => m.GetRoleForClient(It.IsAny<string>())).Returns("Ö2");

            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock, mockUserDataAccess.Object, controllerHelperMock.Object,
                authenticationHelperMock.Object, webCmiConfigProviderMock.Object);

            // act
            var result = sut.GetIdentity(null, null, true);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.Ok);
            result.Roles.Should().ContainInOrder("Ö2");
            result.IssuedAccessTokens.Length.Should().Be(0);
            result.RedirectUrl.Should().BeEmpty();
        }

        [Test]
        public void GetIdentity_For_Existing_Oe3_User_With_Role_And_Correct_AuthenticationMethod_Should_Return_Valid_Identity()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.IsMTanAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "Ö3"
                    }
                });

            var applicationRoleUserDataAccessMock = Mock.Of<IApplicationRoleUserDataAccess>();

            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(
                new User {Id = "1"});
            mockUserDataAccess.Setup(m => m.GetRoleForClient(It.IsAny<string>())).Returns("Ö3");

            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock, mockUserDataAccess.Object, controllerHelperMock.Object,
                authenticationHelperMock.Object, webCmiConfigProviderMock.Object);

            // act
            var result = sut.GetIdentity(null, null, true);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.KeineMTanAuthentication);
            result.Roles.Should().ContainInOrder("Ö3");
            result.RedirectUrl.Should().Be("www.recherche.bar.admin.ch/_pep/myaccount?returnURI=/my-appl/private/welcome.html&op=reg-mobile");
        }

        [Test]
        [TestCase("", true)]
        [TestCase("   ", true)]
        [TestCase(null, true)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        [TestCase(null, false)]
        public void IsValidAuthRole_For_Empty_Role_Should_Return_KeineRolleDefiniert(string role, bool isPublicClient)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "BVW"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, isPublicClient);

            // assert
            result.Should().Be(AuthStatus.KeineRolleDefiniert);
        }

        [Test]
        [TestCase("Ö2", true)]
        [TestCase("Ö2", false)]
        [TestCase("Ö3", true)]
        [TestCase("Ö3", false)]
        public void IsValidAuthRole_For_External_User_With_Internal_Authentication_Should_Throw_AuthenticationException(string role,
            bool isKerberosAuth)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(isKerberosAuth);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(!isKerberosAuth);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = role
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var action = (Action) (() => { sut.IsValidAuthRole(role, true); });

            // assert
            action.Should().Throw<AuthenticationException>()
                .WithMessage("Kerberos oder Smartcard dürfen nicht für Ö2 und Ö3 verwendet werden");
        }

        [Test]
        [TestCase("BVW")]
        [TestCase("BAR")]
        [TestCase("AS")]
        public void IsValidAuthRole_For_Internal_User_With_External_Authentication_Should_Throw_AuthenticationException(string role)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = role
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var action = (Action) (() => { sut.IsValidAuthRole(role, true); });

            // assert
            action.Should().Throw<AuthenticationException>()
                .WithMessage("Interne Benutzerrollen (BVW, AS und BAR) müssen Kerberos oder Smartcard verwenden");
        }

        [Test]
        [TestCase("Ö2", false)]
        [TestCase("BVW", true)]
        public void IsValidAuthRole_For_Public_Client_Roles_Oe2_And_Bvw_Should_Return_Ok(string role, bool isInternalUser)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(isInternalUser);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns("ALLOW");
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(isInternalUser);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(isInternalUser);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = role
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, true);

            // assert
            result.Should().Be(AuthStatus.Ok);
        }

        [Test]
        public void IsValidAuthRole_For_Public_Client_Roles_Oe3_Should_Return_Ok_If_IsMTanAuthentication()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsMTanAuthentication()).Returns(true);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "Ö3"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole("Ö3", true);

            // assert
            result.Should().Be(AuthStatus.Ok);
        }

        [Test]
        public void IsValidAuthRole_For_Public_Client_Roles_Oe3_Should_Return_KeineMTanAuthentication_When_User_Is_Missing_MTan_Claim()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsMTanAuthentication()).Returns(false);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "Ö3"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole("Ö3", true);

            // assert
            result.Should().Be(AuthStatus.KeineMTanAuthentication);
        }


        [Test]
        [TestCase("AS")]
        [TestCase("BAR")]
        public void IsValidAuthRole_For_Public_Client_Roles_As_And_Bar_Should_Return_Ok_When_LoggedIn_With_Kerberos(string role)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(true);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = role
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, true);

            // assert
            result.Should().Be(AuthStatus.Ok);
        }

        [Test]
        [TestCase("AS")]
        [TestCase("BAR")]
        public void IsValidAuthRole_For_Public_Client_Roles_As_And_Bar_Should_Return_KeineKerberosAuthentication_When_Not_LoggedIn_With_Kerberos(
            string role)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(true);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = role
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, true);

            // assert
            result.Should().Be(AuthStatus.KeineKerberosAuthentication);
        }

        [Test]
        public void IsValidAuthRole_For_Public_Client_Roles_Oe1_Should_Throw_Exception()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "Ö1"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var action = (Action) (() => { sut.IsValidAuthRole("Ö1", true); });

            // assert
            action.Should().Throw<InvalidOperationException>("Ö1 are not registered users, so they don't have a real session")
                .WithMessage("Nicht definiertes Rollen handling");
        }

        [Test]
        [TestCase("ALLOW")]
        [TestCase("APPO")]
        public void IsValidAuthRole_For_Management_Client_Roles_Allow_And_Appo_Should_Return_Ok_When_LoggedIn_With_Kerberos(string role)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(true);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = role
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, false);

            // assert
            result.Should().Be(AuthStatus.Ok);
        }

        [Test]
        [TestCase("ALLOW")]
        [TestCase("APPO")]
        public void
            IsValidAuthRole_For_Management_Client_Roles_Allow_And_Appo_Should_Return_KeineKerberosAuthentication_When_Not_LoggedIn_With_Kerberos(
                string role)
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = role
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, false);

            // assert
            result.Should().Be(AuthStatus.KeineKerberosAuthentication);
        }

        [Test]
        public void IsValidAuthRole_For_Management_Client_With_Unknown_Roles_Should_Throw_Exception()
        {
            // arrange
            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.IsKerberosAuthentication()).Returns(false);
            controllerHelperMock.Setup(m => m.IsSmartcartAuthentication()).Returns(false);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(new List<ClaimInfo>
                {
                    new ClaimInfo
                    {
                        Type = "/identity/claims/e-id/profile/role",
                        Value = "X-UNKNOWN"
                    }
                });

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelperMock.Object, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var action = (Action) (() => { sut.IsValidAuthRole("X-UNKNOWN", false); });

            // assert
            action.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Nicht definiertes Rollen handling*");
        }

        [Test]
        public void TryUpdaterUser_With_InExistent_User_Should_Return_False()
        {
            // arrange

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();

            var sut = new AuthControllerHelper(null, mockUserDataAccess, null, null, null);

            // act
            var result = sut.TryUpdateUser("1", null);

            // assert
            result.Should().BeFalse();
        }

        [Test]
        public void TryUpdaterUser_With_Internal_User_Should_Update_User_From_Claims()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User());

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetFromClaim(It.IsAny<string>())).Returns("claimvalue");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == "/identity/claims/surname")));
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == "/identity/claims/givenname")));
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == "/identity/claims/emailaddress")));

            mockUserDataAccess.Verify(m =>
                m.UpdateUserOnLogin(It.Is<User>(u => u.EmailAddress == "claimvalue"), It.IsAny<string>(), It.IsAny<string>()));
        }

        [Test]
        public void TryUpdaterUser_With_External_User_Should_Not_Update_User_From_Db_If_There_Are_No_Changes()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User
            {
                FamilyName = "Meier",
                FirstName = "Bruno",
                EmailAddress = "bruno.meier@cmiag.ch"
            });

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetFromClaim(It.IsAny<string>())).Returns("claimvalue");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == "/identity/claims/surname")), Times.Never);
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == "/identity/claims/givenname")), Times.Never);
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == "/identity/claims/emailaddress")), Times.Never);

            mockUserDataAccess.Verify(m => m.UpdateUserOnLogin(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void TryUpdaterUser_With_User_Should_Update_User_From_Db_If_The_Firstname_Changes()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User
            {
                FamilyName = "Meier",
                FirstName = "Bruno",
                EmailAddress = "bruno.meier@cmiag.ch"
            });

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetFromClaim("/identity/claims/surname")).Returns("Meier");
            controllerHelperMock.Setup(m => m.GetFromClaim("/identity/claims/givenname")).Returns("THOMAS");
            controllerHelperMock.Setup(m => m.GetFromClaim("/identity/claims/emailaddress")).Returns("bruno.meier@cmiag.ch");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            mockUserDataAccess.Verify(m => m.UpdateUserOnLogin(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [Test]
        public void TryUpdaterUser_With_User_Should_Update_User_From_Db_If_Email_Changes()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User
            {
                FamilyName = "Meier",
                FirstName = "Bruno",
                EmailAddress = "bruno.meier@cmiag.ch"
            });

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetFromClaim("/identity/claims/surname")).Returns("Meier");
            controllerHelperMock.Setup(m => m.GetFromClaim("/identity/claims/givenname")).Returns("Bruno");
            controllerHelperMock.Setup(m => m.GetFromClaim("/identity/claims/emailaddress")).Returns("bruno.meier@BAR.ADMIN.ch");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            mockUserDataAccess.Verify(m => m.UpdateUserOnLogin(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [Test]
        public void TryUpdaterUser_With_Internal_User_Should_Set_Standard_Role_Only_For_Allowed_User()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User());

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns(AccessRoles.RoleMgntAllow);
            controllerHelperMock.Setup(m => m.GetFromClaim(It.IsAny<string>())).Returns("claimvalue");

            var applicationRoleUserDataAccessMock = new Mock<IApplicationRoleUserDataAccess>();

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock.Object, mockUserDataAccess.Object, controllerHelperMock.Object, null,
                null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            applicationRoleUserDataAccessMock.Verify(m => m.InsertRoleUser(It.Is<string>(val => val == "Standard"), It.IsAny<string>()));
        }

        [Test]
        [TestCase(AccessRoles.RoleMgntAppo)]
        [TestCase("DENY")]
        public void TryUpdaterUser_With_Internal_User_Should_Not_Set_Standard_Role_For_Other_Users(string role)
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User());

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns(role);
            controllerHelperMock.Setup(m => m.GetFromClaim(It.IsAny<string>())).Returns("claimvalue");

            var applicationRoleUserDataAccessMock = new Mock<IApplicationRoleUserDataAccess>();

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock.Object, mockUserDataAccess.Object, controllerHelperMock.Object, null,
                null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            applicationRoleUserDataAccessMock.Verify(m => m.InsertRoleUser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void TryUpdaterUser_With_User_Without_ManagementRole_Should_Remove_StandardRole(string role)
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User());

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsInternalUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetMgntRoleFromClaim()).Returns(role);
            controllerHelperMock.Setup(m => m.GetFromClaim(It.IsAny<string>())).Returns("claimvalue");

            var applicationRoleUserDataAccessMock = new Mock<IApplicationRoleUserDataAccess>();

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock.Object, mockUserDataAccess.Object, controllerHelperMock.Object, null,
                null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            applicationRoleUserDataAccessMock.Verify(m => m.RemoveRolesUser(It.IsAny<string>(), It.IsAny<string[]>()));
        }

        [Test]
        public void AddAppRolesAndFeatuers_Should_Set_Roles_And_Features_On_Identity()
        {
            // arrange
            var userDataAccessMock = new Mock<IUserDataAccess>();
            userDataAccessMock.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User
            {
                Roles = new List<ApplicationRole>
                {
                    new ApplicationRole {Identifier = "APPO", Name = "Applikationsowner"}
                },
                Features = new List<ApplicationFeature>
                {
                    ApplicationFeature.AdministrationEinstellungenBearbeiten,
                    ApplicationFeature.AuftragsuebersichtAuftraegeBegruendungVerwaltungsausleiheEdit
                }
            });

            var sut = new AuthControllerHelper(null, userDataAccessMock.Object, null, null, null);
            var identity = new Identity();
            // act

            sut.AddAppRolesAndFeatures("1", identity);

            // asset
            identity.ApplicationRoles.First().Identifier = "APPO";
            identity.ApplicationFeatures.First().Identifier.Should().Be(ApplicationFeature.AdministrationEinstellungenBearbeiten.ToString());
            identity.ApplicationFeatures.Last().Identifier.Should()
                .Be(ApplicationFeature.AuftragsuebersichtAuftraegeBegruendungVerwaltungsausleiheEdit.ToString());
        }

        [Test]
        public void AddAppRolesAndFeatuers_When_Error_Should_Not_Throw()
        {
            // arrange
            var userDataAccessMock = new Mock<IUserDataAccess>();
            userDataAccessMock.Setup(m => m.GetUser(It.IsAny<string>())).Throws<Exception>();

            var sut = new AuthControllerHelper(null, userDataAccessMock.Object, null, null, null);

            // act

            var action = (Action) (() => { sut.AddAppRolesAndFeatures("1", new Identity()); });

            // asset
            action.Should().NotThrow();
        }
    }
}