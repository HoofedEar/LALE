using GBHL;
using System.Drawing;

namespace LALE
{
    internal class MinimapDrawer
    {
        private readonly GBFile gb;
        private readonly Color[] bwPalette = { Color.White, Color.LightGray, Color.FromArgb(44, 50, 89), Color.Black };
        private readonly Color[] chestPalette = { Color.FromArgb(248, 248, 168), Color.FromArgb(216, 168, 32), Color.FromArgb(136, 80, 0), Color.Black };
        private readonly Color[,] palette = new Color[8, 4];
        public byte[] minimapGraphics = new byte[64];
        public byte[] roomIndexes = new byte[64];
        public byte[] overworldPal = new byte[256];

        public MinimapDrawer(GBFile g)
        {
            gb = g;
        }

        public Bitmap DrawDungeonTiles(byte[, ,] graphicsData)
        {
            var bmp = new Bitmap(128, 128);
            var fp = new FastPixel(bmp)
            {
                rgbValues = new byte[128 * 128 * 4]
            };
            fp.Lock();
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    var miniD = minimapGraphics[x + (y * 8)];
                    for (var y1 = 0; y1 < 8; y1++)
                    {
                        for (var x1 = 0; x1 < 8; x1++)
                        {
                            switch (miniD)
                            {
                                //Regular room
                                case 0xEF:
                                    fp.SetPixel(x1 + (x * 8), y1 + (y * 8), bwPalette[graphicsData[2, x1, y1]]);
                                    break;
                                //Empty room
                                case 0x7D:
                                    fp.SetPixel(x1 + (x * 8), y1 + (y * 8), Color.FromArgb(44, 50, 89));
                                    break;
                                //Chest room
                                case 0xED:
                                    fp.SetPixel(x1 + (x * 8), y1 + (y * 8), chestPalette[graphicsData[0, x1, y1]]);
                                    break;
                                //Boss room
                                case 0xEE:
                                    fp.SetPixel(x1 + (x * 8), y1 + (y * 8), bwPalette[graphicsData[1, x1, y1]]);
                                    break;
                            }
                        }
                    }
                }
            }
            fp.Unlock(true);
            return bmp;
        }

        public void LoadMinimapDData(byte dungeon)
        {
            minimapGraphics = gb.ReadBytes(0xA49A + (64 * dungeon), 64);
            if (dungeon == 0xFF)
                minimapGraphics = gb.ReadBytes(0xA49A + (64 * 0x9), 64);
            roomIndexes = gb.ReadBytes(0x50220 + (64 * dungeon), 64);
            if (dungeon == 0xFF)
                roomIndexes = gb.ReadBytes(0x504E0, 64);
        }

        public byte[, ,] LoadMinimapDungeon()
        {
            var tiles = gb.ReadBytes(0xCBFD0, 0x30);
            var data = new byte[3, 16, 16];
            gb.ReadTiles(3, 1, tiles, ref data);
            return data;
        }

        public byte[, ,] LoadMinimapOverworld()
        {
            minimapGraphics = gb.ReadBytes(0x81697, 0x100);
            gb.BufferLocation = 0x81797;
            for (var b = 0; b < 256; b++)
                overworldPal[b] = gb.ReadByte();
            gb.BufferLocation = 0x8786E;
            for (var i = 0; i < 8; i++)
            {
                for (var k = 0; k < 4; k++)
                {
                    palette[i, k] = GetColor(gb.BufferLocation);
                }
            }
            var tiles = gb.ReadBytes(0xB3800, 0x800);
            var data = new byte[128, 16, 16];
            gb.ReadTiles(16, 8, tiles, ref data);
            return data;
        }

        private Color GetColor(int offset)
        {
            var value = gb.ReadByte(offset) + (gb.ReadByte(offset + 1) << 8);
            var color2B = (ushort)value;
            var red = (color2B & 31) << 3;
            color2B >>= 5;
            var green = (color2B & 31) << 3;
            color2B >>= 5;
            var blue = (color2B & 31) << 3;
            return Color.FromArgb(red, green, blue);
        }

        public Bitmap DrawOverworldTiles(byte[, ,] graphicsData)
        {
            var bmp = new Bitmap(128, 128);
            var fp = new FastPixel(bmp)
            {
                rgbValues = new byte[128 * 128 * 4]
            };
            fp.Lock();
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var miniD = minimapGraphics[x + (y * 16)];
                    byte i;
                    if (miniD is 0xFF or 0xFE or 0xFD or 0xFC or 0xFB or 0xFA)
                        i = (byte)(miniD - 0xF0);
                    else
                        i = (byte)(miniD + 16);
                    for (var y1 = 0; y1 < 8; y1++)
                    {
                        for (var x1 = 0; x1 < 8; x1++)
                            fp.SetPixel(x1 + (x * 8), y1 + (y * 8), palette[overworldPal[x + (y * 16)], graphicsData[i, x1, y1]]);
                    }
                }
            }
            fp.Unlock(true);
            return bmp;
        }
    }
}
