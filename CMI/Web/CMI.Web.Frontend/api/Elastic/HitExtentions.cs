using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Elastic;

public static class HitExtentions
{
    public static JObject GetHighlightingObj<T>(this IHit<T> hit, UserAccess access, string title ) where T : TreeRecord
    {
        if (hit.Highlight == null || !hit.Highlight.Any())
        {
            return null;
        }

        List<string> titleHighlight;
        List<string> metaDataHighlight;
        if (access.HasAnyTokenFor(hit.Source.FieldAccessTokens))
        {
            titleHighlight = FindHighlights(hit, hit.Source.IsAnonymized ? "unanonymizedFields.title" : "title");
            metaDataHighlight = FindHighlights(hit, "protected_Metadata_Text")?.ToList();
        }
        else
        {
            titleHighlight = FindHighlights(hit, "title");
            metaDataHighlight = FindHighlights(hit, "all_Metadata_Text")?.ToList();
        }

        // Verhindern der Anzeige von geschützten Primärdaten im Snippet für unberechtigte User
        var primaryDataHighlight = access.HasAnyTokenFor(hit.Source.PrimaryDataFulltextAccessTokens)
            ? FindHighlights(hit, "all_Primarydata")
            : null;

        List<string> mostRelevantVektor;
        metaDataHighlight = TakeFirstNonTitleHighlight(titleHighlight, metaDataHighlight);

        if (metaDataHighlight == null || !metaDataHighlight.Any())
        {
            mostRelevantVektor = primaryDataHighlight;
        }
        else
        {
            mostRelevantVektor = metaDataHighlight;
        }

        var highlightobj = new JObject
        {
            ["title"] = titleHighlight != null // titleHighlight kann null enthalten
                ? new JArray(titleHighlight)
                : new JArray(title), // using the passed title that is either the unanonymized title or the normal title 
            ["mostRelevantVektor"] = mostRelevantVektor != null
                ? new JArray(mostRelevantVektor)
                : null
        };

        return highlightobj;
    }
    
    public static JObject GetExplanationObj<T>(this IHit<T> hit, IElasticsearchSerializer serializer) where T : TreeRecord
    {
        if (hit?.Explanation?.Value == null)
        {
            return null;
        }

        var explanationsObj = new JObject
        {
            {"value", hit.Explanation.Value},
            {"explanation", GetExplanationWithObscuredValues(serializer.SerializeToString(hit.Explanation))}
        };

        return explanationsObj;
    }
    
    private static List<string> FindHighlights<T>(IHit<T> hit, string key) where T : TreeRecord
    {
        var foundHighlight = hit.Highlight.FirstOrDefault(kv => kv.Key == key);

        return foundHighlight.Value?.ToList();
    }

    private static List<string> TakeFirstNonTitleHighlight(List<string> titleHighlight, List<string> metaDataHighlight)
    {
        if (metaDataHighlight == null || !metaDataHighlight.Any())
        {
            return new List<string>();
        }

        var enumerable = (IEnumerable<string>) metaDataHighlight;

        if (titleHighlight != null)
        {
            enumerable = metaDataHighlight.Where(e => !titleHighlight.First().Contains(e));
        }

        enumerable = enumerable.Take(1);
        return enumerable.ToList();
    }
    
    private static string GetExplanationWithObscuredValues(string serializedExplanation)
    {
        if (string.IsNullOrEmpty(serializedExplanation))
        {
            return serializedExplanation;
        }

        // Das Pattern findet alle KeyValuePairs, welche
        // 1. mit "metadataAccessTokens", "primaryDataDownloadAccessTokens" oder "primaryDataFulltextAccessTokens" beginnen
        // 2. dann kommt eine beliebige Anzahl an Leerzeichen
        // 3. dann kommt ein Doppelpunkt
        // 4. dann kommt einen beliebige Anzahl beliebiger Zeichen, bis entweder ein ',' oder ein '"' kommt
        // Achtung: der Regex ist Case-sensitive!
        const string pattern = "(metadataAccessTokens|primaryDataDownloadAccessTokens|primaryDataFulltextAccessTokens)[\\s]{0,}:.*?(?=(,|\"))";

        return new Regex(pattern).Replace(serializedExplanation, "***");
    }

}
