using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace forklift_rcs{

    struct Data_Type
    {
        public string name;
        public int pos;
        public int length;
        public string type;
        public byte[] data;
    }


    class DataInterface
    {
        public string file_path = "D:\\forklift\\forklift_rcs\\forklift_rcs\\serial_frame.xml";
        public XmlDocument xml_obj;

        public List<Data_Type> send_frame_desc;
        public List<Data_Type> rece_frame_desc;

        public int send_len;
        public int rece_len;

        public DataInterface()
        {
            int pos;

            send_frame_desc = new List<Data_Type>();
            rece_frame_desc = new List<Data_Type>();

            xml_obj = new XmlDocument();

            //导入指定xml文件
            xml_obj.Load(file_path);

            XmlNode root = xml_obj.SelectSingleNode("/frame");
            XmlNodeList childlist = root.ChildNodes;
            foreach (System.Xml.XmlNode item in childlist)
            {
                if (item.Name == "sendframe")
                {
                    XmlNodeList childlist_sub = item.ChildNodes;

                    pos = 0;
                    foreach (System.Xml.XmlNode entry in childlist_sub)
                    {
                        Data_Type data_obj;
                        data_obj.name = entry.Attributes["name"].Value;
                        data_obj.type = entry.Attributes["type"].Value;

                        data_obj.length = Convert.ToInt32(entry.Attributes["byte"].Value);
                        data_obj.data = new byte[data_obj.length];
                        data_obj.pos = pos;

                        if (entry.Attributes["default"].Value != "none")
                        {
                            if (data_obj.length == 1)
                            {
                                data_obj.data[0] = Convert.ToByte(entry.Attributes["default"].Value);
                            }
                            else if (data_obj.length == 2)
                            {
                                UInt16 tmp = Convert.ToUInt16(entry.Attributes["default"].Value);
                                data_obj.data[0] = (byte)(tmp & 0x00ff);
                                data_obj.data[1] = (byte)((tmp & 0xff00) >> 8);
                            }
                            else if (data_obj.length == 4)
                            {
                                UInt32 tmp = Convert.ToUInt32(entry.Attributes["default"].Value);
                                data_obj.data[0] = (byte)(tmp & 0x000000ff);
                                data_obj.data[1] = (byte)((tmp & 0x0000ff00) >> 8);
                                data_obj.data[2] = (byte)((tmp & 0x00ff0000) >> 16);
                                data_obj.data[3] = (byte)((tmp & 0xff000000) >> 24);
                            }
                        }

                        send_frame_desc.Add(data_obj);

                        pos += data_obj.length;
                    }
                    send_len = pos;

                }

                else if (item.Name == "receframe")
                {
                    XmlNodeList childlist_sub = item.ChildNodes;
                    pos = 0;
                    foreach (System.Xml.XmlNode entry in childlist_sub)
                    {
                        Data_Type data_obj;
                        data_obj.name = entry.Attributes["name"].Value;
                        data_obj.type = entry.Attributes["type"].Value;

                        data_obj.length = Convert.ToInt32(entry.Attributes["byte"].Value);
                        data_obj.data = new byte[data_obj.length];
                        data_obj.pos = pos;

                        rece_frame_desc.Add(data_obj);

                        pos += data_obj.length;
                    }
                    rece_len = pos;
                }
            }//end of loop


        }

        public void SetReceData(byte[] data)
        {
            int pos = 0;

            foreach (Data_Type item in rece_frame_desc)
            {
                for (int offset = 0; offset < item.length; offset++)
                {
                    item.data[offset] = data[pos + offset];
                }
                pos += item.length;
            }
        }

        public byte[] GetSendData()
        {
            byte[] res = new byte[send_len];

            int pos = 0;
            foreach (Data_Type item in send_frame_desc)
            {
                for (int offset = 0; offset < item.length; offset++)
                {
                    res[pos + offset] = item.data[offset];
                }
                pos += item.length;
            }
            return res;
        }




    }
}
