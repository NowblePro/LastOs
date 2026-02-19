using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OsEngine.Entity;
using System.Windows.Forms;

namespace OsEngine.OsOptimizer
{
    public class RandomForestOptimizationResult
    {
        public double R2Holdout { get; set; }

        public double MaeHoldout { get; set; }

        public List<FeatureImportanceRecord> FeatureImportances { get; set; } = new List<FeatureImportanceRecord>();

        public List<ParameterValueRecord> BestParameters { get; set; } = new List<ParameterValueRecord>();

        public double BestPredictedTarget { get; set; }

        public int TrainCount { get; set; }

        public int HoldoutCount { get; set; }

        public int SampledCombinationCount { get; set; }

        public string TargetMetric { get; set; } = "TotalProfit";

        public string StatusMessage { get; set; }
    }

    public class FeatureImportanceRecord
    {
        public string Name { get; set; }
        public double Importance { get; set; }
    }

    public class ParameterValueRecord
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    internal static class RandomForestOptimizer
    {
        internal static RandomForestOptimizationResult Build(List<OptimazerFazeReport> fazeReports, List<bool> paramOn)
        {
            var result = new RandomForestOptimizationResult();

            if (fazeReports == null || fazeReports.Count == 0)
            {
                result.StatusMessage = "Нет данных для обучения.";
                return result;
            }

            OptimazerFazeReport inSample = fazeReports.FirstOrDefault(r => r.Faze.TypeFaze == OptimizerFazeType.InSample);

            if (inSample == null || inSample.Reports.Count == 0)
            {
                result.StatusMessage = "Нет InSample результатов для обучения.";
                return result;
            }

            List<RfDataPoint> data = BuildDataset(inSample.Reports, paramOn);
            result.SampledCombinationCount = data.Count;

            if (data.Count < 5)
            {
                result.StatusMessage = "Недостаточно данных для случайного леса (нужно >= 5 прогонов).";
                return result;
            }

            Shuffle(data);

            int holdoutCount = Math.Max(1, data.Count / 5);
            List<RfDataPoint> train = data.Take(data.Count - holdoutCount).ToList();
            List<RfDataPoint> holdout = data.Skip(train.Count).ToList();

            var forest = new RandomForestRegressor();
            forest.Fit(train.Select(p => p.Features).ToList(), train.Select(p => p.Target).ToList());

            List<double> holdoutPred = holdout.Select(p => forest.Predict(p.Features)).ToList();
            List<double> holdoutTarget = holdout.Select(p => p.Target).ToList();

            result.R2Holdout = CalculateR2(holdoutTarget, holdoutPred);
            result.MaeHoldout = CalculateMae(holdoutTarget, holdoutPred);
            result.TrainCount = train.Count;
            result.HoldoutCount = holdout.Count;
            result.StatusMessage = "OK";

            result.FeatureImportances = BuildImportances(forest.FeatureImportances, data.First().FeatureNames);

            double bestPredicted = double.MinValue;
            RfDataPoint bestPoint = null;
            foreach (RfDataPoint point in data)
            {
                double pred = forest.Predict(point.Features);
                if (pred > bestPredicted)
                {
                    bestPredicted = pred;
                    bestPoint = point;
                }
            }

            if (bestPoint != null)
            {
                result.BestPredictedTarget = bestPredicted;
                result.BestParameters = bestPoint.ParameterValues;
            }

            return result;
        }

        private static List<FeatureImportanceRecord> BuildImportances(double[] raw, List<string> names)
        {
            var records = new List<FeatureImportanceRecord>();
            if (raw == null || raw.Length == 0 || names == null)
            {
                return records;
            }

            double sum = raw.Sum();
            for (int i = 0; i < raw.Length && i < names.Count; i++)
            {
                double value = sum == 0 ? 0 : raw[i] / sum;
                records.Add(new FeatureImportanceRecord
                {
                    Name = names[i],
                    Importance = Math.Round(value, 6)
                });
            }

            return records.OrderByDescending(r => r.Importance).ToList();
        }

        private static double CalculateR2(IReadOnlyList<double> yTrue, IReadOnlyList<double> yPred)
        {
            if (yTrue.Count == 0)
            {
                return 0;
            }

            double mean = yTrue.Average();
            double ssTot = yTrue.Sum(v => Math.Pow(v - mean, 2));
            double ssRes = 0;
            for (int i = 0; i < yTrue.Count; i++)
            {
                ssRes += Math.Pow(yTrue[i] - yPred[i], 2);
            }

            if (Math.Abs(ssTot) < 1e-9)
            {
                return 0;
            }

            return 1 - ssRes / ssTot;
        }

        private static double CalculateMae(IReadOnlyList<double> yTrue, IReadOnlyList<double> yPred)
        {
            if (yTrue.Count == 0)
            {
                return 0;
            }

            double sum = 0;
            for (int i = 0; i < yTrue.Count; i++)
            {
                sum += Math.Abs(yTrue[i] - yPred[i]);
            }
            return sum / yTrue.Count;
        }

        private static void Shuffle(IList<RfDataPoint> list)
        {
            Random rnd = new Random(42);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static List<RfDataPoint> BuildDataset(List<OptimizerReport> reports, List<bool> paramOn)
        {
            var data = new List<RfDataPoint>();

            for (int i = 0; i < reports.Count; i++)
            {
                OptimizerReport report = reports[i];
                List<IIStrategyParameter> parameters = report.GetParameters();
                var features = new List<double>();
                var featureNames = new List<string>();
                var paramValues = new List<ParameterValueRecord>();

                for (int p = 0; p < parameters.Count && p < paramOn.Count; p++)
                {
                    if (!paramOn[p])
                    {
                        continue;
                    }

                    double? value = GetNumericValue(parameters[p]);
                    if (value.HasValue == false)
                    {
                        continue;
                    }

                    features.Add(value.Value);
                    featureNames.Add(parameters[p].Name);
                    paramValues.Add(new ParameterValueRecord
                    {
                        Name = parameters[p].Name,
                        Value = GetDisplayValue(parameters[p])
                    });
                }

                if (features.Count == 0)
                {
                    continue;
                }

                data.Add(new RfDataPoint
                {
                    Features = features.ToArray(),
                    FeatureNames = featureNames,
                    ParameterValues = paramValues,
                    Target = Convert.ToDouble(report.TotalProfit, CultureInfo.InvariantCulture)
                });
            }

            return data;
        }

        private static double? GetNumericValue(IIStrategyParameter parameter)
        {
            if (parameter.Type == StrategyParameterType.Int)
            {
                return ((StrategyParameterInt)parameter).ValueInt;
            }
            if (parameter.Type == StrategyParameterType.Decimal)
            {
                return Convert.ToDouble(((StrategyParameterDecimal)parameter).ValueDecimal, CultureInfo.InvariantCulture);
            }
            if (parameter.Type == StrategyParameterType.DecimalCheckBox)
            {
                var casted = (StrategyParameterDecimalCheckBox)parameter;
                return casted.CheckState == CheckState.Checked
                    ? Convert.ToDouble(casted.ValueDecimal, CultureInfo.InvariantCulture)
                    : 0;
            }

            return null;
        }

        private static string GetDisplayValue(IIStrategyParameter parameter)
        {
            if (parameter.Type == StrategyParameterType.Int)
            {
                return ((StrategyParameterInt)parameter).ValueInt.ToString(CultureInfo.InvariantCulture);
            }
            if (parameter.Type == StrategyParameterType.Decimal)
            {
                return ((StrategyParameterDecimal)parameter).ValueDecimal.ToString(CultureInfo.InvariantCulture);
            }
            if (parameter.Type == StrategyParameterType.DecimalCheckBox)
            {
                StrategyParameterDecimalCheckBox casted = (StrategyParameterDecimalCheckBox)parameter;
                return casted.ValueDecimal.ToString(CultureInfo.InvariantCulture) + $" ({casted.CheckState})";
            }

            return parameter.ToString();
        }
    }

    internal class RfDataPoint
    {
        public double[] Features { get; set; }
        public List<string> FeatureNames { get; set; }
        public double Target { get; set; }
        public List<ParameterValueRecord> ParameterValues { get; set; }
    }

    internal class RandomForestRegressor
    {
        private readonly int _trees;
        private readonly int _maxDepth;
        private readonly int _minSamplesSplit;
        private int _maxFeatures;
        private readonly Random _random;
        private readonly List<DecisionTreeRegressor> _forest = new List<DecisionTreeRegressor>();
        private double[] _featureImportances;

        public RandomForestRegressor(int trees = 30, int maxDepth = 6, int minSamplesSplit = 5, int? seed = null)
        {
            _trees = trees;
            _maxDepth = maxDepth;
            _minSamplesSplit = minSamplesSplit;
            _random = seed.HasValue ? new Random(seed.Value) : new Random(42);
        }

        public void Fit(List<double[]> x, List<double> y)
        {
            if (x == null || x.Count == 0)
            {
                _featureImportances = Array.Empty<double>();
                return;
            }

            int featureCount = x[0].Length;
            _maxFeatures = Math.Max(1, (int)Math.Round(Math.Sqrt(featureCount)));
            _forest.Clear();

            double[] globalImportance = new double[featureCount];

            for (int i = 0; i < _trees; i++)
            {
                List<double[]> sampleX;
                List<double> sampleY;
                Bootstrap(x, y, out sampleX, out sampleY);

                var tree = new DecisionTreeRegressor(_maxDepth, _minSamplesSplit, _maxFeatures, _random);
                tree.Fit(sampleX, sampleY);
                _forest.Add(tree);

                double[] localImportances = tree.FeatureImportances;
                for (int f = 0; f < globalImportance.Length && f < localImportances.Length; f++)
                {
                    globalImportance[f] += localImportances[f];
                }
            }

            _featureImportances = globalImportance;
        }

        public double Predict(double[] x)
        {
            if (_forest.Count == 0)
            {
                return 0;
            }

            double sum = 0;
            for (int i = 0; i < _forest.Count; i++)
            {
                sum += _forest[i].Predict(x);
            }

            return sum / _forest.Count;
        }

        public double[] FeatureImportances => _featureImportances;

        private void Bootstrap(List<double[]> x, List<double> y, out List<double[]> sampleX, out List<double> sampleY)
        {
            sampleX = new List<double[]>();
            sampleY = new List<double>();
            for (int i = 0; i < x.Count; i++)
            {
                int idx = _random.Next(0, x.Count);
                sampleX.Add(x[idx]);
                sampleY.Add(y[idx]);
            }
        }
    }

    internal class DecisionTreeRegressor
    {
        private readonly int _maxDepth;
        private readonly int _minSamplesSplit;
        private readonly int _maxFeatures;
        private readonly Random _random;
        private Node _root;
        private double[] _featureImportances;

        private class Node
        {
            public bool IsLeaf;
            public double Prediction;
            public int FeatureIndex;
            public double Threshold;
            public Node Left;
            public Node Right;
        }

        public DecisionTreeRegressor(int maxDepth, int minSamplesSplit, int maxFeatures, Random random)
        {
            _maxDepth = maxDepth;
            _minSamplesSplit = minSamplesSplit;
            _maxFeatures = maxFeatures;
            _random = random;
        }

        public void Fit(List<double[]> x, List<double> y)
        {
            int featureCount = x[0].Length;
            _featureImportances = new double[featureCount];
            _root = BuildNode(x, y, 0);
        }

        public double Predict(double[] x)
        {
            Node node = _root;
            while (!node.IsLeaf)
            {
                if (x[node.FeatureIndex] <= node.Threshold)
                {
                    node = node.Left;
                }
                else
                {
                    node = node.Right;
                }
            }

            return node.Prediction;
        }

        public double[] FeatureImportances => _featureImportances;

        private Node BuildNode(List<double[]> x, List<double> y, int depth)
        {
            double currentVariance = Variance(y);
            double prediction = y.Average();

            if (depth >= _maxDepth || y.Count < _minSamplesSplit || currentVariance < 1e-9)
            {
                return new Node { IsLeaf = true, Prediction = prediction };
            }

            int[] candidateFeatures = GetFeatureSubset(x[0].Length);
            SplitResult bestSplit = FindBestSplit(x, y, candidateFeatures, currentVariance);

            if (bestSplit == null || bestSplit.Gain <= 1e-9)
            {
                return new Node { IsLeaf = true, Prediction = prediction };
            }

            _featureImportances[bestSplit.FeatureIndex] += bestSplit.Gain;

            Node left = BuildNode(bestSplit.LeftX, bestSplit.LeftY, depth + 1);
            Node right = BuildNode(bestSplit.RightX, bestSplit.RightY, depth + 1);

            return new Node
            {
                FeatureIndex = bestSplit.FeatureIndex,
                Threshold = bestSplit.Threshold,
                Left = left,
                Right = right,
                IsLeaf = false
            };
        }

        private SplitResult FindBestSplit(List<double[]> x, List<double> y, int[] features, double parentVariance)
        {
            SplitResult best = null;

            foreach (int feature in features)
            {
                List<double> values = x.Select(row => row[feature]).ToList();
                List<double> thresholds = GenerateThresholds(values);

                foreach (double threshold in thresholds)
                {
                    var leftX = new List<double[]>();
                    var rightX = new List<double[]>();
                    var leftY = new List<double>();
                    var rightY = new List<double>();

                    for (int i = 0; i < x.Count; i++)
                    {
                        if (x[i][feature] <= threshold)
                        {
                            leftX.Add(x[i]);
                            leftY.Add(y[i]);
                        }
                        else
                        {
                            rightX.Add(x[i]);
                            rightY.Add(y[i]);
                        }
                    }

                    if (leftY.Count == 0 || rightY.Count == 0)
                    {
                        continue;
                    }

                    double gain = parentVariance - WeightedVariance(leftY, rightY);

                    if (best == null || gain > best.Gain)
                    {
                        best = new SplitResult
                        {
                            FeatureIndex = feature,
                            Threshold = threshold,
                            Gain = gain,
                            LeftX = leftX,
                            LeftY = leftY,
                            RightX = rightX,
                            RightY = rightY
                        };
                    }
                }
            }

            return best;
        }

        private int[] GetFeatureSubset(int featureCount)
        {
            List<int> all = Enumerable.Range(0, featureCount).ToList();
            List<int> subset = new List<int>();

            for (int i = 0; i < _maxFeatures; i++)
            {
                if (all.Count == 0)
                {
                    break;
                }

                int idx = _random.Next(0, all.Count);
                subset.Add(all[idx]);
                all.RemoveAt(idx);
            }

            return subset.ToArray();
        }

        private List<double> GenerateThresholds(List<double> values)
        {
            values.Sort();
            List<double> thresholds = new List<double>();
            int step = Math.Max(1, values.Count / 8);

            for (int i = step; i < values.Count; i += step)
            {
                double prev = values[i - 1];
                double cur = values[i];
                if (Math.Abs(cur - prev) < 1e-9)
                {
                    continue;
                }
                thresholds.Add((prev + cur) / 2.0);
            }

            if (thresholds.Count == 0 && values.Count > 1)
            {
                thresholds.Add((values.First() + values.Last()) / 2.0);
            }

            return thresholds;
        }

        private double WeightedVariance(List<double> left, List<double> right)
        {
            double leftVar = Variance(left);
            double rightVar = Variance(right);
            double total = left.Count + right.Count;
            return leftVar * left.Count / total + rightVar * right.Count / total;
        }

        private double Variance(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
            {
                return 0;
            }

            double mean = values.Average();
            double sum = 0;
            for (int i = 0; i < values.Count; i++)
            {
                sum += Math.Pow(values[i] - mean, 2);
            }
            return sum / values.Count;
        }

        private class SplitResult
        {
            public int FeatureIndex;
            public double Threshold;
            public double Gain;
            public List<double[]> LeftX;
            public List<double> LeftY;
            public List<double[]> RightX;
            public List<double> RightY;
        }
    }
}
