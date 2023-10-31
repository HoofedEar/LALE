using GBHL;
namespace LALE;

internal class Chest
{
    private readonly GBFile gb;
    private readonly byte dungeon;
    private readonly byte map;
    private readonly bool overworld;
    public byte chestData;

    public Chest(GBFile g, bool overWorld, byte dung, byte mapData)
    {
        gb = g;
        dungeon = dung;
        map = mapData;
        overworld = overWorld;
        LoadChestData();
    }

    private void LoadChestData()
    {
        if (overworld)
        {
            gb.BufferLocation = 0x50560 + map;
            chestData = gb.ReadByte();
        }
        else switch (dungeon)
        {
            case < 6:
            case >= 0x1A when dungeon != 0xFF:
                gb.BufferLocation = 0x50660 + map;
                chestData = gb.ReadByte();
                break;
            case >= 6 and < 0x1A:
                gb.BufferLocation = 0x50760 + map;
                chestData = gb.ReadByte();
                break;
            case 0xFF:
                gb.BufferLocation = 0x50860 + map;
                chestData = gb.ReadByte();
                break;
        }
    }
}