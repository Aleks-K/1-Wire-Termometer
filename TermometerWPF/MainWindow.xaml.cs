using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Termometer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PortsCombobox.Items.Clear();
            foreach (var portName in SerialPort.GetPortNames())
            {
                PortsCombobox.Items.Add(portName);
            }

            if (!PortsCombobox.Items.IsEmpty)
            {
                PortsCombobox.Text = PortsCombobox.Items[0].ToString();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (port.IsOpen)
            {
                port.Close();
            }
            base.OnClosing(e);
        }

        readonly SerialPort port = new SerialPort();

        delegate void SetIncomingText(string text);

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (port.IsOpen)
            {
                port.Write(e.Key.ToString().ToLower());
            }
        }

        private void PortsCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (port.IsOpen)
            {
                port.Close();
            }
            try
            {
                var portName = PortsCombobox.SelectedValue;
                if (!port.IsOpen)
                {
                    port.PortName = portName.ToString();
                    port.BaudRate = 115200;
                    port.Open();
                    var backgroundThread = new Thread(() =>
                    {
                        while (port.IsOpen)
                        {
                            output.Dispatcher.BeginInvoke(new SetIncomingText((text) =>
                            {
                                Int32 value;
                                if(GetTemperatureFromText(text, out value))
                                {
                                    UpdateTermometerView(value);
                                }
                                output.AppendText(text);
                                output.ScrollToEnd();
                            }), port.ReadExisting());
                            Thread.Sleep(100);
                        }
                    });
                    backgroundThread.Start();
                }
                port.Write("vh");
            }
            catch (IOException ex)
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
        }

        private void UpdateTermometerView(int value)
        {
            TermometerProgressbar.Foreground = value > 0 ? Brushes.Red : Brushes.Blue;
            TermometerProgressbar.Value = value;
            TermometerValueTextblock.Text = GetFormattedStringWithTemperature(value);
        }

        /// <summary>
        /// Find value of temperature at the text
        /// </summary>
        /// <param name="text">input text</param>
        /// <param name="value">value of temperature</param>
        /// <returns>found or not</returns>
        private static bool GetTemperatureFromText(string text, out Int32 value)
        {
            const string keyword = "Temperature: ";
            value = 0;
            if (text.Contains(keyword))
            {
                var index = text.IndexOf(keyword, System.StringComparison.Ordinal) + keyword.Length;
                var endIndex = text.IndexOf(".", index, System.StringComparison.Ordinal);
                if (endIndex == -1)
                {
                    return false;
                }
                var valueText = text.Substring(index, endIndex - index);
                value = Convert.ToInt32(valueText); 
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get string with temperature, similar to "+1°C"
        /// </summary>
        /// <param name="value">Value of temperature</param>
        /// <returns>Formatted string</returns>
        private static string GetFormattedStringWithTemperature(Int32 value)
        {
            var sign = "";
            if (value > 0)
            {
                sign = "+";
            }
            return sign + value.ToString(CultureInfo.InvariantCulture) + "°C";
        }
    }
}
