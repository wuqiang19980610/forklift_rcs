using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

/* waypoint 类型
 0：充电点
 1：待命点
 2: 路径点
 3：装载点     
*/



namespace forklift_rcs
{
    struct Path_Type
    {
        public bool is_ok;
        public List<Point_Type> waypoint;
        public List<float> vel;
    }


    struct Point_Type
    {
        public float px;
        public float py;
    }



    struct WayPoint_Type
    {
        public int id;
        public float px;
        public float py;
        public int point_type;
        public List<int> neighbor;
        public int line;
    }

    struct Line_Param_Type
    {
        public float Ka;
        public float Kb;
        public float Kc;
    }


    struct Line_Type
    {
        public int id;

        public float l1_start_px;
        public float l1_start_py;
        public float l1_end_px;
        public float l1_end_py;

        public float l2_start_px;
        public float l2_start_py;
        public float l2_end_px;
        public float l2_end_py;

        public Line_Param_Type l1_param;
        public Line_Param_Type l2_param;

    }

    static class vel_list
    {
        public const float vel_normal = 0.5f;
        public const float vel_charge = 0.1f;
        public const float vel_load = 0.2f;
    }


    class planner
    {
        public string xml_file_path = "D:\\forklift\\forklift_rcs\\forklift_rcs\\map_route.xml";
        public List<WayPoint_Type> way_point_list = new List<WayPoint_Type>();
        public List<WayPoint_Type> load_point_list = new List<WayPoint_Type>();

        List<Line_Type> road_list = new List<Line_Type>();

        List<forklift_obj> forklift_list = new List<forklift_obj>();

        public planner()
        {
            XmlDocument xml_obj = new XmlDocument(); //新xml工具对象

            //导入指定xml文件
            xml_obj.Load(xml_file_path);

            XmlNode root = xml_obj.SelectSingleNode("/Map");
            XmlNodeList childlist = root.ChildNodes;

            foreach (System.Xml.XmlNode item in childlist)
            {
                if (item.Name == "Route")
                {
                    XmlNodeList pointlist = item.ChildNodes;
                    foreach (System.Xml.XmlNode iwp in pointlist)
                    {
                        WayPoint_Type wp_tmp;
                        wp_tmp.id = Convert.ToInt32(iwp.Attributes["Nr"].Value);

                        string[] pos_str = iwp.Attributes["pos"].Value.Split(new Char[] { ' ' });
                        wp_tmp.px = Convert.ToSingle(pos_str[0]);
                        wp_tmp.py = Convert.ToSingle(pos_str[1]);

                        string[] neighbor_str = iwp.Attributes["neighbor"].Value.Split(new Char[] { ' ' });

                        wp_tmp.neighbor = new List<int>();
                        foreach (string iP in neighbor_str)
                        {
                            wp_tmp.neighbor.Add(Convert.ToInt32(iP));
                        }
                        wp_tmp.point_type = Convert.ToInt32(iwp.Attributes["type"].Value);

                        wp_tmp.line = Convert.ToInt32(iwp.Attributes["line"].Value);

                        if(wp_tmp.point_type == 3)
                            load_point_list.Add(wp_tmp);
                        else
                            way_point_list.Add(wp_tmp);

                    }
                }
                else if(item.Name == "Path")
                {
                    XmlNodeList pointlist = item.ChildNodes;
                    foreach (System.Xml.XmlNode iwp in pointlist)
                    {
                        Line_Type line_tmp;
                        line_tmp.id = Convert.ToInt32(iwp.Attributes["Nr"].Value);

                        string[] pos_str = iwp.Attributes["l1_pos_start"].Value.Split(new Char[] { ' ' });
                        line_tmp.l1_start_px = Convert.ToSingle(pos_str[0]);
                        line_tmp.l1_start_py = Convert.ToSingle(pos_str[1]);

                        pos_str = iwp.Attributes["l1_pos_end"].Value.Split(new Char[] { ' ' });
                        line_tmp.l1_end_px = Convert.ToSingle(pos_str[0]);
                        line_tmp.l1_end_py = Convert.ToSingle(pos_str[1]);

                        pos_str = iwp.Attributes["l2_pos_start"].Value.Split(new Char[] { ' ' });
                        line_tmp.l2_start_px = Convert.ToSingle(pos_str[0]);
                        line_tmp.l2_start_py = Convert.ToSingle(pos_str[1]);

                        pos_str = iwp.Attributes["l2_pos_end"].Value.Split(new Char[] { ' ' });
                        line_tmp.l2_end_px = Convert.ToSingle(pos_str[0]);
                        line_tmp.l2_end_py = Convert.ToSingle(pos_str[1]);

                        line_tmp.l1_param.Ka = line_tmp.l1_end_py - line_tmp.l1_start_py;
                        line_tmp.l1_param.Kb = line_tmp.l1_start_px - line_tmp.l1_end_px;
                        line_tmp.l1_param.Kc = line_tmp.l1_end_px * line_tmp.l1_start_py - line_tmp.l1_start_px * line_tmp.l1_end_py;

                        line_tmp.l2_param.Ka = line_tmp.l2_end_py - line_tmp.l2_start_py;
                        line_tmp.l2_param.Kb = line_tmp.l2_start_px - line_tmp.l2_end_px;
                        line_tmp.l2_param.Kc = line_tmp.l2_end_px * line_tmp.l2_start_py - line_tmp.l2_start_px * line_tmp.l2_end_py;

                        /*line_obj.A = p2[1] - p1[1];
                        line_obj.B = p1[0] - p2[0];
                        line_obj.C = p2[0] * p1[1] - p1[0] * p2[1];*/
                        road_list.Add(line_tmp);

                    }

                }
            }
        }

        public void add_forklift(forklift_obj forklift) {
            forklift_list.Add(forklift);
        }


        //生成去装载点的路径
        public Path_Type find_path2loadstation(UInt16 forklift_id, int station_id) {
            //初始化一个路径对象
            Path_Type path_res;
            path_res.is_ok = false;
            path_res.waypoint = new List<Point_Type>();
            path_res.vel = new List<float>();
            //找出相应的叉车对象
            forklift_obj forklift_tmp = forklift_list[0];
            bool get_obj = false;
            foreach (forklift_obj item in forklift_list)
            {
                if (item.forklift_data.id == forklift_id)
                {
                    forklift_tmp = item;
                    get_obj = true;
                }
            }
            //没有找到叉车对象则退出
            if (get_obj == false) return path_res;
            //找到目标的坐标点信息
            WayPoint_Type station_info = get_waypoint_info(station_id);
            //如果目标信息不存在则退出
            if(station_info.id == -1) return path_res;



            int act_point_id = get_act_waypoint(forklift_tmp);//尝试获取叉车当前所在的路径点

            WayPoint_Type tmp_point;//定义一个临时路径点对象
            Point_Type point_tmp; //临时点对象

            float orient; //叉车初始与路径的方向
            int road_id; //当前叉车所在路径的ID
            Line_Type act_road_info; //当前路径的信息

            if (act_point_id > 0) //路径点有效
            {
                tmp_point = get_waypoint_info(act_point_id);
                point_tmp.px = tmp_point.px;
                point_tmp.py = tmp_point.py;

                path_res.waypoint.Add(point_tmp);
                road_id = tmp_point.line; //叉车当前的路径号
              
                act_road_info = get_road_info(road_id);//获取当前路径对象
                //orient = get_orient2road(forklift_tmp, act_road_info); //确定叉车与当前路径的夹角
                
                if (act_point_id > 10) {
                    path_res.vel.Add(vel_list.vel_load);
                    point_tmp = cal_loadpoint_on_road(act_road_info, tmp_point, forklift_id);
                    path_res.waypoint.Add(point_tmp);
                    path_res.vel.Add(vel_list.vel_normal);
                }
                else if (act_point_id > 4) {
                    path_res.vel.Add(vel_list.vel_normal);
                }
                else if (act_point_id > 2) {
                    path_res.vel.Add(-vel_list.vel_normal);
                    tmp_point = get_waypoint_info(tmp_point.neighbor[1]);
                    point_tmp.px = tmp_point.px;
                    point_tmp.py = tmp_point.py;

                    road_id = tmp_point.line;
                    act_road_info = get_road_info(road_id);//获取当前路径对象

                    path_res.waypoint.Add(point_tmp);
                    path_res.vel.Add(vel_list.vel_normal);
                } 
                else return path_res; //如果叉车位于充电点则退出
            }
            else //叉车不在路径点而是某条路径上
            {
                road_id = get_act_road(forklift_tmp);//获取当前路径号
                if(road_id < 0) return path_res; //路径号不匹配则退出
                act_road_info = get_road_info(road_id);//获取当前路径对象
                //orient = get_orient2road(forklift_tmp, act_road_info); //确定叉车与当前路径的夹角

                tmp_point = get_waypoint_info(station_id);
                tmp_point.px = forklift_tmp.forklift_data.px;
                tmp_point.py = forklift_tmp.forklift_data.py;
                point_tmp = cal_loadpoint_on_road(act_road_info, tmp_point, forklift_id);
                path_res.waypoint.Add(point_tmp);

                path_res.vel.Add(vel_list.vel_normal);
            }

            //开始添加接下来的路径点
            int next_road_id;
            Line_Type next_road_info; //下一条路径的信息

            if (road_id == 88)//叉车位于起始路径上
            {
                next_road_id = 1;
                next_road_info = get_road_info(next_road_id);//获取下一条路径对象

                point_tmp = find_cross_point(act_road_info, next_road_info, forklift_tmp.forklift_data.id);
                path_res.waypoint.Add(point_tmp);
                path_res.vel.Add(vel_list.vel_normal);

                road_id = next_road_id;
                act_road_info = get_road_info(road_id);//获取当前路径对象
            }

            int inc;
            if (station_info.line >= road_id) inc = 1;
            else inc = -1;
                

            while (true)
            {
                if (road_id == station_info.line) break;

                //更新下一条路径
                next_road_id = road_id + inc;
                next_road_info = get_road_info(next_road_id);//获取下一条路径对象
                point_tmp = find_cross_point(act_road_info, next_road_info, forklift_tmp.forklift_data.id);
                path_res.waypoint.Add(point_tmp);
                path_res.vel.Add(vel_list.vel_normal);
                //更新当前路径
                road_id = next_road_id;
                act_road_info = get_road_info(road_id);
            }


            point_tmp = cal_loadpoint_on_road(act_road_info, station_info, forklift_id);
            path_res.waypoint.Add(point_tmp);
            path_res.vel.Add(-vel_list.vel_load);
            point_tmp.px = station_info.px;
            point_tmp.py = station_info.py;
            path_res.waypoint.Add(point_tmp);
            path_res.is_ok = true;


            //检查最初的速度方向是否正常
            orient = get_orient_in2points(forklift_tmp, path_res.waypoint[0], path_res.waypoint[1]);
            if (orient > 0.0)
            {
                if (path_res.vel[0] < 0.0) path_res.vel[0] = -path_res.vel[0];
            }
            else
            {
                if (path_res.vel[0] > 0.0) path_res.vel[0] = -path_res.vel[0];
            }

            return path_res;
        }


        //生成回到起始点的路径
        public Path_Type find_path_back2start(UInt16 forklift_id)
        {
            //初始化一个路径对象
            Path_Type path_res;
            path_res.is_ok = false;
            path_res.waypoint = new List<Point_Type>();
            path_res.vel = new List<float>();
            //找出相应的叉车对象
            forklift_obj forklift_tmp = forklift_list[0];
            bool get_obj = false;
            foreach (forklift_obj item in forklift_list)
            {
                if (item.forklift_data.id == forklift_id)
                {
                    forklift_tmp = item;
                    get_obj = true;
                }
            }
            //没有找到叉车对象则退出
            if (get_obj == false) return path_res;
            //找到目标的坐标点信息
            int target_id;
            if (forklift_tmp.forklift_data.id == 2) target_id = 3;
            else target_id = 4;
            WayPoint_Type target_info = get_waypoint_info(target_id);

            int act_point_id = get_act_waypoint(forklift_tmp);//尝试获取叉车当前所在的路径点

            WayPoint_Type tmp_point;//定义一个临时路径点对象
            Point_Type point_tmp; //临时点对象

            float orient; //叉车初始与路径的方向
            int road_id; //当前叉车所在路径的ID
            Line_Type act_road_info; //当前路径的信息

            if (act_point_id > 0) //路径点有效
            {
                tmp_point = get_waypoint_info(act_point_id);
                point_tmp.px = tmp_point.px;
                point_tmp.py = tmp_point.py;

                path_res.waypoint.Add(point_tmp);
                road_id = tmp_point.line; //叉车当前的路径号
                act_road_info = get_road_info(road_id);//获取当前路径对象

                if (act_point_id > 10)
                {
                    path_res.vel.Add(vel_list.vel_load);
                    point_tmp = cal_loadpoint_on_road(act_road_info, tmp_point, forklift_id);
                    path_res.waypoint.Add(point_tmp);
                    path_res.vel.Add(vel_list.vel_normal);
                }
                else if (act_point_id > 4)
                {
                    path_res.vel.Add(vel_list.vel_normal);
                    tmp_point = get_waypoint_info(tmp_point.neighbor[0]);
                    point_tmp.px = tmp_point.px;
                    point_tmp.py = tmp_point.py;
                    path_res.waypoint.Add(point_tmp);
                    path_res.is_ok = true;
                    return path_res;
                }
                else if (act_point_id <= 2) {
                    path_res.vel.Add(-vel_list.vel_load);
                    tmp_point = get_waypoint_info(tmp_point.neighbor[0]);
                    point_tmp.px = tmp_point.px;
                    point_tmp.py = tmp_point.py;
                    path_res.waypoint.Add(point_tmp);
                    path_res.is_ok = true;
                    return path_res;
                }

                else return path_res; //如果叉车位于充电点或者起始点则退出
            }
            else //叉车不在路径点而是某条路径上
            {
                road_id = get_act_road(forklift_tmp);//获取当前路径号
                if (road_id < 0) return path_res; //路径号不匹配则退出
                act_road_info = get_road_info(road_id);//获取当前路径对象
                //orient = get_orient2road(forklift_tmp, act_road_info); //确定叉车与当前路径的夹角

                tmp_point = get_waypoint_info(0);
                tmp_point.px = forklift_tmp.forklift_data.px;
                tmp_point.py = forklift_tmp.forklift_data.py;
                point_tmp = cal_loadpoint_on_road(act_road_info, tmp_point, forklift_id);
                path_res.waypoint.Add(point_tmp);

                path_res.vel.Add(vel_list.vel_normal);
            }
                
            //开始添加接下来的路径点
            int next_road_id;
            Line_Type next_road_info; //下一条路径的信息

            if (road_id != 88)//叉车不位于起始路径上
            {
                while (true)
                {
                    if (road_id == 88) break;

                    //更新下一条路径
                    if (road_id > 1) next_road_id = road_id - 1;
                    else next_road_id = 88;

                    next_road_info = get_road_info(next_road_id);//获取下一条路径对象
                    point_tmp = find_cross_point(act_road_info, next_road_info, forklift_tmp.forklift_data.id);
                    path_res.waypoint.Add(point_tmp);
                    path_res.vel.Add(vel_list.vel_normal);

                    //更新当前路径
                    road_id = next_road_id;
                    act_road_info = get_road_info(road_id);
                }
            }

            tmp_point = get_waypoint_info(target_info.neighbor[1]);
            point_tmp.px = tmp_point.px;
            point_tmp.py = tmp_point.py;
            path_res.waypoint.Add(point_tmp);
            path_res.vel.Add(vel_list.vel_normal);

            point_tmp.px = target_info.px;
            point_tmp.py = target_info.py;
            path_res.waypoint.Add(point_tmp);
            path_res.is_ok = true;

            //检查最初的速度方向是否正常
            orient = get_orient_in2points(forklift_tmp, path_res.waypoint[0], path_res.waypoint[1]);
            if (orient > 0.0)
            {
                if (path_res.vel[0] < 0.0) path_res.vel[0] = -path_res.vel[0];
            }
            else
            {
                if (path_res.vel[0] > 0.0) path_res.vel[0] = -path_res.vel[0];
            }

            return path_res;
        }


        //生成去充电桩的路径
        public Path_Type find_path2charge(UInt16 forklift_id)
        {
            //初始化一个路径对象
            Path_Type path_res;
            path_res.is_ok = false;
            path_res.waypoint = new List<Point_Type>();
            path_res.vel = new List<float>();
            //找出相应的叉车对象
            forklift_obj forklift_tmp = forklift_list[0];
            bool get_obj = false;
            foreach (forklift_obj item in forklift_list)
            {
                if (item.forklift_data.id == forklift_id)
                {
                    forklift_tmp = item;
                    get_obj = true;
                }
            }

            //没有找到叉车对象则退出
            if (get_obj == false) return path_res;

            int act_point_id = get_act_waypoint(forklift_tmp);//尝试获取叉车当前所在的路径点
            if(act_point_id > 4 || act_point_id < 3) return path_res;
            WayPoint_Type act_point_info = get_waypoint_info(act_point_id);


            //找到目标的坐标点信息
            int target_id = act_point_info.neighbor[0];
            WayPoint_Type target_info = get_waypoint_info(target_id);

            Point_Type point_tmp; //临时点对象

            point_tmp.px = act_point_info.px;
            point_tmp.py = act_point_info.py;
            path_res.waypoint.Add(point_tmp);
            path_res.vel.Add(vel_list.vel_charge);


            point_tmp.px = target_info.px;
            point_tmp.py = target_info.py;
            path_res.waypoint.Add(point_tmp);
            path_res.is_ok = true;

            return path_res;
        }

        //生成离开充电桩的路径
        public Path_Type find_path2chargeout(UInt16 forklift_id)
        {
            //初始化一个路径对象
            Path_Type path_res;
            path_res.is_ok = false;
            path_res.waypoint = new List<Point_Type>();
            path_res.vel = new List<float>();
            //找出相应的叉车对象
            forklift_obj forklift_tmp = forklift_list[0];
            bool get_obj = false;
            foreach (forklift_obj item in forklift_list)
            {
                if (item.forklift_data.id == forklift_id)
                {
                    forklift_tmp = item;
                    get_obj = true;
                }
            }

            //没有找到叉车对象则退出
            if (get_obj == false) return path_res;

            int act_point_id = get_act_waypoint(forklift_tmp);//尝试获取叉车当前所在的路径点
            if (act_point_id > 2) return path_res;
            WayPoint_Type act_point_info = get_waypoint_info(act_point_id);


            //找到目标的坐标点信息
            int target_id = act_point_info.neighbor[0];
            WayPoint_Type target_info = get_waypoint_info(target_id);

            Point_Type point_tmp; //临时点对象

            point_tmp.px = act_point_info.px;
            point_tmp.py = act_point_info.py;
            path_res.waypoint.Add(point_tmp);
            path_res.vel.Add(-vel_list.vel_charge);


            point_tmp.px = target_info.px;
            point_tmp.py = target_info.py;
            path_res.waypoint.Add(point_tmp);
            path_res.is_ok = true;

            return path_res;
        }




        public int get_act_waypoint(forklift_obj forklift)
        {
            //确定叉车是否在某个地图点附近
            Double min_rho = 20.0;
            int hit_point_id = -1;
            foreach (WayPoint_Type way_point in way_point_list)
            {
                Double dx = (Double)(way_point.px - forklift.forklift_data.px);
                Double dy = (Double)(way_point.py - forklift.forklift_data.py);
                Double rho = Math.Sqrt(dx * dx + dy * dy);
                if (rho < min_rho)
                {
                    min_rho = rho;
                    hit_point_id = way_point.id;
                }
            }

            if (min_rho < 0.4) return hit_point_id;

            min_rho = 20.0;
            foreach (WayPoint_Type way_point in load_point_list)
            {
                Double dx = (Double)(way_point.px - forklift.forklift_data.px);
                Double dy = (Double)(way_point.py - forklift.forklift_data.py);
                Double rho = Math.Sqrt(dx * dx + dy * dy);
                if (rho < min_rho)
                {
                    min_rho = rho;
                    hit_point_id = way_point.id;
                }
            }

            if (min_rho < 0.4) return hit_point_id;
            else return -1;
        }


        public int get_act_road(forklift_obj forklift)
        {
            int hit_line_id = -1;
            double min_rho = 20.0;
            double Dist = 0;
            double rho = 0;


            foreach (Line_Type road in road_list) {
                if (forklift.forklift_data.id == 2) {
                    Dist = Math.Sqrt(road.l1_param.Ka * road.l1_param.Ka + road.l1_param.Kb * road.l1_param.Kb);
                    rho = Math.Abs(road.l1_param.Ka * forklift.forklift_data.px + road.l1_param.Kb * forklift.forklift_data.py + road.l1_param.Kc);
                }
                else {
                    Dist = Math.Sqrt(road.l2_param.Ka * road.l2_param.Ka + road.l2_param.Kb * road.l2_param.Kb);
                    rho = Math.Abs(road.l2_param.Ka * forklift.forklift_data.px + road.l2_param.Kb * forklift.forklift_data.py + road.l2_param.Kc);
                }

                if (rho < min_rho)
                {
                    min_rho = rho;
                    hit_line_id = road.id;
                }

            }

            if (min_rho < 0.5) return hit_line_id;
            else return -1;

            /*float D=sqrt(linefuc[0]*linefuc[0]+linefuc[1]*linefuc[1]);
            float dist=fabsf((linefuc[0]*P.x+linefuc[1]*P.y+linefuc[2])/D);*/
        }

        public WayPoint_Type get_waypoint_info(int id)
        {
            WayPoint_Type res;
            res.id = -1;
            res.line = -1;
            res.px = 0.0f;
            res.py = 0.0f;
            res.neighbor = new List<int>();
            res.point_type = 0;

            if (id < 10)
            {
                foreach (WayPoint_Type iP in way_point_list)
                {
                    if (iP.id == id) return iP;
                }
                return res;

            }
            else {
                foreach (WayPoint_Type iP in load_point_list)
                {
                    if (iP.id == id) return iP;
                }
                return res;
            }
        }

        public Line_Type get_road_info(int id)
        {
            Line_Type res;
            res.id = -1;
            res.l1_start_px = 0.0f;
            res.l1_start_py = 0.0f;
            res.l2_start_px = 0.0f;
            res.l2_start_py = 0.0f;

            res.l1_end_px = 0.0f;
            res.l1_end_py = 0.0f;
            res.l2_end_px = 0.0f;
            res.l2_end_py = 0.0f;

            Line_Param_Type l_param;
            l_param.Ka = 0.0f;
            l_param.Kb = 0.0f;
            l_param.Kc = 0.0f;

            res.l1_param = l_param;
            res.l2_param = l_param;

            foreach (Line_Type iLine in road_list)
            {
                if (iLine.id == id) return iLine;
            }
            return res;
        }

        public Point_Type cal_loadpoint_on_road(Line_Type road, WayPoint_Type loadpoint, int forklift_id)
        {
            float vec_x, vec_y;
            float lineA, lineB, lineC;

            if (forklift_id == 2)
            {
                vec_x = road.l1_end_px - road.l1_start_px;
                vec_y = road.l1_end_py - road.l1_start_py;
                lineA = road.l1_param.Ka;
                lineB = road.l1_param.Kb;
                lineC = road.l1_param.Kc;
            }
            else {
                vec_x = road.l2_end_px - road.l2_start_px;
                vec_y = road.l2_end_py - road.l2_start_py;
                lineA = road.l2_param.Ka;
                lineB = road.l2_param.Kb;
                lineC = road.l2_param.Kc;
            }

            float det = vec_x * lineB - vec_y * lineA;

            float H_inv_11 = -vec_y / det;
            float H_inv_12 = -lineB / det;
            float H_inv_21 = vec_x / det;
            float H_inv_22 = lineA / det;

            float b1 = -lineC;
            float b2 = -vec_x * loadpoint.px - vec_y * loadpoint.py;


            Point_Type res;
            res.px = H_inv_11 * b1 + H_inv_12 * b2;
            res.py = H_inv_21 * b1 + H_inv_22 * b2;

            return res;
        }

        public Point_Type find_cross_point(Line_Type line1, Line_Type line2, int forklift_id)
        {
            float L1_Ka, L1_Kb, L1_Kc;
            float L2_Ka, L2_Kb, L2_Kc;

            if (forklift_id == 2)
            {
                L1_Ka = line1.l1_param.Ka;
                L1_Kb = line1.l1_param.Kb;
                L1_Kc = line1.l1_param.Kc;

                L2_Ka = line2.l1_param.Ka;
                L2_Kb = line2.l1_param.Kb;
                L2_Kc = line2.l1_param.Kc;
            }
            else
            {
                L1_Ka = line1.l2_param.Ka;
                L1_Kb = line1.l2_param.Kb;
                L1_Kc = line1.l2_param.Kc;

                L2_Ka = line2.l2_param.Ka;
                L2_Kb = line2.l2_param.Kb;
                L2_Kc = line2.l2_param.Kc;
            }

            Point_Type res;
            float det = L1_Ka * L2_Kb - L1_Kb * L2_Ka;
            float A_inv_11 = L2_Kb / det;
            float A_inv_12 = -L1_Kb / det;
            float A_inv_21 = -L2_Ka / det;
            float A_inv_22 = L1_Ka / det;

            res.px = -L1_Kc * A_inv_11 - L2_Kc * A_inv_12;
            res.py = -L1_Kc * A_inv_21 - L2_Kc * A_inv_22;

            return res;
        }

        public float get_orient2road(forklift_obj forklift, Line_Type road)
        {
            double p2_x, p2_y, p1_x, p1_y;
            if (forklift.forklift_data.point_id == 2)
            {
                p1_x = road.l1_start_px;
                p1_y = road.l1_start_py;
                p2_x = road.l1_end_px;
                p2_y = road.l1_end_py;
            }
            else
            {
                p1_x = road.l2_start_px;
                p1_y = road.l2_start_py;
                p2_x = road.l2_end_px;
                p2_y = road.l2_end_py;
            }

            double dx = p2_x - p1_x;
            double dy = p2_y - p1_y;

            double path_heading = Math.Atan2(dy, dx);

            double delta_yaw = path_heading - forklift.forklift_data.yaw;

            while (delta_yaw > Math.PI)
            {
                delta_yaw = delta_yaw - 2.0 * Math.PI;
            }

            while (delta_yaw < -Math.PI)
            {
                delta_yaw = delta_yaw + 2.0 * Math.PI;
            }

            if (Math.Abs(delta_yaw) < (Math.PI / 2.0)) return 1.0f;
            else return -1.0f;

        }


        public float get_orient_in2points(forklift_obj forklift, Point_Type point1, Point_Type point2)
        {
            double p2_x, p2_y, p1_x, p1_y;

            p1_x = point1.px;
            p1_y = point1.py;
            p2_x = point2.px;
            p2_y = point2.py; 


            double dx = p2_x - p1_x;
            double dy = p2_y - p1_y;

            double path_heading = Math.Atan2(dy, dx);

            double delta_yaw = path_heading - forklift.forklift_data.yaw;

            while (delta_yaw > Math.PI)
            {
                delta_yaw = delta_yaw - 2.0 * Math.PI;
            }

            while (delta_yaw < -Math.PI)
            {
                delta_yaw = delta_yaw + 2.0 * Math.PI;
            }

            if (Math.Abs(delta_yaw) < (Math.PI / 2.0)) return 1.0f;
            else return -1.0f;

        }

        public int check_side_road(int forklift_id, Line_Type road, Point_Type point)
        {
            double p_start_x;
            double p_start_y;

            double p_end_x;
            double p_end_y;


            if (forklift_id == 2)
            {
                p_start_x = road.l1_start_px;
                p_start_y = road.l1_start_py;
                p_end_x = road.l1_end_px;
                p_end_y = road.l1_end_py;
            }
            else
            {
                p_start_x = road.l2_start_px;
                p_start_y = road.l2_start_py;
                p_end_x = road.l2_end_px;
                p_end_y = road.l2_end_py;
            }

            double dx1 = p_start_x - point.px;
            double dy1 = p_start_y - point.py;

            double dx2 = p_end_x - point.px;
            double dy2 = p_end_y - point.py;

            Double dist2start = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            Double dist2end = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

            if (dist2start < dist2end) return 1;
            else return 0;

        }

        public bool check_point_confliect(Point_Type point1, Point_Type point2) {
            float dx = point1.px - point2.px;
            float dy = point1.py - point2.py;

            if (Math.Sqrt(dx * dx + dy * dy) < 0.2) return true;
            else return false;
        }


        public bool check_point_on_line(Point_Type point, Point_Type Line_point1, Point_Type Line_point2)
        {

            Double Ka = Line_point2.py - Line_point1.py;
            Double Kb = Line_point1.px - Line_point2.px;
            Double Kc = Line_point2.px * Line_point1.py - Line_point1.px * Line_point2.py;

            Double px = point.px;
            Double py = point.py;

            Double dist = (Ka * px + Kb * py + Kc)/ Math.Sqrt(Ka * Ka + Kb * Kb);

            if (dist < 0.3) return true;
            else return false;
        }

    }
}
