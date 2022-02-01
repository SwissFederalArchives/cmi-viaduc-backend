using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public class Extractor : IFileProcessorFactory
    {
        private readonly TextExtractorBase[] extractors;
        private List<string> availableExtensions;

        public Extractor(TextExtractorBase[] extractors)
        {
            this.extractors = extractors;
            StoreAvailableExtensions();
        }

        public IEnumerable<string> GetAvailableExtensions()
        {
            return availableExtensions;
        }

        public bool IsValidExtension(string extension)
        {
            return GetAvailableExtensions().Any(e => e.Equals(extension, StringComparison.InvariantCultureIgnoreCase));
        }

        public void SetAbbyyInfosIfNeccessary(string pathToAbbyyFrEngineDll, string missingAbbyyPathInstallationMessage)
        {
            extractors
                .OfType<INeedsAbbyyInstallation>()
                .ToList()
                .ForEach(e =>
                {
                    if (!e.PathToAbbyFrEngineDllHasBeenSet)
                    {
                        e.PathToAbbyyFrEngineDll = pathToAbbyyFrEngineDll;
                        e.PathToAbbyFrEngineDllHasBeenSet = true;
                    }

                    if (!e.MissingAbbyyPathInstallationMessageHasBeenSet)
                    {
                        e.MissingAbbyyPathInstallationMessage = missingAbbyyPathInstallationMessage;
                        e.MissingAbbyyPathInstallationMessageHasBeenSet = true;
                    }
                });

            StoreAvailableExtensions();
        }

        public TextExtractorBase GetExtractorForExtension(string extension, Type excludeType = null)
        {
            return extractors.OrderBy(ex => ex.Rank).FirstOrDefault(
                ex => ex.SupportsExtension(extension)
                      && ex.IsAvailable
                      && (excludeType == null || ex.GetType() != excludeType));
        }

        public TextExtractorBase GetAsposeExtractor()
        {
            return extractors.OrderBy(ex => ex.Rank).FirstOrDefault(
                ex => ex.GetType() == typeof(AsposePdfTextExtractor)
                      && ex.IsAvailable);
        }

        public TextExtractorBase GetAbbyyExtractor()
        {
            return extractors.OrderBy(ex => ex.Rank).FirstOrDefault(
                ex => ex.GetType() == typeof(AbbyyTextExtractor)
                      && ex.IsAvailable);
        }

        private void StoreAvailableExtensions()
        {
            var lst = new List<string>();
            foreach (var ex in extractors.Where(e => e.IsAvailable).ToList())
            {
                lst.AddRange(ex.AllowedExtensions);
            }

            availableExtensions = lst.Distinct().ToList();
        }
    }
}