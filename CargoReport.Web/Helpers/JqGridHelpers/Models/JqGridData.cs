using System.Collections.Generic;

namespace CargoReport.Web.Helpers.JqGridHelpers.Models
{
    public class JqGridData<T>
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int Records { get; set; }
        public IList<JqGridRowData<T>> Rows { get; set; }
        public object UserData { get; set; }
    }

    public class JqGridRowData<T>
    {
        public T Id { set; get; }
        public IList<object> RowCells { set; get; }
    }
}