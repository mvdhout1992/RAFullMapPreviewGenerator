using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RAFullMapPreviewGenerator
{
    class TemplateReader
    {
        public readonly List<byte[]> TileBitmapBytes = new List<byte[]>();
        public static int TileSize = 24;
        static Dictionary<string, TemplateReader> Cache = new Dictionary<string, TemplateReader>();
        public int Width;
        public int Height;

		public TemplateReader( Stream stream, int size )
		{
			// Try loading as a cnc .tem
			BinaryReader reader = new BinaryReader( stream );
            int Width_ = reader.ReadUInt16();
            int Height_ = reader.ReadUInt16();


			if( Width_ != size || Height_ != size )
				throw new InvalidDataException(string.Format( "{0}x{1} != {2}x{2}", Width_, Height_, size ) );

            /*NumTiles = */
            reader.ReadUInt16();
            /*Zero1 = */
            reader.ReadUInt16();
            /*uint Size = */
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            uint ImgStart = reader.ReadUInt32();
            /*Zero2 = */
            reader.ReadUInt32();

			int IndexEnd, IndexStart;
			if (reader.ReadUInt16() == 65535) // ID1 = FFFFh for cnc
			{
				/*ID2 = */reader.ReadUInt16();
				IndexEnd = reader.ReadInt32();
				IndexStart = reader.ReadInt32();
			}
			else // Load as a ra .tem
			{
				stream.Position = 0;
				reader = new BinaryReader( stream );
				Width_ = reader.ReadUInt16();
				Height_ = reader.ReadUInt16();


				/*NumTiles = */reader.ReadUInt16();
				reader.ReadUInt16();
				/*XDim = */reader.ReadUInt16();
				/*YDim = */reader.ReadUInt16();
				/*uint FileSize = */reader.ReadUInt32();
				ImgStart = reader.ReadUInt32();
				reader.ReadUInt32();
				reader.ReadUInt32();
				IndexEnd = reader.ReadInt32();
				reader.ReadUInt32();
				IndexStart = reader.ReadInt32();
			}
			stream.Position = IndexStart;

			foreach( byte b in new BinaryReader(stream).ReadBytes(IndexEnd - IndexStart) )
			{
				if (b != 255)
				{
					stream.Position = ImgStart + b * size * size;
					TileBitmapBytes.Add(new BinaryReader(stream).ReadBytes(size * size));
				}
				else
					TileBitmapBytes.Add(null);
			}
		}
        public static TemplateReader Load(string filename)
        {
            if (TemplateReader.Cache.ContainsKey(filename))
            {
               TemplateReader Ret = null;
                TemplateReader.Cache.TryGetValue(filename, out Ret);
                return Ret;
            }
            else
            {
                var s = File.OpenRead(filename);
                TemplateReader res = new TemplateReader(s, TileSize);
                TemplateReader.Cache.Add(filename, res);
                return res;
            }
        }
	}
}
