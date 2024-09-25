using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using FluentAssertions;
using Microsoft.Owin;
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
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:20", "E-ID CH-LOGIN", "Ö2")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:30", "E-ID CH-LOGIN", "Ö2")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:40", "E-ID CH-LOGIN", "Ö3")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:50", "E-ID CH-LOGIN", "Ö3")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:60", "E-ID CH-LOGIN", "Ö3")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:40", "E-ID FED-LOGIN", "BVW")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:50", "E-ID FED-LOGIN", "BVW")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:60", "E-ID FED-LOGIN", "BVW")]
        public void GetIdentity_For_InExisting_External_Public_Client_User_Should_Return_Valid_Identity_With_Status_New_User_And_Specific_Role(string authType, string homeName, string roleResult)
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "Dummy"),
                new(ClaimValueNames.AuthenticationMethod, authType),
                new(ClaimValueNames.HomeName, homeName)
            };

            var controllerHelper = new ControllerHelper(claims);
            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object, null);

            // act
            var result = sut.GetIdentity(null, null, true);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.NeuerBenutzer);
            result.Roles.Should().ContainInOrder(roleResult);
            result.IssuedAccessTokens.Length.Should().Be(0);
            result.RedirectUrl.Should().BeEmpty();
        }

       


        [Test]
        public void GetIdentity_For_InExisting_Internal_Management_Client_User_Should_Return_Valid_Identity()
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche-management-client.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:60"),
                new(ClaimValueNames.HomeName, "E-ID FED-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
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
        public void GetIdentity_For_Existing_Management_Client_User_Without_Role_Should_Throw_Exception()
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche-management-client.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:60"),
                new(ClaimValueNames.HomeName, "E-ID FED-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var applicationRoleUserDataAccessMock = Mock.Of<IApplicationRoleUserDataAccess>();

            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(
                new User {Id = "1"});

            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock, mockUserDataAccess.Object, controllerHelper,
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
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:20"),
                new(ClaimValueNames.HomeName, "E-ID CH-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var applicationRoleUserDataAccessMock = Mock.Of<IApplicationRoleUserDataAccess>();

            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(
                new User {Id = "1"});
            mockUserDataAccess.Setup(m => m.GetRoleForClient(It.IsAny<string>())).Returns("Ö2");

            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock, mockUserDataAccess.Object, controllerHelper,
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
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:30"),
                new(ClaimValueNames.HomeName, "E-ID CH-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var applicationRoleUserDataAccessMock = Mock.Of<IApplicationRoleUserDataAccess>();

            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(
                new User {Id = "1"});
            mockUserDataAccess.Setup(m => m.GetRoleForClient(It.IsAny<string>())).Returns("Ö3");

            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(applicationRoleUserDataAccessMock, mockUserDataAccess.Object, controllerHelper,
                authenticationHelperMock.Object, webCmiConfigProviderMock.Object);

            // act
            var result = sut.GetIdentity(null, null, true);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.Ok);
            result.Roles.Should().ContainInOrder("Ö3");
            result.RedirectUrl.Should().Be("");
        }

        [Test]
        public void GetIdentity_For_Existing_Oe3_User_With_Role_And_Insuficcient_QoA_Should_Return_KeineMTanAuthentication()
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:20"),
                new(ClaimValueNames.HomeName, "E-ID CH-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var request = new Mock<HttpRequestMessage>();
            var applicationRoleUserDataAccessMock = Mock.Of<IApplicationRoleUserDataAccess>();

            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(
                new User {Id = "1"});
            mockUserDataAccess.Setup(m => m.GetRoleForClient(It.IsAny<string>())).Returns("Ö3");

            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sutMock = new Mock<AuthControllerHelper>(applicationRoleUserDataAccessMock, mockUserDataAccess.Object, controllerHelper,
                authenticationHelperMock.Object, webCmiConfigProviderMock.Object);
            sutMock.Setup(m => m.OnExternalSignOut(null, true));
            var sut = sutMock.Object;

            // act
            var result = sut.GetIdentity(request.Object, null, true);

            // assert
            result.AuthStatus.Should().Be(AuthStatus.KeineMTanAuthentication);
            result.Roles.Should().BeEmpty();
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
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:20"),
                new(ClaimValueNames.HomeName, "E-ID CH-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, isPublicClient);

            // assert
            result.Should().Be(AuthStatus.KeineRolleDefiniert);
        }
        
        [Test]
        [TestCase("Ö2", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:10", "E-ID CH-LOGIN", false, AuthStatus.ZuTieferQoAWert)]
        [TestCase("Ö3", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:10", "E-ID CH-LOGIN", false, AuthStatus.ZuTieferQoAWert)]
        [TestCase("Ö3", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:20", "E-ID CH-LOGIN", false, AuthStatus.KeineMTanAuthentication)]
        [TestCase("BVW", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:30", "E-ID FED-LOGIN", false, AuthStatus.ZuTieferQoAWert)]
        [TestCase("AS", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:40", "E-ID FED-LOGIN", true, AuthStatus.ZuTieferQoAWert)]
        [TestCase("BAR", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:50", "E-ID FED-LOGIN", true,AuthStatus.KeineSmartcardAuthentication)]
        [TestCase("AS", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:40", "E-ID FED-LOGIN", false, AuthStatus.RequiresElevatedCheck)]
        [TestCase("BAR", "urn:qoa.eiam.admin.ch:names:tc:ac:classes:40", "E-ID FED-LOGIN", false, AuthStatus.RequiresElevatedCheck)]
        public void IsValidAuthRole_For_User_With_Too_Low_QoA_Should_Return_Status_ZuTieferQoAWert(string role, string authMethod, string homeName, bool isElevatedLogin, AuthStatus expectedResult)
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, authMethod),
                new(ClaimValueNames.HomeName, homeName)
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, true, isElevatedLogin); 

            // assert
            result.Should().Be(expectedResult);
        }

        [Test]
        public void IsValidAuthRole_For_BAR_User_With_HomeName_Not_FED_Login_Should_throw_exception()
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:60"),
                new(ClaimValueNames.HomeName, "E-ID CH-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var action = (Action) (() => { sut.IsValidAuthRole("BAR", true); });

            // assert
            action.Should().Throw<AuthenticationException>()
                .WithMessage("Die BAR-Rolle verlangt zwingend ein FED-Login");
        }

        
       
        
       
        [Test]
        public void IsValidAuthRole_For_Public_Client_Roles_Oe1_Should_Throw_Exception()
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "BAR-recherche.ALLOW"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:60"),
                new(ClaimValueNames.HomeName, "E-ID CH-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
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
        public void IsValidAuthRole_For_Management_Client_Roles_Allow_And_Appo_Should_Return_Ok(string role)
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, role),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:60"),
                new(ClaimValueNames.HomeName, "E-ID FED-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, false);

            // assert
            result.Should().Be(AuthStatus.Ok);
        }

        [Test]
        [TestCase("ALLOW", false, AuthStatus.RequiresElevatedCheck)]
        [TestCase("APPO", false, AuthStatus.RequiresElevatedCheck)]
        [TestCase("ALLOW", true, AuthStatus.KeineSmartcardAuthentication)]
        [TestCase("APPO", true, AuthStatus.KeineSmartcardAuthentication)]
        public void
            IsValidAuthRole_For_Management_Client_Roles_Allow_And_Appo_With_Too_Low_QoA_Should_Return_KeineSmartcardAuthentication(
                string role, bool isElevatedLogin, AuthStatus expectedResult)
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, role),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:50"),
                new(ClaimValueNames.HomeName, "E-ID FED-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
                webCmiConfigProviderMock.Object);

            // act
            var result = sut.IsValidAuthRole(role, false, isElevatedLogin);

            // assert
            result.Should().Be(expectedResult);
        }
        
        [Test]
        public void IsValidAuthRole_For_Management_Client_With_Unknown_Roles_Should_Throw_Exception()
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "dummy"),
                new(ClaimValueNames.AuthenticationMethod, "urn:qoa.eiam.admin.ch:names:tc:ac:classes:20"),
                new(ClaimValueNames.HomeName, "E-ID FED-LOGIN")
            };

            var controllerHelper = new ControllerHelper(claims);

            var authenticationHelperMock = new Mock<IAuthenticationHelper>();
            authenticationHelperMock
                .Setup(m => m.GetClaimsForRequest(It.IsAny<IPrincipal>(), It.IsAny<HttpRequestMessage>()))
                .Returns(claims.Select(ClaimInfo.ConvertClaimToClaimInfo).ToList());

            var mockUserDataAccess = Mock.Of<IUserDataAccess>();
            var webCmiConfigProviderMock = new Mock<IWebCmiConfigProvider>();
            webCmiConfigProviderMock.Setup(m => m.GetStringSetting(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string _, string defaultValue) => defaultValue);

            var sut = new AuthControllerHelper(null, mockUserDataAccess, controllerHelper, authenticationHelperMock.Object,
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
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetFromClaim(It.IsAny<string>())).Returns("claimvalue");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == ClaimValueNames.FamilyName)));
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == ClaimValueNames.FirstName)));
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == ClaimValueNames.Email)));

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
                EmailAddress = "bruno.meier@cmiag.ch",
                IsIdentifiedUser = false,
                HomeName = "claimvalue",
                QoAValue = 30
            });

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetQoAFromClaim()).Returns(30);
            controllerHelperMock.Setup(m => m.GetFromClaim(It.IsAny<string>())).Returns("claimvalue");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == ClaimValueNames.FamilyName)), Times.Never);
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == ClaimValueNames.FirstName)), Times.Never);
            controllerHelperMock.Verify(m => m.GetFromClaim(It.Is<string>(val => val == ClaimValueNames.Email)), Times.Never);

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
                EmailAddress = "bruno.meier@cmiag.ch",
                IsIdentifiedUser = true
            });

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.FamilyName)).Returns("Meier");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.FirstName)).Returns("THOMAS");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.Email)).Returns("bruno.meier@cmiag.ch");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            mockUserDataAccess.Verify(m => m.UpdateUserOnLogin(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(true);
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.FamilyName)).Returns("Meier");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.FirstName)).Returns("Bruno");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.Email)).Returns("bruno.meier@BAR.ADMIN.ch");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            mockUserDataAccess.Verify(m => m.UpdateUserOnLogin(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void TryUpdaterUser_With_User_Should_Update_User_From_Db_If_QoA_Changes()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User
            {
                FamilyName = "Meier",
                FirstName = "Bruno",
                EmailAddress = "bruno.meier@cmiag.ch",
                QoAValue = 50
            });

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetQoAFromClaim()).Returns(30);
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.FamilyName)).Returns("Meier");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.FirstName)).Returns("Bruno");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.Email)).Returns("bruno.meier@BAR.ADMIN.ch");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            mockUserDataAccess.Verify(m => m.UpdateUserOnLogin(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void TryUpdaterUser_With_User_Should_Update_User_From_Db_If_HomeName_Changes()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User
            {
                FamilyName = "Meier",
                FirstName = "Bruno",
                EmailAddress = "bruno.meier@cmiag.ch",
                QoAValue = 50,
                HomeName = "Test"
            });

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(false);
            controllerHelperMock.Setup(m => m.GetQoAFromClaim()).Returns(50);
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.HomeName)).Returns("Changed");
            controllerHelperMock.Setup(m => m.GetFromClaim( ClaimValueNames.FamilyName)).Returns("Meier");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.FirstName)).Returns("Bruno");
            controllerHelperMock.Setup(m => m.GetFromClaim(ClaimValueNames.Email)).Returns("bruno.meier@BAR.ADMIN.ch");

            var sut = new AuthControllerHelper(null, mockUserDataAccess.Object, controllerHelperMock.Object, null, null);

            // act
            sut.TryUpdateUser("1", new List<ClaimInfo>());

            // assert
            mockUserDataAccess.Verify(m => m.UpdateUserOnLogin(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }


        [Test]
        public void TryUpdaterUser_With_Internal_User_Should_Set_Standard_Role_Only_For_Allowed_User()
        {
            // arrange
            var mockUserDataAccess = new Mock<IUserDataAccess>();
            mockUserDataAccess.Setup(m => m.GetUser(It.IsAny<string>())).Returns(new User());

            var controllerHelperMock = new Mock<IControllerHelper>();
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(true);
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
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(true);
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
            controllerHelperMock.Setup(m => m.IsIdentifiedUser()).Returns(true);
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

        [Test]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:20", "E-ID CH-LOGIN", "Ö2")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:30", "E-ID CH-LOGIN", "Ö2")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:40", "E-ID CH-LOGIN", "Ö3")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:50", "E-ID CH-LOGIN", "Ö3")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:60", "E-ID CH-LOGIN", "Ö3")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:40", "E-ID FED-LOGIN", "BVW")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:50", "E-ID FED-LOGIN", "BVW")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:60", "E-ID FED-LOGIN", "BVW")]
        public void Get_Initial_role_for_new_user_returns_correct_role(string authMethod, string homeName, string expectedResult)
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "dummy"),
                new(ClaimValueNames.AuthenticationMethod, authMethod),
                new(ClaimValueNames.HomeName, homeName)
            };

            var sut = new ControllerHelper(claims);

            // act
            var result = sut.GetInitialRoleFromClaim(); 

            // assert
            result.Should().Be(expectedResult);
        }

        [Test]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:22", "E-ID CH-LOGIN")]
        [TestCase("urn:qoa.eiam.admin.ch:names:tc:ac:classes:10", "E-ID CH-LOGIN")]
        public void Get_Initial_role_for_new_user_returns_exception_for_unknown_authMethods(string authMethod, string homeName)
        {
            // arrange
            var claims = new List<Claim>
            {
                new(ClaimValueNames.EIdProfileRole, "dummy"),
                new(ClaimValueNames.AuthenticationMethod, authMethod),
                new(ClaimValueNames.HomeName, homeName)
            };

            var sut = new ControllerHelper(claims);

            // act
            var action = (Action) (() => { sut.GetInitialRoleFromClaim(); });

            // asset
            action.Should().Throw<ArgumentException>();
        }
    }
}