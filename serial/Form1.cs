using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.IO;
using System.Diagnostics;

namespace serial
{
    public partial class ID_FORM1 : Form
    {
        private string[] m_baudLate;
        private string[] m_dataBit;
        private string[] m_stopBit;
        private string[] m_patiryBit;
        private int m_speed;
        private int m_byteTerm;
        private int m_msgTerm;
        private System.Timers.Timer m_stimer;
        //private System.Windows.Forms.Timer m_stimer;
        private Stopwatch m_stopWatch = new Stopwatch();
        static SerialPort m_serialPort;
        Thread m_SendThread;
        Thread m_RecvThread;

        enum TIME_STS{

            timer_start,
            timer_stop
        };

        struct __SEND_INFO_T__
        {
            public byte[] sendBuffer;
            public bool sendFlag;
            public int sendSize;
        };
        __SEND_INFO_T__[] m_sendInfo_t = new __SEND_INFO_T__[6];

        struct __HEADER_INFO_T__
        {
            public byte[] headerBuffer;
            public bool headerFlag;
            public int HeaderSize;
            public int compareIdx;
        };
        __HEADER_INFO_T__[] m_headerInfo_t = new __HEADER_INFO_T__[6];

        struct __FORM_INFO_T__
        {
            public string name;
            public string value;
        };
        __FORM_INFO_T__[] m_formInfo_t = new __FORM_INFO_T__[128];

        bool m_sendThreadFlag;
        bool m_recvThreadFlag;

        bool m_sendTimerFlag;
        int m_sendTimerState;

        TextBox[] m_sendTextBox = new TextBox[6];
        TextBox[] m_recvHeaderTextBox = new TextBox[6];
        TextBox[] m_sendCntTextBox = new TextBox[6];
        TextBox[] m_recvCntTextBox = new TextBox[6];

        CheckBox[] m_sendCheckBox = new CheckBox[6];

        byte[,] m_headerArr = new byte[6,10];

        uint m_userCnt = 0;
        uint m_sendCnt = 0;

        public ID_FORM1()
        {
            InitializeComponent();

            this.FormClosed += formClosing;

            m_baudLate = new string[] { "9600", "14400", "19200", "38400", "57600", "115200", "230400", "460800", "921600" };
            m_dataBit = new string[] { "5", "6", "7", "8" };
            m_stopBit = new string[] { "1", "2" };
            m_patiryBit = new string[] { "EVEN", "ODD", "NONE" };

            ID_BAUDRATE_COMBOBOX.Items.AddRange(m_baudLate);
            ID_BAUDRATE_COMBOBOX.SelectedIndex = 0;

            ID_DATABIT_COMBOBOX.Items.AddRange(m_dataBit);
            ID_DATABIT_COMBOBOX.SelectedIndex = 3;

            ID_STOPBIT_COMBOBOX.Items.AddRange(m_stopBit);
            ID_STOPBIT_COMBOBOX.SelectedIndex = 0;

            ID_PARITY_COMBOBOX.Items.AddRange(m_patiryBit);
            ID_PARITY_COMBOBOX.SelectedIndex = 2;

            m_speed = 1000;
            ID_SEND_SPEED.Text = m_speed.ToString();

#if true
            m_stimer = new System.Timers.Timer(1000);
            m_stimer.Elapsed += OnTimeEventSend;
            m_stimer.AutoReset = true;
#else
            m_stimer = new System.Windows.Forms.Timer();
            m_stimer.Tick += new EventHandler(TimerEventProcessor);
            m_stimer.Interval = 1000;
            m_sendTimerState = (int)m_TimerState.timer_stop;
#endif
            m_serialPort = new SerialPort();
            sendBtn.Enabled = false;
            stopBtn.Enabled = false;
            m_sendThreadFlag = false;
            m_recvThreadFlag = false;
            ID_RECV_SHOW_CHECKBOX.Checked = true;

            m_sendCheckBox[0] = ID_SEND1_CHECKBOX;
            m_sendCheckBox[1] = ID_SEND2_CHECKBOX;
            m_sendCheckBox[2] = ID_SEND3_CHECKBOX;
            m_sendCheckBox[3] = ID_SEND4_CHECKBOX;
            m_sendCheckBox[4] = ID_SEND5_CHECKBOX;
            m_sendCheckBox[5] = ID_SEND6_CHECKBOX;

            m_sendTextBox[0] = SendList1;
            m_sendTextBox[1] = SendList2;
            m_sendTextBox[2] = SendList3;
            m_sendTextBox[3] = SendList4;
            m_sendTextBox[4] = SendList5;
            m_sendTextBox[5] = SendList6;

            m_recvHeaderTextBox[0] = ID_HEADER_TEXTBOX1;
            m_recvHeaderTextBox[1] = ID_HEADER_TEXTBOX2;
            m_recvHeaderTextBox[2] = ID_HEADER_TEXTBOX3;
            m_recvHeaderTextBox[3] = ID_HEADER_TEXTBOX4;
            m_recvHeaderTextBox[4] = ID_HEADER_TEXTBOX5;
            m_recvHeaderTextBox[5] = ID_HEADER_TEXTBOX6;

            m_sendCntTextBox[0] = ID_SEND_CNT1;
            m_sendCntTextBox[1] = ID_SEND_CNT2;
            m_sendCntTextBox[2] = ID_SEND_CNT3;
            m_sendCntTextBox[3] = ID_SEND_CNT4;
            m_sendCntTextBox[4] = ID_SEND_CNT5;
            m_sendCntTextBox[5] = ID_SEND_CNT6;

            m_recvCntTextBox[0] = ID_RECV_CNT1;
            m_recvCntTextBox[1] = ID_RECV_CNT2;
            m_recvCntTextBox[2] = ID_RECV_CNT3;
            m_recvCntTextBox[3] = ID_RECV_CNT4;
            m_recvCntTextBox[4] = ID_RECV_CNT5;
            m_recvCntTextBox[5] = ID_RECV_CNT6;

            ID_SENDCNT1_TEXT.Text = "0";
            ID_SENDCNT2_TEXT.Text = "1";

            for (int i = 0; i < m_sendCntTextBox.Length; i++)
            {
                m_sendCntTextBox[i].Text = "0";
            }

            for (int i=0; i<m_recvCntTextBox.Length; i++)
            {
                m_recvCntTextBox[i].Text = "0";
            }

            for (int i = 0; i < m_sendInfo_t.Length; i++)
            {
                m_sendInfo_t[i].sendFlag = false;
                m_sendInfo_t[i].sendBuffer = new byte[256];
            }

            for (int i = 0; i < m_headerInfo_t.Length; i++)
            {
                m_headerInfo_t[i].headerFlag = false;
                m_headerInfo_t[i].headerBuffer = new byte[256];
            }

            m_formInfo_t[0].name = "baudrate";
            m_formInfo_t[1].name = "databit";
            m_formInfo_t[2].name = "stopbit";
            m_formInfo_t[3].name = "paritybit";
            m_formInfo_t[4].name = "sendlist1";
            m_formInfo_t[5].name = "sendlist2";
            m_formInfo_t[6].name = "sendlist3";
            m_formInfo_t[7].name = "sendlist4";
            m_formInfo_t[8].name = "sendlist5";
            m_formInfo_t[9].name = "sendlist6";
            m_formInfo_t[10].name = "frequency";
            m_formInfo_t[11].name = "header1";
            m_formInfo_t[12].name = "header2";
            m_formInfo_t[13].name = "header3";
            m_formInfo_t[14].name = "header4";
            m_formInfo_t[15].name = "header5";
            m_formInfo_t[16].name = "header6";
            m_formInfo_t[17].name = "sendcheck1";
            m_formInfo_t[18].name = "sendcheck2";
            m_formInfo_t[19].name = "sendcheck3";
            m_formInfo_t[20].name = "sendcheck4";
            m_formInfo_t[21].name = "sendcheck5";
            m_formInfo_t[22].name = "sendcheck6";
            m_formInfo_t[23].name = "sendcnt2";
            m_formInfo_t[24].name = "byteterm";
        }

        private void OnTimeEventSend(object sender, ElapsedEventArgs e)
        {
            m_sendTimerFlag = true;
        }

        private void TimerEventProcessor(object myObject, EventArgs myEventArgs)
        {
            m_stimer.Stop();
            
        }

        private void formClosing(object sender, FormClosedEventArgs e)
        {
            if (m_SendThread != null)
            {
                if (m_SendThread.IsAlive)
                {
                    m_SendThread.Abort();
                }
            }

            if (m_RecvThread != null)
            {
                if (m_RecvThread.IsAlive)
                {
                    m_RecvThread.Abort();
                }
            }

            if (m_serialPort != null)
            {
                if (m_serialPort.IsOpen == false)
                {
                    m_serialPort.Close();
                }
            }
        }

        private void sendProcessor()
        {
            uint tmpl = 0;
            long preTime = 0;
            long currentTime = 0;

            if (m_sendCnt < m_userCnt)
            {
                for (int i = 0; i < m_sendInfo_t.Length; i++)
                {
                    if (m_sendInfo_t[i].sendFlag == true)
                    {
                        m_stopWatch.Start();
                        preTime = m_stopWatch.ElapsedMilliseconds;

                        if (0 == m_byteTerm)
                        {
                            m_serialPort.Write(m_sendInfo_t[i].sendBuffer, 0, m_sendInfo_t[i].sendSize);

                            tmpl = uint.Parse(m_sendCntTextBox[i].Text) + 1;
                            m_sendCntTextBox[i].Text = tmpl.ToString();
                        }
                        else
                        {
                            for (int z = 0; z < m_sendInfo_t[i].sendSize; z++)
                            {
                                m_serialPort.Write(m_sendInfo_t[i].sendBuffer, z, 1);
                                Thread.Sleep(m_byteTerm);
                            }
                            tmpl = uint.Parse(m_sendCntTextBox[i].Text) + 1;
                            m_sendCntTextBox[i].Text = tmpl.ToString();
                        }

                        while (true)
                        {
                            currentTime = m_stopWatch.ElapsedMilliseconds;
                            if (m_speed <= (currentTime - preTime))
                            {
                                m_stopWatch.Reset();
                                break;
                            }
                            else
                                Thread.Sleep(0);
                        }                     
                    }
                }

                m_sendCnt += 1;
                ID_SENDCNT1_TEXT.Text = m_sendCnt.ToString();
            }
            else
            {
                stopBtn_Click(null, null);
            }
        }

        private void sendThread()
        {
            while (true)
            {
                Thread.Sleep(0);

                if (m_sendThreadFlag)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            sendProcessor();
                        }));
                    }
                    else
                    {
                        sendProcessor();
                    }
                }
            }
        }

        
        private bool parseRecvData()
        {
            bool result = false;
            byte[] data = new byte[256];
            string msg;
            int dataOffset = 0;

            while (0 < m_serialPort.BytesToRead)
            {
                m_serialPort.Read(data, dataOffset++, 1);
                if (255 <= dataOffset)
                    break;
            }

            parseHeader(data, dataOffset);

            if (true == ID_RECV_SHOW_CHECKBOX.Checked)
            {
                msg = BitConverter.ToString(data, 0, dataOffset);
                msg = msg.Replace('-', ' ');
                msg = msg.Insert(0, " ");

                //ID_RECV_TEXTBOX.Text += msg;
                ID_RECV_TEXTBOX.AppendText(msg);

            }

            return result;
        }

        private void recvThread()
        {
            String msg;
            int byteToRead = 0;

            while (true)
            {
                Thread.Sleep(1);
                if (m_recvThreadFlag == true)
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            try
                            {
                                    byteToRead = m_serialPort.BytesToRead;
                                    if (0 < byteToRead)
                                    {
                                        parseRecvData();
                                    }
                            
                            }
                            catch (TimeoutException e)
                            {
                                msg = e.ToString();
                            }
                        }));
                    }
                    else
                    {
                        byteToRead = m_serialPort.BytesToRead;
                        if (0 < byteToRead)
                        {
                            parseRecvData();
                        }
                    }
                }
            }
        }

        private void comListBox_Click(object sender, EventArgs e)
        {
            string[] comlist = System.IO.Ports.SerialPort.GetPortNames();

            if (0 < comlist.Length)
            {
                ID_COMPORT_COMBOBOX.Items.Clear();
                ID_COMPORT_COMBOBOX.Items.AddRange(comlist);
                ID_COMPORT_COMBOBOX.SelectedIndex = 0;
            }
        }

        private void comOpenBtn_Click(object sender, EventArgs e)
        {
#if false
            if (String.IsNullOrWhiteSpace(comListBox.Text)) 
            {
                MessageBox.Show("open error #1");
            }
            else
            {
                uint num = uint.Parse(comListBox.Text, System.Globalization.NumberStyles.AllowHexSpecifier);
                //m_sendBuffer[0,] = BitConverter.GetBytes(num);
                byte[] sendBuffer = BitConverter.GetBytes(num);
                m_serialPort.Write(sendBuffer, 0, sendBuffer.Length);
            }
#else
            if (String.IsNullOrWhiteSpace(ID_COMPORT_COMBOBOX.Text))
            {
                MessageBox.Show("open error #1");
            }
            else
            {
                m_serialPort.PortName = ID_COMPORT_COMBOBOX.Text;
                m_serialPort.BaudRate = int.Parse(ID_BAUDRATE_COMBOBOX.Text);
                m_serialPort.DataBits = int.Parse(ID_DATABIT_COMBOBOX.Text) ;
                m_serialPort.StopBits = (StopBits)int.Parse(ID_STOPBIT_COMBOBOX.Text);

                if (ID_PARITY_COMBOBOX.Text.Equals("NONE"))
                    m_serialPort.Parity = Parity.None;
                else if (ID_PARITY_COMBOBOX.Text.Equals("EVEN"))
                    m_serialPort.Parity = Parity.Even;
                else if (ID_PARITY_COMBOBOX.Text.Equals("ODD"))
                    m_serialPort.Parity = Parity.Odd;
                else
                    m_serialPort.Parity = Parity.None;

                m_serialPort.Handshake = Handshake.None;

                m_serialPort.ReadTimeout = 1000;
                m_serialPort.WriteTimeout = 1000;

                if (m_serialPort.IsOpen == true)
                {
                    MessageBox.Show("open error #2");
                }
                else
                {
                    try
                    {
                        m_serialPort.Open();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("\"" + m_serialPort.PortName + "\" : " + "Access denied");
                        return;
                    }
                    
                    comOpenBtn.Enabled = false;
                    comCloseBtn.Enabled = true;
                    sendBtn.Enabled = true;
                    m_recvThreadFlag = true;
                    comCloseBtn.Enabled = true;

                    m_SendThread = new Thread(new ThreadStart(sendThread));
                    m_RecvThread = new Thread(new ThreadStart(recvThread));

                    m_SendThread.Priority = ThreadPriority.Normal;
                    m_SendThread.Priority = ThreadPriority.Normal;

                    m_SendThread.Start();
                    m_RecvThread.Start();
                }
            }
#endif
        }


        private void comBtnClose_Click(object sender, EventArgs e)
        {

            if (m_SendThread != null)
            {
                if (m_SendThread.IsAlive)
                {
                    m_SendThread.Abort();
                }
            }

            if (m_RecvThread != null)
            {
                if (m_RecvThread.IsAlive)
                {
                    m_RecvThread.Abort();
                }
            }

            if (m_serialPort != null)
            {
                if (m_serialPort.IsOpen == true)
                {
                    m_serialPort.Close();
                }
            }

            m_recvThreadFlag = false;
            m_sendThreadFlag = false;

            comOpenBtn.Enabled = true;
            comCloseBtn.Enabled = false;
            sendBtn.Enabled = false;
            stopBtn.Enabled = false;
        }

        private void sendBtn_Click(object sender, EventArgs e)
        {
            bool result = true;

            if (m_serialPort.IsOpen == false)
            {
                MessageBox.Show("Send error #1");
                return;
            }
            else
            {
                if (String.IsNullOrWhiteSpace(ID_SEND_SPEED.Text))
                {
                    ID_SENDCNT1_TEXT.Text = "0";
                    m_stimer.Interval = 1000;
                }
                else
                {
                    if (int.TryParse(ID_SEND_SPEED.Text, out m_speed))
                    {
                        ID_SENDCNT1_TEXT.Text = "0";
                        m_stimer.Interval = m_speed;
                    }
                    else
                    {
                        MessageBox.Show("Send error #2");
                        return;
                    }
                }

                if (String.IsNullOrWhiteSpace(ID_BYTETERM_TEXTBOX.Text))
                {
                    m_byteTerm = 0;
                }
                else
                {
                    if (int.TryParse(ID_BYTETERM_TEXTBOX.Text, out m_byteTerm))
                    {
                        ;
                    }
                }

                result = parseSendData();
                if (result == false)
                {
                    MessageBox.Show("data error #1");
                    return;
                }
                else
                {
                    m_sendCnt = 0;
                }

                m_userCnt = uint.Parse(ID_SENDCNT2_TEXT.Text);
                m_sendThreadFlag = true;
                m_stimer.Enabled = true;
                //m_stimer.Start();
                sendBtn.Enabled = false;
                stopBtn.Enabled = true;
                comCloseBtn.Enabled = false;
            }
        }

        private void stopBtn_Click(object sender, EventArgs e)
        {
            sendBtn.Enabled = true;
            stopBtn.Enabled = false;
            m_sendThreadFlag = false;
            m_stimer.Enabled = false;
            comCloseBtn.Enabled = true;
            ID_SEND_SPEED.Enabled = true;

            for (int i = 0; i < m_sendTextBox.Length; i++)
            {
                m_sendTextBox[i].Enabled = true;
                m_sendCheckBox[i].Enabled = true;
            }
        }

        private bool getHeader(int headerIdx)
        {
            bool result = false;
            String[] tmp_data;
            uint num = 0;
            int j = 0;
            int splitSize = 0;

            if (String.IsNullOrWhiteSpace(m_recvHeaderTextBox[headerIdx].Text) == false)
            {
                tmp_data = m_recvHeaderTextBox[headerIdx].Text.Split(' ');

                for (j = 0; j < tmp_data.Length; j++)
                {
                    if (!String.IsNullOrWhiteSpace(tmp_data[j]))
                    {
                        num = uint.Parse(tmp_data[j], System.Globalization.NumberStyles.AllowHexSpecifier);
                        m_headerInfo_t[headerIdx].headerBuffer[j] = (byte)num;
                        splitSize++;
                    }
                }

                m_headerInfo_t[headerIdx].HeaderSize = splitSize;
                m_headerInfo_t[headerIdx].headerFlag = true;
                result = true;
                splitSize = 0;
            }

            return result;
        }

        private bool parseHeader(byte[] rdata, int size)
        {
            bool result = false;
            uint tmpl = 0;

            for (int i = 0; i < m_headerInfo_t.Length; i++)
            {
                if (m_headerInfo_t[i].headerFlag == true)
                {
                    for (int j = 0; j < size; j++)
                    {
                    
                        if (m_headerInfo_t[i].headerBuffer[m_headerInfo_t[i].compareIdx] == rdata[j])
                        {
                            m_headerInfo_t[i].compareIdx += 1;
                            if (m_headerInfo_t[i].compareIdx == m_headerInfo_t[i].HeaderSize)
                            {
                                tmpl = uint.Parse(m_recvCntTextBox[i].Text) + 1;
                                m_recvCntTextBox[i].Text = tmpl.ToString();
                                m_headerInfo_t[i].compareIdx = 0;
                            }
                        }
                        else
                        {
                            m_headerInfo_t[i].compareIdx = 0;
                        }
                    }
                }
            }

            return result;
        }

        private bool parseSendData()
        {
            int j = 0;
            int splitSize = 0;
            bool result = false;
            uint num = 0;
            String[] tmp_data;

            for (int i=0; i< m_sendInfo_t.Length; i++)
            {
                Array.Clear(m_sendInfo_t[i].sendBuffer, 0, m_sendInfo_t[i].sendBuffer.Length);
                m_sendInfo_t[i].sendFlag = false;
                m_sendInfo_t[i].sendSize = 0;
            }

            for (int i=0; i<m_sendTextBox.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(m_sendTextBox[i].Text))
                {
                    tmp_data = m_sendTextBox[i].Text.Split(' ');

                    for (j = 0; j < tmp_data.Length; j++)
                    {
                        if (!String.IsNullOrWhiteSpace(tmp_data[j]))
                        {
                            num = uint.Parse(tmp_data[j], System.Globalization.NumberStyles.AllowHexSpecifier);
                            m_sendInfo_t[i].sendBuffer[j] = (byte)num;
                            splitSize++;
                        }
                    }

                    if (m_sendCheckBox[i].Checked == true)
                    {
                        m_sendInfo_t[i].sendFlag = true;
                    }
                    else
                    {
                        m_sendInfo_t[i].sendFlag = false;
                    }

                    m_sendInfo_t[i].sendSize = splitSize;
                    result = true;
                    splitSize = 0;
                }
                else
                {
                    m_sendInfo_t[i].sendFlag = false;
                }
            }

            if (result == true)
            {
                for (int i = 0; i < m_sendTextBox.Length; i++)
                {
                    m_sendTextBox[i].Enabled = false;
                    m_sendCheckBox[i].Enabled = false;
                }
            }

            return result;
        }

        private void ID_FREQUENCY_TIMER(object sender, EventArgs e)
        {
            MessageBox.Show("aa\r\n");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void ID_RECVLIST_CLEAR_Click(object sender, EventArgs e)
        {
            ID_RECV_TEXTBOX.Text = "";
        }

        private void ID_SENDCNT_CLEAR_Click(object sender, EventArgs e)
        {
            for (int i= 0; i<m_sendCntTextBox.Length; i++)
            {
                m_sendCntTextBox[i].Text = "0";
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            for (int i = 0; i < m_sendCntTextBox.Length; i++)
            {
                m_sendCntTextBox[i].Text = "0";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            String str = "apply list\r\n";

            for (int i =0; i<m_recvHeaderTextBox.Length; i++)
            {
                if (true == getHeader(i))
                {
                    str += "header #" + i.ToString() + "\r\n";
                }
            }

            MessageBox.Show(str);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            for (int i=0; i<m_recvCntTextBox.Length; i++)
            {
                m_recvCntTextBox[i].Text = "0";
            }
        }

        String m_str;

        private void button4_Click(object sender, EventArgs e)  /* save btn */
        {
            /* port info */
            m_formInfo_t[0].value = ID_BAUDRATE_COMBOBOX.SelectedIndex.ToString();
            m_formInfo_t[1].value = ID_DATABIT_COMBOBOX.SelectedIndex.ToString();
            m_formInfo_t[2].value = ID_STOPBIT_COMBOBOX.SelectedIndex.ToString();
            m_formInfo_t[3].value = ID_PARITY_COMBOBOX.SelectedIndex.ToString();

            /* send info */
            m_formInfo_t[4].value = SendList1.Text;
            m_formInfo_t[5].value = SendList2.Text;
            m_formInfo_t[6].value = SendList3.Text;
            m_formInfo_t[7].value = SendList4.Text;
            m_formInfo_t[8].value = SendList5.Text;
            m_formInfo_t[9].value = SendList6.Text;
            m_formInfo_t[10].value = ID_SEND_SPEED.Text;

            /* recv info */
            m_formInfo_t[11].value = ID_HEADER_TEXTBOX1.Text;
            m_formInfo_t[12].value = ID_HEADER_TEXTBOX2.Text;
            m_formInfo_t[13].value = ID_HEADER_TEXTBOX3.Text;
            m_formInfo_t[14].value = ID_HEADER_TEXTBOX4.Text;
            m_formInfo_t[15].value = ID_HEADER_TEXTBOX5.Text;
            m_formInfo_t[16].value = ID_HEADER_TEXTBOX6.Text;

            /* send check box */
            for (int i = 0; i < 6; i++)
            {
                if (m_sendCheckBox[i].Checked == true)
                    m_formInfo_t[17 + i].value = "true";
            }

            m_formInfo_t[23].value = ID_SENDCNT2_TEXT.Text;

            m_formInfo_t[24].value = ID_BYTETERM_TEXTBOX.Text;

            File.WriteAllText("./serial.ini", m_str, Encoding.Default);

            for (int i = 0; i < 24; i++)
            {
                if (!string.IsNullOrWhiteSpace(m_formInfo_t[i].value))
                {
                    m_str = m_formInfo_t[i].name + " " + m_formInfo_t[i].value + "\r\n";
                    File.AppendAllText("./serial.ini", m_str, Encoding.Default);
                }
            }            

            MessageBox.Show("save ok \"serial.ini\"");
            m_str = "";
        }

        private void FormInit()
        {
            for (int i=0; i<6; i++)
            {
                m_sendCheckBox[i].Checked = false;
                m_sendTextBox[i].Text = "";
                m_recvHeaderTextBox[i].Text = "";
                m_sendCntTextBox[i].Text = "0";
                m_recvCntTextBox[i].Text = "0";

                ID_RECV_TEXTBOX.Text = "";
                ID_BYTETERM_TEXTBOX.Text = "";
            }
        }

        private void button5_Click(object sender, EventArgs e)  /* load btn */
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = "(*.ini)|*.ini";
            ofd.ShowDialog();
            string path = ofd.FileName;
            string[] str;

            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            FormInit();

            StreamReader sr = new StreamReader(path);
            while (0 <= sr.Peek())
            {
                str = sr.ReadLine().Split(' ');

                if (m_formInfo_t[0].name.Equals(str[0]))
                {
                    ID_BAUDRATE_COMBOBOX.SelectedIndex = int.Parse(str[1].ToString());
                }
                else if (m_formInfo_t[1].name.Equals(str[0]))
                    ID_DATABIT_COMBOBOX.SelectedIndex = int.Parse(str[1].ToString());
                else if (m_formInfo_t[2].name.Equals(str[0]))
                    ID_STOPBIT_COMBOBOX.SelectedIndex = int.Parse(str[1].ToString());
                else if (m_formInfo_t[3].name.Equals(str[0]))
                    ID_PARITY_COMBOBOX.SelectedIndex = int.Parse(str[1].ToString());
                else if (m_formInfo_t[4].name.Equals(str[0]))
                {
                    m_sendTextBox[0].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_sendTextBox[0].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[5].name.Equals(str[0]))
                {
                    m_sendTextBox[1].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_sendTextBox[1].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[6].name.Equals(str[0]))
                {
                    m_sendTextBox[2].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_sendTextBox[2].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[7].name.Equals(str[0]))
                {
                    m_sendTextBox[3].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_sendTextBox[3].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[8].name.Equals(str[0]))
                {
                    m_sendTextBox[4].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_sendTextBox[4].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[9].name.Equals(str[0]))
                {
                    m_sendTextBox[5].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_sendTextBox[5].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[10].name.Equals(str[0]))
                {
                    ID_SEND_SPEED.Text = str[1];
                }
                else if (m_formInfo_t[11].name.Equals(str[0]))
                {
                    m_recvHeaderTextBox[0].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_recvHeaderTextBox[0].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[12].name.Equals(str[0]))
                {
                    m_recvHeaderTextBox[1].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_recvHeaderTextBox[1].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[13].name.Equals(str[0]))
                {
                    m_recvHeaderTextBox[2].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_recvHeaderTextBox[2].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[14].name.Equals(str[0]))
                {
                    m_recvHeaderTextBox[3].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_recvHeaderTextBox[3].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[15].name.Equals(str[0]))
                {
                    m_recvHeaderTextBox[4].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_recvHeaderTextBox[4].Text += str[i] + " ";
                    }
                }
                else if (m_formInfo_t[16].name.Equals(str[0]))
                {
                    m_recvHeaderTextBox[5].Clear();
                    for (int i = 1; i < str.Length; i++)
                    {
                        m_recvHeaderTextBox[5].Text += str[i] + " ";
                    }
                }

                else if (m_formInfo_t[17].name.Equals(str[0]))
                {
                    m_sendCheckBox[0].Checked = true;
                }

                else if (m_formInfo_t[18].name.Equals(str[0]))
                {
                    m_sendCheckBox[1].Checked = true;
                }

                else if (m_formInfo_t[19].name.Equals(str[0]))
                {
                    m_sendCheckBox[2].Checked = true;
                }

                else if (m_formInfo_t[20].name.Equals(str[0]))
                {
                    m_sendCheckBox[3].Checked = true;
                }

                else if (m_formInfo_t[21].name.Equals(str[0]))
                {
                    m_sendCheckBox[4].Checked = true;
                }

                else if (m_formInfo_t[22].name.Equals(str[0]))
                {
                    m_sendCheckBox[5].Checked = true;
                }

                else if (m_formInfo_t[23].name.Equals(str[0]))
                {
                    ID_SENDCNT2_TEXT.Text = str[1];
                }

                else if (m_formInfo_t[24].name.Equals(str[0]))
                {
                    ID_BYTETERM_TEXTBOX.Text = str[1];
                }
            }
            sr.Close();
            toolStripStatusLabel1.Text = path;
            MessageBox.Show("load ok");
        }
    }
}
