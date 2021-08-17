using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace forklift_rcs
{
    //无线通讯的控制字
    static class rc_controlword
    {
        public const byte RC_AGVSTATUS_ENABLE = 0x01;
        public const byte RC_AGVSTATUS_ERROR = 0x02;
        public const byte RC_AGVSTATUS_CHARGE = 0x04;
        public const byte RC_AGVSTATUS_RUN = 0x08;
        public const byte RC_AGVSTATUS_TASK = 0x10;
        public const byte RC_AGVSTATUS_MODE = 0x20;
        public const byte RC_AGVSTATUS_LS = 0x40;
        public const byte RC_AGVSTATUS_IS_FORK = 0x80;

        public const byte RC_SENSOR_STATUS_TORCH = 0x01;
        public const byte RC_SENSOR_STATUS_LEFTLIGHT = 0x02;
        public const byte RC_SENSOR_STATUS_RIGHTLIGHT = 0x04;
        public const byte RC_SENSOR_STATUS_LSZONE0 = 0x08;
        public const byte RC_SENSOR_STATUS_LSZONE1 = 0x10;
        public const byte RC_SENSOR_STATUS_LSZONE2 = 0x20;

        public const byte RC_TASK_STATE_STANDBY = 0x00;
        public const byte RC_TASK_STATE_RUN = 0x01;
        public const byte RC_TASK_STATE_FORKBOTTOM = 0x02;
        public const byte RC_TASK_STATE_FORKTOP = 0x03;

        public const byte RC_CMD_AKTIV = 0x01;
        public const byte RC_CMD_DEAKTIV = 0x02;
        public const byte RC_CMD_RESET = 0x03;
        public const byte RC_CMD_FORK = 0x04;
        public const byte RC_CMD_ASK = 0x07;
        public const byte RC_CMD_LS = 0x08;
        public const byte RC_CMD_MISSON_HEAD = 0x09;
        public const byte RC_CMD_MISSON_END = 0x0a;
        public const byte RC_CMD_PATH = 0x0b;
        public const byte RC_CMD_CHARGE = 0x0c;
        public const byte RC_CMD_CHARGE_OUT = 0x0d;

        public const UInt16 ErrorCodeESTOP = 0x0001;
        public const UInt16 ErrorCodeBatLow = 0x0002;
        public const UInt16 ErrorCodeBarrier = 0x0004;
        public const UInt16 ErrorCodeLostMap = 0x0008;
        public const UInt16 ErrorCodeCAN = 0x0010;
        public const UInt16 ErrorCodeBatError = 0x0020;
        public const UInt16 ErrorCodeFork = 0x0040;
        public const UInt16 ErrorCodeNaviFail = 0x0080;
        public const UInt16 ErrorCodeInitRoute = 0x0100;
        public const UInt16 ErrorCodeInitMap = 0x0200;
    }

    struct Forklift_Mean_Type
    {
        public byte id;
        public byte status;
        public UInt16 errorcode;
        public byte state;
        public float vel;
        public float px;
        public float py;
        public float yaw;
        public int forklevel;
        public int point_id;
        public byte sensor;
        public byte bat;

        public bool mission_run;
        public bool mission_fork;
        public UInt32 task_id;


        public Path_Type path;

    }

    struct Forklift_Mission_Type
    {
        public UInt16 req_id; //叉车的ID号
        public byte cmd_type;
        public double timestamp;
        public int mission_state;
        public byte cmd_val;
        public float px;
        public float py;
        public float vel;
    }


    class scheduler
    {
        public Forklift_Mission_Type mission_manage;//任务管理模块
        //叉车对象
        public forklift_obj forklift1;
        public forklift_obj forklift2;
        //路径规划对象
        public planner planner_obj;

        bool loop_is_run;

        public serialport port_obj; //串口通讯模块
        public scheduler()
        {
            mission_manage.timestamp = 0.0;
            mission_manage.cmd_type = 0;
            mission_manage.mission_state = 0;

            port_obj = new serialport();

            planner_obj = new planner();

            forklift1 = new forklift_obj(2, planner_obj.way_point_list);
            forklift2 = new forklift_obj(3, planner_obj.way_point_list);

            planner_obj.add_forklift(forklift1);
            planner_obj.add_forklift(forklift2);


            ThreadStart loop_thread_startup = new ThreadStart(loop);
            Thread loop_thread = new Thread(loop_thread_startup);
            loop_thread.IsBackground = true;
            loop_thread.Start();

            port_obj.open_port(9600, "COM3");

        }

        //计算时间戳
        public double getTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            Int64 tmp = Convert.ToInt64(ts.TotalMilliseconds);

            return ((double)tmp * 0.001);
        }


        //===========================================================================================
        public bool aktiv_forklift(UInt16 forklift_id)
        {
            if (mission_manage.mission_state != 0) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_AKTIV;
            mission_manage.req_id = forklift_id;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }

        }

        public bool deaktiv_forklift(UInt16 forklift_id)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_DEAKTIV;
            mission_manage.req_id = forklift_id;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }

        }

        public bool reset_forklift(UInt16 forklift_id)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_RESET;
            mission_manage.req_id = forklift_id;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }

        }

        public bool charge_forklift(UInt16 forklift_id)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_CHARGE;
            mission_manage.req_id = forklift_id;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        public bool end_charge_forklift(UInt16 forklift_id)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_CHARGE_OUT;
            mission_manage.req_id = forklift_id;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        public bool setfork_forklift(UInt16 forklift_id, byte level)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_FORK;
            mission_manage.req_id = forklift_id;
            mission_manage.cmd_val = level;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }

        }

        public bool getstatus_forklift(UInt16 forklift_id)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_ASK;
            mission_manage.req_id = forklift_id;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }

        }

        public bool set_laserscan_forklift(UInt16 forklift_id, byte on)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            mission_manage.cmd_type = rc_controlword.RC_CMD_LS;
            mission_manage.req_id = forklift_id;
            mission_manage.cmd_val = on;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }

        }



        //给叉车设定一条路径
        public bool set_drive_forklift(UInt16 forklift_id, Path_Type path)
        {
            if (mission_manage.mission_state != 0) return false;
            if (port_obj.is_open == false) return false;

            //检查另一个叉车是否占用
            Forklift_Mean_Type tmp_another_forklift;
            if (forklift_id == 2)
            {
                tmp_another_forklift = forklift2.forklift_data;
            }
            else
            {
                tmp_another_forklift = forklift1.forklift_data;
            }

            if (tmp_another_forklift.mission_run == true)
            {
                int N1 = path.waypoint.Count - 1;
                int N2 = tmp_another_forklift.path.waypoint.Count - 1;
                
                if (planner_obj.check_point_confliect(path.waypoint[N1], tmp_another_forklift.path.waypoint[N2]) == true) return false;
            }
            else
            {
                Point_Type point_tmp; //临时点对象
                point_tmp.px = tmp_another_forklift.px;
                point_tmp.py = tmp_another_forklift.py;

                int N1 = path.waypoint.Count - 1;
                int N2 = path.waypoint.Count - 2;

                if (planner_obj.check_point_on_line(point_tmp, path.waypoint[N1], path.waypoint[N2]) == true) return false;
            }



            mission_manage.cmd_type = rc_controlword.RC_CMD_MISSON_HEAD;
            mission_manage.req_id = forklift_id;

            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    break;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }

            System.Threading.Thread.Sleep(10);

            int iWP = 0;
            foreach (Point_Type waypoint in path.waypoint)
            {
                mission_manage.cmd_type = rc_controlword.RC_CMD_PATH;
                mission_manage.req_id = forklift_id;
                mission_manage.px = waypoint.px;
                mission_manage.py = waypoint.py;

                if(iWP >= path.vel.Count) mission_manage.vel = path.vel[iWP - 1];
                else mission_manage.vel = path.vel[iWP];
                while (true)
                {
                    if (mission_manage.mission_state == 2)
                    {
                        mission_manage.req_id = 0;
                        mission_manage.mission_state = 0;
                        break;
                    }
                    else if (mission_manage.mission_state == -1)
                    {
                        mission_manage.req_id = 0;
                        mission_manage.mission_state = 0;
                        return false;
                    }
                    System.Threading.Thread.Sleep(1);
                }
                System.Threading.Thread.Sleep(10);
                iWP++;
            }

            mission_manage.cmd_type = rc_controlword.RC_CMD_MISSON_END;
            mission_manage.req_id = forklift_id;
            while (true)
            {
                if (mission_manage.mission_state == 2)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;

                    if (forklift_id == 2) forklift1.forklift_data.path = path;
                    else if (forklift_id == 3) forklift2.forklift_data.path = path;

                    return true;
                }
                else if (mission_manage.mission_state == -1)
                {
                    mission_manage.req_id = 0;
                    mission_manage.mission_state = 0;
                    return false;
                }
                System.Threading.Thread.Sleep(1);
            }
        }
        //===========================================================================================
        //调度器循环工作
        public void loop() {
            loop_is_run = true;

            while (true)
            {

                if (mission_manage.mission_state == 0 && mission_manage.req_id > 0)
                {
                    generate_sendframe();
                    mission_manage.mission_state = 1;
                    mission_manage.timestamp = getTimeStamp();
                    if (port_obj.is_open==true)
                        port_obj.data_send();
                    else
                        mission_manage.mission_state = -1;

                }

                //检测发送数据帧后回收有没有超时
                if (mission_manage.mission_state == 1)
                {
                    double t = getTimeStamp();
                    if (t - mission_manage.timestamp > 0.5)
                    {
                        mission_manage.timestamp = 0.0;
                        mission_manage.mission_state = -1;
                    }

                }

                if (port_obj.rece_flag)
                {
                    parse_rece_frame();//解析接收数据帧
                    port_obj.rece_flag = false;
                }

                if (loop_is_run == false) return;

                System.Threading.Thread.Sleep(1);
            }
        }

        public void close_scheduler()
        {
            loop_is_run = false;
            port_obj.close_port();
        }

        //解析接收数据帧
        public void parse_rece_frame() {
            byte forklift_id = 0;
            byte cmd = 0;
            byte cmd_val = 0;

            if (mission_manage.mission_state != 1) {
                mission_manage.timestamp = 0.0;
                mission_manage.mission_state = -1;
                return;
            }

            //提取数据
            foreach (Data_Type item in port_obj.inter_obj.rece_frame_desc)
            {
                if (item.name == "agv_address")
                {
                    forklift_id = item.data[0];
                }
                else if (item.name == "pc_address")
                {
                    if (item.data[0] != 0x01) return;
                }
                else if (item.name == "cmd")
                {
                    cmd = item.data[0];
                }
                else if (item.name == "agv_status")
                {
                    cmd_val = item.data[0];
                }

            }

            //解析数据
            if (cmd == rc_controlword.RC_CMD_ASK)
            {
                if (forklift_id == 2) forklift1.update_mean(port_obj.inter_obj.rece_frame_desc);
                else if (forklift_id == 3) forklift2.update_mean(port_obj.inter_obj.rece_frame_desc);
                mission_manage.mission_state = 2;
                mission_manage.timestamp = 0.0;
            }
            else 
            {
                if (cmd_val == 0) mission_manage.mission_state = -1;
                else mission_manage.mission_state = 2;
                mission_manage.timestamp = 0.0;
            }


        }

        public void generate_sendframe() {
            foreach (Data_Type item in port_obj.inter_obj.send_frame_desc) {
                if (item.name == "agv_address")
                {
                    item.data[0] = (byte)mission_manage.req_id;
                }
                else if (item.name == "pc_address")
                {
                    item.data[0] = 0x01;
                }
                else if (item.name == "cmd")
                {
                    item.data[0] = mission_manage.cmd_type;
                }
                else if(item.name == "data")
                {
                    if (mission_manage.cmd_type != rc_controlword.RC_CMD_PATH)
                    {
                        item.data[0] = mission_manage.cmd_val;
                    }
                    else
                    {
                        Int16 tmp = (Int16)(mission_manage.px * 100.0f);
                        item.data[0] = (byte)(tmp & 0xff);
                        item.data[1] = (byte)((tmp>>8) & 0xff);
                        tmp = (Int16)(mission_manage.py * 100.0f);
                        item.data[2] = (byte)(tmp & 0xff);
                        item.data[3] = (byte)((tmp >> 8) & 0xff);
                        tmp = (Int16)(mission_manage.vel * 100.0f);
                        item.data[4] = (byte)(tmp & 0xff);
                        item.data[5] = (byte)((tmp >> 8) & 0xff);

                    }
                }
            }

        }

    }

    class forklift_obj {
        public Forklift_Mean_Type forklift_data;
        public List<WayPoint_Type> wap_point_map = new List<WayPoint_Type>();
        

        public forklift_obj(byte id, List<WayPoint_Type> wap_point_map)
        {
            //初始化叉车数据
            forklift_data.id = id;
            forklift_data.status = 0x00;
            forklift_data.point_id = -1;
            forklift_data.forklevel = 0;
            forklift_data.vel = 0.0f;
            forklift_data.px = 0.0f;
            forklift_data.py = 0.0f;
            forklift_data.yaw = 0.0f;
            forklift_data.state = 0x00;
            forklift_data.sensor = 0x00;
            forklift_data.errorcode = 0;
            forklift_data.bat = 0;

            forklift_data.mission_run = false;
            forklift_data.mission_fork = false;
            forklift_data.task_id = 0;

            this.wap_point_map = wap_point_map;
        }


        public bool get_agv_sensor(byte muster) {
            if ((forklift_data.sensor & muster)==0) return false;
            else return true;
        }

        public bool get_agv_state(byte muster) {
            if (forklift_data.state == muster) return true;
            else return false;
        }

        public bool get_agv_status(byte muster) {
            if ((forklift_data.status & muster) == 0) return false;
            else return true;
        }



        //解析叉车的当前数据
        public void update_mean(List<Data_Type> rece_dat) {
            foreach (Data_Type item in rece_dat){
                if (item.name == "agv_address")
                {
                    if (item.data[0] != forklift_data.id) return;
                }
                else if (item.name == "cmd")
                {
                    if (item.data[0] != rc_controlword.RC_CMD_ASK) return;
                }
                else if (item.name == "agv_status")
                {
                    forklift_data.status = item.data[0];
                    continue;
                }
                else if (item.name == "error_code")
                {
                    forklift_data.errorcode = BitConverter.ToUInt16(item.data.ToArray(), 0);
                    continue;
                }
                else if (item.name == "bat")
                {
                    forklift_data.bat = item.data[0];
                    continue;
                }
                else if (item.name == "task_state")
                {
                    forklift_data.state = item.data[0];
                    continue;
                }
                else if (item.name == "fork_level")
                {
                    forklift_data.forklevel = item.data[0];
                    continue;
                }
                else if (item.name == "agv_vel")
                {
                    forklift_data.vel = (float)BitConverter.ToInt16(item.data.ToArray(), 0) * 0.01f;
                    continue;
                }
                else if (item.name == "agv_pos_x")
                {
                    forklift_data.px = (float)BitConverter.ToInt16(item.data.ToArray(), 0) * 0.01f;
                    continue;
                }
                else if (item.name == "agv_pos_y")
                {
                    forklift_data.py = (float)BitConverter.ToInt16(item.data.ToArray(), 0) * 0.01f;
                    continue;
                }
                else if (item.name == "agv_yaw")
                {
                    forklift_data.yaw = (float)BitConverter.ToInt16(item.data.ToArray(), 0) * 0.1f;
                    continue;
                }
                else if (item.name == "sensor_status")
                {
                    forklift_data.sensor = item.data[0];
                    continue;
                }
            }

            //确定叉车是否在某个地图点附近
            Double min_rho = 20.0;
            int hit_point_id = -1;
            foreach (WayPoint_Type way_point in wap_point_map)
            {
                Double dx = (Double)(way_point.px - forklift_data.px);
                Double dy = (Double)(way_point.py - forklift_data.py);
                Double rho = Math.Sqrt(dx * dx + dy * dy);
                if (rho < min_rho) {
                    min_rho = rho;
                    hit_point_id = way_point.id;
                } 
            }

            if (min_rho < 0.4) forklift_data.point_id = hit_point_id;
            else forklift_data.point_id = -1;

            //检查叉车当前任务状态
            if ((forklift_data.status & rc_controlword.RC_AGVSTATUS_RUN) > 0x00)
            {
                forklift_data.mission_run = true;
            }
            else
            {
                if (forklift_data.mission_run == true && forklift_data.task_id > 0) forklift_data.task_id = 0;

                forklift_data.mission_run = false;

            }
            if ((forklift_data.status & rc_controlword.RC_AGVSTATUS_IS_FORK) > 0x00)
            {
                forklift_data.mission_fork = true;
            }
            else
            {
                if (forklift_data.mission_fork == true && forklift_data.task_id > 0) forklift_data.task_id = 0;
                forklift_data.mission_fork = false;
            }
        }


    }
}

