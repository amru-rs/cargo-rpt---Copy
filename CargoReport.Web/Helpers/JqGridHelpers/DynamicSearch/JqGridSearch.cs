using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using CargoReport.Web.Helpers.JqGridHelpers.Models;
using CargoReport.Web.Helpers.JqGridHelpers.Utils;
using Newtonsoft.Json;
using System.Linq.Dynamic;

namespace CargoReport.Web.Helpers.JqGridHelpers.DynamicSearch
{
    public class JqGridSearch
    {
        private static readonly Dictionary<string, string> WhereOperation =
            new Dictionary<string, string>
            {
                {"in", " {0} = @{1} "}, //is in
                {"eq", " {0} = @{1} "},
                {"ni", " {0} != @{1} "}, //is not in
                {"ne", " {0} != @{1} "},
                {"lt", " {0} < @{1} "},
                {"le", " {0} <= @{1} "},
                {"gt", " {0} > @{1} "},
                {"ge", " {0} >= @{1} "},
                {"bw", " {0}.StartsWith(@{1}) "}, //begins with
                {"bn", " !{0}.StartsWith(@{1}) "}, //does not begin with
                {"ew", " {0}.EndsWith(@{1}) "}, //ends with
                {"en", " !{0}.EndsWith(@{1}) "}, //does not end with
                {"cn", " {0}.Contains(@{1}) "}, //contains
                {"nc", " !{0}.Contains(@{1}) "} //does not contain
            };

        private static readonly Dictionary<string, string> ValidOperators =
            new Dictionary<string, string>
            {
                {"Object", ""},
                {"Boolean", "eq:ne:"},
                {"Char", ""},
                {"String", "eq:ne:lt:le:gt:ge:bw:bn:cn:nc:"},
                {"SByte", ""},
                {"Byte", "eq:ne:lt:le:gt:ge:"},
                {"Int16", "eq:ne:lt:le:gt:ge:"},
                {"UInt16", ""},
                {"Int32", "eq:ne:lt:le:gt:ge:"},
                {"UInt32", ""},
                {"Int64", "eq:ne:lt:le:gt:ge:"},
                {"UInt64", ""},
                {"Decimal", "eq:ne:lt:le:gt:ge:"},
                {"Single", "eq:ne:lt:le:gt:ge:"},
                {"Double", "eq:ne:lt:le:gt:ge:"},
                {"DateTime", "eq:ne:lt:le:gt:ge:"},
                {"TimeSpan", ""},
                {"Guid", ""}
            };

        private readonly DateTimeType _dateTimeType;
        private readonly NameValueCollection _form;
        private readonly JqGridRequest _request;
        private int _parameterIndex;

        public JqGridSearch(JqGridRequest request, NameValueCollection form, DateTimeType dateTimeType)
        {
            _request = request;
            _form = form;
            _dateTimeType = dateTimeType;
        }

        public IQueryable<T> ApplyFilter<T>(IQueryable<T> query)
        {
            if (!_request._search)
                return query;

            if (!string.IsNullOrWhiteSpace(_request.searchString) &&
                !string.IsNullOrWhiteSpace(_request.searchOper) &&
                !string.IsNullOrWhiteSpace(_request.searchField))
            {
                return ManageSingleFieldSearch(query);
            }

            return !string.IsNullOrWhiteSpace(_request.filters)
                ? ManageMultiFieldSearch(query)
                : ManageToolbarSearch(query);
        }

        private Tuple<string, object> GetPredicate<T>(string searchField, string searchOper, string searchValue)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                return null;

            var type = typeof (T).FindFieldType(searchField);
            if (type == null)
                throw new InvalidOperationException(searchField + " is not defined.");

            if (!ValidOperators[type.Name].Contains(searchOper + ":"))
            {
                // این اپراتور روی نوع داده‌ای جاری کار نمی‌کند  
                return null;
            }

            if (type == typeof (decimal))
            {
                decimal value;
                if (decimal.TryParse(searchValue, NumberStyles.Any, Thread.CurrentThread.CurrentCulture, out value))
                {
                    return new Tuple<string, object>(GetSearchOperator(searchOper, searchField, type), value);
                }
            }

            if (type == typeof (DateTime))
            {
                DateTime dateTime;
                switch (_dateTimeType)
                {
                    case DateTimeType.Gregorian:
                        dateTime = DateTime.Parse(searchValue);
                        break;
                    case DateTimeType.Persian:
                        var parts = searchValue.Split('/'); //ex. 1391/1/19
                        if (parts.Length != 3) return null;
                        var year = int.Parse(parts[0]);
                        var month = int.Parse(parts[1]);
                        var day = int.Parse(parts[2]);
                        dateTime = new DateTime(year, month, day, new PersianCalendar());
                        break;
                    default:
                        throw new NotSupportedException(_dateTimeType + " is not supported.");
                }
                return new Tuple<string, object>(GetSearchOperator(searchOper, searchField, type), dateTime);
            }

            var resultValue = Convert.ChangeType(searchValue, type);
            return new Tuple<string, object>(GetSearchOperator(searchOper, searchField, type), resultValue);
        }

        private string GetSearchOperator(string ruleSearchOperator, string searchField, Type type)
        {
            string whereOperation;
            if (!WhereOperation.TryGetValue(ruleSearchOperator, out whereOperation))
            {
                throw new NotSupportedException(string.Format("{0} is not supported.", ruleSearchOperator));
            }

            if (type == typeof (DateTime))
            {
                switch (ruleSearchOperator)
                {
                    case "eq":
                        whereOperation = " {0}.Date = @{1} ";
                        break;
                    case "ne":
                        whereOperation = " {0}.Date != @{1} ";
                        break;
                }
            }

            var searchOperator = string.Format(whereOperation, searchField, _parameterIndex);

            _parameterIndex++;
            return searchOperator;
        }

        private IQueryable<T> ManageMultiFieldSearch<T>(IQueryable<T> query)
        {
            var filtersArray = JsonConvert.DeserializeObject<SearchFilter>(_request.filters);
            var groupOperator = filtersArray.GroupOp;
            if (filtersArray.Rules == null)
                return query;

            var valuesList = new List<object>();
            var filterExpression = string.Empty;
            foreach (var rule in filtersArray.Rules)
            {
                var predicate = GetPredicate<T>(rule.Field, rule.Op, rule.Data);
                if (predicate == null)
                    continue;

                valuesList.Add(predicate.Item2);
                filterExpression = filterExpression + predicate.Item1 + " " + groupOperator + " ";
            }

            if (string.IsNullOrWhiteSpace(filterExpression))
                return query;

            filterExpression = filterExpression.Remove(filterExpression.Length - groupOperator.Length - 2);
            query = query.Where(filterExpression, valuesList.ToArray());
            return query;
        }

        private IQueryable<T> ManageSingleFieldSearch<T>(IQueryable<T> query)
        {
            var predicate = GetPredicate<T>(_request.searchField, _request.searchOper, _request.searchString);
            if (predicate != null)
                query = query.Where(predicate.Item1, predicate.Item2);
            return query;
        }

        private IQueryable<T> ManageToolbarSearch<T>(IQueryable<T> query)
        {
            var filterExpression = string.Empty;
            var valuesList = new List<object>();
            foreach (var predicate in from postDataKey in _form.AllKeys
                where !postDataKey.Equals("nd") && !postDataKey.Equals("sidx")
                      && !postDataKey.Equals("sord") && !postDataKey.Equals("page")
                      && !postDataKey.Equals("rows") && !postDataKey.Equals("_search")
                      && _form[postDataKey] != null
                select GetPredicate<T>(postDataKey, "eq", _form[postDataKey])
                into predicate
                where predicate != null
                select predicate)
            {
                valuesList.Add(predicate.Item2);
                filterExpression = filterExpression + predicate.Item1 + " And ";
            }

            if (string.IsNullOrWhiteSpace(filterExpression))
                return query;

            filterExpression = filterExpression.Remove(filterExpression.Length - 5);
            query = query.Where(filterExpression, valuesList.ToArray());
            return query;
        }
    }
}