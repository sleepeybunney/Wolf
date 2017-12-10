﻿using System;
using System.IO;
using System.Windows.Forms;
using Celtic_Guardian;

namespace Abaki
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void LoadToolStripMenuItem_Click(object Sender, EventArgs Args)
        {
            using (var Ofd = new OpenFileDialog())
            {
                Ofd.Title = "Select File To Decode";
                Ofd.Filter = "Language file (*.bnd) | *.bnd";
                if (Ofd.ShowDialog() != DialogResult.OK) return;

                if (!Directory.Exists(new FileInfo(Ofd.FileName).Name))
                    Directory.CreateDirectory(new FileInfo(Ofd.FileName).Name);

                var LangFileName = new FileInfo(Ofd.FileName).Name;
                using (var Reader = new BinaryReader(File.Open(Ofd.FileName, FileMode.Open, FileAccess.Read)))
                {
                    const long DataStartOffset = 0x11CD;
                    const long AmountOfStrings = 472;
                }
            }
        }

        private void ExportToolStripMenuItem_Click(object Sender, EventArgs Args)
        {
        }
    }
}