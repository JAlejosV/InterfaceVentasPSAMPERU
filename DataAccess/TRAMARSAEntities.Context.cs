﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class TRAMARSAEntities : DbContext
    {
        public TRAMARSAEntities()
            : base("name=TRAMARSAEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<USR_CORMVHIE> USR_CORMVHIE { get; set; }
        public virtual DbSet<USR_FCRAFA> USR_FCRAFA { get; set; }
        public virtual DbSet<USR_FCRFAC> USR_FCRFAC { get; set; }
        public virtual DbSet<USR_FCRFAI> USR_FCRFAI { get; set; }
        public virtual DbSet<USR_CORMVIIE> USR_CORMVIIE { get; set; }
    }
}