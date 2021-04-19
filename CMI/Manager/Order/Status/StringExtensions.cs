namespace CMI.Manager.Order.Status
{
    public static class StringExtensions
    {
        public static string Append(this string s, string suffix)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return suffix;
            }

            if (string.IsNullOrWhiteSpace(suffix))
            {
                return s;
            }

            return $"{s}\r\n{suffix}";
        }

        public static string Prepend(this string s, string prefix)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return prefix;
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                return s;
            }

            return $"{prefix}\r\n{s}";
        }
    }
}