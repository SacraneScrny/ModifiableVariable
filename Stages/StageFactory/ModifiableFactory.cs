using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace ModifiableVariable.Stages.StageFactory
{
    /// <summary>Builds stages for a modifiable from the <see cref="StageOpAttribute"/> markers on its stage enum.</summary>
    public static class ModifiableFactory
    {
        static readonly Dictionary<Type, StageOpKind[]> _cache = new();

        /// <summary>
        /// Adds a stage to <paramref name="modifiable"/> for every <typeparamref name="TStage"/>
        /// member that carries a <see cref="StageOpAttribute"/> with an available operation.
        /// </summary>
        public static bool TryPopulate<T, TStage>(Modifiable<T, TStage> modifiable)
            where TStage : Enum
        {
            var values = (TStage[])Enum.GetValues(typeof(TStage));
            var ops = TryGetOps<TStage>();
            if (ops == null)
            {
                Debug.LogWarning($"No {nameof(StageOpAttribute)} found on any members of {typeof(TStage).Name}. Stages will not be created.");
                return false;
            }

            bool hasAny = false;
            for (var i = 0; i < values.Length; i++)
            {
                var s = StageArithmetic<T>.Get(ops[i]);
                if (s == null)
                {
                    Debug.LogWarning($"No {ops[i]} operation available for type {typeof(T).Name}. Stage {values[i]}.");
                    continue;
                }
                hasAny = true;
                modifiable.AddStage(s, values[i]);
            }

            if (!hasAny)
                Debug.LogWarning($"No {nameof(StageOpAttribute)} found on any members of {typeof(TStage).Name}. Stages is empty.");
            return hasAny;
        }

        static StageOpKind[] TryGetOps<TStage>() where TStage : Enum
        {
            var type = typeof(TStage);
            if (_cache.TryGetValue(type, out var cached)) return cached;

            var values = Enum.GetValues(type);
            var ops = new StageOpKind[values.Length];
            var hasAny = false;

            for (var i = 0; i < values.Length; i++)
            {
                var name = Enum.GetName(type, values.GetValue(i));
                var attr = type.GetField(name).GetCustomAttribute<StageOpAttribute>();
                if (attr != null) hasAny = true;
                ops[i] = attr?.Kind ?? StageOpKind.Add;
            }

            if (!hasAny) return null;

            _cache[type] = ops;
            return ops;
        }
    }
    
    /// <summary>Identifies the binary operation a stage applies between values.</summary>
    public enum StageOpKind
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Min,
        Max,
        Override,
        Lerp,
        Scale,
        
        Or,
        And
    }
}