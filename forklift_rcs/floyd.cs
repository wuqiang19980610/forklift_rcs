using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace forklift_rcs_old
{
    struct Path_Type{
        public float length;
        public int status;
        public List<int> path;
    }

    struct WayPoint_Type{
        public int id;
        public float px;
        public float py;
        public int point_type;
        public List<int> neighbor;
    }


    class floyd_planner
    {
        public string xml_file_path = "D:\\forklift\\forklift_rcs\\forklift_rcs\\map_route.xml";

        public float[,] map;
        public int Nwp;
        public List<WayPoint_Type> wap_point_list = new List<WayPoint_Type>();

        public floyd_planner(){

            XmlDocument xml_obj = new XmlDocument(); //新xml工具对象

            //导入指定xml文件
            xml_obj.Load(xml_file_path);

            XmlNode root = xml_obj.SelectSingleNode("/Map");
            XmlNodeList childlist = root.ChildNodes;

            foreach (System.Xml.XmlNode item in childlist){
                if (item.Name == "Route")
                {
                    XmlNodeList pointlist = item.ChildNodes;
                    foreach (System.Xml.XmlNode iwp in pointlist) {
                        WayPoint_Type wp_tmp;
                        wp_tmp.id = Convert.ToInt32(iwp.Attributes["Nr"].Value);

                        string[] pos_str = iwp.Attributes["pos"].Value.Split(new Char[] { ' ' });
                        wp_tmp.px = Convert.ToSingle(pos_str[0]);
                        wp_tmp.py = Convert.ToSingle(pos_str[1]);

                        string[] neighbor_str = iwp.Attributes["neighbor"].Value.Split(new Char[] { ' ' });

                        wp_tmp.neighbor = new List<int>();
                        foreach (string iP in neighbor_str) {
                            wp_tmp.neighbor.Add(Convert.ToInt32(iP));
                        }


                        wp_tmp.point_type = Convert.ToInt32(iwp.Attributes["type"].Value);

                        wap_point_list.Add(wp_tmp);

                    }

                }


            }

            Nwp = wap_point_list.Count;
            map = new float[Nwp, Nwp];
            foreach(WayPoint_Type item in wap_point_list){
                foreach (int iP in item.neighbor) {
                    float dx = wap_point_list[iP - 1].px - item.px;
                    float dy = wap_point_list[iP - 1].py - item.py;
                    float dist = dx * dx + dy * dy;
                    map[item.id - 1, iP - 1] = dist;
                }
            }
        }

        public List<Path_Type> find_path(int src, int dist) {
            List<Path_Type> res = new List<Path_Type>();

            Path_Type path_obj;

            path_obj.length = 0.0f;
            path_obj.path = new List<int>();
            path_obj.path.Add(src);
            path_obj.status = 0;
            res.Add(path_obj);

            int act_path_ind = 0;

            while (true) {

                Path_Type path_tmp;

                if (res[act_path_ind].status != 0) {
                    act_path_ind++;
                    if (act_path_ind >= res.Count) break;
                }
                else {
                    List<int> next = find_next_way_point(res[act_path_ind]);
                    if (next.Count > 0) {
                        foreach (int item in next) {
                            path_tmp = res[act_path_ind];

                            if (check_path_repeat(path_tmp, item) == true){
                                path_tmp.status = -1;
                            }
                            else {
                                if(item == dist) path_tmp.status = 1;
                                else path_tmp.status = 0;
                                path_tmp.length += map[path_tmp.path.Count-1,item];
                                path_tmp.path.Add(item);
                                
                            }
                            res.Add(path_tmp);
                        }

                    }
                    else {
                        path_tmp = res[act_path_ind];
                        path_tmp.status = -1;
                        res.Add(path_tmp);
                    }
                    res.RemoveAt(act_path_ind);
                }

            }
            return res;
        }
        
        //检查当前路径点是否已经在该路径下了
        private bool check_path_repeat(Path_Type path_obj, int act_point) {
            bool is_repeat = false;

            foreach (int item in path_obj.path) {
                if(act_point == item){
                    is_repeat = true;
                    break;
                }
            }

            return is_repeat;
        }

        //给当前路径查找下个路径点
        private List<int> find_next_way_point(Path_Type path_obj) {
            List<int> res = new List<int>();
            int act_wp = path_obj.path[path_obj.path.Count-1];
            

            for (int i = 0; i < Nwp; i++) {
                if (map[act_wp, i] > 0.0f) {
                    res.Add(i);
                }
            }
            return res;
        }

    }
}
