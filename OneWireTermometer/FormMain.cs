using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OneWireTermometer
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            // TODO: Select port name. Now Select last finded port
            var p = OneWire.OneWire.GetPortNames();
            if (p.Length > 0)
                oneWire1.PortName = p[p.Length - 1];
            try
            {
                oneWire1.Open();
            }
            catch
            {
            }
            backgroundWorker1.RunWorkerAsync(oneWire1);
        }

        private delegate void SetTextCallback(string value);

        private void SetText(string value)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new SetTextCallback(SetText), value);
                    return;
                }
                labelTemperature.Text = value;
            }
            catch
            {
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var port = e.Argument as OneWire.OneWire;
                var sensor = new OneWire.SensorDS18B20(port)
                {
                    // TODO: Select device address
                    Address = OneWire.OneWire.Address.Broadcast,
                };
                SetText(port.PortName);
                if (port.IsOpen)
                {
                    sensor.MeasureT();
                    System.Threading.Thread.Sleep(1000);
                }
                while (port.IsOpen)
                {
                    if (sensor.ReadT())
                        SetText(string.Format("{0:F3}°C", sensor.Value));
                    else
                        SetText("UNKN.");
                    sensor.MeasureT();
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch
            {
            }
            SetText("CLOSED");
        }
    }
}
