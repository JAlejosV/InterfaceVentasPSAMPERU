using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Configuration;
using System;
using System.Linq;
using System.Data.Entity;
using System.Data.Entity.Validation;

namespace DataAccess
{
    public class ClsDal
    {
        public HELMEntities entityHelm = new HELMEntities();
        public TRAMARSAEntities entityTramarsa = new TRAMARSAEntities();

        public void PersistirFatura()
        {
            try
            {
                entityHelm.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                .SelectMany(x => x.ValidationErrors)
                .Select(x => "- " + x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                throw new Exception(exceptionMessage);
            }
        }

        public void RevertirFactura()
        {
            var changedEntries = entityHelm.ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged).ToList();

            foreach (var entry in changedEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }

        public void PersistirOfisis()
        {
            try
            {
                entityTramarsa.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                .SelectMany(x => x.ValidationErrors)
                .Select(x => "- " + x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                throw new Exception(exceptionMessage);
            }
        }

        public void RevertirOfisis()
        {
            var changedEntries = entityTramarsa.ChangeTracker.Entries()
                .Where(x => x.State != EntityState.Unchanged).ToList();

            foreach (var entry in changedEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }

        public Documento BuscarDocumento(string IdHelm)
        {
            return entityHelm.Documento.FirstOrDefault(d => d.IdHelm == IdHelm);
        }

        public Documento ValidarFechaEmision(string serie, int codigoDocumento, DateTime fechaEmision)
        {
            return entityHelm.Documento
                .Where(d => d.Serie.Equals(serie)
                    && d.CodigoDocumento == codigoDocumento
                    && DbFunctions.TruncateTime(d.FechaEmision) > fechaEmision.Date)
                .OrderByDescending(d2 => d2.FechaEmision)
                .FirstOrDefault();
        }

        public void IncluirDocumento(Documento documento)
        {
            entityHelm.Documento.Add(documento);
        }

        public void IncluirDocumentoDetalle(DocumentoDetalle documentoDetalle)
        {
            entityHelm.DocumentoDetalle.Add(documentoDetalle);
        }

        public void IncluirUsrFcrFai(USR_FCRFAI usrfcrfai)
        {
            entityTramarsa.USR_FCRFAI.Add(usrfcrfai);
        }

        public void IncluirUsrFcrFac(USR_FCRFAC usrfcrfac)
        {
            entityTramarsa.USR_FCRFAC.Add(usrfcrfac);
        }

        public string GetNumeroSequencial(int value, string area)
        {
            string sequencial = "";
            HELMEntities entitySequencial = new HELMEntities();

            Numerador numerador = entitySequencial.Numerador.Find(1);

            if (area == "TALARA")
            {
                switch (value)
                {
                    case 184:
                        sequencial = numerador.FacturaTalara.ToString("D5");
                        break;
                    case 191:
                        sequencial = numerador.BoletaTalara.ToString("D5");
                        break;
                    case 15:
                    case 186:
                        sequencial = numerador.NotaCreditoTalara.ToString("D5");
                        break;
                    case 16:
                    case 187:
                        sequencial = numerador.NotaDebitoTalara.ToString("D5");
                        break;
                }
            }
            else
            {
                switch (value)
                {
                    case 184:
                        sequencial = numerador.Factura.ToString("D5");
                        break;
                    case 191:
                        sequencial = numerador.Boleta.ToString("D5");
                        break;
                    case 15:
                    case 186:
                        sequencial = numerador.NotaCredito.ToString("D5");
                        break;
                    case 16:
                    case 187:
                        sequencial = numerador.NotaDebito.ToString("D5");
                        break;
                }
            }
            return sequencial;
        }

        public void SetNumeroSequencial(int value, string area)
        {
            HELMEntities entitySequencial = new HELMEntities();
            Numerador numerador = entitySequencial.Numerador.Find(1);

            if (area == "TALARA")
            {
                switch (value)
                {
                    case 184:
                        numerador.FacturaTalara += 1;
                        break;
                    case 191:
                        numerador.BoletaTalara += 1;
                        break;
                    case 15:
                    case 186:
                        numerador.NotaCreditoTalara += 1;
                        break;
                    case 16:
                    case 187:
                        numerador.NotaDebitoTalara += 1;
                        break;
                }
            }
            else
            {
                switch (value)
                {
                    case 184:
                        numerador.Factura += 1;
                        break;
                    case 191:
                        numerador.Boleta += 1;
                        break;
                    case 15:
                    case 186:
                        numerador.NotaCredito += 1;
                        break;
                    case 16:
                    case 187:
                        numerador.NotaDebito += 1;
                        break;
                }
            }
            entitySequencial.SaveChanges();
        }
    }
}
