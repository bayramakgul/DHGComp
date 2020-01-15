#define ORIGINAL


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace DHGComp1
{
    public partial class FrmMain : Form
    {
        DirectoryInfo dir = new FileInfo(Application.ExecutablePath).Directory;
        DHGCalc part1 = new DHGCalc();
        Image imOrg;
        Image smallIm;
        Image grSca;
        Image binIm;

        public FrmMain()
        {
            InitializeComponent();

            CheckBox chk = new CheckBox() { Text = "Print Matrices", Checked = false, AutoSize = true, Width = 200, Height = 20 };
            chk.CheckedChanged += delegate { part1.bPrintMatrix = chk.Checked; };   
            ToolStripControlHost host = new ToolStripControlHost(chk) { AutoSize = false, Width = 200, Height = 20 };
            toolStrip1.Items.Add(host);
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {

            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Resim Dosyaları(*.jpg;*.jpeg;*.bmp;*.png;*.gif)|*.jpg;*.jpeg;*.bmp;*.png;*.gif";
            fd.Multiselect = false;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                imOrg = Image.FromFile(fd.FileName);
                pictureBox1.Image = imOrg;
                lblSize.Text = imOrg.Size.ToString();


                this.Text = Application.ProductName + " [ " + fd.FileName + " ]";
                FileInfo fi = new FileInfo(fd.FileName);
                string ff = fi.Name.Substring(0, fi.Name.LastIndexOf("."));
                part1.out_filename = dir.FullName + "\\"+ff+"_part.txt";
                StartProcessing();
            }
        }
        //---------------------------------------------------



        public void StartProcessing()
        {
            part1.Init();

            grSca = (Image)imOrg.Clone();
            pictureBox2.Image = ImageClass.MakeGrayscale3((Bitmap)grSca);


            binIm = (Image)grSca.Clone();
            pictureBox3.Image = ImageClass.Threshold((Bitmap)binIm, 128);

            smallIm = (Image)binIm.Clone();
            int w, h;


#if (ORIGINAL)
            smallIm = (Image)imOrg.Clone();
            w = smallIm.Width;
            h = smallIm.Height;
#else
            w = int.Parse(txtW.Text);
            h = int.Parse(txtH.Text);
            smallIm = smallIm.GetThumbnailImage(w, h, () => false, IntPtr.Zero);
#endif
            //---------------------------------------
            part1.bit = new Bitmap(w, h);
            int count = 0;

            part1.table = new int[w, h];
            part1.W = w;
            part1.H = h;

            int threshold = 0;
            {
                for (int j = 0; j < h; j++)
                {
                    for (int i = 0; i < w; i++)
                    {
                        Color val = ((Bitmap)smallIm).GetPixel(i, j);
                        int ort = (val.R + val.G + val.B) / 3;
                        threshold += ort;
                    }
                }
                threshold /= (h * w);
            }

            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    Color val = ((Bitmap)smallIm).GetPixel(i, j);
                    int ort = (val.R + val.G + val.B) / 3;

                    part1.table[i, j] = -1;

                    part1.bit.SetPixel(i, j, ort > threshold ? Color.White : Color.Black);
                    if (ort < threshold)
                    {
                        part1.table[i, j] = count;

                        part1.simp0_list.Add(new Point(i, j));
                        part1.KerDelta0++;
                        count++;
                    }
                }
            }
            //bit.SetResolution(1, 1);

            pictureBox4.Image = part1.bit;
            imagePanel1.Bitmap = part1.bit;
            imagePanel1.Invalidate();
            //---------------------------------------

        }


        /*
        private void Yak4SinirOp1_Click(object sender, EventArgs e)
        {
            // 4 yakınlık 1 simplex
            List<string> Yak4Simp1 = Simp1YakinlikBul(Yakinlik.Yakinlik4);

            output = new StreamWriter(out_filename);

            if (PrintMatrix)
            {
                output.WriteLine("***0-SIMPLEXES*******");
                int cnt = 0;
                foreach (Point p in simp0_list)
                    output.WriteLine((cnt++) + ":  x=" + p.X + ",  y=" + p.Y);

                output.WriteLine("\r\n***1-SIMPLEXES*******");
                foreach (string s in Yak4Simp1)
                    output.WriteLine(s);
            }

            SinirOp1(Yak4Simp1);

            PrintOut(simp0_list.Count, Yak4Simp1.Count, 0, 0);

            output.Flush();
            output.Close();
            System.Diagnostics.Process.Start(out_filename);

        }

        private void Yak6PSinirOp1_Click(object sender, EventArgs e)
        {

            List<string> Yak6PSimp1 = Simp1YakinlikBul(Yakinlik.Yakinlik6P);
            SinirOp1(Yak6PSimp1);

            List<string> Yak6PSimp2 = Simp2YakinlikBul(Yakinlik.Yakinlik6P);

            SinirOp2(Yak6PSimp1, Yak6PSimp2);
            PrintOut();
        }

        private void Yak6NSinirOp1_Click(object sender, EventArgs e)
        {
            List<string> Yak6NSimp1 = Simp1YakinlikBul(Yakinlik.Yakinlik6N);
            SinirOp1(Yak6NSimp1);

            List<string> Yak6NSimp2 = Simp2YakinlikBul(Yakinlik.Yakinlik6N);
            SinirOp2(Yak6NSimp1, Yak6NSimp2);
            PrintOut();
        }
        */


        private void btnEulerCharacteristic_Click(object sender, EventArgs e)
        {
            part1.StartCalc(CalcType.Euler);
        }


        private void btnBettyNumbers_Click(object sender, EventArgs e)
        {
            part1.StartCalc(CalcType.Betti);
        }

        private void btnHomologyGroups_Click(object sender, EventArgs e)
        {
            part1.StartCalc(CalcType.Homology);
        }



        private void yardımToolStripButton_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog();
        }

    }
}
