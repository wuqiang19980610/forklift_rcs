using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.IO;

namespace forklift_rcs
{
    class serialport : SerialPort{
        public struct Buffer_Type
        {
            public int len;
            public int read_ptr;
            public int wirte_ptr;
            public byte[] buff;
        }

        private Buffer_Type send_buff;
        private Buffer_Type rece_buff;

        public DataInterface inter_obj;

        public bool rece_flag;
        public bool is_open;

        //构造函数
        public serialport()
        {
            PortName = " ";
            BaudRate = 9600;
            Parity = Parity.None;
            StopBits = StopBits.One;
            DataBits = 8;
            ReceivedBytesThreshold = 1;

            WriteTimeout = 300;
            ReadTimeout = 300;



            inter_obj = new DataInterface();

            this.send_buff.len = inter_obj.send_len;
            this.send_buff.read_ptr = 0;
            this.send_buff.wirte_ptr = 0;
            this.send_buff.buff = new byte[inter_obj.send_len];

            this.rece_buff.len = inter_obj.rece_len;
            this.rece_buff.read_ptr = 0;
            this.rece_buff.wirte_ptr = 0;
            this.rece_buff.buff = new byte[inter_obj.rece_len];

            DataReceived += new SerialDataReceivedEventHandler(data_rece_cb);
            

            is_open = false;
            rece_flag = false;
        }

        //打开串口
        public int open_port(int baudrate, string com){
            PortName = com;
            BaudRate = baudrate;

            try
            {
                Open();
                is_open = true;
                return 0;
            }
            catch (System.IO.IOException e)
            {
                is_open = false;
                return -1;
            }
        }
        //关闭串口
        public void close_port(){
            if (is_open == false) return;
            Close();
            is_open = false;
        }

        //串口接收数据的回调函数
        private void data_rece_cb(object sender, SerialDataReceivedEventArgs e)
        {
            int availCount = BytesToRead;

            if (availCount > 0) {
                for (int i = 0; i < availCount; i++) {
                    byte data = (byte)ReadByte();
                    rece_buff.buff[rece_buff.read_ptr] = data;
                    rece_buff.read_ptr++;
                }
            }


            if (rece_buff.read_ptr >= rece_buff.len)
            {
                rece_buff.read_ptr = 0;
                inter_obj.SetReceData(rece_buff.buff);
                rece_flag = true;
            }

            Console.WriteLine(availCount);
        }

        //发送一帧
        public void data_send()
        {
            send_buff.buff = inter_obj.GetSendData();
            Write(send_buff.buff, 0, send_buff.len);
            send_buff.wirte_ptr = 0;
        }

    }

}
