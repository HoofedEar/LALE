using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GBHL;

namespace LALE;

internal class OverworldDrawer
{
    private readonly GBFile gb;
    private byte cMap;
    public int mapAddress;
    public byte[] mapData;
    public int[] pointers;
    public List<int> unSortedPointers;
    private bool s;
    private LAObject overworldObjects = new();
    public List<LAObject> objects = new();
    public List<Warps> warps = new();
    public byte floor;
    public byte music;
    private bool collision;
    public byte wall;
    public byte spriteBank;

    public OverworldDrawer(GBFile g)
    {
        gb = g;
    }

    public void GetMusic(byte map)
    {
        gb.BufferLocation = 0x8000 + map;
        music = gb.ReadByte();
    }

    public byte[] ReadMap(byte map, bool special)
    {
        cMap = map;
        mapData = new byte[80];
        s = special;
        collision = false;
        //c = collision;
        GetOffsetOverworld();
        mapAddress = gb.BufferLocation;
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                mapData[x + (y * 10)] = gb.ReadByte();
            }
        }

        return mapData;
    }

    private void GetOffsetOverworld()
    {
        var i = -1;
        if (s)
        {
            i = cMap switch
            {
                0x06 => 0x5040,
                0x0E => 0x5090,
                0x1B => 0x50E0,
                0x2B => 0x5130,
                0x79 => 0x5180,
                0x8C => 0x51D0,
                _ => i
            };
        }

        if (i > 0)
        {
            gb.BufferLocation = 0x98000 + i;
            return;
        }

        if (cMap < 0xCC)
        {
            gb.BufferLocation = 0x98000 + 0x50 * cMap;
            return;
        }

        gb.BufferLocation = 0x9C000 + 0x50 * (cMap - 0xCC);
    }

    public Bitmap DrawMap(Bitmap tiles, byte[] map, bool borders)
    {
        var bmp = new Bitmap(160, 128);
        var fp = new FastPixel(bmp);
        var src = new FastPixel(tiles);
        fp.rgbValues = new byte[160 * 128 * 4];
        src.rgbValues = new byte[256 * 256 * 4];
        fp.Lock();
        src.Lock();
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                var v = map[x + (y * 10)];

                for (var yy = 0; yy < 16; yy++)
                {
                    for (var xx = 0; xx < 16; xx++)
                    {
                        fp.SetPixel(x * 16 + xx, y * 16 + yy, src.GetPixel((v % 16) * 16 + xx, (v / 16) * 16 + yy));
                    }
                }
            }
        }

        fp.Unlock(true);
        src.Unlock(true);

        if (borders && collision)
            DrawBorders(bmp);
        //drawSelectedObject(bmp, selected);
        return bmp;
    }

    private void DrawBorders(Bitmap image)
    {
        var fp = new FastPixel(image)
        {
            rgbValues = new byte[160 * 128 * 4]
        };
        fp.Lock();
        foreach (var obj in objects)
        {
            var border = obj.is3Byte switch
            {
                true when !obj.special && obj.direction == 8 => Color.DarkRed,
                true when !obj.special && obj.direction == 0xC => Color.DarkBlue,
                true when obj.special && obj.direction == 8 => Color.DarkGoldenrod,
                true when obj.special && obj.direction == 0xC => Color.Purple,
                false when !obj.special => Color.DarkGreen,
                _ => Color.DeepPink
            };
            var x = obj.x * 16;
            var y = obj.y * 16;
            if (obj.hFlip)
                x = -16;
            if (obj.vFlip)
                y = -16;
            var v = false;
            var h = false;
            if (!obj.is3Byte && !obj.special)
            {
                if (obj.x > 9 || obj.y > 7)
                    continue;
            }

            if (!obj.is3Byte)
            {
                if (!obj.special)
                {
                    for (var yy = 0; yy < 16; yy++)
                    {
                        for (var xx = 0; xx < 16; xx++)
                        {
                            if (yy > 0 && yy != 15)
                            {
                                if (xx is 0 or 15)
                                {
                                    fp.SetPixel(x + xx, y + yy, border);
                                }
                            }
                            else
                            {
                                fp.SetPixel(x + xx, y + yy, border);
                            }
                        }
                    }
                }
                else
                {
                    for (var yy = 0; yy < obj.h * 16; yy++)
                    {
                        for (var xx = 0; xx < obj.w * 16; xx++)
                        {
                            if (x < 0 && !h)
                            {
                                xx += 16;
                                h = true;
                            }

                            if (y < 0 && !v)
                            {
                                yy += 16;
                                v = true;
                            }

                            if (x + xx >= 160 || x + xx < 0)
                                continue;
                            if (y + yy >= 128 || y + yy < 0)
                                continue;
                            if (yy > 0 && yy != ((obj.h * 16) - 1))
                            {
                                if (xx == 0 || xx == (obj.w * 16) - 1)
                                    fp.SetPixel(x + xx, y + yy, border);
                            }
                            else
                                fp.SetPixel(x + xx, y + yy, border);
                        }
                    }
                }
            }
            else
            {
                if (!obj.special)
                {
                    if (obj.direction == 8)
                    {
                        for (var yy = 0; yy < 16; yy++)
                        {
                            for (var xx = 0; xx < (obj.length * 16); xx++)
                            {
                                if (x < 0 && !h)
                                {
                                    xx += 16;
                                    h = true;
                                }

                                if (y < 0 && !v)
                                {
                                    yy += 16;
                                    v = true;
                                }

                                if (x + xx >= 160 || x + xx < 0)
                                    continue;
                                if (y + yy >= 128 || y + yy < 0)
                                    continue;
                                if (yy > 0 && yy != 15)
                                {
                                    if (xx == 0 || xx == (obj.length * 16) - 1)
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                                else
                                    fp.SetPixel(x + xx, y + yy, border);
                            }
                        }
                    }
                    else
                    {
                        for (var yy = 0; yy < obj.length * 16; yy++)
                        {
                            for (var xx = 0; xx < 16; xx++)
                            {
                                if (x < 0 && !h)
                                {
                                    xx += 16;
                                    h = true;
                                }

                                if (y < 0 && !v)
                                {
                                    yy += 16;
                                    v = true;
                                }

                                if (x + xx >= 160 || x + xx < 0)
                                    continue;
                                if (y + yy >= 128 || y + yy < 0)
                                    continue;
                                if (yy > 0 && yy != (obj.length * 16) - 1)
                                {
                                    if (xx is 0 or 15)
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                                else
                                    fp.SetPixel(x + xx, y + yy, border);
                            }
                        }
                    }
                }
                else
                {
                    if (obj.direction == 8)
                    {
                        for (var i = 0; i < obj.length; i++)
                        {
                            for (var yy = 0; yy < obj.h * 16; yy++)
                            {
                                for (var xx = 0 + (i * 16); xx < obj.w * (obj.length * 16); xx++)
                                {
                                    if (x < 0 && !h)
                                    {
                                        xx += 16;
                                        h = true;
                                    }

                                    if (y < 0 && !v)
                                    {
                                        yy += 16;
                                        v = true;
                                    }

                                    if (x + xx >= 160 || x + xx < 0)
                                        continue;
                                    if (y + yy >= 128 || y + yy < 0)
                                        continue;
                                    if (yy > 0 && yy != ((obj.h * 16) - 1))
                                    {
                                        if (xx == 0 || xx == ((obj.length * 16) * obj.w) - 1)
                                            fp.SetPixel(x + xx, y + yy, border);
                                    }
                                    else
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < obj.length; i++)
                        {
                            for (var yy = 0 + (i * 16); yy < obj.h * (obj.length * 16); yy++)
                            {
                                for (var xx = 0; xx < obj.w * 16; xx++)
                                {
                                    if (x < 0 && !h)
                                    {
                                        xx += 16;
                                        h = true;
                                    }

                                    if (y < 0 && !v)
                                    {
                                        yy += 16;
                                        v = true;
                                    }

                                    if (x + xx >= 160 || x + xx < 0)
                                        continue;
                                    if (y + yy >= 128 || y + yy < 0)
                                        continue;
                                    if (yy > 0 && yy != ((obj.length * 16) * obj.h) - 1)
                                    {
                                        if (xx == 0 || xx == (obj.w * 16) - 1)
                                            fp.SetPixel(x + xx, y + yy, border);
                                    }
                                    else
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                            }
                        }
                    }
                }
            }
        }

        fp.Unlock(true);
    }

    public void GetFloor(byte map, bool special)
    {
        int secondHalf;
        var i = -1;
        if (special)
        {
            i = map switch
            {
                0x06 => 0x31F4,
                0x0E => 0x31C4,
                0x1B => 0x3204,
                0x2B => 0x3214,
                0x79 => 0x31E4,
                0x8C => 0x31D4,
                _ => i
            };
        }

        if (i > 0)
        {
            secondHalf = gb.Get2BytePointerAddress(i).Address + 1;
            if (map > 0x7F)
                secondHalf += 0x68000;
            else
                secondHalf += 0x24000;
            gb.BufferLocation = secondHalf;
        }
        else
        {
            secondHalf = 0x24000 + (map * 2);
            secondHalf = gb.Get2BytePointerAddress(secondHalf).Address + 1;
            if (map > 0x7F)
                secondHalf = 0x68000 + (secondHalf - 0x24000);
            gb.BufferLocation = secondHalf;
        }

        var b = gb.ReadByte();
        floor = (byte)(b & 0xF);
        wall = (byte)(b >> 4);
        spriteBank = gb.ReadByte(0x830DB + map);
    }

    public void GetCollisionDataOverworld(byte map, bool swatch)
    {
        var i = -1;
        objects = new List<LAObject>();
        warps = new List<Warps>();
        overworldObjects = new LAObject();
        int secondHalf;
        collision = true;

        if (swatch)
        {
            i = map switch
            {
                0x06 => 0x31F4,
                0x0E => 0x31C4,
                0x1B => 0x3204,
                0x2B => 0x3214,
                0x79 => 0x31E4,
                0x8C => 0x31D4,
                _ => i
            };
        }

        if (i > 0)
        {
            secondHalf = gb.Get2BytePointerAddress(i).Address + 1;
            if (map > 0x7F)
                secondHalf += 0x68000;
            else
                secondHalf += 0x24000;
            gb.BufferLocation = secondHalf;
        }
        else
        {
            secondHalf = 0x24000 + (map * 2);
            secondHalf = gb.Get2BytePointerAddress(secondHalf).Address + 1;
            if (map > 0x7F)
                secondHalf = 0x68000 + (secondHalf - 0x24000);
            gb.BufferLocation = secondHalf;
        }

        mapAddress = gb.BufferLocation - 1;
        gb.ReadByte();
        byte b;
        while ((b = gb.ReadByte()) != 0xFE) //0xFE = End of room
        {
            switch (b >> 4)
            {
                case 0xE:
                {
                    var w = new Warps
                    {
                        type = (byte)(b & 0x0F),
                        region = gb.ReadByte(),
                        map = gb.ReadByte(),
                        x = gb.ReadByte(),
                        y = gb.ReadByte()
                    };
                    warps.Add(w);
                    continue;
                }
                case 8:
                //3-Byte objects
                case 0xC:
                {
                    var o = new LAObject
                    {
                        is3Byte = true,
                        length = (byte)(b & 0xF),
                        direction = (byte)(b >> 4)
                    };
                    var b2 = gb.ReadByte();
                    o.y = (byte)(b2 >> 4);
                    o.x = (byte)(b2 & 0xF);
                    o.id = gb.ReadByte();
                    overworldObjects.getOverworldObjs(o);
                    continue;
                }
                default:
                {
                    var ob = new LAObject
                    {
                        y = (byte)(b >> 4),
                        x = (byte)(b & 0xF),
                        id = gb.ReadByte()
                    }; // 2-Byte tiles
                    overworldObjects.getOverworldObjs(ob);
                    continue;
                }
            }
        }

        foreach (var obj in overworldObjects.objectIDs)
        {
            objects.Add(obj);
        }
    }

    public void LoadCollisionsOverworld()
    {
        mapData = new byte[80];
        for (var i = 0; i < 80; i++)
            mapData[i] = (byte)(floor + (wall * 0x10));
        foreach (var obj in objects)
        {
            var dx = (obj.x == 0xF ? (obj.x - 16) : obj.x);
            var dy = (obj.y == 0xF ? (obj.y - 16) : obj.y);
            var specialObject = obj.id;
            switch (obj.is3Byte)
            {
                case false when obj.special:
                {
                    for (var h = 0; h < obj.h; h++)
                    {
                        for (var w = 0; w < obj.w; w++)
                        {
                            if (dy < 0)
                            {
                                obj.y = 0;
                                dy++;
                                h++;
                            }

                            if (dx < 0)
                            {
                                obj.x = 0;
                                dx++;
                                w++;
                            }

                            if (dx > 9)
                            {
                                dx++;
                                if (w == obj.w - 1)
                                {
                                    if (obj.hFlip)
                                        dx = obj.x - 1;
                                    else
                                        dx = obj.x;
                                    if (h == obj.h - 1)
                                        dy = obj.y;
                                    else
                                        dy++;
                                }

                                continue;
                            }

                            if (dy > 7)
                            {
                                dx++;
                                if (w == obj.w - 1)
                                {
                                    if (obj.vFlip)
                                        dx = obj.x - 1;
                                    else
                                        dx = obj.x;
                                    if (h == obj.h - 1)
                                        dy = obj.y;
                                    else
                                        dy++;
                                }

                                continue;
                            }

                            if (obj.hFlip && obj.vFlip)
                            {
                                obj.id = obj.tiles[(h * obj.w) + w];
                                mapData[(obj.x + (w - 1)) + ((obj.y + (h - 1)) * 10)] = obj.id;
                            }
                            else if (obj.hFlip)
                            {
                                obj.id = obj.tiles[(h * obj.w) + w];
                                mapData[(obj.x + (w - 1)) + ((obj.y + h) * 10)] = obj.id;
                            }
                            else if (obj.vFlip)
                            {
                                obj.id = obj.tiles[(h * obj.w) + w];
                                mapData[(obj.x + w) + ((obj.y + (h - 1)) * 10)] = obj.id;
                            }
                            else
                            {
                                obj.id = obj.tiles[(h * obj.w) + w];
                                mapData[(obj.x + w) + ((obj.y + h) * 10)] = obj.id;
                            }

                            dx++;
                            if (w >= (obj.w - 1))
                            {
                                if (obj.hFlip)
                                    dx = obj.x - 1;
                                else
                                    dx = obj.x;
                                dy++;
                            }
                        }
                    }

                    obj.id = specialObject;
                    if (obj.hFlip)
                        obj.x = 0x0F;
                    if (obj.vFlip)
                        obj.y = 0x0F;
                    break;
                }
                case false when dy is < 0 or > 7:
                case false when dx is < 0 or > 9:
                    continue;
                case false:
                    mapData[obj.x + (obj.y * 10)] = obj.id;
                    break;
                case true when !obj.special:
                {
                    for (var i = 0; i < obj.length; i++)
                    {
                        if (obj.direction == 8) //Horizontal
                        {
                            if (dx < 0)
                            {
                                dx++;
                                i++;
                            }

                            if (dy is < 0 or > 7)
                                continue;
                            if (dx > 9)
                                continue;
                            if (obj.hFlip)
                            {
                                obj.x = 0;
                                mapData[obj.x + (obj.y * 10) + (i - 1)] = obj.id;
                            }
                            else
                                mapData[obj.x + (obj.y * 10) + i] = obj.id;

                            dx++;
                        }
                        else
                        {
                            if (dx is < 0 or > 9)
                                continue;
                            if (dy < 0)
                            {
                                dy++;
                                i++;
                            }

                            if (dy > 7)
                                continue;
                            if (obj.vFlip)
                            {
                                obj.y = 0;
                                mapData[obj.x + (obj.y * 10) + ((i - 1) * 10)] = obj.id;
                            }
                            else
                                mapData[obj.x + (obj.y * 10) + (i * 10)] = obj.id;

                            dy++;
                        }
                    }

                    if (obj.hFlip)
                        obj.x = 0x0F;
                    if (obj.vFlip)
                        obj.y = 0x0F;
                    break;
                }
                case true:
                {
                    for (var i = 0; i < obj.length; i++)
                    {
                        if (obj.direction == 8)
                        {
                            if (dx >= 0)
                            {
                                dx = obj.x + (i * obj.w);
                                if (obj.hFlip)
                                    dx = obj.x - 1 + (i * obj.w);
                            }

                            if (dy >= 0)
                            {
                                dy = obj.y;
                                if (obj.vFlip)
                                    dy = obj.y - 1;
                            }
                        }
                        else
                        {
                            if (dx >= 0)
                            {
                                dx = obj.x;
                                if (obj.hFlip)
                                    dx = obj.x - 1;
                            }

                            if (dy >= 0)
                            {
                                dy = obj.y + (i * obj.h);
                                if (obj.vFlip)
                                    dy = obj.y - 1 + (i * obj.h);
                            }
                        }

                        for (var h = 0; h < obj.h; h++)
                        {
                            for (var w = 0; w < obj.w; w++)
                            {
                                if (dy < 0)
                                {
                                    obj.y = 0;
                                    dy++;
                                    h++;
                                }

                                if (dx < 0)
                                {
                                    obj.x = 0;
                                    dx++;
                                    w++;
                                }

                                if (dx > 9)
                                {
                                    dx++;
                                    if (w == obj.w - 1)
                                    {
                                        if (obj.direction == 8)
                                        {
                                            if (obj.hFlip)
                                                dx = obj.x - 1 + (i * obj.w);
                                            else
                                                dx = obj.x + (i * obj.w);
                                        }
                                        else
                                        {
                                            if (obj.hFlip)
                                                dx = obj.x - 1;
                                            else
                                                dx = obj.x;
                                        }

                                        if (h == obj.h - 1)
                                            dy = obj.y;
                                        else
                                            dy++;
                                    }

                                    continue;
                                }

                                if (dy > 7)
                                {
                                    dx++;
                                    if (w == obj.w - 1)
                                    {
                                        if (obj.direction == 8)
                                        {
                                            if (obj.vFlip)
                                                dx = obj.x - 1 + (i * obj.w);
                                            else
                                                dx = obj.x + (i * obj.w);
                                        }
                                        else
                                        {
                                            if (obj.vFlip)
                                                dy = obj.y - 1 + (i * obj.h);
                                            else
                                                dx = obj.x;
                                        }

                                        if (h == obj.h - 1)
                                            dy = obj.y;
                                        else
                                            dy++;
                                    }

                                    continue;
                                }

                                if (obj.hFlip && obj.vFlip)
                                {
                                    obj.id = obj.tiles[(h * obj.w) + w];
                                    if (obj.direction == 8)
                                        mapData[(obj.x + (w - 1) + (i * obj.w)) + (obj.y + (h - 1) * 10)] = obj.id;
                                    else
                                        mapData[(obj.x + (w - 1)) + ((obj.y + (h - 1) + (i * obj.h)) * 10)] =
                                            obj.id;
                                }
                                else if (obj.hFlip)
                                {
                                    obj.id = obj.tiles[(h * obj.w) + w];
                                    if (obj.direction == 8)
                                        mapData[(obj.x + (w - 1) + (i * obj.w)) + ((obj.y + h) * 10)] = obj.id;
                                    else
                                        mapData[(obj.x + (w - 1)) + ((obj.y + h + (i * obj.h)) * 10)] = obj.id;
                                }
                                else if (obj.vFlip)
                                {
                                    obj.id = obj.tiles[(h * obj.w) + w];
                                    if (obj.direction == 8)
                                        mapData[(obj.x + w + (i * obj.w)) + ((obj.y + (h - 1)) * 10)] = obj.id;
                                    else
                                        mapData[(obj.x + w) + ((obj.y + (h - 1) + (i * obj.h)) * 10)] = obj.id;
                                }
                                else
                                {
                                    obj.id = obj.tiles[(h * obj.w) + w];
                                    if (obj.direction == 8) //Horizontal
                                        mapData[(obj.x + w + (i * obj.w)) + ((obj.y + h) * 10)] = obj.id;
                                    else
                                        mapData[(obj.x + w) + (((obj.y + h) + (i * obj.h)) * 10)] = obj.id;
                                }

                                dx++;
                                if (w >= (obj.w - 1))
                                {
                                    if (obj.direction == 8)
                                    {
                                        dx = obj.x + (i * obj.w);
                                        if (obj.hFlip && h != (obj.h - 1))
                                            dx = obj.x - 1 + (i * obj.w);
                                    }
                                    else
                                    {
                                        dx = obj.x;
                                        if (obj.hFlip && h != (obj.h - 1))
                                            dx = obj.x - 1;
                                    }

                                    dy++;
                                }
                            }
                        }
                    }

                    obj.id = specialObject;
                    if (obj.hFlip)
                        obj.x = 0x0F;
                    if (obj.vFlip)
                        obj.y = 0x0F;
                    break;
                }
            }
        }
    }

    public int GetUsedSpace()
    {
        var i = 0;
        foreach (var o in objects)
        {
            if (o.is3Byte)
                i += 3;
            else
                i += 2;
        }

        if (warps.Count == 0) return i;
        i += warps.Sum(_ => 5);
        return i;
    }

    public int GetFreeSpace(byte byteMap, bool spec)
    {
        unSortedPointers = new List<int>();
        pointers = new int[262];
        var cMapPointer = 0;
        var map = 0;
        int space;
        while (map < 256)
        {
            var i = 0;
            var i2 = 0;
            var secondHalf = 0x24000 + map * 2;
            secondHalf = gb.Get2BytePointerAddress(secondHalf).Address;
            if (map > 0x7F)
                secondHalf = 0x68000 + (secondHalf - 0x24000);
            if (map == byteMap)
                cMapPointer = secondHalf;
            pointers[map] = secondHalf;
            switch (map)
            {
                case 0x06:
                    i = 0x31F4;
                    i2 = 1;
                    break;
                case 0x0E:
                    i = 0x31C4;
                    i2 = 2;
                    break;
                case 0x1B:
                    i = 0x3204;
                    i2 = 3;
                    break;
                case 0x2B:
                    i = 0x3214;
                    i2 = 4;
                    break;
                case 0x79:
                    i = 0x31E4;
                    i2 = 5;
                    break;
                case 0x8C:
                    i = 0x31D4;
                    i2 = 6;
                    break;
            }

            if (i > 0)
            {
                secondHalf = gb.Get2BytePointerAddress(i).Address;
                if (map > 0x7F)
                    secondHalf += 0x68000;
                else
                    secondHalf += 0x24000;
                if (spec && map == byteMap)
                    cMapPointer = secondHalf;
                pointers[i2 + 0xFF] = secondHalf;
            }

            map++;
        }

        foreach (var point in pointers)
            unSortedPointers.Add(point);
        Array.Sort(pointers);
        var index = Array.IndexOf(pointers, cMapPointer);
        switch (byteMap)
        {
            case 0xFF:
                gb.BufferLocation = cMapPointer;
                space = 0x69E73 - cMapPointer;
                break;
            case 0x7F:
                gb.BufferLocation = cMapPointer;
                space = 0x2668B - cMapPointer;
                break;
            //else if (Map == 0x51 || Map == 0x63)
            // space = ((int)pointers.GetValue(index + 2) - 3) - cMapPointer;
            default:
            {
                while ((int)pointers.GetValue(index + 1) == cMapPointer)
                    index++;
                space = ((int)pointers.GetValue(index + 1) - 3) - cMapPointer;
                break;
            }
        }

        return space;
    }

    public void DrawSelectedObject(Bitmap image, LAObject selected)
    {
        var fp = new FastPixel(image);
        var border = Color.White;
        fp.rgbValues = new byte[160 * 128 * 4];
        fp.Lock();
        foreach (var obj in objects)
        {
            if (obj.x != selected.x)
                continue;
            if (obj.y != selected.y)
                continue;
            if (obj.direction != selected.direction)
                continue;
            if (obj.length != selected.length)
                continue;
            if (obj.id != selected.id)
                continue;
            var x = obj.x * 16;
            var y = obj.y * 16;
            if (obj.hFlip)
                x = -16;
            if (obj.vFlip)
                y = -16;
            var v = false;
            var h = false;
            if (!obj.is3Byte && !obj.special)
            {
                if (obj.x > 9 || obj.y > 7)
                    continue;
            }

            if (!obj.is3Byte)
            {
                if (!obj.special)
                {
                    for (var yy = 0; yy < 16; yy++)
                    {
                        for (var xx = 0; xx < 16; xx++)
                        {
                            if (yy > 0 && yy != 15)
                            {
                                if (xx is 0 or 15)
                                {
                                    fp.SetPixel(x + xx, y + yy, border);
                                }
                            }
                            else
                            {
                                fp.SetPixel(x + xx, y + yy, border);
                            }
                        }
                    }
                }
                else
                {
                    for (var yy = 0; yy < obj.h * 16; yy++)
                    {
                        for (var xx = 0; xx < obj.w * 16; xx++)
                        {
                            if (x < 0 && !h)
                            {
                                xx += 16;
                                h = true;
                            }

                            if (y < 0 && !v)
                            {
                                yy += 16;
                                v = true;
                            }

                            if (x + xx >= 160 || x + xx < 0)
                                continue;
                            if (y + yy >= 128 || y + yy < 0)
                                continue;
                            if (yy > 0 && yy != ((obj.h * 16) - 1))
                            {
                                if (xx == 0 || xx == (obj.w * 16) - 1)
                                    fp.SetPixel(x + xx, y + yy, border);
                            }
                            else
                                fp.SetPixel(x + xx, y + yy, border);
                        }
                    }
                }
            }
            else
            {
                if (!obj.special)
                {
                    if (obj.direction == 8)
                    {
                        for (var yy = 0; yy < 16; yy++)
                        {
                            for (var xx = 0; xx < (obj.length * 16); xx++)
                            {
                                if (x < 0 && !h)
                                {
                                    xx += 16;
                                    h = true;
                                }

                                if (y < 0 && !v)
                                {
                                    yy += 16;
                                    v = true;
                                }

                                if (x + xx >= 160 || x + xx < 0)
                                    continue;
                                if (y + yy >= 128 || y + yy < 0)
                                    continue;
                                if (yy > 0 && yy != 15)
                                {
                                    if (xx == 0 || xx == (obj.length * 16) - 1)
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                                else
                                    fp.SetPixel(x + xx, y + yy, border);
                            }
                        }
                    }
                    else
                    {
                        for (var yy = 0; yy < obj.length * 16; yy++)
                        {
                            for (var xx = 0; xx < 16; xx++)
                            {
                                if (x < 0 && !h)
                                {
                                    xx += 16;
                                    h = true;
                                }

                                if (y < 0 && !v)
                                {
                                    yy += 16;
                                    v = true;
                                }

                                if (x + xx >= 160 || x + xx < 0)
                                    continue;
                                if (y + yy >= 128 || y + yy < 0)
                                    continue;
                                if (yy > 0 && yy != (obj.length * 16) - 1)
                                {
                                    if (xx is 0 or 15)
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                                else
                                    fp.SetPixel(x + xx, y + yy, border);
                            }
                        }
                    }
                }
                else
                {
                    if (obj.direction == 8)
                    {
                        for (var i = 0; i < obj.length; i++)
                        {
                            for (var yy = 0; yy < obj.h * 16; yy++)
                            {
                                for (var xx = 0 + (i * 16); xx < obj.w * (obj.length * 16); xx++)
                                {
                                    if (x < 0 && !h)
                                    {
                                        xx += 16;
                                        h = true;
                                    }

                                    if (y < 0 && !v)
                                    {
                                        yy += 16;
                                        v = true;
                                    }

                                    if (x + xx >= 160 || x + xx < 0)
                                        continue;
                                    if (y + yy >= 128 || y + yy < 0)
                                        continue;
                                    if (yy > 0 && yy != ((obj.h * 16) - 1))
                                    {
                                        if (xx == 0 || xx == ((obj.length * 16) * obj.w) - 1)
                                            fp.SetPixel(x + xx, y + yy, border);
                                    }
                                    else
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < obj.length; i++)
                        {
                            for (var yy = 0 + (i * 16); yy < obj.h * (obj.length * 16); yy++)
                            {
                                for (var xx = 0; xx < obj.w * 16; xx++)
                                {
                                    if (x < 0 && !h)
                                    {
                                        xx += 16;
                                        h = true;
                                    }

                                    if (y < 0 && !v)
                                    {
                                        yy += 16;
                                        v = true;
                                    }

                                    if (x + xx >= 160 || x + xx < 0)
                                        continue;
                                    if (y + yy >= 128 || y + yy < 0)
                                        continue;
                                    if (yy > 0 && yy != ((obj.length * 16) * obj.h) - 1)
                                    {
                                        if (xx == 0 || xx == (obj.w * 16) - 1)
                                            fp.SetPixel(x + xx, y + yy, border);
                                    }
                                    else
                                        fp.SetPixel(x + xx, y + yy, border);
                                }
                            }
                        }
                    }
                }
            }
        }

        fp.Unlock(true);
    }
}