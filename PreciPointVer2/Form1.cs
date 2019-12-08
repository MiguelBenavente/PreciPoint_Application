using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;


namespace PreciPointVer2
{
    public partial class Form1 : Form
    {
        string folderName;  //Path were the stack will be stored
        double[] sharps;    //Sharpness values for the whole stack
        double maxSharp;    //Maximum sharpness value
        int sharpestCoef;   //Image position in the stack with the largest sharpness value

        public Form1()
        {
            InitializeComponent();
            
            OpenFileDialog Openfile = new OpenFileDialog();
            if (Openfile.ShowDialog() == DialogResult.OK)
            {
                //Initiliasing variables and getting paths
                Image<Bgr, Byte> My_Image;
                Random rnd = new Random();
                int num = rnd.Next(0, 20); //Image in the stack that won't be blurred
                //num = 0;
                double nPixels, sharp;
                sharpestCoef = 0;
                maxSharp = 0;
                sharps = new double[20];
                var folderBrowserDialog1 = new FolderBrowserDialog();
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    folderName = folderBrowserDialog1.SelectedPath;
                
                } else
                {
                    folderName = "C:\\";
                }

                //The following loop generates the stack:
                //1 is left as is (determine randomly by num)
                //19 are blurred  using a gaussian mask with different sigma  values
                for (int k = 0; k < 20; k++) 
                {
                    My_Image = new Image<Bgr, Byte>(Openfile.FileName);
                    if (k != num){
                        My_Image = My_Image.SmoothGaussian(11, 11, 0.4 * (k + 1), 0.4 * (k + 1));
                    }
                    if (System.IO.File.Exists(folderName + "\\" + k.ToString() + ".jpg")) //Overwrite if image exists
                        System.IO.File.Delete(folderName + "\\" + k.ToString() + ".jpg");
                    My_Image.ToBitmap().Save(folderName + "\\" + k.ToString() + ".jpg");
                }

                //The following loop measures the sharpness of each image in the stack
                //The one with the largest Sharpness value is the image most in-focus
                for (int k = 0; k < 20; k++)
                {
                    double minval = 0, maxval = 0;
                    Point minloc = new Point();
                    Point maxloc = new Point();
                    My_Image = new Image<Bgr, Byte>(folderName + "\\" + k.ToString() + ".jpg");
                    Image<Gray, float> conImg = My_Image.Convert<Gray, float>();
                    //CvInvoke.Imshow("Image", My_Image);
                    Image<Gray, float> FFT3DImage = Mag_FFT2D(conImg); //Finding magnitude image of the discrete Fourier Transform
                    //imDisplay("Magnitude", FFT3DImage);
                    CvInvoke.MinMaxLoc(FFT3DImage, ref minval, ref maxval, ref minloc, ref maxloc); //Finding maximum value in the magnitude image
                    double threshold = maxval / 1000; //Threshold value: pixels in magnitude image with values above it count towards sharpness

                    Matrix<float> auxDft = new Matrix<float>(FFT3DImage.Width, FFT3DImage.Height);

                    //Following loop finds the amount of pixels with values above the threshold
                    FFT3DImage.GetSubRect(new Rectangle(Point.Empty, FFT3DImage.Size)).CopyTo(auxDft);
                    nPixels = 0; //Number of pixels which meet the above requirement
                    for (int i = 0; i < auxDft.Width; i++)
                    {
                        for (int j = 0; j < auxDft.Height; j++)
                        {
                            if (auxDft[j, i] > threshold)
                                nPixels += 1;
                        }
                    }

                    //Finding sharpness value
                    sharp = nPixels / (auxDft.Width * auxDft.Height);

                    //Update maximum sharpness value if the current one is the largest yet
                    if (sharp > maxSharp)
                    {
                        maxSharp = sharp;
                        sharpestCoef = k;
                    }
                    //Saving the sharpness value for later display
                    sharps[k] = sharp;
                }

                //Displaying image with largest Sharpness value
                pictureBox1.Image = new Image<Bgr, Byte>(folderName + "\\" + sharpestCoef.ToString() + ".jpg").ToBitmap();
                SharpnessValueLabel.Text = maxSharp.ToString();
                SharpestStackLabel.Text = "Yes";
                trackBar1.Value = sharpestCoef;
            }
        }

        private Image<Gray, float> Mag_FFT2D(Image<Gray, float> SourceGrayDoubleImage)
        {
            //This function returnss the Magnitude image of the discrete Fourier Transform of the input image
            Image<Gray, float> FFTImage = new Image<Gray, float>(SourceGrayDoubleImage.Width, SourceGrayDoubleImage.Height);
            Matrix<float> dft = new Matrix<float>(SourceGrayDoubleImage.Rows, SourceGrayDoubleImage.Cols, 1);
            CvInvoke.Dft(SourceGrayDoubleImage, dft, Emgu.CV.CvEnum.DxtType.Forward, 0);
            Matrix<float> shiftDft = new Matrix<float>(dft.Size);
            cvShiftDFT(dft, shiftDft);
            shiftDft.GetSubRect(new Rectangle(Point.Empty, dft.Size)).CopyTo(FFTImage);

            Image<Gray, float> realDft = FFTImage.Rotate(180, new Gray(255), false);
            Image<Gray, float> q1 = realDft.GetSubRect(new Rectangle(0, 0, FFTImage.Width / 2, FFTImage.Height / 2));
            Image<Gray, float> q2 = realDft.GetSubRect(new Rectangle(0, FFTImage.Height / 2, (FFTImage.Width / 2), FFTImage.Height / 2));
            Image<Gray, float> d1 = FFTImage.GetSubRect(new Rectangle(0, 0, FFTImage.Width / 2, FFTImage.Height / 2));
            Image<Gray, float> d2 = FFTImage.GetSubRect(new Rectangle(0, FFTImage.Height / 2, (FFTImage.Width / 2), FFTImage.Height / 2));

            q1.GetSubRect(new Rectangle(Point.Empty, q1.Size)).CopyTo(d1);
            q2.GetSubRect(new Rectangle(Point.Empty, q2.Size)).CopyTo(d2);

            Image<Gray, float> imDft = FFTImage.Rotate(180, new Gray(255), false);
            Image<Gray, float> q3 = imDft.GetSubRect(new Rectangle(0, 0, FFTImage.Width / 2, FFTImage.Height / 2));
            Image<Gray, float> q4 = imDft.GetSubRect(new Rectangle(0, FFTImage.Height / 2, (FFTImage.Width / 2), FFTImage.Height / 2));
            Image<Gray, float> d3 = FFTImage.GetSubRect(new Rectangle(0, 0, FFTImage.Width / 2, FFTImage.Height / 2));
            Image<Gray, float> d4 = FFTImage.GetSubRect(new Rectangle(0, FFTImage.Height / 2, (FFTImage.Width / 2), FFTImage.Height / 2));

            q3.GetSubRect(new Rectangle(Point.Empty, q3.Size)).CopyTo(d3);
            q4.GetSubRect(new Rectangle(Point.Empty, q4.Size)).CopyTo(d4);

            //imDisplay("Real", realDft);
            //imDisplay("Img", imDft);

            CvInvoke.Pow(realDft, 2, realDft);
            CvInvoke.Pow(imDft, 2, imDft);
            CvInvoke.Add(realDft, imDft, FFTImage);
            CvInvoke.Pow(FFTImage, 0.5, FFTImage);

            return FFTImage;
        }

        void imDisplay(string name, Image<Gray, float> iImg)
        {
            //Adapts window/level for better visualization
            //Written for Fourier transforms and debugging (NOT USED IN FINAL CODE)
            double minval = 0, maxval = 0;
            Point minloc = new Point();
            Point maxloc = new Point();
            iImg += 1;
            CvInvoke.Log(iImg, iImg);
            CvInvoke.MinMaxLoc(iImg, ref minval, ref maxval, ref minloc, ref maxloc);
            CvInvoke.cvConvertScale(iImg, iImg, 1.0 / (maxval - minval), 1.0 * (-minval) / (maxval - minval));
            CvInvoke.Imshow(name, iImg);
        }

        int cvShiftDFT(Matrix<float> src_arr, Matrix<float> dst_arr)
        {   
            //Puts the origin of the input Fourier image in the center (lower frequencies in the center)
            Matrix<float> q1 = new Matrix<float>(dst_arr.Width / 2, dst_arr.Height / 2);
            Matrix<float> q2 = new Matrix<float>(dst_arr.Width / 2, dst_arr.Height / 2);
            Matrix<float> q3 = new Matrix<float>(dst_arr.Width, dst_arr.Height);
            Matrix<float> q4 = new Matrix<float>(dst_arr.Width, dst_arr.Height);
            Matrix<float> d1 = new Matrix<float>(dst_arr.Width, dst_arr.Height);
            Matrix<float> d2 = new Matrix<float>(dst_arr.Width, dst_arr.Height);
            Matrix<float> d3 = new Matrix<float>(dst_arr.Width, dst_arr.Height);
            Matrix<float> d4 = new Matrix<float>(dst_arr.Width, dst_arr.Height);
            Matrix<float> tmp = new Matrix<float>(src_arr.Size); ;

            int cx, cy;

            if (dst_arr.Width != src_arr.Width ||
               dst_arr.Height != src_arr.Height)
            {
                return 0;
            }

            cx = dst_arr.Width / 2;
            cy = dst_arr.Height / 2; // image center
            q1 = src_arr.GetSubRect(new Rectangle(0, 0, cx, cy));
            q2 = src_arr.GetSubRect(new Rectangle(cx, 0, cx, cy));
            q3 = src_arr.GetSubRect(new Rectangle(cx, cy, cx, cy));
            q4 = src_arr.GetSubRect(new Rectangle(0, cy, cx, cy));
            d1 = dst_arr.GetSubRect(new Rectangle(0, 0, cx, cy));
            d2 = dst_arr.GetSubRect(new Rectangle(cx, 0, cx, cy));
            d3 = dst_arr.GetSubRect(new Rectangle(cx, cy, cx, cy));
            d4 = dst_arr.GetSubRect(new Rectangle(0, cy, cx, cy));

            q3.GetSubRect(new Rectangle(Point.Empty, q3.Size)).CopyTo(d1);
            q4.GetSubRect(new Rectangle(Point.Empty, q3.Size)).CopyTo(d2);
            q1.GetSubRect(new Rectangle(Point.Empty, q3.Size)).CopyTo(d3);
            q2.GetSubRect(new Rectangle(Point.Empty, q3.Size)).CopyTo(d4);

            return 1;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Used to update display
            pictureBox1.Image = new Image<Bgr, Byte>(folderName + "\\" + trackBar1.Value.ToString() + ".jpg").ToBitmap();
            SharpnessValueLabel.Text = sharps[trackBar1.Value].ToString();
            if (trackBar1.Value == sharpestCoef)
            {
                SharpestStackLabel.Text = "Yes";
            } else
            {
                SharpestStackLabel.Text = "No";
            }
        }

    }
}
