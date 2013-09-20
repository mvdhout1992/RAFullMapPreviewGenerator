using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace RAFullMapPreviewGenerator
{
    public class ImageHeader
    {
        public uint Offset;
        public Format Format;

        public uint RefOffset;
        public Format RefFormat;
        public ImageHeader RefImage;

        public byte[] Image;

        public ImageHeader() { }

        public ImageHeader(BinaryReader reader)
        {
            var data = reader.ReadUInt32();
            Offset = data & 0xffffff;
            Format = (Format)(data >> 24);

            RefOffset = reader.ReadUInt16();
            RefFormat = (Format)reader.ReadUInt16();
        }

        public static readonly int SizeOnDisk = 8;

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Offset | ((uint)Format << 24));
            writer.Write((ushort)RefOffset);
            writer.Write((ushort)RefFormat);
        }
    }

    public enum Format { Format20 = 0x20, Format40 = 0x40, Format80 = 0x80 }

    public class ShpReader
    {
        public readonly int ImageCount;
        public readonly ushort Width;
        public readonly ushort Height;

        public Size Size { get { return new Size(Width, Height); } }

        static Dictionary<string, ShpReader> Cache = new Dictionary<string, ShpReader>();

        readonly List<ImageHeader> headers = new List<ImageHeader>();

        int recurseDepth = 0;

        public ShpReader(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                ImageCount = reader.ReadUInt16();
                reader.ReadUInt16();
                reader.ReadUInt16();
                Width = reader.ReadUInt16();
                Height = reader.ReadUInt16();
                reader.ReadUInt32();

                for (int i = 0; i < ImageCount; i++)
                    headers.Add(new ImageHeader(reader));

                new ImageHeader(reader); // end-of-file header
                new ImageHeader(reader); // all-zeroes header


                Dictionary<uint, ImageHeader> offsets = new Dictionary<uint, ImageHeader>();

                foreach (ImageHeader h in headers)
                {
                    offsets.Add(h.Offset, h);
                }


                for (int i = 0; i < ImageCount; i++)
                {
                    var h = headers[i];
                    if (h.Format == Format.Format20)
                        h.RefImage = headers[i - 1];

                    else if (h.Format == Format.Format40)
                        if (!offsets.TryGetValue(h.RefOffset, out h.RefImage))
                            throw new InvalidDataException(string.Format("Reference doesnt point to image data {0}->{1}",h.Offset, h.RefOffset));
                }

                foreach (ImageHeader h in headers)
                    Decompress(stream, h);
            }
        }

        public ImageHeader this[int index]
        {
            get { return headers[index]; }
        }

        void Decompress(Stream stream, ImageHeader h)
        {
            if (recurseDepth > ImageCount)
                throw new InvalidDataException("Format20/40 headers contain infinite loop");

            switch (h.Format)
            {
                case Format.Format20:
                case Format.Format40:
                    {
                        if (h.RefImage.Image == null)
                        {
                            ++recurseDepth;
                            Decompress(stream, h.RefImage);
                            --recurseDepth;
                        }

                        h.Image = CopyImageData(h.RefImage.Image);
                        Format40.DecodeInto(ReadCompressedData(stream, h), h.Image);
                        break;
                    }
                case Format.Format80:
                    {
                        var imageBytes = new byte[Width * Height];
                        Format80.DecodeInto(ReadCompressedData(stream, h), imageBytes);
                        h.Image = imageBytes;
                        break;
                    }
                default:
                    throw new InvalidDataException();
            }
        }

        static byte[] ReadCompressedData(Stream stream, ImageHeader h)
        {
            stream.Position = h.Offset;
            // Actually, far too big. There's no length field with the correct length though :(
            var compressedLength = (int)(stream.Length - stream.Position);

            var compressedBytes = new byte[compressedLength];
            stream.Read(compressedBytes, 0, compressedLength);

            return compressedBytes;
        }

        byte[] CopyImageData(byte[] baseImage)
        {
            var imageData = new byte[Width * Height];
            for (int i = 0; i < Width * Height; i++)
                imageData[i] = baseImage[i];

            return imageData;
        }

        public IEnumerable<ImageHeader> Frames { get { return headers; } }

        public static ShpReader Load(string filename)
        {
            if (ShpReader.Cache.ContainsKey(filename))
            {
                ShpReader Ret = null;
                ShpReader.Cache.TryGetValue(filename, out Ret);
                return Ret;
            }
            else
            {
                var s = File.OpenRead(filename);
                ShpReader res = new ShpReader(s);
                ShpReader.Cache.Add(filename, res);
                return res;
            }
        }
    }
}
