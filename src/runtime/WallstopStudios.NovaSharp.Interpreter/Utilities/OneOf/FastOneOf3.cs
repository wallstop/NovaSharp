namespace WallstopStudios.NovaSharp.Interpreter.Utilities.OneOf
{
    using System;
    using System.Runtime.CompilerServices;
    using DataStructs;

    /// <summary>
    /// A value-typed discriminated union that can hold one of three types.
    /// Zero-allocation alternative to object boxing or class-based unions.
    /// </summary>
    /// <typeparam name="T0">The first possible type.</typeparam>
    /// <typeparam name="T1">The second possible type.</typeparam>
    /// <typeparam name="T2">The third possible type.</typeparam>
    public readonly struct FastOneOf<T0, T1, T2> : IEquatable<FastOneOf<T0, T1, T2>>
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly byte _index;

        /// <summary>
        /// Initializes a new instance with a value of type <typeparamref name="T0"/>.
        /// </summary>
        /// <param name="value">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastOneOf(T0 value)
        {
            _value0 = value;
            _value1 = default;
            _value2 = default;
            _index = 0;
        }

        /// <summary>
        /// Initializes a new instance with a value of type <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="value">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastOneOf(T1 value)
        {
            _value0 = default;
            _value1 = value;
            _value2 = default;
            _index = 1;
        }

        /// <summary>
        /// Initializes a new instance with a value of type <typeparamref name="T2"/>.
        /// </summary>
        /// <param name="value">The value to store.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastOneOf(T2 value)
        {
            _value0 = default;
            _value1 = default;
            _value2 = value;
            _index = 2;
        }

        /// <summary>
        /// Gets a value indicating whether this instance contains a value of type <typeparamref name="T0"/>.
        /// </summary>
        public bool IsT0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index == 0;
        }

        /// <summary>
        /// Gets a value indicating whether this instance contains a value of type <typeparamref name="T1"/>.
        /// </summary>
        public bool IsT1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index == 1;
        }

        /// <summary>
        /// Gets a value indicating whether this instance contains a value of type <typeparamref name="T2"/>.
        /// </summary>
        public bool IsT2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index == 2;
        }

        /// <summary>
        /// Gets the index of the currently active type (0, 1, or 2).
        /// </summary>
        public int Index
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _index;
        }

        /// <summary>
        /// Gets the value as type <typeparamref name="T0"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the value is not of type <typeparamref name="T0"/>.</exception>
        public T0 AsT0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index != 0)
                {
                    ThrowInvalidState(0);
                }
                return _value0;
            }
        }

        /// <summary>
        /// Gets the value as type <typeparamref name="T1"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the value is not of type <typeparamref name="T1"/>.</exception>
        public T1 AsT1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index != 1)
                {
                    ThrowInvalidState(1);
                }
                return _value1;
            }
        }

        /// <summary>
        /// Gets the value as type <typeparamref name="T2"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the value is not of type <typeparamref name="T2"/>.</exception>
        public T2 AsT2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index != 2)
                {
                    ThrowInvalidState(2);
                }
                return _value2;
            }
        }

        /// <summary>
        /// Attempts to get the value as type <typeparamref name="T0"/>.
        /// </summary>
        /// <param name="value">When this method returns, contains the value if successful; otherwise, the default value.</param>
        /// <returns><c>true</c> if the value is of type <typeparamref name="T0"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetT0(out T0 value)
        {
            if (_index == 0)
            {
                value = _value0;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the value as type <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="value">When this method returns, contains the value if successful; otherwise, the default value.</param>
        /// <returns><c>true</c> if the value is of type <typeparamref name="T1"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetT1(out T1 value)
        {
            if (_index == 1)
            {
                value = _value1;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Attempts to get the value as type <typeparamref name="T2"/>.
        /// </summary>
        /// <param name="value">When this method returns, contains the value if successful; otherwise, the default value.</param>
        /// <returns><c>true</c> if the value is of type <typeparamref name="T2"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetT2(out T2 value)
        {
            if (_index == 2)
            {
                value = _value2;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Matches the contained value to a result using the appropriate function.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="matchT0">Function to invoke if the value is of type <typeparamref name="T0"/>.</param>
        /// <param name="matchT1">Function to invoke if the value is of type <typeparamref name="T1"/>.</param>
        /// <param name="matchT2">Function to invoke if the value is of type <typeparamref name="T2"/>.</param>
        /// <returns>The result of invoking the matching function.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any match function is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Match<TResult>(
            Func<T0, TResult> matchT0,
            Func<T1, TResult> matchT1,
            Func<T2, TResult> matchT2
        )
        {
            if (matchT0 == null)
            {
                throw new ArgumentNullException(nameof(matchT0));
            }
            if (matchT1 == null)
            {
                throw new ArgumentNullException(nameof(matchT1));
            }
            if (matchT2 == null)
            {
                throw new ArgumentNullException(nameof(matchT2));
            }
            if (_index == 0)
            {
                return matchT0(_value0);
            }
            if (_index == 1)
            {
                return matchT1(_value1);
            }
            return matchT2(_value2);
        }

        /// <summary>
        /// Executes an action based on the contained value's type.
        /// </summary>
        /// <param name="actionT0">Action to invoke if the value is of type <typeparamref name="T0"/>.</param>
        /// <param name="actionT1">Action to invoke if the value is of type <typeparamref name="T1"/>.</param>
        /// <param name="actionT2">Action to invoke if the value is of type <typeparamref name="T2"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when any action is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Switch(Action<T0> actionT0, Action<T1> actionT1, Action<T2> actionT2)
        {
            if (actionT0 == null)
            {
                throw new ArgumentNullException(nameof(actionT0));
            }
            if (actionT1 == null)
            {
                throw new ArgumentNullException(nameof(actionT1));
            }
            if (actionT2 == null)
            {
                throw new ArgumentNullException(nameof(actionT2));
            }
            if (_index == 0)
            {
                actionT0(_value0);
            }
            else if (_index == 1)
            {
                actionT1(_value1);
            }
            else
            {
                actionT2(_value2);
            }
        }

        /// <summary>
        /// Creates a <see cref="FastOneOf{T0, T1, T2}"/> from a value of type <typeparamref name="T0"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A new instance containing the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1000 // Do not declare static members on generic types - required by CA2225 for implicit operator alternate
        public static FastOneOf<T0, T1, T2> FromT0(T0 value)
#pragma warning restore CA1000
        {
            return new FastOneOf<T0, T1, T2>(value);
        }

        /// <summary>
        /// Creates a <see cref="FastOneOf{T0, T1, T2}"/> from a value of type <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A new instance containing the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1000 // Do not declare static members on generic types - required by CA2225 for implicit operator alternate
        public static FastOneOf<T0, T1, T2> FromT1(T1 value)
#pragma warning restore CA1000
        {
            return new FastOneOf<T0, T1, T2>(value);
        }

        /// <summary>
        /// Creates a <see cref="FastOneOf{T0, T1, T2}"/> from a value of type <typeparamref name="T2"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A new instance containing the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1000 // Do not declare static members on generic types - required by CA2225 for implicit operator alternate
        public static FastOneOf<T0, T1, T2> FromT2(T2 value)
#pragma warning restore CA1000
        {
            return new FastOneOf<T0, T1, T2>(value);
        }

        /// <summary>
        /// Implicitly converts a value of type <typeparamref name="T0"/> to a <see cref="FastOneOf{T0, T1, T2}"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastOneOf<T0, T1, T2>(T0 value)
        {
            return new FastOneOf<T0, T1, T2>(value);
        }

        /// <summary>
        /// Implicitly converts a value of type <typeparamref name="T1"/> to a <see cref="FastOneOf{T0, T1, T2}"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastOneOf<T0, T1, T2>(T1 value)
        {
            return new FastOneOf<T0, T1, T2>(value);
        }

        /// <summary>
        /// Implicitly converts a value of type <typeparamref name="T2"/> to a <see cref="FastOneOf{T0, T1, T2}"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastOneOf<T0, T1, T2>(T2 value)
        {
            return new FastOneOf<T0, T1, T2>(value);
        }

        /// <summary>
        /// Determines whether two <see cref="FastOneOf{T0, T1, T2}"/> instances are equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FastOneOf<T0, T1, T2> left, FastOneOf<T0, T1, T2> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="FastOneOf{T0, T1, T2}"/> instances are not equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if both instances are not equal; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FastOneOf<T0, T1, T2> left, FastOneOf<T0, T1, T2> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether this instance equals another <see cref="FastOneOf{T0, T1, T2}"/>.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastOneOf<T0, T1, T2> other)
        {
            if (_index != other._index)
            {
                return false;
            }

            if (_index == 0)
            {
                if (_value0 == null)
                {
                    return other._value0 == null;
                }
                return _value0.Equals(other._value0);
            }

            if (_index == 1)
            {
                if (_value1 == null)
                {
                    return other._value1 == null;
                }
                return _value1.Equals(other._value1);
            }

            if (_value2 == null)
            {
                return other._value2 == null;
            }
            return _value2.Equals(other._value2);
        }

        /// <summary>
        /// Determines whether this instance equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="FastOneOf{T0, T1, T2}"/> and equals this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is FastOneOf<T0, T1, T2> other)
            {
                return Equals(other);
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode()
        {
            if (_index == 0)
            {
                return HashCodeHelper.HashCode(_index, _value0);
            }
            if (_index == 1)
            {
                return HashCodeHelper.HashCode(_index, _value1);
            }
            return HashCodeHelper.HashCode(_index, _value2);
        }

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>A string representing the contained value.</returns>
        public override string ToString()
        {
            if (_index == 0)
            {
                if (_value0 == null)
                {
                    return "T0: null";
                }
                return "T0: " + _value0.ToString();
            }

            if (_index == 1)
            {
                if (_value1 == null)
                {
                    return "T1: null";
                }
                return "T1: " + _value1.ToString();
            }

            if (_value2 == null)
            {
                return "T2: null";
            }
            return "T2: " + _value2.ToString();
        }

        private static void ThrowInvalidState(int expectedIndex)
        {
            throw new InvalidOperationException(
                "Cannot access T" + expectedIndex + " when value is of a different type."
            );
        }
    }
}
