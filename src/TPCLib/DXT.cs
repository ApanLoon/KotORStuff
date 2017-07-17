
using System;
using System.IO;

namespace TPCLib
{
    public static class DXT
    {
        public enum Format
        {
            DXT1,
            DXT3,
            DXT5
        }

        static UInt32 convert565To8888(UInt16 color)
        {
            return (UInt32)(((color & 0x1f) << 11) | ((color & 0x7e0) << 13) | ((color & 0xf800) << 16) | 0xff);
        }

        static UInt32 interpolate32(double weight, UInt32 color_0, UInt32 color_1)
        {
            byte red   = (byte)((1.0f - weight) * (double) (color_0 >> 24)         + weight * (double) (color_1 >> 24));
            byte green = (byte)((1.0f - weight) * (double)((color_0 >> 16) & 0xff) + weight * (double)((color_1 >> 16) & 0xff));
            byte blue  = (byte)((1.0f - weight) * (double)((color_0 >> 8)  & 0xff) + weight * (double)((color_1 >> 8)  & 0xff));
            byte alpha = (byte)((1.0f - weight) * (double)( color_0        & 0xff) + weight * (double)( color_1        & 0xff));
            return (UInt32)(red << 24 | green << 16 | blue << 8 | alpha);
        }

        static UInt32 ReadUInt32BE(BinaryReader reader)
        {
            byte[] buf = reader.ReadBytes(4);
            Array.Reverse(buf);
            return BitConverter.ToUInt32(buf, 0);
        }
        static UInt64 ReadUInt48(BinaryReader reader)
        {
            UInt64 output = reader.ReadUInt32();
            return output | ((UInt64)reader.ReadUInt16() << 32);
        }

        static void WriteUInt32BE(UInt32 i, byte[] buf, UInt32 index)
        {
            byte[] tmp = BitConverter.GetBytes(i);
            buf[index + 0] = tmp[3];
            buf[index + 1] = tmp[2];
            buf[index + 2] = tmp[1];
            buf[index + 3] = tmp[0];
        }
        static void WriteUInt32LE(UInt32 i, byte[] buf, UInt32 index)
        {
            byte[] tmp = BitConverter.GetBytes(i);
            buf[index + 0] = tmp[0];
            buf[index + 1] = tmp[1];
            buf[index + 2] = tmp[2];
            buf[index + 3] = tmp[3];
        }

        class DXT1Texel
        {
            public UInt16 color_0;
            public UInt16 color_1;
            public UInt32 pixels;

            public virtual void Read(BinaryReader reader)
            {
                color_0 = reader.ReadUInt16();
                color_1 = reader.ReadUInt16();
                pixels = ReadUInt32BE(reader);
            }
        };

        class DXT23Texel : DXT1Texel
        {
            public UInt16[] alpha = new UInt16[4];
            public override void Read(BinaryReader reader)
            {
                alpha[0] = reader.ReadUInt16();
                alpha[1] = reader.ReadUInt16();
                alpha[2] = reader.ReadUInt16();
                alpha[3] = reader.ReadUInt16();

                base.Read(reader);
            }
        };

        class DXT45Texel : DXT1Texel
        {
            public byte alpha_0;
            public byte alpha_1;
            public UInt64 alphabl;

            public override void Read(BinaryReader reader)
            {
                alpha_0 = reader.ReadByte();
                alpha_1 = reader.ReadByte();
                alphabl = ReadUInt48(reader);
                base.Read(reader);
            }
        };


        public static void DecompressDXT1(byte[] dest, BinaryReader reader, UInt32 width, UInt32 height, UInt32 pitch)
        {
            for (int ty = (int)height; ty > 0; ty -= 4)
            {
                for (UInt32 tx = 0; tx < width; tx += 4)
                {
                    DXT1Texel tex = new DXT1Texel();
                    tex.Read(reader);
                    UInt32[] blended = new UInt32[4];

                    blended[0] = convert565To8888(tex.color_0);
                    blended[1] = convert565To8888(tex.color_1);

                    if (tex.color_0 > tex.color_1)
                    {
                        blended[2] = interpolate32(0.333333f, blended[0], blended[1]);
                        blended[3] = interpolate32(0.666666f, blended[0], blended[1]);
                    }
                    else
                    {
                        blended[2] = interpolate32(0.5f, blended[0], blended[1]);
                        blended[3] = 0;
                    }

                    UInt32 cpx = tex.pixels;
                    UInt32 blockWidth = (UInt32)Math.Min(width, 4);
                    UInt32 blockHeight = (UInt32)Math.Min(height, 4);

                    for (byte y = 0; y < blockHeight; ++y)
                    {
                        for (byte x = 0; x < blockWidth; ++x)
                        {
                            UInt32 destX = tx + x;
                            UInt32 destY = (UInt32)(height - 1 - (ty - blockHeight + y));

                            UInt32 pixel = blended[cpx & 3];

                            cpx >>= 2;

                            if ((destX < width) && (destY < height))
                            {
                                WriteUInt32BE(pixel, dest, destY * pitch + destX * 4);
                            }
                        }
                    }
                }
            }
        }


        public static void DecompressDXT3(byte[] dest, BinaryReader reader, UInt32 width, UInt32 height, UInt32 pitch)
        {
            for (int ty = (int)height; ty > 0; ty -= 4)
            {
                for (UInt32 tx = 0; tx < width; tx += 4)
                {
                    DXT23Texel tex = new DXT23Texel();
                    tex.Read(reader);

                    UInt32[] blended = new UInt32[4];

                    blended[0] = convert565To8888(tex.color_0) & 0xFFFFFF00;
                    blended[1] = convert565To8888(tex.color_1) & 0xFFFFFF00;
                    blended[2] = interpolate32(0.333333f, blended[0], blended[1]);
                    blended[3] = interpolate32(0.666666f, blended[0], blended[1]);

                    UInt32 cpx = tex.pixels;
                    UInt32 blockWidth = (UInt32)Math.Min(width, 4);
                    UInt32 blockHeight = (UInt32)Math.Min(height, 4);

                    for (byte y = 0; y < blockHeight; ++y)
                    {
                        for (byte x = 0; x < blockWidth; ++x)
                        {
                            UInt32 destX = tx + x;
                            UInt32 destY = (UInt32)(height - 1 - (ty - blockHeight + y));

                            UInt32 alpha = (UInt32)((tex.alpha[y] >> (x * 4)) & 0xf);
                            UInt32 pixel = blended[cpx & 3] | alpha << 4;

                            cpx >>= 2;

                            if ((destX < width) && (destY < height))
                            {
                                WriteUInt32BE(pixel, dest, destY * pitch + destX * 4);
                            }
                        }
                    }
                }
            }
        }

        public static void DecompressDXT5(byte[] dest, BinaryReader reader, UInt32 width, UInt32 height, UInt32 pitch)
        {
            for (int ty = (int)height; ty > 0; ty -= 4)
            {
                for (UInt32 tx = 0; tx < width; tx += 4)
                {
                    UInt32[] blended = new UInt32[4];
                    byte[] alphab = new byte[8];
                    DXT45Texel tex = new DXT45Texel();
                    tex.Read(reader);

                    alphab[0] = tex.alpha_0;
                    alphab[1] = tex.alpha_1;

                    if (tex.alpha_0 > tex.alpha_1)
                    {
                        alphab[2] = (byte)((6.0f * (double)alphab[0] + 1.0f * (double)alphab[1] + 3.0f) / 7.0f);
                        alphab[3] = (byte)((5.0f * (double)alphab[0] + 2.0f * (double)alphab[1] + 3.0f) / 7.0f);
                        alphab[4] = (byte)((4.0f * (double)alphab[0] + 3.0f * (double)alphab[1] + 3.0f) / 7.0f);
                        alphab[5] = (byte)((3.0f * (double)alphab[0] + 4.0f * (double)alphab[1] + 3.0f) / 7.0f);
                        alphab[6] = (byte)((2.0f * (double)alphab[0] + 5.0f * (double)alphab[1] + 3.0f) / 7.0f);
                        alphab[7] = (byte)((1.0f * (double)alphab[0] + 6.0f * (double)alphab[1] + 3.0f) / 7.0f);
                    }
                    else
                    {
                        alphab[2] = (byte)((4.0f * (double)alphab[0] + 1.0f * (double)alphab[1] + 2.0f) / 5.0f);
                        alphab[3] = (byte)((3.0f * (double)alphab[0] + 2.0f * (double)alphab[1] + 2.0f) / 5.0f);
                        alphab[4] = (byte)((2.0f * (double)alphab[0] + 3.0f * (double)alphab[1] + 2.0f) / 5.0f);
                        alphab[5] = (byte)((1.0f * (double)alphab[0] + 4.0f * (double)alphab[1] + 2.0f) / 5.0f);
                        alphab[6] = 0;
                        alphab[7] = 255;
                    }

                    blended[0] = convert565To8888(tex.color_0) & 0xFFFFFF00;
                    blended[1] = convert565To8888(tex.color_1) & 0xFFFFFF00;
                    blended[2] = interpolate32(0.333333f, blended[0], blended[1]);
                    blended[3] = interpolate32(0.666666f, blended[0], blended[1]);

                    UInt32 cpx = tex.pixels;
                    UInt32 blockWidth = (UInt32)Math.Min(width, 4);
                    UInt32 blockHeight = (UInt32)Math.Min(height, 4);

                    for (byte y = 0; y < blockHeight; ++y)
                    {
                        for (byte x = 0; x < blockWidth; ++x)
                        {
                            UInt32 destX = tx + x;
                            UInt32 destY = (UInt32)(height - 1 - (ty - blockHeight + y));

                            UInt32 alpha = alphab[(tex.alphabl >> (3 * (4 * (3 - y) + x))) & 7];
                            UInt32 pixel = blended[cpx & 3] | alpha;

                            cpx >>= 2;

                            if ((destX < width) && (destY < height))
                            {
                                WriteUInt32BE(pixel, dest, destY * pitch + destX * 4);
                            }
                        }
                    }
                }
            }
        }
    }
}
