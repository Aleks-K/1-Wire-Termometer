using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneWireConsoleScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("OneWire scanner");
            var ports = OneWire.OneWire.GetPortNames();
            if (ports.Length == 0)
            {
                Console.WriteLine("No one availible port");
                return;
            }

            var oneWire = new OneWire.OneWire();
            foreach (var port in ports)
            {
                oneWire.PortName = port;
                try
                {
                    oneWire.Open();
                    if (oneWire.ResetLine())
                    {
                        List<OneWire.OneWire.Address> devices;
                        oneWire.FindDevices(out devices);
                        Console.WriteLine("Found {0} devices on port {1}", devices.Count, port);
                        devices.ForEach(Console.WriteLine);
                    }
                    else
                    {
                        Console.WriteLine("No devices on port {0}", port);
                    }
                }
                catch
                {
                    Console.WriteLine("Can't scan port {0}", port);
                }
                finally
                {
                    oneWire.Close();
                }
            }
        }
    }
}
