using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;

using System.Windows.Controls;
using System.IO;
using System.Windows.Forms;

//05/31/16 experiment
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using Color = SharpDX.Color;
using HitTestResult = HelixToolkit.Wpf.SharpDX.HitTestResult;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

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
            //RichTextBox rtb = new RichTextBox();
            //rtb.IsReadOnly = true;
            //rtb.Focusable = false;
            //rtb.BorderThickness = new Thickness(0);
            //rtb.Background = Brushes.Transparent;
            //rtb.AppendText("Update Raw View");
            //btn_UpdateRawView.Content = rtb;
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

            //06/12/16 moved all point move code to function so can call from mi_UseSelRadius_Checked()
            MoveSelToSelPointsVisual();

            //if (selidxcount > 0)
            //{

            //    int newcount = 1;
            //    //06/06/16 new try at moving selected points from m_pointsVisual to selPointsVisual
            //    for (int selidx = selidxcount - 1; selidx >= 0; selidx--)//have to work from top down to avoid crashes
            //    {
            //        //retrieve selected point from pointsVisual collection
            //        int visptidx = m_selidxlist[selidx]; //this is the index into pointsVisual.Points collection
            //        Point3D selvispt = new Point3D();
            //        selvispt.X = m_pointsVisual.Points[visptidx].X;
            //        selvispt.Y = m_pointsVisual.Points[visptidx].Y;
            //        selvispt.Z = m_pointsVisual.Points[visptidx].Z;
            //        string selvisptstr = selvispt.X.ToString("F2") + ","
            //                        + selvispt.Y.ToString("F2") + ","
            //                        + selvispt.Z.ToString("F2");
            //        Debug.Print("selected pt (" + selvisptstr + ") added to selPointsVisual Points Collection. Count now "
            //            + newcount);

            //        //add this point to selPointsVisual
            //        selPointsVisual.Points.Add(selvispt);

            //        //testing - print out added point
            //        int addedcount = selPointsVisual.Points.Count;
            //        Point3D addedpt = new Point3D();
            //        addedpt = selPointsVisual.Points[addedcount - 1];
            //        string addedptstr = addedpt.X.ToString("F2") + ","
            //                        + addedpt.Y.ToString("F2") + ","
            //                        + addedpt.Z.ToString("F2");
            //        Debug.Print("added pt (" + addedptstr + ") added to selPointsVisual. Count now "
            //            + addedcount);

            //        //remove this point from m_pointsVisual collection
            //        Debug.Print("removing point at m_pointsVisual[" + m_selidxlist[selidx] + "]");
            //        m_pointsVisual.Points.RemoveAt(m_selidxlist[selidx]);
            //    }

            //    //DEBUG!!
            //    Debug.Print("\r\n selPointsVisual Points collection contains the following points");
            //    foreach (Point3D pt3 in selPointsVisual.Points)
            //    {
            //        string newptstr3 = pt3.X.ToString("F2") + ","
            //                        + pt3.Y.ToString("F2") + ","
            //                        + pt3.Z.ToString("F2");
            //        Debug.Print("Selected pt (" + newptstr3 + ")");
            //    }
            //}

            vp_raw.UpdateLayout();//refresh the 'raw' view
        }

        private void vp_cal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.Print("In vp_cal_MouseDown");
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
            VerifyOctaveExeFile(tbOctavePath.Text);
            VerifyOctaveScriptFiles(tbOctaveScriptFolder.Text);
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

            //Refresh Ports button
            btn_RefreshPorts.IsEnabled = !bCommPortOpen;

            //file/script locaton textbox colors
            tbOctaveScriptFolder.Background = (bOctaveScriptFileExists && bOctaveFunctionFileExists) ? Brushes.LightGreen : Brushes.LightPink;
            tbOctavePath.Background = (bOctaveExeFileExists) ? Brushes.LightGreen : Brushes.LightPink;

            //raw view stats labels
            lbl_AvgRadius.Content = GetRawAvgRadius().ToString("F2");
            lbl_NumPoints.Content = m_pointsVisual.Points.Count;
            lbl_SelPoints.Content = selPointsVisual.Points.Count;
        }

        //public void ProcessCommPortString(string linestr)
        //{
        //    //convert commport line into Vector3D object
        //    Vector3D v3d = GetVector3DFromString(linestr);

        //    //add the line to the rich text box
        //    AddLineToRawDataView(System.Drawing.Color.Black, linestr);

        //    //add the point to the viewport Entity list
        //    AddPointToRaw3DView(v3d);

        //    //refresh the view
        //    //vp_Raw.Refresh();
        //    //vp_Raw.ZoomFit();
        //    //lbl_NumRawPoints.Text = vp_Raw.Entities.Count.ToString();

        //    //update controls as necessary
        //    UpdateControls();
        //}

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
            //    DialogResult res = System.Windows.MessageBox.Show("This will clear existing points in raw 3D view.  Proceed?",
            //                                        "3D View Clear", System.Windows.MessageBoxButton.YesNoCancel);
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
                //System.Windows.MessageBox.Show("Vector generation failed with message: " + e.Message);
                throw;
            }

            return pt;
        }

        private Point3D GetPoint3DFromString(string linestr)
        {
            Point3D pt = new Point3D();

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
                //System.Windows.MessageBox.Show("Vector generation failed with message: " + e.Message);
                throw;
            }

            return pt;
        }


        private void btn_CommPortOpen_Click(object sender, RoutedEventArgs e)
        {
            //be a little more clever about clearing the raw data text window
            //don't clear it if there was a previous open/close sesssion
            if (!tbox_RawMagData.Text.Contains("Closed"))
            {
                tbox_RawMagData.Text = string.Empty;
            }

            commMgr.PortName = cbox_CommPort.Text;
            commMgr.BaudRate = cbox_BaudRate.Text;
            try
            {
                bCommPortOpen = commMgr.OpenPort();
                UpdateControls();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Comm Port Open() Failed with message: " + ex.Message);
            }

        }

        private void btn_CommPortClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.Print("In btn_CommPortClose_Click before call to ClosePort()");
                bCommPortOpen = !commMgr.ClosePort();
                Debug.Print("In btn_CommPortClose_Click after call to ClosePort()");
                tbox_RawMagData.ScrollToEnd(); //needed to display 'Port Closed' line
                UpdateControls();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Comm Port Close() Failed with message: " + ex.Message);
            }
        }

        private void btn_RefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            LoadValues();
            cbox_CommPort.SelectedIndex = 0;
        }

        private void btn_BrowseOctave_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            CommonOpenFileDialog openFileDialog1 = new CommonOpenFileDialog();
            openFileDialog1.Title = "Location of Octave CLI Executable";
            openFileDialog1.IsFolderPicker = false;

            // Set filter options and filter index.
            openFileDialog1.Filters.Add(new CommonFileDialogFilter("Programs", "*.exe"));
            openFileDialog1.DefaultExtension = ".exe";
            openFileDialog1.InitialDirectory = Path.GetDirectoryName(tbOctavePath.Text);

            // Call the ShowDialog method to show the dialog box.
            CommonFileDialogResult userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == CommonFileDialogResult.Ok)
            {
                tbOctavePath.Text = openFileDialog1.FileName;
                VerifyOctaveExeFile(tbOctavePath.Text);
                Properties.Settings.Default.OctaveExePath = tbOctavePath.Text;
            }
            UpdateControls();
        }

        private void btn_BrowseScript_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Choose the folder containing the '" + OctaveScriptFile;
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = Environment.CurrentDirectory;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = Environment.CurrentDirectory;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = dlg.FileName;
                tbOctaveScriptFolder.Text = folder;
                VerifyOctaveScriptFiles(tbOctaveScriptFolder.Text);
            }

            UpdateControls(); //updates textbox background color
        }

        private void btn_Import_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Set filter options and filter index.
            openFileDialog1.Filter = "Text Format|*.txt;*.csv|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.InitialDirectory = Properties.Settings.Default.MagDataFolder;

            // Call the ShowDialog method to show the dialog box.
            DialogResult userClickedOK = openFileDialog1.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == System.Windows.Forms.DialogResult.OK)
            {
                string datafilename = openFileDialog1.FileName;
                Properties.Settings.Default.MagDataFolder = Path.GetFileName(Path.GetDirectoryName(datafilename));
                tbox_RawMagData.Text = string.Empty;
                StreamReader sr = new StreamReader(datafilename);

                using (new WaitCursor())
                {
                    try
                    {
                        //accumulate lines into local string; MUCH faster than updating textbox
                        //typical 2000 line dataset takes < 1sec without updates, > 10 sec with
                        int numlines = 0;
                        string datastr = string.Empty;

                        while (!sr.EndOfStream)
                        {
                            numlines++;
                            datastr += sr.ReadLine().Trim() + Environment.NewLine;
                        }

                        //update controls at end - MUCH faster!
                        lbl_NumRtbLines.Content = numlines.ToString();
                        tbox_RawMagData.Text = datastr;
                        UpdateControls();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Mag data import from " + datafilename + " Failed with Message: " + ex.Message);
                    }
                }
            }
        }

        private void btn_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            // Create a SaveFileDialog to request a path and file name to save to.
            SaveFileDialog saveFile1 = new SaveFileDialog();

            // Initialize the SaveFileDialog to specify the TXT extension for the file.
            saveFile1.DefaultExt = "*.txt";
            saveFile1.Filter = "Text Files|*.txt";

            // Determine if the user selected a file name from the saveFileDialog.
            if (saveFile1.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
               saveFile1.FileName.Length > 0)
            {
                try
                {
                    string str = tbox_RawMagData.Text;
                    StringReader sr = new StringReader(str);
                    StreamWriter sw = new StreamWriter(saveFile1.FileName);
                    while (sr.Peek() >= 0) //peek() returns -1 if nothing avail
                    {
                        string linestr = sr.ReadLine();
                        if (!linestr.Contains("Port"))
                        {
                            sw.WriteLine(linestr);
                        }
                    }

                    Properties.Settings.Default.MagDataFolder = Path.GetDirectoryName(saveFile1.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Mag data save to " + saveFile1.FileName + " Failed with Message: " + ex.Message);
                }
            }
        }

        private void btn_ClearMagData_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void btn_UpdateRawView_Click(object sender, RoutedEventArgs e)
        {
            //Purpose: transfer contents of raw magnetometer data text window to 'raw' 3D viewport
            //Plan:
            //  Step1: For each line in text view
            //      convert text to 3D point if possible (might fail)
            //      add point to vp_raw Point collecton
            //  Step2: Refresh vp_raw viewport

            //Step1: clear vp_raw contents
            selPointsVisual.Points.Clear();
            m_pointsVisual.Points.Clear();
            vp_raw.UpdateLayout();

            //Step2: Add all points to raw 3D view
            StringReader sr = new StringReader(tbox_RawMagData.Text);
            string linestr = string.Empty;
            int linenum = 1;
            int errnum = 0;
            Point3D pt3d = new Point3D();

            using (new WaitCursor())
            {
                //convert each line in text view to 3D point if possible, and add to m_pointsVisual.Points collection
                while (sr.Peek() >= 0) //Peek() returns -1 at end
                {
                    //convert text to 3D point if possible (might fail)
                    try
                    {
                        linestr = sr.ReadLine();
                        pt3d = GetPoint3DFromString(linestr);
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("GetVector3DFromString() Failed on line " + linenum + ": " + linestr + ": " + ex.Message);
                        errnum++;
                    }
                    linenum++;

                    //add point to vp_raw Point collecton
                    m_pointsVisual.Points.Add(pt3d);
                }

                //Step2: Refresh vp_raw viewport
                Debug.Print("processed " + linenum + " lines with " + errnum + " errors");
                vp_raw.UpdateLayout();

                //Step3: Update Numpts and average radius labels
                UpdateControls();
            }
        }

        private void btn_Compute_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBox_Click(object sender, RoutedEventArgs e)
        {

        }

        //public void AddLineToRawDataView(System.Drawing.Color txtcolor, string portstr)
        //{
        //    tbox_RawMagData.AppendText(portstr);
        //}

        public void AddPointToRaw3DView(Vector3D v3d)
        {
            //devDept.Eyeshot.Standard.Point pt = new devDept.Eyeshot.Standard.Point(v3d, Color.Red);
            //pt.EntityData = v3d;//Point objects don't have position prop - so do it this way
            //vp_Raw.Entities.Add(pt);
        }

        private void VerifyOctaveExeFile(string exefilename)
        {
            string prestr = "I can't seem to find the ";
            string poststr = " at this location - and calibration can't proceed without it!";
            FileInfo fi = new FileInfo(exefilename);
            if (fi.Exists)
            {
                bOctaveExeFileExists = true;
            }
            else
            {
                bOctaveExeFileExists = false;
                System.Windows.MessageBox.Show(prestr + "'Octave executable" + poststr,
                    "Octave Executable Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
            }
        }

        private void VerifyOctaveScriptFiles(string foldername)
        {
            //see if the files are really there
            string prestr = "I can't seem to find the ";
            string poststr = " in this folder - and calibration can't proceed without it!";
            FileInfo fi = new FileInfo(foldername + "\\" + OctaveScriptFile);
            if (fi.Exists)
            {
                bOctaveScriptFileExists = true;

                FileInfo fi2 = new FileInfo(foldername + "\\" + OctaveFunctionFile);
                if (fi2.Exists)
                {
                    //OK, both the script and function files exist in this folder - good to go
                    tbOctaveScriptFolder.Background = Brushes.LightGreen;
                    bOctaveFunctionFileExists = true;
                }
                else
                {
                    bOctaveFunctionFileExists = false;
                    System.Windows.MessageBox.Show(prestr + "'" + OctaveFunctionFile + "' Octave function" + poststr,
                        "Octave Function Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                bOctaveScriptFileExists = false;
                System.Windows.MessageBox.Show(prestr + "'" + OctaveScriptFile + "' Octave script" + poststr,
                    "Octave Script Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

        }

        private void ExecuteOctaveScript(string rawdatafilepath, out double[][] A, out double[] c, out double[][] Caldata)
        {
            string cmdstr = "";
            A = new double[3][]; //initial required by use of 'out' parameter & try/catch blocks
            Caldata = new double[3][]; //initial required by use of 'out' parameter & try/catch blocks
            c = new double[3]; //initial required by use of 'out' parameter & try/catch blocks

            //gfp's new code
            string OctaveScriptFolderName = tbOctaveScriptFolder.Text.Replace(@"\", "/");
            string OctaveRawDataFilePath = rawdatafilepath.Replace(@"\", "/");

            ////display current working directory (pwd)
            //textBox1.Text += octave.GetString("pwd"); //this also executes the command

            //prepend script/function path to Octave search path
            int slashidx = tbOctaveScriptFolder.Text.LastIndexOf("\\");
            cmdstr = "addpath (" + "'" + OctaveScriptFolderName + "'" + ")";

            try
            {
                octave.ExecuteCommand(cmdstr);

                ////read it back out for debug purposes
                //displaystr = octave.GetString("path");//this also executes the command
                //System.Windows.MessageBox.Show(displaystr);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Addpath Command Failed with Message: " + e.Message);
            }
            //octave.ExecuteCommand(cmdstr);

            //load datafile
            cmdstr = "S = load (" + quote + "-ascii" + quote + ", " + quote
                + OctaveRawDataFilePath + quote + ");";

            try
            {
                octave.ExecuteCommand(cmdstr);
                //displaystr = DisplayMatrixSample("S");
                double[][] m = octave.GetMatrix("S");
                //displaystr = DisplayMatrixSample("S",m);
                //System.Windows.MessageBox.Show(displaystr);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Load Command Failed with Message: " + e.Message);
            }

            //execute script file - the script calls a function in the same folder
            try
            {
                cmdstr = "source(" + quote + OctaveScriptFolderName + "/" + OctaveScriptFile + quote + ")";
                octave.ExecuteCommand(cmdstr);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Source Command Failed with Message: " + e.Message);
            }

            //display function return results.  
            try
            {
                //'A' matrix and 'c' vector
                A = octave.GetMatrix("A");
                c = octave.GetVector("c");
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("[A,c] Results display Failed with Message: " + e.Message);
            }

            //generate/display calibrated data sample
            try
            {
                Caldata = octave.GetMatrix("Caldata'");
                //displaystr = DisplayMatrixSample("Caldata'", Caldata);
                //System.Windows.MessageBox.Show(displaystr);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Caldata' Results display Failed with Message: " + e.Message);
            }
            //Oops - these aren't returned (yet) from MagnCalibration.m
            //try
            //{
            //    displaystr = DisplayMatrixSample("Caldata'");
            //    displaystr += DisplayVectorSample("b");
            //    displaystr += DisplayMatrixSample("D");
            //    displaystr += DisplayMatrixSample("U");
            //    displaystr += DisplayMatrixSample("V");
            //    displaystr += DisplayMatrixSample("p");
            //    System.Windows.MessageBox.Show(displaystr);
            //}
            //catch (Exception e)
            //{

            //    throw;
            //}
        }

        //05/13/16 - matrix acquisition abstracted to calling fcn
        //private string DisplayMatrixSample(string M)
        private string DisplayMatrixSample(string M, double[][] m)
        {
            //double[][] m = octave.GetMatrix(M);
            string str = "Size of " + M + " is " + m.Length + "x" + m[0].Length + "\r\n";

            if (m.Length > 10)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (m[0].Length > 10)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            str += m[i][j].ToString("0.000") + "\t";
                        }
                    }
                    else
                    {
                        for (int j = 0; j < m[0].Length; j++)
                        {
                            str += m[i][j].ToString("0.000") + "\t";
                        }
                    }
                    str += "\r\n";
                }
            }
            else
            {
                for (int i = 0; i < m.Length; i++)
                {
                    if (m[0].Length > 10)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            str += m[i][j].ToString("0.000") + "\t";
                        }
                    }
                    else
                    {
                        for (int j = 0; j < m[0].Length; j++)
                        {
                            str += m[i][j].ToString("0.000") + "\t";
                        }
                    }
                    str += "\r\n";
                }
            }

            str += "\r\n";
            return str;
        }

        //05/13/16 abstracted vector acquisition to calling fcn
        private string DisplayVectorSample(string v, double[] c)
        {
            //double[] c = octave.GetVector(v);
            string str = "size of " + v + " = " + c.Length.ToString() + Environment.NewLine;
            str += v + " = ";
            for (int i = 0; i < c.Length; i++)
            {
                str += c[i].ToString() + " ";
            }
            str += "\r\n\r\n";
            return str;
        }

        private void UpdateCalViewport()
        {
            //vp_Calibrated.Entities.Clear();
            //vp_Calibrated.Refresh();

            Vector3D pt3D = new Vector3D(0, 0, 0);
            for (int i = 0; i < Caldata.Length; i++)
            {
                pt3D.X = Caldata[i][0];
                pt3D.Y = Caldata[i][1];
                pt3D.Z = Caldata[i][2];
                //devDept.Eyeshot.Standard.Point pt = new devDept.Eyeshot.Standard.Point(pt3D, Color.Black);
                //vp_Calibrated.Entities.Add(pt);
            }

            //draw unit radius reference circles if selected
            if (chk_RefCircles.IsChecked.Value)
            {
                Point3D cenpt = new Point3D(0, 0, 0);
                //Ellipse ell = new Ellipse(Plane.XY, cenpt, 1, 1, Color.Red);
                //vp_Calibrated.Entities.Add(ell);
                //ell = new Ellipse(Plane.YZ, cenpt, 1, 1, Color.Green);
                //vp_Calibrated.Entities.Add(ell);
                //ell = new Ellipse(Plane.ZX, cenpt, 1, 1, Color.Blue);
                //vp_Calibrated.Entities.Add(ell);
            }

            //vp_Calibrated.Refresh();
            //vp_Calibrated.ZoomFit();

        }

        //see http://stackoverflow.com/questions/4502037/where-is-the-application-doevents-in-wpf
        public void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            //Dispatcher.BeginInvoke(DispatcherPriority.Background,
            //    new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        public object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;

            return null;
        }

        private void frmMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.OctaveExePath = tbOctavePath.Text;
            Properties.Settings.Default.OctaveScriptFolder = tbOctaveScriptFolder.Text;

            //actually save to config file
            Properties.Settings.Default.Save();
        }

        private void RawOptionsMenu_Drop(object sender, System.Windows.DragEventArgs e)
        {
            System.Windows.Controls.Menu menu = (System.Windows.Controls.Menu)sender;
            int itemnum = 0;
            foreach (System.Windows.Controls.MenuItem mi in menu.Items)
            {
                string header = mi.Header.ToString();
                Debug.Print("Menu Item " + itemnum + " Header = " + header);
                itemnum++;
            }

        }

        private void mi_Options_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Debug.Print("mi_Options_ContextMenuOpening");
        }

        private void mi_Options_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("mi_Options_Click");
        }

        private void mi_Options_LayoutUpdated(object sender, EventArgs e)
        {
            //Debug.Print("mi_Options_LayoutUpdated");
        }

        private void mi_Options_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem mi = (System.Windows.Controls.MenuItem)sender;
            Debug.Print("mi_Options_SubmenuOpened: Item name = " + mi.Name.ToString());
            mi_SelRadius.IsEnabled = mi_UseSelRadius.IsChecked;
            btn_RemSel.IsEnabled = mi_UseSelRadius.IsChecked;
        }

        //private void USR_Radius_Click(object sender, RoutedEventArgs e)
        //{
        //    System.Windows.Controls.MenuItem mi = (System.Windows.Controls.MenuItem)sender;
        //    Debug.Print("USR_Radius_Click: Item name = " + mi.Name.ToString());
        //    Debug.Print("USR_Radius Checked State = " + mi.IsChecked.ToString());
        //}

        private void mi_SelRadius_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem mi = (System.Windows.Controls.MenuItem)sender;
            Debug.Print("mi_SelRadius_Click: Item name = " + mi.Name.ToString());
        }

        private void mi_SelRadius_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.MenuItem mi = (System.Windows.Controls.MenuItem)sender;
            Debug.Print("mi_SelRadius_SubmenuOpened: Item name = " + mi.Name.ToString());
        }

        private void btn_RemSel_Click(object sender, RoutedEventArgs e)
        {
            //Purpose: Remove all selected objects from m_pointsVisual.Points collection
            //Plan:  All selected points should be in selPointsVisual.Points collection, so can just clear
            //      this collection and update 

            selPointsVisual.Points.Clear();
            UpdateControls();
        }

        private double GetRawAvgRadius()
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

        private void mi_UseSelRadius_Checked(object sender, RoutedEventArgs e)
        {
            Debug.Print("in mi_UseSelRadius_Checked");

            //Purpose: Select all points beyond the radius specified in tb_SelRadius
            try
            {
                double selradius = Convert.ToDouble(tb_SelRadius.Text);//this could fail

                //Step3: copy pointsVisual.Points index of any selected points to m_selidxlist
                m_selidxlist.Clear();
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

                //move selected points from pointsVisual3D to selpointsVisual3D
                int selidxcount = m_selidxlist.Count;
                Debug.Print("Selected Index List Contains " + selidxcount + " items");
                MoveSelToSelPointsVisual();
                vp_raw.UpdateLayout();
            }
            catch (Exception)
            {

                throw;
            }
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
    }
}
