// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CalViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides a ViewModel for the Main window.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MyWPFMagViewer2
{
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using HelixToolkit.Wpf;
    using System.Windows.Input;
    using HelixToolkit.Wpf.SharpDX;
    using SharpDX;

    /// <summary>
    /// Provides a ViewModel for the Main window 'Raw' viewport.
    /// </summary>
    public class RawViewModel
    {
        enum CirclePlane
        {
            PLANE_XY,
            PLANE_XZ,
            PLANE_YZ
        }

        //private int m_numberOfPoints;
        private PointsVisual3D m_pointsVisual; //3D point cloud for magnetometer points
        private PointsVisual3D selPointsVisual;//contains selected points from m_pointsVisual
        private Point3DCollection m_points;
        private const int POINTSIZE = 6; //display size for magnetometer points
        private List<int> m_selidxlist = new List<int>();//contains indices into mag points collection for selected points


        public PointsVisual3D rawpointsVisual
        { get
            {
                return m_pointsVisual;
            }
        }

        public PointsVisual3D selpointsVisual
        { get
            {
                return selPointsVisual;
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

        public int SelPointCount
        {
            get
            {
                return selPointsVisual.Points.Count;
            }
        }

        public int RawPointCount
        {
            get
            {
                return m_pointsVisual.Points.Count;
            }
        }

        public HelixViewport3D view3d { get; set; }
        public MainWindow main_window { get; set; }
        public System.Windows.Media.Media3D.Model3D ViewportGeometryModel { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public RawViewModel(HelixViewport3D view, MainWindow main)
        {
            view3d = view;
            main_window = main;

            //generated points are only used on startup so there is something to see in the viewport
            Points = new Point3DCollection(GeneratePoints(100, 10.3));

            // Create a model group - this contains all the 3D models to be displayed
            var modelGroup = new Model3DGroup();

            //'raw' and 'selected' points aren't part of model group - they are added directly to the view
            m_pointsVisual = new PointsVisual3D { Color = Colors.Red, Size = POINTSIZE };
            //m_pointsVisual = new PointsVisual3D { Color = Colors.Green, Size = POINTSIZE };
            m_pointsVisual.Points = Points;
            m_pointsVisual.SetName("magpoints");
            view3d.Children.Add(m_pointsVisual);

            selPointsVisual = new PointsVisual3D { Color = Colors.Yellow, Size = 1.5 * POINTSIZE };
            selPointsVisual.SetName("selpoints");
            view3d.Children.Add(selPointsVisual);

            // Create materials for reference circles (TubeVisual3D objects)
            var greenMaterial = MaterialHelper.CreateMaterial(Colors.Green);
            var redMaterial = MaterialHelper.CreateMaterial(Colors.Red);
            var blueMaterial = MaterialHelper.CreateMaterial(Colors.Blue);
            var insideMaterial = MaterialHelper.CreateMaterial(Colors.Yellow);


            //reference circles using TubeVisual3D object
            double thicknessfactor = 0.05; //established empirically
            double diam = 1000; 

            //ring in XY plane
            TubeVisual3D t_xy = new TubeVisual3D();
            //t_xy.Fill = System.Windows.Media.Brushes.Black;
            Point3DCollection p3dc = GenerateCirclePoints(36, diam, CirclePlane.PLANE_XY);
            t_xy.Diameter = thicknessfactor*diam; //1% factor emperically determined
            t_xy.Material = redMaterial;
            t_xy.Path = p3dc;
            modelGroup.Children.Add(t_xy.Model);

            //ring in Xz plane
            TubeVisual3D t_xz = new TubeVisual3D();
            t_xz.Material = greenMaterial;
            p3dc = GenerateCirclePoints(36, diam, CirclePlane.PLANE_XZ);
            t_xz.Material = greenMaterial;
            t_xz.Path = p3dc;
            t_xz.Diameter = t_xy.Diameter;
            modelGroup.Children.Add(t_xz.Model);

            ////ring in yz plane
            TubeVisual3D t_yz = new TubeVisual3D();
            p3dc = GenerateCirclePoints(36, diam, CirclePlane.PLANE_YZ);
            t_yz.Diameter = t_xy.Diameter;
            t_yz.Material = blueMaterial;
            t_yz.Path = p3dc;
            modelGroup.Children.Add(t_yz.Model);

            // Set the property, which will be bound to the Content property of the ModelVisual3D (see MainWindow.xaml)
            ViewportGeometryModel = modelGroup;
        }

        private Point3DCollection GeneratePoints(int n, double time)
        {
            Point3DCollection pc = new Point3DCollection();
            const double R = 2;
            const double Q = 0.5;
            for (int i = 0; i < n; i++)
            {
                double t = Math.PI * 2 * i / (n - 1);
                double u = (t * 24) + (time * 5);
                var pt = new Point3D(Math.Cos(t) * (R + (Q * Math.Cos(u))), Math.Sin(t) * (R + (Q * Math.Cos(u))), Q * Math.Sin(u));
                pc.Add(pt);
            }
            return pc;
        }

        Point3DCollection GenerateCirclePoints(int numpts = 100, double radius = 1, CirclePlane plane = CirclePlane.PLANE_XY)
        {
            double d_theta = Math.PI * 2 / numpts;
            Point3DCollection p3dc = new Point3DCollection();

            switch (plane)
            {
                case CirclePlane.PLANE_XY:
                    for (int i = 0; i <= numpts; i++)
                    {
                        p3dc.Add(new Point3D(Math.Cos(i * d_theta) * radius, Math.Sin(i * d_theta) * radius, 0));
                        Debug.Print("point " + i + ": " + p3dc[i].ToString());
                    }
                    break;
                case CirclePlane.PLANE_XZ:
                    for (int i = 0; i <= numpts; i++)
                    {
                        p3dc.Add(new Point3D(Math.Cos(i * d_theta) * radius, 0, Math.Sin(i * d_theta) * radius));
                        Debug.Print("point " + i + ": " + p3dc[i].ToString());
                    }
                    break;
                case CirclePlane.PLANE_YZ:
                    for (int i = 0; i <= numpts; i++)
                    {
                        p3dc.Add(new Point3D(0, Math.Cos(i * d_theta) * radius, Math.Sin(i * d_theta) * radius));
                        Debug.Print("point " + i + ": " + p3dc[i].ToString());
                    }
                    break;
                default:
                    break;
            }

            return p3dc;
        }

        public void Rawview_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Point mousept = e.GetPosition(view3d);
            Debug.Print("At top of MouseDown event, Mouse position " + mousept.ToString());

            //code to print out current selected point list
            if (selPointsVisual != null)
            {
                int selcount = selPointsVisual.Points.Count;
                for (int i = 0; i < selcount; i++)
                {
                    Point3D selpt = selPointsVisual.Points[i];
                    Debug.Print("selected pt at index " + i + " = ("
                        + selpt.X.ToString("F2") + ", "
                        + selpt.Y.ToString("F2") + ", "
                        + selpt.Z.ToString("F2") + ")");
                }
            }
            else
            {
                Debug.Print("No selected points exist");
            }

            //Step1:  Get the current transformation matrix
            //get a reference to the visual element containing datapoints
            int chldcount = view3d.Children.Count;
            PointsVisual3D p3dvis = null; //temp object
            for (int i = 0; i < view3d.Children.Count; i++)
            {
                Visual3D Vis = (Visual3D)view3d.Children[i];
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
                            Point3D selpt = selPointsVisual.Points[i];
                            string selptstr = selpt.X.ToString("F2") + ","
                                            + selpt.Y.ToString("F2") + ","
                                            + selpt.Z.ToString("F2");

                            Debug.Print("Moving selected pt (" + selptstr + ") to pointsVisual.  Before add, pt count is "
                                + m_pointsVisual.Points.Count);

                            //add it to pointsVisual
                            m_pointsVisual.Points.Add(selPointsVisual.Points[i]);
                            int newcount = m_pointsVisual.Points.Count;

                            //check that point got added properly
                            Point3D newpt = m_pointsVisual.Points[newcount - 1];
                            string newptstr = newpt.X.ToString("F2") + ","
                                            + newpt.Y.ToString("F2") + ","
                                            + newpt.Z.ToString("F2");

                            Debug.Print("point (" + newptstr + ") added to pointsVsiual at index " + (newcount - 1)
                                + ". Count now " + newcount);
                        }
                        selPointsVisual.Points.Clear();
                    }
                }
            }

            //Step3: copy pointsVisual.Points index of any selected points to m_selidxlist
            m_selidxlist.Clear();
            int ptidx = 0;
            foreach (Point3D vispt in m_pointsVisual.Points)
            {
                Point3D xpt = m3D.Transform(vispt);
                double distsq = (mousept.X - xpt.X) * (mousept.X - xpt.X) + (mousept.Y - xpt.Y) * (mousept.Y - xpt.Y);
                double dist = Math.Sqrt(distsq);
                if (dist < 5)
                {
                    m_selidxlist.Add(ptidx); //save the index of the point to be removed

                    string visptstr = vispt.X.ToString("F2") + ","
                        + vispt.Y.ToString("F2") + ","
                        + vispt.Z.ToString("F2");

                    string xfrmptstr = xpt.X.ToString("F2") + ", "
                        + xpt.Y.ToString("F2") + ", "
                        + xpt.Z.ToString("F2");
                    Debug.Print("dist = " + dist.ToString("F2") + ": mousepoint " + mousept.ToString()
                        + " matches with magpt[" + ptidx + "] ( " + visptstr + "), "
                        + "which transforms to " + xfrmptstr);
                }

                ptidx++;
            }

            //move selected points from pointsVisual3D to selpointsVisual3D
            int selidxcount = m_selidxlist.Count;
            Debug.Print("Selected Index List Contains " + selidxcount + " items");

            //06/12/16 moved all point move code to function so can call from mi_UseSelRadius_Checked()
            MoveSelToSelPointsVisual();
            view3d.UpdateLayout();//refresh the 'raw' view
            main_window.UpdateControls();
        }


        private void MoveSelToSelPointsVisual()
        {
            int selidxcount = m_selidxlist.Count;
            if (selidxcount > 0)
            {
                int newcount = 1;
                //06/06/16 new try at moving selected points from m_pointsVisual to selPointsVisual
                for (int selidx = selidxcount - 1; selidx >= 0; selidx--)//have to work from top down to avoid crashes
                {
                    //retrieve selected point from pointsVisual collection
                    int visptidx = m_selidxlist[selidx]; //this is the index into pointsVisual.Points collection
                    Point3D selvispt = new Point3D();
                    selvispt.X = m_pointsVisual.Points[visptidx].X;
                    selvispt.Y = m_pointsVisual.Points[visptidx].Y;
                    selvispt.Z = m_pointsVisual.Points[visptidx].Z;
                    string selvisptstr = selvispt.X.ToString("F2") + ","
                                    + selvispt.Y.ToString("F2") + ","
                                    + selvispt.Z.ToString("F2");
                    Debug.Print("selected pt (" + selvisptstr + ") added to selPointsVisual Points Collection. Count now "
                        + newcount);

                    //add this point to selPointsVisual
                    selPointsVisual.Points.Add(selvispt);

                    //testing - print out added point
                    int addedcount = selPointsVisual.Points.Count;
                    Point3D addedpt = new Point3D();
                    addedpt = selPointsVisual.Points[addedcount - 1];
                    string addedptstr = addedpt.X.ToString("F2") + ","
                                    + addedpt.Y.ToString("F2") + ","
                                    + addedpt.Z.ToString("F2");
                    Debug.Print("added pt (" + addedptstr + ") added to selPointsVisual. Count now "
                        + addedcount);

                    //remove this point from m_pointsVisual collection
                    Debug.Print("removing point at m_pointsVisual[" + m_selidxlist[selidx] + "]");
                    m_pointsVisual.Points.RemoveAt(m_selidxlist[selidx]);
                }
            }
        }

        public double GetRawAvgRadius()
        {
            //Purpose: Compute the average radius of all non-selected raw magetometer points
            //Plan:
            //Step1: Iterate through all points in m_pointsVisual, computing average radius
            double avgradius = 0;
            double radius = 0;
            int numpts = 0;
            foreach (Point3D pt3d in m_pointsVisual.Points)
            {
                numpts++;
                Vector3 V3 = pt3d.ToVector3();
                radius = V3.Length();
                avgradius = ((double)(numpts - 1) / numpts) * avgradius + (radius / numpts);//running average
            }
            return avgradius;
        }

        public void SelectBeyondRadius(string selradius_str)
        {
            //Purpose: Select all points beyond the radius specified in tb_SelRadius
            //Plan:
            //  Step1: Clear list of selected indices, and all points from selPointsVisual.Points list
            //  Step2: Iterate through all m_pointsVisual points & copy index of any qualifying points to m_selidxlist
            //  Step3: For each index in m_selidxlist, move assoc pt from m_pointsVisaul to selPointsVisual
            //  Step4: Update 'selected' count label

            try
            {
                double selradius = Convert.ToDouble(selradius_str);//this could fail

                //Step1: Clear list of selected indices, and all points from selPointsVisual.Points list
                m_selidxlist.Clear();
                selPointsVisual.Points.Clear(); //added 06/21/16

                //Step2: Iterate through all m_pointsVisual points & copy index of any qualifying points to m_selidxlist
                int ptidx = 0;
                foreach (Point3D vispt in m_pointsVisual.Points)
                {
                    double radius = vispt.DistanceTo(new Point3D(0, 0, 0));
                    if (radius > selradius)
                    {
                        m_selidxlist.Add(ptidx); //save the index of the point to be moved
                    }

                    ptidx++;
                }

                //Step3: For each index in m_selidxlist, move assoc pt from m_pointsVisaul to selPointsVisual
                int selidxcount = m_selidxlist.Count;
                Debug.Print("Selected Index List Contains " + selidxcount + " items");
                                MoveSelToSelPointsVisual();

                //  Step4: Update view & 'selected' count label
                main_window.UpdateLayout();
                main_window.UpdateControls();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}