using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace RAFullMapPreviewGenerator
{
    static class RenderUtils
    {
        static Dictionary<ShpReader, Bitmap[]> ShpFramesCache = new Dictionary<ShpReader, Bitmap[]>();
        static Dictionary<TemplateReader, Bitmap> TemplateCache = new Dictionary<TemplateReader, Bitmap>();
        static Dictionary<TemplateReader, Bitmap[]> TemplateTileCache = new Dictionary<TemplateReader, Bitmap[]>();


        static public Bitmap RenderShp(ShpReader shp, Palette p, int Frame_)
        {
            var frame = shp[Frame_];

            var bitmap = new Bitmap(shp.Width, shp.Height, PixelFormat.Format8bppIndexed);

            bitmap.Palette = p.AsSystemPalette();

            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            unsafe
            {
                byte* q = (byte*)data.Scan0.ToPointer();
                var stride2 = data.Stride;

                for (var i = 0; i < shp.Width; i++)
                    for (var j = 0; j < shp.Height; j++)
                        q[j * stride2 + i] = frame.Image[i + shp.Width * j];
            }

            bitmap.UnlockBits(data);

            return bitmap;
        }

        static public Bitmap RenderShpBibs(ShpReader shp, Palette p, int CellX, int CellY)
        {
            if (shp.ImageCount == 1) return RenderUtils.RenderShp(shp, p, 0);

            var bitmap = new Bitmap(TemplateReader.TileSize * shp.Width, TemplateReader.TileSize * shp.Height);

            Graphics g = Graphics.FromImage(bitmap);

            int Frame = 0;
            for (int y = 0; y < CellY; y++)
            {
                for (int x = 0; x < CellX; x++)
                {
                    Bitmap StructBitmap = RenderUtils.RenderShp(shp, p, Frame);

                    g.DrawImage(bitmap, CellX * TemplateReader.TileSize, CellY * TemplateReader.TileSize, StructBitmap.Width, StructBitmap.Height);

                    Frame++;
                }
                
            }
            return bitmap;
        }

        public static Bitmap RenderTemplate(TemplateReader template, Palette p, int frame)
        {
            if (RenderUtils.TemplateTileCache.ContainsKey(template))
            {
                Bitmap[] Frames = null;
                RenderUtils.TemplateTileCache.TryGetValue(template, out Frames);

                if (Frames[frame] != null)
                {
                    return Frames[frame];
                }
            }
            else
            {
                Bitmap[] TemplateBitmaps = new Bitmap[50];
                RenderUtils.TemplateTileCache.Add(template, TemplateBitmaps);
            }

            var bitmap = new Bitmap(TemplateReader.TileSize, TemplateReader.TileSize,
                PixelFormat.Format8bppIndexed);

            bitmap.Palette = p.AsSystemPalette();

            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            unsafe
            {
                byte* q = (byte*)data.Scan0.ToPointer();
                var stride = data.Stride;

                        if (template.TileBitmapBytes[frame] != null)
                        {
                            var rawImage = template.TileBitmapBytes[frame];
                            for (var i = 0; i < TemplateReader.TileSize; i++)
                                for (var j = 0; j < TemplateReader.TileSize; j++)
                                    q[j * stride + i] = rawImage[i + TemplateReader.TileSize * j];
                        }
                        else
                        {
                            for (var i = 0; i < TemplateReader.TileSize; i++)
                                for (var j = 0; j < TemplateReader.TileSize; j++)
                                    q[j * stride + i] = 0;
                        }
            }

            bitmap.UnlockBits(data);

            Bitmap[] TemplateArray = null;
            TemplateTileCache.TryGetValue(template, out TemplateArray);
            TemplateArray[frame] = bitmap;

            return bitmap;
        }

        public static Bitmap RenderTemplate(TemplateReader template, Palette p)
        {
            if (RenderUtils.TemplateCache.ContainsKey(template))
            {
                Bitmap Ret = null;
                RenderUtils.TemplateCache.TryGetValue(template, out Ret);
                return Ret;
            }

            var bitmap = new Bitmap(TemplateReader.TileSize * template.Width, TemplateReader.TileSize * template.Height,
                PixelFormat.Format8bppIndexed);

            bitmap.Palette = p.AsSystemPalette();

            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            unsafe
            {
                byte* q = (byte*)data.Scan0.ToPointer();
                var stride = data.Stride;

                for (var u = 0; u < template.Width; u++)
                    for (var v = 0; v < template.Height; v++)
                        if (template.TileBitmapBytes[u + v * template.Width] != null)
                        {
                            var rawImage = template.TileBitmapBytes[u + v * template.Width];
                            for (var i = 0; i < TemplateReader.TileSize; i++)
                                for (var j = 0; j < TemplateReader.TileSize; j++)
                                    q[(v * TemplateReader.TileSize + j) * stride + u * TemplateReader.TileSize + i] = rawImage[i + TemplateReader.TileSize * j];
                        }
                        else
                        {
                            for (var i = 0; i < TemplateReader.TileSize; i++)
                                for (var j = 0; j < TemplateReader.TileSize; j++)
                                    q[(v * TemplateReader.TileSize + j) * stride + u * TemplateReader.TileSize + i] = 0;
                        }
            }

            bitmap.UnlockBits(data);

            TemplateCache.Add(template, bitmap);

            return bitmap;
        }
        public static void Clear_Caches()
        {
            ShpFramesCache.Clear();
            TemplateCache.Clear();
            TemplateTileCache.Clear();
        }

    }
}
