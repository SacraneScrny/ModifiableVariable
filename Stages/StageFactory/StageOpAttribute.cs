using System;

namespace ModifiableVariable.Stages.StageFactory
{
    [AttributeUsage(AttributeTargets.Field)]
    public class StageOpAttribute : Attribute
    {
        public readonly StageOpKind Kind;
        public StageOpAttribute(StageOpKind kind) => Kind = kind;
    }
}