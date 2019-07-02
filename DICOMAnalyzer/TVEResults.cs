using System;
using System.Linq;

namespace DICOMAnalyzer
{
    class TVEResults
    {
        private readonly string _separator = "\t";
        public SliceData SliceData { get; private set; }
        public int Total { get; private set; }
        public int Cutoff { get; private set; }
        public int Count { get; private set; }
        public double Percentage { get; private set; }
        public double Factor { get; private set; }
        public CalcMode Mode { get; private set; }

        private TVEResults()
        {
            // Instantiate via static generator.
        }

        public string Dump()
        {
            return string.Join(Environment.NewLine,
                string.Join(_separator, new string[]
                {
                    "File",
                    "NumVoxels",
                    "Total",
                    "Factor",
                    "Mode",
                    "Cutoff",
                    "Count",
                    "Percentage",
                }),
                string.Join(_separator, new string[]
                {
                    $"{SliceData.RawData.SourceFileName}",
                    $"{SliceData.Count}",
                    $"{Total}",
                    $"{Factor:0.000}",
                    $"{Mode.ToString()}",
                    $"{Cutoff}",
                    $"{Count}",
                    $"{Percentage:0.000}",
                }));
        }

        public static TVEResults Calculate(SliceData sliceData, double factor = 0.3, CalcMode mode = CalcMode.BySum)
        {
            var data = mode == CalcMode.BySum ? sliceData.DataBySum : sliceData.DataByCount;
            var total = data.Sum(x => x);
            var tve = new TVEResults
            {
                SliceData = sliceData,
                Factor = factor,
                Total = total,
                Cutoff = (int)(total * factor),
                Mode = mode,
            };

            var sorted = data.OrderByDescending(x => x).ToArray();
            int sum = 0;
            int nToExceed = 0;
            for (int i = 0; i < sorted.Length; i++)
            {
                nToExceed++;
                sum += sorted[i];
                if (sum > tve.Cutoff) break;
            }
            tve.Count = nToExceed;
            tve.Percentage = nToExceed * 100.0 / sliceData.Count;
            return tve;
        }
    }
}
