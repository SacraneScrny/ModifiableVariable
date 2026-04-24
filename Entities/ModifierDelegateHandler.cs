using System;

namespace ModifiableVariable.Entities
{
    public readonly struct ModifierDelegateHandler<T> : IDisposable
    {
        public readonly ModifierDelegate<T> Modifier;
        readonly Func<ModifierDelegate<T>, bool> _disposeDelegate;

        public ModifierDelegateHandler(
            ModifierDelegate<T> modifier, 
            Func<ModifierDelegate<T>, bool> disposeDelegate)
        {
            Modifier = modifier;
            _disposeDelegate = disposeDelegate;
        }
        
        public void Dispose()
        {
            try { 
                _disposeDelegate?.Invoke(Modifier);
            }
            catch
            {
                // ignored
            }
        }
    }
}