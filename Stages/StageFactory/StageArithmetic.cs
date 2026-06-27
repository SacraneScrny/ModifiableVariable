using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using UnityEngine;

namespace ModifiableVariable.Stages.StageFactory
{
    public static class StageArithmetic<T>
    {
        static readonly Dictionary<StageOpKind, StageOp<T>> _ops = new();
        readonly static HashSet<StageOpKind> _fallbackTried = new();

        static StageArithmetic()
        {
            Register(StageOpKind.Override, (a, b) => b);
        }

        public static StageOp<T> Get(StageOpKind kind)
        {
            if (_ops.TryGetValue(kind, out var op))
                return op;

            return CompileFallback(kind);
        }

        public static void Register(StageOpKind kind, StageOp<T> op)
            => _ops[kind] = op;

        // Fallback for types that were not registered explicitly (e.g. custom numeric
        // structs). It relies on runtime expression compilation, which needs a JIT and is
        // therefore NOT available on AOT/IL2CPP targets such as WebGL. Every built-in type
        // (primitives + Unity structs) is registered explicitly in the bootstrap classes,
        // so it never reaches this path and stays WebGL-safe.
        static StageOp<T> CompileFallback(StageOpKind kind)
        {
            // Attempt compilation at most once per (type, kind); cache success and failure.
            if (!_fallbackTried.Add(kind))
                return _ops.GetValueOrDefault(kind);

            var factory = GetExpressionFactory(kind);
            if (factory == null)
                return null;

            try
            {
                var a = Expression.Parameter(typeof(T), "a");
                var b = Expression.Parameter(typeof(T), "b");
                var op = Expression.Lambda<StageOp<T>>(factory(a, b), a, b).Compile();
                _ops[kind] = op;
                return op;
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[ModifiableVariable] No '{kind}' operation available for type '{typeof(T).Name}'. " +
                    "Runtime compilation failed (operator not defined, or unsupported on AOT/IL2CPP/WebGL): " +
                    $"{e.Message}. Register it explicitly via StageArithmetic<{typeof(T).Name}>.Register(...).");
                return null;
            }
        }

        static Func<Expression, Expression, BinaryExpression> GetExpressionFactory(StageOpKind kind)
            => kind switch
            {
                StageOpKind.Add => Expression.Add,
                StageOpKind.Subtract => Expression.Subtract,
                StageOpKind.Multiply => Expression.Multiply,
                StageOpKind.Divide => Expression.Divide,
                _ => null
            };
    }
}
