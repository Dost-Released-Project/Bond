public readonly struct PureDataArithmetic
{
    public readonly float A;
    public readonly float B;

    public PureDataArithmetic(float a, float b)
    {
        A = a;
        B = b;
    }
}

public static class ArithmeticSystem
{
    public static float Add(PureDataArithmetic data) => data.A + data.B;
    public static float Subtract(PureDataArithmetic data) => data.A - data.B;
}
