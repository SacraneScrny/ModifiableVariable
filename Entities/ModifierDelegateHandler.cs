using System;

using UnityEngine;

namespace ModifiableVariable.Entities
{
    /// <summary>Disposable handle that removes its modifier from the owning stage when disposed.</summary>
    public readonly struct ModifierDelegateHandler<T> : IDisposable
    {
        /// <summary>The modifier this handle refers to.</summary>
        public readonly ModifierDelegate<T> Modifier;
        readonly Func<ModifierDelegate<T>, bool> _disposeDelegate;

        /// <summary>Creates a handle wrapping a modifier and its removal callback.</summary>
        public ModifierDelegateHandler(
            ModifierDelegate<T> modifier,
            Func<ModifierDelegate<T>, bool> disposeDelegate)
        {
            Modifier = modifier;
            _disposeDelegate = disposeDelegate;
        }

        /// <summary>Removes the referenced modifier from its stage.</summary>
        public void Dispose()
        {
            try 
            {
                _disposeDelegate?.Invoke(Modifier);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
