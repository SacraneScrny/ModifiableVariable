namespace ModifiableVariable.Stages
{
    /// <summary>A binary operation combining a running value with a modifier value.</summary>
    public delegate T StageOp<T>(T a, T b);
}
