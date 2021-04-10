using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public sealed class WebConfigReader
    {
        public sealed class Mailer
        {
            public static string From { get { return Convert.ToString(ConfigurationManager.AppSettings["From"]); } }
            public static string Host { get { return Convert.ToString(ConfigurationManager.AppSettings["Host"]); } }
            public static int Port { get { return Convert.ToInt32(ConfigurationManager.AppSettings["Port"]); } }
            public static bool EnabledSSL { get { return Convert.ToBoolean(ConfigurationManager.AppSettings["EnabledSSL"]); } }
            public static bool UseDefaultCredentials { get { return Convert.ToBoolean(ConfigurationManager.AppSettings["UseDefaultCredentials"]); } }
            public static string CredentialsUser { get { return Convert.ToString(ConfigurationManager.AppSettings["CredentialsUser"]); } }
            public static string CredentialsClave { get { return Convert.ToString(ConfigurationManager.AppSettings["CredentialsClave"]); } }

        }

        public sealed class ServicesHost
        {
            public static string ComexRolCliente { get { return Convert.ToString(ConfigurationManager.AppSettings["urlObtenerRolCliente"]); } }
            public static string ComexGetComprobanteEmitido { get { return Convert.ToString(ConfigurationManager.AppSettings["urlGetComprobanteEmitido"]); } }
            public static string ComexGetDetalleComprobanteEmitido { get { return Convert.ToString(ConfigurationManager.AppSettings["urlGetDetalleComprobanteEmitido"]); } }
            public static string ComexCalculaImpuestoReclamo { get { return Convert.ToString(ConfigurationManager.AppSettings["urlComexCalculaImpuestoReclamo"]); } }
            public static string ComexGenerarPreDocumento { get { return Convert.ToString(ConfigurationManager.AppSettings["urlGenerarPreDocumento"]); } }

            public static string FalconGetComprobanteEmitido { get { return Convert.ToString(ConfigurationManager.AppSettings["urlGetComprobanteEmitidoFalcon"]); } }
            public static string FalconGetDetalleComprobanteEmitido { get { return Convert.ToString(ConfigurationManager.AppSettings["urlGetDetalleComprobanteEmitidoFalcon"]); } }
            public static string FalconCalculaImpuestoReclamo { get { return Convert.ToString(ConfigurationManager.AppSettings["urlFalconCalculaImpuestoReclamo"]); } }
            public static string SeguridadInfoUsuario { get { return Convert.ToString(ConfigurationManager.AppSettings["urlListaInfoUsuario"]); } }
            public static string TramarsaConsultarCliente { get { return Convert.ToString(ConfigurationManager.AppSettings["urlTramarsaConsultarCliente"]); } }
            public static string TramarsaConsultarClienteCorreo { get { return Convert.ToString(ConfigurationManager.AppSettings["urlTramarsaConsultarClienteCorreo"]); } }
            public static string TramarsaConsultarClientePorMatchCodes { get { return Convert.ToString(ConfigurationManager.AppSettings["urlTramarsaConsultarClientePorMatchCode"]); } }
            public static string SeguridadCambiarClaveWeb { get { return Convert.ToString(ConfigurationManager.AppSettings["urlCambiarClaveWeb"]); } }
            public static string TramarsaConsultarClienteXCodigoCliente { get { return Convert.ToString(ConfigurationManager.AppSettings["urlTramarsaConsultarClienteXCodigoCliente"]); } }
        }

        public sealed class Ambiente
        {
            public static string SociedadPropietaria { get { return Convert.ToString(ConfigurationManager.AppSettings["SociedadPropietaria"]); } }
            public static int PaginaInicial { get { return Int32.Parse(ConfigurationManager.AppSettings["PaginaInicial"].ToString()); } }
            public static int CantidadDeRegistros { get { return Int32.Parse(ConfigurationManager.AppSettings["CantidadDeRegistros"].ToString()); } }
        }

        public static int TimeOutSqlCommand
        {
            get { return Convert.ToInt32(ConfigurationManager.AppSettings["timeOutSqlCommand"].ToString()); }
            //get { return 1800; }
        }

        public static string ConnectionStringANTP
        {
            get { return (ConfigurationManager.ConnectionStrings["ANTP"].ToString()); }
        }

        public sealed class Encriptacion
        {
            public static string SemillaEncriptacionPublica { get { return Convert.ToString(ConfigurationManager.AppSettings["semillaEncriptacionPublica"]); } }
        }

        public sealed class Aplicacion
        {
            public static string AcronimoAplicacion { get { return Convert.ToString(ConfigurationManager.AppSettings["AcronimoAplicacion"]); } }
            public static string DominioAplicacion { get { return Convert.ToString(ConfigurationManager.AppSettings["DominioAplicacion"]); } }
            public static string UrlAplicacion { get { return Convert.ToString(ConfigurationManager.AppSettings["urlAcuerdosComerciales"]); } }
        }

        public sealed class Usuario
        {
            public static string DominioUsuarioExterno { get { return Convert.ToString(ConfigurationManager.AppSettings["textoDominioUsuarioExterno"]); } }
        }
    }
}
