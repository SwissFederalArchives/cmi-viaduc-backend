using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace CMI.Tools.JsonCombiner.Tests
{
    [TestFixture]
    public class JsonCombinerTests
    {
        private static readonly string defaultpath = "CMI.Tools.JsonCombiner.Tests.Resources.";

        [Test]
        public void normal_tests()
        {
            ExcuteFolderTests("normal_tests.");
        }

        [Test]
        public void error_handeling_tests()
        {
            ExcuteFolderTests("error_handeling_tests.");
        }

        private static void ExcuteFolderTests(string a)
        {
            foreach (var manifestResourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .Where(s => s.StartsWith(defaultpath + a))
                .Select(s => s.Replace(defaultpath, "").Split('.')[0] + "." + s.Replace(defaultpath, "").Split('.')[1])
                .Distinct())
            {
                try
                {
                    TestFolder(manifestResourceName, Language.En);
                    Console.WriteLine("tested: " + manifestResourceName.Split('.')[1]);
                }
                catch (Exception)
                {
                    Console.WriteLine("failed: " + manifestResourceName.Split('.')[1]);
                    throw;
                }
            }
        }

        private static void TestFolder(string name, Language lng)
        {
            var master = ReadResource(name + ".master.json");
            var source = ReadResource(name + ".source.json");
            var combiner = new JSonFileCombiner();
            string expected;
            var exception = false;

            try
            {
                expected = JToken.Parse(ReadResource(name + ".result.json")).ToString();
            }
            catch (ResourceNotFoundException)
            {
                expected = ReadResource(name + ".exception.txt");
                exception = true;
            }

            try
            {
                JObject.Parse(master);
                JObject.Parse(source);

                if (!exception)
                {
                    if (JObject.Parse(ReadResource(name + ".result.json")).DescendantsAndSelf()
                        .Any(jv => jv.GetType() == typeof(JArray)))
                    {
                        throw new InternalException("result file can not contain json Arrays");
                    }
                }

                Assert.AreEqual(expected, combiner.CombineJsons(JObject.Parse(master), JObject.Parse(source), lng));
            }
            catch (Exception ex)
            {
                if (ex is AssertionException || ex is InternalException)
                {
                    throw;
                }

                Assert.AreEqual(expected.Split('\n')[0].Replace("\r", ""), ex.GetType().Name);
                if (expected.Split('\n').Length > 1)
                {
                    Assert.AreEqual(expected.Split('\n')[1], ex.Message);
                }
            }
        }

        private static string ReadResource(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = defaultpath + path;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new ResourceNotFoundException(resourceName);
                }

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}