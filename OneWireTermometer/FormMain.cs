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
            // TODO: Select port name
            oneWire1.PortName = "COM1";
            try
            {
                oneWire1.Open();
            }
            catch
            {
            }
            if (oneWire1.IsOpen)
            {
                UpdateT();
                timerUpdateT.Start();
            }
        }

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
            float temp = float.NaN;
            try
            {
                bool ok = true;
                // DS18B20 command: Skip ROM, Convert T
                if (ok)
                    ok = oneWire1.ResetLine();
                if (ok)
                    ok = 0xCC == oneWire1.WriteByte(0xCC);
                if (ok)
                    ok = 0x44 == oneWire1.WriteByte(0x44);
                // Wait for convert t command execute
                if (ok)
                    System.Threading.Thread.Sleep(1000);
                // DS18B20 command: Skip ROM, Read T
                if (ok)
                    ok = oneWire1.ResetLine();
                if (ok)
                    ok = 0xCC == oneWire1.WriteByte(0xCC);
                if (ok)
                    ok = 0xBE == oneWire1.WriteByte(0xBE);
                if (ok)
                {
                    int tL = oneWire1.ReadByte();
                    int tH = oneWire1.ReadByte();
                    temp = (Int16)((byte)(tH << 8) | (byte)tL) / 16f;
                }
            }
            catch
            {
            }
            SetTemperature(temp);
        }
    }
}
