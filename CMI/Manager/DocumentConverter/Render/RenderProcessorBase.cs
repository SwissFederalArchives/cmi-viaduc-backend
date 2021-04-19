using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CMI.Manager.DocumentConverter.Render
{
    public abstract class RenderProcessorBase : IRenderer
    {
        protected static string logFileName = "log.txt";

        protected RendererCommand cmd = null;
        protected string title;

        protected RenderProcessorBase(string outputExtension, List<string> allowedExtensions)
        {
            OutputExtension = outputExtension;
            AllowedExtensions = allowedExtensions;
        }

        public virtual List<string> AllowedExtensions { get; }
        public string OutputExtension { get; }
        public string ProcessorPath { get; set; }

        public abstract FileInfo Render(RendererCommand rendererCommand);

        protected string GetFileNameWithoutExtension(FileInfo fileInfo)
        {
            return fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
        }

        protected void CreateAndStoreTitle()
        {
            title = GetFileNameWithoutExtension(cmd.SourceFile);
        }

        protected void CreateAndStoreTargetFilePath()
        {
            cmd.TargetFile = new FileInfo(Path.Combine(cmd.WorkingDir, $"{title}.{OutputExtension}"));
        }

        protected abstract FileInfo GetResult();

        protected void RenameSourceFileAndStoreValues()
        {
            Debug.Assert(cmd != null);

            var changedSourceFile = new FileInfo($"{Path.Combine(cmd.SourceFile.DirectoryName, cmd.Identifier)}{cmd.SourceFile.Extension}");

            File.Move(cmd.SourceFile.FullName, changedSourceFile.FullName);

            changedSourceFile.Refresh();

            if (!changedSourceFile.Exists)
            {
                throw new FileNotFoundException($"Unable to find source file '{changedSourceFile.FullName}'");
            }

            cmd.SourceFile.Delete();

            cmd.SourceFile.Refresh();

            if (cmd.SourceFile.Exists)
            {
                throw new Exception($"Unable to delete original source file '{cmd.SourceFile.FullName}'");
            }

            cmd.SourceFile = changedSourceFile;
        }

        protected FileInfo GetLogFilePath()
        {
            Debug.Assert(cmd != null);
            Debug.Assert(cmd.WorkingDir != null);

            return new FileInfo(Path.Combine(cmd.WorkingDir, logFileName));
        }
    }
}