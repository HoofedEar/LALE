using System;
using System.Windows.Forms;

namespace LALE
{
    public partial class ChestEditor : Form
    {
        public byte chestData;

        public ChestEditor(byte chest)
        {
            InitializeComponent();
            chestData = chest;
            nItem.Value = chest;
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
            chestData = (byte)nItem.Value;
        }
    }
}
