using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct ComplexF
{
    public float Real;
    public float Imag;

    public ComplexF(float real, float imag)
    {
        Real = real;
        Imag = imag;
    }

    public readonly float MagnitudeSquared => Real * Real + Imag * Imag;
    public readonly float Magnitude => MathF.Sqrt(Real * Real + Imag * Imag);
    public readonly float Phase => MathF.Atan2(Imag, Real);

    public static ComplexF FromPolar(float magnitude, float phase)
        => new(
            magnitude * MathF.Cos(phase),
            magnitude * MathF.Sin(phase)
        );

    public static ComplexF operator +(ComplexF a, ComplexF b)
        => new ComplexF(a.Real + b.Real, a.Imag + b.Imag);

    public static ComplexF operator -(ComplexF a, ComplexF b)
        => new ComplexF(a.Real - b.Real, a.Imag - b.Imag);

    public static ComplexF operator *(ComplexF a, ComplexF b)
        => new ComplexF(
            a.Real * b.Real - a.Imag * b.Imag,
            a.Real * b.Imag + a.Imag * b.Real
        );

    public static ComplexF operator /(ComplexF a, ComplexF b)
    {
        float denom = b.Real * b.Real + b.Imag * b.Imag;
        return new ComplexF(
            (a.Real * b.Real + a.Imag * b.Imag) / denom,
            (a.Imag * b.Real - a.Real * b.Imag) / denom
        );
    }

    public static ComplexF operator *(ComplexF value, float scalar)
    {
        return new ComplexF(value.Real * scalar, value.Imag * scalar);
    }

    public static ComplexF operator *(float scalar, ComplexF value)
    {
        return new ComplexF(value.Real * scalar, value.Imag * scalar);
    }

    public override string ToString()
        => $"{Real} + {Imag}i";
}