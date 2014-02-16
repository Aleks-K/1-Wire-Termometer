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
            m_Sensor1 = new OneWire.SensorDS18B20(oneWire1)
            {
                // TODO: Select device address
                Address = new OneWire.OneWire.Address(new byte[8] { 0x28, 0xDE, 0xD3, 0xB9, 0x04, 0x00, 0x00, 0x45 }),
            };
            if (oneWire1.IsOpen)
            {
                UpdateT();
                timerUpdateT.Start();
            }
        }

        private OneWire.SensorDS18B20 m_Sensor1;

        private delegate void SetTemperatureCallback(float value);

        private void SetTemperature(float value)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new SetTemperatureCallback(SetTemperature), value);
                    return;
                }
            }
            catch
            {
            }
            if (float.IsNaN(value))
                labelTemperature.Text = "UNKNOWN";
            else
                labelTemperature.Text = string.Format("{0}°C", value);
        }

        private void timerUpdateT_Tick(object sender, EventArgs e)
        {
            UpdateT();
        }

        protected void UpdateT()
        {
            try
            {
                if (m_Sensor1.UpdateValue())
                    SetTemperature(m_Sensor1.Value);
                else
                    SetTemperature(float.NaN);
            }
            catch
            {
                SetTemperature(float.NaN);
            }
        }
    }
}
