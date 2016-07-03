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
            //set
            //{
            //    m_pointsVisual = value;
            //}
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
        public System.Windows.Media.Media3D.Model3D Model { get; set; }
        private const int POINTSIZE = 6; //display size for magnetometer points

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public CalViewModel(HelixViewport3D view, MainWindow main)
        {
            view3d = view;
            main_window = main;
            m_pointsVisual = new PointsVisual3D { Color = Colors.Red, Size = POINTSIZE };
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

            // Add 3 models to the group (using the same mesh, that's why we had to freeze it)
            //modelGroup.Children.Add(new GeometryModel3D { Geometry = mesh, Material = greenMaterial, BackMaterial = insideMaterial });
            //modelGroup.Children.Add(new GeometryModel3D { Geometry = mesh, Transform = new TranslateTransform3D(-2, 0, 0), Material = redMaterial, BackMaterial = insideMaterial });
            //modelGroup.Children.Add(new GeometryModel3D { Geometry = mesh, Transform = new TranslateTransform3D(2, 0, 0), Material = blueMaterial, BackMaterial = insideMaterial });

            ////add Y-Z plane ellipsoid
            //EllipsoidVisual3D YZ = new EllipsoidVisual3D();
            //YZ.RadiusX = 0; //Y-Z plane
            //YZ.Model.Material = redMaterial;
            //YZ.Model.BackMaterial = YZ.Model.Material;
            //modelGroup.Children.Add(YZ.Model);

            ////add X-Z ellipsoid
            //EllipsoidVisual3D XZ = new EllipsoidVisual3D();
            //XZ.RadiusY = 0; //X-Z plane
            //XZ.Model.Material = greenMaterial;
            //XZ.Model.BackMaterial = XZ.Model.Material;
            //modelGroup.Children.Add(XZ.Model);

            ////add X-Y ellipsoid
            //EllipsoidVisual3D XY = new EllipsoidVisual3D();
            //XY.RadiusZ = 0; //X-Z plane
            //XY.Model.Material = blueMaterial;
            //XY.Model.BackMaterial = XY.Model.Material;
            //modelGroup.Children.Add(XY.Model);

            //add circular pipe (tube)
            //TubeVisual3D tubeXY = new TubeVisual3D();
            ////tubeXY.Material = redMaterial;
            ////tubeXY.Diameter = 2;
            //modelGroup.Children.Add(tubeXY.Model);
            //TubeVisual3D t = new TubeVisual3D();
            //t.Fill = System.Windows.Media.Brushes.Black;

            //Point3DCollection p3dc = new Point3DCollection();
            //List<double> diamlist = new List<double>();
            //double dx = 15;
            //double dy = 15;
            //double dz = 15;
            //double dd = 10;
            //int n = 36;
            //double d_theta = Math.PI * 2 / n;
            //double radius = 10;
            //for (int i = 0; i <= n; i++)
            //{
            //    //p3dc.Add(new Point3D(0 + Math.Pow((double)i / n, 3) * dx, 0 + ((double)i / n) * dy, 0 + ((double)i / n) * dz));
            //    p3dc.Add(new Point3D(Math.Cos(i*d_theta)*radius, Math.Sin(i*d_theta)*radius, 0));
            //    Debug.Print("point " + i + ": " + p3dc[i].ToString());
            //    //diamlist.Add(3.5 + ((double)i / n) * dd);
            //}
            ////t.Diameters = new DoubleCollection(diamlist);
            //t.Diameter = 0.01*radius; //1% factor emperically determined
            //t.Material = redMaterial;
            //t.Path = p3dc;
            //modelGroup.Children.Add(t.Model);

            //Point3DCollection p3dc = GeneratePoints(1000, 1000);

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
            this.Model = modelGroup;
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        //public Model3D Model { get; set; }

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

        //public static IEnumerable<Point3D> GeneratePoints(int n, double time)
        //{
        //    const double R = 2;
        //    const double Q = 0.5;
        //    for (int i = 0; i < n; i++)
        //    {
        //        double t = Math.PI * 2 * i / (n - 1);
        //        double u = (t * 24) + (time * 5);
        //        var pt = new Point3D(Math.Cos(t) * (R + (Q * Math.Cos(u))), Math.Sin(t) * (R + (Q * Math.Cos(u))), Q * Math.Sin(u));
        //        yield return pt;
        //        if (i > 0 && i < n - 1)
        //        {
        //            yield return pt;
        //        }
        //    }
        //}

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