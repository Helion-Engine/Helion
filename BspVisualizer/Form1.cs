using Helion.BSP.Builder;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BspVisualizer
{
    public partial class Form1 : Form
    {
        private static Font defaultFont = new Font("Arial", 16.0f);
        private static SolidBrush blackBackgroundBrush = new SolidBrush(Color.Black);
        private static SolidBrush whiteBrush = new SolidBrush(Color.White);
        private static SolidBrush cyanBrush = new SolidBrush(Color.Cyan);
        private static Pen whitePen = new Pen(new SolidBrush(Color.White));
        private static Pen lightGrayPen = new Pen(new SolidBrush(Color.LightGray));
        private static Pen yellowPen = new Pen(new SolidBrush(Color.Yellow));

        private StepwiseBspBuilder bspBuilder;

        public Form1(StepwiseBspBuilder builder)
        {
            bspBuilder = builder;
            InitializeComponent();
        }

        /// <summary>
        /// Draws a series of strings on one line. This allows multiple strings
        /// with different colors to be drawn sequentially.
        /// </summary>
        /// <param name="g">The graphics object.</param>
        /// <param name="font">The font to draw with.</param>
        /// <param name="topLeft">The top left corner.</param>
        /// <param name="colorAndStrings">A variable length array which should
        /// be an alternation of brush, then string pairs. For example, this
        /// can be: whitebrush, "hi", blueBrush "!"</param>
        private void DrawStrings(Graphics g, Font font, PointF topLeft, params object[] colorAndStrings)
        {
            int xOffset = (int)topLeft.X;

            for (int i = 0; i < colorAndStrings.Length / 2; i++)
            {
                SolidBrush brush = colorAndStrings[i * 2] as SolidBrush ?? whiteBrush;
                string str = colorAndStrings[(i * 2) + 1] as string ?? "?";

                SizeF size = g.MeasureString(str, font);
                RectangleF drawBox = new RectangleF(xOffset, topLeft.Y, xOffset + size.Width, topLeft.Y + size.Height);
                g.DrawString(str, font, brush, drawBox);

                xOffset += (int)size.Width;
            }
        }

        private void PaintBackground(Graphics g, Rectangle windowBounds)
        {
            g.FillRectangle(blackBackgroundBrush, windowBounds);
        }

        private void PaintShadowLines(Graphics g, Rectangle windowBounds)
        {
            // TODO
        }

        private void PaintCurrentState(Graphics g, Rectangle windowBounds)
        {
            // TODO
        }

        private void PaintTextInfo(Graphics g, Rectangle windowBounds)
        {
            DrawStrings(g, defaultFont, new PointF(2, windowBounds.Height - 30),
                        whiteBrush, "State: ", 
                        cyanBrush, bspBuilder.States.Next.ToString());
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            canvasPanel.Invalidate();
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle windowBounds = e.ClipRectangle;

            PaintBackground(g, windowBounds);
            PaintShadowLines(g, windowBounds);
            PaintCurrentState(g, windowBounds);
            PaintTextInfo(g, windowBounds);
        }

        void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            bool consumedKey = true;
            bool needsRepaint = true;

            switch (e.KeyChar)
            {
                case 'w':
                case 'W':
                    MoveCameraUp();
                    break;
                case 'a':
                case 'A':
                    MoveCameraLeft();
                    break;
                case 's':
                case 'S':
                    MoveCameraDown();
                    break;
                case 'd':
                case 'D':
                    MoveCameraRight();
                    break;
                case '-':
                    ZoomOut();
                    break;
                case '=':
                    ZoomIn();
                    break;
                case 'n':
                case 'N':
                    bspBuilder.ExecuteMinorStep();
                    break;
                case 'm':
                case 'M':
                    bspBuilder.ExecuteMajorStep();
                    break;
                default:
                    needsRepaint = false;
                    consumedKey = false;
                    break;
            }

            if (consumedKey)
                e.Handled = false;

            if (needsRepaint)
                canvasPanel.Invalidate();
        }
    }
}
