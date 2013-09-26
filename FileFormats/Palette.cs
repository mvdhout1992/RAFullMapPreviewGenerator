#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 * 
 * Additional modifications were made to this code by Iran.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System;
using System.Reflection;

namespace RAFullMapPreviewGenerator
{
    public class Palette
    {
        uint[] colors;
        public Color GetColor(int index)
        {
            return Color.FromArgb((int)colors[index]);
        }

        public void SetColor(int index, Color color)
        {
            colors[index] = (uint)color.ToArgb();
        }

        public void SetColor(int index, uint color)
        {
            colors[index] = (uint)color;
        }

        public uint[] Values
        {
            get { return colors; }
        }

        public Palette(Stream s, int[] remapShadow)
        {
            colors = new uint[256];

            using (BinaryReader reader = new BinaryReader(s))
            {
                for (int i = 0; i < 256; i++)
                {
                    byte r = (byte)(reader.ReadByte() << 2);
                    byte g = (byte)(reader.ReadByte() << 2);
                    byte b = (byte)(reader.ReadByte() << 2);
                    colors[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
                }
            }

            colors[0] = 0; //convert black background to transparency
            foreach (int i in remapShadow)
                colors[i] = 140u << 24;
        }

        public Palette(Palette p, IPaletteRemap r)
        {
            colors = new uint[256];
            for (int i = 0; i < 256; i++)
                colors[i] = (uint)r.GetRemappedColor(Color.FromArgb((int)p.colors[i]), i).ToArgb();
        }

        public Palette(Palette p)
        {
            colors = (uint[])p.colors.Clone();
        }

        public void Set_RGB(int index, RGB rgb)
        {
            colors[index] = Value_From_RGB(rgb);
        }

        public ColorPalette AsSystemPalette()
        {
            ColorPalette pal;
            using (var b = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
                pal = b.Palette;

            for (var i = 0; i < 256; i++)
                pal.Entries[i] = GetColor(i);

            // hack around a mono bug -- the palette flags get set wrong.
            if (Platform.CurrentPlatform != PlatformType.Windows)
                typeof(ColorPalette).GetField("flags",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pal, 1);

            return pal;
        }

        public static Palette Load(string filename, int[] remap)
        {
            using (var s = File.OpenRead(filename))
                return new Palette(s, remap);
        }

        public static Palette Load_With_Remaps(string filename, int[] shadowreamp, RGB[] remaps)
        {
            if (remaps.Length != 16) throw new ArgumentException("remaps int array needs to have size of 16..");

            using (var s = File.OpenRead(filename))
            {
                Palette p = new Palette(s, shadowreamp);
                p.colors[80] = Value_From_RGB(remaps[0]);
                p.colors[81] = Value_From_RGB(remaps[1]);
                p.colors[82] = Value_From_RGB(remaps[2]);
                p.colors[83] = Value_From_RGB(remaps[3]);
                p.colors[84] = Value_From_RGB(remaps[4]);
                p.colors[85] = Value_From_RGB(remaps[5]);
                p.colors[86] = Value_From_RGB(remaps[6]);
                p.colors[87] = Value_From_RGB(remaps[7]);
                p.colors[88] = Value_From_RGB(remaps[8]);
                p.colors[89] = Value_From_RGB(remaps[9]);
                p.colors[90] = Value_From_RGB(remaps[10]);
                p.colors[91] = Value_From_RGB(remaps[11]);
                p.colors[92] = Value_From_RGB(remaps[12]);
                p.colors[93] = Value_From_RGB(remaps[13]);
                p.colors[94] = Value_From_RGB(remaps[14]);
                p.colors[95] = Value_From_RGB(remaps[15]);

                return p;
            }
        }

        public static uint Value_From_RGB(RGB rgb)
        {
            byte r = rgb.R;
            byte g = rgb.G;
            byte b = rgb.B;
            return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
        }
    }

    public struct RGB
    {
        public byte R;
        public byte G;
        public byte B;

        public RGB(byte R_, byte G_, byte B_)
        {
            R = R_;
            G = G_;
            B = B_;
        }
    }

    public interface IPaletteRemap { Color GetRemappedColor(Color original, int index); }
}
