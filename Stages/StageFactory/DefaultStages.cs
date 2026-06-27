namespace ModifiableVariable.Stages.StageFactory
{
    /// <summary>Disjunction (OR) gate with an override stage.</summary>
    public enum GateDisjunction
    {
        [StageOp(StageOpKind.Or)] DisjunctionState,
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Conjunction (AND) gate with an override stage.</summary>
    public enum GateConjunction
    {
        [StageOp(StageOpKind.And)] ConjunctionState,
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Combined OR/AND gate with an override stage.</summary>
    public enum GateGeneral
    {
        [StageOp(StageOpKind.Or)] DisjunctionState,
        [StageOp(StageOpKind.And)] ConjunctionState,
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Multi-step OR/AND/OR gate with an override stage.</summary>
    public enum GateComplex
    {
        [StageOp(StageOpKind.Or)] DisjunctionState,
        [StageOp(StageOpKind.And)] ConjunctionState,
        [StageOp(StageOpKind.Or)] LastDisjunctionState,
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Single additive stage.</summary>
    public enum Simple
    {
        [StageOp(StageOpKind.Add)] Flat,
    }

    /// <summary>Additive then multiplicative stages.</summary>
    public enum General
    {
        [StageOp(StageOpKind.Add)] Flat,
        [StageOp(StageOpKind.Multiply)] Multiply,
    }

    /// <summary>Additive, multiplicative and post-additive stages.</summary>
    public enum Complex
    {
        [StageOp(StageOpKind.Add)] Flat,
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Add)] Post,
    }

    /// <summary>Damage pipeline: flat, multiply, penetration and cap stages.</summary>
    public enum Damage
    {
        [StageOp(StageOpKind.Add)] Flat,
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Subtract)] Penetration,
        [StageOp(StageOpKind.Min)] Cap,
    }

    /// <summary>Defense pipeline: flat, multiply and cap stages.</summary>
    public enum Defense
    {
        [StageOp(StageOpKind.Add)] Flat,
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Min)] Cap,
    }

    /// <summary>Speed pipeline: flat, multiply and clamp stages.</summary>
    public enum Speed
    {
        [StageOp(StageOpKind.Add)] Flat,
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Max)] Min,
        [StageOp(StageOpKind.Min)] Max,
    }

    /// <summary>Resource pipeline: flat, multiply, regen and cap stages.</summary>
    public enum Resource
    {
        [StageOp(StageOpKind.Add)] Flat,
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Add)] Regen,
        [StageOp(StageOpKind.Min)] Cap,
    }

    /// <summary>Chance pipeline: flat, multiply and cap stages.</summary>
    public enum Chance
    {
        [StageOp(StageOpKind.Add)] Flat,
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Min)] Cap,
    }

    /// <summary>Cooldown pipeline: reduction, multiply and floor stages.</summary>
    public enum Cooldown
    {
        [StageOp(StageOpKind.Subtract)] Reduction,
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Max)] Floor,
    }

    /// <summary>Color pipeline: tint, overlay and override stages.</summary>
    public enum ColorModificator
    {
        [StageOp(StageOpKind.Multiply)] Tint,
        [StageOp(StageOpKind.Add)] Overlay,
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Position pipeline: offset, scale and override stages.</summary>
    public enum Position
    {
        [StageOp(StageOpKind.Add)] Offset,
        [StageOp(StageOpKind.Multiply)] Scale,
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Rotation pipeline: multiply and override stages.</summary>
    public enum Rotation
    {
        [StageOp(StageOpKind.Multiply)] Multiply,
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Single override stage.</summary>
    public enum Overridable
    {
        [StageOp(StageOpKind.Override)] Override,
    }

    /// <summary>Blend pipeline: offset, lerp and override stages.</summary>
    public enum Blend
    {
        [StageOp(StageOpKind.Add)] Offset,
        [StageOp(StageOpKind.Lerp)] Lerp,
        [StageOp(StageOpKind.Override)] Override,
    }
}
