using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LALE;

public partial class ChestEditor : Form
{
    public byte chestData;

    private readonly Dictionary<string, byte> items = new();

    private void AddGenericItems()
    {
        items.Add("Power Bracelet", 0x0);
        items.Add("Shield", 0x1);
        items.Add("Bow (No Arrows)", 0x2);
        items.Add("Hookshot", 0x3);
        items.Add("Magic Rod", 0x4);
        items.Add("Pegasus Boots", 0x5);
        items.Add("Ocarina", 0x6);
        items.Add("Roc's Feather", 0x7);
        items.Add("Shovel", 0x8);
        items.Add("Magic Powder (No powder)", 0x9);
        items.Add("Bomb (1)", 0xA);
        items.Add("Sword", 0xB);
        items.Add("Flippers", 0xC);
        items.Add("Magnifying Lens", 0xD);
        items.Add("Secret Medicine", 0x10);
        items.Add("20 Rupees", 0x1C);
        items.Add("50 Rupees", 0x1B);
        items.Add("100 Rupees", 0x1D);
        items.Add("200 Rupees", 0x1E);
        items.Add("500 Rupees", 0x1F);
        items.Add("Secret Seashell", 0x20);
        items.Add("Gold Leaf", 0x15);
        items.Add("Message (Catfish's Maw)", 0x21);
        items.Add("Green slime enemy", 0x22);
    }
    
    private void AddDungeonItems()
    {
        items.Add("Map", 0x16);
        items.Add("Compass", 0x17);
        items.Add("Stone Beak", 0x18);
        items.Add("Nightmare Key", 0x19);
        items.Add("Small Key", 0x1A);
    }
    
    private void AddDungeonKeys()
    {
        items.Add("Tail Key", 0x11);
        items.Add("Angler Key (weird)", 0x12);
        items.Add("Face Key (weird)", 0x13);
        items.Add("Bird Key (weird)", 0x14);
    }

    public ChestEditor(byte chest, int mapType)
    {
        InitializeComponent();
        AddGenericItems();
        
        // Add Dungeon Items
        if (mapType == 1)
        {
            AddDungeonItems();
        }
        
        // Add Dungeon Keys
        if (mapType != 1)
        {
            AddDungeonKeys();
        }
        
        cbChestItems.DataSource = new BindingSource(items, null);
        cbChestItems.DisplayMember = "Key";
        cbChestItems.ValueMember = "Value";
        chestData = chest;
        cbChestItems.SelectedValue = chest;
    }

    private void bCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void bAccept_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private void nItem_ValueChanged(object sender, EventArgs e)
    {
        var value = ((KeyValuePair<string, byte>)cbChestItems.SelectedItem).Value;
        chestData = value;
    }
}