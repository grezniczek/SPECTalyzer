using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DICOMAnalyzer
{
    class Program
    {
        static int Main(string[] args)
        {
            var lcArgs = args.Select(arg => arg.ToLower());
            if (args.Length == 0 || lcArgs.Any(arg => arg == "/?" || arg == "help"))
            {
                Console.WriteLine($"Usage: dotnet {Assembly.GetEntryAssembly().GetName().Name} commands DICOM.file");
                Console.WriteLine("Valid commands are: PixelData SliceData Rolling TVEbySum TVEbyCount Histo");
                Console.WriteLine("Call with --version to get version info");
                return 0;
            }

            if (lcArgs.Any(arg => arg == "--version"))
            {
                Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version);
                return 0;
            }

            // Check if last arg is a valid file.
            var path = Path.GetFullPath(args.LastOrDefault() ?? @"\");
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                Console.WriteLine("No valid path supplied. Aborting.");
                return 1;
            }

            // Read and parse the DICOM file.
            var raw = RawData.FromDicom(fileInfo.FullName);
            if (raw == null)
            {
                Console.WriteLine("Failed to parse DICOM file. Aborting.");
                return 1;
            }

            // Output raw pixel data?
            if (lcArgs.Any(arg => arg == "pixeldata"))
            {
                Console.WriteLine(raw.DumpPixelData());
            }

            var sd = SliceData.Calculate(raw);

            // Output slice data?
            if (lcArgs.Any(arg => arg == "slicedata"))
            {
                Console.WriteLine(sd.Dump());
            }

            // Output rolling data?
            if (lcArgs.Any(arg => arg == "rolling"))
            {
                Console.WriteLine(sd.DumpRolling());
            }

            // Calculate and output TVE by sums?
            if (lcArgs.Any(arg => arg == "tvebysum"))
            {
                var tveSum = TVEResults.Calculate(sd);
                Console.WriteLine(tveSum.Dump());
            }

            // Calculate and output TVE by counts?
            if (lcArgs.Any(arg => arg == "tvebycount"))
            {
                var tveCount = TVEResults.Calculate(sd, mode: CalcMode.ByCount);
                Console.WriteLine(tveCount.Dump());
            }

            // Calculate and output histogram data?
            if (lcArgs.Any(arg => arg == "histo"))
            {
                var histo = Histogram.Calculate(sd);
                Console.WriteLine(histo.Dump());
            }

#if DEBUG
            Console.ReadKey();
#endif
            return 0;
        }
    }
}