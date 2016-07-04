// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MagViewerGeometryModel.cs" company="Helix Toolkit">
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
    public class RawViewModel:ViewportGeometryModel
    {
        private PointsVisual3D m_selpointsVisual;//contains selected points from m_pointsVisual
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
                return m_selpointsVisual;
            }
        }

        public int SelPointCount
        {
            get
            {
                return m_selpointsVisual.Points.Count;
            }
        }

        public int RawPointCount
        {
            get
            {
                return m_pointsVisual.Points.Count;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawViewModel"/> class.
        /// </summary>
        public RawViewModel(HelixViewport3D view, MainWindow main):base(view, main)
        {
            m_pointsVisual.SetName("magpoints");

            m_selpointsVisual = new PointsVisual3D { Color = Colors.Yellow, Size = 1.5 * POINTSIZE };
            m_selpointsVisual.SetName("selpoints");
            view3d.Children.Add(m_selpointsVisual);

        }

        public void Rawview_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Windows.Point mousept = e.GetPosition(view3d);
            Debug.Print("At top of MouseDown event, Mouse position " + mousept.ToString());

            //code to print out current selected point list
            if (m_selpointsVisual != null)
            {
                int selcount = m_selpointsVisual.Points.Count;
                for (int i = 0; i < selcount; i++)
                {
                    Point3D selpt = m_selpointsVisual.Points[i];
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
                if (m_selpointsVisual != null)
                {
                    int selcount = m_selpointsVisual.Points.Count;
                    int magptcount = m_pointsVisual.Points.Count;
                    Debug.Print("selcount = " + selcount + ", magptcount = " + magptcount);
                    if (selcount > 0)
                    {
                        for (int i = 0; i < selcount; i++)
                        {
                            //get selected point 
                            Point3D selpt = m_selpointsVisual.Points[i];
                            string selptstr = selpt.X.ToString("F2") + ","
                                            + selpt.Y.ToString("F2") + ","
                                            + selpt.Z.ToString("F2");

                            Debug.Print("Moving selected pt (" + selptstr + ") to pointsVisual.  Before add, pt count is "
                                + m_pointsVisual.Points.Count);

                            //add it to pointsVisual
                            m_pointsVisual.Points.Add(m_selpointsVisual.Points[i]);
                            int newcount = m_pointsVisual.Points.Count;

                            //check that point got added properly
                            Point3D newpt = m_pointsVisual.Points[newcount - 1];
                            string newptstr = newpt.X.ToString("F2") + ","
                                            + newpt.Y.ToString("F2") + ","
                                            + newpt.Z.ToString("F2");

                            Debug.Print("point (" + newptstr + ") added to pointsVsiual at index " + (newcount - 1)
                                + ". Count now " + newcount);
                        }
                        m_selpointsVisual.Points.Clear();
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
                    m_selpointsVisual.Points.Add(selvispt);

                    //testing - print out added point
                    int addedcount = m_selpointsVisual.Points.Count;
                    Point3D addedpt = new Point3D();
                    addedpt = m_selpointsVisual.Points[addedcount - 1];
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
                m_selpointsVisual.Points.Clear(); //added 06/21/16

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