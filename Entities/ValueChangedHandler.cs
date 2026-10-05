using System;

using UnityEngine;

namespace ModifiableVariable.Entities
{
    /// <summary>Disposable handle that unsubscribes its value-changed callback when disposed.</summary>
    public readonly struct ValueChangedHandler<T> : IDisposable
    {
        readonly ValueChangedDelegate<T> _action;
        readonly Func<ValueChangedDelegate<T>, bool> _disposeDelegate;

        /// <summary>Creates a handle wrapping a callback and its removal callback.</summary>
        public ValueChangedHandler(
            ValueChangedDelegate<T> action,
            Func<ValueChangedDelegate<T>, bool> disposeDelegate)
        {
            _action = action;
            _disposeDelegate = disposeDelegate;
        }

        /// <summary>Unsubscribes the referenced callback.</summary>
        public void Dispose()
        {
            try
            {
                _disposeDelegate?.Invoke(_action);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
