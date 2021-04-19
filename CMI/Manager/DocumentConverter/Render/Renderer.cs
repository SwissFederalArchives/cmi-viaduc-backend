using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.DocumentConverter;

namespace CMI.Manager.DocumentConverter.Render
{
    public class Renderer : IFileProcessorFactory
    {
        private readonly List<string> availableExtensions;
        private readonly List<RenderProcessorBase> renderers;

        public Renderer(RenderProcessorBase[] renderers)
        {
            this.renderers = renderers.ToList();
            availableExtensions = new List<string>();
            this.renderers.ForEach(r => availableExtensions.AddRange(r.AllowedExtensions));
        }

        public IEnumerable<string> GetAvailableExtensions()
        {
            return availableExtensions;
        }

        public bool IsValidExtension(string extension)
        {
            return GetAvailableExtensions()
                .Any(e => e.Equals(RemoveDotFromExtension(extension), StringComparison.InvariantCultureIgnoreCase));
        }

        public IRenderer GetRendererForDestinationExtension(string destinationExtension)
        {
            return renderers
                .SingleOrDefault(r =>
                    r.OutputExtension.Equals(RemoveDotFromExtension(destinationExtension), StringComparison.InvariantCultureIgnoreCase));
        }

        private string RemoveDotFromExtension(string extension)
        {
            return extension.Replace(".", string.Empty);
        }
    }
}