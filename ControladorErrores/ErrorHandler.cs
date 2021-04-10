using ControladorErrores.Entidades;
using ControladorErrores.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControladorErrores
{
    public class ErrorHandler
    {
        private ILogger logger;
        public ErrorHandler(ILogger _logger)
        {
            logger = _logger;
        }

        public void Handle(Exception excepcion)
        {
            var error = new ErrorNoControlado(excepcion);
            logger.Log(error);
        }

        public void Handle(Exception excepcion, object ObjetoAdicional)
        {
            var error = new ErrorNoControlado(excepcion, ObjetoAdicional);
            logger.Log(error);
        }

        public void Handle(Exception excepcion, IList<object> ObjetosAdicionales)
        {
            var error = new ErrorNoControlado(excepcion, ObjetosAdicionales);
            logger.Log(error);
        }
    }
}
