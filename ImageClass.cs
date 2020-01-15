using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace DHGComp1
{
    public static class ImageClass
    {


        public static unsafe Bitmap Grayscale(Bitmap image)
        {


            BitmapData imgData = image.LockBits(
                                         new Rectangle(0, 0, image.Width, image.Height),
                                                            ImageLockMode.ReadWrite,
                                                            PixelFormat.Format24bppRgb);
            IntPtr _data = imgData.Scan0;   //goto first pixel
            byte* p0 = (byte*)_data;
            byte* p;
            int offset = imgData.Stride - (image.Width * 3);
            int h = image.Height;
            int w = image.Width;
            byte r, g, b, gray;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    p = p0 + 3 * (w * y + x);
                    r = p[2];
                    g = p[1];
                    b = p[0];
                    gray = (byte)(.299 * r + .587 * g + .114 * b);
                    p[2] = gray;
                    p[1] = gray;
                    p[0] = gray;
                }
            }
            image.UnlockBits(imgData);
            return image;
        }

        public static unsafe Bitmap Threshold(Bitmap image, int th)
        {
            BitmapData imgData = image.LockBits(
                                         new Rectangle(0, 0, image.Width, image.Height),
                                                            ImageLockMode.ReadWrite,
                                                            PixelFormat.Format24bppRgb);
            IntPtr _data = imgData.Scan0;   //goto first pixel
            byte* p0 = (byte*)_data;
            byte* p;
            int offset = imgData.Stride - (image.Width * 3);
            int h = image.Height;
            int w = image.Width;
            byte r, g, b;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    p = p0 + 3 * (w * y + x);
                    r = p[2];
                    g = p[1];
                    b = p[0];

                    if (r >= th)
                    {
                        p[2] = 255;
                        p[1] = 255;
                        p[0] = 255;
                    }
                    else
                    {
                        p[2] = 0;
                        p[1] = 0;
                        p[0] = 0;
                    }

                }
            }
            image.UnlockBits(imgData);
            return image;
        }

        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                     new float[] {.3f, .3f, .3f, 0, 0},
                     new float[] {.59f, .59f, .59f, 0, 0},
                     new float[] {.11f, .11f, .11f, 0, 0},
                     new float[] {0, 0, 0, 1, 0},
                     new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }
    }
}
