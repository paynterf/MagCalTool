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
    using System.Diagnostics;
    using HelixToolkit.Wpf;

    /// <summary>
    /// Provides a ViewModel for the Main window 'Calibrated' viewport.
    /// </summary>
    public class CalViewModel
    {
        enum CirclePlane
        {
            PLANE_XY,
            PLANE_XZ,
            PLANE_YZ
        }

        private PointsVisual3D m_pointsVisual; //3D point cloud for magnetometer points
        private Point3DCollection m_points;

        public PointsVisual3D calpointsVisual
        {
            get
            {
                return m_pointsVisual;
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

        public HelixViewport3D view3d { get; set; }
        public MainWindow main_window { get; set; }
        public Model3D ViewportGeometryModel { get; set; }
        private const int POINTSIZE = 6; //display size for magnetometer points

        /// <summary>
        /// Initializes a new instance of the CalViewModel class.
        /// </summary>
        public CalViewModel(HelixViewport3D view, MainWindow main)
        {
            view3d = view;
            main_window = main;
            m_pointsVisual = new PointsVisual3D { Color = Colors.Red, Size = POINTSIZE };

            //generated points are only used on startup so there is something to see in the viewport
            Points = new Point3DCollection(GeneratePoints(100, 10.3));
            m_pointsVisual.Points = Points;
            m_pointsVisual.SetName("calpoints");
            view3d.Children.Add(m_pointsVisual);

            // Create a model group
            var modelGroup = new Model3DGroup();


            // Create some materials
            var greenMaterial = MaterialHelper.CreateMaterial(Colors.Green);
            var redMaterial = MaterialHelper.CreateMaterial(Colors.Red);
            var blueMaterial = MaterialHelper.CreateMaterial(Colors.Blue);
            var insideMaterial = MaterialHelper.CreateMaterial(Colors.Yellow);


            //reference circles using TubeVisual3D object
            double thicknessfactor = 0.05; //established empirically
            double diam = 1; //in cal view, reference circle radius is always 1

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

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        //public Model3D ViewportGeometryModel { get; set; }

        public Point3DCollection GeneratePoints(int n, double time)
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
    }
}