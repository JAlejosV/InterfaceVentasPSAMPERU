using ControladorErrores.Entidades;
using ControladorErrores.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControladorErrores.Servicios.Logger
{
    public class LoggerTXT : ILogger
    {
        public void Log(ErrorNoControlado error)
        {
            string path = @".\logError\";
            string txt = $"{path}_Fecha_{DateTime.Now.ToString("_yyyy_MM_dd_hh_mm_ss") }.txt";

            FileInfo fileInfo = new FileInfo(txt);
            fileInfo.Directory.Create();

            StreamWriter writer = new StreamWriter(txt);
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var JError = JsonConvert.SerializeObject(error, Formatting.Indented, settings);

            writer.WriteLine(JError);

            writer.Close();
        }
    }
}
