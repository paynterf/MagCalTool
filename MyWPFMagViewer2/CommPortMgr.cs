using System;
using System.Text;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using System.Diagnostics;

namespace MyWPFMagViewer2
{
    class CommPortManager
    {

        #region Manager Variables
        //property variables
        private string _baudRate = string.Empty;
        private string _portName = string.Empty;
        //private RichTextBox _displayWindow;

        //global manager variables
        private Color[] MessageColor = { Color.Blue, Color.Green, Color.Black, Color.Orange, Color.Red };
        private SerialPort comPort = new SerialPort();

        //added 03/29/16 for magnetometer support
        private string _linestr = string.Empty;
        //private Point3D[] dotArray = new Point3D[100];
        //private ViewportProfessional _viewport;
        private MainWindow _frmMgr;

        #endregion

        #region Manager Properties
        /// <summary>
        /// Property to hold the BaudRate
        /// of our manager class
        /// </summary>
        public string BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; }
        }

        /// <summary>
        /// property to hold the PortName
        /// of our manager class
        /// </summary>
        public string PortName
        {
            get { return _portName; }
            set { _portName = value; }
        }

        public MainWindow DisplayForm
        {
            get { return _frmMgr; }
            set { _frmMgr = (MainWindow)value; }
        }
        #endregion

        #region Manager Constructors
        //public CommPortManager(string baud, string par, string sBits, string dBits, 
        //       string name, RichTextBox rtb, ViewportProfessional viewport)
        //public CommPortManager(string baud, string par, string sBits, string dBits,
        //       string name, frmMagManager form)
        public CommPortManager(string baud, string par, string sBits, string dBits,
               string name, MainWindow form)
        {
            _baudRate = baud;
            _portName = name;
            //_displayWindow = rtb;
            //_viewport = viewport; //03/29/16 added for access to eyeshot 3D viewport
            _frmMgr = form;

            //now add an event handler
            comPort.DataReceived += new SerialDataReceivedEventHandler(comPort_DataReceived);
        }

        /// <summary>
        /// Comstructor to set the properties of our
        /// serial port communicator to nothing
        /// </summary>
        public CommPortManager()
        {
            _baudRate = string.Empty;
            _portName = "COM1";
            //_displayWindow = null;
            //_viewport = null;//03/29/16 added for access to eyeshot 3D viewport
            _frmMgr = null;

            //add event handler
            comPort.DataReceived += new SerialDataReceivedEventHandler(comPort_DataReceived);
        }
        #endregion

        #region Port Open/Close
        public bool OpenPort()
        {
            try
            {
                //first check if the port is already open
                //if its open then close it
                if (comPort.IsOpen == true) comPort.Close();

                //set the properties of our SerialPort Object
                comPort.BaudRate = int.Parse(_baudRate);    //BaudRate
                comPort.PortName = _portName;   //PortName

                //now open the port
                comPort.Open();

                //display message
                _frmMgr.AddLineToRawDataView(System.Drawing.Color.Black, "Port opened at " + DateTime.Now + "\n");

                comPort.DiscardInBuffer();
                //return true
                return true;
            }
            catch (Exception ex)
            {
                _frmMgr.AddLineToRawDataView(Color.Red, ex.Message);
                return false;
            }
        }

        public bool ClosePort()
        {
            try
            {
                Debug.Print("In ClosePort(), at top");
                //first check if the port is already open
                //if its open then close it
                if (comPort.IsOpen == true) comPort.Close();
                Debug.Print("In ClosePort(), after call to comPort.Close()");

                //display message
                _frmMgr.AddLineToRawDataView(Color.Black, "Port Closed at " + DateTime.Now + "\n");

                //return true
                return true;
            }
            catch (Exception ex)
            {
                _frmMgr.AddLineToRawDataView(Color.Red, ex.Message);
                return false;
            }
        }
        #endregion

        #region Update PortNameValues
        public void SetPortNameValues(object obj)
        {
            System.Windows.Controls.ComboBox box = (System.Windows.Controls.ComboBox)obj;
            box.Items.Clear();

            foreach (string str in SerialPort.GetPortNames())
            {
                box.Items.Add(str);
            }
        }

        #endregion


        //see http://stackoverflow.com/questions/19701731/parameter-count-mismatch-while-invoking-method
        void comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //read data waiting in the buffer, one whole line at a time
                string linestr = comPort.ReadLine();

                //transfer the serial data to the main thread.  have to use Invoke for cross-thread ops
                //06/11/16 use BeginInvoke vs Invoke to avoide port closing hangs (don't know why).
                //see #3 tip at https://blogs.msdn.microsoft.com/bclteam/2006/10/10/top-5-serialport-tips-kim-hamilton/
                //_frmMgr.Dispatcher.BeginInvoke(new Action(() => UpdateTextbox(linestr)));
                _frmMgr.Dispatcher.BeginInvoke(new Action(() => _frmMgr.ProcessCommPortString(linestr)));
            }
            catch (Exception err)
            {
                System.Windows.MessageBox.Show(err.Message);
            }
        }

        //06/13/16 no longer needed - now using ProcessCommPortString() in MainWindow.xaml.cs
        //public void UpdateTextbox(string msg)
        //{
        //    try
        //    {
        //        _frmMgr.tbox_RawMagData.Text += msg;
        //        _frmMgr.lbl_NumRtbLines.Content = _frmMgr.tbox_RawMagData.LineCount;
        //        _frmMgr.tbox_RawMagData.ScrollToEnd();

        //        //add point to raw viewport

        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}
    }
}
