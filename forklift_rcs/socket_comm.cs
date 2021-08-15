using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;

namespace forklift_rcs
{

    struct data_manage_type
    {
        public int forklift_data_state;
        public JObject send_frame_status;
        public JObject send_frame_result;
        public JObject rece_frame;
    }

    class socket_comm
    {

        public string rece_muster = "{\"cmd_type\":\"task_download\", " +
                                    "\"agv_id\": \"0\", " +
                                    "\"taskid\": \"1000\",  " +
                                    "\"target\": \"31\", " +
                                    "\"type\": \"1\"}";


        public string send_muster_status = "{\"AgvID\":\"01\", " + 
                                            "\"State\": \"0\", " +
                                            "\"SensorStatus\": \"0\",  " +
                                            "\"Fault\": \"0\", " +
                                            "\"Power\": \"0\", " +
                                            "\"X\": \"0\", " +
                                            "\"Y\": \"0\", " +
                                            "\"Yaw\": \"0\", " +
                                            "\"TaskID\": \"0\", " +
                                            "\"UpdateTime\": \"0\"}";


        public string send_muster_result = "{\"code\":\"0\"}";

        public data_manage_type comm_data;

        int port = 2000;
        string host = "127.0.0.1";

        IPAddress ip;
        IPEndPoint endpoirnt;
        Socket socket_obj;


        //用于通信的Socket
        Socket socketSend;

        //将远程连接的客户端的IP地址和Socket存入集合中
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();

        //创建监听连接的线程
        Thread AcceptSocketThread;
        //接收客户端发送消息的线程
        Thread threadReceive;

        public socket_comm()
        {
            //初始化数据结构
            comm_data.forklift_data_state = 0;
            comm_data.send_frame_status = (JObject)JsonConvert.DeserializeObject(send_muster_status);
            comm_data.send_frame_result = (JObject)JsonConvert.DeserializeObject(send_muster_result);

            ip = IPAddress.Parse(host);//把ip地址字符串转换为IPAddress类型的实例
            endpoirnt = new IPEndPoint(ip, port);//用指定的端口和ip初始化IPEndPoint类的新实例
            socket_obj = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建一个socket对像

            socket_obj.Bind(endpoirnt);//绑定EndPoint对像（2000端口和ip地址）

            socket_obj.Listen(10);

            //开始监听进程
            AcceptSocketThread = new Thread(new ParameterizedThreadStart(listen_loop));
            AcceptSocketThread.IsBackground = true;
            AcceptSocketThread.Start(socket_obj);


        }

        public void listen_loop(object param)
        {
            Socket socket_main = param as Socket;
            while (true)
            {
                //等待客户端的连接，并且创建一个用于通信的Socket
                socketSend = socket_main.Accept();
                //获取远程主机的ip地址和端口号
                string strIp = socketSend.RemoteEndPoint.ToString();
                dicSocket.Add(strIp, socketSend);

                //定义接收客户端消息的线程
                threadReceive = new Thread(new ParameterizedThreadStart(send_loop));
                threadReceive.IsBackground = true;
                threadReceive.Start(socketSend);

            }
        }

        public void send_loop(object param)
        {
            Socket socket_main = param as Socket;
            string sendMsg;

            while (true)
            {
                
                //客户端连接成功后，服务器接收客户端发送的消息
                byte[] buffer = new byte[2048];
                int count = 0;
                //实际接收到的有效字节数
                try
                {
                    count = socketSend.Receive(buffer);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    socketSend.Close();
                    Console.WriteLine("connet falied!");
                    break;
                }

                
                if (count > 0)//count 表示客户端关闭，要退出循环  
                {
                    string str = Encoding.Default.GetString(buffer, 0, count);
                    comm_data.rece_frame = (JObject)JsonConvert.DeserializeObject(str);
                    comm_data.forklift_data_state = 1;
                    while (true)
                    {
                        if (comm_data.forklift_data_state >= 2 || comm_data.forklift_data_state == -1) break;
                        System.Threading.Thread.Sleep(1);
                    }


                    if (comm_data.forklift_data_state == 2)
                    {
                        comm_data.send_frame_result["code"] = "0";
                        sendMsg = comm_data.send_frame_result.ToString();
                    }
                    else if (comm_data.forklift_data_state == -1)
                    {
                        comm_data.send_frame_result["code"] = "-1";
                        sendMsg = comm_data.send_frame_result.ToString();
                    }
                    else
                    {
                        sendMsg = comm_data.send_frame_status.ToString();
                    }

                    //JObject jo1 = (JObject)JsonConvert.DeserializeObject(sendMsg);
                    //string tmp = jo1["Yaw"].ToString();


                    byte[] send_buffer = Encoding.Default.GetBytes(sendMsg);
                    socket_main.Send(send_buffer);
                    comm_data.forklift_data_state = 0;
                }
                System.Threading.Thread.Sleep(1);
            }
            

        }

        public void close_comm()
        {
            socket_obj.Close();
            socketSend.Close();
            //终止线程
            AcceptSocketThread.Abort();
            threadReceive.Abort();
        }
    }
}
