//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class ClienteRelacionado
    {
        public long IdClienteRelacionado { get; set; }
        public string NumeroDocumento { get; set; }
        public string NombreCliente { get; set; }
        public bool EstadoRegistro { get; set; }
        public string UsuarioCreacion { get; set; }
        public System.DateTime FechaHoraCreacion { get; set; }
        public string UsuarioActualizacion { get; set; }
        public Nullable<System.DateTime> FechaHoraActualizacion { get; set; }
    }
}