using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DC_Font_Generator
{
    public partial class FontListSelect : Form
    {
        private List<int> index = new List<int>();
        public int SelectIndex = -1;
        private List<Main> ml;
        private int NowID = -1;
        public bool Enable = true;
        public FontListSelect(List<Main> MainList, int NowID, LanguageData lang)
        {
            InitializeComponent();

            this.Text = lang.GetString("Select Link Font");

            this.NowID = NowID;
            ml = MainList;
            int count = 1;
            
            
            foreach (Main m in MainList)
            {
                if (m.DCfontLink == -1 && count != (NowID+1))
                {
                    if (m.name != "")
                        listBox1.Items.Add(count + ". " + m.name);
                    else
                        listBox1.Items.Add(count + ". Font" + count + " (" + m.font2.Name + "," + m.font2.Size + ")");
                    index.Add(count - 1);
                }
                count++;
            }
            if (index.Count < 1) Enable = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int select = listBox1.SelectedIndex;
            SelectIndex = index[select];
            ml[this.NowID].DCfontLink = SelectIndex;
            ml[this.NowID].LinkClone();
            this.Close();
        }
    }
}
