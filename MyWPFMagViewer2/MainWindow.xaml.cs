using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using System.Windows.Controls;

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
        private PointsVisual3D m_pointsVisual; //3D point cloud for magnetometer points
        private PointsVisual3D selPointsVisual;//contains selected points from m_pointsVisual
        private Point3DCollection m_points;//backing store for NumberOfPoints property
        private const int POINTSIZE = 6; //display size for magnetometer points
        private List<int> m_selidxlist = new List<int>();//contains indices into mag points collection for selected points
        private PointsVisual3D m_CalpointsVisual; //3D point cloud for calibrated magnetometer points

        //06/07/16 copied in from MagManager2
        CommPortManager commMgr = new CommPortManager();
        private Octave octave; //Octave object described in Octave.cs
        private string OctaveRawDataFile = "rawpts.txt"; //temp file to pass raw viewport points to Octave 
        private double[][] Caldata; //calibrated magnetometer data from Octave pass

        string transType = string.Empty;
        private bool bOctaveScriptFileExists = false;
        private bool bOctaveFunctionFileExists = false;
        private bool bOctaveExeFileExists = false;
        //private string displaystr = ""; //added for mbox display of interim results
        const string quote = "\""; //used to insert double-quote characters
        const string OctaveScriptFile = "MagCalScript.m";
        const string OctaveFunctionFile = "MgnCalibration.m";


        public bool bStringAvail = false;
        public bool bPtArrayUpdated = false;
        private bool bShowRaw = true;
        private bool bShowComp = false;
        //private bool bShowRefCircles = false;
        private bool bCommPortOpen = false;
        private const int MIN_COMP_POINTS = 100;


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
            Points = new Point3DCollection(GeneratePoints(NumberOfPoints, 10.3));

            //06/08/16 experiment to populate textblock control
            tbox_RawMagData.Text = string.Empty;
            int linecount = 0;
            foreach (Point3D pt in Points)
            {
                linecount++;
                tbox_RawMagData.Text += pt.ToString() + Environment.NewLine;
            }
            lbl_NumRtbLines.Content = linecount.ToString();

            m_pointsVisual = new PointsVisual3D { Color = Colors.Red, Size = POINTSIZE };
            m_pointsVisual.Points = Points;
            m_pointsVisual.SetName("magpoints");
            vp_raw.Children.Add(m_pointsVisual);

            selPointsVisual = new PointsVisual3D { Color = Colors.Yellow, Size = 2 * POINTSIZE };
            selPointsVisual.SetName("selpoints");
            vp_raw.Children.Add(selPointsVisual);

            //06/07/16 experiment with 2nd viewport window
            Point3DCollection CalPoints = new Point3DCollection(GeneratePoints(NumberOfPoints, 10.3));
            m_CalpointsVisual = new PointsVisual3D { Color = Colors.Red, Size = POINTSIZE };
            vp_cal.Title = "Calibrated Magnetometer Points";
            m_CalpointsVisual.Points = CalPoints;
            m_CalpointsVisual.SetName("calpoints");
            vp_cal.Children.Add(m_CalpointsVisual);

            //try adding an ellipse to view
            EllipsoidVisual3D ell1 = new EllipsoidVisual3D();
            //SolidColorBrush perimeterbrush = new SolidColorBrush(Colors.Red);
            System.Windows.Media.Media3D.Material mat = MaterialHelper.CreateMaterial(new SolidColorBrush(Colors.Red));
            //ell1.Material = mat;
            //ell1.BackMaterial = mat;
            //ell1.Fill = perimeterbrush;
            ell1.Center = new Point3D(0, 0, 0);
            ell1.RadiusX = 1;
            ell1.RadiusZ = 1;
            ell1.RadiusY = 1;
            //ell1.Model.BackMaterial = mat;
            //ell1.Model.Material = mat;
            ell1.Fill = new SolidColorBrush(Colors.White);
            vp_cal.Children.Add(ell1);

            //EllipseGeometry ell2 = new EllipseGeometry(new System.Windows.Point(0, 0), 1, 1);
            //vp_cal.Children.Add(ell2);
            vp_cal.UpdateLayout();

            //06/08/16 copied from frmMagManager.cs
            //Load file/folder boxes with default contents from config file
            tbOctavePath.Text = Properties.Settings.Default.OctaveExePath;
            tbOctaveScriptFolder.Text = Properties.Settings.Default.OctaveScriptFolder;

            //06/08/16 experiment with text-wrapping button
            RichTextBox rtb = new RichTextBox();
            rtb.IsReadOnly = true;
            rtb.Focusable = false;
            rtb.BorderThickness = new Thickness(0);
            rtb.Background = Brushes.Transparent;
            rtb.AppendText("Update Raw View");
            btn_UpdateRawView.Content = rtb;
        }

        //this function is just to generate a static set of test points
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

                //DEBUG!!
                Debug.Print("\r\n selPointsVisual Points collection contains the following points");
                foreach (Point3D pt3 in selPointsVisual.Points)
                {
                    string newptstr3 = pt3.X.ToString("F2") + ","
                                    + pt3.Y.ToString("F2") + ","
                                    + pt3.Z.ToString("F2");
                    Debug.Print("Selected pt (" + newptstr3 + ")");
                }
            }

            vp_raw.UpdateLayout();//refresh the 'raw' view
        }

        private void vp_cal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.Print("In vp_cal_MouseDown");
        }

        private void btn_CommPortOpen_Click(object sender, RoutedEventArgs e)
        {
            commMgr.PortName = cbox_CommPort.Text;
            commMgr.BaudRate = cbox_BaudRate.Text;
            bCommPortOpen = commMgr.OpenPort();

            UpdateControls();
        }

        private void frmMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //06/08/16 all copied from frmMagManager.cs
            //this.vp_Raw.ActionMode = viewportActionType.SelectVisibleByPick; //chg 02/13/09 so hidden objects aren't selected
            //this.vp_Raw.DisplayMode = viewportDisplayType.Shaded; //added 02/12/09

            LoadValues();
            SetDefaults();
            SetControlState();
            commMgr.DisplayForm = this;
            UpdateControls();
        }

        private void SetDefaults()
        {
            cbox_CommPort.SelectedIndex = 0;
            cbox_BaudRate.SelectedIndex = 5; //chg from 'SelectedText="9600"' to 'SelectedIndex = 5'
        }

        private void LoadValues()
        {
            commMgr.SetPortNameValues(cbox_CommPort);
        }

        private void SetControlState()
        {
            btn_CommPortClose.IsEnabled = false;
        }

        public void AddLineToRawDataView(System.Drawing.Color txtcolor, string portstr)
        {
            //rtb_RawMagData.SelectedText = string.Empty;
            //rtb_RawMagData.SelectionFont = new Font(rtb_RawMagData.SelectionFont, FontStyle.Bold);
            //rtb_RawMagData.SelectionColor = txtcolor;
            //rtb_RawMagData.AppendText(portstr);
            //rtb_RawMagData.ScrollToCaret();

            //06/08/16 try with WPF TextBlock object
            tbox_RawMagData.Text = tbox_RawMagData.Text + Environment.NewLine + portstr;
        }

        private void UpdateControls()
        {
            //serial commms buttons
            btn_CommPortOpen.IsEnabled = !bCommPortOpen;
            btn_CommPortClose.IsEnabled = bCommPortOpen;

            //file IO
            btn_Import.IsEnabled = !bCommPortOpen;
            btn_SaveAs.IsEnabled = !bCommPortOpen && tbox_RawMagData.LineCount > 0;
            btn_ClearMagData.IsEnabled = !bCommPortOpen;
            btn_UpdateRawView.IsEnabled = !bCommPortOpen && tbox_RawMagData.LineCount > 0;

            //Compute button
            //btn_Compute.Enabled = vp_Raw.Entities.Count >= MIN_COMP_POINTS
            //    && bOctaveFunctionFileExists && bOctaveScriptFileExists;

            //file/script locaton textbox colors
            //tbOctaveScriptFolder.BackColor = (bOctaveScriptFileExists && bOctaveFunctionFileExists) ? Color.LightGreen : Color.LightPink;
            //tbOctavePath.BackColor = (bOctaveExeFileExists) ? Color.LightGreen : Color.LightPink;
            tbOctaveScriptFolder.Background = (bOctaveScriptFileExists && bOctaveFunctionFileExists) ? Brushes.LightGreen : Brushes.LightPink;
            tbOctavePath.Background = (bOctaveExeFileExists) ? Brushes.LightGreen : Brushes.LightPink;
        }

        public void ProcessCommPortString(string linestr)
        {
            //convert commport line into Vector3D object
            Vector3D v3d = GetVector3DFromString(linestr);

            //add the line to the rich text box
            AddLineToRawDataView(System.Drawing.Color.Black, linestr);

            //add the point to the viewport Entity list
            AddPointToRaw3DView(v3d);

            //refresh the view
            //vp_Raw.Refresh();
            //vp_Raw.ZoomFit();
            //lbl_NumRawPoints.Text = vp_Raw.Entities.Count.ToString();

            //update controls as necessary
            UpdateControls();
        }

        private void btn_ClearMagData_Click(object sender, EventArgs e)
        {
            tbox_RawMagData.Text = string.Empty;
            lbl_NumRtbLines.Content = "None";
            UpdateControls();
        }

        private void btn_Update3DView_Click(object sender, EventArgs e)
        {
            ////don't show message if the raw view is already empty
            //if (vp_Raw.Entities.Count > 3)
            //{
            //    DialogResult res = MessageBox.Show("This will clear existing points in raw 3D view.  Proceed?",
            //                                        "3D View Clear", MessageBoxButtons.YesNoCancel);
            //    if (res == System.Windows.Forms.DialogResult.Yes)
            //    {
            //        XfrTextPointsToRawView();

            //    }
            //}
            //else
            //{
            //    XfrTextPointsToRawView();
            //}
        }

        public Vector3D GetVector3DFromString(string linestr)
        {
            Vector3D pt = new Vector3D();

            try
            {
                string[] ptstrArray = linestr.Split(',');
                if (ptstrArray.Length == 3)
                {
                    pt.X = System.Convert.ToDouble(ptstrArray[0].Trim());
                    pt.Y = System.Convert.ToDouble(ptstrArray[1].Trim());
                    pt.Z = System.Convert.ToDouble(ptstrArray[2].Trim());
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Vector generation failed with message: " + e.Message);
            }

            return pt;
        }

        public void AddPointToRaw3DView(Vector3D v3d)
        {
            //devDept.Eyeshot.Standard.Point pt = new devDept.Eyeshot.Standard.Point(v3d, Color.Red);
            //pt.EntityData = v3d;//Point objects don't have position prop - so do it this way
            //vp_Raw.Entities.Add(pt);
        }

        private void btn_CommPortClose_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_RefreshPorts_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_BrowseOctave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_BrowseScript_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_Import_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_SaveAs_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_ClearMagData_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void btn_UpdateRawView_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_Compute_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
