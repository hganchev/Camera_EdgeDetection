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
using AForge.Imaging.Filters;
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
        // Button Camera Capture
        private void button1_Click(object sender, EventArgs e)
        {
            webcam.Stop();
            pictureBox2.Image = pictureBox1.Image;
        }
        //Process Image
        private void button2_Click(object sender, EventArgs e)
        {
            webcam.Stop();
            Bitmap backgroundBitmap = (Bitmap)pictureBox1.Image;
            Bitmap orig = (Bitmap)pictureBox2.Image;
            
            // Detect New Object
            for (int x = 0; x < orig.Width; x++)
            {
                for (int y = 0; y < orig.Height; y++)
                {
                    Color firstPixel = orig.GetPixel(x, y);
                    Color SecondPixel = backgroundBitmap.GetPixel(x, y);              
                    if (Properties.Settings.Default.Threshold == "")
                        {
                        MessageBox.Show("Value not set! Go to Options");
                        return;
                    }
                    int Totalfirst = (int)((firstPixel.R * .3) + (firstPixel.G * .59) + (firstPixel.B * .11));
                    int Totalsecond = (int)((SecondPixel.R * .3) + (SecondPixel.G * .59) + (SecondPixel.B * .11));
                    if (Totalfirst < Totalsecond + Convert.ToInt32(Properties.Settings.Default.Threshold))
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
        // Button Start Camera
        private void button3_Click(object sender, EventArgs e)
        {
            webcam.Start(0);
        }
        // Detection 
        private void button4_Click(object sender, EventArgs e)
        {
            Bitmap BackgroudImage = (Bitmap)pictureBox1.Image;
            Bitmap orig = (Bitmap)pictureBox2.Image;
            Bitmap clone = new Bitmap(orig.Width, orig.Height, PixelFormat.Format24bppRgb);
            if (Properties.Settings.Default.BlobHeight == "" || Properties.Settings.Default.BlobWidth == "")
            {
                MessageBox.Show("Value not set! Go to Options");
                return;
            }
            // Draw Image
            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }

            // lock image
            BitmapData bitmapData = clone.LockBits(
                new Rectangle(0, 0, clone.Width, clone.Height),
                ImageLockMode.ReadWrite, clone.PixelFormat);

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
            blobCounter.MinHeight = Convert.ToInt32(Properties.Settings.Default.BlobHeight);
            blobCounter.MinWidth = Convert.ToInt32(Properties.Settings.Default.BlobWidth);

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            clone.UnlockBits(bitmapData);
            
            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            Graphics g = Graphics.FromImage(BackgroudImage);
            Pen redPen = new Pen(Color.Red, 2);       // quadrilateral
            Pen greenPen = new Pen(Color.Green, 2);   // known triangle

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
                List<IntPoint> corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
               
                    g.DrawPolygon(greenPen, ToPointsArray(corners));
                    System.Drawing.Point centroid1 = Compute2DPolygonCentroid(corners);
                    textBox1.Text = Convert.ToString(centroid1);

                    g.DrawEllipse(redPen, (int)(centroid1.X),
                                            (int)(centroid1.Y),
                                            (int)(5 * 2),
                                            (int)(5 * 2));          
                
            }
            redPen.Dispose();
            greenPen.Dispose();
            g.Dispose();

            // put new image to clipboard
            Clipboard.SetDataObject(BackgroudImage);

            // and to picture box
            pictureBox2.Image = BackgroudImage;
        }

        // Convert to PointArray
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0, n = points.Count; i < n; i++)
            {
                array[i] = new System.Drawing.Point(points[i].X, points[i].Y);
            }

            return array;
        }
        // Compute Center point
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

        // Camera Capture
        void webcam_ImageCaptured(object source, WebcamEventArgs e)
        {
            pictureBox1.Image = e.WebCamImage;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Form Form2 = new Form2();
            Form2.Show();
        }
    }
}

