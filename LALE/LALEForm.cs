using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
// ReSharper disable CommentTypo
// ReSharper disable InconsistentNaming

namespace LALE;

public partial class LALEForm : Form
{
    private GBHL.GBFile gb;
    private string filename = "";
    private string exportToFilename = "";
    private Bitmap tileset;
    private TileLoader tileLoader;
    private DungeonDrawer dungeonDrawer;
    private MinimapDrawer minimapDrawer;
    private OverworldDrawer overworldDrawer;
    private MapSaver mapSaver;
    private Sprites sprites;
    private Patch patches;
    private ExportMap exportMaps;
    private List<Warps> WL = new();
    private LAObject selectedObject = new();
    private Point lastMapHoverPoint = new(-1, -1);
    private Color[,] palette = new Color[8, 4];
    private Color[,] paletteCopy;
    private int objectX;
    private int objectY;
    private byte[] mapData;
    private byte[] mapDataCopy;
    private byte selectedTile;
    private byte mapIndex;
    private byte dungeonIndex;
    private byte animations;
    private byte sog;
    private byte floorTile;
    private byte wallTiles;
    private byte spriteBank;
    private byte music;
    private byte chestData;
    private int freeSpace;
    private int usedSpace;
    private bool overWorld = true;
    private bool overLay = true;
    private bool sideView;

    private byte palOffset;
    private byte startXPos;
    private byte startYPos;
    private byte SEDung;
    private byte SEMap;
    private bool SEOverworld;

    public LALEForm()
    {
        InitializeComponent();
    }

    private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var ofd = new OpenFileDialog();
        ofd.Title = @"Select a ROM";
        ofd.Filter = @"GameBoy ROM|*.gbc;*.gb";
        if (ofd.ShowDialog() != DialogResult.OK) return;
        filename = ofd.FileName;

        var analysis = new ROMAnalysis();
        analysis.Analyze(filename);

        var br = new BinaryReader(File.OpenRead(filename));
        var buffer = br.ReadBytes((int)br.BaseStream.Length);

        gb = new GBHL.GBFile(buffer);
        //toolStripStatusLabel1.Text = ofd.SafeFileName;
        //MessageBox.Show("ROM sucessfully loaded.");

        tileLoader = new TileLoader(gb);
        dungeonDrawer = new DungeonDrawer(gb);
        minimapDrawer = new MinimapDrawer(gb);
        overworldDrawer = new OverworldDrawer(gb);
        mapSaver = new MapSaver(gb);
        patches = new Patch(gb);
        sprites = new Sprites(gb);

        //if (comboBox1.SelectedIndex != 0)
        // comboBox1.SelectedIndex = 0;
        if (tabMapType.SelectedIndex != 0)
            tabMapType.SelectedIndex = 0; // reset to overworld
        nMap.Enabled = true;
        comboBox1.Enabled = true;
        cSpecialTiles.Enabled = true;
        rOverlay.Enabled = true;
        rCollision.Enabled = true;
        cSideview.Enabled = true;
        cSideview2.Enabled = true;
        nRegion.Enabled = true;
        nAnimations.Enabled = true;
        nSOG.Enabled = true;
        nFloor.Enabled = true;
        nWall.Enabled = true;
        nMusic.Enabled = true;
        nSpriteBank.Enabled = true;
        nSelected.Enabled = true;
        comDirection.Enabled = true;
        nLength.Enabled = true;
        nObjectID.Enabled = true;
        cObjectList.Enabled = true;
        cSprite.Enabled = true;
        defaultMusicToolStripMenuItem.Enabled = true;
        defaultMusicToolStripMenuItem.Checked = Properties.Settings.Default.DefaultMusic;
        pMinimap.SelectedIndex = 0;
        LoadSandA();
        setSpriteData();
        LoadTileset();
        //overworldDrawer.getCollisionDataOverworld(0, false);                
        WL = new List<Warps>();
        WL = overworldDrawer.warps;
        drawMinimap();

        gb.WriteByte(0x2FFFF, 0xFE); //Ends collision data for indoor map FF

        br.Close();
    }

    private void LoadSandA()
    {
        tileLoader.getAnimations(mapIndex, dungeonIndex, overWorld, cSpecialTiles.Checked);
        animations = tileLoader.Animations;
        tileLoader.getSOG(mapIndex, overWorld);
        sog = tileLoader.SOG;
        var chest = new Chest(gb, overWorld, dungeonIndex, mapIndex);
        chestData = chest.chestData;
        if (overWorld)
        {
            overworldDrawer.GetFloor(mapIndex, cSpecialTiles.Checked);
            floorTile = overworldDrawer.floor;
            wallTiles = overworldDrawer.wall;
            spriteBank = overworldDrawer.spriteBank;
            overworldDrawer.GetMusic(mapIndex);
            music = overworldDrawer.music;
            nMusic.Value = music;
        }
        else
        {
            cSpecialTiles.Checked = false;
            dungeonDrawer.getMusic(dungeonIndex, mapIndex);
            music = dungeonDrawer.music;
            nMusic.Value = music;
            dungeonDrawer.getWallsandFloor(dungeonIndex, mapIndex, cMagGlass.Checked);
            wallTiles = dungeonDrawer.wall;
            floorTile = dungeonDrawer.floor;
            spriteBank = dungeonDrawer.spriteBank;
            nWall.Value = wallTiles;
            nSpriteBank.Value = spriteBank;
            nFloor.Value = floorTile;
        }
    }

    private void LoadTileset()
    {
        //if (animations != tileLoader.Animations)
        tileLoader.Animations = animations;
        //if (sog != tileLoader.SOG)
        tileLoader.SOG = sog;
        if (overWorld)
        {
            //if (floorTile != overworldDrawer.floor)
            overworldDrawer.floor = floorTile;
            //if (wallTiles != overworldDrawer.wall)
            overworldDrawer.wall = wallTiles;
            //if (spriteBank != overworldDrawer.spriteBank)
            overworldDrawer.spriteBank = spriteBank;

            nFloor.Value = overworldDrawer.floor;
            nWall.Value = overworldDrawer.wall;
            nSpriteBank.Value = overworldDrawer.spriteBank;
        }
        else
            cSpecialTiles.Checked = false;

        var data = tileLoader.loadTileset(dungeonIndex, mapIndex, overWorld, rCrystals.Checked, sideView);
        tileLoader.loadPallete(dungeonIndex, mapIndex, overWorld, sideView);
        palette = tileLoader.palette;
        palOffset = tileLoader.palOffset;
        var tiles = tileLoader.loadPaletteFlipIndexes(mapIndex, dungeonIndex);
        pTiles.Image = tileLoader.drawTileset(data, tiles);
        tileset = tileLoader.drawTileset(data, tiles);

        nAnimations.Value = tileLoader.Animations;
        animations = tileLoader.Animations;
        nSOG.Value = tileLoader.SOG;
    }

    private void setCollisionData()
    {
        comDirection.Enabled = false;
        nLength.Enabled = false;
        nSelected.Value = -1;
        comDirection.SelectedIndex = 0;
        nObjectID.Value = 0;
        nLength.Value = 1;
    }

    private void setSpriteData()
    {
        if (!cSprite.Checked)
        {
            nSpriteSelected.Enabled = false;
            nSpriteID.Enabled = false;
        }

        sprites.LoadObjects(overWorld, dungeonIndex, mapIndex);
        //sprites.loadSpriteBanks(overWorld, dungeonIndex, mapIndex);
        nSpriteSelected.Maximum = sprites.spriteList.Count - 1;
        nSpriteSelected.Value = -1;
        nSpriteID.Value = 0;
    }

    private void drawSprites()
    {
        var b = sprites.DrawSprites((Bitmap)pMap.Image);
        pMap.Image = b;
    }

    private void drawDungeon()
    {
        cSpecialTiles.Checked = false;
        gBoxCollisions.Enabled = !cSprite.Checked;
        //if (wallTiles != dungeonDrawer.wall)
        dungeonDrawer.wall = wallTiles;
        //if (floorTile != dungeonDrawer.floor)
        dungeonDrawer.floor = floorTile;
        //if (spriteBank != dungeonDrawer.spriteBank)
        dungeonDrawer.spriteBank = spriteBank;
        dungeonDrawer.loadWalls();
        dungeonDrawer.loadCollisionsDungeon();
        mapData = dungeonDrawer.data;
        pMap.Image = dungeonDrawer.DrawCollisions(tileset, mapData, collisionBordersToolStripMenuItem.Checked);
        if (cSprite.Checked)
            drawSprites();
        if (!cSprite.Checked)
            toolStripStatusLabel1.Text = @"Map: 0x" + dungeonDrawer.mapAddress.ToString("X");
        else
            toolStripStatusLabel1.Text = @"Objects: 0x" + sprites.objectAddress.ToString("X");
        toolStripStatusLabel2.Text = @"Used/Free Space: " + usedSpace + @"/" + freeSpace;
        nFloor.Value = dungeonDrawer.floor;
        nWall.Value = wallTiles;
        nSpriteBank.Value = spriteBank;
        nSelected.Maximum = dungeonDrawer.objects.Count - 1;
    }

    private void drawOverworld()
    {
        switch (overLay)
        {
            case true:
            {
                gBoxCollisions.Enabled = false;
                pObject.Invalidate();
                mapData = overworldDrawer.ReadMap(mapIndex, cSpecialTiles.Checked);
                pMap.Image = overworldDrawer.DrawMap(tileset, mapData, collisionBordersToolStripMenuItem.Checked);
                if (cSprite.Checked)
                    drawSprites();
                toolStripStatusLabel2.Text = "";
                if (!cSprite.Checked)
                    toolStripStatusLabel1.Text = @"Map: 0x" + overworldDrawer.mapAddress.ToString("X");
                else
                {
                    toolStripStatusLabel1.Text = @"Objects: 0x" + sprites.objectAddress.ToString("X");
                    toolStripStatusLabel2.Text =
                        @"Used/Free Space: " + usedSpace + @"/" + freeSpace;
                }

                CollisionList.Items.Clear();
                break;
            }
            case false:
            {
                gBoxCollisions.Enabled = !cSprite.Checked;
                pObject.Invalidate();
                overworldDrawer.LoadCollisionsOverworld();
                mapData = overworldDrawer.mapData;
                pMap.Image = overworldDrawer.DrawMap(tileset, mapData, collisionBordersToolStripMenuItem.Checked);
                if (cSprite.Checked)
                    drawSprites();
                if (!cSprite.Checked)
                    toolStripStatusLabel1.Text = @"Collision: 0x" + overworldDrawer.mapAddress.ToString("X");
                else
                    toolStripStatusLabel1.Text = @"Objects: 0x" + sprites.objectAddress.ToString("X");
                toolStripStatusLabel2.Text = @"Used/Free Space: " + usedSpace + @"/" + freeSpace;
                nSelected.Maximum = overworldDrawer.objects.Count - 1;
                break;
            }
        }
    }

    private void drawMinimap()
    {
        switch (overWorld)
        {
            case false:
            {
                minimapDrawer.LoadMinimapDData(dungeonIndex);
                var data = minimapDrawer.LoadMinimapDungeon();
                pMinimapD.Image = minimapDrawer.DrawDungeonTiles(data);
                break;
            }
            case true:
            {
                var data = minimapDrawer.LoadMinimapOverworld();
                pMinimap.Image = minimapDrawer.DrawOverworldTiles(data);
                break;
            }
        }
    }

    private void nMap_ValueChanged(object sender, EventArgs e)
    {
        mapIndex = (byte)nMap.Value;
        LoadSandA();
        LoadTileset();

        // not overworld
        if (
            (tabMapType.SelectedIndex != 0 && mapIndex == 0xF5 && dungeonIndex >= 0x1A)
            || tabMapType.SelectedIndex != 0 && mapIndex == 0xF5 && dungeonIndex < 6
        )
        {
            cMagGlass.Visible = true;
            cMagGlass1.Visible = true;
        }
        else
        {
            cMagGlass.Visible = false;
            cMagGlass1.Visible = false;
        }

        if (overWorld)
        {
            overworldDrawer.GetCollisionDataOverworld(mapIndex, cSpecialTiles.Checked);
            if (!overLay)
            {
                setCollisionData();
                collisionListView();
            }

            pMinimap.SelectedIndex = (int)nMap.Value;
            if (!cSprite.Checked)
            {
                freeSpace = overworldDrawer.GetFreeSpace(mapIndex, cSpecialTiles.Checked);
                usedSpace = overworldDrawer.GetUsedSpace();
            }
            else
            {
                setSpriteData();
                freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
                usedSpace = sprites.GetUsedSpace();
            }

            setSpriteData();
            drawOverworld();
            WL = new List<Warps>();
            WL = overworldDrawer.warps;
        }
        else
        {
            cSpecialTiles.Checked = false;
            dungeonDrawer.getCollisionDataDungeon(mapIndex, dungeonIndex, cMagGlass.Checked);
            dungeonDrawer.getEventData(mapIndex, dungeonIndex);
            nEventID.Value = dungeonDrawer.eventID;
            nEventTrigger.Value = dungeonDrawer.eventTrigger;
            label18.Text = @"Location: 0x" + dungeonDrawer.eventDataLocation.ToString("X");
            setCollisionData();
            setSpriteData();
            WL = new List<Warps>();
            WL = dungeonDrawer.warps;
            if (!cSprite.Checked)
            {
                freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
                usedSpace = dungeonDrawer.getUsedSpace();
            }
            else
            {
                freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
                usedSpace = sprites.GetUsedSpace();
            }

            drawDungeon();
        }
    }

    private void pTiles_MouseClick(object sender, MouseEventArgs e)
    {
        selectTile(pTiles.SelectedIndex);
    }

    private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (comboBox1.SelectedIndex == 8)
        {
            dungeonIndex = 0xFF;
            nMap.Maximum = 0x15;
        }
        else
        {
            dungeonIndex = (byte)comboBox1.SelectedIndex;
            nMap.Maximum = 0xFF;
        }

        nMap.Enabled = true;
        pMinimapD.SelectedIndex = 0;
        minimapDrawer.LoadMinimapDData(dungeonIndex);
        mapIndex = minimapDrawer.roomIndexes[0];
        nMap.Value = mapIndex;
        drawMinimap();
        LoadSandA();
        LoadTileset();
        dungeonDrawer.getCollisionDataDungeon(mapIndex, dungeonIndex, cMagGlass.Checked);
        dungeonDrawer.getEventData(mapIndex, dungeonIndex);
        nEventID.Value = dungeonDrawer.eventID;
        nEventTrigger.Value = dungeonDrawer.eventTrigger;
        label18.Text = @"Location: 0x" + dungeonDrawer.eventDataLocation.ToString("X");
        WL = new List<Warps>();
        WL = dungeonDrawer.warps;
        setCollisionData();
        if (!cSprite.Checked)
        {
            freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
            usedSpace = dungeonDrawer.getUsedSpace();
        }
        else
        {
            freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
            usedSpace = sprites.GetUsedSpace();
        }

        collisionListView();
        drawDungeon();
        setSpriteData();
    }

    private void selectTile(int tile)
    {
        selectedTile = (byte)tile;
        if (pTiles.SelectedIndex != tile ||
            pTiles.SelectionRectangle.Width != 1 || pTiles.SelectionRectangle.Height != 1)
            pTiles.SelectedIndex = selectedTile;
        label4.Text = @"Selected tile: " + pTiles.SelectedIndex.ToString("X");
        pTiles.Invalidate();
    }

    private void pMinimap_MouseClick(object sender, MouseEventArgs e)
    {
        if (pTiles.Image == null) return;
        if (!overWorld) return;
        mapIndex = (byte)pMinimap.SelectedIndex;
        nMap.Value = mapIndex;
    }

    private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        switch (tabMapType.SelectedIndex)
        {
            // overworld
            case 0:
                nMap.Minimum = 0x0;
                nMap.Maximum = 0xFF;
                mapIndex = 0;
                label7.Text = @"Floor row:";
                nWall.Maximum = 0xF;
                gEventData.Visible = false;
                cSideview.Checked = false;
                cSideview2.Checked = false;
                cSprite.Checked = false;
                overLay = true;
                rOverlay.Enabled = true;
                rCollision.Enabled = true;
                nMap.Enabled = true;
                rCrystals.Enabled = false;
                rCrystals.Checked = false;
                nMap.Value = mapIndex;
                pMinimap.SelectedIndex = mapIndex;
                overWorld = true;
                LoadSandA();
                setCollisionData();
                WL = new List<Warps>();
                WL = overworldDrawer.warps;
                setSpriteData();
                LoadTileset();
                drawMinimap();
                rOverlay.Checked = true;
                break;
            // dungeon
            case 1:
            {
                cSpecialTiles.Checked = false;
                nMap.Minimum = 0x0;
                nMap.Maximum = 0xFF;
                dungeonIndex = 0;
                comboBox1.SelectedIndex = 0;
                label7.Text = @"Wall tiles:";
                nWall.Maximum = 0x9;
                gEventData.Visible = true;
                cSpecialTiles.Checked = false;
                cSprite.Checked = false;
                cSideview2.Checked = false;
                rCrystals.Enabled = true;
                rOverlay.Enabled = false;
                rCollision.Enabled = false;
                rCollision.Checked = false;
                rOverlay.Checked = false;
                overLay = false;
                nMap.Enabled = true;
                overWorld = false;
                minimapDrawer.LoadMinimapDData(dungeonIndex);
                mapIndex = minimapDrawer.roomIndexes[0];
                nMap.Value = mapIndex;
                pMinimapD.SelectedIndex = mapIndex;
                overWorld = false;
                LoadSandA();
                LoadTileset();
                dungeonDrawer.getCollisionDataDungeon(mapIndex, dungeonIndex, cMagGlass.Checked);
                dungeonDrawer.getEventData(mapIndex, dungeonIndex);
                nEventID.Value = dungeonDrawer.eventID;
                nEventTrigger.Value = dungeonDrawer.eventTrigger;
                label18.Text = @"Location: 0x" + dungeonDrawer.eventDataLocation.ToString("X");
                WL = new List<Warps>();
                WL = dungeonDrawer.warps;
                setCollisionData();
                if (!cSprite.Checked)
                {
                    freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
                    usedSpace = dungeonDrawer.getUsedSpace();
                }
                else
                {
                    freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
                    usedSpace = sprites.GetUsedSpace();
                }

                collisionListView();
                setSpriteData();
                drawDungeon();
                drawMinimap();
                break;
            }
            // indoors
            default:
            {
                cSpecialTiles.Checked = false;
                nMap.Maximum = 0xFF;
                dungeonIndex = (byte)nRegion.Value;
                cSpecialTiles.Checked = false;
                label7.Text = @"Wall tiles:";
                nWall.Maximum = 0x9;
                gEventData.Visible = true;
                cSideview.Checked = false;
                rCrystals.Enabled = true;
                overLay = false;
                overWorld = false;
                cSprite.Checked = false;
                rOverlay.Enabled = false;
                rCollision.Enabled = false;
                rCollision.Checked = false;
                rOverlay.Checked = false;
                nMap.Enabled = true;
                mapIndex = 0x6C;
                nMap.Value = mapIndex;
                LoadSandA();
                dungeonDrawer.getCollisionDataDungeon(mapIndex, dungeonIndex, cMagGlass.Checked);
                dungeonDrawer.getEventData(mapIndex, dungeonIndex);
                nEventID.Value = dungeonDrawer.eventID;
                nEventTrigger.Value = dungeonDrawer.eventTrigger;
                label18.Text = @"Location: 0x" + dungeonDrawer.eventDataLocation.ToString("X");
                WL = new List<Warps>();
                WL = dungeonDrawer.warps;
                setCollisionData();
                if (!cSprite.Checked)
                {
                    freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
                    usedSpace = dungeonDrawer.getUsedSpace();
                }
                else
                {
                    freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
                    usedSpace = sprites.GetUsedSpace();
                }

                collisionListView();
                setSpriteData();
                LoadTileset();
                drawDungeon();
                break;
            }
        }
    }

    private int GetDAndOObjectID(int x, int y)
    {
        x /= 16;
        y /= 16;
        switch (overWorld)
        {
            case false when !cSprite.Checked:
            {
                for (var i = dungeonDrawer.objects.Count - 1; i > -1; i--)
                {
                    if (dungeonDrawer.objects[i].x == x && dungeonDrawer.objects[i].y == y)
                        return i;
                    var o = dungeonDrawer.objects[i];
                    if (dungeonDrawer.objects[i].is3Byte)
                    {
                        if (o.direction == 8)
                        {
                            if (x >= o.x && x < o.length + o.x && y == o.y)
                                return i;
                        }
                        else
                        {
                            if (x == o.x && y >= o.y && y < o.y + o.length)
                                return i;
                        }
                    }

                    if (dungeonDrawer.objects[i].isDoor1)
                    {
                        if (x >= o.x && x < o.w + o.x && y == o.y)
                            return i;
                    }

                    if (dungeonDrawer.objects[i].isDoor2)
                    {
                        if (x == o.x && y >= o.y && y < o.y + o.h)
                            return i;
                    }

                    if (!dungeonDrawer.objects[i].isEntrance) continue;
                    if (x >= o.x && y >= o.y && y < o.y + o.h && x < o.x + o.w)
                        return i;
                }

                break;
            }
            case true when !cSprite.Checked:
            {
                for (var i = overworldDrawer.objects.Count - 1; i > -1; i--)
                {
                    if (overworldDrawer.objects[i].x == x && overworldDrawer.objects[i].y == y)
                        return i;
                    var o = overworldDrawer.objects[i];
                    var dx = o.x == 0xF ? o.x - 16 : o.x;
                    var dy = o.y == 0xF ? o.y - 16 : o.y;
                    if (o.is3Byte)
                    {
                        if (o.direction == 8)
                        {
                            if (o.special)
                            {
                                if (x >= dx && y >= dy && y < dy + o.h && x < dx + o.w * o.length)
                                    return i;
                            }

                            if (x >= dx && x < o.length + dx && y == dy)
                                return i;
                        }
                        else
                        {
                            if (o.special)
                            {
                                if (x >= dx && y >= dy && y < dy + o.h * o.length && x < dx + o.w)
                                    return i;
                            }

                            if (x == dx && y >= dy && y < dy + o.length)
                                return i;
                        }
                    }
                    else if (o.special)
                    {
                        if (x >= dx && y >= dy && y < dy + o.h && x < dx + o.w)
                            return i;
                    }
                }

                break;
            }
            default:
            {
                if (cSprite.Checked)
                {
                    for (var i = sprites.spriteList.Count - 1; i > -1; i--)
                    {
                        if (sprites.spriteList[i].x == x && sprites.spriteList[i].y == y)
                            return i;
                    }
                }

                break;
            }
        }

        return -1;
    }

    private void pMap_MouseDown(object sender, MouseEventArgs e)
    {
        if (pTiles.Image == null) return;
        var ind = GetDAndOObjectID(e.X, e.Y);
        switch (overWorld)
        {
            case true when overLay && !cSprite.Checked:
            {
                switch (e.Button)
                {
                    case MouseButtons.Left:
                    {
                        //if (lastMapHoverPoint.X == e.X / 16 && lastMapHoverPoint.Y == e.Y / 16)
                        //    return;
                        var g = Graphics.FromImage(pMap.Image);
                        if (pTiles.SelectionRectangle is { Width: 1, Height: 1 })
                        {
                            g.DrawImage(pTiles.Image, new Rectangle(e.X / 16 * 16, e.Y / 16 * 16, 16, 16),
                                selectedTile % 16 * 16, selectedTile / 16 * 16, 16, 16, GraphicsUnit.Pixel);
                            mapData[e.X / 16 + e.Y / 16 * 10] = selectedTile;
                        }

                        break;
                    }
                    case MouseButtons.Right:
                        selectTile(mapData[e.X / 16 + e.Y / 16 * 10]);
                        break;
                }

                break;
            }
            case true when !overLay && !cSprite.Checked:
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (ind > -1)
                    {
                        selectCollision(overworldDrawer.objects[ind], ind);
                        objectX = overworldDrawer.objects[ind].x;
                        objectY = overworldDrawer.objects[ind].y;
                        selectedObject = overworldDrawer.objects[ind];
                    }
                    else
                        selectCollision(null, ind);

                    if (ind != -1)
                        overworldDrawer.DrawSelectedObject((Bitmap)pMap.Image, selectedObject);
                }

                break;
            }
            case false when !cSprite.Checked:
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (ind > -1)
                    {
                        selectCollision(dungeonDrawer.objects[ind], ind);
                        objectX = dungeonDrawer.objects[ind].x;
                        objectY = dungeonDrawer.objects[ind].y;
                        selectedObject = dungeonDrawer.objects[ind];
                    }
                    else
                        selectCollision(null, ind);

                    if (ind != -1)
                        dungeonDrawer.drawSelectedObject((Bitmap)pMap.Image, selectedObject);
                }

                break;
            }
            default:
            {
                if (cSprite.Checked)
                {
                    if (ind > -1)
                    {
                        selectCollision(sprites.spriteList[ind], ind);
                        objectX = sprites.spriteList[ind].x;
                        objectY = sprites.spriteList[ind].y;
                        selectedObject = sprites.spriteList[ind];
                    }
                    else
                        selectCollision(null, ind);

                    if (ind != -1)
                        sprites.DrawSelectedSprite(pMap.Image, selectedObject);
                }

                break;
            }
        }

        lastMapHoverPoint = new Point(e.X / 16, e.Y / 16);
        pMap.Invalidate();
    }

    private void pMap_MouseMove(object sender, MouseEventArgs e)
    {
        if (pMap.Image == null)
            return;
        lblHoverPos.Text = @"X: " + (e.X / 16).ToString("X") + @" Y: " + (e.Y / 16).ToString("X");
        switch (overWorld)
        {
            case true when overLay && !cSprite.Checked:
            {
                if (e.Button == MouseButtons.Left)
                {
                    //if (lastMapHoverPoint.X == e.X / 16 && lastMapHoverPoint.Y == e.Y / 16)
                    //    return;
                    var g = Graphics.FromImage(pMap.Image);
                    if (pTiles.SelectionRectangle is { Width: 1, Height: 1 })
                    {
                        if (e.X / 16 > 9 || e.Y / 16 > 7 || e.X / 16 < 0 || e.Y / 16 < 0)
                            return;
                        g.DrawImage(pTiles.Image, new Rectangle(e.X / 16 * 16, e.Y / 16 * 16, 16, 16),
                            selectedTile % 16 * 16, selectedTile / 16 * 16, 16, 16, GraphicsUnit.Pixel);
                        mapData[e.X / 16 + e.Y / 16 * 10] = selectedTile;
                    }
                }

                break;
            }
            case true when !overLay && !cSprite.Checked:
            {
                var ind = (int)nSelected.Value;
                var x = e.X / 16;
                var y = e.Y / 16;
                if (e.Button == MouseButtons.Left)
                {
                    if (ind > -1)
                    {
                        if (objectX == 0xF)
                            objectX = -1;
                        if (objectY == 0xF)
                            objectY = -1;
                        overworldDrawer.objects[ind].x = (byte)(objectX + (x - lastMapHoverPoint.X));
                        overworldDrawer.objects[ind].y = (byte)(objectY + (y - lastMapHoverPoint.Y));
                        if (objectX + (x - lastMapHoverPoint.X) < 0 || objectX + (x - lastMapHoverPoint.X) > 9)
                        {
                            if (!overworldDrawer.objects[ind].is3Byte && !overworldDrawer.objects[ind].special ||
                                !overworldDrawer.objects[ind].special)
                            {
                                overworldDrawer.objects[ind].x = 0;
                                overworldDrawer.objects[ind].hFlip = false;
                            }
                            else
                            {
                                overworldDrawer.objects[ind].x = 0xF;
                                overworldDrawer.objects[ind].hFlip = true;
                            }
                        }
                        else
                            overworldDrawer.objects[ind].hFlip = false;

                        if (objectY + (y - lastMapHoverPoint.Y) < 0 || objectY + (y - lastMapHoverPoint.Y) > 7)
                        {
                            if (!overworldDrawer.objects[ind].is3Byte && !overworldDrawer.objects[ind].special ||
                                !overworldDrawer.objects[ind].special)
                            {
                                overworldDrawer.objects[ind].y = 0;
                                overworldDrawer.objects[ind].vFlip = false;
                            }
                            else
                            {
                                overworldDrawer.objects[ind].y = 0x0F;
                                overworldDrawer.objects[ind].vFlip = true;
                            }
                        }
                        else
                            overworldDrawer.objects[ind].vFlip = false;

                        drawOverworld();
                    }

                    if (ind != -1)
                        overworldDrawer.DrawSelectedObject((Bitmap)pMap.Image, selectedObject);
                }

                break;
            }
            case false when !cSprite.Checked:
            {
                var ind = (int)nSelected.Value;
                var x = e.X / 16;
                var y = e.Y / 16;
                if (e.Button == MouseButtons.Left)
                {
                    if (ind > -1)
                    {
                        if (objectX == 0xF)
                            objectX = -1;
                        if (objectY == 0xF)
                            objectY = -1;
                        dungeonDrawer.objects[ind].x = (byte)(objectX + (x - lastMapHoverPoint.X));
                        dungeonDrawer.objects[ind].y = (byte)(objectY + (y - lastMapHoverPoint.Y));
                        if (objectX + (x - lastMapHoverPoint.X) < 0 || objectX + (x - lastMapHoverPoint.X) > 9)
                            dungeonDrawer.objects[ind].x = 0;
                        if (objectY + (y - lastMapHoverPoint.Y) < 0 || objectY + (y - lastMapHoverPoint.Y) > 7)
                            dungeonDrawer.objects[ind].y = 0;
                        drawDungeon();
                    }

                    if (ind != -1)
                        dungeonDrawer.drawSelectedObject((Bitmap)pMap.Image, selectedObject);
                }

                break;
            }
            default:
            {
                if (cSprite.Checked)
                {
                    var ind = (int)nSpriteSelected.Value;
                    var x = e.X / 16;
                    var y = e.Y / 16;
                    if (e.Button == MouseButtons.Left)
                    {
                        if (ind > -1)
                        {
                            sprites.spriteList[ind].x = (byte)(objectX + (x - lastMapHoverPoint.X));
                            sprites.spriteList[ind].y = (byte)(objectY + (y - lastMapHoverPoint.Y));
                            if (objectX + (x - lastMapHoverPoint.X) < 0 || objectX + (x - lastMapHoverPoint.X) > 9)
                                sprites.spriteList[ind].x = 0;
                            if (objectY + (y - lastMapHoverPoint.Y) < 0 || objectY + (y - lastMapHoverPoint.Y) > 7)
                                sprites.spriteList[ind].y = 0;
                            if (!overWorld)
                                drawDungeon();
                            else
                                drawOverworld();
                            sprites.DrawSelectedSprite(pMap.Image, selectedObject);
                        }
                    }
                }

                break;
            }
        }

        //lastMapHoverPoint = new Point(e.X / 16, e.Y / 16);
        pMap.Invalidate();
    }

    private void saveROMToolStripMenuItem_Click(object sender, EventArgs e)
    {
        save();
    }

    private void save()
    {
        if (filename == "")
            return;
        bool save;
        var bl = gb.Buffer.ToList();
        var ba = bl.ToArray();
        var gbb = new GBHL.GBFile(ba);
        mapSaver.gb = gbb;
        patches.gb = gbb;
        if (overWorld)
        {
            if (usedSpace > freeSpace)
            {
                var dr = MessageBox.Show(
                    @"There is more space being used than allocated.\nSaving may corrupt the next rooms data. This is not\na permanent change and can be fixed.\n\nWould you like to save this data anyway?",
                    @"Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (dr == DialogResult.Yes)
                {
                    if (!cSprite.Checked)
                    {
                        save = mapSaver.saveOverworldCollision(
                            overworldDrawer.objects,
                            overworldDrawer.warps,
                            mapIndex,
                            floorTile,
                            wallTiles,
                            spriteBank,
                            cSpecialTiles.Checked,
                            usedSpace,
                            freeSpace,
                            cSpecialTiles.Checked,
                            overworldDrawer.pointers,
                            overworldDrawer.unSortedPointers);
                    }
                    else
                    {
                        save = mapSaver.saveSprites(sprites.spriteList, overWorld, mapIndex, dungeonIndex,
                            usedSpace, freeSpace, sprites.pointers, sprites.unSortedPointers);
                    }

                    if (save)
                    {
                        ba = bl.ToArray();
                        gbb = new GBHL.GBFile(ba);
                        mapSaver.gb = gbb;
                        patches.gb = gbb;
                    }
                }
                else
                    return;
            }
            else
            {
                if (!cSprite.Checked)
                {
                    save = mapSaver.saveOverworldCollision(
                        overworldDrawer.objects,
                        overworldDrawer.warps,
                        mapIndex,
                        floorTile,
                        wallTiles,
                        spriteBank,
                        cSpecialTiles.Checked,
                        usedSpace,
                        freeSpace,
                        cSpecialTiles.Checked,
                        overworldDrawer.pointers,
                        overworldDrawer.unSortedPointers);
                }
                else
                {
                    save = mapSaver.saveSprites(
                        sprites.spriteList,
                        overWorld,
                        mapIndex,
                        dungeonIndex,
                        usedSpace,
                        freeSpace,
                        sprites.pointers,
                        sprites.unSortedPointers);
                }

                if (save)
                {
                    ba = bl.ToArray();
                    gbb = new GBHL.GBFile(ba);
                    mapSaver.gb = gbb;
                    patches.gb = gbb;
                }
            }

            if (rOverlay.Checked)
                mapSaver.saveOverlay(mapData, mapIndex, cSpecialTiles.Checked);
        }
        else
        {
            if (usedSpace > freeSpace)
            {
                var dr = MessageBox.Show(
                    @"There is more space being used than allocated.\nSaving may corrupt the next rooms data. This is not\na permanent change and can be fixed.\n\nWould you like to save this data anyway?",
                    @"Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (dr == DialogResult.Yes)
                {
                    if (!cSprite.Checked)
                    {
                        save = mapSaver.saveDungeonCollision(dungeonDrawer.objects,
                            dungeonDrawer.warps,
                            dungeonIndex,
                            mapIndex,
                            floorTile,
                            wallTiles,
                            spriteBank,
                            usedSpace,
                            freeSpace,
                            dungeonDrawer.pointers,
                            dungeonDrawer.unSortedPointers,
                            cMagGlass.Checked);
                    }
                    else
                    {
                        save = mapSaver.saveSprites(sprites.spriteList,
                            overWorld,
                            mapIndex,
                            dungeonIndex,
                            usedSpace,
                            freeSpace,
                            sprites.pointers,
                            sprites.unSortedPointers);
                    }

                    if (save)
                    {
                        ba = bl.ToArray();
                        gbb = new GBHL.GBFile(ba);
                        mapSaver.gb = gbb;
                        patches.gb = gbb;
                    }
                }
                else
                    return;
            }
            else
            {
                if (!cSprite.Checked)
                {
                    save = mapSaver.saveDungeonCollision(dungeonDrawer.objects,
                        dungeonDrawer.warps,
                        dungeonIndex,
                        mapIndex,
                        floorTile,
                        wallTiles,
                        spriteBank,
                        usedSpace,
                        freeSpace,
                        dungeonDrawer.pointers,
                        dungeonDrawer.unSortedPointers,
                        cMagGlass.Checked);
                }
                else
                {
                    save = mapSaver.saveSprites(sprites.spriteList,
                        overWorld,
                        mapIndex,
                        dungeonIndex,
                        usedSpace,
                        freeSpace,
                        sprites.pointers,
                        sprites.unSortedPointers);
                }

                if (save)
                {
                    ba = bl.ToArray();
                    gbb = new GBHL.GBFile(ba);
                    mapSaver.gb = gbb;
                    patches.gb = gbb;
                }
            }
        }

        patches.DefaultMusic(defaultMusicToolStripMenuItem.Checked);
        //mapSaver.saveSprites(sprites.spriteList, overWorld, mapIndex, dungeonIndex, usedspace, freespace);
        mapSaver.saveMinimapInfo(overWorld, dungeonIndex, minimapDrawer.roomIndexes, minimapDrawer.minimapGraphics,
            minimapDrawer.overworldPal);
        mapSaver.saveAreaInfo(overWorld, mapIndex, dungeonIndex, animations, sog, music, cSpecialTiles.Checked,
            cMagGlass.Checked);
        mapSaver.saveChestInfo(overWorld, mapIndex, dungeonIndex, chestData);
        mapSaver.savePaletteInfo(palette, overWorld, cSideview.Checked, dungeonIndex, mapIndex, palOffset);
        if (!overWorld)
            mapSaver.saveDungeonEventInfo(dungeonIndex, mapIndex, (byte)nEventID.Value, (byte)nEventTrigger.Value);
        gb.Buffer = gbb.Buffer;
        writeFile();
    }

    private void writeFile()
    {
        var bw = new BinaryWriter(File.Open(filename, FileMode.Open));
        bw.Write(gb.Buffer);
        bw.Close();
        if (overWorld)
        {
            if (cSprite.Checked)
            {
                freeSpace = sprites.GetFreeSpace(true, mapIndex, dungeonIndex);
                usedSpace = sprites.GetUsedSpace();
            }
            else
            {
                freeSpace = overworldDrawer.GetFreeSpace(mapIndex, cSpecialTiles.Checked);
                usedSpace = overworldDrawer.GetUsedSpace();
            }

            drawOverworld();
        }
        else
        {
            if (cSprite.Checked)
            {
                freeSpace = sprites.GetFreeSpace(false, mapIndex, dungeonIndex);
                usedSpace = sprites.GetUsedSpace();
            }
            else
            {
                freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
                usedSpace = dungeonDrawer.getUsedSpace();
            }

            drawDungeon();
        }
    }

    private void pMinimapD_MouseClick(object sender, MouseEventArgs e)
    {
        if (pTiles.Image != null)
        {
            var s = e.X / 8 + e.Y / 8 * 8;
            mapIndex = minimapDrawer.roomIndexes[s];
            nMap.Value = mapIndex;
            LoadSandA();
            LoadTileset();
            collisionListView();
            setCollisionData();
            setSpriteData();
            drawDungeon();
        }
    }

    private void rCrystals_CheckedChanged_1(object sender, EventArgs e)
    {
        LoadTileset();
        drawDungeon();
    }

    private void cSideView_CheckedChanged(object sender, EventArgs e)
    {
        sideView = tabMapType.SelectedIndex == 2 ? cSideview2.Checked : cSideview.Checked;
        LoadSandA();
        LoadTileset();
        drawDungeon();
    }

    private void cSpecialTiles_CheckedChanged(object sender, EventArgs e)
    {
        overworldDrawer.GetCollisionDataOverworld(mapIndex, cSpecialTiles.Checked);
        overworldDrawer.LoadCollisionsOverworld();
        if (!cSprite.Checked)
        {
            usedSpace = overworldDrawer.GetUsedSpace();
            freeSpace = overworldDrawer.GetFreeSpace(mapIndex, cSpecialTiles.Checked);
        }
        else
        {
            usedSpace = sprites.GetUsedSpace();
            freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
        }

        LoadSandA();
        LoadTileset();
        drawOverworld();
    }

    private void nRegion_ValueChanged(object sender, EventArgs e)
    {
        dungeonIndex = (byte)nRegion.Value;
        LoadSandA();
        setCollisionData();
        setSpriteData();
        LoadTileset();
        collisionListView();
        dungeonDrawer.getCollisionDataDungeon(mapIndex, dungeonIndex, cMagGlass.Checked);
        dungeonDrawer.getEventData(mapIndex, dungeonIndex);
        nEventID.Value = dungeonDrawer.eventID;
        nEventTrigger.Value = dungeonDrawer.eventTrigger;
        label18.Text = @"Location: 0x" + dungeonDrawer.eventDataLocation.ToString("X");
        if (tabMapType.SelectedIndex == 2 && mapIndex == 0xF5 && dungeonIndex is >= 0x1A or < 6)
            cMagGlass.Visible = true;
        else
            cMagGlass.Visible = false;
        if (!cSprite.Checked)
        {
            freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
            usedSpace = dungeonDrawer.getUsedSpace();
        }
        else
        {
            freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
            usedSpace = sprites.GetUsedSpace();
        }

        WL = new List<Warps>();
        WL = dungeonDrawer.warps;
        drawDungeon();
    }

    private void rOverlay_CheckedChanged(object sender, EventArgs e)
    {
        if (!rOverlay.Checked) return;
        overLay = true;
        overworldDrawer.GetCollisionDataOverworld(mapIndex, cSpecialTiles.Checked);
        WL = new List<Warps>();
        WL = overworldDrawer.warps;
        drawOverworld();
    }

    private void rCollision_CheckedChanged(object sender, EventArgs e)
    {
        if (!rCollision.Checked) return;
        overLay = false;
        overworldDrawer.GetCollisionDataOverworld(mapIndex, cSpecialTiles.Checked);
        WL = new List<Warps>();
        WL = overworldDrawer.warps;
        setCollisionData();
        if (!cSprite.Checked)
        {
            freeSpace = overworldDrawer.GetFreeSpace(mapIndex, cSpecialTiles.Checked);
            usedSpace = overworldDrawer.GetUsedSpace();
        }
        else
        {
            freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
            usedSpace = sprites.GetUsedSpace();
        }

        collisionListView();
        drawOverworld();
    }

    private void collisionBordersToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void creditsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var credits = new Credits();
        credits.ShowDialog();
    }

    private void nAnimations_ValueChanged(object sender, EventArgs e)
    {
        animations = (byte)nAnimations.Value;
        LoadTileset();
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void nSOG_ValueChanged(object sender, EventArgs e)
    {
        sog = (byte)nSOG.Value;
        LoadTileset();
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void nFloor_ValueChanged(object sender, EventArgs e)
    {
        floorTile = (byte)nFloor.Value;
        LoadTileset();
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void nMusic_ValueChanged(object sender, EventArgs e)
    {
        music = (byte)nMusic.Value;
    }

    private void nWall_ValueChanged(object sender, EventArgs e)
    {
        wallTiles = (byte)nWall.Value;
        LoadTileset();
        if (!overWorld)
            drawDungeon();
        else
            drawOverworld();
    }

    private void nSpriteBank_ValueChanged(object sender, EventArgs e)
    {
        spriteBank = (byte)nSpriteBank.Value;
        LoadTileset();

        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void nSelected_ValueChanged(object sender, EventArgs e)
    {
        if (nSelected.Value == -1)
        {
            if (cObjectList.Checked)
            {
                if (CollisionList.SelectedIndex != -1)
                    CollisionList.SetSelected(CollisionList.SelectedIndex, false);
            }

            pObject.Invalidate();
            if (overWorld)
                drawOverworld();
            else
                drawDungeon();
            return;
        }

        var o = overWorld
            ? overworldDrawer.objects[(byte)nSelected.Value]
            : dungeonDrawer.objects[(byte)nSelected.Value];
        if (o.is3Byte)
        {
            comDirection.Enabled = true;
            nLength.Enabled = true;
            nLength.Value = o.length;
            comDirection.SelectedIndex = o.direction == 8 ? 0 : 1;
        }
        else
        {
            comDirection.Enabled = false;
            nLength.Enabled = false;
        }

        nObjectID.Value = o.id;
        if (cObjectList.Checked)
            CollisionList.SelectedIndex = (byte)nSelected.Value;
        selectedObject = o;
        lblHoverPos.Text = @"X: " + o.x.ToString("X") + @" Y: " + o.y.ToString("X");
        if (overWorld)
        {
            drawOverworld();
            overworldDrawer.DrawSelectedObject((Bitmap)pMap.Image, selectedObject);
        }
        else
        {
            drawDungeon();
            dungeonDrawer.drawSelectedObject((Bitmap)pMap.Image, selectedObject);
        }

        pObject.Invalidate();
    }

    private void selectCollision(LAObject o, int index)
    {
        if (index == -1)
        {
            if (cSprite.Checked)
            {
                nSpriteSelected.Value = -1;
                nSpriteID.Value = 0;
            }
            else
            {
                nSelected.Value = -1;
                comDirection.Enabled = false;
                nLength.Enabled = false;
                nObjectID.Value = 0;
                pObject.Invalidate();
            }

            return;
        }

        if (cSprite.Checked)
        {
            nSpriteSelected.Value = index;
            nSpriteID.Value = o.id;
        }
        else
        {
            nSelected.Value = index;
            if (o.is3Byte)
            {
                comDirection.Enabled = true;
                nLength.Enabled = true;
            }

            comDirection.SelectedIndex = o.direction == 8 ? 0 : 1;
            nLength.Value = o.length;
            nObjectID.Value = o.id;
        }
    }

    private void nObjectID_ValueChanged(object sender, EventArgs e)
    {
        if (nSelected.Value == -1)
            return;
        LAObject o;
        if (overWorld)
        {
            o = overworldDrawer.objects[(byte)nSelected.Value];
            o.id = (byte)nObjectID.Value;
            o = o.getOverworldSpecial(o);
            overworldDrawer.objects[(byte)nSelected.Value] = o;
        }
        else
        {
            o = dungeonDrawer.objects[(byte)nSelected.Value];
            o.id = (byte)nObjectID.Value;
            if (o.id is >= 0xEC and <= 0xFD) // Door tiles
            {
                o.gb = gb;
                o = o.dungeonDoors(o);
                dungeonDrawer.objects[(byte)nSelected.Value] = o;
            }
            else
            {
                o.isDoor2 = false;
                o.isEntrance = false;
                o.isDoor1 = false;
            }
        }

        if (cObjectList.Checked)
        {
            var s = o.is3Byte ? "3-Byte" : "2-Byte";
            s += "      0x" + o.id.ToString("X");
            CollisionList.Items[(int)nSelected.Value] = s;
        }

        selectedObject = o;
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
        pObject.Invalidate();
    }

    private void nLength_ValueChanged(object sender, EventArgs e)
    {
        if (nSelected.Value == -1)
            return;
        var o = overWorld
            ? overworldDrawer.objects[(byte)nSelected.Value]
            : dungeonDrawer.objects[(byte)nSelected.Value];
        o.length = (byte)nLength.Value;
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void comDirection_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (nSelected.Value == -1)
            return;
        var o = overWorld
            ? overworldDrawer.objects[(byte)nSelected.Value]
            : dungeonDrawer.objects[(byte)nSelected.Value];
        o.direction = comDirection.SelectedIndex == 0 ? (byte)8 : (byte)0xC;
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void collisionListView()
    {
        if (!cObjectList.Checked) return;
        CollisionList.Items.Clear();
        var obj = overWorld ? overworldDrawer.objects : dungeonDrawer.objects;
        foreach (var s in from ob in obj
                 let s = ob.is3Byte ? "3-Byte" : "2-Byte"
                 select s + "      0x" + ob.id.ToString("X"))
        {
            CollisionList.Items.Add(s);
        }

        CollisionList.SelectedIndex = (int)nSelected.Value;
    }

    private void CollisionList_SelectedIndexChanged(object sender, EventArgs e)
    {
        nSelected.Value = CollisionList.SelectedIndex;
        if (nSelected.Value == -1) return;
        if (overWorld)
        {
            drawOverworld();
            overworldDrawer.DrawSelectedObject((Bitmap)pMap.Image, selectedObject);
        }
        else
        {
            drawDungeon();
            dungeonDrawer.drawSelectedObject((Bitmap)pMap.Image, selectedObject);
        }
    }

    private void pObject_Paint(object sender, PaintEventArgs e)
    {
        if (pTiles.Image == null || overLay) return;
        if (nSelected.Value == -1)
        {
            pObject.Width = 20;
            pObject.Height = 20;
            return;
        }

        if (!overWorld)
        {
            if (selectedObject == null) return;
            if (selectedObject.id < 0xEC || selectedObject.id > 0xFD || selectedObject.is3Byte)
            {
                pObject.Width = 20;
                pObject.Height = 20;
                e.Graphics.DrawImage(pTiles.Image, new Rectangle(0, 0, 16, 16),
                    selectedObject.id % 16 * 16, selectedObject.id / 16 * 16, 16, 16,
                    GraphicsUnit.Pixel);
            }
            else
            {
                pObject.Width = selectedObject.w * 16 + 4;
                pObject.Height = selectedObject.h * 16 + 4;
                for (var y = 0; y < selectedObject.h; y++)
                {
                    for (var x = 0; x < selectedObject.w; x++)
                    {
                        int id = selectedObject.tiles[y * selectedObject.w + x];
                        e.Graphics.DrawImage(pTiles.Image,
                            new Rectangle(0 + x * 16, 0 + y * 16, 16, 16), id % 16 * 16,
                            id / 16 * 16, 16, 16, GraphicsUnit.Pixel);
                    }
                }
            }
        }
        else
        {
            if (selectedObject == null) return;
            if (selectedObject.id is < 0xF5 or >= 0xFE)
            {
                pObject.Width = selectedObject.w * 16 + 4;
                pObject.Height = selectedObject.h * 16 + 4;
                e.Graphics.DrawImage(pTiles.Image, new Rectangle(0, 0, 16, 16),
                    selectedObject.id % 16 * 16, selectedObject.id / 16 * 16, 16, 16,
                    GraphicsUnit.Pixel);
            }
            else
            {
                pObject.Width = selectedObject.w * 16 + 4;
                pObject.Height = selectedObject.h * 16 + 4;
                for (var y = 0; y < selectedObject.h; y++)
                {
                    for (var x = 0; x < selectedObject.w; x++)
                    {
                        int id = selectedObject.tiles[y * selectedObject.w + x];
                        e.Graphics.DrawImage(pTiles.Image,
                            new Rectangle(0 + x * 16, 0 + y * 16, 16, 16), id % 16 * 16,
                            id / 16 * 16, 16, 16, GraphicsUnit.Pixel);
                    }
                }
            }
        }
    }

    private void bCollisionUp_Click(object sender, EventArgs e)
    {
        if (CollisionList.Items.Count == 0 || nSelected.Value == -1)
            return;
        var list = CollisionList.SelectedItem;
        var index = (byte)CollisionList.SelectedIndex;
        var objects = overWorld ? overworldDrawer.objects : dungeonDrawer.objects;
        var O = objects[index];
        if (index == 0)
            return;
        objects.Remove(O);
        objects.Insert(index - 1, O);
        CollisionList.Items.Remove(list);
        CollisionList.Items.Insert(index - 1, list);

        CollisionList.Items.Clear();
        foreach (var s in from obj in objects
                 let s = obj.is3Byte ? "3-Byte" : "2-Byte"
                 select s + ("      0x" + obj.id.ToString("X")))
        {
            CollisionList.Items.Add(s);
        }

        CollisionList.SelectedIndex = index - 1;

        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void bCollisionDown_Click(object sender, EventArgs e)
    {
        if (CollisionList.Items.Count == 0 || nSelected.Value == -1)
            return;
        var list = CollisionList.SelectedItem;
        var index = (byte)CollisionList.SelectedIndex;
        var objects = overWorld ? overworldDrawer.objects : dungeonDrawer.objects;
        var O = objects[index];
        if (index == nSelected.Maximum)
            return;
        objects.Remove(O);
        objects.Insert(index + 1, O);
        CollisionList.Items.Remove(list);
        CollisionList.Items.Insert(index + 1, list);

        CollisionList.Items.Clear();
        foreach (var s in from obj in objects let s = obj.is3Byte ? "3-Byte" : "2-Byte" select s + ("      0x" + obj.id.ToString("X")))
        {
            CollisionList.Items.Add(s);
        }

        CollisionList.SelectedIndex = index + 1;

        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void pTiles_Paint(object sender, PaintEventArgs e)
    {
        pObject.Invalidate();
    }

    private void cObjectList_CheckedChanged(object sender, EventArgs e)
    {
        if (!cObjectList.Checked)
        {
            CollisionList.Enabled = false;
            bCollisionDown.Enabled = false;
            bCollisionUp.Enabled = false;
            bAdd.Enabled = false;
            bDelete.Enabled = false;
            CollisionList.Items.Clear();
        }
        else
        {
            bAdd.Enabled = true;
            bDelete.Enabled = true;
            CollisionList.Enabled = true;
            bCollisionDown.Enabled = true;
            bCollisionUp.Enabled = true;
            collisionListView();
        }
    }

    private void toolStripWarpEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        if (cSprite.Checked)
        {
            MessageBox.Show(@"You must be editing the collisions/overlay to edit warps.");
        }
        else
        {
            var backup = WL.ToList();
            var WEdit = new WarpEditor(backup);
            WEdit.ShowDialog();
            if (WEdit.DialogResult != DialogResult.OK) return;
            WL = WEdit.warpList;
            if (overWorld)
            {
                overworldDrawer.warps = backup;
                usedSpace = overworldDrawer.GetUsedSpace();
                drawOverworld();
            }
            else
            {
                dungeonDrawer.warps = backup;
                usedSpace = dungeonDrawer.getUsedSpace();
                drawDungeon();
            }
        }
    }

    private void bAdd_Click(object sender, EventArgs e)
    {
        var newOb = new NewObject(gb, overWorld);
        if (newOb.ShowDialog() != DialogResult.OK) return;
        var O = newOb.O;
        selectedObject = O;
        if (overWorld)
        {
            if (nSelected.Value == -1)
                overworldDrawer.objects.Add(O);
            else
                overworldDrawer.objects.Insert((byte)nSelected.Value, O);
            if (O.is3Byte)
            {
                //freespace -= 3;
                usedSpace += 3;
            }
            else
            {
                //freespace -= 2;
                usedSpace += 2;
            }

            collisionListView();
            drawOverworld();
            if (nSelected.Value == -1)
                nSelected.Value = overworldDrawer.objects.Count - 1;
        }
        else
        {
            if (nSelected.Value == -1)
                dungeonDrawer.objects.Add(O);
            else
                dungeonDrawer.objects.Insert((byte)nSelected.Value, O);
            if (O.is3Byte)
            {
                //freespace -= 3;
                usedSpace += 3;
            }
            else
            {
                //freespace -= 2;
                usedSpace += 2;
            }

            collisionListView();
            drawDungeon();
            if (nSelected.Value == -1)
                nSelected.Value = dungeonDrawer.objects.Count - 1;
        }

        pObject.Invalidate();
    }

    private void bDelete_Click(object sender, EventArgs e)
    {
        if (nSelected.Value == -1)
            return;
        var O = new LAObject();
        if (overWorld)
        {
            O = overworldDrawer.objects[(byte)nSelected.Value];
            if (O.is3Byte)
            {
                //freespace += 3;
                usedSpace -= 3;
            }
            else
            {
                //freespace += 2;
                usedSpace -= 2;
            }

            overworldDrawer.objects.RemoveAt((byte)nSelected.Value);
            if ((byte)nSelected.Value == overworldDrawer.objects.Count)
                nSelected.Value -= 1;
            if (nSelected.Value != -1)
                selectedObject = overworldDrawer.objects[(byte)nSelected.Value];
        }
        else
        {
            if (O.is3Byte)
            {
                //freespace += 3;
                usedSpace -= 3;
            }
            else
            {
                //freespace += 2;
                usedSpace -= 2;
            }

            dungeonDrawer.objects.RemoveAt((byte)nSelected.Value);
            if ((byte)nSelected.Value == dungeonDrawer.objects.Count)
                nSelected.Value -= 1;
            if (nSelected.Value != -1)
                selectedObject = dungeonDrawer.objects[(byte)nSelected.Value];
        }

        collisionListView();
        //pObject.Invalidate();
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void cm_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
    {
        // var item = e.ClickedItem;
        if (overWorld)
            overworldDrawer.objects.Clear();
        else
            dungeonDrawer.objects.Clear();
        nSelected.Value = -1;
        collisionListView();
        if (overWorld)
        {
            usedSpace = overworldDrawer.GetUsedSpace();
            freeSpace = overworldDrawer.GetFreeSpace(mapIndex, cSpecialTiles.Checked);
            drawOverworld();
        }
        else
        {
            usedSpace = dungeonDrawer.getUsedSpace();
            freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
            drawDungeon();
        }
    }

    private void CollisionList_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return;
        CollisionList.ContextMenuStrip = new ContextMenuStrip();
        CollisionList.ContextMenuStrip.Items.Add("Clear All");
        CollisionList.ContextMenuStrip.Show(Cursor.Position);
        CollisionList.ContextMenuStrip.ItemClicked += cm_ItemClicked;
    }

    private void pMap_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        var ind = GetDAndOObjectID(e.X, e.Y);
        if (overWorld && !overLay && !cSprite.Checked)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (ind > -1)
                {
                    overworldDrawer.objects.RemoveAt(ind);
                    if (nSelected.Value == overworldDrawer.objects.Count)
                        nSelected.Value--;
                    collisionListView();
                    usedSpace = overworldDrawer.GetUsedSpace();
                    drawOverworld();
                    if (nSelected.Value != -1)
                        selectedObject = overworldDrawer.objects[(byte)nSelected.Value];
                    pObject.Invalidate();
                }
            }
        }
        else if (!overLay)
        {
            if (!cSprite.Checked)
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (ind > -1)
                    {
                        dungeonDrawer.objects.RemoveAt(ind);
                        if (nSelected.Value == dungeonDrawer.objects.Count)
                            nSelected.Value--;
                        usedSpace = dungeonDrawer.getUsedSpace();
                        collisionListView();
                        drawDungeon();
                        if (nSelected.Value != -1)
                            selectedObject = dungeonDrawer.objects[(byte)nSelected.Value];
                        pObject.Invalidate();
                    }
                }
            }
        }

        lastMapHoverPoint = new Point(e.X / 16, e.Y / 16);
        pMap.Invalidate();
    }

    private void toolChestEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var CE = new ChestEditor(chestData, tabMapType.SelectedIndex);
        CE.ShowDialog();
        if (CE.DialogResult == DialogResult.OK)
            chestData = CE.chestData;
    }

    private void toolMiniMapEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null || tabMapType.SelectedIndex == 2) return;
        var bmp = new Bitmap(128, 128);
        var fp = new FastPixel(bmp);
        var src = !overWorld ? new FastPixel((Bitmap)pMinimapD.Image) : new FastPixel((Bitmap)pMinimap.Image);
        src.rgbValues = new byte[128 * 128 * 4];
        fp.rgbValues = new byte[128 * 128 * 4];
        fp.Lock();
        src.Lock();
        if (!overWorld)
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    for (var y1 = 0; y1 < 8; y1++)
                    {
                        for (var x1 = 0; x1 < 8; x1++)
                        {
                            fp.SetPixel(x * 8 + x1, y * 8 + y1, src.GetPixel(x * 8 + x1, y * 8 + y1));
                        }
                    }
                }
            }
        }
        else
        {
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    for (var y1 = 0; y1 < 8; y1++)
                    {
                        for (var x1 = 0; x1 < 8; x1++)
                        {
                            fp.SetPixel(x * 8 + x1, y * 8 + y1, src.GetPixel(x * 8 + x1, y * 8 + y1));
                        }
                    }
                }
            }
        }

        fp.Unlock(true);
        src.Unlock(true);
        var ri = new List<byte>();
        var op = new List<byte>();
        if (!overWorld)
        {
            ri.AddRange(minimapDrawer.roomIndexes);
        }
        else
        {
            op.AddRange(minimapDrawer.overworldPal);
        }

        var loadmini = !overWorld ? minimapDrawer.LoadMinimapDungeon() : minimapDrawer.LoadMinimapOverworld();
        var mga = minimapDrawer.minimapGraphics.ToArray();
        var ria = ri.ToArray();
        var opa = op.ToArray();
        var g = new GBHL.GBFile(gb.Buffer.ToArray());
        var ME = new MinimapEditor(g, bmp, ria, loadmini, mga, overWorld, opa, dungeonIndex);
        ME.ShowDialog();
        if (ME.DialogResult != DialogResult.OK) return;
        minimapDrawer.minimapGraphics = ME.minimapData;
        if (overWorld)
        {
            minimapDrawer.overworldPal = ME.overworldpal;
            pMinimap.Image = ME.pMinimapO.Image;
        }
        else
        {
            minimapDrawer.roomIndexes = ME.roomIndexes;
            pMinimapD.Image = ME.pMinimap.Image;
            gb.Buffer = ME.gb.Buffer;
        }
    }

    private void nSpriteSelected_ValueChanged(object sender, EventArgs e)
    {
        if (nSpriteSelected.Value == -1)
        {
            nSpriteID.Value = 0;
            if (overWorld)
                drawOverworld();
            else
                drawDungeon();
            return;
        }
        
        var o = sprites.spriteList[(byte)nSpriteSelected.Value];
        nSpriteID.Value = o.id;
        selectedObject = o;
        lblHoverPos.Text = @"X: " + o.x.ToString("X") + @" Y: " + o.y.ToString("X");
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
        sprites.DrawSelectedSprite(pMap.Image, selectedObject);
    }

    private void cSprite_CheckedChanged(object sender, EventArgs e)
    {
        if (cSprite.Checked)
        {
            bAddSprite.Enabled = true;
            bDeleteSprite.Enabled = true;
            nSpriteSelected.Enabled = true;
            nSpriteID.Enabled = true;
            nSelected.Value = -1;
            if (cObjectList.Checked)
                cObjectList.Checked = false;
            usedSpace = sprites.GetUsedSpace();
            freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
        }
        else
        {
            nSpriteID.Value = 0;
            nSpriteSelected.Enabled = false;
            nSpriteID.Enabled = false;
            bAddSprite.Enabled = false;
            bDeleteSprite.Enabled = false;
        }

        if (overWorld)
        {
            if (!cSprite.Checked)
            {
                usedSpace = overworldDrawer.GetUsedSpace();
                freeSpace = overworldDrawer.GetFreeSpace(mapIndex, cSpecialTiles.Checked);
            }

            drawOverworld();
        }
        else
        {
            if (!cSprite.Checked)
            {
                usedSpace = dungeonDrawer.getUsedSpace();
                freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
            }

            drawDungeon();
        }

        setSpriteData();
    }

    private void nSpriteID_ValueChanged(object sender, EventArgs e)
    {
        if (nSpriteSelected.Value == -1)
        {
            spriteLabel.Text = @"Sprite: null";
            return;
        }

        sprites.spriteList[(byte)nSpriteSelected.Value].id = (byte)nSpriteID.Value;

        spriteLabel.Text = @"Sprite: " + Names.GetName(Names.Sprites, (int)nSpriteID.Value);
    }

    private void bAddSprite_Click(object sender, EventArgs e)
    {
        var o = new LAObject
        {
            id = 0,
            x = 0,
            y = 0
        };
        sprites.spriteList.Add(o);
        nSpriteSelected.Maximum += 1;
        usedSpace += 2;
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void bDeleteSprite_Click(object sender, EventArgs e)
    {
        if (nSpriteSelected.Value == -1) return;
        var ind = sprites.spriteList.IndexOf(selectedObject);
        sprites.spriteList.RemoveAt(ind);
        nSpriteSelected.Value -= 1;
        nSpriteSelected.Maximum -= 1;
        usedSpace -= 2;
        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void exportMapToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null || !overLay || tabMapType.SelectedIndex != 0) return;
        var s = new SaveFileDialog();
        s.Title = @"Export Map Group";
        s.Filter = @"PNG Files (*.png)|*.png";
        if (s.ShowDialog() != DialogResult.OK)
            return;
        exportToFilename = s.FileName;
        exportMaps = new ExportMap();
        exportMaps.pBar.Maximum = 256;
        exportMaps.Text = @"Generating Map";
        new Thread(exportTheMaps).Start();
        exportMaps.ShowDialog();
    }

    private void exportTheMaps()
    {
        var i = 0;
        var xp = 0;
        var yp = 0;
        var b = new Bitmap(2560, 2048);
        //var srcB = new Bitmap(256, 256);
        var fp = new FastPixel(b)
        {
            rgbValues = new byte[2560 * 2048 * 4]
        };
        fp.Lock();
        while (i != 256)
        {
            exportMaps.setValue(i, 255);

            tileLoader.getAnimations((byte)i, 0, true, cSpecialTiles.Checked);
            tileLoader.getSOG((byte)i, true);
            var data = tileLoader.loadTileset(0, (byte)i, true, false, false);
            tileLoader.loadPallete(0, (byte)i, true, false);
            var tileZ = tileLoader.loadPaletteFlipIndexes((byte)i, 0);
            var srcB = tileLoader.drawTileset(data, tileZ);
            var src = new FastPixel(srcB)
            {
                rgbValues = new byte[256 * 256 * 4]
            };
            src.Lock();

            var bigMap = overworldDrawer.ReadMap((byte)i, cSpecialTiles.Checked);
            if (xp == 16)
            {
                yp++;
                xp = 0;
            }

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    var v = bigMap[x + y * 10];

                    for (var yy = 0; yy < 16; yy++)
                    {
                        for (var xx = 0; xx < 16; xx++)
                        {
                            fp.SetPixel(xp * 160 + x * 16 + xx, yp * 128 + y * 16 + yy,
                                src.GetPixel(v % 16 * 16 + xx, v / 16 * 16 + yy));
                        }
                    }
                }
            }

            i++;
            xp++;
            src.Unlock(true);
        }

        fp.Unlock(true);
        if (exportToFilename != "")
            b.Save(exportToFilename);
    }

    private void cMagGlass_CheckedChanged(object sender, EventArgs e)
    {
        if (tabMapType.SelectedIndex == 1)
        {
            cMagGlass.Checked = cMagGlass1.Checked;
        }

        dungeonDrawer.getCollisionDataDungeon(mapIndex, dungeonIndex, cMagGlass.Checked);
        dungeonDrawer.loadCollisionsDungeon();
        if (!cSprite.Checked)
        {
            usedSpace = dungeonDrawer.getUsedSpace();
            freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
        }
        else
        {
            usedSpace = sprites.GetUsedSpace();
            freeSpace = sprites.GetFreeSpace(overWorld, mapIndex, dungeonIndex);
        }

        cObjectList.Checked = false;
        nSelected.Value = -1;
        LoadSandA();
        LoadTileset();
        drawDungeon();
    }

    private void toolStartPosEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var SE = new StartEditor(gb);
        SE.ShowDialog();
        if (SE.DialogResult != DialogResult.OK) return;
        startXPos = SE.xPos;
        startYPos = SE.yPos;
        SEMap = SE.map;
        SEDung = SE.dungeon;
        SEOverworld = SE.overworld;
        mapSaver.saveStartPos(SEOverworld, SEDung, SEMap, startXPos, startYPos);
    }

    private void toolTextEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var backup = new byte[gb.Buffer.Length];
        var i = 0;
        foreach (var b in gb.Buffer)
        {
            backup[i] = b;
            i++;
        }

        var TE = new TextEditor(backup);
        TE.ShowDialog();

        if (TE.DialogResult == DialogResult.OK)
        {
            gb.Buffer = TE.gb.Buffer;
        }
    }

    private void tSpriteEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var SE = new SpriteEditor(gb, overWorld, mapIndex, dungeonIndex);
        SE.ShowDialog();

        if (SE.DialogResult == DialogResult.OK)
        {
            gb.Buffer = SE.gb.Buffer;
        }
    }

    private void toolStripButton1_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var backup = new byte[gb.Buffer.Length];
        var i = 0;
        foreach (var b in gb.Buffer)
        {
            backup[i] = b;
            i++;
        }

        var SE = new SignEditor(backup, mapIndex);
        SE.ShowDialog();

        if (SE.DialogResult == DialogResult.OK)
        {
            gb.Buffer = SE.gb.Buffer;
        }
    }

    private void toolOwlStatueEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var backup = new byte[gb.Buffer.Length];
        var i = 0;
        foreach (var b in gb.Buffer)
        {
            backup[i] = b;
            i++;
        }

        var OE = new OwlStatueEditor(backup);
        OE.ShowDialog();

        if (OE.DialogResult == DialogResult.OK)
        {
            gb.Buffer = OE.gb.Buffer;
        }
    }

    private void clearOverlayToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (!overLay || pTiles.Image == null) return;
        var i = 0;
        foreach (var _ in mapData)
        {
            mapData[i] = 0;
            i++;
        }

        pMap.Image = overworldDrawer.DrawMap(tileset, mapData, collisionBordersToolStripMenuItem.Checked);
    }

    private void toolCopy_Click(object sender, EventArgs e)
    {
        if (!overLay || pTiles.Image == null) return;
        var i = 0;
        mapDataCopy = new byte[mapData.Length];
        foreach (var b in mapData)
        {
            mapDataCopy[i] = b;
            i++;
        }
    }

    private void toolPaste_Click(object sender, EventArgs e)
    {
        if (!overLay || mapDataCopy == null || pTiles.Image == null) return;
        var i = 0;
        foreach (var b in mapDataCopy)
        {
            mapData[i] = b;
            i++;
        }

        pMap.Image = overworldDrawer.DrawMap(tileset, mapData, collisionBordersToolStripMenuItem.Checked);
    }

    private void exportDungeonToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null || overLay || tabMapType.SelectedIndex != 1) return;
        var s = new SaveFileDialog();
        s.Title = @"Export Dungeon Map Group";
        s.Filter = @"PNG Files (*.png)|*.png";
        if (s.ShowDialog() != DialogResult.OK)
            return;
        exportToFilename = s.FileName;
        exportMaps = new ExportMap();
        exportMaps.pBar.Maximum = 64;
        exportMaps.Text = @"Generating Map";
        new Thread(exportTheDungeonMaps).Start();
        exportMaps.ShowDialog();
    }

    private void exportTheDungeonMaps()
    {
        var i = 0;
        var xp = 0;
        var yp = 0;
        var b = new Bitmap(1280, 1024);
        var fp = new FastPixel(b)
        {
            rgbValues = new byte[1280 * 1024 * 4]
        };
        fp.Lock();

        var sideViewIndexes = new byte[64];
        var roomIndexes = gb.ReadBytes(0x50220 + 64 * dungeonIndex, 64);
        if (dungeonIndex == 0xFF)
            roomIndexes = gb.ReadBytes(0x504E0, 64);
        for (var qq = 0; qq < 64; qq++)
        {
            dungeonDrawer.getCollisionDataDungeon(roomIndexes[qq], dungeonIndex, false);
            if (dungeonDrawer.warps != null)
            {
                foreach (var q in from t in dungeonDrawer.warps where t.type == 2 select Array.IndexOf(roomIndexes, t.map))
                {
                    if (q == -1)
                        sideViewIndexes[qq] = 0;
                    else
                        sideViewIndexes[q] = 1;
                }
            }
            else
                sideViewIndexes[qq] = 0;
        }

        while (i != 64)
        {
            var sView = sideViewIndexes[i] != 0;
            exportMaps.setValue(i, 63);

            tileLoader.getAnimations(roomIndexes[i], dungeonIndex, false, false);
            tileLoader.getSOG(roomIndexes[i], false);
            var data =
                tileLoader.loadTileset(dungeonIndex, roomIndexes[i], false, rCrystals.Checked, sView);
            tileLoader.loadPallete(dungeonIndex, roomIndexes[i], false, sView);
            var tileZ = tileLoader.loadPaletteFlipIndexes(roomIndexes[i], dungeonIndex);
            var srcB = tileLoader.drawTileset(data, tileZ);
            var src = new FastPixel(srcB)
            {
                rgbValues = new byte[256 * 256 * 4]
            };
            src.Lock();

            dungeonDrawer.getCollisionDataDungeon(roomIndexes[i], dungeonIndex, false);
            dungeonDrawer.getWallsandFloor(dungeonIndex, roomIndexes[i], false);
            dungeonDrawer.loadCollisionsDungeon();
            var bigMap = dungeonDrawer.data;

            if (xp == 8)
            {
                yp++;
                xp = 0;
            }

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    var v = bigMap[x + y * 10];

                    for (var yy = 0; yy < 16; yy++)
                    {
                        for (var xx = 0; xx < 16; xx++)
                        {
                            fp.SetPixel(xp * 160 + x * 16 + xx, yp * 128 + y * 16 + yy,
                                src.GetPixel(v % 16 * 16 + xx, v / 16 * 16 + yy));
                        }
                    }
                }
            }

            i++;
            xp++;
            src.Unlock(true);
        }

        fp.Unlock(true);
        if (exportToFilename != "")
            b.Save(exportToFilename);
    }

    private void repointCollisionsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        if (overLay && !cSprite.Checked)
            return;
        var backup = new byte[gb.Buffer.Length];
        var i = 0;
        foreach (var b in gb.Buffer)
        {
            backup[i] = b;
            i++;
        }

        int address;
        List<Warps> warps;
        List<LAObject> objects;

        switch (overWorld)
        {
            case true when !cSprite.Checked:
                address = overworldDrawer.mapAddress;
                objects = overworldDrawer.objects;
                warps = overworldDrawer.warps;
                break;
            case false when !cSprite.Checked:
                address = dungeonDrawer.mapAddress;
                objects = dungeonDrawer.objects;
                warps = dungeonDrawer.warps;
                break;
            default:
                address = sprites.objectAddress;
                objects = sprites.spriteList;
                warps = null;
                break;
        }

        var RC = new RepointCollision(backup,
            overWorld, dungeonIndex, mapIndex, address,
            cSpecialTiles.Checked, cMagGlass.Checked, cSprite.Checked,
            warps, objects, wallTiles, floorTile);
        RC.ShowDialog();

        if (RC.DialogResult != DialogResult.OK) return;
        gb.Buffer = RC.gb.Buffer;
        if (overWorld)
        {
            if (!cSprite.Checked)
            {
                overworldDrawer.LoadCollisionsOverworld();

                overworldDrawer.GetCollisionDataOverworld(mapIndex, cSpecialTiles.Checked);
                overworldDrawer.GetFloor(mapIndex, cSpecialTiles.Checked);
                usedSpace = overworldDrawer.GetUsedSpace();
                freeSpace = overworldDrawer.GetFreeSpace(mapIndex, cSpecialTiles.Checked);
                drawOverworld();
            }
            else
            {
                sprites.LoadObjects(true, dungeonIndex, mapIndex);
                usedSpace = sprites.GetUsedSpace();
                freeSpace = sprites.GetFreeSpace(true, mapIndex, dungeonIndex);
                drawOverworld();
                setSpriteData();
                drawSprites();
            }
        }
        else
        {
            if (!cSprite.Checked)
            {
                dungeonDrawer.loadCollisionsDungeon();
                dungeonDrawer.getWallsandFloor(dungeonIndex, mapIndex, cMagGlass.Checked);
                dungeonDrawer.getCollisionDataDungeon(mapIndex, dungeonIndex, cMagGlass.Checked);
                usedSpace = dungeonDrawer.getUsedSpace();
                freeSpace = dungeonDrawer.getFreeSpace(mapIndex, dungeonIndex, cMagGlass.Checked);
                drawDungeon();
            }
            else
            {
                sprites.LoadObjects(false, dungeonIndex, mapIndex);
                usedSpace = sprites.GetUsedSpace();
                freeSpace = sprites.GetFreeSpace(true, mapIndex, dungeonIndex);
                drawDungeon();
                setSpriteData();
                drawSprites();
            }
        }
    }

    private void pMap_MouseLeave(object sender, EventArgs e)
    {
        lastMapHoverPoint = new Point(-1, -1);
    }

    private void toolMinibossEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var backup = new byte[gb.Buffer.Length];
        var i = 0;
        foreach (var b in gb.Buffer)
        {
            backup[i] = b;
            i++;
        }

        var ME = new MinibossEditor(backup, overWorld, mapIndex);
        ME.ShowDialog();

        if (ME.DialogResult == DialogResult.OK)
        {
            gb.Buffer = ME.gb.Buffer;
        }
    }

    private void toolPaletteEditor_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var backup = new byte[gb.Buffer.Length];
        var i = 0;
        foreach (var b in gb.Buffer)
        {
            backup[i] = b;
            i++;
        }

        var PE = new PaletteEditor(tileLoader, this, palette, backup, dungeonIndex, mapIndex,
            overWorld, cSideview.Checked, cSpecialTiles.Checked, rCrystals.Checked, palOffset);
        PE.ShowDialog();

        if (PE.DialogResult != DialogResult.OK) return;
        palette = PE.palette;
        tileLoader.palette = palette;
        palOffset = PE.offset;
        tileLoader.getAnimations(mapIndex, dungeonIndex, overWorld, cSpecialTiles.Checked);
        tileLoader.getSOG(mapIndex, overWorld);
        var data = tileLoader.loadTileset(dungeonIndex, mapIndex, overWorld,
            rCrystals.Checked, cSideview.Checked);
        var tileZ = tileLoader.loadPaletteFlipIndexes(mapIndex, dungeonIndex);
        pTiles.Image = tileLoader.drawTileset(data, tileZ);
        tileset = (Bitmap)pTiles.Image;

        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void copyMapPaletteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null) return;
        var colors = new Color[32];
        paletteCopy = new Color[8, 4];
        var q = 0;
        for (var k = 0; k < 8; k++)
        for (var i = 0; i < 4; i++)
        {
            colors[q] = palette[k, i];
            q++;
        }

        var f = 0;
        q = 0;
        foreach (var c in colors)
        {
            if (q > 3)
            {
                f++;
                q = 0;
            }

            paletteCopy[f, q] = c;
            q++;
        }
    }

    private void pasteMapPaletteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (pTiles.Image == null || paletteCopy == null) return;
        palette = paletteCopy;
        tileLoader.palette = paletteCopy;
        tileLoader.getAnimations(mapIndex, dungeonIndex, overWorld, cSpecialTiles.Checked);
        tileLoader.getSOG(mapIndex, overWorld);
        var data = tileLoader.loadTileset(dungeonIndex, mapIndex, overWorld, rCrystals.Checked,
            sideView);
        var tileZ = tileLoader.loadPaletteFlipIndexes(mapIndex, dungeonIndex);
        pTiles.Image = tileLoader.drawTileset(data, tileZ);
        tileset = (Bitmap)pTiles.Image;

        if (overWorld)
            drawOverworld();
        else
            drawDungeon();
    }

    private void forumsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var p = new System.Diagnostics.Process();
        p.StartInfo = new System.Diagnostics.ProcessStartInfo("https://zeldahacking.net/");
        p.Start();
    }
}