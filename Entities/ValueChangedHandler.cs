using System;

namespace ModifiableVariable.Entities
{
    public readonly struct ValueChangedHandler<T> : IDisposable
    {
        readonly ValueChangedDelegate<T> _action;
        readonly Func<ValueChangedDelegate<T>, bool> _disposeDelegate;

        public ValueChangedHandler(
            ValueChangedDelegate<T> action, 
            Func<ValueChangedDelegate<T>, bool> disposeDelegate)
        {
            _action = action;
            _disposeDelegate = disposeDelegate;
        }

        public void Dispose()
        {
            try { _disposeDelegate?.Invoke(_action); }
            catch { }
        }
    }
}