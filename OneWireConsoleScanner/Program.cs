using System;
using System.Collections.Generic;

namespace OneWireConsoleScanner
{
    internal class Program
    {
        private static void Main(string[] args)
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
                        if (args.Length > 0)
                        {
                            // when read concrete devices
                            for (int i = 0; i < args.Length; i++)
                            {
                                try
                                {
                                    var sensor = new OneWire.SensorDS18B20(oneWire)
                                    {
                                        Address = OneWire.OneWire.Address.Parse(args[i])
                                    };
                                    if (sensor.UpdateValue())
                                    {
                                        Console.WriteLine("Sensor's {0} value is {1} C", sensor.Address, sensor.Value);
                                    }
                                }
                                catch (ArgumentException ex)
                                {
                                    Console.WriteLine("Sensor address {0} is not valid", args[i]);  
                                }
                                catch
                                {}
                            }
                        }
                        else
                        {
                            List<OneWire.OneWire.Address> devices;
                            oneWire.FindDevices(out devices);
                            Console.WriteLine("Found {0} devices on port {1}", devices.Count, port);
                            devices.ForEach(Console.WriteLine);
                        }
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
