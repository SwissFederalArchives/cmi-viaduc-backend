using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.Results;
using CMI.Tools.AnonymizeServiceMock.Properties;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Tools.AnonymizeServiceMock.Test
{
    [TestFixture]
    public class AnonymizeServiceMockCaller
    {
        private static WordList words;

        [Test]
        public void AnonymizeText_Post_Text_Ohne_Schwaerzung_Return_SameText()
        {
            var mockController = AnonymisierungControllerInitialize();
            var request = new AnonymizationRequest
            {
                Values = new Dictionary<string, string> { { "Test", "Test" } }
            };

            var result = mockController.AnonymizeText(request);
            var returnValue = (OkNegotiatedContentResult<AnonymizationResponse>)result;
            returnValue.Content.AnonymizedValues.FirstOrDefault(a => a.Key == "Test").Value.Should().BeEquivalentTo("Test");
        }

        [Test]
        public void AnonymizeText_Post_Text_Mit_Schwaerzung_Return_Text_Mit_Anonym_Tag()
        {
            var mockController = AnonymisierungControllerInitialize();
            var request = new AnonymizationRequest
            {
                Values = new Dictionary<string, string> { { "Test", "Sommer Hans und Peter Ettwein" } }
            };
            var result = mockController.AnonymizeText(request);
            var returnValue = (OkNegotiatedContentResult<AnonymizationResponse>)result;
            returnValue.Content.AnonymizedValues.FirstOrDefault(a => a.Key == "Test").Value.Should().BeEquivalentTo($@"<anonym type=""n"">Sommer Hans</anonym> und Peter Ettwein");
        }

        [Test]
        public void AnonymizeText_Post_Text_Mit_Mehrfach_Schwaerzung_Return_Text_Mit_Anonym_Tag()
        {
            var mockController = AnonymisierungControllerInitialize();
            var request = new AnonymizationRequest
            {
                Values = new Dictionary<string, string> { { "Test", "Laubscher Berta und Andreas Bellwald in Bern" + Environment.NewLine +
                                                                    "Basel Basel Laubscher Berta und Andreas Bellwald"} }
            };
            var result = mockController.AnonymizeText(request);
            var returnValue = (OkNegotiatedContentResult<AnonymizationResponse>)result;
            returnValue.Content.AnonymizedValues.FirstOrDefault(a => a.Key == "Test").Value.Should().BeEquivalentTo(
                $@"<anonym type=""n"">Laubscher Berta</anonym> und <anonym type=""n"">Andreas Bellwald</anonym> in Bern" + Environment.NewLine +
                $@"Basel Basel <anonym type=""n"">Laubscher Berta</anonym> und <anonym type=""n"">Andreas Bellwald</anonym>");
        }

        [Test]
        public void AnonymizeText_Post_Text_Ist_Groesser_200000_Zeichen_Return_Fehlercode413()
        {
            var mockController = AnonymisierungControllerInitialize();
            var bigText = new StringBuilder();
            for (int index = 0; index < 200005; index++)
            {
                bigText.Append("x");
            }

            var request = new AnonymizationRequest
            {
                Values = new Dictionary<string, string> { { "Test", bigText.ToString() } }
            };
            var result = mockController.AnonymizeText(request);
            var returnValue = (NegotiatedContentResult<string>)result;
            returnValue.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        }

        [Test]
        public void AnonymizeText_Post_Wrong_Parameter_Return_Fehlercode400()
        {
            ApiKeyChecker.Key = "1234";
            var mockController = new AnonymisierungController(words);
            mockController.Request = new HttpRequestMessage();
            mockController.Request.Headers.Add("X-ApiKey", "1234");

            var result = mockController.AnonymizeText(null);
            var returnValue = (BadRequestErrorMessageResult)result;
            returnValue.Should().NotBeNull();
            returnValue.Message.Should().BeEquivalentTo("At least one value must be passed that is not empty.");
        }

        [Test]
        public void AnonymizeText_Post_Wrong_WebApiKey_Return_Fehlercode401()
        {
            ApiKeyChecker.Key = AnonymizeSettings.Default.ApiKey;
            var mockController = new AnonymisierungController(words);
            mockController.Request = new HttpRequestMessage();
            mockController.Request.Headers.Add("X-ApiKey", "1235");
            var request = new AnonymizationRequest();
            var result = mockController.AnonymizeText(request);
            var returnValue = (UnauthorizedResult)result;
            returnValue.Should().NotBeNull();
        }

        [Test]
        public void AnonymizeText_Post_No_WebApiKeyHeader_Return_Fehlercode401()
        {
            ApiKeyChecker.Key = AnonymizeSettings.Default.ApiKey;
            var mockController = new AnonymisierungController(words);
            mockController.Request = new HttpRequestMessage();

            var result = mockController.AnonymizeText(new AnonymizationRequest());
            var returnValue = (UnauthorizedResult)result;
            returnValue.Should().NotBeNull();
        }

        private static AnonymisierungController AnonymisierungControllerInitialize()
        {
            words = new WordList();
            ApiKeyChecker.Key = AnonymizeSettings.Default.ApiKey;
            var mockController = new AnonymisierungController(words);
            mockController.Request = new HttpRequestMessage();
            mockController.Request.Headers.Add("X-ApiKey", AnonymizeSettings.Default.ApiKey);
            return mockController;
        }
    }
}
