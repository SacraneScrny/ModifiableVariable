using System;
using System.Collections.Generic;

using ModifiableVariable.Entities;

namespace ModifiableVariable.Stages
{
    /// <summary>
    /// A single processing step that folds its modifiers into an incoming value using a
    /// binary <see cref="StageOp{T}"/>.
    /// </summary>
    public class Stage<T> : IDisposable
    {
        bool _disposed;
        public bool IsDisposed => _disposed;
        
        /// <summary>The binary operation applied between the running value and each modifier.</summary>
        public readonly StageOp<T> Op;
        readonly List<ModifierDelegate<T>> _modifiers = new();

        /// <summary>The modifiers contributing to this stage.</summary>
        public IReadOnlyList<ModifierDelegate<T>> Modifiers
        {
            get 
            { 
                if (_disposed) throw new ObjectDisposedException(nameof(Stage<T>));
                return _modifiers; 
            }
        }

        /// <summary>Creates a stage that applies the given binary operation.</summary>
        public Stage(StageOp<T> op)
        {
            Op = op;
        }

        internal ModifierDelegateHandler<T> Add(Func<T> modifier)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Stage<T>));
            var d = new ModifierDelegate<T>(modifier);
            _modifiers.Add(d);
            return new ModifierDelegateHandler<T>(d, HandleRemove);
        }

        internal ModifierDelegateHandler<T> Add(ModifierDelegate<T> modifier)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Stage<T>));
            _modifiers.Add(modifier);
            return new ModifierDelegateHandler<T>(modifier, HandleRemove);
        }

        internal bool HandleRemove(ModifierDelegate<T> modifier)
        {
            if (_disposed) return false;
            var status = Remove(modifier);
            if (status) 
                HandleStageRemoved?.Invoke();
            return status;
        }
        internal bool Remove(ModifierDelegate<T> modifier)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Stage<T>));
            var ret = _modifiers.Remove(modifier);
            return ret;
        }
        internal bool Remove(ModifierDelegateHandler<T> handler)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Stage<T>));
            return Remove(handler.Modifier);
        }
        internal T Proceed(T baseValue)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Stage<T>));
            var value = baseValue;
            for (var i = 0; i < _modifiers.Count; i++)
                value = Op(value, _modifiers[i]());

            return value;
        }

        /// <summary>Removes all modifiers from this stage.</summary>
        public void Clear()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Stage<T>));
            _modifiers.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            Clear();
            _disposed = true;
            HandleStageRemoved = null;
        }
        
        internal event Action HandleStageRemoved;
    }
}
