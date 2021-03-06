﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CargoReport.Web.Helpers.JqGridHelpers.Utils
{
    public class StronglyTyped : ExpressionVisitor
    {
        private Stack<string> _stack;

        public static string PropertyName<TEntity>(Expression<Func<TEntity, object>> expression)
        {
            return new StronglyTyped().Name(expression);
        }

        private string Path(Expression expression)
        {
            _stack = new Stack<string>();
            Visit(expression);
            return _stack.Aggregate((s1, s2) => s1 + "." + s2);
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            if (_stack != null)
                _stack.Push(expression.Member.Name);
            return base.VisitMember(expression);
        }

        private string Name<TEntity>(Expression<Func<TEntity, object>> expression)
        {
            return Path(expression);
        }
    }
}