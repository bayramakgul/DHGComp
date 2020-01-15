using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DHGComp1
{
    public partial class ImagePanel : UserControl
    {
        public ImagePanel()
        {
            InitializeComponent();

            DoubleBuffered = true;
        }

        //public int W { get; set; }

        //public int H { get; set; }

        public Bitmap Bitmap { get; set; }

        int num = 0;
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Bitmap == null) return;
            num = 0;
            int W = Bitmap.Width;
            int H = Bitmap.Height;

            if(W>64 || H>64)
            {
                e.Graphics.DrawString("Image size bigger than 64x64", new Font(this.Font.FontFamily, 32, FontStyle.Bold),
                    Brushes.Red, e.Graphics.ClipBounds, new StringFormat() {  Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center});
                return;
            }

            int x = Width / W;
            int y = Height / H;

            for (int j = 0; j < H; j++)
            {
                for (int i = 0; i < W; i++)
                {
                    Color c = Bitmap.GetPixel(i, j);
                    e.Graphics.FillRectangle(new SolidBrush(c), i * x, j * y, x, y);

                    if (c.R == 0)
                    {
                        e.Graphics.DrawString(i + "," + j, Font, Brushes.White, i * x + 1, j * y + 1);
                        e.Graphics.DrawString(num.ToString(), Font, Brushes.Yellow, i * x + 5, j * y + 15);

                        num++;
                    }
                }
            }

            for (int i = 0; i <= W; i++)
            {
                e.Graphics.DrawLine(Pens.Red, x * i, 0, x * i, Height);
            }
            for (int i = 0; i <= H; i++)
            {
                e.Graphics.DrawLine(Pens.Red, 0, y * i, Width, y * i);
            }


        }

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ImagePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "ImagePanel";
            this.Size = new System.Drawing.Size(173, 149);
            this.ResumeLayout(false);

        }

        #endregion

    }

}
