using FlightSimulator.Model;
using FlightSimulator.Model.Interface;
using System;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication.Controllers
{
    public class DefaultController : Controller
    {
        private double latitude;
        private double longitude;
        private IDictionary<string, string> elements = new Dictionary<string, string>()
        {
            { "lon","/position/longitude-deg" },
            { "lat", "/position/latitude-deg" },
            { "rudder", "/controls/flight/rudder"},
            { "throttle", "/controls/engines/current-engine/throttle" }
        };

        // GET: Default
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult display(string ip, int port)
        {
            IModel model = MyModel.Instance;
            model.openServer(ip, port);
            
            Longitude = model.Longitude;
            Latitude = model.Latitude;

            Session["time"] = 0;
            return View();

            
            /*InfoModel.Instance.ip = ip;
            InfoModel.Instance.port = port.ToString();
            InfoModel.Instance.time = time;

            InfoModel.Instance.ReadData("Dor");

            Session["time"] = time;


            return View();*/
        }

        


        private void toXml(XmlWriter writer)
        {
            writer.WriteStartElement("Location");
            writer.WriteElementString("Latitude", this.Latitude.ToString());
            writer.WriteElementString("Longitude", this.Longitude.ToString());
            writer.WriteEndElement();
        }

        private string ToXml(double lon, double lat)
        {
            //Initiate XML stuff
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            XmlWriter writer = XmlWriter.Create(sb, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("Location");

            toXml(writer);

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            return sb.ToString();
        }


        [HttpGet]
        public string GetLocation()
        {
            /*****************************************************************
             * 
             * 
             * update lon & lat values with flight gear
             * 
             * 
             */
            Latitude = 100;
            Longitude = 150;

            return ToXml(Latitude, Longitude);
        }

        private void writeToFile(string path, string content)
        {
            var dir = Server.MapPath("~\\files");
            var file = Path.Combine(dir, path);

            Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(file, content+Environment.NewLine);
            
        }


        public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock)
        {
            try
            {
                Task task = Task.Factory.StartNew(() => codeBlock());
                task.Wait(timeSpan);
                return task.IsCompleted;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerExceptions[0];
            }
        }


        private string extractDouble(string str)
        {
            List<string> values = str.Split(' ').ToList();
            string num = values.Find(
            delegate (string s)
            {
                return s.StartsWith("'") && s.EndsWith("'");
            });
            num = num.Replace("'", "");
            return num;

        }

        LinkedList<string> addGetAndNewLineToStrings(ICollection<string> strings)
        {
            LinkedList<string> copyString = new LinkedList<string>();
            foreach(string str in strings)
            {
                copyString.AddLast("get " + str + Environment.NewLine);
            }
            return copyString;
        }


        [HttpGet]
        public string save(string ip, int port, int tempo, int duration, string fileName)
        {

            var dir = Server.MapPath("~\\files");
            var file = Path.Combine(dir, "checkFile");

            Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(file, "start save" + Environment.NewLine);




            string msg = fileName + " added";
            ICollection<string> elemetsWithGet = addGetAndNewLineToStrings(elements.Values);
            IModel model = MyModel.Instance;

            
                model.connectClient(ip, port);

                System.IO.File.AppendAllText(file, "connected as client" + Environment.NewLine);
                try
                {
                ITelnetClient c = model.getClient();
                System.IO.File.AppendAllText(file, "client gets" + Environment.NewLine);
                string strings = c.read(elemetsWithGet);
                    
            System.IO.File.AppendAllText(file, "read done" + Environment.NewLine);
                    List<string> values = strings.Split(',').ToList();

                    string properties = extractDouble(values[0]);
                    int length = elements.Count();
                    for (int i = 1; i < length; ++i)
                    {
                        properties += "," + extractDouble(values[i]);
                    }

                    writeToFile(fileName, properties);
                    model.disconnectClient();
                }
                catch {
                    msg = "there was a problem";
                }

                System.Threading.Thread.Sleep(1000 * tempo);
            //} while (model.isClientConnected());

            // /save/127.0.0.1/5400/4/10/flight1
            model.disconnectClient();
            return msg;
        }


        public double Latitude
        {
            get
            {
                return latitude;
            }
            set
            {
                latitude = value;
                //NotifyPropertyChanged("Latitude");
            }
        }

        public double Longitude
        {
            get
            {
                return longitude;
            }
            set
            {
                longitude = value;
                //NotifyPropertyChanged("Longitude");
            }
        }
    }
}
