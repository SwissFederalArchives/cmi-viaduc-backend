using System;
using System.IO;
using HtmlAgilityPack;

namespace CMI.Web.Common.Helpers
{
    public static class HtmlHelper
    {
        private static readonly string HtmlIndent = "    ";
        private static readonly string HtmlNewLine = Environment.NewLine;

        private static void WriteHtml(StreamWriter sw, string html, WriteState state)
        {
            sw.Write(html);
            state.EndedWithNewLine = html.EndsWith(HtmlNewLine);
        }

        private static void WriteNode(StreamWriter sw, HtmlNode node, int level, WriteState state)
        {
            if (sw == null || node == null)
            {
                return;
            }

            var indent = "div,form,ol,ul".Contains(node.Name.ToLowerInvariant()) || state.EndedWithNewLine;

            if (node.HasChildNodes == false)
            {
                if (indent)
                {
                    if (!state.EndedWithNewLine)
                    {
                        WriteHtml(sw, HtmlNewLine, state);
                    }

                    for (var i = 0; i < level; i++)
                    {
                        sw.Write(HtmlIndent);
                    }
                }

                WriteHtml(sw, node.OuterHtml, state);
            }
            else
            {
                if (indent)
                {
                    if (!state.EndedWithNewLine)
                    {
                        WriteHtml(sw, HtmlNewLine, state);
                    }

                    for (var i = 0; i < level; i++)
                    {
                        sw.Write(HtmlIndent);
                    }
                }

                // opening tag
                sw.Write("<{0}", node.Name);
                if (node.HasAttributes)
                {
                    foreach (var attr in node.Attributes)
                    {
                        sw.Write(" {0}=\"{1}\"", attr.Name, attr.Value);
                    }
                }

                sw.Write(">");

                // children
                if (indent)
                {
                    WriteHtml(sw, HtmlNewLine, state);
                    level += 1;
                }

                foreach (var child in node.ChildNodes)
                {
                    WriteNode(sw, child, level, state);
                }

                if (indent)
                {
                    level -= 1;
                }

                // closing tag
                if (state.EndedWithNewLine)
                {
                    for (var i = 0; i < level; i++)
                    {
                        sw.Write(HtmlIndent);
                    }
                }

                sw.Write("</{0}>", node.Name);
                state.EndedWithNewLine = false;
            }
        }

        public class WriteState
        {
            public bool EndedWithNewLine = true; // Beginning of file
        }
    }

    public static class HtmlHelperExtensions
    {
        public static HtmlDocument ToHtmlDocument(this string markup)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(markup);
            return doc;
        }

        public static HtmlNode SelectNode(this HtmlNode containerNode, string selector)
        {
            return containerNode.SelectSingleNode(selector);
        }

        public static HtmlNode RemoveNode(this HtmlNode containerNode, string selector)
        {
            var node = containerNode.SelectNode(selector);
            if (node != null)
            {
                node.Remove();
            }

            return node;
        }

        public static HtmlNode CreateScriptNode(this HtmlDocument document, string scriptContent)
        {
            var scriptNode = new HtmlNode(HtmlNodeType.Element, document, 0);
            scriptNode.Name = "script";
            scriptNode.AppendChild(document.CreateTextNode(scriptContent));
            return scriptNode;
        }
    }
}