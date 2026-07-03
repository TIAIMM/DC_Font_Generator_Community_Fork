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
        private readonly IList<FontLinkCandidate> candidates;
        public int SelectIndex = -1;
        public bool Enable = true;
        public FontListSelect(IList<FontLinkCandidate> candidates, LanguageData lang)
        {
            InitializeComponent();
            this.candidates = candidates;

            this.Text = lang.GetString("Select Link Font");

            foreach (FontLinkCandidate candidate in candidates)
            {
                listBox1.Items.Add(candidate.DisplayName);
            }
            if (candidates.Count < 1) Enable = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SelectIndex = FontLinkService.ResolveSelectedIndex(candidates, listBox1.SelectedIndex);
            if (SelectIndex < 0)
            {
                return;
            }
            this.Close();
        }
    }
}
