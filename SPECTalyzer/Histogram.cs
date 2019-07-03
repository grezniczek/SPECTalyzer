using System;
using System.Linq;

namespace SPECTalyzer
{
    class Histogram
    {
        private readonly string _separator = "\t";
        public SliceData SliceData { get; private set; }
        public int[] Frequency;
        public CalcMode CalcMode { get; private set; }
        public HistoMode HistoMode { get; private set; }

        private Histogram()
        {
            // Instantiate via static generator.
        }

        public static Histogram Calculate(SliceData sd, CalcMode calcMode = CalcMode.BySum, HistoMode histoMode = HistoMode.Normal)
        {
            var h = new Histogram
            {
                SliceData = sd,
                Frequency = new int[100],
                CalcMode = calcMode,
                HistoMode = histoMode,
            };

            var data = calcMode == CalcMode.ByCount ? sd.DataByCount : sd.DataBySum;

            if (h.HistoMode == HistoMode.Normal)
            {
                var min = calcMode == CalcMode.ByCount ? sd.RollingCountMinValue : sd.RollingSumMinValue;
                var max = calcMode == CalcMode.ByCount ? sd.RollingCountMaxValue : sd.RollingSumMaxValue;
                double div = Math.Max(1.0, max - min);
                foreach (var val in data)
                {
                    int idx = Math.Min(99, (int)Math.Floor((val - min) / div * 100.0));
                    h.Frequency[idx]++;
                }
            }
            else
            {
                var min = calcMode == CalcMode.ByCount ? sd.RollingCountMinValue : sd.RollingSumMinValue;
                var max = calcMode == CalcMode.ByCount ? sd.RollingCountMaxValue : sd.RollingSumMaxValue;
                double div = Math.Max(10.0, max - min);
                foreach (var val in data)
                {
                    int idx = Math.Min(99, (int)Math.Floor((Math.Log10(Math.Max(1, val - min)) / Math.Log10(div) * 100.0)));
                    h.Frequency[idx]++;
                }
            }

            return h;
        }

        public string Dump()
        {
            var table = Enumerable.Range(0, Frequency.Length).
                Select(i => string.Join(_separator, $"{i}", $"{Frequency[i]}")).ToArray();

            return string.Join(Environment.NewLine,
                string.Join(_separator, new string[]
                {
                    "File",
                    "CalcMode",
                    "HistoMode",
                    "Min",
                    "Max",
                }),
                string.Join(_separator, new string[]
                {
                    $"{SliceData.RawData.SourceFileName}",
                    $"{CalcMode}",
                    $"{HistoMode}",
                    $"{SliceData.MinValue}",
                    $"{SliceData.MaxValue}",
                }),
                string.Join(_separator, new string[]
                {
                    "Signal",
                    "Count",
                }),
                string.Join(Environment.NewLine, table));
        }
    }
}