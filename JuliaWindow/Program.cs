using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using Amplifier.OpenCL;
using Amplifier;

namespace JuliaWindow
{
    class Program
    {
        static WriteableBitmap bitmap;
        static Window windows;
        static Image image;
        static OpenCLCompiler compiler;
        [STAThread]
        static void Main(string[] args)
        {

            
            image = new Image();
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(image, EdgeMode.Aliased);

            windows = new Window();
            windows.Content = image;
            windows.Show();

            bitmap = new WriteableBitmap(
                (int)windows.ActualWidth,
                (int)windows.ActualHeight,
                96,
                96,
                PixelFormats.Bgr32,
                null);

            image.Source = bitmap;

            image.Stretch = Stretch.None;
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;

            image.MouseLeftButtonDown +=
                new MouseButtonEventHandler(image_MouseLeftButtonDown);
            image.MouseMove +=
                new MouseEventHandler(image_MouseMove);
            windows.MouseWheel += new MouseWheelEventHandler(window_MouseWheel);

            compiler = new OpenCLCompiler();

            compiler.UseDevice(0);
            compiler.CompileKernel(typeof(OpenCLJulian));


            UpdateJulia();

            Application app = new Application();
            app.Run();
            
        }

        static void window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int column = (int)e.GetPosition(image).X;
            int row = (int)e.GetPosition(image).Y;

            if (e.Delta > 0)
            {
                juliaDepth =  (int)(juliaDepth * 1.1);
            }
            else
            {
                juliaDepth = (int)(juliaDepth / 1.1);
            }

            windows.Title = $"Julia depth:{juliaDepth}";

            UpdateJulia();
        }

        static void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int column = (int)e.GetPosition(image).X;
            int row = (int)e.GetPosition(image).Y;

            juliaCenterX = column * (2.0 / bitmap.PixelWidth) - 1.0;
            juliaCenterY = row * (2.0 / bitmap.PixelHeight) - 1.0;
           
            UpdateJulia();
        }

        static void image_MouseMove(object sender, MouseEventArgs e)
        {
            int column = (int)e.GetPosition(image).X;
            int row = (int)e.GetPosition(image).Y;

            double mouseCenterX = column * (2.0 / bitmap.PixelWidth) - 1.0;
            double mouseCenterY = row * (2.0 / bitmap.PixelHeight) -1.0;

            windows.Title = $"Julia constant X:{mouseCenterX} Y:{mouseCenterY}";

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                juliaCenterX = mouseCenterX;
                juliaCenterY = mouseCenterY;

                UpdateJulia();
            }
        }

        static double juliaCenterX = 0.0;
        static double juliaCenterY = 0.0;

        static int juliaDepth = 360;

        public static void UpdateJulia()
        {
            try
            {
                // Reserve the back buffer for updates.
                bitmap.Lock();
                int[] arr = new int[bitmap.PixelHeight * bitmap.PixelWidth];

                unsafe
                {
                    var exec = compiler.GetExec();
                    exec.Julia(arr, bitmap.PixelHeight, bitmap.PixelWidth, bitmap.BackBufferStride, juliaCenterX,juliaCenterY,juliaDepth);
                }

                image.Source = BitmapSource.Create((int)bitmap.PixelWidth, (int)bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY, PixelFormats.Bgr32, null, arr, bitmap.PixelWidth * 4);

                // Specify the area of the bitmap that changed.
            }
            finally
            {
                // Release the back buffer and make it available for display.
                bitmap.Unlock();
            }}

        public static int IterCount(double zx, double zy, double cx, double cy)
        {
            int result = 0;
            while ((zx * zx + zy * zy) <= 4.0 && result < juliaDepth)
            {
                double xtemp = zx * zx - zy * zy;
                zy = 2 * zx * zy + cy;
                zx = xtemp + cx;
                result++;
            }
            return result;
        }

        static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        static int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}

