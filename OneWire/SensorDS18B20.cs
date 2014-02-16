using System;
using System.Collections.Generic;
using System.Text;

namespace OneWire
{
    public class SensorDS18B20
    {
        public SensorDS18B20(OneWire port)
        {
            m_Port = port;
            Address = OneWire.Address.Broadcast;
            m_Value = 0;
            m_AlarmValue = 85 * 16;
            m_Config = 0x7F;
        }

        private OneWire m_Port;

        private Int16 m_Value, m_AlarmValue;
        private byte m_Config;

        private bool m_Error = true;
        private bool m_ParasitePowered = true;

        public OneWire.Address Address { get; set; }

        public float Value
        {
            get
            {
                ReadScratchpad();
                if (m_Error) return float.NaN;
                return m_Value / 16f;
            }
        }
        public float AlarmValue
        {
            get
            {
                ReadScratchpad();
                if (m_Error) return float.NaN;
                return m_AlarmValue / 16f;
            }
            set
            {
                m_AlarmValue = (Int16)(value * 16);
                WriteScratchpad((byte)m_AlarmValue, (byte)(m_AlarmValue >> 8), m_Config);
                ReadScratchpad();
            }
        }
        public DS18B20Resolution Resolution
        {
            get
            {
                ReadScratchpad();
                return (DS18B20Resolution)((m_Config & 0x60) >> 5);
            }
            set
            {
                m_Config &= 0x9F;
                m_Config |= (byte)((int)value << 5);
                WriteScratchpad((byte)m_AlarmValue, (byte)(m_AlarmValue >> 8), m_Config);
                ReadScratchpad();
            }
        }

        public bool IsParasitePowered
        {
            get
            {
                ReadPowerSupply();
                return m_ParasitePowered;
            }
        }
        public bool IsError { get { return m_Error; } }



        #region ROM commands
        // UNDONE: bool SearchRom(...) 0xf0
        /// <summary>
        /// Read slave ROM, work with only one slave on the bus
        /// This command can only be used when there is one slave on the bus. It allows the bus master to read the 
        /// slave’s 64-bit ROM code without using the Search ROM procedure. If this command is used when there 
        /// is more than one slave present on the bus, a data collision will occur when all the slaves attempt to 
        /// respond at the same time.
        /// </summary>
        /// <returns>Operation status</returns>
        public bool ReadRom()
        {
            bool ok = m_Port.ResetLine();
            // Read ROM
            if (ok)
                ok = 0xCC == m_Port.WriteByte(0x33);
            var a = new OneWire.Address();
            if (ok)
            {
                for (int i = 0; i < a.Length; i++)
                    a[i] = (byte)m_Port.ReadByte();
                //ok = a[a.Length - 1] == CRC;// UNDONE: Check CRC
            }
            if (ok)
                Address = a;
            return ok;
        }
        bool MatchRom()
        {
            bool ok = m_Port.ResetLine();
            // Match ROM
            if (ok)
                ok = 0x55 == m_Port.WriteByte(0x55);
            // ROM
            for (int i = 0; i < Address.Length; i++)
                if (ok)
                    ok = Address[i] == m_Port.WriteByte(Address[i]);
            return ok;
        }
        bool SkipRom() { return m_Port.ResetLine() && (0xCC == m_Port.WriteByte(0xCC)); }
        //UNDONE: bool AlarmSearch(...) 0xec
        #endregion
        /// <summary>
        /// Select active device at the bus
        /// </summary>
        /// <returns>Operation status</returns>
        bool SelectDevice() { return Address.IsBroadcast ? SkipRom() : MatchRom(); }
        #region Function commands
        /// <summary>
        /// This command initiates a single temperature conversion. Following the conversion, the resulting thermal 
        /// data is stored in the 2-byte temperature register in the scratchpad memory and the DS18B20 returns to its 
        /// low-power idle state. If the device is being used in parasite power mode, within 10µs (max) after this 
        /// command is issued the master must enable a strong pullup on the 1-Wire bus for the duration of the 
        /// conversion (tCONV) as described in the Powering the DS18B20 section. If the DS18B20 is powered by an 
        /// external supply, the master can issue read time slots after the Convert T command and the DS18B20 will 
        /// respond by transmitting a 0 while the temperature conversion is in progress and a 1 when the conversion 
        /// is done. In parasite power mode this notification technique cannot be used since the bus is pulled high by 
        /// the strong pullup during the conversion. 
        /// </summary>
        /// <returns>Operation status</returns>
        bool ConvertT() { return SelectDevice() && (0x44 == m_Port.WriteByte(0x44)); }
        /// <summary>
        /// This command allows the master to write 3 bytes of data to the DS18B20’s scratchpad. The first data byte 
        /// is written into the TH register (byte 2 of the scratchpad), the second byte is written into the TL register 
        /// (byte 3), and the third byte is written into the configuration register (byte 4). Data must be transmitted 
        /// least significant bit first. All three bytes MUST be written before the master issues a reset, or the data 
        /// may be corrupted.
        /// </summary>
        /// <param name="alarmTL"></param>
        /// <param name="alarmTH"></param>
        /// <param name="config">0TT11111, where TT is resolution</param>
        /// <returns>Operation status</returns>
        bool WriteScratchpad(byte alarmTL, byte alarmTH, byte config)
        {
            bool ok = SelectDevice() && (0x4E == m_Port.WriteByte(0x4E));
            if (ok)
                ok = alarmTH == m_Port.WriteByte(alarmTH);
            if (ok)
                ok = alarmTL == m_Port.WriteByte(alarmTL);
            if (ok)
                ok = config == m_Port.WriteByte(config);
            return ok;
        }
        /// <summary>
        /// This command allows the master to read the contents of the scratchpad. The data transfer starts with the 
        /// least significant bit of byte 0 and continues through the scratchpad until the 9th byte (byte 8 – CRC) is 
        /// read. The master may issue a reset to terminate reading at any time if only part of the scratchpad data is 
        /// needed.
        /// </summary>
        /// <returns>Operation status</returns>
        bool ReadScratchpad()
        {
            m_Error = true;
            bool ok = SelectDevice() && (0xBE == m_Port.WriteByte(0xBE));
            byte[] buf = new byte[9];
            if (ok)
            {
                for(int i=0;i<buf.Length;i++)
                    buf[i] = (byte)m_Port.ReadByte();
                // UNDONE: Check CRC //ok = buf[buf.Length - 1] == CRC;
            }
            if (ok)
            {
                m_Value = (Int16)((buf[1] << 8) | buf[0]);
                m_AlarmValue = (Int16)((buf[3] << 8) | buf[2]);
                m_Config = buf[4];
                m_Error = false;
            }
            return ok;
        }
        /// <summary>
        /// This command copies the contents of the scratchpad TH, TL and configuration registers (bytes 2, 3 and 4) 
        /// to EEPROM. If the device is being used in parasite power mode, within 10µs (max) after this command is 
        /// issued the master must enable a strong pullup on the 1-Wire bus for at least 10ms as described in the 
        /// Powering the DS18B20 section. 
        /// </summary>
        /// <returns>Operation status</returns>
        bool CopyScratchpad() { return SelectDevice() && (0x48 == m_Port.WriteByte(0x48)); }
        /// <summary>
        /// This command recalls the alarm trigger values (TH and TL) and configuration data from EEPROM and 
        /// places the data in bytes 2, 3, and 4, respectively, in the scratchpad memory. The master device can issue 
        /// read time slots following the Recall E2
        /// command and the DS18B20 will indicate the status of the recall by 
        /// transmitting 0 while the recall is in progress and 1 when the recall is done. The recall operation happens 
        /// automatically at power-up, so valid data is available in the scratchpad as soon as power is applied to the 
        /// device
        /// </summary>
        /// <returns>Operation status</returns>
        bool RecallScratchpad() { return SelectDevice() && (0xB8 == m_Port.WriteByte(0xB8)); }
        /// <summary>
        /// The master device issues this command followed by a read time slot to determine if any DS18B20s on the 
        /// bus are using parasite power. During the read time slot, parasite powered DS18B20s will pull the bus 
        /// low, and externally powered DS18B20s will let the bus remain high. See the Powering the DS18B20 
        /// section for usage information for this command. 
        /// </summary>
        /// <returns>Operation status</returns>
        bool ReadPowerSupply()
        {
            bool ok = SelectDevice() && (0xB4 == m_Port.WriteByte(0xB4));
            if (ok)
                m_ParasitePowered = 0xFF != (byte)m_Port.ReadByte();
            return ok;
        }
        #endregion



        public bool MeasureT() { return ConvertT(); }
        public bool ReadT() { return ReadScratchpad(); }
        public bool UpdateValue()
        {
            // Measure T
            bool ok = MeasureT();
            if (ok)
            {
                // Wait for measuring
                System.Threading.Thread.Sleep(1000);
                // Read T
                ok = ReadT();
            }
            return ok;
        }
        public bool SetResolution(DS18B20Resolution value)
        {
            m_Config &= 0x9F;
            m_Config |= (byte)((int)value << 5);
            return WriteScratchpad((byte)m_AlarmValue, (byte)(m_AlarmValue >> 8), m_Config) && ReadScratchpad();
        }
        public bool SaveToEeprom()
        {
            bool ok = CopyScratchpad();
            if (ok)
                System.Threading.Thread.Sleep(1000);
            if (ok)
                ok = ReadScratchpad();
            return ok;
        }

        public override string ToString()
        {
            if (m_Error)
                return string.Format("{0} T = UNKNOWN", Address);
            return string.Format("{0} T = {1}°C, AlarmT = {2}°C", Address, Value, AlarmValue);
        }

        public enum DS18B20Resolution
        {
            NineBits = 0,
            TenBits = 1,
            ElevenBits = 2,
            TwelveBits = 3
        }
    }
}
