namespace CMI.Contract.Common
{
    public class DataSourceRequest
    {
        /// <summary>
        ///     Gets or sets the size of the page.
        /// </summary>
        /// <value>The size of the page.</value>
        public int PageSize { get; set; }

        /// <summary>
        ///     Gets or sets the current page.
        /// </summary>
        /// <value>The current page.</value>
        public int Page { get; set; }

        /// <summary>
        ///     Gets or sets the sort field.
        /// </summary>
        public string SortField { get; set; }

        /// <summary>
        ///     Gets or sets the sort order. Can be either ASC or DESC
        /// </summary>
        public string SortOrder { get; set; }
    }
}