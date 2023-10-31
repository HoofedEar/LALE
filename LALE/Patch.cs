using GBHL;

namespace LALE;

public class Patch
{
    public GBFile gb;

    public Patch(GBFile g)
    {
        gb = g;
    }

    public void DefaultMusic(bool music)
    {
        if (music)
        {
            gb.WriteBytes(0x8156, new byte[] { 0x58, 0x41 });
            gb.WriteByte(0xBB47, 0);
            Properties.Settings.Default.DefaultMusic = true;
            Properties.Settings.Default.Save();
        }
        else
        {
            gb.WriteBytes(0x8156, new byte[] { 0xA2, 0x41 });
            gb.WriteByte(0xBB47, 0x41);
            Properties.Settings.Default.DefaultMusic = false;
            Properties.Settings.Default.Save();
        }
    }
}