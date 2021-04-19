using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Serilog;

namespace CMI.Engine.Asset
{
    public class TransformEngine : ITransformEngine
    {
        private readonly Xsl2Processor processor;

        public TransformEngine(Xsl2Processor processor)
        {
            this.processor = processor;
        }

        /// <summary>
        ///     Transforms the passed xml file given the transformation.
        /// </summary>
        /// <param name="sourceFile">The source file to transform.</param>
        /// <param name="transformationFile">The xslt transformation file.</param>
        /// <param name="paramCollection">Parameters to pass to the transformation</param>
        /// <returns>System.String.</returns>
        public string TransformXml(string sourceFile, string transformationFile, Dictionary<string, string> paramCollection)
        {
            var retVal = string.Empty;
            if (!File.Exists(sourceFile))
            {
                retVal += $"Source file to transorm not found! ({sourceFile})\n";
            }

            if (!File.Exists(transformationFile))
            {
                retVal += $"Transformation file not found! ({transformationFile})";
            }

            try
            {
                if (string.IsNullOrEmpty(retVal))
                {
                    processor.Load(transformationFile, paramCollection);
                    var xDocument = XDocument.Load(sourceFile);
                    retVal = processor.Transform(ToXmlDocument(xDocument));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to transform xml file {sourceFile}", sourceFile);
                throw;
            }

            return retVal;
        }

        private XmlDocument ToXmlDocument(XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }

            return xmlDocument;
        }
    }
}