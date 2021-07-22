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
using System.IO.Ports;
using System.IO;
using System.Windows.Threading;

namespace ForceCalibrationWPF
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsLeftStick = true;
        string StickDir = "";
        SerialPort serialTS = new SerialPort();
        SerialPort serialForce = new SerialPort();
        delegate void SetTextCallBack(ScrollViewer viewer, TextBlock txtBlock, string text);
        delegate void SetSquareHeight(int rawForce);
        bool IsRecording = false;
        StreamWriter sw;
        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            InitSerialPorts();
        }

        private void comboBoxLeftRight_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsLeftStick = (sender as ComboBox).SelectedItem.ToString() == "Left";
        }

        private void comboBoxDir_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StickDir = (sender as ComboBox).SelectedItem.ToString();
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();

            closePortsIfOpened();

            comboBoxTSPort.Items.Clear();
            comboBoxForcePort.Items.Clear();

            foreach (string port in ports)
            {
                comboBoxTSPort.Items.Add(port);
                comboBoxForcePort.Items.Add(port);
            }
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            closePortsIfOpened();

            serialTS.PortName = comboBoxTSPort.SelectedItem.ToString();
            serialForce.PortName = comboBoxForcePort.SelectedItem.ToString();

            Console.WriteLine("Serial Connect");

            try
            {
                serialTS.Open();
                serialForce.Open();

                textBlockTS.Text = serialTS.ReadExisting();
                textBlockForce.Text = serialForce.ReadExisting();

                Console.WriteLine("...Success");
                buttonConnect.Background = Brushes.Orange;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Serial port open failed");
                Console.WriteLine(ex.Message);
                buttonConnect.Background = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
            }
        }

        private void closePortsIfOpened()
        {
            if (serialForce.IsOpen)
                serialForce.Close();
            if (serialTS.IsOpen)
                serialTS.Close();
            buttonConnect.Background = new SolidColorBrush(Color.FromArgb(255,221,221,221));
        }

        private void InitSerialPorts()
        {
            serialTS.BaudRate = 115200;
            serialForce.BaudRate = 115200;
            serialTS.RtsEnable = true;
            serialForce.RtsEnable = true;

            //serialTS.DataReceived += new SerialDataReceivedEventHandler(serialTS_DataReceived);
            //serialForce.DataReceived += new SerialDataReceivedEventHandler(serialForce_DataReceived);            
        }

        private void serialTS_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serialTS.ReadLine();
                if (line != string.Empty)
                {
                    SetLog(ScrollViewerTS, textBlockTS, line);
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Serial TS Timeout!");
            }
        }

        private void serialForce_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serialForce.ReadExisting();
                if (line != string.Empty)
                {
                    SetLog(ScrollViewerForce, textBlockForce, line);
                    string[] words = line.Trim('\n').Split('\n');
                    int rawVal;
                    if (words.Length > 0 && int.TryParse(words[words.Length - 1], out rawVal))
                    {
                        controlForceBar(rawVal);
                    }
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Serial Force Timeout!");
            }
        }

        private void SetLog(ScrollViewer viewer, TextBlock txtBlock, string text)
        {
            if (txtBlock.Dispatcher.CheckAccess())
            {
                txtBlock.Text = text;
            }
            else
            {
                SetTextCallBack d = new SetTextCallBack(SetLog);
                txtBlock.Dispatcher.Invoke(d, new object[] { viewer, txtBlock, text });
            }
        }

        private void controlForceBar(int rawForce)
        {
            double height = ForceBarCanvas.ActualHeight * (float)rawForce / 1024f;
            if (forceBar.Dispatcher.CheckAccess())
            {
                forceBar.Height = height;
            }
            else
            {
                SetSquareHeight d = new SetSquareHeight(controlForceBar);
                forceBar.Dispatcher.Invoke(d, new object[] { rawForce });
            }
        }

        private void buttonRecord_Click(object sender, RoutedEventArgs e)
        {
            if(!IsRecording)
            {
                // Set up streamwriter
                string filename = Directory.GetCurrentDirectory();
                if (IsLeftStick)
                    filename += "/Left_" + StickDir + ".csv";
                else
                    filename += "/Right_" + StickDir + ".csv";
                Console.WriteLine(filename);
                sw = new StreamWriter(filename);
                sw.WriteLine("time,ts_val,gauge_raw,gauge_newton");
                sw.Flush();
                Console.WriteLine("File writer open: " + filename);

                // Start timer
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Tick += new EventHandler(double_serial_Tick);
                timer.Start();

                buttonRecord.Background = Brushes.Green;
                IsRecording = true;
            }
            else
            {
                sw.Flush();
                timer.Stop();
                buttonRecord.Background = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221));
                IsRecording = false;
            }
        }

        private void double_serial_Tick(object sender, EventArgs e)
        {
            string time = DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString() + "." + DateTime.Now.Millisecond.ToString();

            string TS_line = serialTS.ReadExisting();
            string[] TS_val_list = TS_line.Split('\n');
            int TS_val = 0;
            if (IsLeftStick)
                TS_val = int.Parse(TS_val_list[TS_val_list.Length - 2].Split(',')[0]);
            else
                TS_val = int.Parse(TS_val_list[TS_val_list.Length - 2].Split(',')[1]);

            string force_line = serialForce.ReadExisting();
            string[] force_list = force_line.Split('\n');
            int force_raw = int.Parse(force_list[force_list.Length - 2]);
            float force_newtons = (float)force_raw * 10f / 1024f;

            string logLine = string.Format("{0},{1},{2},{3}", time, TS_val, force_raw, force_newtons);
            sw.WriteLine(logLine);

            controlForceBar(force_raw);
        }
    }
}
