using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class Recurso
    {
        public Recurso(string IdHelmIntegral, string NombreServicio, string NombreRecurso, string TipoRecurso, string CentroBeneficioR, string TipoManiobra)
        {
            this.IdHelmIntegral = IdHelmIntegral;
            this.NombreServicio = NombreServicio;
            this.NombreRecurso = NombreRecurso;
            this.TipoRecurso = TipoRecurso;
            this.CentroBeneficioR = CentroBeneficioR;
            this.TipoManiobra = TipoManiobra;
        }
        public string IdHelmIntegral { get; set; }
        public string NombreServicio { get; set; }
        public string NombreRecurso { get; set; }
        public string TipoRecurso { get; set; }
        public string CentroBeneficioR { get; set; }
        public string TipoManiobra { get; set; }
    }
}
