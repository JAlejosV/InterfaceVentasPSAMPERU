using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public static class CadenaConexion
    {
        public static string CadenaBDIntermedia => ConfigurationManager.ConnectionStrings["BDIntermedia"].ConnectionString;
    }
}
