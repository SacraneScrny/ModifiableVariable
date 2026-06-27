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
        /// <summary>The binary operation applied between the running value and each modifier.</summary>
        public readonly StageOp<T> Op;
        readonly List<ModifierDelegate<T>> _modifiers = new();

        /// <summary>The modifiers contributing to this stage.</summary>
        public IReadOnlyList<ModifierDelegate<T>> Modifiers => _modifiers;

        /// <summary>Creates a stage that applies the given binary operation.</summary>
        public Stage(StageOp<T> op)
        {
            Op = op;
        }

        /// <summary>Adds a value-producing modifier and returns a disposable handle.</summary>
        public ModifierDelegateHandler<T> Add(Func<T> modifier)
        {
            var d = new ModifierDelegate<T>(modifier);
            _modifiers.Add(d);
            return new ModifierDelegateHandler<T>(d, Remove);
        }

        /// <summary>Adds a modifier delegate and returns a disposable handle.</summary>
        public ModifierDelegateHandler<T> Add(ModifierDelegate<T> modifier)
        {
            _modifiers.Add(modifier);
            return new ModifierDelegateHandler<T>(modifier, Remove);
        }

        /// <summary>Removes a modifier from this stage.</summary>
        public bool Remove(ModifierDelegate<T> modifier)
        {
            var ret = _modifiers.Remove(modifier);
            return ret;
        }

        /// <summary>Removes the modifier referenced by a handle from this stage.</summary>
        public bool Remove(ModifierDelegateHandler<T> handler)
        {
            return Remove(handler.Modifier);
        }

        /// <summary>Folds every modifier into <paramref name="baseValue"/> using the stage operation.</summary>
        public T Proceed(T baseValue)
        {
            var value = baseValue;
            for (var i = 0; i < _modifiers.Count; i++)
                value = Op(value, _modifiers[i]());

            return value;
        }

        /// <summary>Removes all modifiers from this stage.</summary>
        public void Clear()
        {
            _modifiers.Clear();
        }

        /// <summary>Clears the stage.</summary>
        public void Dispose() => Clear();
    }
}
