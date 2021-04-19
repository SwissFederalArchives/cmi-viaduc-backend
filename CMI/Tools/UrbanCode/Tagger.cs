using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace CMI.Tools.UrbanCode
{
    internal class Tagger
    {
        private readonly string dir;

        public Tagger(string dir)
        {
            this.dir = dir;
        }

        public void ReplaceTags()
        {
            var scanner = new Scanner();
            scanner.Scan(dir);

            foreach (var configFile in FindConfigFiles())
            {
                CreateOrReplaceTagsInConfigFile(configFile, scanner);
            }
        }

        private static void CreateOrReplaceTagsInConfigFile(string configFile, Scanner scanner)
        {
            var currentService = configFile.ExtractServiceName();

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(configFile);

            foreach (var t in scanner.ServicesByType.Keys)
            {
                if (!scanner.ServicesByType[t].Contains(currentService))
                {
                    continue; // dieser Service beötigt das Setting nicht
                }

                CreateSection(xmlDoc, t);
                CreateApplicationSetting(xmlDoc, t);
            }

            xmlDoc.Save(configFile);
        }

        //<?xml version = "1.0" encoding="utf-8"?>
        //<configuration>
        //    <configSections>
        //        <sectionGroup name = "applicationSettings" type="System.Configuration.App...
        //            <section name = "CMI.Utilities.Bus.Configuration.Properties.Settings" t.....
        //        </sectionGroup>
        //    </configSections> 
        private static void CreateSection(XmlDocument xmlDoc, Type type)
        {
            var sectionGroupNode = xmlDoc.SelectSingleNode("configuration/configSections/sectionGroup[@name ='applicationSettings']");

            if (sectionGroupNode == null)
            {
                throw new Exception("sectionGroup Node not found");
            }

            var existingSectionNode = sectionGroupNode.SelectSingleNode($"section[@name='{type.FullName}']");
            if (existingSectionNode != null)
            {
                sectionGroupNode.RemoveChild(existingSectionNode);
            }

            var newSectionNode = sectionGroupNode.AppendChild(xmlDoc.CreateElement("section"));

            var nameAttribute = newSectionNode.Attributes.Append(xmlDoc.CreateAttribute("name"));
            nameAttribute.Value = type.FullName;

            var typeAttribute = newSectionNode.Attributes.Append(xmlDoc.CreateAttribute("type"));
            typeAttribute.Value =
                "System.Configuration.ClientSettingsSection, System, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089";

            var requirePermissionAttribute = newSectionNode.Attributes.Append(xmlDoc.CreateAttribute("requirePermission"));
            requirePermissionAttribute.Value = "false";
        }

        private static void CreateApplicationSetting(XmlDocument xmlDoc, Type t)
        {
            var applicationSettingsNode = xmlDoc.SelectSingleNode("configuration/applicationSettings");
            if (applicationSettingsNode == null)
            {
                throw new Exception("applicationSettings Node not found");
            }

            ;

            var existingAppSettingNode = applicationSettingsNode.SelectSingleNode(t.FullName);

            if (existingAppSettingNode != null)
            {
                applicationSettingsNode.RemoveChild(existingAppSettingNode);
            }

            var newAppSettingNode = applicationSettingsNode.AppendChild(xmlDoc.CreateElement(t.FullName));

            var p = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in p)
            {
                if (prop.DeclaringType == t)
                {
                    var settingNode = newAppSettingNode.AppendChild(xmlDoc.CreateElement("setting"));
                    var nameAttribute = settingNode.Attributes.Append(xmlDoc.CreateAttribute("name"));
                    nameAttribute.Value = prop.Name;

                    var serializeAsAttribute = settingNode.Attributes.Append(xmlDoc.CreateAttribute("serializeAs"));
                    serializeAsAttribute.Value =
                        "String"; // da steht auch bei anderen Typen wie int, double guid und so immer 'String' !

                    var valueNode = settingNode.AppendChild(xmlDoc.CreateElement("value"));
                    valueNode.InnerText = "@@" + t.FullName + "." + prop.Name + "@@";
                }
            }
        }

        private string[] FindConfigFiles()
        {
            var info = new DirectoryInfo(dir);
            return info
                .GetFiles("*.exe.config", SearchOption.AllDirectories)
                .Select(fi => fi.FullName)
                .Where(path => !path.Contains(@"\obj\"))
                .ToArray();
        }
    }
}