using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace forklift_rcs
{
    class rcs_console
    {
        scheduler scheduler_obj;
        socket_comm sock_obj;
        double timestamp;

        UInt16 ask_forklift_id;


        public rcs_console()
        {
            scheduler_obj = new scheduler();
            sock_obj = new socket_comm();

            timestamp = scheduler_obj.getTimeStamp();
            ask_forklift_id = 2;
        }

        public void loop()
        {
            UInt16 forklift_id;
            if (sock_obj.comm_data.forklift_data_state == 1) //接收到来自socket的请求
            {
                forklift_id = Convert.ToUInt16(sock_obj.comm_data.rece_frame["agv_id"].ToString());

                if (sock_obj.comm_data.rece_frame["cmd_type"].ToString() == "task_download")
                {
                    if (sock_obj.comm_data.rece_frame["type"].ToString() == "1")
                    {
                        //获得目标点的ID号
                        int target_station = Convert.ToInt32(sock_obj.comm_data.rece_frame["target"].ToString());
                        //生成轨迹
                        Path_Type path = scheduler_obj.planner_obj.find_path2loadstation(forklift_id, target_station);


                        bool is_ok = scheduler_obj.set_drive_forklift(forklift_id, path);
                        if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                        else sock_obj.comm_data.forklift_data_state = -1;
                    }
                    else if (sock_obj.comm_data.rece_frame["type"].ToString() == "2")
                    {
                        bool is_ok = scheduler_obj.setfork_forklift(forklift_id, 1);
                        if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                        else sock_obj.comm_data.forklift_data_state = -1;
                    }
                    else if (sock_obj.comm_data.rece_frame["type"].ToString() == "3")
                    {
                        bool is_ok = scheduler_obj.setfork_forklift(forklift_id, 0);
                        if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                        else sock_obj.comm_data.forklift_data_state = -1;
                    }
                    else if (sock_obj.comm_data.rece_frame["type"].ToString() == "4")
                    {
                        Path_Type path = scheduler_obj.planner_obj.find_path_back2start(forklift_id);
                        bool is_ok = scheduler_obj.set_drive_forklift(forklift_id, path);
                        if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                        else sock_obj.comm_data.forklift_data_state = -1;
                    }

                    UInt32 task_id = Convert.ToUInt32(sock_obj.comm_data.rece_frame["taskid"].ToString());

                    if (forklift_id == 2) scheduler_obj.forklift1.forklift_data.task_id = task_id;
                    else scheduler_obj.forklift2.forklift_data.task_id = task_id;


                }
                else if (sock_obj.comm_data.rece_frame["cmd_type"].ToString() == "task_cancel")
                {
                    bool is_ok = scheduler_obj.reset_forklift(forklift_id);
                    if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                    else sock_obj.comm_data.forklift_data_state = -1;
                }
                else if (sock_obj.comm_data.rece_frame["cmd_type"].ToString() == "agv_status")
                {
                    if (forklift_id == 2)
                    {
                        sock_obj.comm_data.send_frame_status["AgvID"] = "02";
                        sock_obj.comm_data.send_frame_status["State"] = scheduler_obj.forklift1.forklift_data.status.ToString();
                        sock_obj.comm_data.send_frame_status["SensorStatus"] = scheduler_obj.forklift1.forklift_data.sensor.ToString();
                        sock_obj.comm_data.send_frame_status["Fault"] = scheduler_obj.forklift1.forklift_data.errorcode.ToString();
                        sock_obj.comm_data.send_frame_status["Power"] = scheduler_obj.forklift1.forklift_data.bat.ToString();
                        sock_obj.comm_data.send_frame_status["X"] = scheduler_obj.forklift1.forklift_data.px.ToString();
                        sock_obj.comm_data.send_frame_status["Y"] = scheduler_obj.forklift1.forklift_data.py.ToString();
                        sock_obj.comm_data.send_frame_status["Yaw"] = scheduler_obj.forklift1.forklift_data.yaw.ToString();
                        sock_obj.comm_data.send_frame_status["TaskID"] = scheduler_obj.forklift1.forklift_data.task_id.ToString();

                    }
                    else
                    {
                        sock_obj.comm_data.send_frame_status["AgvID"] = "03";
                        sock_obj.comm_data.send_frame_status["State"] = scheduler_obj.forklift2.forklift_data.status.ToString();
                        sock_obj.comm_data.send_frame_status["SensorStatus"] = scheduler_obj.forklift2.forklift_data.sensor.ToString();
                        sock_obj.comm_data.send_frame_status["Fault"] = scheduler_obj.forklift2.forklift_data.errorcode.ToString();
                        sock_obj.comm_data.send_frame_status["Power"] = scheduler_obj.forklift2.forklift_data.bat.ToString();
                        sock_obj.comm_data.send_frame_status["X"] = scheduler_obj.forklift2.forklift_data.px.ToString();
                        sock_obj.comm_data.send_frame_status["Y"] = scheduler_obj.forklift2.forklift_data.py.ToString();
                        sock_obj.comm_data.send_frame_status["Yaw"] = scheduler_obj.forklift2.forklift_data.yaw.ToString();
                        sock_obj.comm_data.send_frame_status["TaskID"] = scheduler_obj.forklift2.forklift_data.task_id.ToString();
                    }
                    
                    sock_obj.comm_data.forklift_data_state = 3;

                }
                else if (sock_obj.comm_data.rece_frame["cmd_type"].ToString() == "aktiv_agv")
                {
                    bool is_ok = scheduler_obj.aktiv_forklift(forklift_id);
                    if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                    else sock_obj.comm_data.forklift_data_state = -1;
                }
                else if (sock_obj.comm_data.rece_frame["cmd_type"].ToString() == "deaktiv_agv")
                {
                    bool is_ok = scheduler_obj.deaktiv_forklift(forklift_id);
                    if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                    else sock_obj.comm_data.forklift_data_state = -1;
                }
                else if (sock_obj.comm_data.rece_frame["cmd_type"].ToString() == "charge")
                {
                    bool is_ok = scheduler_obj.charge_forklift(forklift_id);
                    if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                    else sock_obj.comm_data.forklift_data_state = -1;
                }
                else if (sock_obj.comm_data.rece_frame["cmd_type"].ToString() == "out_charge")
                {
                    bool is_ok = scheduler_obj.end_charge_forklift(forklift_id);
                    if (is_ok) sock_obj.comm_data.forklift_data_state = 2;
                    else sock_obj.comm_data.forklift_data_state = -1;
                }
            }
            else //在没有收到来自上一层的指令发送要求时，自己发送询问叉车状态的报文。
            {

                double act_time = scheduler_obj.getTimeStamp(); //当前时间

                if (act_time - timestamp > 0.4)//当前时间与上次隔了0.4秒则询问一次叉车状态
                {
                    bool is_ok = scheduler_obj.getstatus_forklift(ask_forklift_id); //询问叉车状态
                    timestamp = act_time;

                    if(is_ok) Console.WriteLine("ask status success");
                    else Console.WriteLine("ask status failed");

                    //叉车ID号切换
                    if (ask_forklift_id == 2) ask_forklift_id = 3;
                    else ask_forklift_id = 2;
                }
            }

        }

    }
}
