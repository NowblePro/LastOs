using System;
using System.IO;
using System.Security.Policy;
using Newtonsoft.Json;

namespace OsEngine.Robots
{
    public class OrdersLogger
    {
        public OrdersLogger(string name, string dir_path = "")
        {
            if (name.Length == 0)
            {
                _name = "orders_log";
            } else
            {
                _name = name;
            }

            if (dir_path.Length > 0 && Directory.Exists(dir_path))
            {
                _path = System.IO.Path.Combine(dir_path, _name + ".json");
            }
            else
            {
                _path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), _name + ".json");
            }


        }

        public void CreateFile()
        {

        }

        public void AppendOrder(OrderLog order)
        {
            string jsonLine = JsonConvert.SerializeObject(order);

            // Дописываем в конец файла (если файла нет — он создаётся)
            using (StreamWriter sw = File.AppendText(_path))
            {
                sw.WriteLine(jsonLine);
            }
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
        private string _path;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private string _name;
    }

    public class OrderLog
    {
        public int position_number { get; set; }
        public string order_type { get; set; }
        public string side { get; set; }
        public string open_date_time { get; set; }
        public decimal volume { get; set; }
        public decimal activation_price { get; set; }
        public decimal stop_level { get; set; }
    }
}
