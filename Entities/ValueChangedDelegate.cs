namespace ModifiableVariable.Entities
{
    /// <summary>Invoked with the new computed value when a modifiable changes.</summary>
    public delegate T ValueChangedDelegate<T>(T changedValue);
}