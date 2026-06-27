using System;
using System.Collections.Generic;

using ModifiableVariable.Comparers;
using ModifiableVariable.Entities;
using ModifiableVariable.Stages;
using ModifiableVariable.Stages.StageFactory;

using UnityEngine;

namespace ModifiableVariable
{
    /// <summary>
    /// A value of type <typeparamref name="T"/> that is run through an ordered set of
    /// modifier stages identified by the <typeparamref name="TStage"/> enum. The final
    /// value is computed on demand and cached per frame.
    /// </summary>
    [Serializable]
    public class Modifiable<T, TStage> : IDisposable
        where TStage : Enum
    {
        [SerializeField] T _baseValue;

        /// <summary>The unmodified base value before any stage is applied.</summary>
        public T BaseValue => _baseValue;

        readonly Dictionary<int, Stage<T>> _stageMap = new();
        readonly List<ValueChangedDelegate<T>> _valueChangedCallbacks = new();
        T _cachedValue;
        T _defaultValue;
        bool _hasInited;
        bool _modifiersHasChanged;
        int _cachedCount;
        int _cachedFrame = -1;

        IEqualityComparer<T> _comparer;
        readonly List<Stage<T>> _stages = new();

        /// <summary>The stages applied to the base value, in evaluation order.</summary>
        public IReadOnlyList<Stage<T>> Stages => _stages;

        /// <summary>Creates a modifiable with the given base value and explicit stages.</summary>
        public Modifiable(T baseValue, params (TStage, StageOp<T>)[] stages)
        {
            _comparer = ComparerFactory.Get<T>();
            _baseValue = baseValue;
            for (var i = 0; i < stages.Length; i++)
            {
                AddStage(stages[i]);
            }
        }

        /// <summary>
        /// Creates a modifiable with the given base value, populating stages from the
        /// <see cref="StageOpAttribute"/> markers on the <typeparamref name="TStage"/> enum.
        /// </summary>
        public Modifiable(T baseValue)
        {
            _comparer = ComparerFactory.Get<T>();
            _baseValue = baseValue;
            ModifiableFactory.TryPopulate(this);
        }

        /// <summary>Overrides the equality comparer used for caching and change detection.</summary>
        public Modifiable<T, TStage> WithComparer(IEqualityComparer<T> comparer)
        {
            _comparer = comparer;
            return this;
        }

        /// <summary>Adds a stage with the given operation under the given enum key.</summary>
        public Modifiable<T, TStage> AddStage(StageOp<T> op, TStage stage)
        {
            var s = new Stage<T>(op);
            _stages.Add(s);
            _stageMap[ToInt(stage)] = s;

            _modifiersHasChanged = true;
            return this;
        }

        /// <summary>Adds a stage described by an enum key and operation tuple.</summary>
        public Modifiable<T, TStage> AddStage((TStage, StageOp<T>) stage)
        {
            AddStage(stage.Item2, stage.Item1);
            return this;
        }

        #if UNITY_EDITOR
        /// <summary>Editor-only accessor that computes the current value without caching side effects.</summary>
        public T GetValueEditor()
        {
            var result = Calculate();
            return result;
        }
        #endif

        /// <summary>Gets the computed value (cached per frame) or sets the base value.</summary>
        public T Value
        {
            set
            {
                if (!_comparer.Equals(_baseValue, value))
                {
                    _baseValue = value;
                    _cachedFrame = -1;
                }
            }
            get
            {
                if (!_hasInited)
                {
                    _defaultValue = _baseValue;
                    _hasInited = true;
                }

                UpdateCountIfDirty();
                if (!IsFrameDirty())
                    return _cachedValue;

                var value = Calculate();
                if (!_comparer.Equals(_cachedValue, value))
                {
                    _cachedValue = value;
                    for (var i = 0; i < _valueChangedCallbacks.Count; i++)
                        _valueChangedCallbacks[i](value);
                }

                return value;
            }
        }

        /// <summary>Adds a modifier to the given stage and returns a disposable handle.</summary>
        public ModifierDelegateHandler<T> Add(ModifierDelegate<T> modifier, TStage stage = default)
        {
            _modifiersHasChanged = true;
            _cachedFrame = -1;
            return GetStage(stage)?.Add(modifier) ?? default;
        }

        /// <summary>Removes a modifier from the given stage.</summary>
        public bool Remove(ModifierDelegate<T> modifier, TStage stage)
        {
            _modifiersHasChanged = true;
            _cachedFrame = -1;
            return GetStage(stage)?.Remove(modifier) ?? false;
        }

        /// <summary>Removes the modifier referenced by a handle from the given stage.</summary>
        public bool Remove(ModifierDelegateHandler<T> handler, TStage stage)
        {
            _modifiersHasChanged = true;
            _cachedFrame = -1;
            return Remove(handler.Modifier, stage);
        }

        /// <summary>Removes a modifier from any stage that contains it.</summary>
        public bool Remove(ModifierDelegate<T> modifier)
        {
            _modifiersHasChanged = true;
            _cachedFrame = -1;
            for (var i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].Remove(modifier))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Removes the modifier referenced by a handle from any stage that contains it.</summary>
        public bool Remove(ModifierDelegateHandler<T> modifier)
        {
            _modifiersHasChanged = true;
            _cachedFrame = -1;
            for (var i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].Remove(modifier))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Registers a callback invoked when the computed value changes; returns a disposable handle.</summary>
        public ValueChangedHandler<T> OnValueChanged(ValueChangedDelegate<T> callback)
        {
            _valueChangedCallbacks.Add(callback);
            return new ValueChangedHandler<T>(callback, _valueChangedCallbacks.Remove);
        }

        /// <summary>Removes all modifiers from every stage and restores the original base value.</summary>
        public void Clear()
        {
            _cachedFrame = -1;
            for (int i = 0; i < _stages.Count; i++)
                _stages[i].Clear();
            if (_hasInited)
                _baseValue = _defaultValue;
        }

        T Calculate()
        {
            var value = _baseValue;
            for (var i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                value = stage.Proceed(value);
            }
            return value;
        }
        bool IsFrameDirty()
        {
            var frame = _cachedFrame;
            _cachedFrame = Time.frameCount;
            return frame != _cachedFrame;
        }
        void UpdateCountIfDirty()
        {
            if (_modifiersHasChanged)
            {
                _modifiersHasChanged = false;
                _cachedCount = 0;
                for (var i = 0; i < _stages.Count; i++)
                    _cachedCount += _stages[i].Modifiers.Count;
            }
        }
        Stage<T> GetStage(TStage key) => !_stageMap.TryGetValue(ToInt(key), out var stage) ? _stages[0] : stage;
        static int ToInt(TStage key) => (int)(object)key;
        static TStage ToStage(int key) => (TStage)(object)key;

        /// <summary>Implicitly resolves the modifiable to its computed value.</summary>
        public static implicit operator T (Modifiable<T, TStage> obj)
        {
            return obj.Value;
        }

        /// <summary>Implicitly wraps a raw value in a modifiable with default stages.</summary>
        public static implicit operator Modifiable<T, TStage> (T obj)
        {
            return new Modifiable<T, TStage>(obj);
        }

        /// <summary>The total number of modifiers across all stages.</summary>
        public int Count
        {
            get
            {
                UpdateCountIfDirty();
                return _cachedCount;
            }
        }

        /// <summary>Clears all change callbacks and disposes every stage.</summary>
        public void Dispose()
        {
            _valueChangedCallbacks.Clear();
            for (var i = 0; i < _stages.Count; i++)
                _stages[i].Dispose();
        }
    }
}
