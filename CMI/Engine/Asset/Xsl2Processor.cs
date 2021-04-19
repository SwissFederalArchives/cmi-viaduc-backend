using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Saxon.Api;
using Serilog;

namespace CMI.Engine.Asset
{
    public class Xsl2Processor
    {
        private readonly XsltCompiler compiler;
        private readonly Processor processor;
        private XsltTransformer transformer;

        public Xsl2Processor()
        {
            // Create a Processor instance.
            processor = new Processor();
            compiler = processor.NewXsltCompiler();
            compiler.ErrorList = new List<StaticError>();
        }

        public void Load(string xslPath, Dictionary<string, string> paramCollection)
        {
            try
            {
                TextReader input = new StreamReader(xslPath);
                var directory = new FileInfo(xslPath).Directory;
                if (directory != null)
                {
                    compiler.BaseUri = new Uri(directory.FullName + "\\");
                }

                transformer = compiler.Compile(input).Load();
                // Set the parameters, if any were passed
                if (paramCollection != null)
                {
                    foreach (var param in paramCollection)
                    {
                        transformer.SetParameter(new QName("", "", param.Key), new XdmAtomicValue(param.Value));
                    }
                }

                input.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                throw;
            }
        }

        public void Load(XmlReader xmlReader)
        {
            transformer = compiler.Compile(xmlReader).Load();
        }

        public void Load(XmlNode xslDoc)
        {
            var xmlNodeReader = new XmlNodeReader(xslDoc);
            Load(xmlNodeReader);
        }

        public string Transform(XmlNode input)
        {
            var inputNode = processor.NewDocumentBuilder().Build(input);
            transformer.InitialContextNode = inputNode;
            var dest = processor.NewSerializer();
            var stream = new MemoryStream();
            dest.SetOutputStream(stream);
            transformer.Run(dest);

            return StreamUtility.MemoryStreamToString(stream);
        }
    }
}