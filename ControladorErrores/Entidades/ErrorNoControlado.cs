using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControladorErrores.Entidades
{
    public class ErrorNoControlado
    {
        public string Clase { get; set; }
        public string FuncionEjecutada { get; set; }
        public int LineaCodigo { get; set; }
        public int LineaCodigo_Columna { get; set; }
        public DateTime FechaHora { get; set; }
        public StackTrace Stacktrace { get; set; }
        public Exception Excepcion { get; set; }
        public string ObjetoAdicionalTipo { get; set; }
        public object ObjetoAdicional { get; set; }
        public IDictionary<string, object> ObjetosAdicionales { get; set; }

        public ErrorNoControlado(Exception excepcion)
        {
            InicializarInformacíon(excepcion);
        }

        public ErrorNoControlado(Exception excepcion, object Objeto)
        {
            InicializarInformacíon(excepcion);

            if (Objeto != null)
            {
                ObjetoAdicionalTipo = Objeto.GetType().Name;
                ObjetoAdicional = Objeto;
            }
        }

        public ErrorNoControlado(Exception excepcion, IList<object> Objetos)
        {
            InicializarInformacíon(excepcion);

            ObjetosAdicionales = new Dictionary<string, object>();
            int contador = 0;

            var nombres = new List<string>();

            if (Objetos != null)
            {
                foreach (var objeto in Objetos)
                {
                    var object_key = objeto.GetType().Name;
                    var nombre_existe = nombres.Exists(n => n == objeto.GetType().Name);
                    if (nombre_existe)
                    {
                        object_key = contador + "_" + object_key;
                        contador++;
                    }
                    else
                    {
                        nombres.Add(object_key);
                    }
                    ObjetosAdicionales.Add(
                        object_key,
                        objeto
                    );
                }
            }
        }

        internal void InicializarInformacíon(Exception excepcion)
        {
            Excepcion = excepcion;
            Stacktrace = ObtenerStackTrace(true);


            FechaHora = DateTime.Now;

            ObtenerClase();
            ObtenerFuncionEjecutada();
            ObtenerLineaCodigo();
        }

        internal void ObtenerClase()
        {
            Clase = string.Empty;

            var frame = Stacktrace.GetFrame(0);

            if (frame != null)
            {
                Clase = frame.GetFileName();
            }

        }

        internal void ObtenerFuncionEjecutada()
        {
            FuncionEjecutada = string.Empty;

            var frame = Stacktrace.GetFrame(0);

            if (frame != null)
            {
                FuncionEjecutada = frame.GetMethod().Name;
            }
        }

        internal void ObtenerLineaCodigo()
        {
            LineaCodigo = 0;
            LineaCodigo_Columna = 0;

            var frame = Stacktrace.GetFrame(0);

            if (frame != null)
            {
                LineaCodigo = frame.GetFileLineNumber();
                LineaCodigo_Columna = frame.GetFileColumnNumber();
            }

        }

        internal StackTrace ObtenerStackTrace(bool obtenerSourceInformation = false)
        {
            var stackTrace = new StackTrace(Excepcion, obtenerSourceInformation);

            return stackTrace;
        }
    }
}
