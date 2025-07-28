using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Fallout3_Font_Generator
{
    public partial class Fallout3ini_selector : Form
    {
        public int SelectIndex = -1;
        public Fallout3ini_selector(List<string> list, string title,int DefaultIndex)
        {
            InitializeComponent();
            this.Text = title;
            foreach (string s in list)
            {
                listBox1.Items.Add(s);
            }
            if (DefaultIndex >= 0)
            {
                listBox1.SelectedIndex = DefaultIndex;
                SelectIndex = DefaultIndex;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            SelectIndex = listBox1.SelectedIndex;
            this.Close();
        }

    }
}
