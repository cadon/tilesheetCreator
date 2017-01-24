using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tileCreator
{
    public partial class Form1 : Form
    {
        private string path, saveFile;
        private string css, cssPre1, cssPre2;
        private string[] files;
        private Bitmap tileSheet;
        private int width, height;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                setPath(fbd.SelectedPath);
            }
        }

        private void setPath(string newPath)
        {
            path = newPath;
            files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);
            label1.Text = "Selected Path: " + path + ", " + files.Length + " files.";
            progressBar1.Value = 0;
            numericUpDown1.Value = (decimal)Math.Ceiling(Math.Sqrt(files.Length));

            // get size from first image
            System.Drawing.Image img = System.Drawing.Image.FromFile(files[0]);
            width = img.Width;
            height = img.Height;
            img.Dispose();
            labelSize.Text = "Size of source-icons:\n" + width + " × " + height;
            numericUpDownSize.Value = width;
            saveFile = "tilesheet.png";

            buttonSaveTilesheet.Enabled = false;
            buttonCSS2Clipboard.Enabled = false;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            int rows = (int)Math.Ceiling(files.Length / numericUpDown1.Value);
            labelProduct.Text = "× " + rows + " = " + (numericUpDown1.Value * rows);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            files = Directory.GetFiles(path, "*.png", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                int cols = (int)numericUpDown1.Value;
                int rows = (int)Math.Ceiling(files.Length / numericUpDown1.Value);

                int nWidth = (int)numericUpDownSize.Value;
                int nHeight = (int)Math.Round(numericUpDownSize.Value * height / width);

                if ((nWidth * cols > 20000 || nHeight * rows > 20000) && MessageBox.Show("The resulting image will be very large (" + (nWidth * cols) + " × " + (nHeight * rows) + " px, proceed?", "Large image", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { return; }

                progressBar1.Maximum = files.Length;
                progressBar1.Value = 0;
                Cursor = Cursors.WaitCursor;

                string cssPrefix = textBox1.Text;

                Regex r = new Regex(@"\-+$");
                string cssBaseClass = r.Replace(cssPrefix, "");
                cssPre1 = "/* tilesheet */\n." + ((cssBaseClass.Length == 0) ? "icon" : cssBaseClass) + " {\n    background-image: url('images/";
                cssPre2 = "');\n    width: " + nWidth + "px;\n    height: " + nHeight + "px;\n}";
                css = "";

                tileSheet = new Bitmap(nWidth * cols, nHeight * rows);

                Graphics g = Graphics.FromImage(tileSheet);
                if (nWidth != width)
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                }

                int i = 0;
                r = new Regex(@"[^a-z0-9]+");

                foreach (string file in files)
                {
                    int x = (i % cols) * nWidth;
                    int y = (i / cols) * nHeight;

                    Image icon;
                    // avoiding to lock the loaded icon-file
                    using (var bmpTemp = new Bitmap(file))
                    {
                        icon = new Bitmap(bmpTemp);
                    }
                    g.DrawImage(icon, x, y, nWidth, nHeight);
                    string className = r.Replace(Path.GetFileNameWithoutExtension(file).ToLower(), "-");
                    css += "\n." + cssPrefix + className + " { background-position: " + (-x) + (x == 0 ? "" : "px") + " " + (-y) + (y == 0 ? "" : "px") + "; }";
                    i++;
                    progressBar1.Value = i;
                }
                buttonSaveTilesheet.Enabled = true;
                buttonCSS2Clipboard.Enabled = true;
                Cursor = Cursors.Default;
            }

            else { MessageBox.Show("No files in the selected folder", "No Files", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (tileSheet != null && tileSheet.Width > 0)
            {
                SaveFileDialog fd = new SaveFileDialog();
                fd.Filter = "*.png|*.png|*.jpg|*.jpg";
                fd.Title = "Save the Tilesheet";
                fd.ShowDialog();

                // If the file name is not an empty string open it for saving.
                if (fd.FileName != "")
                {
                    saveFile = Path.GetFileName(fd.FileName);
                    System.IO.FileStream fs = (System.IO.FileStream)fd.OpenFile();
                    switch (fd.FilterIndex)
                    {
                        case 1:
                            tileSheet.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                            break;

                        case 2:
                            tileSheet.Save(fs, System.Drawing.Imaging.ImageFormat.Jpeg);
                            break;
                    }

                    fs.Close();
                }
            }
            else { MessageBox.Show("No Tilesheet created, please choose a folder with files you want to merge into one tilesheed first", "No Tilesheet", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(cssPre1 + saveFile + cssPre2 + css);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://github.com/"); // TODO
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            DragDropEffects effect = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (Directory.Exists(path))
                    effect = DragDropEffects.Copy;
            }
            e.Effect = effect;
        }

        private void numericUpDownSize_ValueChanged(object sender, EventArgs e)
        {
            labelnewSize.Text = "New size of icons:\n" + numericUpDownSize.Value + " × " + Math.Round(numericUpDownSize.Value * height / width);
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            setPath(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
        }

    }
}
