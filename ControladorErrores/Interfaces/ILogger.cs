using ControladorErrores.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControladorErrores.Interfaces
{
    public interface ILogger
    {
        void Log(ErrorNoControlado error);
    }
}
