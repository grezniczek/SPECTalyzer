using Dicom;
using Dicom.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SPECTalyzer
{
    class RawData
    {
        public string SourceFileName { get; private set; }
        public string SourcePath { get; private set; }
        public ushort[] Data { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FrameCount { get; private set; }
        public int PixelsPerFrame { get; private set; }
        public int Size { get; private set; }

        private RawData()
        {
            // Instantiate via static generator.
        }

        public string DumpPixelData()
        {
            StringBuilder sb = new StringBuilder();
            Data.Each(x => sb.AppendLine(x.ToString()));
            return sb.ToString();
        }

        public static RawData FromDicom(string filename)
        {
            RawData raw = new RawData
            {
                SourceFileName = Path.GetFileName(filename),
                SourcePath = filename,
            };

            try
            {
                var dicomImage = new DicomImage(raw.SourcePath);
                raw.Width = dicomImage.Width;
                raw.Height = dicomImage.Height;
                raw.FrameCount = dicomImage.NumberOfFrames;
                raw.PixelsPerFrame = raw.Width * raw.Height;
                raw.Size = raw.PixelsPerFrame * raw.FrameCount;
                raw.Data = new ushort[raw.Size];

                byte[] buffer = dicomImage.Dataset.GetDicomItem<DicomElement>(DicomTag.PixelData).Buffer.Data;

                for (int frameIdx = 0; frameIdx < raw.FrameCount; frameIdx++)
                {
                    int dataOffset = frameIdx * raw.PixelsPerFrame;
                    int sourceOffset = dataOffset * 2;
                    for (int i = 0; i < raw.PixelsPerFrame; i++)
                    {
                        int bufferOffset = sourceOffset + i * 2;
                        byte low = buffer[bufferOffset];
                        byte high = buffer[bufferOffset + 1];
                        raw.Data[dataOffset + i] = (ushort)(high * 256 + low);
                    }
                }
            }
            catch
            {
                return null;
            }
            return raw;
        }

    }
}
