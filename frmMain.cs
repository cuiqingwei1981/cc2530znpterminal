/**************************************************************************************************
 * frmMain.cs
 * 
 * Copyright 1998-2003 XiaoCui' Technology Co.,Ltd.
 * 
 * DESCRIPTION: - public methods for cc2530znp
 * 	   
 * modification history
 * --------------------
 * 01a, 02.25.2013, cuiqingwei written
 * --------------------
 **************************************************************************************************/
#region 相关引用
using System;
using System.Data;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using CC2530ZNP.Properties;
using System.Xml.Serialization;
//
using KONST = CC2530ZNP.Constants;
#endregion

namespace CC2530ZNP
{
    #region 公共枚举
    public enum DataMode   { Text, Hex }
    #endregion

    public partial class mForm : Form
    {
        #region 本地变量

        // The main control for communicating through the RS-232 port
        private SerialPort comport = new SerialPort();

        // Various colors for logging info
        public enum LogMsgType { Incoming, Outgoing, Normal, Warning, Error, Copyright, Rx, Tx, Hex, Msg };
        private Color[] LogMsgTypeColor = { Color.Blue, Color.Green, Color.Black, Color.Orange, Color.Red, Color.OrangeRed, Color.Blue, Color.Red, Color.Black, Color.Black };

        // Temp holder for whether a key was pressed
        private bool KeyHandled = false;

        byte[] txData;

        #endregion

        #region 构造
        public mForm()
        {
            // Build the form
            InitializeComponent();

            // 这一行很关键，用于对线程的不安全调用
            Control.CheckForIllegalCrossThreadCalls = false;

            // Restore the users settings
            InitializeControlValues();

            // Enable/disable controls based on the current state
            EnableControls();

            // When data is recieved through the port, call this method
            comport.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
        }
        #endregion

        #region 属性
        // 读取可用串口
        private void ScanSerialPort()
        {
            cmbPortName.Items.Clear();
            foreach (string s in SerialPort.GetPortNames())
            {
                cmbPortName.Items.Add(s);
            }

            if (cmbPortName.Items.Contains(Settings.Default.PortName))
            {
                cmbPortName.Text = Settings.Default.PortName;
            }
            else if (cmbPortName.Items.Count > 0)
            {
                cmbPortName.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show(this, "There are no COM Ports detected on this computer.\nPlease install a COM Port and restart this app.", "No COM Ports Installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        /// <summary> Save the user's settings. </summary>
        private void SaveSettings()
        {
            Settings.Default.BaudRate = int.Parse(cmbBaudRate.Text);
            Settings.Default.DataBits = int.Parse(cmbDataBits.Text);
            Settings.Default.DataMode = CurrentDataMode;
            Settings.Default.Parity = (Parity)Enum.Parse(typeof(Parity), cmbParity.Text);
            Settings.Default.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cmbStopBits.Text);
            Settings.Default.PortName = cmbPortName.Text;

            Settings.Default.Save();
        }

        /// <summary> Populate the form's controls with default settings. </summary>
        private void InitializeControlValues()
        {           
            cmbParity.Items.Clear(); cmbParity.Items.AddRange(Enum.GetNames(typeof(Parity)));
            cmbStopBits.Items.Clear(); cmbStopBits.Items.AddRange(Enum.GetNames(typeof(StopBits)));

            cmbParity.Text = Settings.Default.Parity.ToString();
            cmbStopBits.Text = Settings.Default.StopBits.ToString();
            cmbDataBits.Text = Settings.Default.DataBits.ToString();
            cmbParity.Text = Settings.Default.Parity.ToString();
            cmbBaudRate.Text = Settings.Default.BaudRate.ToString();
            CurrentDataMode = Settings.Default.DataMode;

            ScanSerialPort();
        }

        /// <summary> Enable/disable controls based on the app's current state. </summary>
        private void EnableControls()
        {
            // Enable/disable controls based on whether the port is open or not
            gbPortSettings.Enabled = !comport.IsOpen;
            gbZNPConfig.Enabled = comport.IsOpen;
            gbCC2530ZNP.Enabled = comport.IsOpen;
            gbAddress.Enabled = false;
            gbLight.Enabled = false;
            txtSendData.Enabled = btnSend.Enabled = comport.IsOpen;
            //
            if (comport.IsOpen)
            {
                btnOpenPort.Text = "&Close";
            }
            else
            {
                btnOpenPort.Text = "&Open";
            }
        }

        /// <summary> Change Data Mode
        private DataMode CurrentDataMode
        {
            get
            {
                if (rbHex.Checked)
                {
                    return DataMode.Hex;
                }
                else
                {
                    return DataMode.Text;
                }
            }
            set
            {
                if (value == DataMode.Text)
                {
                    rbText.Checked = true;
                }
                else
                {
                    rbHex.Checked = true;
                }
            }
        }

        /// <summary> Log data to the terminal window. </summary>
        /// <param name="msgtype"> The type of message to be written. </param>
        /// <param name="msg"> The string containing the message to be shown. </param>
        private void Log(LogMsgType msgtype, string msg)
        {
            switch(msgtype)
            {
                default:
                case LogMsgType.Copyright:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText(msg);
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Msg:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Italic);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText(msg);
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Incoming:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText(msg);
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Outgoing:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText(DateTime.Now.ToLongTimeString() + " ");
                        rtbConsole.AppendText(msg);
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Rx:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText("<RX>");
                        rtbConsole.AppendText(DateTime.Now.ToLongTimeString() + " ");
                        rtbConsole.AppendText(msg);
                        rtbConsole.AppendText("\n");
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Tx:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Bold);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText("<TX>");
                        rtbConsole.AppendText(DateTime.Now.ToLongTimeString() + " ");
                        rtbConsole.AppendText(msg);
                        rtbConsole.AppendText("\n");
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
                case LogMsgType.Hex:
                    rtbConsole.Invoke(new EventHandler(delegate
                    {
                        rtbConsole.SelectedText = string.Empty;
                        rtbConsole.SelectionFont = new Font(rtbConsole.SelectionFont, FontStyle.Regular);
                        rtbConsole.SelectionColor = LogMsgTypeColor[(int)msgtype];
                        rtbConsole.AppendText("  Hex: ");
                        rtbConsole.AppendText(msg);
                        rtbConsole.AppendText("\n");
                        rtbConsole.ScrollToCaret();
                    }));
                    break;
            }
           
        }

        /// <summary> Convert a string of hex digits (ex: E4 CA B2) to a byte array. </summary>
        /// <param name="s"> The string containing the hex digits (with or without spaces). </param>
        /// <returns> Returns an array of bytes. </returns>
        private byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
            {
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            }
            
            return buffer;
        }

        /// <summary> Converts an array of bytes into a formatted string of hex digits (ex: E4 CA B2)</summary>
        /// <param name="data"> The array of bytes to be translated into a string of hex digits. </param>
        /// <returns> Returns a well formatted string of hex digits with spacing. </returns>
        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
            {
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            }
            
            return sb.ToString().ToUpper();
        }

        /// <summary>
        /// 拆分&合并
        /// </summary>
        /// <param name="intValue"></param>
        /// <returns></returns>
        public byte hiInt16(ushort intValue)
        {
            return Convert.ToByte(intValue >> 8);
        }
        public byte loInt16(ushort intValue)
        {
            return Convert.ToByte(intValue & 255);
        }
        public ushort buildInt16(byte loByte, byte hiByte)
        {
            return ((ushort)(((loByte) & 0x00FF) + (((hiByte) & 0x00FF) << 8)));
        }
        #endregion

        #region 事件处理

        private void frmMain_Shown(object sender, EventArgs e)
        {
            this.Log(LogMsgType.Copyright, string.Format("{0} v{1} Started at {2}\n", Application.ProductName, Application.ProductVersion, DateTime.Now));
            this.Log(LogMsgType.Copyright, "Design By Cuiqingwei (cuiqingwei@gmail.com)\n");
            this.Log(LogMsgType.Copyright, "Copyright \x00a9 2006-2009 www.educationtek.com All Right Reserved.\n");
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // The form is closing, save the user's preferences
            SaveSettings();
        }
        private void cmbBaudRate_Validating(object sender, CancelEventArgs e)
        { 
            int x; e.Cancel = !int.TryParse(cmbBaudRate.Text, out x); 
        }
        private void cmbDataBits_Validating(object sender, CancelEventArgs e)
        { 
            int x; e.Cancel = !int.TryParse(cmbDataBits.Text, out x); 
        }
        private void btnOpenPort_Click(object sender, EventArgs e)
        {
            // If the port is open, close it.
            if (comport.IsOpen)
            {
                comport.Close();
            }
            else
            {
                // Set the port's settings
                comport.BaudRate = int.Parse(cmbBaudRate.Text);
                comport.DataBits = int.Parse(cmbDataBits.Text);
                comport.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cmbStopBits.Text);
                comport.Parity = (Parity)Enum.Parse(typeof(Parity), cmbParity.Text);
                comport.PortName = cmbPortName.Text;

                // Open the port
                comport.Open();

                // clear richtextbox
                rtbConsole.Clear();
            }

            // Change the state of the form's controls
            EnableControls();

            // If the port is open, send focus to the send data box
            if (comport.IsOpen)
            {
                txtSendData.Focus();
            }
        }
        private void rbText_CheckedChanged(object sender, EventArgs e)
        {
            if (rbText.Checked)
            {
                CurrentDataMode = DataMode.Text;
            }
        }
        private void rbHex_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHex.Checked)
            {
                CurrentDataMode = DataMode.Hex;
            }
        }
        private void btnSend_Click(object sender, EventArgs e)
        { 
            SendData(); 
        }
        private void txtSendData_KeyDown(object sender, KeyEventArgs e)
        { 
            // If the user presses [ENTER], send the data now
            if (KeyHandled = e.KeyCode == Keys.Enter) 
            { 
                e.Handled = true; SendData(); 
            } 
        }
        private void txtSendData_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = KeyHandled; 
        }
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbConsole.Clear();
        }
        private void exportLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Termial LogFile(*.txt)|*.txt|All Files|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            dialog.AddExtension = true;
            dialog.InitialDirectory = Path.Combine(Application.StartupPath, "log");
            dialog.DefaultExt = "txt";
            dialog.Title = "Select export Logfile path and name";
            if ((dialog.ShowDialog() == DialogResult.OK) && !File.Exists(dialog.FileName))
            {
                TextWriter writer = new StreamWriter(dialog.FileName);
                string str = this.rtbConsole.Text.ToString().Replace("\n", "\r\n");
                writer.WriteLine(str);
                writer.Close();
                Process.Start("notepad", dialog.FileName);
            }
        }
        #endregion

        #region 数据发送

        /// <summary> Send the user's data currently entered in the 'send' box.</summary>
        private void SendData()
        {
            if (CurrentDataMode == DataMode.Text)
            {
                // Send the user's text straight out the port
                comport.Write(txtSendData.Text);

                // Show in the terminal window the user's text
                Log(LogMsgType.Outgoing, txtSendData.Text + "\n");
            }
            else
            {
                try
                {
                    // Convert the user's string of hex digits (ex: B4 CA E2) to a byte array
                    byte[] data = HexStringToByteArray(txtSendData.Text);

                    // Send the binary data out the port
                    comport.Write(data, 0, data.Length);

                    // Show the hex digits on in the terminal window
                    Log(LogMsgType.Outgoing, ByteArrayToHexString(data) + "\n");
                }
                catch (FormatException)
                {
                    // Inform the user if the hex string was not properly formatted
                    Log(LogMsgType.Error, "Not properly formatted hex string: " + txtSendData.Text + "\n");
                }
            }
            txtSendData.SelectAll();
        }

        /// <summary>
        /// 发送指令，不带数据的指令
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public void SendData(ushort cmd)
        {
            byte[] _tmpData = new byte[5];
            _tmpData[0] = 0xFE;             	// 帧开头
            _tmpData[1] = 0;                	// 数据长度
            _tmpData[2] = hiInt16(cmd);	// 命令类型
            _tmpData[3] = loInt16(cmd);            
            int b = _tmpData[1];
            for (int i = 2; i < (_tmpData.Length - 1); i++)
            {
                b = b ^ _tmpData[i];
            }
            _tmpData[_tmpData.Length - 1] = (byte)b;

            try
            {
                // Send the binary data out the port
                comport.Write(_tmpData, 0, _tmpData.Length);

                // Show the hex digits on in the terminal window
                Log(LogMsgType.Hex, ByteArrayToHexString(_tmpData));
            }
            catch (FormatException)
            {
                ;
            }
        }
        /// <summary>
        /// 对数据进行城帧处理，包括加入帧头，加入数据长度、命令类型、数据、校验和
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="_arrData">数据</param>
        public void SendData(ushort cmd, byte[] _arrData)
        {
            byte[] _tmpData = new byte[5 + _arrData.Length];
            _tmpData[0] = 0xFE;                 	// 帧开头
            _tmpData[1] = (byte)_arrData.Length;	// 数据长度
            _tmpData[2] = hiInt16(cmd);    			// 命令类型
            _tmpData[3] = loInt16(cmd);
            _arrData.CopyTo(_tmpData, 4);            
            int b= _tmpData[1];
            for (int i = 2; i < (_tmpData.Length - 1); i++)
            {                
                b =b ^ _tmpData[i];
            }
            _tmpData[_tmpData.Length - 1] = (byte)b;

            try
            {
                // Send the binary data out the port
                comport.Write(_tmpData, 0, _tmpData.Length);

                // Show the hex digits on in the terminal window
                Log(LogMsgType.Hex, ByteArrayToHexString(_tmpData));
            }
            catch (FormatException)
            {
                ;
            }
        }
        #endregion 

        #region 接收处理

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            /* 必要延时 */
            System.Threading.Thread.Sleep(3);
            // This method will be called when there is data waiting in the port's buffer
            // Determain which mode (string or binary) the user is in
            if (CurrentDataMode == DataMode.Text)
            {
                // Read all the data waiting in the buffer
                string data = comport.ReadExisting();

                // Display the text to the user in the terminal
                Log(LogMsgType.Incoming, data);
            }
            else
            {
                // Obtain the number of bytes waiting in the port's buffer
                int bytes = comport.BytesToRead;

                // Create a byte array buffer to hold the incoming data
                byte[] buffer = new byte[bytes];

                // Read the data from the port and store it in our buffer
                comport.Read(buffer, 0, bytes);

                // Show the user the incoming data in hex format
                // Log(LogMsgType.Incoming, ByteArrayToHexString(buffer));
                //
                if (bytes > 4)
                {
                    byte[] panId = new byte[2];
                    byte[] chann = new byte[4];
                    byte[] nwkAddr = new byte[2];
                    byte[] macAddr = new byte[8];
                    string tempStr = "";
                    // CC2530-ZNP format  : SOF:1, LEN:1, CMD0:1, CMD1:1, DATA:n, FCS:1
                    switch (buildInt16(buffer[3], buffer[2]))
                    {
                        case KONST.SYS_RESET_IND:
                            Log(LogMsgType.Rx, "SYS_RESET_IND");
                            break;

                        case KONST.ZDO_STARTUP_FROM_APP_SRSP:
                            Log(LogMsgType.Rx, "ZDO_STARTUP_FROM_APP_SRSP");
                            break;

                        case KONST.ZB_START_REQUEST_SRSP:
                            Log(LogMsgType.Rx, "ZB_START_REQUEST_SRSP");
                            break;
                        
                        case KONST.ZB_SEND_DATA_REQUEST_SRSP:
                            Log(LogMsgType.Rx, "ZB_SEND_DATA_REQUEST_SRSP");
                            break;

                        case KONST.SYS_VERSION_SRSP:
                            Log(LogMsgType.Rx, "SYS_VERSION_SRSP");
                            break;
 
                        case KONST.ZB_READ_CONFIGURATION_SRSP:
                            /* 	len   =     1     1     1      1         2       1     n
                            SOF | LEN | CMD0 | CMD1 | ConfigID | Len | Value  
                            Index =     0     1     2	   3         4、5    6     7... */
                            // example:
                            // 17:56:41 Tx:FE 01 26 04 83 A0 
                            // 17:56:41 Rx:FE 05 66 04 00 83 02 FF FE E7 
                            Log(LogMsgType.Rx, "ZB_READ_CONFIGURATION_SRSP");
                            if (buildInt16(buffer[5],buffer[4]) == KONST.ZCD_NV_PAN_ID)
                            {
                                /* PanID */
                                panId[0] = buffer[7];
                                panId[1] = buffer[8];
                                txtPanID.Text = ByteArrayToHexString(panId).Replace(" ", "");
                            }
                            else if (buildInt16(buffer[5], buffer[4]) == KONST.ZCD_NV_CHANLIST)
                            {
                                chann[0] = buffer[7];
                                chann[1] = buffer[8];
                                chann[2] = buffer[9];
                                chann[3] = buffer[10];
                                tempStr = ByteArrayToHexString(chann).Replace(" ", "");
                                // 找寻匹配
                                for (int i = 0; i < cmbChannel.Items.Count; cmbChannel.SelectedIndex=i++)
                                {
                                    if (cmbChannel.Text.Substring(7, 8).Equals(tempStr))
                                    {
                                        break;
                                    }
                                    
                                }
                            }
                            else if (buildInt16(buffer[5], buffer[4]) == KONST.ZCD_NV_LOGICAL_TYPE)
                            {
                                cmbLogicType.SelectedIndex = buffer[7];
                            }
                            break;

                        case KONST.ZB_WRITE_CONFIGURATION_SRSP:
                            Log(LogMsgType.Rx, "ZB_WRITE_CONFIGURATION_SRSP");
                            break;

                        case KONST.UTIL_GET_DEVICE_INFO_SRSP:
                            //         1            1            1          1       8          2          1 
                            //  Length = 0x02  Cmd0 = 0x67  Cmd1 = 0x00  Status  IEEEAddr  ShortAddr  DeviceType 
                            //      1              1              0-128 
                            // DeviceState  NumAssocDevices  AssocDeviceList 
                            Log(LogMsgType.Rx, "UTIL_GET_DEVICE_INFO_SRSP");
                            for (int i=0; i<buffer[17]; i++)
                            {
                                /* nwkID */
                                nwkAddr[0] = buffer[19+2*i];
                                nwkAddr[1] = buffer[18+2*i];
                                tempStr = ByteArrayToHexString(nwkAddr);
                                /* 判断重复 */
                                foreach (object item in cmbNwkAddr.Items)
                                {
                                    if (item.Equals(tempStr))
                                    {
                                        break;
                                    }
                                }
                                cmbNwkAddr.Items.Add(tempStr);
                                cmbNwkAddr.SelectedIndex = cmbNwkAddr.Items.Count - 1;
                                if (cmbBaudRate.Items.Count != 0)
                                {
                                    gbAddress.Enabled = true;
                                    gbLight.Enabled = true;
                                }
                            }
                            break;
                        case KONST.ZB_SEND_DATA_CONFIRM:
                            Log(LogMsgType.Rx, "ZB_SEND_DATA_CONFIRM");
                            break;

                        case KONST.ZB_RECEIVE_DATA_INDICATION:
                            Log(LogMsgType.Rx, "ZB_RECEIVE_DATA_INDICATION");
                            break;
                    }
                    // Show the user the incoming data in hex format
                    Log(LogMsgType.Hex, ByteArrayToHexString(buffer));
                }
            }
        }
        #endregion
        //////////////////////////////////////////////////////////////////////////////////////////////////
        #region CC2530ZNP接口函数
        public void sys_reset_req()
        {
            Log(LogMsgType.Tx, "SYS_RESET_REQ");
            this.SendData(KONST.SYS_RESET_REQ);
        }
        public void sys_version()
        {
            Log(LogMsgType.Tx, "SYS_VERSION");
            this.SendData(KONST.SYS_VERSION);
        }
        public void zb_read_configuration(byte ConfigId)
        {
            txData = new byte[1];
            /* ConfigId */
            txData[0] = (byte)ConfigId;
            //
            Log(LogMsgType.Tx, "ZB_READ_CONFIGURATION");
            this.SendData(KONST.ZB_READ_CONFIGURATION, txData);
        }
        public void zb_write_configuration(byte ConfigId, byte[] _arrData)
        { 
            txData = new byte[1 + _arrData.Length];
            /* ConfigId */
            txData[0] = (byte)ConfigId;
            _arrData.CopyTo(txData, 1);     
            //
            Log(LogMsgType.Tx, "ZB_WRITE_CONFIGURATION");
            this.SendData(KONST.ZB_WRITE_CONFIGURATION, txData);
        }
        public void zb_app_register_request()
        {
            //        1                1            1           1             2           2           1 
            // Length = variable  Cmd0 = 0x26  Cmd1 = 0x0A  AppEndPoint  AppProfileID  DeviceId  DeviceVersion 
            //    1            1         2 x Input commands           1        2 x Output commands 
            // Unused  InputCommandsNum  InputCommandsList  OutputCommandsNum  OutputCommandsList 
            txData = new byte[14];

            /* AppEndpoint */
            txData[0] = 12;
            /* AppProfileID */
            txData[1] = loInt16(260);   // lo
            txData[2] = hiInt16(260);   // hi

            /* DestEndpoint */
            txData[3] = 13;

            /* DeviceId*/
            txData[4] = 0;
            txData[5] = 0;

            /* DeviceVersion */
            txData[6] = 0;

            /* Unused */
            txData[7] = 0;

            /* InputCommandsNum */
            txData[8] = 1;

            /* InputCommandsList */
            txData[9] = 0;
            txData[10] = 0;

            /* InputCommandsNum */
            txData[11] = 1;

            /* InputCommandsList */
            txData[12] = 0;
            txData[13] = 0;
            //
            Log(LogMsgType.Tx, "ZB_APP_REGISTER_REQUEST");
            this.SendData(KONST.ZB_APP_REGISTER_REQUEST, txData);
        }

        public void zb_start_request()
        {
            Log(LogMsgType.Tx, "ZB_START_REQUEST");
            this.SendData(KONST.ZB_START_REQUEST);
        }

        public void zdo_startup_from_app(ushort startdelay)
        {
            txData = new byte[2];
            // StartDelay(2B):Specifies the time delay before the device starts in milliseconds. 
            txData[0] = loInt16(startdelay);
            txData[1] = hiInt16(startdelay);
            //
            Log(LogMsgType.Tx, "ZDO_STARTUP_FROM_APP");
            this.SendData(KONST.ZDO_STARTUP_FROM_APP, txData);
        }
        public void util_get_device_info()
        {
            Log(LogMsgType.Tx, "UTIL_GET_DEVICE_INFO");
            this.SendData(KONST.UTIL_GET_DEVICE_INFO);
        }

        public void zb_send_data_request(ushort Destination, ushort CommandId, byte[] _arrData)
        { 
            /*      1                1            1            2           2        1 
            Length = 0x08-0x5C  Cmd0 = 0x26  Cmd1 = 0x03  Destination  CommandId  Handle 
             1      1     1   0-84 
            Ack  Radius  Len  Data */
            txData = new byte[8 + _arrData.Length];
            /* Destination */
            txData[0] = loInt16(Destination);
            txData[1] = hiInt16(Destination);
            /* CommandId */
            txData[2] = loInt16(CommandId);
            txData[3] = hiInt16(CommandId);
            /* Handle */
            txData[4] = 0;
            /* Ack */
            txData[5] = 0;
            /* Radius */
            txData[6] = 0;
            /* Len */
            txData[7] = (byte)_arrData.Length;
            /* Data */
            _arrData.CopyTo(txData, 8);
            //
            Log(LogMsgType.Tx, "ZB_SEND_DATA_REQUEST");
            this.SendData(KONST.ZB_SEND_DATA_REQUEST,txData);
        }
        #endregion
        /////////////////////////////////////////////////////i/////////////////////////////////////////////
        #region CC2530ZNP控制
        private void btnReset_Click(object sender, EventArgs e)
        {
            this.sys_reset_req();
        }
        private void btnVersion_Click(object sender, EventArgs e)
        {
            this.sys_version();
        }
        private void btnPanIDRead_Click(object sender, EventArgs e)
        {
            this.zb_read_configuration((byte)KONST.ZCD_NV_PAN_ID);
        }
        private void btnPanIDWrite_Click(object sender, EventArgs e)
        {
            txData = new byte[3];
            /* Len*/
            txData[0] = 2;
            /* Value */
            string tempStr = txtPanID.Text;
            try
            {
                byte[] tempPanID = this.HexStringToByteArray(tempStr);
                txData[1] = tempPanID[1];   // lo
                txData[2] = tempPanID[0];   // hi
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "PanID Error. Details:" + exception.Message, "PanID Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }
            this.zb_write_configuration((byte)KONST.ZCD_NV_PAN_ID, txData);
        }
        private void btnChannRead_Click(object sender, EventArgs e)
        {
            this.zb_read_configuration((byte)KONST.ZCD_NV_CHANLIST);
        }
        private void btnChannWrite_Click(object sender, EventArgs e)
        {
            txData = new byte[5];
            /* Len*/
            txData[0] = 4;
            /* Value */
            string tempStr = cmbChannel.Text;   // 取得信道值
            tempStr = tempStr.Substring(7, 8);  // 截取有效值 
            byte[] tempChannel = this.HexStringToByteArray(tempStr);
            txData[1] = tempChannel[3];
            txData[2] = tempChannel[2];
            txData[3] = tempChannel[1];
            txData[4] = tempChannel[0];
            this.zb_write_configuration((byte)KONST.ZCD_NV_CHANLIST, txData);
        }
        private void btnLogicTypeRead_Click(object sender, EventArgs e)
        {
            this.zb_read_configuration((byte)KONST.ZCD_NV_LOGICAL_TYPE);
        }
        private void btnLogicTypeWrite_Click(object sender, EventArgs e)
        {
            txData = new byte[2];
            /* Len*/
            txData[0] = 1;
            /* Value */
            txData[1] = (byte)cmbLogicType.SelectedIndex;

            this.zb_write_configuration((byte)KONST.ZCD_NV_LOGICAL_TYPE, txData);
        }
        private void btnRegister_Click(object sender, EventArgs e)
        {
            this.zb_app_register_request();
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            this.zb_start_request();
        }
        private void btnStartAPP_Click(object sender, EventArgs e)
        {
            this.zdo_startup_from_app(0);
        }
        private void btnGetDeviceInfo_Click(object sender, EventArgs e)
        {
            this.util_get_device_info();
        }    
        #endregion

        public void lightON()
        {
            txData = new byte[8];

            /* AppEndpoint */
            txData[0] = 12;

            string tempStr = cmbNwkAddr.Text.Substring(0, 5);
            byte[] tempAddr = this.HexStringToByteArray(tempStr);

            /* DestAddress */
            txData[1] = tempAddr[1];    // lo
            txData[2] = tempAddr[0];    // hi

            /* DestEndpoint */
            txData[3] = 13;

            /* ClusterID*/
            txData[4] = 0x06;
            txData[5] = 0;

            /* MsgLen */
            txData[6] = 1;

            /* Message (on/off)*/
            txData[7] = 1;

            this.SendData(KONST.APP_MSG, txData);
        }
        public void lightOFF()
        {
            txData = new byte[8];
            
            /* AppEndpoint */
            txData[0] = 12;          

            string tempStr  = cmbNwkAddr.Text.Substring(0, 5);
            byte[] tempAddr = this.HexStringToByteArray(tempStr);
            
            /* DestAddress */
            txData[1] = tempAddr[1];    // lo
            txData[2] = tempAddr[0];    // hi

			/* DestEndpoint */
            txData[3] = 13; 
         	
            /* ClusterID*/
            txData[4] = 0x06;
            txData[5] = 0;

            /* MsgLen */         	 
            txData[6] = 1;  
            
			/* Message (on/off)*/
            txData[7] = 0;              

            this.SendData(KONST.APP_MSG, txData);
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////
        private void btnLightON_Click(object sender, EventArgs e)
        {
            lightON();
        }

        private void btnLightOFF_Click(object sender, EventArgs e)
        {
            lightOFF();
        }
    }
}

/*-------------------------------------------------------------------------------------------------
								             	 0ooo
							           	ooo0     (   )
								        (   )     ) /
							           	 \ (     (_/
	    				                  \_)        By:cuiqingwei [gary]
--------------------------------------------------------------------------------------------------*/