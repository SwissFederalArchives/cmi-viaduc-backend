﻿using System.Collections.Generic;

namespace CMI.Engine.Asset
{
    public interface ITransformEngine
    {
        /// <summary>
        ///     Transforms the passed xml file given the transformation.
        /// </summary>
        /// <param name="sourceFile">The source file to transform.</param>
        /// <param name="transformationFile">The xslt transformation file.</param>
        /// <param name="paramCollection">Parameters to pass to the transformation</param>
        /// <returns>System.String.</returns>
        string TransformXml(string sourceFile, string transformationFile, Dictionary<string, string> paramCollection);

        /// <summary>
        /// Converts the metadata xml found in a Benutzungskopie to a valid Arelda metadata.xml
        /// </summary>
        /// <param name="tempFolder">The temporary folder where the metadata.xml can be found</param>
        /// <returns></returns>
        bool ConvertAreldaMetadataXml(string tempFolder);
    }
}