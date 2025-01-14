﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace System
{
    /// <summary>
    /// Expression表达树扩展类
    /// </summary>
    public static class ExpressionExtension
    {
        /// <summary>
        /// 判断最子级是否为常量表达式
        /// </summary>
        /// <param name="exp">需要判断的表达式</param>
        /// <returns>true表示是</returns>
        public static bool IsConstantExpression(this Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
            {
                return true;
            }
            bool flage = false;
            var checkExp = exp as MemberExpression;
            while (checkExp != null)
            {
                if (checkExp.Expression == null)
                {
                    if (checkExp.NodeType == ExpressionType.Constant)
                    {
                        flage = true;
                    }
                    checkExp = checkExp.Expression as MemberExpression;
                }
                else
                {
                    flage = checkExp.Expression.NodeType == ExpressionType.Constant;
                    checkExp = checkExp.Expression as MemberExpression;
                }
            }
            return flage;
        }

        /// <summary>
        /// 判断最子级是否为参数或者变量表达式
        /// </summary>
        /// <param name="exp">需要判断的表达式</param>
        /// <returns>true表示不是</returns>
        public static bool IsNotParameterExpression(this Expression exp)
        {
            if (exp.NodeType == ExpressionType.Parameter)
            {
                return false;
            }
            bool flage = true;
            var checkExp = exp as MemberExpression;
            while (checkExp != null && checkExp.Expression != null)
            {
                if (checkExp.Expression.NodeType == ExpressionType.Parameter)
                {
                    flage = false;
                    break;
                }
                checkExp = checkExp.Expression as MemberExpression;
            }
            return flage;
        }

        /// <summary>
        /// 将表达式通过调用方法转为常量表达式
        /// </summary>
        /// <param name="exp">要转换的表示</param>
        /// <returns></returns>
        public static ConstantExpression ToConstantExpression(this Expression exp)
        {
            var memberValue = Expression.Lambda(exp).Compile().DynamicInvoke();
            return Expression.Constant(memberValue);
        }

        /// <summary>
        /// 以特定的条件运行组合两个Expression表达式
        /// </summary>
        /// <typeparam name="T">表达式的主实体类型</typeparam>
        /// <param name="first">第一个Expression表达式</param>
        /// <param name="second">要组合的Expression表达式</param>
        /// <param name="merge">组合条件运算方式</param>
        /// <returns>组合后的表达式</returns>
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            Dictionary<ParameterExpression, ParameterExpression> map =
                first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
            Expression secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        /// <summary>
        /// 以 Expression.AndAlso 组合两个Expression表达式
        /// </summary>
        /// <typeparam name="T">表达式的主实体类型</typeparam>
        /// <param name="first">第一个Expression表达式</param>
        /// <param name="second">要组合的Expression表达式</param>
        /// <returns>组合后的表达式</returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        /// <summary>
        /// 以 Expression.OrElse 组合两个Expression表达式
        /// </summary>
        /// <typeparam name="T">表达式的主实体类型</typeparam>
        /// <param name="first">第一个Expression表达式</param>
        /// <param name="second">要组合的Expression表达式</param>
        /// <returns>组合后的表达式</returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        private class ParameterRebinder : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

            private ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                _map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                ParameterExpression replacement;
                if (_map.TryGetValue(node, out replacement))
                {
                    node = replacement;
                }
                return base.VisitParameter(node);
            }
        }
    }
}