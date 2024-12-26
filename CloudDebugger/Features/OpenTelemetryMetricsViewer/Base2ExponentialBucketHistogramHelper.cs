// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace CloudDebugger.Features.OpenTelemetryMetricsViewer;

/// <summary>
/// Contains helper methods for the Base2ExponentialBucketHistogram class.
/// 
/// Source: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/Shared/Metrics/Base2ExponentialBucketHistogramHelper.cs
/// </summary>
internal static class Base2ExponentialBucketHistogramHelper
{
    private const double EpsilonTimes2 = double.Epsilon * 2;
    private static readonly double Ln2 = Math.Log(2);
    private const double Tolerance = 1e-10;

    public static double CalculateLowerBoundary(int index, int scale)
    {
        if (scale > 0)
        {
#if NET
            var inverseFactor = Math.ScaleB(Ln2, -scale);
#else
            var inverseFactor = ScaleB(Ln2, -scale);
#endif
            var lowerBound = Math.Exp(index * inverseFactor);
            return Math.Abs(lowerBound) < Tolerance ? double.Epsilon : lowerBound;
        }
        else
        {
            if (scale == -1 && index == -537 || scale == 0 && index == -1074)
            {
                return EpsilonTimes2;
            }

            var n = index << -scale;

            if (n < -1074)
            {
                return double.Epsilon;
            }

#if NET
            return Math.ScaleB(1, n);
#else
            return ScaleB(1, n);
#endif
        }
    }

#if !NET
    private const double SCALEB_C1 = 8.98846567431158E+307;
    private const double SCALEB_C2 = 2.2250738585072014E-308;
    private const double SCALEB_C3 = 9007199254740992;

    private static double ScaleB(double x, int n)
    {
        double y = x;
        if (n > 1023)
        {
            y *= SCALEB_C1;
            n -= 1023;
            if (n > 1023)
            {
                y *= SCALEB_C1;
                n -= 1023;
                if (n > 1023)
                {
                    n = 1023;
                }
            }
        }
        else if (n < -1022)
        {
            y *= SCALEB_C2 * SCALEB_C3;
            n += 1022 - 53;
            if (n < -1022)
            {
                y *= SCALEB_C2 * SCALEB_C3;
                n += 1022 - 53;
                if (n < -1022)
                {
                    n = -1022;
                }
            }
        }

        double u = BitConverter.Int64BitsToDouble(((long)(0x3ff + n) << 52));
        return y * u;
    }
#endif
}
