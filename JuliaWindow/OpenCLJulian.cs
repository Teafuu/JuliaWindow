using Amplifier;
using Amplifier.OpenCL;
using System;
using System.Windows.Media.Imaging;

namespace JuliaWindow
{
    class OpenCLJulian : OpenCLFunctions
    {
      

        [OpenCLKernel]
        public unsafe void Julia([Global] int[] image, int height, int width, int backBufferStride, double juliaCenterX, double juliaCenterY, int juliaDepth)
        {
            int i = get_global_id(0);

            var row = (int)(i / height);
            var column = (i % width) - 1;
            // Get a pointer to the back buffer.

            // Find the address of the pixel to draw.
            image[i] += row * backBufferStride;
            image[i] += column * 4;

            int light = IterCount((column * 2.0 / width) - 1.0, (row * 2.0 / height) - 1.0, juliaCenterX, juliaCenterY, juliaDepth);

          
            int result = HsvToRgb(light, 1.0, light < juliaDepth ? 1.0 : 0.0);

            // Compute the pixel's color.

            // Assign the color data to the pixel.
            image[i] = result;
        }

        int IterCount(double zx, double zy, double cx, double cy, int juliaDepth)
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
        unsafe int HsvToRgb(double h, double S, double V)
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
                int i = (int)floor<double>(hf);
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

            int color_data = Clamp((int)(R * 255.0)) << 16; // R
            color_data |= Clamp((int)(G * 255.0)) << 8;   // G
            color_data |= Clamp((int)(B * 255.0)) << 0;   // B
            return color_data;
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        int Clamp(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
