using System.Collections.Generic;

namespace CargoReport.Web.Helpers.JqGridHelpers.Models
{
    public class SearchFilter
    {
        public string GroupOp { set; get; }
        public List<SearchGroup> Groups { set; get; }
        public List<SearchRule> Rules { set; get; }
    }

    public class SearchRule
    {
        public string Field { set; get; }
        public string Op { set; get; }
        public string Data { set; get; }

        public override string ToString()
        {
            return string.Format("'{0}' {1} '{2}'", Field, Op, Data);
        }
    }

    public class SearchGroup
    {
        public string GroupOp { set; get; }
        public List<SearchRule> Rules { set; get; }
    }
}