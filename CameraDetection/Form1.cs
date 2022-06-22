using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using WebCam_Capture;
using System.Runtime.InteropServices;
using AForge;
using AForge.Imaging;
using AForge.Math.Geometry;

namespace CameraDetection
{
    
    public partial class Form1 : Form
    {
        private WebCamCapture webcam;
        private int FrameNumber =30;

        public Form1()
        {
            InitializeComponent();

            webcam = new WebCamCapture();
            webcam.FrameNumber = ((ulong)(0ul));
            webcam.ImageCaptured += new WebCamCapture.WebCamEventHandler(webcam_ImageCaptured);
            webcam.TimeToCapture_milliseconds = FrameNumber;
            webcam.Start(0);
            pictureBox1.Height = webcam.Height;
            pictureBox1.Width = webcam.Width;
            pictureBox2.Height = webcam.Height;
            pictureBox2.Width = webcam.Width;
            
        }
        void webcam_ImageCaptured(object source, WebcamEventArgs e)
        {
            
            pictureBox1.Image = e.WebCamImage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            webcam.Stop();
            pictureBox2.Image = pictureBox1.Image;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            webcam.Stop();
            Bitmap backgroundBitmap = (Bitmap)pictureBox1.Image;
            Bitmap orig = (Bitmap)pictureBox2.Image;
            

            for (int x = 0; x < orig.Width; x++)
            {
                for (int y = 0; y < orig.Height; y++)
                {
                    Color firstPixel = orig.GetPixel(x, y);
                    Color SecondPixel = backgroundBitmap.GetPixel(x, y);
                    int Treshhold = 0;

                    if (textBox2.Text == "")
                    {
                        Treshhold = 0;
                    }
                    else
                    {
                        Treshhold = Convert.ToInt32(textBox2.Text);
                    }                  
                    

                    int Totalfirst = (int)((firstPixel.R * .3) + (firstPixel.G * .59) + (firstPixel.B * .11));
                    int Totalsecond = (int)((SecondPixel.R * .3) + (SecondPixel.G * .59) + (SecondPixel.B * .11));
                    if (Totalfirst < Totalsecond + Treshhold)
                    {
                       Color WhiteColor = Color.FromArgb(0, 0, 0);
                        orig.SetPixel(x, y, WhiteColor);                     
                    }
                    else  
                    {
                        //Color SameColor = Color.FromArgb(SecondPixel.R, SecondPixel.G, SecondPixel.B);
                        Color SameColor = Color.FromArgb(255, 255, 255);
                        orig.SetPixel(x, y, SameColor);
                    }                                                       
                }
            }

            pictureBox2.Image = orig;                  
        }

        private void button3_Click(object sender, EventArgs e)
        {
            webcam.Start(0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Bitmap orig = (Bitmap)pictureBox2.Image;
            Bitmap clone = new Bitmap(orig.Width, orig.Height, PixelFormat.Format24bppRgb);
            

            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }

            Bitmap bitmap;
            if (clone.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                bitmap = clone;
            }
            else
            {
                Bitmap d = new Bitmap(clone.Width, clone.Height);
                for (int i = 0; i < clone.Width; i++)
                {
                    for (int x = 0; x < clone.Height; x++)
                    {
                        Color oc = clone.GetPixel(i, x);
                        int grayScale = (int)((oc.R * 0.3) + (oc.G * 0.59) + (oc.B * 0.11));
                        Color nc = Color.FromArgb(oc.A, grayScale, grayScale, grayScale);
                        d.SetPixel(i, x, nc);
                    }
                }
                bitmap = d;
            }
            // lock image
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);

            /*// step 1 - turn background to black
            ColorFiltering colorFilter = new ColorFiltering();

            colorFilter.Red = new IntRange(0, 64);
            colorFilter.Green = new IntRange(0, 64);
            colorFilter.Blue = new IntRange(0, 64);
            colorFilter.FillOutsideRange = false;

            colorFilter.ApplyInPlace(bitmapData);*/

            // step 2 - locating objects
            BlobCounter blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 5;
            blobCounter.MinWidth = 5;

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            bitmap.UnlockBits(bitmapData);
            
            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            Graphics g = Graphics.FromImage(bitmap);
            Pen yellowPen = new Pen(Color.Yellow, 2); // circles
            Pen redPen = new Pen(Color.Red, 2);       // quadrilateral
            Pen brownPen = new Pen(Color.Brown, 2);   // quadrilateral with known sub-type
            Pen greenPen = new Pen(Color.Green, 2);   // known triangle
            Pen bluePen = new Pen(Color.Blue, 2);     // triangle

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                List<IntPoint> corners;
               
                // is triangle or quadrilateral
                if (shapeChecker.IsConvexPolygon(edgePoints, out corners))
                    {
                        // get sub-type
                        PolygonSubType subType = shapeChecker.CheckPolygonSubType(corners);

                    /*Pen pen;

                    if (subType == PolygonSubType.Unknown)
                    {
                        pen = (corners.Count == 4) ? redPen : bluePen;
                    }
                    else
                    {
                        pen = (corners.Count == 4) ? brownPen : greenPen;
                    }*/

                    g.DrawPolygon(greenPen, ToPointsArray(corners));
                        System.Drawing.Point centroid1 = Compute2DPolygonCentroid(corners);
                        textBox1.Text = Convert.ToString(centroid1);

                    g.DrawEllipse(redPen, (int)(centroid1.X),
                                            (int)(centroid1.Y),
                                            (int)(5 * 2),
                                            (int)(5 * 2));
                }
                
            }

            yellowPen.Dispose();
            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            brownPen.Dispose();
            g.Dispose();

            // put new image to clipboard
            Clipboard.SetDataObject(bitmap);

            // and to picture box
            pictureBox2.Image = bitmap;
        }

        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
            {
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);
            }

            return array;
        }

        static System.Drawing.Point Compute2DPolygonCentroid(List<IntPoint> vertices)
        {
            System.Drawing.Point centroid = new System.Drawing.Point() { X = 0, Y = 0 };
            double signedArea = 0.0;
            double x0 = 0.0; // Current vertex X
            double y0 = 0.0; // Current vertex Y
            double x1 = 0.0; // Next vertex X
            double y1 = 0.0; // Next vertex Y
            double a = 0.0;  // Partial signed area


            // For all vertices except last
            int i = 0;
            for (i = 0; i < vertices.Count - 1; ++i)
            {
                x0 = vertices[i].X;
                y0 = vertices[i].Y;
                x1 = vertices[i + 1].X;
                y1 = vertices[i + 1].Y;
                a = x0 * y1 - x1 * y0;
                signedArea += a;
                centroid.X += (int)((x0 + x1) * a);
                centroid.Y += (int)((y0 + y1) * a);
            }

            // Do last vertex
            x0 = vertices[i].X;
            y0 = vertices[i].Y;
            x1 = vertices[0].X;
            y1 = vertices[0].Y;
            a = x0 * y1 - x1 * y0;
            signedArea += a;
            centroid.X += (int)((x0 + x1) * a);
            centroid.Y += (int)((y0 + y1) * a);

            signedArea *= 0.5;
            centroid.X /= (int)(6 * signedArea);
            centroid.Y /= (int)(6 * signedArea);

            return centroid;
        }
    }
}

