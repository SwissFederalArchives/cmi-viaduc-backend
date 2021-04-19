using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CMI.Web.Common.Helpers;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CMI.Web.Common.Tests
{
    [TestFixture]
    public class SettingsHelperTests
    {
        [Test]
        public void SimpleTest()
        {
            string json;

            json = GetJsonFromResource("CMI.Web.Common.Tests.Resources.simple.json");

            JToken jObj = JObject.Parse(json);


            var attributesToCleanup = new HashSet<string>();
            attributesToCleanup.Add("_internal");
            attributesToCleanup.Add("_private");
            attributesToCleanup.Add("_replace");
            attributesToCleanup.Add("_remove");
            attributesToCleanup.Add("_extend");

            jObj.RemoveDescendantsByName(attributesToCleanup);

            var expectedJson = GetJsonFromResource("CMI.Web.Common.Tests.Resources.simpleResult.json");
            jObj.ToString().Should().Be(expectedJson);
        }

        private static string GetJsonFromResource(string resourceName)
        {
            string json;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                json = reader.ReadToEnd();
            }

            return json;
        }
    }
}