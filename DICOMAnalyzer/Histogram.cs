using System;
using System.Linq;

namespace DICOMAnalyzer
{
    class Histogram
    {
        private readonly string _separator = "\t";
        public SliceData SliceData { get; private set; }
        public int[] Frequency;
        public CalcMode Mode { get; private set; }

        private Histogram()
        {
            // Instantiate via static generator.
        }

        public static Histogram Calculate(SliceData sd, CalcMode mode = CalcMode.BySum)
        {
            var h = new Histogram
            {
                SliceData = sd,
                Frequency = new int[100],
                Mode = mode,
            };

            var data = mode == CalcMode.ByCount ? sd.DataByCount : sd.DataBySum;
            var min = mode == CalcMode.ByCount ? sd.RollingCountMinValue : sd.RollingSumMinValue;
            var max = mode == CalcMode.ByCount ? sd.RollingCountMaxValue : sd.RollingSumMaxValue;
            double div = Math.Max(1.0, max - min);
            foreach (var val in data)
            {
                int idx = Math.Min(99, (int)Math.Floor((val - min) / div * 100.0));
                h.Frequency[idx]++;
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
                    "Min",
                    "Max",
                }),
                string.Join(_separator, new string[]
                {
                    $"{SliceData.RawData.SourceFileName}",
                    $"{Mode}",
                    $"{SliceData.MinValue}",
                    $"{SliceData.MaxValue}",
                }),
                string.Join(_separator, new string[]
                {
                    "Signal (relative)",
                    "Count",
                }),
                string.Join(Environment.NewLine, table));
        }
    }
}