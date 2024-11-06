// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member.

namespace UltEvents
{
    /// <summary>
    /// Utility methods for <see cref="UltEvents"/> to call which execute primitive type operators which
    /// aren't normally callable because they're handled directly by IL Opcodes rather than actual methods.
    /// </summary>
    /// <remarks>
    /// Using these is not generally recommended because UltEvents isn't intended to be a visual scripting system.
    /// Setting up events to use them is very cumbersome so it's probably better to just write your own methods
    /// with whatever math you need.
    /// </remarks>
    public static class Operators
    {
        public static bool Invert(bool value)
            => !value;
        public static bool Equals(bool a, bool b)
            => a == b;

        public static double Add(double a, double b)
            => a + b;
        public static double Subtract(double a, double b)
            => a - b;
        public static double Multiply(double a, double b)
            => a * b;
        public static double Divide(double a, double b)
            => a / b;
        public static bool Equals(double a, double b)
            => a == b;

        public static float Add(float a, float b)
            => a + b;
        public static float Subtract(float a, float b)
            => a - b;
        public static float Multiply(float a, float b)
            => a * b;
        public static float Divide(float a, float b)
            => a / b;
        public static bool Equals(float a, float b)
            => a == b;

        public static int Add(int a, int b)
            => a + b;
        public static int Subtract(int a, int b)
            => a - b;
        public static int Multiply(int a, int b)
            => a * b;
        public static int Divide(int a, int b)
            => a / b;
        public static bool Equals(int a, int b)
            => a == b;

        public static long Add(long a, long b)
            => a + b;
        public static long Subtract(long a, long b)
            => a - b;
        public static long Multiply(long a, long b)
            => a * b;
        public static long Divide(long a, long b)
            => a / b;
        public static bool Equals(long a, long b)
            => a == b;
    }
}