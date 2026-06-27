using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using UnityEngine;

namespace ModifiableVariable.Stages.StageFactory
{
    /// <summary>
    /// Registry of binary operations per <see cref="StageOpKind"/> for a given type
    /// <typeparamref name="T"/>. Built-in types are registered explicitly by the bootstrap
    /// classes; unregistered types fall back to runtime expression compilation.
    /// </summary>
    public static class StageArithmetic<T>
    {
        static readonly Dictionary<StageOpKind, StageOp<T>> _ops = new();
        readonly static HashSet<StageOpKind> _fallbackTried = new();

        static StageArithmetic()
        {
            Register(StageOpKind.Override, (a, b) => b);
        }

        /// <summary>Returns the operation for the given kind, or null if none is available.</summary>
        public static StageOp<T> Get(StageOpKind kind)
        {
            if (_ops.TryGetValue(kind, out var op))
                return op;

            return CompileFallback(kind);
        }

        /// <summary>Registers an explicit operation for the given kind, replacing any existing one.</summary>
        public static void Register(StageOpKind kind, StageOp<T> op)
            => _ops[kind] = op;

        static StageOp<T> CompileFallback(StageOpKind kind)
        {
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
