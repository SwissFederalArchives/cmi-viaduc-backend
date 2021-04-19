using System;
using System.Collections.Generic;
using System.Dynamic;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CMI.Utilities.Template.Tests
{
    [TestFixture]
    public class HelperTests
    {
        [SetUp]
        public void SetUp()
        {
            mailHelper = new MailHelper();
        }

        private IMailHelper mailHelper;

        [Test]
        public void AnEmptyTemplateShouldReturnAnEmptyResult()
        {
            mailHelper.TransformToHtml("", new object()).Should().Be("");
        }

        [Test]
        public void ATemplateWithoutPlaceholdersShouldReturnTheTemplate()
        {
            mailHelper.TransformToHtml("Hello", new object()).Should().Be("Hello");
        }

        [Test]
        public void ASimplePlaceholderShouldBeReplaced()
        {
            var data = new Dictionary<string, object>();
            data["InteractiveUser"] = new Person {Name = "Peter"};
            mailHelper.TransformToHtml("Hello {{InteractiveUser.Name}}", data).Should().Be("Hello Peter");
        }

        [Test]
        public void AMailTemplate()
        {
            var mt = new MailTemplate
                {To = "{{InteractiveUser.EMail}}", Body = "Hello {{ InteractiveUser.Name }}\r\nDein Auftrag kann abgeholt werden."};

            var data = new Dictionary<string, object>();
            data["InteractiveUser"] = new Person {Name = "Peter", EMail = "meier@cmiag.ch"};

            var serializeObject = JsonConvert.SerializeObject(mt);
            var result = mailHelper.TransformToHtml(serializeObject, data);

            var email = JsonConvert.DeserializeObject<MailTemplate>(result);
            email.Body.Should().Be("Hello Peter\r\nDein Auftrag kann abgeholt werden.");
            email.To.Should().Be("meier@cmiag.ch");
        }


        [Test]
        public void MehrzeiligesFeldMitCrNlSollMitBrGerendertWerden()
        {
            dynamic expando = new ExpandoObject();
            expando.Text = "Hello\r\nWorld!";
            string r = mailHelper.TransformToHtml("{{Text}}", expando);
            r.Should().Be("Hello<br>World!");
        }

        [Test]
        public void MehrzeiligesFeldMitNlSollMitBrGerendertWerden()
        {
            dynamic expando = new ExpandoObject();
            expando.Text = "Hello\nWorld!";
            string r = mailHelper.TransformToHtml("{{Text}}", expando);
            r.Should().Be("Hello<br>World!");
        }

        [Test]
        public void NewlineInDerVorlageSollKeinBrErzeugen()
        {
            dynamic expando = new ExpandoObject();
            expando.Text = "hello";
            string r = mailHelper.TransformToHtml("<HTML><H1>\n{{Text}}</H1>\n</HTML>", expando);
            r.Should().Be("<HTML><H1>\nhello</H1>\n</HTML>");
        }

        [Test]
        public void UmlauteSollenHtmlEscapedtWerden()
        {
            dynamic expando = new ExpandoObject();
            expando.Text = "hä";
            string r = mailHelper.TransformToHtml("{{Text}}", expando);
            r.Should().Be("h&#228;");
        }

        [Test]
        public void HtmlTemplateMussErhaltenBleiben()
        {
            dynamic expando = new ExpandoObject();
            expando.Text = "hello";
            string r = mailHelper.TransformToHtml("<HTML><H1>{{Text}}</H1></HTML>", expando);
            r.Should().Be("<HTML><H1>hello</H1></HTML>");
        }

        [Test]
        public void DatumMussInUserCultureGeparsedWerdenDefaultDeutsch()
        {
            dynamic expando = new ExpandoObject();

            expando.Datum = new DateTime(2019, 5, 16, 13, 15, 02);

            string r = mailHelper.TransformToHtml("<HTML><H1>{{Datum}}</H1></HTML>", expando);
            r.Should().Be("<HTML><H1>16.05.2019 13:15:02</H1></HTML>");
        }

        [Test]
        public void DatumMussInUserCultureGeparsedWerdenDeutsch()
        {
            dynamic expando = new ExpandoObject();
            expando.Datum = new DateTime(2019, 5, 16, 13, 15, 02).ToString("dd.MM.yyyy HH:mm");

            string r = mailHelper.TransformToHtml("<html lang=\"en\"><H1>{{Datum}}</H1></html>", expando, "de");
            r.Should().Be("<html lang=\"en\"><H1>16.05.2019 13:15</H1></html>");
        }

        [Test]
        public void DatumMussInUserCultureGeparsedWerdenFranzoesisch()
        {
            dynamic expando = new ExpandoObject();
            expando.Datum = new DateTime(2019, 5, 16, 13, 15, 02);

            string r = mailHelper.TransformToHtml("<HTML><H1>{{Datum}}</H1></HTML>", expando, "fr");
            r.Should().Be("<HTML><H1>16.05.2019 13:15:02</H1></HTML>");
        }

        [Test]
        public void DatumMussInUserCultureGeparsedWerdenItalienisch()
        {
            dynamic expando = new ExpandoObject();
            expando.Datum = new DateTime(2019, 5, 16, 13, 15, 02);

            string r = mailHelper.TransformToHtml("<HTML><H1>{{Datum}}</H1></HTML>", expando, "it");
            r.Should().Be("<HTML><H1>16.05.2019 13:15:02</H1></HTML>");
        }

        [Test]
        public void DatumMussInUserCultureGeparsedWerdenEnglisch()
        {
            dynamic expando = new ExpandoObject();
            expando.Datum = new DateTime(2019, 5, 16, 13, 15, 02);

            string r = mailHelper.TransformToHtml("<HTML><H1>{{Datum}}</H1></HTML>", expando, "en");
            r.Should().Be("<HTML><H1>16/05/2019 13:15:02</H1></HTML>");
        }

        [Test]
        public void ZahlMussInUserCultureGeparsedWerdenDeutschDefault()
        {
            dynamic expando = new ExpandoObject();
            expando.Zahl = 1234567.50M;

            string r = mailHelper.TransformToText("{{Zahl}}", expando, "de");
            r.Should().Be("1234567.50");
        }

        [Test]
        public void ZahlMussInUserCultureGeparsedWerdenDeutsch()
        {
            dynamic expando = new ExpandoObject();
            expando.Zahl = 1234567.50M;

            string r = mailHelper.TransformToText("{{Zahl}}", expando, "de");
            r.Should().Be("1234567.50");
        }

        [Test]
        public void ZahlMussInUserCultureGeparsedWerdenItalienisch()
        {
            dynamic expando = new ExpandoObject();
            expando.Zahl = 1234567.50M;

            string r = mailHelper.TransformToText("{{Zahl}}", expando, "it");
            r.Should().Be("1234567.50");
        }

        [Test]
        public void ZahlMussInUserCultureGeparsedWerdenEnglisch()
        {
            dynamic expando = new ExpandoObject();
            expando.Zahl = 1234567.50M;

            string r = mailHelper.TransformToText("{{Zahl}}", expando, "en");
            r.Should().Be("1234567.50");
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public string Vorname { get; set; }
        public string EMail { get; set; }
        public string Organisation { get; set; }
    }

    public class MailTemplate
    {
        public string To { get; set; }
        public string Body { get; set; }
    }
}