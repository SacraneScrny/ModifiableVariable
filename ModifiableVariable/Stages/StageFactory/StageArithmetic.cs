using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ModifiableVariable.Stages.StageFactory
{
    public static class StageArithmetic<T>
    {
        static readonly Dictionary<StageOpKind, StageOp<T>> _ops = new();

        static StageArithmetic()
        {
            TryCompile(StageOpKind.Add, Expression.Add);
            TryCompile(StageOpKind.Subtract, Expression.Subtract);
            TryCompile(StageOpKind.Multiply, Expression.Multiply);
            TryCompile(StageOpKind.Divide, Expression.Divide);

            Register(StageOpKind.Override, (a, b) => b);
        }

        public static StageOp<T> Get(StageOpKind kind)
        {
            return _ops.GetValueOrDefault(kind);
        }

        public static void Register(StageOpKind kind, StageOp<T> op)
            => _ops[kind] = op;

        static void TryCompile(StageOpKind kind, Func<Expression, Expression, BinaryExpression> expr)
        {
            try
            {
                var a = Expression.Parameter(typeof(T), "a");
                var b = Expression.Parameter(typeof(T), "b");
                var op = Expression.Lambda<StageOp<T>>(expr(a, b), a, b).Compile();
                _ops[kind] = op;
            }
            catch { }
        }
    }
}