using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OsEngine.Indicators.TrigonumCustom
{
    public static class DecimalConsts
    {
        public const decimal Pi = 3.1415926535897932384626433833m;
        public const decimal E = 2.7182818284590452353602874714m;
    }

    [Indicator("NadarayaWatson")]
    public class NadarayaWatson : Aindicator
    {
        public IndicatorParameterInt nw_length;
        public IndicatorParameterString nw_kernel;
        public IndicatorParameterDecimal kernel_bandwidth;

        public IndicatorDataSeries nw_central_line;

        private delegate decimal KernelFunc(decimal normalized_distance);
        private KernelFunc Kernel = null;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                nw_length = CreateParameterInt("Length", 15);
                nw_kernel = CreateParameterStringCollection("Nadaraya-Watson Kernel", "Gaussian", new List<string>() { "Gaussian",
                                                                                                                       "Epanechnikov",
                                                                                                                       "Uniform",
                                                                                                                       "Triangular" });
                
                kernel_bandwidth = CreateParameterDecimal("Kernel Bandwidth", 0.5m);

                nw_central_line = CreateSeries("NWE Line", Color.DarkSalmon, IndicatorChartPaintType.Line, true);

                switch (nw_kernel.ValueString)
                {
                    case "Gaussian":
                        Kernel = GaussianKernel;
                        break;

                    case "Epanechnikov":
                        Kernel = EpanechnikovKernel;
                        break;

                    case "Uniform":
                        Kernel = UniformKernel;
                        break;

                    case "Triangular":
                        Kernel = TriangularKernel;
                        break;

                    default:
                        Kernel = GaussianKernel;
                        break;
                }
            }
        }

        public override void OnProcess(List<Candle> source, int index)
        {
            if (index <= nw_length.ValueInt)
            {
                return;
            }

            nw_central_line.Values[index] = NadarayaWatsonSmooth(source, index);

            return;
        }

        private decimal NadarayaWatsonSmooth(List<Candle> source, int index)
        {
            if (nw_length.ValueInt < 1)
            {
                return 0.0m;
            }

            decimal weighted_values_sum = 0.0m;
            decimal weights_sum = 0.0m;
            for (int i = 0; i < nw_length.ValueInt; ++i)
            {
                decimal normalized_distance = NormalizedDistance(i, kernel_bandwidth.ValueDecimal);
                decimal weight = Kernel(normalized_distance);

                weights_sum += weight;
                weighted_values_sum += source[index - i].Close * weight;
            }

            return weighted_values_sum / weights_sum;
        }

        private decimal GaussianKernel(decimal normalized_distance)
        {
            decimal power = 0 - ( DecimalPow(normalized_distance, 2) / 2 );
            decimal denominator = NthRoot(2 * DecimalConsts.Pi, 2);
            decimal result = (1 / denominator) * DecimalPow(DecimalConsts.E, power);

            return result;
        }

        private decimal EpanechnikovKernel(decimal normalized_distance)
        {
            decimal result = 0.0m;
            return result;
        }

        private decimal UniformKernel(decimal normalized_distance)
        {
            decimal result = 0.0m;
            return result;
        }

        private decimal TriangularKernel(decimal normalized_distance)
        {
            decimal result = 0.0m;
            return result;
        }

        private decimal NormalizedDistance(int distance, decimal bandwidth)
        {
            return distance / bandwidth;
        }

        private static decimal NthRoot(decimal x, int n, decimal epsilon = 0.0000001M)
        {
            if (n == 0)
                throw new ArgumentException("Power can not be zero");

            decimal current = (decimal)Math.Pow((double)x, 1.0 / n);
            decimal previous;

            do
            {
                previous = current;
                current = ((n - 1) * previous + x / DecimalPow(previous, n - 1)) / n;
            }
            while (Math.Abs(previous - current) > epsilon);

            return current;
        }

        private static decimal DecimalPow(decimal x, decimal power)
        {
            return (decimal)Math.Pow((double)x, (double)power);
        }
    }
}
