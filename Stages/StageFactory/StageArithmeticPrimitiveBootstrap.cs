using System;

using UnityEngine;

namespace ModifiableVariable.Stages.StageFactory
{
    /// <summary>
    /// Registers numeric primitives with explicit delegates so they never depend on
    /// runtime expression compilation, keeping them functional on AOT/IL2CPP targets
    /// such as WebGL.
    /// </summary>
    public static class StageArithmeticPrimitiveBootstrap
    {
        /// <summary>Registers the built-in numeric primitive operations.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            StageArithmetic<int>.Register(StageOpKind.Add, (a, b) => a + b);
            StageArithmetic<int>.Register(StageOpKind.Subtract, (a, b) => a - b);
            StageArithmetic<int>.Register(StageOpKind.Multiply, (a, b) => a * b);
            StageArithmetic<int>.Register(StageOpKind.Divide, (a, b) => a / b);
            StageArithmetic<int>.Register(StageOpKind.Min, Math.Min);
            StageArithmetic<int>.Register(StageOpKind.Max, Math.Max);
            StageArithmetic<int>.Register(StageOpKind.Override, (a, b) => b);

            StageArithmetic<uint>.Register(StageOpKind.Add, (a, b) => a + b);
            StageArithmetic<uint>.Register(StageOpKind.Subtract, (a, b) => a - b);
            StageArithmetic<uint>.Register(StageOpKind.Multiply, (a, b) => a * b);
            StageArithmetic<uint>.Register(StageOpKind.Divide, (a, b) => a / b);
            StageArithmetic<uint>.Register(StageOpKind.Min, Math.Min);
            StageArithmetic<uint>.Register(StageOpKind.Max, Math.Max);
            StageArithmetic<uint>.Register(StageOpKind.Override, (a, b) => b);

            StageArithmetic<long>.Register(StageOpKind.Add, (a, b) => a + b);
            StageArithmetic<long>.Register(StageOpKind.Subtract, (a, b) => a - b);
            StageArithmetic<long>.Register(StageOpKind.Multiply, (a, b) => a * b);
            StageArithmetic<long>.Register(StageOpKind.Divide, (a, b) => a / b);
            StageArithmetic<long>.Register(StageOpKind.Min, Math.Min);
            StageArithmetic<long>.Register(StageOpKind.Max, Math.Max);
            StageArithmetic<long>.Register(StageOpKind.Override, (a, b) => b);

            StageArithmetic<float>.Register(StageOpKind.Add, (a, b) => a + b);
            StageArithmetic<float>.Register(StageOpKind.Subtract, (a, b) => a - b);
            StageArithmetic<float>.Register(StageOpKind.Multiply, (a, b) => a * b);
            StageArithmetic<float>.Register(StageOpKind.Divide, (a, b) => a / b);
            StageArithmetic<float>.Register(StageOpKind.Min, Mathf.Min);
            StageArithmetic<float>.Register(StageOpKind.Max, Mathf.Max);
            StageArithmetic<float>.Register(StageOpKind.Lerp, (a, b) => Mathf.Lerp(a, b, 0.5f));
            StageArithmetic<float>.Register(StageOpKind.Override, (a, b) => b);

            StageArithmetic<double>.Register(StageOpKind.Add, (a, b) => a + b);
            StageArithmetic<double>.Register(StageOpKind.Subtract, (a, b) => a - b);
            StageArithmetic<double>.Register(StageOpKind.Multiply, (a, b) => a * b);
            StageArithmetic<double>.Register(StageOpKind.Divide, (a, b) => a / b);
            StageArithmetic<double>.Register(StageOpKind.Min, Math.Min);
            StageArithmetic<double>.Register(StageOpKind.Max, Math.Max);
            StageArithmetic<double>.Register(StageOpKind.Lerp, (a, b) => a + (b - a) * 0.5d);
            StageArithmetic<double>.Register(StageOpKind.Override, (a, b) => b);
        }
    }
}
