using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Diagnostics;

namespace MyWPFMagViewer2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int numberOfPoints;
        private PointsVisual3D pointsVisual;
        private Point3DCollection points;

        public int NumberOfPoints
        {
            get
            {
                return this.numberOfPoints;
            }

            set
            {
                numberOfPoints = value;
            }
        }

        public Point3DCollection Points
        {
            get
            {
                return points;
            }

            set
            {
                points = value;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            NumberOfPoints = 100;

            if (Points == null ||Points.Count != this.NumberOfPoints)
            {
                Points = new Point3DCollection(GeneratePoints(NumberOfPoints,5));
                Debug.Print("After GeneratePoints, NumberOfPoints = " + NumberOfPoints + " Points.Count = " + Points.Count);
            }

            if (pointsVisual == null)
            {
                pointsVisual = new PointsVisual3D { Color = Colors.Red, Size = 6 };
               pointsVisual.Points = Points;

                vp_raw.Children.Add(pointsVisual);
            }
        }

        public static IEnumerable<Point3D> GeneratePoints(int n, double time)
        {
            const double R = 2;
            const double Q = 0.5;
            for (int i = 0; i < n; i++)
            {
                double t = Math.PI * 2 * i / (n - 1);
                double u = (t * 24) + (time * 5);
                var pt = new Point3D(Math.Cos(t) * (R + (Q * Math.Cos(u))), Math.Sin(t) * (R + (Q * Math.Cos(u))), Q * Math.Sin(u));
                //yield return pt;
                if (i > 0 && i < n - 1)
                {
                    yield return pt;
                }
            }
        }

    }
}
