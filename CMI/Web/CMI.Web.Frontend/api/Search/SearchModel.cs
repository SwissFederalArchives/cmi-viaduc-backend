using System.Collections.Generic;

namespace CMI.Web.Frontend.api.Search
{
    public class SearchModel
    {
        public SearchModel()
        {
            GroupOperator = GroupOperator.And;
            SearchGroups = new List<SearchGroup>();
        }

        public List<SearchGroup> SearchGroups { get; set; }
        public GroupOperator GroupOperator { get; set; }
    }

    public class SearchGroup
    {
        public SearchGroup()
        {
            SearchFields = new List<SearchField>();
            FieldOperator = FieldOperator.And;
        }

        public List<SearchField> SearchFields { get; set; }
        public FieldOperator FieldOperator { get; set; }
    }

    public class SearchField
    {
        public string Value { get; set; }
        public string Key { get; set; }
    }

    public enum FieldOperator
    {
        And = 1,
        Or = 2,
        Not = 3
    }

    public enum GroupOperator
    {
        And = 1,
        Or = 2
    }
}