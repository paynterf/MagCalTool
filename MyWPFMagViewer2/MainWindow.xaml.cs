using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;

//05/31/16 experiment
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using Color = SharpDX.Color;
using HitTestResult = HelixToolkit.Wpf.SharpDX.HitTestResult;

namespace MyWPFMagViewer2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int m_numberOfPoints;
        private PointsVisual3D m_pointsVisual;
        private Point3DCollection m_points;
        private Point3DCollection m_selpoints;
        private PointsVisual3D selPointsVisual;
        private const int POINTSIZE = 6;
        private List<int> m_selidxlist = new List<int>();

        public int NumberOfPoints
        {
            get
            {
                return this.m_numberOfPoints;
            }

            set
            {
                m_numberOfPoints = value;
            }
        }

        public Point3DCollection Points
        {
            get
            {
                return m_points;
            }

            set
            {
                m_points = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            NumberOfPoints = 100;

            if (Points == null || Points.Count != this.NumberOfPoints)
            {
                //time parameter in millisec used to animate dot positions
                //Points = new Point3DCollection(GeneratePoints(NumberOfPoints,
                //    (double)(DateTime.Now.Millisecond) / 10));
                Points = new Point3DCollection(GeneratePoints(NumberOfPoints,
                    10.3));
                //Debug.Print("After GeneratePoints, NumberOfPoints = " + NumberOfPoints + " Points.Count = " + Points.Count);

                if (m_pointsVisual != null)
                {
                    m_pointsVisual.Points.Clear();
                    m_pointsVisual.Points = Points;
                }
                else
                {
                    m_pointsVisual = new PointsVisual3D { Color = Colors.Red, Size = POINTSIZE };
                    m_pointsVisual.Points = Points;
                    m_pointsVisual.SetName("magpoints");

                    vp_raw.Children.Add(m_pointsVisual);
                }
            }
        }

        public static IEnumerable<Point3D> GeneratePoints(int n, double time)
        {
            //Purpose: Generate animated array of points
            //Inputs:
            //  n = Integer denoting number of points to display
            //  time = elapsed time in msec since start of program - used to animate point positions

            //Debug.Print("GeneratePoints time = " + time);

            const double R = 2;
            const double Q = 0.5;
            for (int i = 0; i < n; i++)
            {
                double t = Math.PI * 2 * i / (n - 1);
                double u = (t * 24) + (time * 5);
                var pt = new Point3D(Math.Cos(t) * (R + (Q * Math.Cos(u))), Math.Sin(t) * (R + (Q * Math.Cos(u))), Q * Math.Sin(u));
                //yield return pt2;
                if (i > 0 && i < n - 1)
                {
                    yield return pt;
                }
            }
        }

        private void vp_raw_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Point mousept = e.GetPosition(vp_raw);
            Debug.Print("At top of MouseDown event, Mouse position " + mousept.ToString());

            //code to print out current selected point list
            if (selPointsVisual != null)
            {
                int selcount = selPointsVisual.Points.Count;
                for (int i = 0; i < selcount; i++)
                {
                    Point3D pt = selPointsVisual.Points[i];
                    Debug.Print("selected pt2 at index 1 = ("
                        + pt.X.ToString("F2") + ", "
                        + pt.Y.ToString("F2") + ", "
                        + pt.Z.ToString("F2") + ")");
                }
            }
            else
            {
                Debug.Print("No selected points exist");
            }

        //Step1:  Get the current transformation matrix
            //get a reference to the visual element containing datapoints
            int chldcount = vp_raw.Children.Count;
            PointsVisual3D p3dvis = null; //temp object
            for (int i = 0; i < vp_raw.Children.Count; i++)
            {
                Visual3D Vis = (Visual3D)vp_raw.Children[i];
                string visnamestr = Vis.GetName();
                Debug.Print("visual element [" + i + "]'s name is " + visnamestr);
                if (visnamestr != null && visnamestr.Contains("magpoints"))
                {
                    p3dvis = (PointsVisual3D)Vis;
                    break;
                }
            }

            //use the temp object to get the transformation matrix for the parent viewport
            Matrix3D m3D = p3dvis.GetViewportTransform();
            //Debug.Print(m3D.ToString());

            //Point3D xfrmpt = m3D.Transform(new Point3D(0,0,0));
            //string ctrptstr = xfrmpt.X.ToString("F2") + ", "
            //    + xfrmpt.Y.ToString("F2") + ", "
            //    + xfrmpt.Z.ToString("F2");
            //Debug.Print("(0,0,0) transforms to " + ctrptstr);

        //Step2: transfer any currently selected points back to main points list unless SHIFT key is down
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
            {
                if (selPointsVisual != null)
                {
                    int selcount = selPointsVisual.Points.Count;
                    int magptcount = m_pointsVisual.Points.Count;
                    Debug.Print("selcount = " + selcount + ", magptcount = " + magptcount);
                    if (selcount > 0)
                    {
                        for (int i = 0; i < selcount; i++)
                        {
                            //get selected point 
                            Point3D pt = selPointsVisual.Points[i];
                            string ptstr = pt.X.ToString("F2") + ","
                                            + pt.Y.ToString("F2") + ","
                                            + pt.Z.ToString("F2");

                            Debug.Print("Moving selected pt2 ("+ptstr+") to pointsVisual.  Before add, pt2 count is "
                                + magptcount);

                            //add it to pointsVisual
                            m_pointsVisual.Points.Add(selPointsVisual.Points[i]);
                            int newcount = m_pointsVisual.Points.Count;

                            //check that point got added properly
                            Point3D newpt = m_pointsVisual.Points[newcount-1];
                            string newptstr = pt.X.ToString("F2") + ","
                                            + pt.Y.ToString("F2") + ","
                                            + pt.Z.ToString("F2");

                            Debug.Print("pt2 ("+ptstr+") added to pointsVsiual at index "+ (newcount-1)
                                +". Count now " + newcount);
                        }
                        selPointsVisual.Points.Clear();
                    }
                }
            }

        //Step3: copy pointsVisual.Points index of any selected points to m_selidxlist
            m_selidxlist.Clear();
            int ptidx = 0;
            //List<int> m_selidxlist = new List<int>();
            foreach (Point3D pt in m_pointsVisual.Points)
            {
                Point3D xpt = m3D.Transform(pt);
                double distsq = (mousept.X-xpt.X)*(mousept.X-xpt.X) + (mousept.Y-xpt.Y)*(mousept.Y-xpt.Y);
                double dist = Math.Sqrt(distsq);
                if (dist < 5)
                {
                    m_selidxlist.Add(ptidx); //save the index of the point to be removed

                    //p3dvis.Color = Colors.Yellow;
                    string ptstr = pt.X.ToString("F2") + ","
                        + pt.Y.ToString("F2") + ","
                        + pt.Z.ToString("F2");

                    string xfrmptstr = xpt.X.ToString("F2") + ", "
                        + xpt.Y.ToString("F2") + ", "
                        + xpt.Z.ToString("F2");
                    Debug.Print("dist = " + dist.ToString("F2") + ": mousepoint " + mousept.ToString()
                        + " matches with magpt[" + ptidx + "] ( " + ptstr + "), " 
                        + "which transforms to " + xfrmptstr);
                }

                ptidx++;
            }

            //move selected points from pointsVisual3D to selpointsVisual3D
            Debug.Print("Selected Index List Contains " + m_selidxlist.Count+" items");
            int count = m_selidxlist.Count;
            for (int i = count-1; i >= 0 ; i--)
            {
                //copy the point to the selected points PointsVisual3D collection
                if (m_selpoints == null)
                {
                    m_selpoints = new Point3DCollection();
                    for (int selidx = 0; selidx < count; selidx++)
                    {
                        //retrieve selected point from pointsVisual collection
                        int visptidx = m_selidxlist[selidx]; //this is the index into pointsVisual.Points collection
                        Point3D selvispt = new Point3D();
                        selvispt.X = m_pointsVisual.Points[visptidx].X;
                        selvispt.Y = m_pointsVisual.Points[visptidx].Y;
                        selvispt.Z = m_pointsVisual.Points[visptidx].Z;

                        //add the point to 
                        m_selpoints.Add(selvispt);

                        int newcount = m_selpoints.Count;
                        string ptstr2 = selvispt.X.ToString("F2") + ","
                                        + selvispt.Y.ToString("F2") + ","
                                        + selvispt.Z.ToString("F2");

                        Debug.Print("pt2 (" + ptstr2 + ") added to m_selpoints Collection. Count now " + newcount);
                    }

                    selPointsVisual = new PointsVisual3D { Color = Colors.Yellow, Size = 2* POINTSIZE };
                    selPointsVisual.Points = m_selpoints;
                    selPointsVisual.SetName("Selected Points");
                    vp_raw.Children.Add(selPointsVisual);
                }
                else
                {
                    selPointsVisual.Points.Add(m_pointsVisual.Points[m_selidxlist[i]]);
                }

                //DEBUG!!
                foreach (Point3D pt3 in selPointsVisual.Points)
                {
                    string newptstr3 = pt3.X.ToString("F2") + ","
                                    + pt3.Y.ToString("F2") + ","
                                    + pt3.Z.ToString("F2");
                    Debug.Print("Selected pt2 (" + newptstr3 + ")");
                }

                //Point3D pt = pointsVisual.Points[i];
                Point3D pt = m_pointsVisual.Points[m_selidxlist[i]];
                string ptstr = pt.X.ToString("F2") + ","
                                + pt.Y.ToString("F2") + ","
                                + pt.Z.ToString("F2");

                Debug.Print("pt2 (" + ptstr + ") added to m_selpoints Collection.");

                //p3dvis.Points.RemoveAt(m_selidxlist[i]);
                Debug.Print("removing point at " + m_selidxlist[i]);
                m_pointsVisual.Points.RemoveAt(m_selidxlist[i]);
            }
            vp_raw.UpdateLayout();
        }
    }
}
