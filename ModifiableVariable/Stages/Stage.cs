using System;
using System.Collections.Generic;

using ModifiableVariable.Entities;

namespace ModifiableVariable.Stages
{
    public class Stage<T>
    {
        public readonly StageOp<T> Op;
        readonly List<ModifierDelegate<T>> _modifiers = new();
        
        public IReadOnlyList<ModifierDelegate<T>> Modifiers => _modifiers;

        public Stage(StageOp<T> op)
        {
            Op = op;
        }
        
        public ModifierDelegateHandler<T> Add(Func<T> modifier)
        {
            var d = new ModifierDelegate<T>(modifier);
            _modifiers.Add(d);
            return new ModifierDelegateHandler<T>(d, Remove);
        }
        public ModifierDelegateHandler<T> Add(ModifierDelegate<T> modifier)
        {
            _modifiers.Add(modifier);
            return new ModifierDelegateHandler<T>(modifier, Remove);
        }
        public bool Remove(ModifierDelegate<T> modifier)
        {
            var ret = _modifiers.Remove(modifier);
            return ret;
        }
        public bool Remove(ModifierDelegateHandler<T> handler)
        {
            return Remove(handler.Modifier);
        }
        public T Proceed(T baseValue)
        {
            var value = baseValue;
            for (var i = 0; i < _modifiers.Count; i++)
                value = Op(value, _modifiers[i]());

            return value;
        }

        public void Clear()
        {
            _modifiers.Clear();
        }
    }
}