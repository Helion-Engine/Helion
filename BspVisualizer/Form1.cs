using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BSPVisualizer
{
    public partial class Form1 : Form
    {
        private static Font defaultFont = new Font("Arial", 16.0f);
        private static SolidBrush blackBackgroundBrush = new SolidBrush(Color.Black);
        private static Pen whitePen = new Pen(new SolidBrush(Color.White));
        private static Pen lightGrayPen = new Pen(new SolidBrush(Color.LightGray));
        private static Pen yellowPen = new Pen(new SolidBrush(Color.Yellow));

        public Form1()
        {
            InitializeComponent();
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.FillRectangle(blackBackgroundBrush, e.ClipRectangle);

            //PaintTextInfo(g, e.ClipRectangle);
            //PaintShadowLines(g, e.ClipRectangle);
            //PaintCurrentState(g, e.ClipRectangle);

            //g.DrawLine(oneSidedLinePen, 200, 200, 300, 300);
            //g.DrawString("hello", defaultFont, oneSidedLineBrush, new RectangleF(100, 100, 50, 10));
        }
    }
}
