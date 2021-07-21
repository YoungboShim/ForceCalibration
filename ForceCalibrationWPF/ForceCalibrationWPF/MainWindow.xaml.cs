﻿using System;
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

                textBlockTS.Text += serialTS.ReadLine();
                textBlockForce.Text += serialForce.ReadExisting();

                Console.WriteLine("...Success");
                buttonConnect.Background = Brushes.Orange;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Serial port open failed");
                Console.WriteLine(ex.Message);
                buttonConnect.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ffdddddd"));
            }
        }

        private void closePortsIfOpened()
        {
            if (serialForce.IsOpen)
                serialForce.Close();
            if (serialTS.IsOpen)
                serialTS.Close();
            buttonConnect.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#ffdddddd"));
        }

        private void InitSerialPorts()
        {
            serialTS.BaudRate = 115200;
            serialForce.BaudRate = 115200;

            serialTS.DataReceived += new SerialDataReceivedEventHandler(serialTS_DataReceived);
            serialForce.DataReceived += new SerialDataReceivedEventHandler(serialForce_DataReceived);            
        }

        private void serialTS_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serialTS.ReadExisting();
                Console.WriteLine(line);
                if (line != string.Empty)
                {
                    SetLog(ScrollViewerTS, textBlockTS, line);
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Timeout!");
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
                }
            }
            catch (TimeoutException)
            {
            }
        }

        private void SetLog(ScrollViewer viewer, TextBlock txtBlock, string text)
        {
            Console.WriteLine(text);
            if (txtBlock.Dispatcher.CheckAccess())
            {
                txtBlock.Text += text;
                viewer.ScrollToEnd();
            }
            else
            {
                SetTextCallBack d = new SetTextCallBack(SetLog);
                txtBlock.Dispatcher.Invoke(d, new object[] { viewer, txtBlock, text });
            }
        }
    }
}
