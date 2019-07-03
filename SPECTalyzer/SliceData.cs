using System;
using System.Linq;

namespace SPECTalyzer
{
    class SliceData
    {
        private readonly string _separator = "\t";
        public RawData RawData { get; private set; }

        public int[] Sums;
        public int[] PixelsAboveThreshold;
        public int[] Mins;
        public int[] Maxs;
        public int[] RollingSums;
        public int[] RollingCounts;
        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }

        public int RollingSumMaxStartIndex { get; private set; }
        public int RollingCountMaxStartIndex { get; private set; }
        public int RollingSumMinValue { get; private set; }
        public int RollingSumMaxValue { get; private set; }
        public int RollingCountMinValue { get; private set; }
        public int RollingCountMaxValue { get; private set; }

        public int RollingWidth { get; private set; }
        public double ThresholdPercent { get; private set; }
        public int Count { get; private set; }

        private SliceData()
        {
            // Instantiate via static generator.
        }

        public ushort[] DataBySum
        {
            get
            {
                int start = RollingSumMaxStartIndex * RawData.PixelsPerFrame;
                int length = RollingWidth * RawData.PixelsPerFrame;
                return new Span<ushort>(RawData.Data, start, length).ToArray();
            }
        }

        public ushort[] DataByCount
        {
            get
            {
                int start = RollingCountMaxStartIndex * RawData.PixelsPerFrame;
                int length = RollingWidth * RawData.PixelsPerFrame;
                return new Span<ushort>(RawData.Data, start, length).ToArray();
            }
        }

        public string Dump()
        {
            return string.Join(Environment.NewLine,
                DumpHeader(),
                string.Join(Environment.NewLine, Enumerable.Range(0, RawData.FrameCount).Select(i => DumpRow(i)).ToArray()));
        }

        public string DumpHeader()
        {
            return string.Join(_separator, new string[]
            {
                "Slice",
                "Min",
                "Max",
                "Sum",
                "PixelsAboveThreshold",
                "ThresholdFactor",
                "OverallMin",
                "OverallMax"
            });
        }

        public string DumpRow(int row)
        {
            if (row < 0 || row > RawData.FrameCount - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
            return string.Join(_separator, new string[] {
                $"{row}",
                $"{Mins[row]}",
                $"{Maxs[row]}",
                $"{Sums[row]}",
                $"{PixelsAboveThreshold[row]}",
                $"{ThresholdPercent}",
                $"{MinValue}",
                $"{MaxValue}"
            });
        }

        public string DumpRolling()
        {
            return string.Join(Environment.NewLine,
                DumpRollingHeader(),
                string.Join(Environment.NewLine, Enumerable.Range(0, RawData.FrameCount - RollingWidth).Select(i => DumpRollingRow(i)).ToArray()));
        }

        public string DumpRollingHeader()
        {
            return string.Join(_separator, new string[]
            {
                "StartIndex",
                "Sum",
                "MaxSumIndex",
                "Count",
                "MaxCountIndex",
                "RollingWidth"
            });
        }

        public string DumpRollingRow(int row)
        {
            if (row < 0 || row > RawData.FrameCount - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
            return string.Join(_separator, new string[] {
                $"{row}",
                $"{RollingSums[row]}",
                $"{RollingSumMaxStartIndex}",
                $"{RollingCounts[row]}",
                $"{RollingCountMaxStartIndex}",
                $"{RollingWidth}"
            });
        }

        public static SliceData Calculate(RawData raw, double thresholdPercent = 0.1, int rollingWidth = 100)
        {
            var sd = new SliceData
            {
                RawData = raw,
                Sums = new int[raw.FrameCount],
                PixelsAboveThreshold = new int[raw.FrameCount],
                Mins = new int[raw.FrameCount],
                Maxs = new int[raw.FrameCount],
                RollingSums = new int[raw.FrameCount - rollingWidth + 1],
                RollingCounts = new int[raw.FrameCount - rollingWidth + 1],
                RollingWidth = rollingWidth,
                ThresholdPercent = thresholdPercent,
                Count = rollingWidth * raw.PixelsPerFrame,
            };

            var sorted = raw.Data.OrderByDescending(s => s).ToArray();
            sd.MinValue = sorted.Last();
            sd.MaxValue = sorted.First();

            var nonzero = sorted.Where(s => s > 0).ToArray();
            int threshold = nonzero[(int)((1 - thresholdPercent) * nonzero.Length)];

            for (int f = 0; f < raw.FrameCount; f++)
            {
                int min = int.MaxValue;
                int max = int.MinValue;
                int sum = 0;
                int aboveThreshold = 0;

                int frameOffset = raw.PixelsPerFrame * f;
                for (int p = frameOffset; p < frameOffset + raw.PixelsPerFrame; p++)
                {
                    int val = raw.Data[p];
                    if (val > max)
                    {
                        max = val;
                    }
                    else if (val < min)
                    {
                        min = val;
                    }
                    sum += val;
                    if (val > threshold) aboveThreshold++;
                }
                sd.Sums[f] = sum;
                sd.Mins[f] = min;
                sd.Maxs[f] = max;
                sd.PixelsAboveThreshold[f] = aboveThreshold;
            }

            // Rolling Sums
            int rs_sum = 0;
            for (int i = 0; i < rollingWidth; i++)
            {
                rs_sum += sd.Sums[i];
            }
            sd.RollingSums[0] = rs_sum;
            int rs_max = rs_sum;
            int rs_maxIndex = 0;
            for (int i = rollingWidth; i < raw.FrameCount; i++)
            {
                rs_sum = sd.RollingSums[i - rollingWidth] - sd.Sums[i - rollingWidth] + sd.Sums[i];
                sd.RollingSums[i - rollingWidth + 1] = rs_sum;
                if (rs_sum > rs_max)
                {
                    rs_max = rs_sum;
                    rs_maxIndex = i - rollingWidth + 1;
                }
            }
            sd.RollingSumMaxStartIndex = rs_maxIndex;
            sd.RollingSumMinValue = sd.DataBySum.Min();
            sd.RollingSumMaxValue = sd.DataBySum.Max();

            // Rolling Counts
            int rc_sum = 0;
            for (int i = 0; i < rollingWidth; i++)
            {
                rc_sum += sd.PixelsAboveThreshold[i];
            }
            sd.RollingCounts[0] = rc_sum;
            int rc_max = rc_sum;
            int rc_maxIndex = 0;
            for (int i = rollingWidth; i < raw.FrameCount; i++)
            {
                rc_sum = sd.RollingCounts[i - rollingWidth] - sd.PixelsAboveThreshold[i - rollingWidth] + sd.PixelsAboveThreshold[i];
                sd.RollingCounts[i - rollingWidth + 1] = rc_sum;
                if (rc_sum > rc_max)
                {
                    rc_max = rc_sum;
                    rc_maxIndex = i - rollingWidth + 1;
                }
            }
            sd.RollingCountMaxStartIndex = rc_maxIndex;
            sd.RollingCountMinValue = sd.DataByCount.Min();
            sd.RollingCountMaxValue = sd.DataByCount.Max();

            return sd;
        }
    }
}
