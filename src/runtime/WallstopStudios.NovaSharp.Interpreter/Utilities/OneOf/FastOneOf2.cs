namespace WallstopStudios.NovaSharp.Interpreter.Utilities.OneOf
{
    using System;
    using System.Runtime.CompilerServices;
    using DataStructs;

    /// <summary>
    /// A value-typed discriminated union that can hold one of two types.
    /// Zero-allocation alternative to object boxing or class-based unions.
    /// </summary>
    /// <typeparam name="T0">The first possible type.</typeparam>
    /// <typeparam name="T1">The second possible type.</typeparam>
    public readonly struct FastOneOf<T0, T1> : IEquatable<FastOneOf<T0, T1>>
    {
        private readonly T0 _value0;
        private readonly T1 _value1;
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
            _index = 1;
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
        /// Gets the index of the currently active type (0 or 1).
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
        /// Matches the contained value to a result using the appropriate function.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="matchT0">Function to invoke if the value is of type <typeparamref name="T0"/>.</param>
        /// <param name="matchT1">Function to invoke if the value is of type <typeparamref name="T1"/>.</param>
        /// <returns>The result of invoking the matching function.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any match function is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Match<TResult>(Func<T0, TResult> matchT0, Func<T1, TResult> matchT1)
        {
            if (matchT0 == null)
            {
                throw new ArgumentNullException(nameof(matchT0));
            }
            if (matchT1 == null)
            {
                throw new ArgumentNullException(nameof(matchT1));
            }
            if (_index == 0)
            {
                return matchT0(_value0);
            }
            return matchT1(_value1);
        }

        /// <summary>
        /// Executes an action based on the contained value's type.
        /// </summary>
        /// <param name="actionT0">Action to invoke if the value is of type <typeparamref name="T0"/>.</param>
        /// <param name="actionT1">Action to invoke if the value is of type <typeparamref name="T1"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when any action is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Switch(Action<T0> actionT0, Action<T1> actionT1)
        {
            if (actionT0 == null)
            {
                throw new ArgumentNullException(nameof(actionT0));
            }
            if (actionT1 == null)
            {
                throw new ArgumentNullException(nameof(actionT1));
            }
            if (_index == 0)
            {
                actionT0(_value0);
            }
            else
            {
                actionT1(_value1);
            }
        }

        /// <summary>
        /// Creates a <see cref="FastOneOf{T0, T1}"/> from a value of type <typeparamref name="T0"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A new instance containing the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1000 // Do not declare static members on generic types - required by CA2225 for implicit operator alternate
        public static FastOneOf<T0, T1> FromT0(T0 value)
#pragma warning restore CA1000
        {
            return new FastOneOf<T0, T1>(value);
        }

        /// <summary>
        /// Creates a <see cref="FastOneOf{T0, T1}"/> from a value of type <typeparamref name="T1"/>.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A new instance containing the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1000 // Do not declare static members on generic types - required by CA2225 for implicit operator alternate
        public static FastOneOf<T0, T1> FromT1(T1 value)
#pragma warning restore CA1000
        {
            return new FastOneOf<T0, T1>(value);
        }

        /// <summary>
        /// Implicitly converts a value of type <typeparamref name="T0"/> to a <see cref="FastOneOf{T0, T1}"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastOneOf<T0, T1>(T0 value)
        {
            return new FastOneOf<T0, T1>(value);
        }

        /// <summary>
        /// Implicitly converts a value of type <typeparamref name="T1"/> to a <see cref="FastOneOf{T0, T1}"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator FastOneOf<T0, T1>(T1 value)
        {
            return new FastOneOf<T0, T1>(value);
        }

        /// <summary>
        /// Determines whether two <see cref="FastOneOf{T0, T1}"/> instances are equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FastOneOf<T0, T1> left, FastOneOf<T0, T1> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="FastOneOf{T0, T1}"/> instances are not equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if both instances are not equal; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FastOneOf<T0, T1> left, FastOneOf<T0, T1> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether this instance equals another <see cref="FastOneOf{T0, T1}"/>.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns><c>true</c> if both instances are equal; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastOneOf<T0, T1> other)
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

            if (_value1 == null)
            {
                return other._value1 == null;
            }
            return _value1.Equals(other._value1);
        }

        /// <summary>
        /// Determines whether this instance equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is a <see cref="FastOneOf{T0, T1}"/> and equals this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (obj is FastOneOf<T0, T1> other)
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
            return HashCodeHelper.HashCode(_index, _value1);
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

            if (_value1 == null)
            {
                return "T1: null";
            }
            return "T1: " + _value1.ToString();
        }

        private static void ThrowInvalidState(int expectedIndex)
        {
            throw new InvalidOperationException(
                "Cannot access T" + expectedIndex + " when value is of a different type."
            );
        }
    }
}
