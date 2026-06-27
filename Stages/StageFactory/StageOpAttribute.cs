using System;

namespace ModifiableVariable.Stages.StageFactory
{
    /// <summary>Marks an enum member with the arithmetic operation its stage should use.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class StageOpAttribute : Attribute
    {
        /// <summary>The arithmetic operation associated with the marked stage.</summary>
        public readonly StageOpKind Kind;

        /// <summary>Associates the given operation with the marked enum member.</summary>
        public StageOpAttribute(StageOpKind kind) => Kind = kind;
    }
}