using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GBHL;

namespace LALE;

internal class Sprites
{
    private readonly GBFile gb;
    public int objectAddress;
    private byte[] spriteData;
    public List<LAObject> spriteList = new();
    public int[] pointers;
    public List<int> unSortedPointers;
    private byte[] spriteInfo;
    private byte[] spriteGraphics = new byte[400];

    public Sprites(GBFile g)
    {
        gb = g;
    }

    public void LoadObjects(bool overworld, byte dungeon, byte map)
    {
        spriteList = new List<LAObject>();
        if (overworld)
            gb.BufferLocation = 0x58000;
        else
        {
            gb.BufferLocation = dungeon switch
            {
                >= 6 and < 0x1A => 0x58400,
                0xFF => 0x58600,
                _ => 0x58200
            };
        }
        gb.BufferLocation = gb.Get3BytePointerAddress((byte)(gb.BufferLocation / 0x4000), gb.BufferLocation + map * 2).Address;
        objectAddress = gb.BufferLocation;
        byte b;
        while ((b = gb.ReadByte()) != 0xFF) //0xFE = End of room
        {
            var ob = new LAObject
            {
                y = (byte)(b >> 4),
                x = (byte)(b & 0xF),
                id = gb.ReadByte()
            }; // 2-Byte tiles
            spriteList.Add(ob);
        }
        spriteData = new byte[80];
        foreach (var obj in spriteList.Where(obj => obj.y <= 7).Where(obj => obj.x <= 9))
        {
            spriteData[obj.x + obj.y * 10] = obj.id;
        }
    }

    public Bitmap DrawSprites(Bitmap map)
    {
        var fp = new FastPixel(map)
        {
            rgbValues = new byte[160 * 128 * 4]
        };
        fp.Lock();
        foreach (var obj in spriteList.Where(obj => obj.x < 9 && obj.y < 8))
        {
            for (var yy = 0; yy < 16; yy++)
            {
                for (var xx = 0; xx < 16; xx++)
                {
                    fp.SetPixel(obj.x * 16 + xx, obj.y * 16 + yy, Color.Black);
                }
            }
        }
        fp.Unlock(true);
        return map;
    }

    public void DrawSelectedSprite(Image map, LAObject selected)
    {
        var b = (Bitmap)map;
        var fp = new FastPixel(b)
        {
            rgbValues = new byte[160 * 128 * 4]
        };
        fp.Lock();
        if (selected.x > 9 || selected.y > 7)
        {
            fp.Unlock(true);
            return;
        }
        for (var yy = 0; yy < 16; yy++)
        {
            for (var xx = 0; xx < 16; xx++)
            {
                var x = selected.x * 16;
                var y = selected.y * 16;
                if (xx is 0 or 15)
                {
                    switch (xx)
                    {
                        case 0:
                        //    fp.SetPixel(x + (xx + 1), y + yy, Color.White);
                        case 15:
                            //  fp.SetPixel(x + (xx - 1), y + yy, Color.White);
                            fp.SetPixel(x + xx, y + yy, Color.White);
                            break;
                    }
                }
                else
                {
                    switch (yy)
                    {
                        case 0:
                        //    fp.SetPixel(x + xx, y + (yy + 1), Color.White);
                        //     if (xx == 0)
                        //          fp.SetPixel(x + (xx - 1), y + yy, Color.White);
                        //     else if (xx == 15)
                        //       fp.SetPixel(x + (xx + 1), y + yy, Color.White);
                        case 15:
                            //      fp.SetPixel(x + xx, y + (yy - 1), Color.White);
                            fp.SetPixel(x + xx, y + yy, Color.White);
                            //      if (xx == 0)
                            //         fp.SetPixel(x + (xx - 1), y + yy, Color.White);
                            //     else if (xx == 15)
                            //         fp.SetPixel(x + (xx + 1), y + yy, Color.White);
                            break;
                    }
                }
            }
        }
        fp.Unlock(true);
    }

    public int GetUsedSpace()
    {
        return spriteList.Sum(_ => 2);
    }

    public int GetFreeSpace(bool overworld, byte mapData, byte dungeon)
    {
        unSortedPointers = new List<int>();
        pointers = new int[256];
        var cMapPointer = 0;
        var map = 0;
        int space;
        while (map < 256)
        {
            if (overworld)
                gb.BufferLocation = 0x58000;
            else
            {
                gb.BufferLocation = dungeon switch
                {
                    >= 6 and < 0x1A => 0x58400,
                    0xFF => 0x58600,
                    _ => 0x58200
                };
            }
            gb.BufferLocation = gb.Get3BytePointerAddress((byte)(gb.BufferLocation / 0x4000), gb.BufferLocation + map * 2).Address;
            if (map == mapData)
                cMapPointer = gb.BufferLocation;
            pointers[map] = gb.BufferLocation;
            map++;
        }
        foreach (var point in pointers)
            unSortedPointers.Add(point);
        Array.Sort(pointers);
        var index = Array.IndexOf(pointers, cMapPointer);
        if (mapData == 0xFF)
        {
            gb.BufferLocation = cMapPointer;
            
            if (overworld)
                space = 0x59663 - cMapPointer;
            else if (dungeon is >= 0x1A or < 6)
                space = 0x58CA3 - cMapPointer;
            else
                space = 0x59185 - cMapPointer;

        }
        else
        {
            while ((int)pointers.GetValue(index + 1) == cMapPointer)
                index++;
            space = (int)pointers.GetValue(index + 1) - 1 - cMapPointer;
        }
        return space;
    }

    public void LoadSpriteBanks(bool overWorld, byte dungeon, byte map)
    {

        gb.BufferLocation = 0x830DB + map;
        if (dungeon is >= 6 and < 0x1A)
            gb.BufferLocation = 0x831DB + map;
        var b = gb.ReadByte();

        if (!overWorld)
        {
            if (dungeon == 0x10 && map == 0xB5)
                b = 0x3D;
        }
        //else
        //{
        //0x0DBD Look into??
        //}

        var a = b << 2;
        if (dungeon == 0xFF) return;
        gb.BufferLocation = 0x833FB + a;
        if (!overWorld)
            gb.BufferLocation = 0x836FB + a;

        var i = 0;
        var h = gb.ReadByte();
        spriteInfo = new byte[4];
                
        while (i < 4)
        {
            if (h == 0xFF)
                h = 0;
            spriteInfo[i] = h;
            h = gb.ReadByte();
            i++;                   
        }
    }

    public void GetSpriteLocation(bool overworld, bool sideScrolling, byte dungeon, byte map)
    {
        spriteGraphics = new byte[400];
        var skip = 0;

        for (var i = 0; i < 4; i++)
        {
            var b1 = spriteInfo[i];

            if (b1 != 0)
            {
                int b = (byte)(b1 & 0x3F);
                var bank = (((b1 & 0xF) * 0x10 + (b1 >> 4)) >> 2) & 3;

                gb.BufferLocation = 0x8262E + bank;
                var b2 = gb.ReadByte();
                if (b2 != 0)
                {
                    bank |= 20;
                }
                gb.BufferLocation = bank * 0x4000 + b * 0x100;

                for (var h = 0; h < 100; h++)
                {
                    spriteGraphics[h + skip] = gb.ReadByte();
                }
            }
            else
                skip += 100;
        }
    }
}