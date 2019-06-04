using Helion.Bsp.Builder;
using Helion.Bsp.Geometry;
using Helion.Bsp.States;
using Helion.Util.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace BspVisualizer
{
    public partial class Form1 : Form
    {
        private static Font defaultFont = new Font("Arial", 16.0f);
        private static SolidBrush blackBackgroundBrush = new SolidBrush(Color.Black);
        private static SolidBrush blueBrush = new SolidBrush(Color.Blue);
        private static SolidBrush cyanBrush = new SolidBrush(Color.Cyan);
        private static SolidBrush redBrush = new SolidBrush(Color.Red);
        private static SolidBrush grayBrush = new SolidBrush(Color.Gray);
        private static SolidBrush whiteBrush = new SolidBrush(Color.White);
        private static SolidBrush yellowBrush = new SolidBrush(Color.Yellow);
        private static Pen bluePen = new Pen(blueBrush);
        private static Pen cyanPen = new Pen(cyanBrush);
        private static Pen grayPen = new Pen(grayBrush);
        private static Pen redPen = new Pen(redBrush);
        private static Pen whitePen = new Pen(whiteBrush);
        private static Pen yellowPen = new Pen(yellowBrush);
        private static Pen shadowOneSidedLinePen = new Pen(new SolidBrush(Color.FromArgb(0x0A, 0x13, 0x30)));
        private static Pen shadowTwoSidedLinePen = new Pen(new SolidBrush(Color.FromArgb(0x07, 0x09, 0x18)));
        private static Vec2I cornerOffset = new Vec2I(4, 4);
        private static StringCornerDrawer bottomLeftCornerDrawer = new StringCornerDrawer(Corner.BottomLeft, cornerOffset);

        private StepwiseBspBuilder bspBuilder;
        private List<BspSegment> shadowSegments;
        private Vector2 camera = new Vector2(0, 0);
        private float zoom = 0.25f;

        public Form1(StepwiseBspBuilder builder)
        {
            bspBuilder = builder;
            shadowSegments = GetShadowSegments(builder.SegmentAllocator);

            InitializeComponent();
        }

        private List<BspSegment> GetShadowSegments(SegmentAllocator segmentAllocator)
        {
            List<BspSegment> segments = new List<BspSegment>();
            for (int i = 0; i < segmentAllocator.Count; i++)
                segments.Add(segmentAllocator[i]);
            return segments;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            canvasPanel.Invalidate();
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle windowBounds = e.ClipRectangle;

            AddMajorStateInfo();

            PaintBackground(g, windowBounds);
            PaintShadowLines(g);
            PaintCurrentWorkItem(g);
            PaintCurrentState(g, windowBounds);
            PaintVertices(g);
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
            case 'b':
            case 'B':
                bspBuilder.ExecuteMinorStep();
                break;
            case 'n':
            case 'N':
                bspBuilder.ExecuteMajorStep();
                break;
            case 'm':
            case 'M':
                bspBuilder.ExecuteFullCycleStep();
                break;
            case 'c':
            case 'C':
                BspWorkItem? workItem = bspBuilder.GetCurrentWorkItem();
                if (workItem != null)
                    Clipboard.SetText(workItem.BranchPath);
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
