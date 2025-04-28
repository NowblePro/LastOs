using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using static Tinkoff.InvestApi.V1.GetTechAnalysisRequest.Types;

namespace OsEngine.Indicators.TrigonumCustom
{

    [Indicator("NadarayaWatson")]
    public class NadarayaWatson : Aindicator
    {
        public static class DecimalConsts
        {
            public const decimal Pi = 3.1415926535897932384626433833m;
            public const decimal E = 2.7182818284590452353602874714m;
        }

        public IndicatorParameterInt nw_length;
        public IndicatorParameterDecimal nw_multiplier;
        public IndicatorParameterString nw_kernel;
        public IndicatorParameterDecimal kernel_bandwidth;

        public IndicatorDataSeries nw_estimate;
        public IndicatorDataSeries nw_up;
        public IndicatorDataSeries nw_down;

        private delegate decimal KernelFunc(decimal normalized_distance);
        private KernelFunc Kernel = null;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                nw_length = CreateParameterInt("Length", 20);
                nw_multiplier = CreateParameterDecimal("Multiplier", 2.0m);
                nw_kernel = CreateParameterStringCollection("Nadaraya-Watson Kernel", "Gaussian", new List<string>() { "Gaussian",
                                                                                                                       "Epanechnikov",
                                                                                                                       "Uniform",
                                                                                                                       "Triangular" });
                
                kernel_bandwidth = CreateParameterDecimal("Kernel Bandwidth", 0.5m);

                nw_estimate = CreateSeries("NWE Line", Color.DarkSalmon, IndicatorChartPaintType.Line, true);
                nw_up = CreateSeries("NW Up Line", Color.LightSalmon, IndicatorChartPaintType.Line, true);
                nw_down = CreateSeries("NW Down Line", Color.LightSalmon, IndicatorChartPaintType.Line, true);

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

            decimal nwe = NadarayaWatsonEstimate(source, index);
            nw_estimate.Values[index] = nwe;

            nw_up.Values[index] = nwe + StdDev(source, nw_estimate.Values, index) * nw_multiplier.ValueDecimal;
            nw_down.Values[index] = nwe - StdDev(source, nw_estimate.Values, index) * nw_multiplier.ValueDecimal;

            return;
        }

        private decimal NadarayaWatsonEstimate(List<Candle> source, int index)
        {
            if (nw_length.ValueInt < 1)
            {
                return 0.0m;
            }

            return Dispersion(source, index);
        }

        private decimal StdDev(List<Candle> source, List<decimal> nwe_src, int index)
        {
            return NthRoot(Dispersion(source, nwe_src, index), 2);
        }

        // TODO code dublicate
        private decimal Dispersion(List<Candle> source, int index)
        {
            decimal weighted_values_sum = 0.0m;
            decimal weights_sum = 0.0m;
            for (int i = 0; i < nw_length.ValueInt; ++i)
            {
                decimal normalized_distance = NormalizedDistance(i, kernel_bandwidth.ValueDecimal);
                decimal weight = Kernel(normalized_distance);

                weights_sum += weight;
                weighted_values_sum += source[index - i].Close * weight;
            }

            if (weights_sum == 0)
            {
                return 1.0m;
            }

            return weighted_values_sum / weights_sum;
        }

        // TODO code dublicate
        private decimal Dispersion(List<Candle> source, List<decimal> nwe_src, int index)
        {
            decimal weighted_values_sum = 0.0m;
            decimal weights_sum = 0.0m;
            for (int i = 0; i < nw_length.ValueInt; ++i)
            {
                decimal normalized_distance = NormalizedDistance(i, kernel_bandwidth.ValueDecimal);
                decimal weight = Kernel(normalized_distance);

                weights_sum += weight;
                weighted_values_sum += DecimalPow(source[index - i].Close - nwe_src[index], 2) * weight;
            }

            if (weights_sum == 0)
            {
                return 1.0m;
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
            if (Math.Abs(normalized_distance) <= 1.0m)
            {
                result = (3 / 4) * (1 - DecimalPow(normalized_distance, 2.0m));
            }
            return result;
        }

        private decimal UniformKernel(decimal normalized_distance)
        {
            decimal result = 0.0m;
            if (Math.Abs(normalized_distance) <= 1.0m)
            {
                result = 1 / 2;
            }
            return result;
        }

        private decimal TriangularKernel(decimal normalized_distance)
        {
            decimal result = 0.0m;
            if (Math.Abs(normalized_distance) <= 1.0m)
            {
                result = 1 - Math.Abs(normalized_distance);
            }
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
