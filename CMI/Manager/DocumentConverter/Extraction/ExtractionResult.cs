using System.Text;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public class ExtractionResult
    {
        private readonly int maxSize;
        private readonly StringBuilder sb = new StringBuilder();

        public ExtractionResult(int maxResultSize)
        {
            maxSize = maxResultSize;
        }

        public int CharactersLeft => maxSize - sb.Length;

        public bool LimitExceeded => sb.Length > maxSize;

        public bool HasError { get; set; }

        public string ErrorMessage { get; set; }

        public bool HasContent
        {
            get
            {
                if (sb.Length == 0)
                {
                    return false;
                }

                return !string.IsNullOrWhiteSpace(sb.ToString());
            }
        }

        public void Append(string text)
        {
            if (text == null)
            {
                return;
            }

            var s = text.Trim();

            if (s.Length > 1)
            {
                sb.AppendLine(s);
            }
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}