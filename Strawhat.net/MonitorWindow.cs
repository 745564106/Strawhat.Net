using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.IO;

namespace Strawhat.net
{
    public partial class MonitorWindow : Form
    {
        SerialPort serialPort = new SerialPort();
        ManagementEventWatcher watcher = new ManagementEventWatcher();
        WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent");
        BindingList<String> ports = new BindingList<string>();
        BindingSource pSource = new BindingSource();
        BindingList<string> sent = new BindingList<string>();
        BindingSource sSource = new BindingSource();

        public MonitorWindow()
        {
            InitializeComponent();
            chartBox.Enabled = false;
            chartBox.Visible = false;
            chartBox.Series.Add("Series1");
            chartBox.Series["Series1"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            chartBox.Series["Series1"].BorderWidth = 3;
            saveGraphToolStripMenuItem.Enabled = false;
            saveGraphToolStripMenuItem.Visible = false;

            trailBox.Items.Add("");
            trailBox.Items.Add("LF");
            trailBox.Items.Add("CR");
            trailBox.Items.Add("CRLF");
            trailBox.SelectedIndex = 0;

            //Initiate baud rate list
            List<int> baudRate = new List<int>(){ 9600, 19200, 115200, 384000, 250000, 1000000, 2000000 };
            baudBox.DataSource = baudRate;
            baudBox.SelectedIndex = 0;
            
            //Bind ports list
            pSource.DataSource = ports;
            portBox.DataSource = pSource;
            ports_refresh();

            //Set receive text box empy
            receiveText.Text = "";
            
            //Bind sendbox list
            sSource.DataSource = sent;
            sendBox.DataSource = sSource;

            //Serial port
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = Handshake.None;
            serialPort.RtsEnable = false;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            //Set watcher event handler of usb device change event
            watcher.EventArrived += new EventArrivedEventHandler(watcherEventArrived);
            watcher.Query = query;
            try
            {
                watcher.Start();
            }
            catch
            {
                Console.WriteLine("Watcher failed to start");
            }
            
        }

        private void ports_refresh()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    ports.Clear();
                    Console.WriteLine("Ports cleared");
                    foreach (String port in SerialPort.GetPortNames())
                    {
                        ports.Add(port);
                        Console.Write(port);
                        Console.WriteLine(" added");
                    }
                    ports.ResetBindings();
                    Console.WriteLine("Ports binding resetted");
                }));
            }
            else
            {
                ports.Clear();
                Console.WriteLine("Ports cleared");
                foreach (String port in SerialPort.GetPortNames())
                {
                    ports.Add(port);
                    Console.Write(port);
                    Console.WriteLine(" added");
                }
                ports.ResetBindings();
                Console.WriteLine("Ports binding resetted");
            }
            
        }

        private void watcherEventArrived(Object sender, EventArrivedEventArgs e)
        {
            ports_refresh();
            if (!serialPort.IsOpen)
            {
                connectButton.Invoke(new MethodInvoker(delegate
                {
                    connectButton.Text = "Connect";
                }));
            }
            Console.WriteLine("Device change event");
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.PortName = portBox.SelectedItem.ToString();
                    serialPort.BaudRate = Int32.Parse(baudBox.SelectedItem.ToString());
                    serialPort.Open();
                    connectButton.Text = "Disconnect";
                    Console.WriteLine(serialPort.PortName + " " + serialPort.BaudRate + " Connected");
                }
                else if (serialPort.IsOpen)
                {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                    serialPort.Close();
                    connectButton.Text = "Connect";
                    Console.WriteLine(serialPort.PortName + " " + serialPort.BaudRate + " Disconnected");
                }
            }
            catch
            {
                Console.WriteLine("Connection Failed");
            }
        }

        private void DataReceivedHandler(Object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            try
            {
                String data;
                while (serialPort.BytesToRead > 0)
                {
                    if (chartBox.Enabled)
                    {
                        data = sp.ReadLine();
                        string[] y = data.Split('\n');
                        double check;
                        if(double.TryParse(y[0], out check))
                        {
                            if (chartBox.InvokeRequired)
                            {
                                chartBox.Invoke(new MethodInvoker(delegate
                                {
                                    chartBox.Series["Series1"].Points.AddY(y[0]);
                                }));
                            }
                        }
                        
                    } else data = sp.ReadExisting();

                    if (receiveText.InvokeRequired)
                    {
                        receiveText.BeginInvoke(new MethodInvoker(delegate
                        {
                            receiveText.AppendText(data);
                            if (chartBox.Enabled) receiveText.AppendText(Environment.NewLine);
                            receiveText.ScrollToCaret();
                        }));
                    }
                    
                }
            }
            catch (System.InvalidOperationException err)
            {
                Console.WriteLine(err.Message);
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            String sendMessage = sendBox.Text;

            try
            {
                if (serialPort.IsOpen)
                {
                    switch (trailBox.Text)
                    {
                        case "": serialPort.Write(sendMessage); break;
                        case "CR": serialPort.Write(sendMessage + "\r"); break;
                        case "LF": serialPort.Write(sendMessage + "\n"); break;
                        case "CRLF": serialPort.Write(sendMessage + "\r\n"); break;
                    }
                    sent.Add(sendMessage);
                    sent.ResetBindings();
                    sendBox.Text = "";
                }
            }
            catch
            {
                Console.WriteLine("Failed to send");
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            receiveText.Text = "";
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Text Files | *.txt";
            saveDialog.Title = "Save Serial Monitor Messages";
            saveDialog.ShowDialog(this);
            try
            {
                File.WriteAllText(saveDialog.FileName, receiveText.Text);
            }
            catch
            {
                Console.WriteLine("Failed to save");
            }
                
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void plotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!chartBox.Enabled)
            {
                receiveText.Height = 50;
                chartBox.Visible = true;
                chartBox.Enabled = true;
                if (this.WindowState == FormWindowState.Maximized) { chartBox.Height = 500; }
                else chartBox.Height = 300;
                plotToolStripMenuItem.Checked = true;
                saveGraphToolStripMenuItem.Enabled = true;
                saveGraphToolStripMenuItem.Visible = true;
                this.MinimumSize = new System.Drawing.Size(425, 489);
                Console.WriteLine("Windows Size: {0},{1}", this.Width, this.Height);
            }
            else if (chartBox.Enabled)
            {
                chartBox.Series.Clear();
                this.MinimumSize = new System.Drawing.Size(425, 348);
                receiveText.Height = 215;
                chartBox.Visible = false;
                chartBox.Enabled = false;
                plotToolStripMenuItem.Checked = false;
                saveGraphToolStripMenuItem.Enabled = false;
                saveGraphToolStripMenuItem.Visible = false;
                Console.WriteLine("Windows Size: {0},{1}", this.Width, this.Height);
            }
        }

        private void saveGraphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Jpeg Images | *.jpeg";
            saveDialog.ShowDialog(this);
            try
            {
                chartBox.SaveImage(saveDialog.FileName, System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Jpeg);
            }
            catch
            {

            }
        }

        private void MonitorWindow_Resize(object sender, EventArgs e)
        {
            if(chartBox.Enabled && this.WindowState == FormWindowState.Maximized) chartBox.Height = 500;
            else if(chartBox.Enabled && this.WindowState == FormWindowState.Normal) chartBox.Height = 300;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
