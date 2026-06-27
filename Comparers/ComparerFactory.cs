using System;
using System.Collections.Generic;

namespace ModifiableVariable.Comparers
{
    /// <summary>Resolves equality comparers per type, with built-in comparers for common Unity types.</summary>
    public static class ComparerFactory
    {
        static readonly Dictionary<Type, object> _comparers = new();

        static ComparerFactory()
        {
            Register(new FloatComparer());
            Register(new IntComparer());
            Register(new Vector2Comparer());
            Register(new Vector2IntComparer());
            Register(new Vector3Comparer());
            Register(new Vector3IntComparer());
            Register(new Vector4Comparer());
            Register(new ColorComparer());
            Register(new Color32Comparer());
            Register(new QuaternionComparer());
        }

        /// <summary>Registers an explicit comparer for type <typeparamref name="T"/>.</summary>
        public static void Register<T>(IEqualityComparer<T> comparer)
            => _comparers[typeof(T)] = comparer;

        /// <summary>Returns the registered comparer for <typeparamref name="T"/>, or the default one.</summary>
        public static IEqualityComparer<T> Get<T>()
        {
            if (_comparers.TryGetValue(typeof(T), out var c))
                return (IEqualityComparer<T>)c;
            return EqualityComparer<T>.Default;
        }
    }
}