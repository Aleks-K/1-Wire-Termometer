using System;
using System.Collections.Generic;
using System.Text;

namespace OneWire
{
    /// <summary>
    /// 1-Wire over serial port
    /// </summary>
    public class OneWire : System.ComponentModel.Component
    {
        public OneWire()
        {
            System.Diagnostics.Debug.WriteLine("OW()");
            m_SerialPort = new System.IO.Ports.SerialPort()
            {
                BaudRate = c_BaudRateReset,
            };
        }

        private readonly System.IO.Ports.SerialPort m_SerialPort;

        private const int c_BaudRateReset = 9600;
        private const int c_BaudRateWork = 115200;

        private const string c_NotOpenError = "Port not openned!";

        public static string[] GetPortNames()
        {
            return System.IO.Ports.SerialPort.GetPortNames();
        }

        public bool IsOpen
        {
            get
            {
                return m_SerialPort.IsOpen;
            }
        }

        public string PortName
        {
            get
            {
                return m_SerialPort.PortName;
            }
            set
            {
                m_SerialPort.PortName = value;
            }
        }

        public void Open()
        {
            System.Diagnostics.Debug.WriteLine("OW: Open(" + PortName + ")");
            m_SerialPort.Open();
        }

        public void Close()
        {
            System.Diagnostics.Debug.WriteLine("OW: Close");
            m_SerialPort.Close();
        }

        public bool ResetLine()
        {
            if (!IsOpen)
                throw new InvalidOperationException(c_NotOpenError);
            m_SerialPort.BaudRate = c_BaudRateReset;
            byte[] bufTx = new byte[1] { 0xf0 };
            byte[] bufRx = new byte[bufTx.Length];
            int rxCnt = 0;
            while (m_SerialPort.BytesToRead > 0)
                m_SerialPort.ReadByte();
            m_SerialPort.Write(bufTx, 0, bufTx.Length);
            WaitExchange(bufTx.Length);
            rxCnt = m_SerialPort.BytesToRead;
            if (rxCnt > bufRx.Length)
                rxCnt = bufRx.Length;
            rxCnt = m_SerialPort.Read(bufRx, 0, rxCnt);
            if (rxCnt == 0)
                System.Diagnostics.Debug.WriteLine(string.Format("OW: Reset :: Fail not 1-Wire. Tx: {0} => Rx {1}", GetByteArrayString(bufTx), GetByteArrayString(bufRx, rxCnt)));
            else if (rxCnt == bufTx.Length)
                System.Diagnostics.Debug.WriteLine(string.Format("OW: Reset :: {2}. Tx: {0} => Rx: {1}", GetByteArrayString(bufTx), GetByteArrayString(bufRx), (bufRx[0] == bufTx[0]) ? "No devices" : "OK"));
            else
                System.Diagnostics.Debug.WriteLine(string.Format("OW: Reset :: FAIL. Tx: {0} => Rx: {1}", GetByteArrayString(bufTx), GetByteArrayString(bufRx, rxCnt)));
            return (bufRx[0] != bufTx[0]) && (rxCnt == bufTx.Length);
        }

        public bool FindDevices(out List<Address> addresses)
        {
            if (!IsOpen)
                throw new InvalidOperationException(c_NotOpenError);
            System.Diagnostics.Debug.WriteLine("OW: Find. Starting... => Write(0xf0)");
            addresses = new List<Address>();
            if (WriteByte(0xF0) != 0xF0)
            {
                System.Diagnostics.Debug.WriteLine("OW: Find. Command :: FAIL");
                return false;
            }
            int lastCollision = 0;
            var curAddr = new Address();
            for (int n = 0; n < 64; n++)
            {
                bool curBitSelection = true;
                // Reading two bits: main and additional
                byte[] bufTx = new byte[2] { 0xff, 0xff };
                byte[] bufRx = new byte[bufTx.Length];
                int rxCnt = 0;
                while (m_SerialPort.BytesToRead > 0)
                    m_SerialPort.ReadByte();
                m_SerialPort.Write(bufTx, 0, bufTx.Length);
                WaitExchange(bufTx.Length);
                rxCnt = m_SerialPort.BytesToRead;
                if (rxCnt > bufRx.Length)
                    rxCnt = bufRx.Length;
                rxCnt = m_SerialPort.Read(bufRx, 0, rxCnt);
                if (rxCnt != bufTx.Length)
                {
                    System.Diagnostics.Debug.WriteLine("OW: Find. Read address bits :: FAIL");
                    return false;
                }
                System.Diagnostics.Debug.WriteLine(string.Format("OW: Find. Write2Bits :: Tx: {0} => Rx: {1}", GetByteArrayString(bufTx), GetByteArrayString(bufRx)));
                bool bit0 = bufRx[0] == bufTx[0];
                bool bit1 = bufRx[1] == bufTx[1];
                // Analise readed bits
                if (bit0 && bit1)
                {
                    // 11, wrong combination
                    System.Diagnostics.Debug.WriteLine("OW: Find. 11 :: FAIL");
                    return false;
                }
                else if (!bit0 && bit1)
                {
                    // 01, all devices have 0 in current address bit
                    curBitSelection = false;
                }
                else if (bit0 && !bit1)
                {
                    // 10, all devices have 1 in current address bit
                    curBitSelection = true;
                }
                else
                {
                    // 00, collision. Any devices have 0 and any devices have 1 in current address bit
                    if (n < lastCollision)
                    {
                        // UNDONE: Current version works with only one device on line!
                    }
                }
                // Add selected bit to current address
                curAddr.set_Bit(n, curBitSelection);
                // Send selected bit
                bufTx = new byte[1] { (byte)(curBitSelection ? 0xff : 0x00) };
                bufRx = new byte[bufTx.Length];
                rxCnt = 0;
                while (m_SerialPort.BytesToRead > 0)
                    m_SerialPort.ReadByte();
                m_SerialPort.Write(bufTx, 0, bufTx.Length);
                WaitExchange(bufTx.Length);
                rxCnt = m_SerialPort.BytesToRead;
                if (rxCnt > bufRx.Length)
                    rxCnt = bufRx.Length;
                rxCnt = m_SerialPort.Read(bufRx, 0, rxCnt);
                if (rxCnt != bufTx.Length)
                {
                    System.Diagnostics.Debug.WriteLine("OW: Find. Accept address bit :: FAIL");
                    return false;
                }
                System.Diagnostics.Debug.WriteLine(string.Format("OW: Find. Accept address bit :: Tx: {0} => Rx: {1}", GetByteArrayString(bufTx), GetByteArrayString(bufRx)));
            }
            System.Diagnostics.Debug.WriteLine("OW: Find :: Found new device: " + curAddr.ToString());
            addresses.Add(curAddr);
            System.Diagnostics.Debug.WriteLine("OW: Find :: OK");
            return true;
        }

        public int ReadByte()
        {
            System.Diagnostics.Debug.WriteLine("OW: Read. => Write(0xff)");
            return WriteByte(0xff);
        }

        public int WriteByte(byte value)
        {
            if (!IsOpen)
                throw new InvalidOperationException(c_NotOpenError);
            m_SerialPort.BaudRate = c_BaudRateWork;
            byte[] bufTx = new byte[8];
            int mask = 0x01;
            for (int i = 0; i < 8; i++)
            {
                bufTx[i] = (byte)(((value & mask) != 0) ? 0xff : 0x00);
                mask <<= 1;
            }
            byte[] bufRx = new byte[bufTx.Length];
            int rxCnt = 0;
            while (m_SerialPort.BytesToRead > 0)
                m_SerialPort.ReadByte();
            m_SerialPort.Write(bufTx, 0, bufTx.Length);
            WaitExchange(bufTx.Length);
            rxCnt = m_SerialPort.BytesToRead;
            if (rxCnt > bufRx.Length)
                rxCnt = bufRx.Length;
            rxCnt = m_SerialPort.Read(bufRx, 0, rxCnt);
            if (rxCnt != bufTx.Length)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("OW: Write(0x{2:X2}) :: FAIL. Tx: {0} => Rx: {1}", GetByteArrayString(bufTx), GetByteArrayString(bufRx, rxCnt), value));
                return -1;
            }
            int res = 0xff;
            mask = 0x01;
            for (int i = 0; i < 8; i++)
            {
                if (bufRx[i] != 0xff)
                    // Write bit 0, else leave bit 1
                    res &= ~mask;
                if (bufRx[i] != bufTx[i])
                    unchecked
                    {
                        res |= (int)0x80000000;
                    }
                mask <<= 1;
            }
            System.Diagnostics.Debug.WriteLine(string.Format("OW: Write(0x{3:X2}) :: 0x{2:X2}. Tx: {0} => Rx: {1}", GetByteArrayString(bufTx), GetByteArrayString(bufRx), res, value));
            return res;
        }

        private static string GetByteArrayString(byte[] buf, int len = -1)
        {
            int length = len;
            if ((length < 0) || (length > buf.Length))
                length = buf.Length;
            var sb = new StringBuilder();
            sb.Append("{ ");
            if (length > 0)
                sb.AppendFormat("0x{0:X2}", buf[0]);
            for (int i = 1; i < length; i++)
                sb.AppendFormat("; 0x{0:X2}", buf[i]);
            sb.Append(" }");
            return sb.ToString();
        }

        private void WaitExchange(int length)
        {
            long k = 100;
            while (m_SerialPort.BytesToWrite > 0)
            {
                System.Threading.Thread.Sleep(1);
                k--;
                if (k <= 0) break;
            }
            k = 10 * length;
            while (m_SerialPort.BytesToRead < length)
            {
                System.Threading.Thread.Sleep(1);
                k--;
                if (k <= 0) break;
            }
        }

        public class Address
        {
            public Address()
            {
                m_Address = new byte[8];
            }
            public Address(byte[] address)
            {
                if (address.Length != 8)
                    throw new ArgumentException();
                m_Address = address;
            }

            private byte[] m_Address;

            public byte this[int index]
            {
                get
                {
                    if ((index < 0) || (index >= m_Address.Length))
                        throw new IndexOutOfRangeException();
                    return m_Address[index];
                }
                set
                {
                    if ((index < 0) || (index >= m_Address.Length))
                        throw new IndexOutOfRangeException();
                    m_Address[index] = value;
                }
            }

            public void set_Bit(int index, bool value)
            {
                if ((index < 0) || (index >= m_Address.Length * 8))
                    throw new IndexOutOfRangeException();
                byte b = m_Address[index / 8];
                int n = index % 8;
                int mask = 0x01 << n;
                if (value)
                    b |= (byte)mask;
                else
                    b &= (byte)~mask;
                m_Address[index / 8] = b;
            }
            public bool get_Bit(int index)
            {
                if ((index < 0) || (index >= m_Address.Length * 8))
                    throw new IndexOutOfRangeException();
                byte b = m_Address[index / 8];
                int n = index % 8;
                int mask = 0x01 << n;
                return (b & mask) != 0;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("{");
                sb.AppendFormat("{0:X2}", m_Address[0]);
                for (int i = 1; i < m_Address.Length; i++)
                    sb.AppendFormat(":{0:X2}", m_Address[i]);
                sb.Append("}");
                return sb.ToString();
            }
        }
    }
}
