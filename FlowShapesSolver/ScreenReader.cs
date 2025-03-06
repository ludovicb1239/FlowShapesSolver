using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FlowShapesSolver
{
    internal class ScreenReader
    {
        public static Bitmap TakeScreenshotRegion(Vector2 topLeft, Vector2 botRight)
        {
            int width = (int)(botRight.X - topLeft.X);
            int height = (int)(botRight.Y - topLeft.Y);

            Bitmap bitmap = new Bitmap(width, height);
            // Use the Graphics object to copy the pixel from the screen
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen((int)topLeft.X, (int)topLeft.Y, 0, 0, new Size(width, height));
            }
            return bitmap;
        }
    }
}
