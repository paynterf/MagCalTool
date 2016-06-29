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

            //selPointsVisual = new PointsVisual3D { Color = Colors.Yellow, Size = 2 * POINTSIZE };
            selPointsVisual = new PointsVisual3D { Color = Colors.Yellow, Size = 1.5 * POINTSIZE };
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
            //EllipsoidVisual3D ell1 = new EllipsoidVisual3D();
            //MyEllpsoidVisual ell1 = new MyEllpsoidVisual();
            //SolidColorBrush perimeterbrush = new SolidColorBrush(Colors.Red);
            //System.Windows.Media.Media3D.Material mat = MaterialHelper.CreateMaterial(new SolidColorBrush(Colors.Red));
            //ell1.Material = mat;
            //ell1.BackMaterial = mat;
            //ell1.Fill = perimeterbrush;
            //ell1.Center = new Point3D(0, 0, 0);
            //ell1.RadiusX = 1;
            //ell1.RadiusZ = 1;
            //ell1.RadiusY = 1;
            //ell1.Model.BackMaterial = mat;
            //ell1.Model.Material = mat;
            //ell1.Fill = new SolidColorBrush(Colors.White);

            //vp_cal.Children.Add(ell1);
            //vp_cal.UpdateLayout();

            //06/08/16 copied from frmMagManager.cs
            //Load file/folder boxes with default contents from config file
            tbOctavePath.Text = Properties.Settings.Default.OctaveExePath;
            tbOctaveScriptFolder.Text = Properties.Settings.Default.OctaveScriptFolder;

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
            vp_raw.UpdateLayout();//refresh the 'raw' view
            UpdateControls();
        }

        private void vp_cal_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.Print("In vp_cal_MouseDown");
        }

        private void frmMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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

            //Refresh Ports button
            btn_RefreshPorts.IsEnabled = !bCommPortOpen;

            //file/script locaton textbox colors
            tbOctaveScriptFolder.Background = (bOctaveScriptFileExists && bOctaveFunctionFileExists) ? Brushes.LightGreen : Brushes.LightPink;
            tbOctavePath.Background = (bOctaveExeFileExists) ? Brushes.LightGreen : Brushes.LightPink;

            //text view stats labels
            //tbox_RawMagData.Text = tbox_RawMagData.Text.Trim();
            lbl_NumRtbLines.Content = (tbox_RawMagData.Text.Length > 0) ? tbox_RawMagData.LineCount.ToString() : "0";

            //raw view stats labels
            lbl_AvgRadius.Content = GetRawAvgRadius().ToString("F2");
            lbl_NumPoints.Content = m_pointsVisual.Points.Count;
            lbl_SelPoints.Content = selPointsVisual.Points.Count;

            //Compute button
            btn_Compute.IsEnabled = m_pointsVisual.Points.Count >= MIN_COMP_POINTS
                && bOctaveFunctionFileExists && bOctaveScriptFileExists;

            //Compensation value save button added 06 / 29 / 16
            string compvalstr = lbl_U11.Content.ToString();
            btn_SaveCompVals.IsEnabled = !compvalstr.Contains("U11");
        }

        private void btn_ClearMagData_Click(object sender, EventArgs e)
        {
            tbox_RawMagData.Text = string.Empty;
            UpdateControls();
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
            //06/18/16 rev to accommodate 1,2 & 3 element data
            Point3D pt = new Point3D();

            try
            {
                //string[] ptstrArray = linestr.Split(new char[]{ ',','\t',' '});
                //if (ptstrArray.Length == 3)
                //{
                //    pt.X = System.Convert.ToDouble(ptstrArray[0].Trim());
                //    pt.Y = System.Convert.ToDouble(ptstrArray[1].Trim());
                //    pt.Z = System.Convert.ToDouble(ptstrArray[2].Trim());
                //}

                //06/18/16 rev to accommodate 1,2,3 element data
                string[] ptstrArray = linestr.Split(new char[] { ',', '\t', ' ' });
                switch (ptstrArray.Length)
                {
                    case 1:
                        pt.X = System.Convert.ToDouble(ptstrArray[0].Trim());
                        pt.Y = 0;
                        pt.Z = 0;
                        break;
                    case 2:
                        pt.X = System.Convert.ToDouble(ptstrArray[0].Trim());
                        pt.Y = System.Convert.ToDouble(ptstrArray[1].Trim());
                        pt.Z = 0;
                        break;
                    case 3:
                        pt.X = System.Convert.ToDouble(ptstrArray[0].Trim());
                        pt.Y = System.Convert.ToDouble(ptstrArray[1].Trim());
                        pt.Z = System.Convert.ToDouble(ptstrArray[2].Trim());
                        break;
                    default:
                        pt.X = 0;
                        pt.Y = 0;
                        pt.Z = 0;
                        break;
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
            //int linenum = 1;
            int linenum = 0;
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
                        if (linestr.Trim().Length > 0)
                        {
                            linenum++;
                            pt3d = GetPoint3DFromString(linestr);

                            //add point to vp_raw Point collecton
                            m_pointsVisual.Points.Add(pt3d);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print("GetVector3DFromString() Failed on line " + linenum + ": " + linestr + ": " + ex.Message);
                        errnum++;
                    }
                    //linenum++;

                    ////add point to vp_raw Point collecton
                    //m_pointsVisual.Points.Add(pt3d);
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
            DialogResult res = System.Windows.Forms.MessageBox.Show("This will compute the best estimate calibration values"
                + " based on the current contents of the 'raw' viewport"
                + " and display the calibrated results in the 'Calibrated' viewport."
                + "  This can take a while - Proceed?", "Magnetometer Data Calibration",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);

            if (res != System.Windows.Forms.DialogResult.Yes)
            {
                return;
            }
            //clear the calibrated display so will be easier to tell when refreshed
            //has to be above WaitCursor() call, as DoEvents() resets cursor to default
            m_CalpointsVisual.Points.Clear();
            vp_cal.UpdateLayout();
            DoEvents();

            using (new WaitCursor())
            {

                try
                {
                    octave = new Octave(tbOctavePath.Text, false);
                }
                catch (Exception err)
                {
                    System.Windows.Forms.MessageBox.Show("Octave 'new' command failed with message: " + err.Message);
                    return;
                }

                //if we get to here, we have a valid octave object
                //load points from 'raw' viewport into 'rawpts.txt' for use by Octave
                string fullPathToRawDatafile = tbOctaveScriptFolder.Text + ".\\" + OctaveRawDataFile;

                try
                {
                    //int numpts = vp_Raw.Entities.Count;

                    //save raw points to temp file
                    StreamWriter sw = new StreamWriter(fullPathToRawDatafile);
                    foreach (Point3D pt3d in m_pointsVisual.Points)
                    {
                        sw.WriteLine(pt3d.ToString());
                    }
                    sw.Close();


                    //execute the script
                    double[][] A;
                    double[] c;
                    ExecuteOctaveScript(fullPathToRawDatafile, out A, out c, out Caldata); //actually execute the script

                    //display results
                    //05/13/16 - update 'U' labels
                    lbl_U11.Content = A[0][0].ToString();
                    lbl_U12.Content = A[0][1].ToString();
                    lbl_U13.Content = A[0][2].ToString();

                    lbl_U21.Content = A[1][0].ToString();
                    lbl_U22.Content = A[1][1].ToString();
                    lbl_U23.Content = A[1][2].ToString();

                    lbl_U31.Content = A[2][0].ToString();
                    lbl_U32.Content = A[2][1].ToString();
                    lbl_U33.Content = A[2][2].ToString();

                    ////05/13/16 - update 'C' labels
                    lbl_Cx.Content = c[0].ToString();
                    lbl_Cy.Content = c[1].ToString();
                    lbl_Cz.Content = c[2].ToString();

                    //plot Caldata in 'Calibrated' view
                    UpdateCalViewport();
                    vp_cal.ZoomExtents();
                    UpdateControls(); //added 06/29/16 to refresh 'SaveCompVals...' button state
                }
                catch (Exception err)
                {
                    System.Windows.Forms.MessageBox.Show("Failed to write raw data to " + fullPathToRawDatafile + ": " + err.Message);
                    return;
                }
            }
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
            m_CalpointsVisual.Points.Clear();
            Vector3D pt3D = new Vector3D(0, 0, 0);
            for (int i = 0; i < Caldata.Length; i++)
            {
                pt3D.X = Caldata[i][0];
                pt3D.Y = Caldata[i][1];
                pt3D.Z = Caldata[i][2];
                m_CalpointsVisual.Points.Add(new Point3D(pt3D.X, pt3D.Y, pt3D.Z));
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

            vp_cal.UpdateLayout();
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

        private void btn_SaveRawPtsToFile_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("In btn_SaveRawPtsToFile_Click");

            //Purpose: Save current contents of raw view to a file - preserving culling op
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Save All Currently Displayed Raw Magnetometer Points";
            dlg.IsFolderPicker = false;
            //dlg.InitialDirectory = Environment.CurrentDirectory;
            dlg.InitialDirectory = tbOctaveScriptFolder.Text;
            dlg.DefaultExtension = ".txt";
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = Environment.CurrentDirectory;
            dlg.EnsureFileExists = false;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var savefile = dlg.FileName;
                Debug.Print("Save file is: " + savefile);

                //save all point in m_pointsVisual to the file
                int ptssaved = 0;
                try
                {
                    StreamWriter sw = new StreamWriter(savefile);
                    foreach (Point3D p3d in m_pointsVisual.Points)
                    {
                        sw.WriteLine(p3d.ToString());
                        ptssaved++;
                    }

                    Debug.Print("Saved " + ptssaved + " points to " + savefile);
                    sw.Close();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            UpdateControls(); //updates textbox background color
        }

        private void btn_SelectBeyondRadius_Click_1(object sender, RoutedEventArgs e)
        {
            //Purpose: Select all points beyond the radius specified in tb_SelRadius
            //Plan:
            //  Step1: Clear list of selected indices, and all points from selPointsVisual.Points list
            //  Step2: Iterate through all m_pointsVisual points & copy index of any qualifying points to m_selidxlist
            //  Step3: For each index in m_selidxlist, move assoc pt from m_pointsVisaul to selPointsVisual
            //  Step4: Update 'selected' count label

            try
            {
                double selradius = Convert.ToDouble(tb_SelRadius.Text);//this could fail

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
                vp_raw.UpdateLayout();
                UpdateControls();
            }
            catch (Exception)
            {
                throw;
            }
        }

        //this is called from CommPortMgr.cs
        public void ProcessCommPortString(string linestr)
        {
            //Debug.Print("In ProcessCommPortString with linestr = " + linestr);
            try
            {
                tbox_RawMagData.Text += linestr;
                lbl_NumRtbLines.Content = tbox_RawMagData.LineCount;
                tbox_RawMagData.ScrollToEnd();

                //add point to raw viewport

                try
                {
                    Point3D pt3d = new Point3D();
                    pt3d = GetPoint3DFromString(linestr);
                    //Debug.Print("GetPoint3DFromString(" + linestr + ") returned " + pt3d.ToString());
                    m_pointsVisual.Points.Add(pt3d);
                    vp_raw.UpdateLayout();
                }
                catch (Exception)
                {
                    Debug.Print("Failed to convert " + linestr + " to Point3D object");
                }

                UpdateControls();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        //called by 'Zoom Extents' raw view option button
        private void btn_ZoomExtents_Click(object sender, RoutedEventArgs e)
        {
            vp_raw.ZoomExtents();
        }

        private void btn_SaveCompVals_Click(object sender, RoutedEventArgs e)
        {
            Debug.Print("In btn_SaveCompVals_Click");

            //Purpose: Save current compensation values to a text file
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Save Currently Displayed Compensation Values";
            dlg.IsFolderPicker = false;
            dlg.InitialDirectory = Environment.CurrentDirectory;
            //dlg.InitialDirectory = tbOctaveScriptFolder.Text;
            dlg.DefaultExtension = ".txt";
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = Environment.CurrentDirectory;
            dlg.EnsureFileExists = false;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var savefile = dlg.FileName;
                Debug.Print("Save file is: " + savefile);

                //save all non-zero compensation values
                try
                {
                    StreamWriter sw = new StreamWriter(savefile);

                    //write out date and time
                    sw.WriteLine("Magnetometer Compensation Values Saved " + DateTime.Now.ToString("F"));
                    sw.WriteLine(); //blank line

                    //compensation matrix
                    sw.WriteLine("Compensation Matrix");

                    //1st row
                    sw.WriteLine("U11: " + lbl_U11.Content);
                    sw.WriteLine("U12: " + lbl_U12.Content);
                    sw.WriteLine("U13: " + lbl_U13.Content);

                    //2nd row - only U22 & U23 are non-zero
                    sw.WriteLine("U22: " + lbl_U22.Content);
                    sw.WriteLine("U23: " + lbl_U23.Content);

                    //3rd row - only U33 non-zero
                    sw.WriteLine("U33: " + lbl_U33.Content);
                    sw.WriteLine(); //blank line

                    //Center offset
                    sw.WriteLine("Center Offset");

                    sw.WriteLine("Cx: " + lbl_Cx.Content);
                    sw.WriteLine("Cx: " + lbl_Cy.Content);
                    sw.WriteLine("Cx: " + lbl_Cz.Content);

                    Debug.Print("Saved Compensation values to " + savefile);
                    sw.Close();
                }
                catch (Exception)
                {
                    throw;
                }
            }

            UpdateControls(); //updates textbox background color
        }
    }
}

