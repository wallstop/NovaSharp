namespace NovaSharp.Interpreter
{
    using System;

    public static class InvariantString
    {
        public static string ToLowerInvariantIfNeeded(string value)
        {
            return ConvertIfNeeded(value, lowerCase: true);
        }

        public static string ToUpperInvariantIfNeeded(string value)
        {
            return ConvertIfNeeded(value, lowerCase: false);
        }

        private static string ConvertIfNeeded(string value, bool lowerCase)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                char converted = lowerCase
                    ? char.ToLowerInvariant(current)
                    : char.ToUpperInvariant(current);

                if (converted != current)
                {
                    return string.Create(
                        value.Length,
                        (value, lowerCase),
                        static (destination, state) =>
                        {
                            string source = state.value;

                            if (state.lowerCase)
                            {
                                for (int i = 0; i < destination.Length; i++)
                                {
                                    destination[i] = char.ToLowerInvariant(source[i]);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < destination.Length; i++)
                                {
                                    destination[i] = char.ToUpperInvariant(source[i]);
                                }
                            }
                        }
                    );
                }
            }

            return value;
        }
    }
}
