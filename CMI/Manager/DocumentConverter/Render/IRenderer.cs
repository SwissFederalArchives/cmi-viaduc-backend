using System.Collections.Generic;
using System.IO;

namespace CMI.Manager.DocumentConverter.Render
{
    public interface IRenderer
    {
        /// <summary>
        ///     Enthaelt die Extensions, die verarbeitet werden koennen
        /// </summary>
        List<string> AllowedExtensions { get; }

        /// <summary>
        ///     Enthaelt die Extension, mit der das Ergebnis abgespeichert wird.
        ///     Die Outputextension kann mit einer AllowedExtension identisch sein
        /// </summary>
        string OutputExtension { get; }

        /// <summary>
        ///     Der Pfad enthaelt den kompletten Pfad einschliesslich der Datei
        /// </summary>
        string ProcessorPath { get; set; }

        FileInfo Render(RendererCommand rendererCommand);
    }
}