using System;
using System.Collections.Generic;
using System.Linq;

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
        bool _disposed;
        [SerializeField] T _baseValue;

        /// <summary>The unmodified base value before any stage is applied.</summary>
        public T BaseValue => _baseValue;

        readonly Dictionary<int, Stage<T>> _stageMap = new();
        readonly List<ValueChangedDelegate<T>> _valueChangedCallbacks = new();
        bool _recalculateValueGuard;
        
        T _cachedValue;
        T _defaultValue;
        bool _hasInited;
        bool _hasDefault;
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
            _cachedValue = baseValue;
            
            _defaultValue = baseValue;
            _hasDefault = true;
            
            for (var i = 0; i < stages.Length; i++)
            {
                AddStage(stages[i]);
            }
            Invalidate();
        }

        /// <summary>
        /// Creates a modifiable with the given base value, populating stages from the
        /// <see cref="StageOpAttribute"/> markers on the <typeparamref name="TStage"/> enum.
        /// </summary>
        public Modifiable(T baseValue)
        {
            _comparer = ComparerFactory.Get<T>();
            _baseValue = baseValue;
            _cachedValue = baseValue;
            
            _defaultValue = baseValue;
            _hasDefault = true;
            
            Invalidate();
        }

        /// <summary>Overrides the equality comparer used for caching and change detection.</summary>
        public Modifiable<T, TStage> WithComparer(IEqualityComparer<T> comparer)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot set a comparer on {nameof(Modifiable<T, TStage>)} while computing its value.");
            LazyInitialize();
            _comparer = comparer;
            Invalidate();
            return this;
        }

        /// <summary>Adds a stage with the given operation under the given enum key.</summary>
        public Modifiable<T, TStage> AddStage(StageOp<T> op, TStage stage)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (op == null) throw new ArgumentNullException(nameof(op));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot add a stage to {nameof(Modifiable<T, TStage>)} while computing its value.");
            
            var s = new Stage<T>(op);
            s.HandleStageRemoved += Invalidate;
            _stages.Add(s);
            if (_stageMap.TryGetValue(Convert.ToInt32(stage), out var existing))
            {
                Debug.LogWarning($"Stage {stage} already exists in {nameof(Modifiable<T, TStage>)}. Replacing it with a new stage.");
                _stages.Remove(existing);
            }
            _stageMap[Convert.ToInt32(stage)] = s;

            Invalidate();
            return this;
        }

        /// <summary>Adds a stage described by an enum key and operation tuple.</summary>
        public Modifiable<T, TStage> AddStage((TStage, StageOp<T>) stage)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot add a stage to {nameof(Modifiable<T, TStage>)} while computing its value.");
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
                if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
                if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot change the base value of {nameof(Modifiable<T, TStage>)} while computing its Value property.");
                
                LazyInitialize();
                
                if (!_comparer.Equals(_baseValue, value))
                {
                    _baseValue = value;
                    Invalidate();
                }
            }
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
                LazyInitialize();
                
                if (_recalculateValueGuard)
                    return _cachedValue;
                if (!IsFrameDirty() && !IsModifiersHasChanged())
                    return _cachedValue;

                _recalculateValueGuard = true;
                T value = default;
                try
                {
                    value = Calculate();
                    if (!_comparer.Equals(_cachedValue, value))
                    {
                        _cachedValue = value;
                        for (var i = 0; i < _valueChangedCallbacks.Count; i++)
                            _valueChangedCallbacks[i](value);
                    }

                    ClearFrameDirty();
                    ClearModifiersChanged();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return _cachedValue;
                }
                finally
                {
                    _recalculateValueGuard = false;
                }

                return value;
            }
        }

        /// <summary>Adds a modifier to the given stage and returns a disposable handle.</summary>
        public ModifierDelegateHandler<T> Add(ModifierDelegate<T> modifier, TStage stage = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot add a modifier to {nameof(Modifiable<T, TStage>)} while computing its value.");
            LazyInitialize();

            var foundStage = GetStage(stage);
            if (foundStage == null)
                throw new KeyNotFoundException($"No stage {stage} for {typeof(TStage).Name}");
            
            Invalidate();
            return foundStage.Add(modifier);
        }

        /// <summary>Removes a modifier from the given stage.</summary>
        public bool Remove(ModifierDelegate<T> modifier, TStage stage)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot remove a modifier from {nameof(Modifiable<T, TStage>)} while computing its value.");
            LazyInitialize();

            var foundStage = GetStage(stage);
            if (foundStage == null)
                throw new KeyNotFoundException($"No stage {stage} for {typeof(TStage).Name}");
            
            Invalidate();
            return foundStage.Remove(modifier);
        }

        /// <summary>Removes the modifier referenced by a handle from the given stage.</summary>
        public bool Remove(ModifierDelegateHandler<T> handler, TStage stage)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot remove a modifier from {nameof(Modifiable<T, TStage>)} while computing its value.");
            return Remove(handler.Modifier, stage);
        }

        /// <summary>Removes a modifier from any stage that contains it.</summary>
        public bool Remove(ModifierDelegate<T> modifier)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot remove a modifier from {nameof(Modifiable<T, TStage>)} while computing its value.");
            LazyInitialize();
            
            bool removed = false;
            bool hasInvalidStages = false;
            
            for (var i = 0; i < _stages.Count; i++)
            {
                if (_stages[i] == null || _stages[i].IsDisposed)
                {
                    hasInvalidStages = true;
                    continue;
                }
                if (_stages[i].Remove(modifier))
                {
                    removed = true;
                    break;
                }
            }
            if (hasInvalidStages)
                ClearInvalidStages();
            if (removed)
                Invalidate();
            
            return removed;
        }

        /// <summary>Removes the modifier referenced by a handle from any stage that contains it.</summary>
        public bool Remove(ModifierDelegateHandler<T> modifier)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot remove a modifier from {nameof(Modifiable<T, TStage>)} while computing its value.");
            LazyInitialize();
            
            bool removed = false;
            bool hasInvalidStages = false;
            
            for (var i = 0; i < _stages.Count; i++)
            {
                if (_stages[i] == null || _stages[i].IsDisposed)
                {
                    hasInvalidStages = true;
                    continue;
                }
                if (_stages[i].Remove(modifier))
                {
                    removed = true;
                    break;
                }
            }
            if (hasInvalidStages)
                ClearInvalidStages();
            if (removed)
                Invalidate();
            
            return removed;
        }

        /// <summary>Registers a callback invoked when the computed value changes; returns a disposable handle.</summary>
        public ValueChangedHandler<T> OnValueChanged(ValueChangedDelegate<T> callback)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot register a value changed callback on {nameof(Modifiable<T, TStage>)} while computing its value.");
            LazyInitialize();
            _valueChangedCallbacks.Add(callback);
            return new ValueChangedHandler<T>(callback, (v) =>
            {
                if (_disposed) return false;
                if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot remove a value changed callback from {nameof(Modifiable<T, TStage>)} while computing its value.");
                return _valueChangedCallbacks.Remove(v);
            });
        }

        /// <summary>Removes all modifiers from every stage and restores the original base value.</summary>
        public void Clear()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot clear {nameof(Modifiable<T, TStage>)} while computing its value.");
            Invalidate();
            
            bool hasInvalidStages = false;
            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i] != null && !_stages[i].IsDisposed)
                    _stages[i].Clear();
                else hasInvalidStages = true;
            }
            if (hasInvalidStages) ClearInvalidStages();
            
            if (_hasDefault)
                _baseValue = _defaultValue;
        }

        T Calculate()
        {
            var value = _baseValue;
            bool hasInvalidStages = false;
            for (var i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];

                if (stage == null)
                {
                    hasInvalidStages = true;
                    Debug.LogWarning($"Stage {i} is null and will be skipped in {nameof(Modifiable<T, TStage>)}.");
                    continue;
                }
                if (stage.IsDisposed)
                {
                    hasInvalidStages = true;
                    Debug.LogWarning($"Stage {i} has been disposed and will be skipped in {nameof(Modifiable<T, TStage>)}.");
                    continue;
                }
                value = stage.Proceed(value);
            }
            if (hasInvalidStages)
                ClearInvalidStages();
            
            return value;
        }
        void ClearInvalidStages()
        {
            for (var i = _stages.Count - 1; i >= 0; i--)
            {
                if (_stages[i] == null || _stages[i].IsDisposed)
                {
                    _stages.RemoveAt(i);
                }
            }

            foreach (var k in _stageMap.Keys.ToList())
            {
                var s = _stageMap[k];
                if (s == null || s.IsDisposed) _stageMap.Remove(k);
            }
        }
        
        bool IsFrameDirty() => Time.frameCount != _cachedFrame;
        void ClearFrameDirty() => _cachedFrame = Time.frameCount;
        bool IsModifiersHasChanged() => _modifiersHasChanged;
        void ClearModifiersChanged() => _modifiersHasChanged = false;
        void Invalidate()
        {
            _cachedFrame = -1;
            _modifiersHasChanged = true;
            UpdateCount();
        }
        
        Stage<T> GetStage(TStage key)
        {
            if (_stages.Count == 0)
                throw new InvalidOperationException($"No stages have been added to {nameof(Modifiable<T, TStage>)}.");
            return _stageMap.TryGetValue(Convert.ToInt32(key), out var s) 
                ? s 
                : throw new KeyNotFoundException($"No stage {key} for {typeof(TStage).Name}");
        }
        static TStage ToStage(int key) => (TStage)(object)key;

        /// <summary>Implicitly resolves the modifiable to its computed value.</summary>
        public static implicit operator T (Modifiable<T, TStage> obj)
        {
            if (obj._disposed) throw new ObjectDisposedException(nameof(Modifiable<T, TStage>));
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
                return _cachedCount;
            }
        }
        internal void UpdateCount()
        {
            _cachedCount = 0;
            bool hasInvalidStages = false;
            for (var i = 0; i < _stages.Count; i++)
            {
                if (_stages[i] == null || _stages[i].IsDisposed)
                {
                    hasInvalidStages = true;
                    continue;    
                }
                _cachedCount += _stages[i].Modifiers.Count;
            }
            if (hasInvalidStages)
                ClearInvalidStages();
        }

        /// <summary>Clears all change callbacks and disposes every stage.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            if (_recalculateValueGuard) throw new InvalidOperationException($"Cannot dispose {nameof(Modifiable<T, TStage>)} while computing its value.");
            _disposed = true;
            _valueChangedCallbacks.Clear();
            for (var i = 0; i < _stages.Count; i++)
                _stages[i]?.Dispose();
            _stages.Clear();
            _stageMap.Clear();
            _cachedFrame = -1;
            _modifiersHasChanged = false;
            _cachedCount = 0;
        }
        void LazyInitialize()
        {
            if (!_hasInited)
            {
                if (!_hasDefault)
                {
                    _defaultValue = _baseValue;
                    _hasDefault = true;
                }
                _comparer ??= ComparerFactory.Get<T>();
                if (_stages.Count == 0)
                {
                    if (!ModifiableFactory.TryPopulate(this))
                        throw new InvalidOperationException($"[LAZY INITIALIZE]\n" +
                                                            $"No stages were added to {nameof(Modifiable<T, TStage>)}. " +
                                                            $"Either add stages explicitly or mark the {typeof(TStage).Name} " +
                                                            $"enum with {nameof(StageOpAttribute)}.");
                }
                _hasInited = true;
            }
        }
    }
}
