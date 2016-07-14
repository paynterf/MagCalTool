using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MyWPFMagViewer2
{
    class LabeledCoordSysVis3D:CoordinateSystemVisual3D
    {
        #region Constants and Fields

        /// <summary>
        /// The arrow lengths property.
        /// </summary>
        public static readonly DependencyProperty AxisLabelSizeProperty = DependencyProperty.Register(
            "AxisLabelSize",
            typeof(double),
            typeof(LabeledCoordSysVis3D),
            new UIPropertyMetadata(1.0, GeometryChanged));

        ///// <summary>
        ///// The x axis label property.
        ///// </summary>
        //public static readonly DependencyProperty XAxisLabelProperty = DependencyProperty.Register(
        //    "XAxisLabel",
        //    typeof(BillboardTextItem),
        //    typeof(LabeledCoordSysVis3D),
        //    new UIPropertyMetadata("X"));
        /// <summary>
        /// The x axis label property.
        /// </summary>
        public static readonly DependencyProperty XAxisLabelProperty = DependencyProperty.Register(
            "XAxisLabel",
            typeof(string),
            typeof(LabeledCoordSysVis3D),
            new UIPropertyMetadata("X"));

        /// <summary>
        /// The y axis color property.
        /// </summary>
        public static readonly DependencyProperty YAxisLabelProperty = DependencyProperty.Register(
            "YAxisLabel",
            typeof(string),
            typeof(LabeledCoordSysVis3D),
            new UIPropertyMetadata("Y"));

        /// <summary>
        /// The z axis color property.
        /// </summary>
        public static readonly DependencyProperty ZAxisLabelProperty = DependencyProperty.Register(
            "ZAxisLabel",
            typeof(string),
            typeof(LabeledCoordSysVis3D),
            new UIPropertyMetadata("Z"));
        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LabeledCoordSysVis3D" /> class.
        /// </summary>
        public LabeledCoordSysVis3D()
        {
            base.OnGeometryChanged();
            this.OnGeometryChanged();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the label size.
        /// </summary>
        /// <value>The label size.</value>
        public double AxisLabelSize
        {
            get
            {
                return (double)this.GetValue(AxisLabelSizeProperty);
            }

            set
            {
                this.SetValue(AxisLabelSizeProperty, value);
            }
        }

        /// <summary>
        ///   Gets or sets the label of the X axis.
        /// </summary>
        /// <value>The label of the X axis.</value>
        public string XAxisLabel
        {
            get
            {
                return (string)GetValue(XAxisLabelProperty);
            }

            set
            {
                this.SetValue(XAxisLabelProperty, value);
            }
        }

        /// <summary>
        ///   Gets or sets the label of the Y axis.
        /// </summary>
        /// <value>The label of the Y axis.</value>
        public string YAxisLabel
        {
            get
            {
                return (string)GetValue(YAxisLabelProperty);
            }

            set
            {
                this.SetValue(YAxisLabelProperty, value);
            }
        }

        /// <summary>
        ///   Gets or sets the label of the Z axis.
        /// </summary>
        /// <value>The label of the Z axis.</value>
        public string ZAxisLabel
        {
            get
            {
                return (string)GetValue(ZAxisLabelProperty);
            }

            set
            {
                this.SetValue(ZAxisLabelProperty, value);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The geometry changed.
        /// </summary>
        /// <param name="obj">
        /// The obj.
        /// </param>
        /// <param name="args">
        /// The args.
        /// </param>
        protected new static void GeometryChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((LabeledCoordSysVis3D)obj).OnGeometryChanged();
        }

        /// <summary>
        /// Called when the geometry has changed.
        /// </summary>
        protected override void OnGeometryChanged()
        {
            base.OnGeometryChanged();
            //this.Children.Clear();
            double h = this.AxisLabelSize;

            var xlabel = new BillboardTextVisual3D();
            xlabel.Text = XAxisLabel.ToString();
            xlabel.Foreground = new SolidColorBrush(this.XAxisColor);
            xlabel.HeightFactor = AxisLabelSize;
            xlabel.Position = new Point3D(ArrowLengths * 1.2, 0, 0);
            this.Children.Add(xlabel);

            var ylabel = new BillboardTextVisual3D();
            ylabel.Text = YAxisLabel.ToString();
            ylabel.Foreground = new SolidColorBrush(this.YAxisColor);
            ylabel.HeightFactor = AxisLabelSize;
            ylabel.Position = new Point3D(0, ArrowLengths * 1.2, 0);
            this.Children.Add(ylabel);

            var zlabel = new BillboardTextVisual3D();
            zlabel.Text = ZAxisLabel.ToString();
            zlabel.Foreground = new SolidColorBrush(this.ZAxisColor);
            zlabel.HeightFactor = AxisLabelSize;
            zlabel.Position = new Point3D(0, 0, ArrowLengths * 1.2);
            this.Children.Add(zlabel);
        }

        #endregion
    }
}
