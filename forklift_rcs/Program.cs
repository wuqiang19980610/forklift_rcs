using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;

namespace forklift_rcs
{
    class Program
    {
        static void Main(string[] args)
        {
            rcs_console console_obj = new rcs_console();
            while (true)
            {
                console_obj.loop();
            }



        }


        public void test_planner()
        {
            planner planner_obj = new planner();

            forklift_obj forklift1 = new forklift_obj(2, planner_obj.way_point_list);
            forklift_obj forklift2 = new forklift_obj(3, planner_obj.way_point_list);

            planner_obj.add_forklift(forklift1);
            planner_obj.add_forklift(forklift2);

            forklift1.forklift_data.px = 8.0f;
            forklift1.forklift_data.py = 60.0f;
            forklift1.forklift_data.yaw = -1.57f;

            //Path_Type path = planner_obj.find_path2loadstation(2, 21);


            Path_Type path = planner_obj.find_path_back2start(2);

        }

        public void test_socket()
        {
            socket_comm obj = new socket_comm();

            while (true) {

            }
        }

        /*
            int ms = DateTime.Now.Millisecond;


            string jsonText3 = "{\"cmd_type\":\"task_download\", " +
                                "\"agv_id\": \"2\", " +
                                "\"taskid\": \"12345\",  " +
                                "\"target\": \"102\", " +
                                "\"type\": \"1\"}";


            JObject jo1 = (JObject)JsonConvert.DeserializeObject(jsonText3);
            string tmp = jo1["target"].ToString();
            tmp = jo1["cmd_type"].ToString();
            tmp = jo1["taskid"].ToString();

            jo1["taskid"] = "3";
            tmp = jo1["taskid"].ToString();


            int tmp_s32 = 10;
            float tmp_f32 = 12.34f;
            tmp = tmp_f32.ToString();

            string s = "4 3 5";
            string[] sArray = s.Split(new Char[] { ' ' });
            foreach (string i in sArray) {
                Console.WriteLine(i);
            }

            Int16 tmp = -3;
            byte b1 = (byte)(tmp & 0xff);
            byte b2 = (byte)((tmp>>8) & 0xff);

            scheduler sch_obj = new scheduler();


            System.Threading.Thread.Sleep(10);

            sch_obj.getstatus_forklift(3);
            ms = DateTime.Now.Millisecond;
            sch_obj.aktiv_forklift(3);
            int dt = DateTime.Now.Millisecond - ms;

            sch_obj.set_laserscan_forklift(3,1);

            System.Threading.Thread.Sleep(5000);

            sch_obj.close_scheduler();

            Program main_programm = new Program();
            main_programm.test_socket();
         */


    }
}
