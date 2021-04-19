using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using CMI.Utilities.Common;

namespace CMI.Tools.UrbanCode
{
    public class Documenter
    {
        private readonly Scanner scanner = new Scanner();

        public Documenter(string direcory)
        {
            scanner.Scan(direcory);
        }

        public string GetHtml()
        {
            var stringBuilder = new StringBuilder();
            AppendHead(stringBuilder);

            AppendUebersicht(stringBuilder);

            foreach (var t in scanner.ServicesByType.Keys.OrderBy(k => k.FullName))
            {
                stringBuilder.AppendLine($"<h1>{t.FullName}</h1>");

                stringBuilder.AppendLine("<table>");
                stringBuilder.AppendLine("<tr><th>Eigenschaft</th><th>Typ</th><th>Default</th><th>Beschreibung</th></tr>");

                var parts = t.FullName?.Split('.');
                parts[parts.Length - 1] = "Documentation";
                var tp = t.Assembly.GetType(string.Join(".", parts));
                var documentationType = tp == null || !tp.IsSubclassOf(typeof(AbstractDocumentation)) ? null : tp;

                var docByPropInfo = new Dictionary<PropertyInfo, string>();
                if (documentationType != null)
                {
                    var v = (AbstractDocumentation) Activator.CreateInstance(documentationType);
                    v.LoadDescriptions();
                    docByPropInfo = v.Documentations;
                }

                var failedDescriptions = new List<string>();

                foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (prop.DeclaringType != t)
                    {
                        continue;
                    }

                    var defaultvalue = prop.GetCustomAttributes(typeof(DefaultSettingValueAttribute)).OfType<DefaultSettingValueAttribute>()
                        .FirstOrDefault();
                    if (!docByPropInfo.TryGetValue(prop, out var desc))
                    {
                        failedDescriptions.Add(prop.Name);
                    }

                    var printDefault = defaultvalue != null ? defaultvalue.Value : string.Empty;
                    printDefault = printDefault.Replace("\r\n", " ");
                    stringBuilder.AppendLine($"<tr><td>{prop.Name}</td><td>{prop.PropertyType.Name}</td><td>{printDefault}</td><td>{desc}</td></tr>");
                }

                if (failedDescriptions.Count > 0)
                {
                    throw new DescriptionNotSetException("Folgende Beschreibungen für den Typ " + t.AssemblyQualifiedName +
                                                         " wurden nicht gesetzt: " + string.Join(",", failedDescriptions) +
                                                         ". Help: https://github.com/CMInformatik/Viaduc/wiki/Dokumentation-f%C3%BCr-Settings");
                }

                stringBuilder.AppendLine("</table>");
            }

            stringBuilder.AppendLine("</html>");
            return stringBuilder.ToString();
        }

        private void AppendUebersicht(StringBuilder sb)
        {
            sb.AppendLine("<h1>Übersicht</h1>");
            sb.AppendLine("<table>");

            sb.AppendLine("<tr>");
            sb.AppendLine("<th></th>");
            foreach (var service in scanner.Services)
            {
                sb.AppendLine($"<th>{service}</th>");
            }

            sb.AppendLine("</tr>");

            foreach (var t in scanner.ServicesByType.Keys.OrderBy(k => k.FullName))
            {
                sb.Append("<tr>");
                sb.Append($"<td>{t.FullName}</td>");

                foreach (var service in scanner.Services)
                {
                    sb.Append(scanner.ServicesByType[t].Contains(service)
                        ? @"<td style=""text-align: center"">X</td>"
                        : "<td> </td>");
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
        }

        private static void AppendHead(StringBuilder sb)
        {
            sb.AppendLine(@"<!doctype html>
                          <html>
                <head>
                <style>
            h1 {
            padding-top:20px;
            font-size: 10pt;
        }

        body {
            font-family: Helvetica, Arial, sans-serif;
            font-size: 9pt;
        }

        table {
            font-family: ""Lucida Sans Unicode"", ""Lucida Grande"", Sans-Serif;
            font-size: 12px;
            background: #eee;
            border-collapse: collapse;
            text-align: left;
            margin: 20px;
            border: 1px solid #ddd;
        }



        td, th {
            padding-left: 10px;
            padding-right: 10px;
            border-bottom: 1px solid darkgray; /* No more visible border */
            height: 30px;

        }



        th {
            border-bottom: 2px solid black; /* No more visible border */
            font-weight: bold;
            text-align: left;

        }

        td {

            text-align: left;
        }


                        </style>
                        </head> ");
        }
    }
}