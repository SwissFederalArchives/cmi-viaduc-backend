using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend.api.Entities
{
    public class EntityMetaOptions
    {
        #region Standard options

        public static readonly EntityMetaOptions DefaultOptions = new EntityMetaOptions
        {
            FetchAncestors = false,
            FetchChildren = false,

            SkipCleanup = false,

            Language = WebHelper.DefaultLanguage,
            SetDepth = 0
        };

        #endregion

        public int SetDepth { get; set; }

        public bool FetchAncestors { get; set; }
        public bool FetchChildren { get; set; }

        public bool SkipCleanup { get; set; }
        public Paging ChildrenPaging { get; set; }

        public string Language { get; set; }
    }
}