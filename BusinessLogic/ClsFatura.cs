using DataAccess;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using System.Net.Mail;
using System.Net;
using System.Data.SqlClient;
using ControladorErrores.Servicios.Logger;
using ControladorErrores;

namespace BusinessLogic
{
    public class ClsFatura
    {
        public string Error = "";
        
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private struct StrucImposto
        {
            public string id;
            public string descricao;
            public string remolcador;
            public int sequencial;
            public decimal taxa;
            public decimal valor;
        }

        private struct StructRebaje
        {
            public string id;
            public string descricao;
            public string remolcador;
            public int sequencial;
            public decimal taxa;
            public decimal valor;
        }

        private struct StrucDesconto
        {
            public string id;
            public string descricao;
            public string remolcador;
            public int sequencial;
            public decimal taxa;
            public decimal valor;
        }

        private struct StructDetalle
        {
            public string id;
            public string referenceId;
            public string codigoArticulo;
            public string nombreArticulo;
            public string codigoProduto;
            public string remolcador;
            public decimal cantidad;
            public decimal precioUnitario;
            public decimal subTotal;
            public int sequencial;
        }

        private struct StructCompanies
        {
            public string id;
            public string accountNumber;
            public string name;
            public string ruc01;
            public string ccidf;
            public string codDoc01;
            public string address;
            public string email;
            public bool isMyCompany;
        }

        private struct StructAssets
        {
            public string id;
            public string name;
            public string shortName;
            public string accountingCode;
            public string vesselTypeNames;
        }


        private struct LogError
        {
            public string IdTransaccion;
            public string Error;
            public List<string> ErrorDetalle;
        }

        StringBuilder _error = new StringBuilder();

        public void InterfaceFaturamento()
        {
            HttpClient client = new HttpClient();
            ClsDal context = new ClsDal();

            Documento documento = new Documento();
            List<DocumentoDetalle> listDetalle = new List<DocumentoDetalle>();
            USR_FCRFAC usrfcrfac = new USR_FCRFAC();
            List<USR_FCRFAI> listUsrfcrfai = new List<USR_FCRFAI>();

            CultureInfo usCulture = new CultureInfo("en-US");
            log.Info("- Se inició el servicio.");

            try
            {
                #region DeclaracionVariables

                byte tipoVenda = 0;

                bool setPosted = Convert.ToBoolean(ConfigurationManager.AppSettings["setPosted"].ToString());
                bool interfaceFaturaAtiva = Convert.ToBoolean(ConfigurationManager.AppSettings["facturaAtiva"].ToString());
                bool interfaceOfisisAtiva = Convert.ToBoolean(ConfigurationManager.AppSettings["ofisisAtiva"].ToString());
                bool notaCredito;
                bool temImposto;
                bool ordemNula;
                bool revenueAllocationsNulo;
                bool isMyCompany;

                string uriAddress = ConfigurationManager.AppSettings["uriBase"].ToString();
                string mediaType = ConfigurationManager.AppSettings["mediaType"].ToString();
                string apiKey = ConfigurationManager.AppSettings["apiKey"].ToString();
                string itemServico = "";
                string tipoTransacao = "";
                string clienteNacional = "";
                string prefixoSerie = "";
                string keyVessel = "";
                string idAlternativo = "";
                string codigoArticulo = "";
                string codigoProduto = "";
                string sufixoSerie = "";
                string areaName = "";
                string moeda = "";
                string usrCodEmp = "";
                string usrTipPro = "";
                string divisaoEmpresa = "";
                string codigoDetraccion = "";

                int codigoDocumento;
                int numeroLinha;
                int qtdDias = 0;
                int sequencialRebaje;
                int sequencialImpuesto;
                int sequencialDetalle;
                int sequencialDesconto;
                int porcentajeDetraccion = -1;
                int page = 1;

                decimal montoDTR = -1;
                decimal somaDesconto;
                decimal somaValorIGV;
                decimal somaFatura;
                decimal valorBruto;
                string CodigoConcepto = "";
                bool salir = false;
                string NombreRecurso = "";
                string TipoRecurso = "";
                string CentroBeneficioR = "";
                string NombreTipoViaje = "";
                int CodigoSitioFromLocation = 0;
                int CodigoSitioToLocation = 0;
                string ItemServicioIntegral = "";
                string TipoManiobraIntegral = "";
                string NombreTipoRecurso;
                bool LanchaSinCargo = false;
                string NombreTipoViajeSinCargo = "";
                DateTime FechaManiobra1;
                DateTime FechaManiobra2;
                TimeSpan DifHoras;
                decimal DiferenciaHoras;
                string ServicioSinCargo = "";
                string TipoManiobraSinCargo = "";
                string CentroBeneficioSinCargo = "";
                string IdHelmIntegral = "";
                bool FlagManiobra = false;
                int CantidadRercurso = 0;
                //Impresion de Fechas
                string TipoViajeAtraque = "Atraque - Berthing";
                string TipoViajeDesatraque = "Desatraque - Unberthing";
                string RecargoEspaniol = "RECARGO";
                string RecargoIngles = "SURCHARGE";
                int EsAtraqueDesatraque = 0;
                string TipoViajeF = "";
                string NombreRecursoFactura = "";
                string NombreTipoViajeFecha = "";
                string IdHelmOrden = "";
                DateTime? FechaOrden = null;
                string FechaOrdenBase = "";
                int ExisteViaje = 0;
                bool TieneRecargo = false;
                #endregion;

                log.Info("- Se inició la carga de información.");

                #region Carga de Informacion
                ClsJSON json = new ClsJSON();

                List<string> lstTransactionId = new List<string>();
                List<StructCompanies> lstCompanies = new List<StructCompanies>();
                List<StructAssets> lstAssets = new List<StructAssets>();
                List<Recurso> ListaRecurso = new List<Recurso>();
                List<Integral> ListaIntegral = new List<Integral>();
                List<DateTime> ListaFechaManiobra = new List<DateTime>();
                //Impresion de Fechas
                List<ViajeFactura> ListaViajeFactura = new List<ViajeFactura>();
                List<RecursoFactura> ListaRecursoFactura = new List<RecursoFactura>();
                List<RecursoOrden> ListaRecursoOrden = new List<RecursoOrden>();

                client.BaseAddress = new System.Uri(uriAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
                client.DefaultRequestHeaders.Add("API-Key", apiKey);

                SetCompanies(lstCompanies, client);
                SetListAssets(lstAssets, client);
                #endregion

                log.Info("- Se inició el proceso de migración de documentos a la BDI y OFISIS.");
                //JObject jobjTransaction = JObject.Parse(json.TesteJson_Tx13801());
                JObject jobjTransaction = JObject.Parse(GetSyncApi($"api/v1/Jobs/Transactions/Details?page={page}&posted=false", client));

                while (jobjTransaction["Data"]["Page"].HasValues)
                {
                    dynamic dynTransactions = (JArray)jobjTransaction["Data"]["Page"];
                    foreach (var iTransactions in dynTransactions)
                    {
                        try
                        {
                            #region Iniciar proceso transaccional de Inserción de registros

                            //Evaluamos si se trata de revocación para ignorarlo
                            if (iTransactions.TransactionNumber.ToString().StartsWith("RV"))
                                continue;

                            //Evaluamos si se trata de reversión para ignorarlo
                            if (iTransactions.ReversedTransactionId != null)
                                continue;

                            tipoTransacao = iTransactions.TransactionType.Name.ToString().ToUpper();
                            if (tipoTransacao != "COMISIONES")
                            {
                                #region Validar si comprobante existe en BDI
                                
                                Documento documentoExiste = context.BuscarDocumento(iTransactions.Id.ToString());
                                if (documentoExiste != null && !string.IsNullOrEmpty(documentoExiste.IdHelm))
                                {
                                    string mensajeError = $"- DOCUMENTO YA REGISTRADO EN LA BDI.";
                                    throw new Exception(mensajeError);
                                }

                                #endregion

                                #region Cargar objetos y realizar validaciones adicionales antes de guardar en BDI y OFISIS

                                var InformacionFacturaValida = ValidacionInformacionFactura(iTransactions, lstCompanies, lstAssets);
                                if (!InformacionFacturaValida)
                                {
                                    if (setPosted)
                                    {
                                        SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                    }
                                    continue;
                                }
                                //Declaracion de variable error para captur ar los errores de los datos calculados
                                LogError errores = new LogError();
                                errores.Error = string.Empty;
                                errores.IdTransaccion = string.Empty;
                                errores.ErrorDetalle = new List<string>();


                                List<StrucImposto> lstImposto = new List<StrucImposto>();
                                List<StructDetalle> lstDetalle = new List<StructDetalle>();
                                List<StrucDesconto> lstDesconto = new List<StrucDesconto>();
                                List<StructRebaje> lstRebaje = new List<StructRebaje>();
                                Hashtable htOrder = new Hashtable();

                                JObject jobjOrder = new JObject();

                                documento = new Documento();
                                listDetalle = new List<DocumentoDetalle>();
                                listUsrfcrfai = new List<USR_FCRFAI>();

                                usrfcrfac = new USR_FCRFAC();

                                somaDesconto = 0;
                                somaValorIGV = 0;
                                somaFatura = 0;
                                codigoDocumento = 0;
                                numeroLinha = 0;
                                tipoVenda = 0;
                                valorBruto = 0;
                                prefixoSerie = "";
                                sufixoSerie = "";

                                temImposto = false;
                                notaCredito = false;
                                CodigoSitioFromLocation = 0;
                                CodigoSitioToLocation = 0;
                                LanchaSinCargo = false;
                                NombreTipoViajeSinCargo = "";
                                TieneRecargo = false;

                                notaCredito = iTransactions.TransactionNumber.ToString().Contains("RV");

                                if (notaCredito)
                                {
                                    if (setPosted)
                                    {
                                        SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                    }

                                    continue;
                                }

                                ordemNula = string.IsNullOrEmpty(iTransactions.Order.ToString());
                                areaName = iTransactions.Area.Name.ToString().ToUpper();

                                StructCompanies companies = lstCompanies.Find(x => x.accountNumber == iTransactions.AccountNumber.ToString());
                                if (!string.IsNullOrEmpty(companies.accountNumber))
                                {
                                    documento.DireccionCliente = companies.address;
                                    documento.NumeroDocumentoCliente = companies.ccidf;

                                    //JAV-07/01/2020, Si el Cliente es NULL o Blanco, debe postear sin generar Factura Sunat, El usuario debe Revocar
                                    if (string.IsNullOrEmpty(documento.NumeroDocumentoCliente))
                                    {
                                        if (setPosted)
                                        {
                                            SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                        }
                                        continue;
                                    }

                                    documento.CodigoDocumentoCliente = Convert.ToInt32(companies.codDoc01);
                                    documento.EmailCliente = companies.email;
                                    isMyCompany = companies.isMyCompany;
                                    clienteNacional = companies.ruc01;
                                    if (string.IsNullOrEmpty(clienteNacional))
                                    {
                                        clienteNacional = "";
                                    }
                                }
                                else
                                {
                                    isMyCompany = false;
                                }

                                if (!ordemNula)
                                {
                                    jobjOrder = JObject.Parse(GetSyncApi($"api/v1/Jobs/Orders/FindOrders?page=1&OrderNumber={iTransactions.Order.OrderNumber.ToString()}", client));

                                    // --------------------------------------------------------
                                    // Author: André Azevedo
                                    // Company: Helm Operations
                                    // Date: Apr-08-2020
                                    // Definition: return ship Gross Tonage and LOA. 
                                    dynamic dynOrder = jobjOrder["Data"]["Page"][0];
                                    string imoNumber = dynOrder.Ship.IMONumber;
                                    if (!string.IsNullOrEmpty(imoNumber))
                                    {
                                        JObject jsShip = new JObject();
                                        jsShip = GetDataShip(imoNumber, client);
                                        if (jsShip["Data"]["Page"].HasValues)
                                        {
                                            dynamic dynShip = jsShip["Data"]["Page"];
                                            documento.TRB = dynShip[0].GT;
                                            documento.Eslora = dynShip[0].LOA;
                                        }
                                        //documento.TRB = Math.Abs(Convert.ToDecimal(htOrder["TRB01"]));
                                        //documento.Eslora = Math.Abs(Convert.ToDecimal(htOrder["ESL01"]));
                                    }
                                    // --------------------------------------------------------

                                    SetOrderUserDefined(jobjOrder, htOrder);
                                    if (!string.IsNullOrEmpty((string)htOrder["BEN01"]))
                                    {
                                        StructCompanies company = lstCompanies.Find(x => x.id == (string)htOrder["BEN01"]);
                                        documento.NombreSolidario = company.name;
                                        //TF-22/07/2020, Numero Documento del Solidario
                                        documento.NumeroDocumentoSolidario = company.ccidf;
                                    }
                                    else
                                    {
                                        documento.NombreSolidario = null;
                                        documento.NumeroDocumentoSolidario = null;
                                    }
                                }

                                documento.IdHelm = iTransactions.Id.ToString();
                                documento.NombreCliente = iTransactions.AccountName.ToString().Trim();
                                documento.CodigoMoneda = Convert.ToInt32(iTransactions.CurrencyType.ExternalSystemCode.ToString());
                                documento.TransaccionHelm = iTransactions.TransactionNumber.ToString();
                                documento.SerieReferencia = null;
                                documento.FechaEmision = Convert.ToDateTime(iTransactions.TransactionDate.ToString());
                                documento.SerieReferencia2 = null;
                                documento.NumeroReferencia2 = null;
                                documento.Observaciones = iTransactions.Note.ToString();
                                moeda = iTransactions.CurrencyType.ShortName.ToString().ToUpper();
                                documento.IdPuerto = Convert.ToInt32(iTransactions.Area.ExternalSystemCode.ToString());

                                if (!ordemNula)
                                {
                                    documento.OrdenCompra = iTransactions.Order.OrderNumber.ToString();
                                    documento.NombreNave = $"{iTransactions.Order.ShipName.ToString()}-{iTransactions.Order.VoyageNumber.ToString()}";
                                    documento.NombreNave2 = iTransactions.Order.ShipName.ToString();
                                }

                                if (!string.IsNullOrEmpty(iTransactions.DueDate.ToString()))
                                {
                                    documento.FechaVencimiento = Convert.ToDateTime(iTransactions.DueDate.ToString());
                                }

                                if (!string.IsNullOrEmpty(iTransactions.ExchangeRate.ToString()))
                                {
                                    documento.TipoCambio = Convert.ToDecimal(iTransactions.ExchangeRate.ToString());
                                }

                                if (!string.IsNullOrEmpty(iTransactions.AccountingTerm.ToString()))
                                {
                                    documento.FormaPago = iTransactions.AccountingTerm.ExternalSystemCode.ToString();
                                }

                                try
                                {
                                    documento.FormaPago = iTransactions.AccountingTerm.ExternalSystemCode.ToString();
                                }
                                catch
                                {
                                    documento.FormaPago = "C000";
                                }

                                dynamic dynTransactionsLines = iTransactions.TransactionLines.Children();
                                foreach (var iTransactionsLines in dynTransactionsLines)
                                {
                                    StructDetalle stuDetalle = new StructDetalle();
                                    StrucImposto stuImposto = new StrucImposto();
                                    StrucDesconto stuDesconto = new StrucDesconto();
                                    StructRebaje stuRebaje = new StructRebaje();

                                    idAlternativo = "";
                                    codigoArticulo = "";
                                    codigoProduto = "";
                                    keyVessel = "";
                                    sequencialRebaje = 1;
                                    sequencialImpuesto = 1;
                                    sequencialDetalle = 1;
                                    sequencialDesconto = 1;

                                    dynamic dynRevenueAllocations = iTransactionsLines.RevenueAllocations.First;
                                    revenueAllocationsNulo = (dynRevenueAllocations == null);

                                    itemServico = iTransactionsLines.DetailDescription.ToString().ToUpper().Trim();
                                    if (itemServico.Contains("REBAJE"))
                                    {
                                        if (!revenueAllocationsNulo)
                                        {
                                            dynamic dynTransactionsLinesRevenueAllocations = iTransactionsLines.RevenueAllocations.Children();
                                            foreach (var item in dynTransactionsLinesRevenueAllocations)
                                            {
                                                foreach (var iAccounting in item.AccountingCodes)
                                                {
                                                    switch (iAccounting.ReferenceType.ToString().ToUpper())
                                                    {
                                                        case "VESSEL":
                                                        case "RESOURCE":
                                                            if (!string.IsNullOrEmpty(keyVessel))
                                                            {
                                                                keyVessel += ";";
                                                            }
                                                            keyVessel += iAccounting.EntityName.ToString();
                                                            break;
                                                    }
                                                }
                                            }
                                            string[] aux = keyVessel.Split(';');
                                            Array.Sort(aux);
                                            keyVessel = "";
                                            foreach (var i in aux)
                                            {
                                                keyVessel += i;
                                            }
                                        }

                                        StructRebaje rebaje = new StructRebaje();
                                        rebaje = lstRebaje.FindLast(x => (x.descricao == itemServico.Replace(".", "")) &&
                                                                         (x.remolcador == keyVessel));
                                        if (!string.IsNullOrEmpty(rebaje.id))
                                        {
                                            sequencialRebaje = rebaje.sequencial + 1;
                                        }
                                        stuRebaje.id = iTransactionsLines.Id.ToString();
                                        stuRebaje.descricao = itemServico.Replace(".", "");
                                        stuRebaje.taxa = (Convert.ToDecimal(iTransactionsLines.Rate.ToString()) * -1) * 100;
                                        stuRebaje.remolcador = keyVessel;
                                        stuRebaje.valor = Math.Abs(Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                        stuRebaje.sequencial = sequencialRebaje;
                                        lstRebaje.Add(stuRebaje);
                                    }
                                    else if (itemServico.Contains("IMPUESTO") || itemServico.Contains("TAX") || itemServico.Contains("DESCUENTO") || itemServico.Contains("DISCOUNT"))
                                    {
                                        if (itemServico.Contains("DESCUENTO") || itemServico.Contains("DISCOUNT"))
                                        {
                                            if (!revenueAllocationsNulo)
                                            {
                                                dynamic dynTransactionsLinesRevenueAllocations = iTransactionsLines.RevenueAllocations.Children();
                                                foreach (var item in dynTransactionsLinesRevenueAllocations)
                                                {
                                                    foreach (var iAccounting in item.AccountingCodes)
                                                    {
                                                        switch (iAccounting.ReferenceType.ToString().ToUpper())
                                                        {
                                                            case "VESSEL":
                                                            case "RESOURCE":
                                                                if (!string.IsNullOrEmpty(keyVessel))
                                                                {
                                                                    keyVessel += ";";
                                                                }
                                                                keyVessel += iAccounting.EntityName.ToString();
                                                                break;
                                                        }
                                                    }
                                                }
                                                string[] aux = keyVessel.Split(';');
                                                Array.Sort(aux);
                                                keyVessel = "";
                                                foreach (var i in aux)
                                                {
                                                    keyVessel += i;
                                                }

                                                if (string.IsNullOrEmpty(iTransactionsLines.RevenueAllocations[0].ReferenceTransactionLineId.ToString()))
                                                {
                                                    idAlternativo = iTransactionsLines.RevenueAllocations[0].Id.ToString();
                                                }
                                                else
                                                {
                                                    idAlternativo = iTransactionsLines.RevenueAllocations[0].ReferenceTransactionLineId.ToString();
                                                }
                                            }
                                            stuDesconto.id = idAlternativo;
                                            stuDesconto.descricao = itemServico;
                                            stuDesconto.taxa = (Convert.ToDecimal(iTransactionsLines.Rate.ToString()) * -1) * 100;
                                            stuDesconto.remolcador = keyVessel;
                                            stuDesconto.valor = Math.Abs(Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                            stuDesconto.sequencial = sequencialDesconto;
                                            lstDesconto.Add(stuDesconto);
                                        }
                                        else
                                        {
                                            if (!revenueAllocationsNulo)
                                            {
                                                dynamic dynTransactionsLinesRevenueAllocations = iTransactionsLines.RevenueAllocations.Children();
                                                foreach (var item in dynTransactionsLinesRevenueAllocations)
                                                {
                                                    foreach (var iAccounting in item.AccountingCodes)
                                                    {
                                                        switch (iAccounting.ReferenceType.ToString().ToUpper())
                                                        {
                                                            case "VESSEL":
                                                            case "RESOURCE":
                                                                if (!string.IsNullOrEmpty(keyVessel))
                                                                {
                                                                    keyVessel += ";";
                                                                }
                                                                keyVessel += iAccounting.EntityName.ToString();
                                                                break;
                                                        }
                                                    }
                                                }
                                                string[] aux = keyVessel.Split(';');
                                                Array.Sort(aux);
                                                keyVessel = "";
                                                foreach (var i in aux)
                                                {
                                                    keyVessel += i;
                                                }
                                            }

                                            StrucImposto chaveImposto = new StrucImposto();
                                            chaveImposto = lstImposto.FindLast(x => (x.descricao == itemServico) &&
                                                                                    (x.remolcador == keyVessel));
                                            if (!string.IsNullOrEmpty(chaveImposto.id))
                                            {
                                                sequencialImpuesto = chaveImposto.sequencial + 1;
                                            }

                                            stuImposto.id = iTransactionsLines.Id.ToString();
                                            stuImposto.descricao = itemServico;
                                            stuImposto.taxa = Convert.ToDecimal(iTransactionsLines.Rate.ToString());
                                            stuImposto.remolcador = keyVessel;
                                            stuImposto.sequencial = sequencialImpuesto;
                                            stuImposto.valor = Convert.ToDecimal(iTransactionsLines.Amount.ToString());
                                            lstImposto.Add(stuImposto);

                                            temImposto = true;
                                        }
                                    }
                                    else
                                    {
                                        stuDetalle.referenceId = "";
                                        if (!revenueAllocationsNulo)
                                        {
                                            dynamic dynTransactionsLinesRevenueAllocations = iTransactionsLines.RevenueAllocations.Children();
                                            foreach (var item in dynTransactionsLinesRevenueAllocations)
                                            {
                                                foreach (var iAccounting in item.AccountingCodes)
                                                {
                                                    switch (iAccounting.ReferenceType.ToString().ToUpper())
                                                    {
                                                        case "TRIPTYPE":
                                                            codigoProduto = iAccounting.AccountingCode.ToString();
                                                            TipoViajeF = iAccounting.EntityName.ToString().Trim();
                                                            break;

                                                        case "BILLINGTYPE":
                                                            if (iAccounting.AccountingCode.ToString() == "BV")
                                                            {
                                                                codigoDocumento = 191;
                                                                prefixoSerie = "B";
                                                                sufixoSerie = "B";
                                                            }
                                                            else if (iAccounting.AccountingCode.ToString() == "FAC")
                                                            {
                                                                codigoDocumento = 184;
                                                                prefixoSerie = "F";
                                                                sufixoSerie = "F";
                                                            }
                                                            break;
                                                        case "VESSEL":
                                                        case "RESOURCE":
                                                            if (!string.IsNullOrEmpty(keyVessel))
                                                            {
                                                                keyVessel += ";";
                                                            }
                                                            keyVessel += iAccounting.EntityName.ToString();
                                                            codigoArticulo = iAccounting.AccountingCode.ToString();
                                                            NombreRecursoFactura = iAccounting.EntityName.ToString();

                                                            if (!itemServico.Contains(RecargoIngles) && !itemServico.Contains(RecargoEspaniol))
                                                            {
                                                                ListaRecursoFactura.Add(new RecursoFactura(iTransactionsLines.Id.ToString(), itemServico, TipoViajeF, NombreRecursoFactura));
                                                            }
                                                            break;
                                                    }
                                                }
                                                //Verificar si la factura es Integral y tipo de viaje (atraque - berthing o desatraque unberthing) y no sea Recargo o SURCHARGE y Almacenar los Recursos
                                                if (itemServico.Contains("INTEGRAL") && (itemServico.Contains("ATRAQUE") || itemServico.Contains("BERTHING") || itemServico.Contains("DESATRAQUE") || itemServico.Contains("UNBERTHING"))
                                                    && !itemServico.Contains("SURCHARGE") && !itemServico.Contains("RECARGO"))
                                                {
                                                    IdHelmIntegral = iTransactionsLines.Id.ToString();
                                                    ItemServicioIntegral = itemServico;
                                                    TipoManiobraIntegral = ItemServicioIntegral.Substring(ItemServicioIntegral.IndexOf("-") + 1, ItemServicioIntegral.Length - ItemServicioIntegral.IndexOf("-") - 1).Trim();

                                                    foreach (var iAccounting in item.AccountingCodes)
                                                    {
                                                        NombreRecurso = "";
                                                        TipoRecurso = "";
                                                        CentroBeneficioR = "";

                                                        if ((iAccounting.ReferenceType.ToString().ToUpper() == "VESSEL") || (iAccounting.ReferenceType.ToString().ToUpper() == "RESOURCE"))
                                                        {
                                                            NombreRecurso = iAccounting.EntityName.ToString().Trim();
                                                            CentroBeneficioR = iAccounting.AccountingCode.ToString();
                                                            ListaRecurso.Add(new Recurso(IdHelmIntegral, itemServico, NombreRecurso, TipoRecurso, CentroBeneficioR, TipoManiobraIntegral));
                                                        }
                                                    }
                                                }
                                            }

                                            if (!itemServico.ToUpper().Contains("SURCHARGE") && !itemServico.ToUpper().Contains("RECARGO"))
                                            {
                                                TieneRecargo = true;
                                                ListaViajeFactura.Add(new ViajeFactura(iTransactionsLines.Id.ToString(), TipoViajeF));
                                            }

                                            string[] aux = keyVessel.Split(';');
                                            Array.Sort(aux);
                                            keyVessel = "";
                                            foreach (var i in aux)
                                            {
                                                keyVessel += i;
                                            }
                                            stuDetalle.referenceId = dynRevenueAllocations.ReferenceTransactionLineId.ToString();
                                        }
                                        StructDetalle chaveDetalle = new StructDetalle();
                                        chaveDetalle = lstDetalle.FindLast(x => (x.nombreArticulo.ToUpper() == itemServico) &&
                                                                                (x.remolcador == keyVessel));
                                        if (!string.IsNullOrEmpty(chaveDetalle.id))
                                        {
                                            sequencialDetalle = chaveDetalle.sequencial + 1;
                                        }

                                        stuDetalle.id = iTransactionsLines.Id.ToString();
                                        stuDetalle.nombreArticulo = iTransactionsLines.DetailDescription.ToString().Trim();
                                        stuDetalle.cantidad = Math.Abs(Convert.ToDecimal(iTransactionsLines.Quantity.ToString()));
                                        stuDetalle.precioUnitario = Math.Abs(Convert.ToDecimal(iTransactionsLines.Rate.ToString()));
                                        stuDetalle.subTotal = Math.Abs(Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                        stuDetalle.remolcador = keyVessel;
                                        stuDetalle.sequencial = sequencialDetalle;

                                        // Si Codigo de Producto es Blanco no enviar factura a Sunat, se postea para que el facturador la revoque
                                        if (string.IsNullOrEmpty(codigoProduto) && (!ordemNula))
                                        {
                                            errores.IdTransaccion = iTransactions.Id.ToString();
                                            errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());
                                            errores.ErrorDetalle.Add(string.Format("El codigo del producto esta en blanco"));
                                            Correo correo = new Correo();

                                            try
                                            {
                                                construirCorreoError(correo, iTransactions, errores);
                                                EnviarCorreoElectronico(correo, true);
                                                if (setPosted)
                                                {
                                                    SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                                }
                                                salir = true;
                                                break;
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error($"- Error al enviar correo con información de errores. " + ArmarLog(documento, iTransactions), ex);

                                                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
                                            }
                                        }
                                        else
                                        {
                                            salir = false;
                                        }

                                        stuDetalle.codigoProduto = codigoProduto;
                                        stuDetalle.codigoArticulo = "";

                                        if ((!ordemNula) && (string.IsNullOrEmpty(codigoArticulo)))
                                        {
                                            codigoArticulo = GetCentroBeneficio(iTransactionsLines.DetailDescription.ToString().Trim(),
                                                                                sequencialDetalle,
                                                                                iTransactions.Order.OrderNumber.ToString(),
                                                                                lstAssets,
                                                                                jobjOrder);
                                        }

                                        // Si CB es Blanco no enviar factura a Sunat, se postea para que el facturador la revoque
                                        if (!ordemNula)
                                        {
                                            if (string.IsNullOrEmpty(codigoArticulo))
                                            {
                                                errores.IdTransaccion = iTransactions.Id.ToString();
                                                errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());
                                                errores.ErrorDetalle.Add(string.Format("El Centro de Beneficio esta en blanco"));
                                                Correo correo = new Correo();

                                                try
                                                {
                                                    construirCorreoError(correo, iTransactions, errores);
                                                    EnviarCorreoElectronico(correo, true);
                                                    if (setPosted)
                                                    {
                                                        SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                                    }
                                                    salir = true;
                                                    break;
                                                }
                                                catch (Exception ex)
                                                {
                                                    log.Error($"- Error al enviar correo con información de errores. " + ArmarLog(documento, iTransactions), ex);

                                                    new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
                                                }

                                            }
                                            else
                                            {
                                                salir = false;
                                            }

                                            // Verificar si es CB de de Practico (Empieza en "TFI0299") validar segun su Sede si es el Correcto
                                            if (codigoArticulo.ToUpper().Contains("TFI0299"))
                                            {
                                                var query = (from t in context.entityHelm.CentroBeneficioPracticoSede
                                                             where (t.IdPuerto == documento.IdPuerto)
                                                             select t).ToList();

                                                //var CentroBeneficio = query.First().CentroBeneficio.ToString();
                                                var CentroBeneficio = codigoArticulo;
                                                if (query.Count() > 0)
                                                {
                                                    CentroBeneficio = query.First().CentroBeneficio.ToString();
                                                }

                                                if (CentroBeneficio == codigoArticulo)
                                                {
                                                    stuDetalle.codigoArticulo = codigoArticulo;
                                                }
                                                else
                                                {
                                                    stuDetalle.codigoArticulo = CentroBeneficio;
                                                }
                                            }
                                            else
                                            {
                                                stuDetalle.codigoArticulo = codigoArticulo;
                                            }
                                        }

                                        lstDetalle.Add(stuDetalle);

                                        //Ingresar si el item es integral y atraque o desatraque
                                        if (itemServico.Contains("INTEGRAL") && (itemServico.Contains("ATRAQUE") || itemServico.Contains("BERTHING") || itemServico.Contains("DESATRAQUE") || itemServico.Contains("UNBERTHING"))
                                                && !itemServico.Contains("SURCHARGE") && !itemServico.Contains("RECARGO"))
                                        {
                                            ListaIntegral.Add(new Integral(stuDetalle.id, stuDetalle.nombreArticulo.ToUpper().Trim()));
                                        }
                                    }
                                }

                                if (!ordemNula)
                                {
                                    EsAtraqueDesatraque = ListaViajeFactura.Where(p => p.TipoViaje == TipoViajeAtraque || p.TipoViaje == TipoViajeDesatraque).Count();
                                    foreach (var itemFactura in ListaViajeFactura.ToList())
                                    {
                                        string NombreServicioFecha = itemFactura.TipoViaje.ToString().Trim();
                                        dynamic dynOrderFecha = (JArray)jobjOrder["Data"]["Page"];

                                        foreach (var iOrder in dynOrderFecha)
                                        {
                                            dynamic dynViajes = iOrder.Trips.Children();
                                            foreach (var iViajes in dynViajes)
                                            {
                                                NombreTipoViajeFecha = "";
                                                dynamic TipoViaje = iViajes.Triptype;
                                                IdHelmOrden = iViajes.Id.ToString().Trim();
                                                NombreTipoViajeFecha = TipoViaje.Name.ToString().Trim();
                                                FechaOrdenBase = string.IsNullOrEmpty(iViajes.EndDate.ToString()) ? null : iViajes.EndDate.ToString();

                                                if (string.IsNullOrEmpty(FechaOrdenBase))
                                                {
                                                    FechaOrden = null;
                                                }
                                                if (!string.IsNullOrEmpty(FechaOrdenBase))
                                                {
                                                    FechaOrden = Convert.ToDateTime(FechaOrdenBase);
                                                }

                                                if (EsAtraqueDesatraque > 0)
                                                {
                                                    if (NombreServicioFecha.Contains(NombreTipoViajeFecha) && (NombreServicioFecha.Contains(TipoViajeAtraque) || NombreServicioFecha.Contains(TipoViajeDesatraque)))
                                                    {
                                                        dynamic dynJobs = iViajes.Jobs.Children();
                                                        foreach (var iJobs in dynJobs)
                                                        {
                                                            dynamic TipoRecursos = iJobs.RequiredResourceType;
                                                            dynamic Recursos = iJobs.Resource;

                                                            NombreTipoRecurso = TipoRecursos.Name.ToString().ToUpper().Trim();
                                                            NombreRecurso = Recursos.Name.ToString().Trim();

                                                            FechaOrdenBase = string.IsNullOrEmpty(iJobs.EndDate.ToString()) ? null : iJobs.EndDate.ToString();

                                                            if (string.IsNullOrEmpty(FechaOrdenBase))
                                                            {
                                                                FechaOrden = null;
                                                            }
                                                            if (!string.IsNullOrEmpty(FechaOrdenBase))
                                                            {
                                                                FechaOrden = Convert.ToDateTime(FechaOrdenBase);
                                                            }

                                                            ExisteViaje = ListaRecursoOrden.Where(p => p.IdHelm == IdHelmOrden && p.NombreRecurso == NombreRecurso).Count();

                                                            if (ExisteViaje == 0)
                                                            {
                                                                if (NombreTipoRecurso.Contains("PRACTICO") || NombreTipoRecurso.Contains("PRÁCTICO"))
                                                                {
                                                                    ListaRecursoOrden.Add(new RecursoOrden(IdHelmOrden, NombreTipoViajeFecha, NombreRecurso, "P", FechaOrden));
                                                                }
                                                                else
                                                                {
                                                                    ListaRecursoOrden.Add(new RecursoOrden(IdHelmOrden, NombreTipoViajeFecha, NombreRecurso, "O", FechaOrden));
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                if (EsAtraqueDesatraque == 0)
                                                {
                                                    if (NombreServicioFecha.Contains(NombreTipoViajeFecha) && !NombreServicioFecha.Contains(TipoViajeAtraque) && !NombreServicioFecha.Contains(TipoViajeDesatraque))
                                                    {
                                                        dynamic dynJobs = iViajes.Jobs.Children();
                                                        foreach (var iJobs in dynJobs)
                                                        {
                                                            dynamic TipoRecursos = iJobs.RequiredResourceType;
                                                            dynamic Recursos = iJobs.Resource;

                                                            NombreTipoRecurso = TipoRecursos.Name.ToString().ToUpper().Trim();
                                                            NombreRecurso = Recursos.Name.ToString().Trim();

                                                            ExisteViaje = ListaRecursoOrden.Where(p => p.IdHelm == IdHelmOrden && p.NombreRecurso == NombreRecurso).Count();

                                                            if (ExisteViaje == 0)
                                                            {
                                                                ListaRecursoOrden.Add(new RecursoOrden(IdHelmOrden, NombreTipoViajeFecha, NombreRecurso, "O", FechaOrden));
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    #region Obtiene Fecha de Atraque y Desatraque
                                    SqlConnection conexion1 = new SqlConnection(CadenaConexion.CadenaBDIntermedia);

                                    Dictionary<string, object> parametrosIn1 = new Dictionary<string, object>();
                                    Dictionary<string, object> parametrosOut1 = new Dictionary<string, object>();

                                    var dtViajeFactura = ListaViajeFactura.ToDataTable();
                                    var dtRecursoFactura = ListaRecursoFactura.ToDataTable();
                                    var dtRecursoOrden = ListaRecursoOrden.ToDataTable();
                                    DateTime? FechaAtraque = DateTime.Now;
                                    DateTime? FechaDesatraque = DateTime.Now;
                                    string FechaAtraqueInicio = "";
                                    string FechaDesatraqueFinal = "";

                                    parametrosIn1.Add("@dtViajeFactura", dtViajeFactura);
                                    parametrosIn1.Add("@dtRecursoFactura", dtRecursoFactura);
                                    parametrosIn1.Add("@dtRecursoOrden", dtRecursoOrden); ;

                                    parametrosOut1.Add("@FechaAtraque", FechaAtraque);
                                    parametrosOut1.Add("@FechaDesatraque", FechaDesatraque);

                                    using (SqlCommand cmd = SqlHelper.CreateCommandWithParameters("USP_CALCULA_FECHA_ATRAQUE_DESATRAQUE", conexion1, parametrosIn1, true, parametrosOut1))
                                    {
                                        cmd.ExecuteNonQuery();
                                        FechaAtraqueInicio = string.IsNullOrEmpty(cmd.Parameters["@FechaAtraque"].Value.ToString()) ? null : cmd.Parameters["@FechaAtraque"].Value.ToString();
                                        FechaDesatraqueFinal = string.IsNullOrEmpty(cmd.Parameters["@FechaDesatraque"].Value.ToString()) ? null : cmd.Parameters["@FechaDesatraque"].Value.ToString();

                                        if (string.IsNullOrEmpty(FechaAtraqueInicio))
                                        {
                                            FechaAtraque = null;
                                        }
                                        if (!string.IsNullOrEmpty(FechaAtraqueInicio))
                                        {
                                            FechaAtraque = Convert.ToDateTime(FechaAtraqueInicio);
                                        }

                                        if (string.IsNullOrEmpty(FechaDesatraqueFinal))
                                        {
                                            FechaDesatraque = null;
                                        }
                                        if (!string.IsNullOrEmpty(FechaDesatraqueFinal))
                                        {
                                            FechaDesatraque = Convert.ToDateTime(FechaDesatraqueFinal);
                                        }
                                        SqlHelper.CloseConnection(conexion1);
                                    }

                                    documento.FechaAtraque = FechaAtraque;
                                    documento.FechaDesatraque = FechaDesatraque;
                                }
                                #endregion

                                ListaViajeFactura.Clear();
                                ListaRecursoFactura.Clear();
                                ListaRecursoOrden.Clear();

                                if (salir)
                                {
                                    continue;
                                }

                                if (temImposto)
                                {
                                    lstImposto = ValidListaImposto(lstImposto, lstDetalle);
                                }

                                if (!notaCredito)
                                {
                                    if (codigoDocumento == 0)
                                    {
                                        switch (tipoTransacao)
                                        {
                                            case "FACTURA":
                                                codigoDocumento = 184;
                                                prefixoSerie = "F";
                                                sufixoSerie = "F";
                                                break;
                                            case "NOTA CREDITO":
                                                codigoDocumento = 15;
                                                prefixoSerie = "NC";
                                                sufixoSerie = "C";
                                                break;
                                            case "NOTA CREDITO ELECTRONICA":
                                                codigoDocumento = 186;
                                                prefixoSerie = "NC";
                                                sufixoSerie = "C";
                                                break;
                                            case "NOTA DEBITO":
                                                codigoDocumento = 16;
                                                prefixoSerie = "ND";
                                                sufixoSerie = "D";
                                                break;
                                            case "NOTA DEBITO ELECTRONICA":
                                                codigoDocumento = 187;
                                                prefixoSerie = "ND";
                                                sufixoSerie = "D";
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    codigoDocumento = 186;
                                    prefixoSerie = "NC";
                                    sufixoSerie = "C";
                                }

                                if ((codigoDocumento == 184) && (clienteNacional.ToUpper() == "NO"))
                                {
                                    tipoVenda = 3;
                                }
                                else if ((codigoDocumento == 184) || (codigoDocumento == 191))
                                {
                                    tipoVenda = 1;
                                }
                                else
                                {
                                    tipoVenda = 2;
                                }

                                documento.CodigoDocumento = codigoDocumento;
                                if (areaName == "TALARA")
                                {
                                    documento.Serie = prefixoSerie + "002";
                                }
                                else
                                {
                                    documento.Serie = prefixoSerie + "001";
                                }

                                usrfcrfac.USR_MODFOR = "VT";
                                usrCodEmp = (string.IsNullOrEmpty(iTransactions.DivisionAccountingCode.ToString())) ? "TFSVTA" : iTransactions.DivisionAccountingCode.ToString();
                                switch (usrCodEmp)
                                {
                                    case "TFSVTA":
                                        usrfcrfac.USR_CODEMP = "01";
                                        if (areaName == "TALARA")
                                        {
                                            usrfcrfac.USR_DEPOSI = "TFTALA";
                                        }
                                        else
                                        {
                                            usrfcrfac.USR_DEPOSI = "TFCALL";
                                        }
                                        usrTipPro = "TFSVTA";
                                        divisaoEmpresa = "T";
                                        break;
                                    case "NTSVTA":
                                        usrfcrfac.USR_CODEMP = "02";
                                        usrfcrfac.USR_DEPOSI = "NTTALA";
                                        usrTipPro = "NTSVTA";
                                        divisaoEmpresa = "N";
                                        break;
                                    case "DISVTA":
                                        usrfcrfac.USR_CODEMP = "03";
                                        usrfcrfac.USR_DEPOSI = "DITALA";
                                        usrTipPro = "DISVTA";
                                        divisaoEmpresa = "D";
                                        break;
                                }

                                if (areaName == "TALARA")
                                {
                                    usrfcrfac.USR_CODFOR = prefixoSerie + divisaoEmpresa + sufixoSerie + "002";
                                    usrfcrfac.USR_SUCURS = prefixoSerie + "002";
                                }
                                else
                                {
                                    usrfcrfac.USR_CODFOR = prefixoSerie + divisaoEmpresa + sufixoSerie + "001";
                                    usrfcrfac.USR_SUCURS = prefixoSerie + "001";
                                }

                                //if (codigoDocumento != 0)
                                //{
                                //    documento.Numero = context.GetNumeroSequencial(codigoDocumento, areaName);
                                //    usrfcrfac.USR_NROFOR = Convert.ToInt32(documento.Numero);
                                //}

                                //Cliente relacionado en Ventas
                                var querycl = (from t in context.entityHelm.ClienteRelacionado
                                               where ((t.NumeroDocumento == documento.NumeroDocumentoCliente) && (t.EstadoRegistro))
                                               select t).ToList();

                                if (querycl.Count > 0)
                                {
                                    usrfcrfac.USR_CODOPR = "RELA";
                                }
                                else
                                {
                                    usrfcrfac.USR_CODOPR = "COME";
                                }

                                foreach (var item in lstDetalle)
                                {
                                    DocumentoDetalle documentoDetalhe = new DocumentoDetalle();
                                    USR_FCRFAI usrfcrfai = new USR_FCRFAI();

                                    string servicoImposto = "";

                                    numeroLinha += 1;
                                    //documentoDetalhe.IdHelm = iTransactions.Id.ToString();
                                    if (item.nombreArticulo.ToUpper().Contains("SURCHARGE") || item.nombreArticulo.ToUpper().Contains("RECARGO"))
                                    {
                                        documentoDetalhe.IdHelm = item.referenceId;
                                    }
                                    else
                                    {
                                        documentoDetalhe.IdHelm = item.id;
                                    }
                                    documentoDetalhe.ItemDetalle = numeroLinha;
                                    documentoDetalhe.NombreArticulo = item.nombreArticulo.ToString().Trim();
                                    documentoDetalhe.Cantidad = item.cantidad;
                                    documentoDetalhe.PrecioUnitario = item.precioUnitario;
                                    documentoDetalhe.CodigoArticulo = item.codigoArticulo;
                                    documentoDetalhe.EsDescuento = false;
                                    documentoDetalhe.MontoIGV = 0;
                                    documentoDetalhe.Descuento = 0;
                                    documentoDetalhe.TipoPrecio = 2;
                                    try
                                    {
                                        if ((temImposto) && (string.IsNullOrEmpty(item.referenceId)))
                                        {
                                            StrucImposto imposto = new StrucImposto();
                                            imposto = lstImposto.Find(x => (item.nombreArticulo.ToUpper().Contains(x.descricao.Replace("IMPUESTO", "").Trim())) &&
                                                                           (x.remolcador == item.remolcador) &&
                                                                           (x.sequencial == item.sequencial));
                                            if (!String.IsNullOrEmpty(imposto.id))
                                            {
                                                documentoDetalhe.MontoIGV = imposto.valor;
                                                documentoDetalhe.TipoPrecio = 1;

                                                if (lstRebaje.Count > 0)
                                                {
                                                    servicoImposto = imposto.descricao.Replace("IMPUESTO ", "").Trim();
                                                    StructRebaje rebaje = new StructRebaje();
                                                    rebaje = lstRebaje.Find(x => (x.descricao == "REBAJE IMPUESTO POR DESCUENTO " + servicoImposto) &&
                                                                                 (x.remolcador == imposto.remolcador) &&
                                                                                  (x.sequencial == imposto.sequencial)); //DCP-08/07/2020 Secuencial
                                                    if (!string.IsNullOrEmpty(rebaje.id))
                                                    {
                                                        documentoDetalhe.MontoIGV -= rebaje.valor;
                                                        documentoDetalhe.TipoPrecio = 1;
                                                    }
                                                }
                                            }

                                            somaValorIGV += documentoDetalhe.MontoIGV.Value;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        somaValorIGV += documentoDetalhe.MontoIGV.Value;
                                    }

                                    usrfcrfai.USR_PCTBF1 = 0;
                                    if (lstDesconto.Count > 0)
                                    {
                                        StrucDesconto desconto = new StrucDesconto();

                                        //Si es Recargo colocar el % de Descuento y llegue a Ofisis correctamente
                                        if (item.nombreArticulo.ToUpper().Contains("SURCHARGE") || item.nombreArticulo.ToUpper().Contains("RECARGO"))
                                        {
                                            desconto = lstDesconto.Find(x => (x.id == item.referenceId));
                                            documentoDetalhe.IdHelm = item.referenceId;
                                        }
                                        else
                                        {
                                            desconto = lstDesconto.Find(x => (x.id == item.id));
                                            documentoDetalhe.IdHelm = item.id;
                                        }

                                        if (!string.IsNullOrEmpty(desconto.id))
                                        {
                                            if (item.nombreArticulo.ToUpper().Contains("SURCHARGE") || item.nombreArticulo.ToUpper().Contains("RECARGO"))
                                            {
                                                documentoDetalhe.Descuento = 0;
                                                documentoDetalhe.EsDescuento = false;
                                                documentoDetalhe.PorcentajePercepcionArticulo = desconto.taxa;
                                                usrfcrfai.USR_PCTBF1 = desconto.taxa * -1;
                                            }
                                            else
                                            {
                                                documentoDetalhe.Descuento = Math.Abs(desconto.valor);
                                                somaDesconto += Math.Abs(desconto.valor);
                                                documentoDetalhe.EsDescuento = true;
                                                documentoDetalhe.PorcentajePercepcionArticulo = desconto.taxa;
                                                usrfcrfai.USR_PCTBF1 = desconto.taxa * -1;
                                            }
                                        }
                                        else
                                        {
                                            desconto = lstDesconto.Find(x => (item.nombreArticulo.ToUpper().Contains(x.descricao.Replace("DISCOUNT", "").Trim())) &&
                                                                             (x.remolcador == item.remolcador) &&
                                                                             (x.sequencial == item.sequencial));
                                            if (desconto.valor == 0)
                                            {
                                                desconto = lstDesconto.Find(x => (item.nombreArticulo.ToUpper().Contains(x.descricao.Replace("DESCUENTO", "").Trim())) &&
                                                                             (x.remolcador == item.remolcador) &&
                                                                             (x.sequencial == item.sequencial));
                                            }

                                            if (desconto.valor != 0)
                                            {
                                                documentoDetalhe.Descuento = Math.Abs(desconto.valor);
                                                somaDesconto += Math.Abs(desconto.valor);
                                                documentoDetalhe.EsDescuento = true;
                                                documentoDetalhe.PorcentajePercepcionArticulo = desconto.taxa;
                                                usrfcrfai.USR_PCTBF1 = desconto.taxa * -1;
                                            }
                                        }
                                    }

                                    documentoDetalhe.MontoBruto = Math.Abs(item.subTotal);
                                    documentoDetalhe.SubTotal = Math.Abs(item.subTotal);
                                    documentoDetalhe.NombreUnidadMedida = "Unidad";
                                    documentoDetalhe.SiglaUnidadMedida = "UN";
                                    documentoDetalhe.UnidadMedidaSunat = "ZZ";
                                    documentoDetalhe.MontoMinimoConsumidorFinal = 0;
                                    documentoDetalhe.MontoISC = 0;
                                    documentoDetalhe.TasaISC = 0;
                                    documentoDetalhe.TipoISC = 0;
                                    documentoDetalhe.TipoPercepcion = 0;
                                    documentoDetalhe.ImportePercepcion = 0;
                                    documentoDetalhe.PorcentajePercepcionVenta = 0;

                                    somaFatura += documentoDetalhe.MontoBruto.Value - documentoDetalhe.Descuento.Value;
                                    valorBruto += documentoDetalhe.MontoBruto.Value;

                                    usrfcrfai.USR_FCRFAI_MODFOR = usrfcrfac.USR_MODFOR;
                                    usrfcrfai.USR_FCRFAI_CODFOR = usrfcrfac.USR_CODFOR;
                                    //usrfcrfai.USR_FCRFAI_NROFOR = usrfcrfac.USR_NROFOR;
                                    usrfcrfai.USR_FCRFAI_CODEMP = usrfcrfac.USR_CODEMP;
                                    usrfcrfai.USR_NROITM = numeroLinha;
                                    usrfcrfai.USR_TIPPRO = usrTipPro;

                                    if (ordemNula)
                                    {
                                        var query = (from t in context.entityHelm.TarifaDetraccion
                                                     where (t.NombreArticulo.ToUpper().Trim() == item.nombreArticulo.ToUpper().Trim())
                                                     select t).ToList();
                                        if (query.Count > 0)
                                        {
                                            salir = false;
                                            usrfcrfai.USR_ARTCOD = query[0].CodigoProducto;
                                            usrfcrfai.USR_CODDIS = query[0].CentroBeneficio;
                                            documentoDetalhe.CodigoArticulo = (string.IsNullOrEmpty(documentoDetalhe.CodigoArticulo)) ?
                                                                               query[0].CentroBeneficio : documentoDetalhe.CodigoArticulo;

                                            documentoDetalhe.CodigoProducto = (string.IsNullOrEmpty(documentoDetalhe.CodigoProducto)) ?
                                                                               query[0].CodigoProducto : documentoDetalhe.CodigoProducto;

                                            query[0].TopeMinimoSoles = (query[0].TopeMinimoSoles == null) ? 0 : query[0].TopeMinimoSoles;
                                            query[0].TopeMinimoDolares = (query[0].TopeMinimoDolares == null) ? 0 : query[0].TopeMinimoDolares;
                                            if (montoDTR == -1)
                                            {
                                                montoDTR = (moeda == "SOL") ? (decimal)query[0].TopeMinimoSoles : (decimal)query[0].TopeMinimoDolares;
                                            }

                                            codigoDetraccion = (string.IsNullOrEmpty(codigoDetraccion)) ? query[0].CodigoDetraccion : codigoDetraccion;
                                            query[0].PorcentajeDetraccion = (query[0].PorcentajeDetraccion == null) ? 0 : query[0].PorcentajeDetraccion;
                                            porcentajeDetraccion = (porcentajeDetraccion == -1) ? (int)query[0].PorcentajeDetraccion : (int)porcentajeDetraccion;
                                        }
                                        else
                                        {

                                            errores.IdTransaccion = iTransactions.Id.ToString();
                                            errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());
                                            errores.ErrorDetalle.Add(string.Format("El servicio {0} no existe", item.nombreArticulo.ToUpper().Trim()));
                                            Correo correo = new Correo();

                                            try
                                            {
                                                construirCorreoError(correo, iTransactions, errores);
                                                EnviarCorreoElectronico(correo, true);
                                                if (setPosted)
                                                {
                                                    SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                                }
                                                salir = true;
                                                break;
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error($"- Error al enviar correo con información de errores. " + ArmarLog(documento, iTransactions), ex);

                                                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string servico = (item.nombreArticulo.ToUpper().Trim() == "CARGO DE ACCESO") ? "CARGO DE ACCESO" : "SERVICIOS DESDE HELM";
                                        var query = (from t in context.entityHelm.TarifaDetraccion
                                                     where (t.NombreArticulo.ToUpper().Trim() == servico.ToUpper().Trim())
                                                     select t).ToList();

                                        if (query.Count > 0)
                                        {
                                            codigoDetraccion = query[0].CodigoDetraccion;
                                            porcentajeDetraccion = (int)query[0].PorcentajeDetraccion;
                                            usrfcrfai.USR_ARTCOD = (item.nombreArticulo.ToUpper().Trim() == "CARGO DE ACCESO") ? query[0].CodigoProducto : item.codigoProduto;
                                            usrfcrfai.USR_CODDIS = item.codigoArticulo;
                                            documentoDetalhe.CodigoProducto = usrfcrfai.USR_ARTCOD;
                                            montoDTR = (moeda == "SOL") ? (decimal)query[0].TopeMinimoSoles : (decimal)query[0].TopeMinimoDolares;
                                        }
                                    }

                                    usrfcrfai.USR_IDHELM = iTransactions.TransactionNumber.ToString();
                                    usrfcrfai.USR_MODCPT = "VT";
                                    usrfcrfai.USR_TIPCPT = "A";

                                    //Si es Recargo Tomar el Concepto de sus padres, siempre son los primeros servicios
                                    if (item.nombreArticulo.ToUpper().Contains("SURCHARGE") || item.nombreArticulo.ToUpper().Contains("RECARGO"))
                                    {
                                        usrfcrfai.USR_CODCPT = CodigoConcepto;
                                    }
                                    else
                                    {
                                        if ((documento.CodigoDocumentoCliente == 1 || documento.CodigoDocumentoCliente == 2) &&
                                           (documentoDetalhe.MontoIGV > 0) &&
                                           (usrfcrfac.USR_CODOPR == "COME"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVG001";
                                            CodigoConcepto = "AVG001";
                                        }
                                        else if ((documento.CodigoDocumentoCliente == 1 || documento.CodigoDocumentoCliente == 2) &&
                                           (documentoDetalhe.MontoIGV > 0) &&
                                           (usrfcrfac.USR_CODOPR == "RELA"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVG003";
                                            CodigoConcepto = "AVG003";
                                        }
                                        else if ((documento.CodigoDocumentoCliente == 1 || documento.CodigoDocumentoCliente == 2) &&
                                           (documentoDetalhe.MontoIGV == 0) &&
                                           (usrfcrfac.USR_CODOPR == "COME"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVI002";
                                            CodigoConcepto = "AVI002";
                                        }
                                        else if ((documento.CodigoDocumentoCliente == 1 || documento.CodigoDocumentoCliente == 2) &&
                                           (documentoDetalhe.MontoIGV == 0) &&
                                           (usrfcrfac.USR_CODOPR == "RELA"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVI004";
                                            CodigoConcepto = "AVI004";
                                        }
                                        else if ((documento.CodigoDocumentoCliente == 114) &&
                                           (documentoDetalhe.MontoIGV > 0) &&
                                           (usrfcrfac.USR_CODOPR == "COME"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVG005";
                                            CodigoConcepto = "AVG005";
                                        }
                                        else if ((documento.CodigoDocumentoCliente == 114) &&
                                           (documentoDetalhe.MontoIGV > 0) &&
                                           (usrfcrfac.USR_CODOPR == "RELA"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVG006";
                                            CodigoConcepto = "AVG006";
                                        }
                                        else if ((documento.CodigoDocumentoCliente == 114) &&
                                           (documentoDetalhe.MontoIGV == 0) &&
                                           (usrfcrfac.USR_CODOPR == "COME"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVX001";
                                            CodigoConcepto = "AVX001";
                                        }
                                        else if ((documento.CodigoDocumentoCliente == 114) &&
                                           (documentoDetalhe.MontoIGV == 0) &&
                                           (usrfcrfac.USR_CODOPR == "RELA"))
                                        {
                                            usrfcrfai.USR_CODCPT = "AVX002";
                                            CodigoConcepto = "AVX002";
                                        }
                                        else
                                        {
                                            usrfcrfai.USR_CODCPT = "";
                                            CodigoConcepto = "";
                                        }
                                    }

                                    usrfcrfai.USR_PRECIO = documentoDetalhe.PrecioUnitario;
                                    usrfcrfai.USR_CANTID = documentoDetalhe.Cantidad;
                                    usrfcrfai.USR_TEXTOD = null;
                                    usrfcrfai.USR_FC_FECALT = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:sszzz"));

                                    if (valorBruto != 0)
                                    {
                                        if (documento.CodigoDocumento == 184)
                                        {
                                            if (interfaceFaturaAtiva)
                                            {
                                                listDetalle.Add(documentoDetalhe);
                                                //context.IncluirDocumentoDetalle(documentoDetalhe);
                                            }

                                            if (interfaceOfisisAtiva)
                                            {
                                                listUsrfcrfai.Add(usrfcrfai);
                                                //context.IncluirUsrFcrFai(usrfcrfai);
                                            }
                                        }
                                        else
                                        {
                                            if (somaValorIGV != 0)
                                            {
                                                if (interfaceFaturaAtiva)
                                                {
                                                    listDetalle.Add(documentoDetalhe);
                                                    //context.IncluirDocumentoDetalle(documentoDetalhe);
                                                }

                                                if (interfaceOfisisAtiva)
                                                {
                                                    listUsrfcrfai.Add(usrfcrfai);
                                                    //context.IncluirUsrFcrFai(usrfcrfai);
                                                }
                                            }
                                        }
                                    }
                                }

                                //Sale del bucle para Tx Manuales que no esten en la BD
                                if (salir)
                                {
                                    continue;
                                }

                                documento.TotalDescuento = Math.Abs(somaDesconto);
                                documento.DescuentoGlobal = Math.Abs(somaDesconto);

                                if (somaValorIGV == 0)
                                {
                                    documento.MontoInafecto = Math.Abs(valorBruto);
                                    documento.MontoAfecto = 0;
                                }
                                else
                                {
                                    documento.MontoInafecto = 0;
                                    documento.MontoAfecto = Math.Abs(valorBruto) - Math.Abs(somaDesconto);
                                }

                                documento.MontoIGV = Math.Abs(somaValorIGV);

                                //Si es Boleta y su IGV es Cero no debe crear factura Sunat, solo debe contabilizar
                                if (documento.CodigoDocumento == 191 && documento.MontoIGV == 0)
                                {

                                    errores.IdTransaccion = iTransactions.Id.ToString();
                                    errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());
                                    errores.ErrorDetalle.Add("El monto del IGV en Boleta de Venta no puede ser 0");
                                    Correo correo = new Correo();
                                    
                                    try
                                    {
                                        construirCorreoError(correo, iTransactions, errores);
                                        EnviarCorreoElectronico(correo, true);
                                        if (setPosted)
                                        {
                                            SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                        }
                                        continue;
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error($"- Error al enviar correo con información de errores. " + ArmarLog(documento, iTransactions), ex);

                                        new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
                                    }

                                }

                                documento.MontoTotal = Math.Abs(somaFatura) + documento.MontoIGV;
                                //if (documento.MontoTotal == 0)
                                //{

                                //errores.ErrorDetalle.Add("El monto total no puede ser 0");
                                //Correo correo = new Correo();
                                //construirCorreoError(correo, iTransactions, errores);
                                //try
                                //{
                                //    EnviarCorreoElectronico(correo, true);
                                //if (setPosted)
                                //{
                                //    SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                //}
                                //continue;
                                //}
                                //catch (Exception)
                                //{
                                //    throw;
                                //}
                                //}
                                documento.MontoISC = 0;
                                documento.MontoExonerado = 0;
                                documento.MontoDonacion = 0;
                                documento.MontoRegalo = 0;
                                documento.TipoIGV = 2;
                                documento.AfectoDetraccion = false;
                                documento.ClienteConsumidorFinal = 1;
                                documento.TipoAgenteTributario = 0;
                                documento.Estado = true;
                                documento.FechaHoraCreacion = DateTime.Now;
                                documento.FlagSunat = false;
                                documento.FlagFE = false;
                                documento.TipoVenta = tipoVenda;
                                documento.FechaVencimiento = documento.FechaEmision.Value.AddDays(qtdDias);

                                if ((clienteNacional.ToUpper() == "SÍ") &&
                                    (documento.MontoIGV.Value > 0) &&
                                    (codigoDocumento == 184) &&
                                    (documento.MontoTotal.Value > montoDTR))
                                {
                                    documento.CodigoDetraccion = codigoDetraccion;
                                    documento.PorcentajeDetraccion = porcentajeDetraccion;
                                    documento.GlosaDetraccion = "OPERACION SUJETA A SPOT";
                                    documento.AfectoDetraccion = true;
                                }

                                usrfcrfac.USR_TIPORI = null;
                                switch (prefixoSerie)
                                {
                                    case "F":
                                        usrfcrfac.USR_CIRAPL = "VLO500";
                                        usrfcrfac.USR_CIRCOM = "VLO500";
                                        usrfcrfac.USR_CODCOM = "RFAC";
                                        usrfcrfac.USR_NROORI = null;
                                        usrfcrfac.USR_FECORI = null;
                                        usrfcrfac.USR_TIPORI = null;
                                        usrfcrfac.USR_MOTDEV = null;
                                        break;
                                    case "B":
                                        usrfcrfac.USR_CIRAPL = "VLO500";
                                        usrfcrfac.USR_CIRCOM = "VLO500";
                                        usrfcrfac.USR_CODCOM = "RBOL";
                                        usrfcrfac.USR_NROORI = null;
                                        usrfcrfac.USR_FECORI = null;
                                        usrfcrfac.USR_TIPORI = null;
                                        usrfcrfac.USR_MOTDEV = null;
                                        break;
                                    case "NC":
                                        usrfcrfac.USR_CIRAPL = "VLO600";
                                        usrfcrfac.USR_CIRCOM = "VLO600";
                                        usrfcrfac.USR_CODCOM = "RNCR";
                                        usrfcrfac.USR_NROORI = iTransactions.TransactionNumber.ToString();
                                        usrfcrfac.USR_FECORI = Convert.ToDateTime(iTransactions.TransactionDate.ToString("yyyy-MM-dd"));
                                        usrfcrfac.USR_MOTDEV = "";
                                        break;
                                    case "ND":
                                        usrfcrfac.USR_CIRAPL = "VLO610";
                                        usrfcrfac.USR_CIRCOM = "VLO610";
                                        usrfcrfac.USR_CODCOM = "RNDE";
                                        usrfcrfac.USR_NROORI = iTransactions.TransactionNumber.ToString();
                                        usrfcrfac.USR_FECORI = Convert.ToDateTime(iTransactions.TransactionDate.ToString("yyyy-MM-dd"));
                                        usrfcrfac.USR_MOTDEV = "";
                                        break;
                                }

                                usrfcrfac.USR_CNDPAG = documento.FormaPago;

                                if (iTransactions.CurrencyType.ShortName.ToString().ToUpper() == "SOL")
                                {
                                    usrfcrfac.USR_CODLIS = "PEN";
                                }
                                else
                                {
                                    usrfcrfac.USR_CODLIS = iTransactions.CurrencyType.ShortName.ToString();
                                }

                                usrfcrfac.USR_IDHELM = iTransactions.TransactionNumber.ToString();
                                usrfcrfac.USR_FCHMOV = Convert.ToDateTime(iTransactions.TransactionDate.ToString("yyyy-MM-dd"));

                                //Si tiene menor a 11 digitos y es DNI
                                if ((documento.NumeroDocumentoCliente.Length < 11) && (documento.CodigoDocumentoCliente == 1))
                                {
                                    usrfcrfac.USR_NROCTA = documento.NumeroDocumentoCliente.PadLeft(11, '0');
                                }
                                else
                                {
                                    usrfcrfac.USR_NROCTA = documento.NumeroDocumentoCliente;
                                }

                                usrfcrfac.USR_OCCLIE = documento.OrdenCompra;
                                usrfcrfac.USR_SECTOR = "0";
                                usrfcrfac.USR_TEXTOS = iTransactions.Note.ToString();
                                usrfcrfac.USR_CAMSEC = 1;
                                usrfcrfac.USR_MODCOM = "VT";
                                usrfcrfac.USR_TIPOPR = "GEN";
                                usrfcrfac.USR_COMELE = "S";
                                usrfcrfac.USR_IMBAAF = documento.MontoAfecto;
                                usrfcrfac.USR_IMBAIN = documento.MontoInafecto;
                                usrfcrfac.USR_IMBAEX = 0;
                                usrfcrfac.USR_IMIMPU = documento.MontoIGV;
                                usrfcrfac.USR_IMTOTA = documento.MontoTotal;
                                usrfcrfac.USR_ESTDOC = "ACT";
                                usrfcrfac.USR_CODACT = documento.CodigoDetraccion;
                                usrfcrfac.USR_FC_FECALT = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:sszzz"));
                                usrfcrfac.USR_FETIOP = "0101";
                                usrfcrfac.USR_NROSUB = documento.NumeroDocumentoSolidario;
                                //usrfcrfac.USR_DENAVE = documento.NombreNave2;
                                //CAMBIO REALIZADO EN COORDINACION CON JONATHAN: 30/10/2020
                                usrfcrfac.USR_DENAVE = documento.NombreNave;
                                usrfcrfac.USR_PUERTO = Convert.ToInt32(documento.IdPuerto);

                                //NO SE GRABA AQUI. LA DATA NO PERSISTE HASTA QUE TRAIGA TODOS LOS DATOS CORRECTOS. EST 31/10/2020
                                //if (documento.MontoTotal != 0)
                                //{
                                //    context.SetNumeroSequencial(documento.CodigoDocumento.Value, areaName);

                                //    if (interfaceFaturaAtiva)
                                //    {   
                                //        context.IncluirDocumento(documento);
                                //        context.PersistirFatura();

                                //        if (documento != null)
                                //        {
                                //            log.Info($"DOCUMENTO GRABADO EN BDI: {documento.Serie} - {documento.Numero}. " +
                                //            $"CLIENTE: {documento.NumeroDocumentoCliente} {documento.NombreCliente}." +
                                //            $"Nro. Transacción HELM: {documento.TransaccionHelm}. Orden: {documento.OrdenCompra}");
                                //        }
                                //    }

                                //    if (interfaceOfisisAtiva)
                                //    {
                                //        context.IncluirUsrFcrFac(usrfcrfac);
                                //        context.PersistirOfisis();
                                //    }
                                //}

                                #endregion

                                #region Validar Listas Integrales por comprobante

                                if (ListaIntegral.Count > 0)
                                {
                                    foreach (dynamic ItemDocIntegral in ListaIntegral.ToList())
                                    {
                                        string IdHelmItemIntegral = ItemDocIntegral.IdHelmIntegral.ToString();
                                        string NombreServicioItemIntegral = ItemDocIntegral.NombreServicioIntegral.ToString();

                                        // Verificar que la orden cumpla con la factura y obtener recursos, agregar lancha
                                        // Hacer Match Factura vs Orden, validar por Tipo de Viaje y Nombre de Recursos, solo considerar
                                        //JObject jobjOrder2 = JObject.Parse(GetSyncApi($"api/v1/Jobs/Orders/FindOrders?page=1&OrderNumber={iTransactions.Order.OrderNumber.ToString()}", client));
                                        dynamic dynOrder = (JArray)jobjOrder["Data"]["Page"];

                                        foreach (var iOrder in dynOrder)
                                        {
                                            dynamic dynViajes = iOrder.Trips.Children();
                                            foreach (var iViajes in dynViajes)
                                            {
                                                NombreTipoViaje = "";
                                                dynamic TipoViaje = iViajes.Triptype;
                                                NombreTipoViaje = TipoViaje.Name.ToString().ToUpper().Trim();
                                                int CuentaTipoRercurso = ListaRecurso.Where(p => p.IdHelmIntegral == IdHelmItemIntegral && p.TipoRecurso == "").Count();

                                                if (CuentaTipoRercurso > 0)
                                                {
                                                    if (NombreServicioItemIntegral.Contains(NombreTipoViaje))
                                                    {
                                                        dynamic dynJobs = iViajes.Jobs.Children();
                                                        foreach (var iJobs in dynJobs)
                                                        {
                                                            CantidadRercurso = 0;
                                                            dynamic TipoRecursos = iJobs.RequiredResourceType;
                                                            dynamic Recursos = iJobs.Resource;

                                                            NombreTipoRecurso = TipoRecursos.Name.ToString().ToUpper().Trim();
                                                            NombreRecurso = Recursos.Name.ToString().Trim();
                                                            CantidadRercurso = ListaRecurso.Where(p => p.IdHelmIntegral == IdHelmItemIntegral && p.NombreRecurso == NombreRecurso).Count();

                                                            if (NombreTipoRecurso.Contains("REMOLCADOR") && CantidadRercurso > 0)
                                                            {
                                                                ListaRecurso.Find(p => p.IdHelmIntegral == IdHelmItemIntegral && p.NombreRecurso == NombreRecurso).TipoRecurso = "R";
                                                            }
                                                            if ((NombreTipoRecurso.Contains("PRÁCTICO") || NombreTipoRecurso.Contains("PRACTICO")) && CantidadRercurso > 0)
                                                            {
                                                                ListaRecurso.Find(p => p.IdHelmIntegral == IdHelmItemIntegral && p.NombreRecurso == NombreRecurso).TipoRecurso = "P";

                                                                // Busca CB del practico por Sede
                                                                var query = (from t in context.entityHelm.CentroBeneficioPracticoSede
                                                                             where (t.IdPuerto == documento.IdPuerto)
                                                                             select t).ToList();

                                                                if (query.Count() > 0)
                                                                {
                                                                    string CentroBeneficioX = query.First().CentroBeneficio.ToString();
                                                                    ListaRecurso.Find(p => p.IdHelmIntegral == IdHelmItemIntegral && p.NombreRecurso == NombreRecurso).CentroBeneficioR = CentroBeneficioX;
                                                                }
                                                            }
                                                            if (NombreTipoRecurso.Contains("LANCHA") && CantidadRercurso > 0)
                                                            {
                                                                ListaRecurso.Find(p => p.IdHelmIntegral == IdHelmItemIntegral && p.NombreRecurso == NombreRecurso).TipoRecurso = "L";
                                                            }
                                                            if (CantidadRercurso == 0)
                                                            {
                                                                ListaRecurso.Find(p => p.IdHelmIntegral == IdHelmItemIntegral).TipoRecurso = "";
                                                                break;
                                                            }
                                                        }

                                                        // Luego de hacer Match la Factura vs Orden, validar Lancha sin cargo
                                                        if (CantidadRercurso > 0)
                                                        {
                                                            dynamic SitioFromLocation = iViajes.FromLocation;
                                                            dynamic SitioToLocation = iViajes.ToLocation;

                                                            //FechaManiobra1 = Convert.ToDateTime(iViajes.StartDate.ToString());

                                                            if (!string.IsNullOrEmpty(iViajes.StartDate.ToString()))
                                                            {
                                                                FechaManiobra1 = Convert.ToDateTime(iViajes.StartDate.ToString());
                                                            }
                                                            else
                                                            {
                                                                FechaManiobra1 = DateTime.Now;
                                                            }

                                                            try
                                                            {
                                                                if (!string.IsNullOrEmpty(SitioFromLocation.ExternalSystemCode.ToString()) && CodigoSitioFromLocation == 0)
                                                                {
                                                                    CodigoSitioFromLocation = Convert.ToInt32(SitioFromLocation.ExternalSystemCode.ToString());
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                string mensajeError = "- Variable SitioFromLocation.ExternalSystemCode - FromLocation no traída de HELM.";
                                                                //log.Error(mensajeError);

                                                                throw new Exception(mensajeError, ex);
                                                            }

                                                            try
                                                            {
                                                                if (!string.IsNullOrEmpty(SitioToLocation.ExternalSystemCode.ToString()) && CodigoSitioToLocation == 0)
                                                                {
                                                                    CodigoSitioToLocation = Convert.ToInt32(SitioToLocation.ExternalSystemCode.ToString());
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                string mensajeError = "- Variable SitioToLocation.ExternalSystemCode - ToLocation no traída de HELM.";
                                                                //log.Error(mensajeError);

                                                                throw new Exception(mensajeError, ex);
                                                            }

                                                            FlagManiobra = false;
                                                            foreach (var FechaManiobra in ListaFechaManiobra.ToList())
                                                            {
                                                                if (FechaManiobra == FechaManiobra1)
                                                                {
                                                                    FlagManiobra = true;
                                                                }
                                                                if (FlagManiobra)
                                                                {
                                                                    break;
                                                                }
                                                            }
                                                            if (!FlagManiobra)
                                                            {
                                                                //JObject jobjOrder3 = JObject.Parse(GetSyncApi($"api/v1/Jobs/Orders/FindOrders?page=1&OrderNumber={iTransactions.Order.OrderNumber.ToString()}", client));
                                                                dynamic dynOrder2 = (JArray)jobjOrder["Data"]["Page"];

                                                                foreach (var iOrder2 in dynOrder2)
                                                                {
                                                                    dynamic dynViajes2 = iOrder2.Trips.Children();
                                                                    foreach (var iViajes2 in dynViajes2)
                                                                    {
                                                                        LanchaSinCargo = Convert.ToBoolean(iViajes2.IsNoCharge.ToString());
                                                                        dynamic TipoViaje2 = iViajes2.Triptype;
                                                                        NombreTipoViajeSinCargo = TipoViaje2.Name.ToString().ToUpper().Trim();

                                                                        if (LanchaSinCargo) // Viaje (Lancha) sin Cargo
                                                                        {
                                                                            //FechaManiobra2 = Convert.ToDateTime(iViajes2.StartDate.ToString());

                                                                            if (!string.IsNullOrEmpty(iViajes2.StartDate.ToString()))
                                                                            {
                                                                                FechaManiobra2 = Convert.ToDateTime(iViajes2.StartDate.ToString());
                                                                            }
                                                                            else
                                                                            {
                                                                                FechaManiobra2 = DateTime.Now;
                                                                            }

                                                                            DifHoras = FechaManiobra1 - FechaManiobra2;
                                                                            DiferenciaHoras = Math.Abs(Convert.ToDecimal(DifHoras.TotalHours));

                                                                            if (DiferenciaHoras <= 5) // Diferencia de 5 horas como maximo (corresponde a su maniobra de practico (lancha))
                                                                            {
                                                                                ServicioSinCargo = NombreServicioItemIntegral;
                                                                                TipoManiobraSinCargo = ServicioSinCargo.Substring(ServicioSinCargo.IndexOf("-") + 1, ServicioSinCargo.Length - ServicioSinCargo.IndexOf("-") - 1).Trim();

                                                                                if (NombreTipoViajeSinCargo.Contains(TipoManiobraSinCargo) && (NombreTipoViajeSinCargo.Contains("PRÁCTICO") || NombreTipoViajeSinCargo.Contains("PRACTICO") || NombreTipoViajeSinCargo.Contains("PILOT")))
                                                                                {
                                                                                    // Si ingresa se asigna el tipo de recurso a Lancha para colocar la distribucion correcta, asi usen lancha o RAM
                                                                                    dynamic dynJobs2 = iViajes2.Jobs.Children();
                                                                                    foreach (var iJobs2 in dynJobs2)
                                                                                    {
                                                                                        dynamic TipoRecursos2 = iJobs2.RequiredResourceType;
                                                                                        dynamic Recursos2 = iJobs2.Resource;

                                                                                        NombreTipoRecurso = TipoRecursos2.Name.ToString().ToUpper().Trim();
                                                                                        NombreRecurso = Recursos2.Name.ToString().Trim();
                                                                                        CentroBeneficioSinCargo = "";

                                                                                        if (NombreTipoRecurso.Contains("LANCHA"))
                                                                                        {
                                                                                            TipoRecurso = "L";
                                                                                        }
                                                                                        if (NombreTipoRecurso.Contains("REMOLCADOR"))
                                                                                        {
                                                                                            TipoRecurso = "R";
                                                                                        }
                                                                                        if (NombreTipoRecurso.Contains("PRÁCTICO") || NombreTipoRecurso.Contains("PRACTICO"))
                                                                                        {
                                                                                            TipoRecurso = "P";
                                                                                        }

                                                                                        // Obtener el Centro de Beneficio de la Lancha
                                                                                        JObject jobjRecurso2 = JObject.Parse(GetSyncApi($"api/v1/Jobs/resources/FindResources?page=1&Name={NombreRecurso}", client));
                                                                                        dynamic dynRecurso2 = (JArray)jobjRecurso2["Data"]["Page"];

                                                                                        foreach (var iRecurso2 in dynRecurso2)
                                                                                        {
                                                                                            CentroBeneficioSinCargo = iRecurso2.AccountingCode.ToString().ToUpper();
                                                                                        }

                                                                                        // Controlar que no repita lanchas para el atraque/desatraque (colocar 1 sola lancha por maniobra de AT/DT)
                                                                                        int X = ListaRecurso.Where(p => p.IdHelmIntegral == IdHelmItemIntegral && p.TipoRecurso == "L").Count();
                                                                                        if (X == 0)
                                                                                        {
                                                                                            ListaFechaManiobra.Add(FechaManiobra1);
                                                                                            ListaRecurso.Add(new Recurso(IdHelmItemIntegral, NombreServicioItemIntegral, NombreRecurso, TipoRecurso, CentroBeneficioSinCargo, TipoManiobraSinCargo));
                                                                                        }
                                                                                        //ListaFechaManiobra.Add(FechaManiobra1);
                                                                                        //ListaRecurso.Add(new Recurso(IdHelmItemIntegral, NombreServicioItemIntegral, NombreRecurso, TipoRecurso, CentroBeneficioSinCargo, TipoManiobraSinCargo));
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                #endregion

                                #region Persistencia (BD) de Comprobante en BDI y OFISIS

                                //REALIZAMOS AFINACION DE PROPIEDADES - BDI
                                documento.NumeroDocumentoCliente = documento.NumeroDocumentoCliente.Trim();

                                if (!String.IsNullOrEmpty(documento.Observaciones))
                                {
                                    var len = Math.Min(200, documento.Observaciones.Length); //200 es la longitud máxima
                                    documento.Observaciones = documento.Observaciones.Substring(0, len);
                                }
                                
                                //REALIZAMOS AFINACION DE PROPIEDADES - OFISIS
                                usrfcrfac.USR_NROCTA = usrfcrfac.USR_NROCTA.Trim();

                                //REALIZAMOS VALIDACIÓN DE CONSISTENCIA DE INFORMACIÓN PREVIA A GUARDAR
                                string mensajeValidacion = ValidacionPreviaGuardar(codigoDocumento, documento, listDetalle);
                                if (mensajeValidacion != string.Empty)
                                    throw new Exception(mensajeValidacion);

                                //REALIZAMOS EVALUACIÓN PARA QUITAR HORA EN FECHA ATRAQUE/DESATRAQUE (LANCHAS)
                                EvaluarReferenciaLanchas(documento, listDetalle);

                                //GUARDAMOS FACTURA, PORQUE SE IDENTIFICA QUE TODO ES CORRECTO
                                if (interfaceFaturaAtiva)
                                {
                                    try
                                    {
                                        //Asignamos número correlativo a comprobante
                                        documento.Numero = context.GetNumeroSequencial(codigoDocumento, areaName);

                                        foreach (var detalle in listDetalle)
                                        {
                                            context.IncluirDocumentoDetalle(detalle);
                                        }

                                        context.IncluirDocumento(documento);
                                    }
                                    catch (Exception ex)
                                    {
                                        context.RevertirFactura();

                                        //Informamos con LOG de error
                                        string mensajeError = "ERROR al guardar en BDI.";
                                        //log.Error(mensajeError);
                                        throw new Exception(mensajeError, ex);
                                    }
                                }

                                if (interfaceOfisisAtiva)
                                {
                                    try
                                    {
                                        //Asignamos Número Correlativo
                                        usrfcrfac.USR_NROFOR = Convert.ToInt32(documento.Numero);

                                        foreach (var usrfcrfai in listUsrfcrfai)
                                        {
                                            usrfcrfai.USR_FCRFAI_NROFOR = usrfcrfac.USR_NROFOR;
                                            context.IncluirUsrFcrFai(usrfcrfai);
                                        }

                                        context.IncluirUsrFcrFac(usrfcrfac);
                                    }
                                    catch (Exception ex)
                                    {
                                        context.RevertirOfisis();
                                        context.RevertirFactura();

                                        //Informamos con LOG de error
                                        string mensajeError = "- ERROR al guardar en OFISIS y BDI.";
                                        //log.Error(mensajeError);
                                        throw new Exception(mensajeError, ex);
                                    }
                                }

                                #endregion

                                #region Hacemos persistir el documento en BDI
                                if (interfaceFaturaAtiva)
                                {
                                    context.PersistirFatura();

                                    //Informamos con LOG que se grabó
                                    log.Info($"GRABACIÓN EXITOSA EN BDI. " + ArmarLog(documento, iTransactions));
                                }
                                #endregion

                                #region Hacemos persistir el documento en OFISIS
                                if (interfaceOfisisAtiva)
                                {
                                    context.PersistirOfisis();

                                    //Informamos con LOG que se grabó
                                    log.Info($"- GRABACIÓN EXITOSA EN OFISIS. " + ArmarLog(documento, iTransactions));
                                }
                                #endregion

                                #region Actualizamos número correlativo de comprobante

                                context.SetNumeroSequencial(documento.CodigoDocumento.Value, areaName);

                                #endregion

                                #region Distribuir de Tarifa Integral - Persistencia en BDI

                                if (ListaIntegral.Count > 0)
                                {
                                    SqlConnection conexion = new SqlConnection(CadenaConexion.CadenaBDIntermedia);

                                    Dictionary<string, object> parametrosIn = new Dictionary<string, object>();
                                    var dtRecurso = ListaRecurso.ToDataTable();
                                    parametrosIn.Add("@dtRecurso", dtRecurso);
                                    parametrosIn.Add("@CodigoSitioFromLocation", CodigoSitioFromLocation);
                                    parametrosIn.Add("@CodigoSitioToLocation", CodigoSitioToLocation);
                                    parametrosIn.Add("@IdCodigoDocumento", documento.IdCodigoDocumento);

                                    using (SqlCommand cmd = SqlHelper.CreateCommandWithParameters("USP_DISTRIBUCION_TARIFA_INTEGRAL", conexion, parametrosIn, true))
                                    {
                                        cmd.ExecuteNonQuery();
                                        SqlHelper.CloseConnection(conexion);
                                    }
                                    ListaRecurso.Clear();
                                    ListaIntegral.Clear();
                                    ListaFechaManiobra.Clear();
                                }

                                #endregion

                                #region Modificar Recargos con IGV y Descuento DCP-23/07/2020

                                if ((documento.MontoIGV.Value > 0) && (TieneRecargo))
                                {
                                    SqlConnection conexion = new SqlConnection(CadenaConexion.CadenaBDIntermedia);

                                    Dictionary<string, object> parametrosIn2 = new Dictionary<string, object>();
                                    parametrosIn2.Add("@IdCodigoDocumento", documento.IdCodigoDocumento);

                                    using (SqlCommand cmd = SqlHelper.CreateCommandWithParameters("USP_ACTUALIZA_TIPO_PRECIO", conexion, parametrosIn2, true))
                                    {
                                        cmd.ExecuteNonQuery();
                                        SqlHelper.CloseConnection(conexion);
                                    }
                                }

                                #endregion
                                
                                #region Informar de procesamiento de transacción a Helm
                                if (setPosted)
                                {
                                    try
                                    {
                                        SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                        log.Info($"- POSTEO EXITOSO A HELM. " + ArmarLog(documento, iTransactions));
                                    }
                                    catch (Exception ex)
                                    {
                                        //Informamos con LOG de error
                                        string mensajeError = "- ERROR al postear a HELM.";
                                        //log.Error(mensajeError);
                                        throw new Exception(mensajeError, ex);
                                    }
                                }
                                #endregion
                            }

                            #endregion
                        }
                        catch (Exception ex)
                        {
                            string logDocumento = ArmarLog(documento, iTransactions);
                            log.Error($"- Error al procesar documento: " + logDocumento, ex);

                            #region Envio de Correo
                            Correo correo = new Correo();
                            LogError errores = new LogError()
                            {
                                IdTransaccion = "ERROR GENERAL",
                                Error = "ERROR GENERAL",
                                ErrorDetalle = new List<string>()
                                {
                                    GetExceptionMessageFull(ex)
                                }
                            };

                            construirCorreoErrorGeneral(correo, logDocumento, errores);
                            EnviarCorreoElectronico(correo, true);
                            #endregion
                        }
                    }
                    page++;
                    jobjTransaction = JObject.Parse(GetSyncApi($"api/v1/Jobs/Transactions/Details?page={page}&posted=false", client));

                    //break; // Rompe el bucle para usar un json localmente para pruebas
                }
                //if (lstTransactionId.Count > 0)
                //{
                //    if (setPosted)
                //    {
                //        SetPostedTransaction(client, lstTransactionId);
                //    }
                //}

                log.Info("- Se finalizó el proceso de migración de documentos a la BDI y OFISIS.");
            }
            catch (Exception ex)
            {
                string logDocumento = ArmarLog(documento);
                log.Error($"- Se finalizó el proceso de migración de documentos a la BDI y OFISIS con errores. " + logDocumento, ex);

                #region Envio de Correo
                Correo correo = new Correo();
                LogError errores = new LogError()
                    {
                        IdTransaccion = "ERROR GENERAL",
                        Error = "ERROR GENERAL",
                        ErrorDetalle = new List<string>()
                        {
                            GetExceptionMessageFull(ex)
                        }
                    };

                construirCorreoErrorGeneral(correo, logDocumento, errores);
                EnviarCorreoElectronico(correo, true);
                #endregion

                _error.AppendLine($"[InterfaceFaturamento] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
            }
            finally
            {
                client.Dispose();
            }

            Error = _error.ToString();

            log.Info("- Se finalizó el servicio." + Environment.NewLine + Environment.NewLine);
        }

        private string ArmarLog(Documento documento, dynamic iTransactions = null)
        {
            string msDocumento = documento != null && !string.IsNullOrEmpty(documento.Serie) ? 
                documento.Serie : string.Empty;
            string msCliente = documento != null && !string.IsNullOrEmpty(documento.NombreCliente) ? 
                documento.NumeroDocumentoCliente + " " + documento.NombreCliente : (iTransactions != null ? 
                    iTransactions.AccountName.ToString().Trim() : string.Empty);
            string msNroTransaccionHelm = documento != null && !string.IsNullOrEmpty(documento.TransaccionHelm) ? 
                documento.TransaccionHelm : (iTransactions != null ? 
                    iTransactions.TransactionNumber.ToString() : string.Empty);
            string msOrden = documento != null && !string.IsNullOrEmpty(documento.OrdenCompra) ? 
                documento.OrdenCompra : (iTransactions != null && iTransactions.Order != null ? 
                iTransactions.Order.OrderNumber.ToString() : string.Empty);
            string msIdHelm = documento != null && !string.IsNullOrEmpty(documento.IdHelm) ?
                documento.IdHelm : (iTransactions != null ? 
                    iTransactions.Id.ToString() : string.Empty);

            string mensajeLog = string.Empty;

            if (!string.IsNullOrEmpty(msDocumento))
                mensajeLog += $"DOCUMENTO: {msDocumento}. ";

            if(!string.IsNullOrEmpty(msCliente))
                mensajeLog += $"CLIENTE: {msCliente}. ";

            if (!string.IsNullOrEmpty(msNroTransaccionHelm))
                mensajeLog += $"NRO. TRANSACCIÓN HELM: {msNroTransaccionHelm}. ";

            if (!string.IsNullOrEmpty(msOrden))
                mensajeLog += $"ORDEN: {msOrden}. ";

            if (!string.IsNullOrEmpty(msIdHelm))
                mensajeLog += $"ID HELM: {msIdHelm}.";

            return mensajeLog;
        }

        private string ValidacionPreviaGuardar(int codigoDocumento, Documento documento, List<DocumentoDetalle> listDetalle)
        {
            string errMsg = string.Empty;

            //1. Validamos codigoDocumento
            if (codigoDocumento == 0)
            {
                errMsg += "- No se asignó un identificador correcto para el tipo de documento." + Environment.NewLine;
            }

            //2. Validación de monto total
            if (documento.MontoTotal == 0)
            {
                errMsg += "- El monto total indicado en el documento es igual a cero." + Environment.NewLine;
            }

            //3. Validación de RUC (Codigo = 2)
            if (documento.CodigoDocumentoCliente == 2 && !Int64.TryParse(documento.NumeroDocumentoCliente, out _))
            {
                errMsg += $"- El número de documento del cliente NO es un RUC NACIONAL: {documento.NumeroDocumentoCliente}." + Environment.NewLine;
            }

            //4. Validación del período del Módulo CLIENTES de OFISIS
            errMsg += ValidarPeriodoOFISIS((DateTime)documento.FechaEmision);

            //5. Validación si existen documentos con fechas posteriores a la enviada
            ClsDal context = new ClsDal();
            Documento documentoUltFecha = context.ValidarFechaEmision(documento.Serie, (int)documento.CodigoDocumento, (DateTime)documento.FechaEmision);
            if (documentoUltFecha != null)
            {
                errMsg += $"- Existe(n) documento(s) registrado(s) en la BDI con serie {documento.Serie} " +
                    $"y fecha de emisión POSTERIOR a la fecha enviada {(DateTime)documento.FechaEmision:dd/MM/yyyy}. " +
                    $"El ÚLTIMO DOCUMENTO es {documentoUltFecha.Serie} - {documentoUltFecha.Numero}, " +
                    $"con FECHA EMISIÓN {(DateTime)documentoUltFecha.FechaEmision:dd/MM/yyyy}." + Environment.NewLine;
            }

            //6. Validación de Centros de Beneficio
            string errMsgDet = string.Empty;
            foreach (var detalle in listDetalle)
            {
                if (!String.IsNullOrEmpty(detalle.CodigoArticulo))
                {
                    if (!EsCentroBeneficio(detalle.CodigoArticulo))
                    {
                        errMsgDet += $"- Artículo: {detalle.NombreArticulo}. " +
                            $"Unidad: {detalle.NombreUnidadMedida}. " +
                            $"Centro Beneficio: {detalle.CodigoArticulo}" +
                            Environment.NewLine;
                    }
                }
            }

            if (errMsgDet != string.Empty)
            {
                errMsg += "- El documento tiene Centros de Beneficio asignados incorrectamente. " +
                    "Los siguientes artículos tienen un centro de beneficio incorrecto:" +
                    Environment.NewLine + errMsgDet;
            }

            return errMsg;
        }

        public DateTime GetFechaCorrecta(string fecha, char parameter = '/') // dd/MM/yyyy
        {
            var values = fecha.Split(parameter);
            int dia = Convert.ToInt32(values[0]);
            int mes = Convert.ToInt32(values[1]);
            int anio = Convert.ToInt32(values[2]);

            return new DateTime(anio, mes, dia);
        }

        private void EvaluarReferenciaLanchas(Documento documento, List<DocumentoDetalle> listDetalle)
        {
            int countLanchas = 0;
            foreach (var detalle in listDetalle)
            {
                string nombreArticulo = detalle.NombreArticulo.Trim().ToUpper();
                if (nombreArticulo.Contains("LAUNCH") || nombreArticulo.Contains("LANCHA"))
                    countLanchas++;
            }

            //Evaluamos si TODOS los detalles referencian a lanchas
            if (countLanchas == listDetalle.Count)
            {
                documento.FechaAtraque = ((DateTime)documento.FechaAtraque).Date;
                documento.FechaDesatraque = ((DateTime)documento.FechaDesatraque).Date;
            }
        }

        private string ValidarPeriodoOFISIS(DateTime fechaEmision)
        {
            string errMsg = string.Empty;
            DateTime? fechaInicio = null;
            DateTime? fechaFin = null;

            //Procedemos a leer período activo
            SqlConnection conexionOfisis = new SqlConnection(ObtenerConexionOfisis());
            SqlCommand cmd = SqlHelper.CreateCommand("USP_LISTAR_PERIODOS_OFISIS", conexionOfisis, true);
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader["GRTPAR_CODPAR"].ToString() == "VT_PERDES")
                        fechaInicio = GetFechaCorrecta(reader["VALPAR"].ToString());
                    else if (reader["GRTPAR_CODPAR"].ToString() == "VT_PERHAS")
                        fechaFin = GetFechaCorrecta(reader["VALPAR"].ToString());

                    if (fechaInicio != null && fechaFin != null)
                        break;
                }
            }

            SqlHelper.CloseConnection(conexionOfisis);

            if (fechaInicio == null || fechaFin == null)
                errMsg = "- No se encontró el período del módulo CLIENTES de OFISIS." + Environment.NewLine;
            else if (!(fechaInicio <= fechaEmision && fechaEmision <= fechaFin))
                errMsg = $"- El documento enviado con fecha {fechaEmision:dd/MM/yyyy} se encuentra fuera del período: " +
                    $"{(DateTime)fechaInicio:dd/MM/yyyy} - {(DateTime)fechaFin:dd/MM/yyyy}, del módulo CLIENTES." + Environment.NewLine;

            return errMsg;
        }

        private string ObtenerConexionOfisis()
        {
            string serverName = string.Empty;
            string databaseName = string.Empty;
            string userId = string.Empty;
            string password = string.Empty;
            var ofisisTags = ConfigurationManager.ConnectionStrings["TRAMARSAEntities"].ConnectionString
                .Split(new string[] { "connection string=" }, StringSplitOptions.RemoveEmptyEntries)[1]
                .Split(';');
            foreach (var tramarsaTag in ofisisTags)
            {
                var tagValue = tramarsaTag.Replace("\"","").Split('=');
                string value = tagValue[0].ToUpper().Trim();
                switch (value)
                {
                    case "DATA SOURCE":
                    case "SERVER":
                        serverName = tagValue[1];
                        break;
                    case "INITIAL CATALOG":
                    case "DATABASE":
                        databaseName = tagValue[1];
                        break;
                    case "USER ID":
                    case "UID":
                        userId = tagValue[1];
                        break;
                    case "PASSWORD":
                    case "PWD":
                        password = tagValue[1];
                        break;
                }
            }

            return $"Data Source={serverName};Initial Catalog={databaseName};user id={userId};password={password};MultipleActiveResultSets=true";
        }

        private bool ValidacionInformacionFactura(dynamic iTransactions, List<StructCompanies> lstCompanies, List<StructAssets> lstAssets)
        {
            try
            {
                LogError errores = new LogError();
                errores.Error = string.Empty;
                errores.IdTransaccion = string.Empty;
                errores.ErrorDetalle = new List<string>();
                //string.Format("El idRegistro:{0} del codigo de viaje {1} no se encontró.", "", "");
                #region Validacion Cliente
                if (string.IsNullOrEmpty(iTransactions.AccountName.ToString()))
                {
                    errores.ErrorDetalle.Add(string.Format("El cliente: no posee un nombre registrado."));
                }
                else
                {
                    if (!string.IsNullOrEmpty(iTransactions.AccountNumber.ToString()))
                    {
                        StructCompanies companies = lstCompanies.Find(x => x.accountNumber == iTransactions.AccountNumber.ToString());
                        if (!string.IsNullOrEmpty(companies.codDoc01))
                        {

                            if (!string.IsNullOrEmpty(companies.ccidf))
                            {
                                if (companies.codDoc01.Contains("01") && companies.ccidf.Trim().Length != 8)
                                {
                                    errores.ErrorDetalle.Add(string.Format("El numero de documento del cliente {0} de tipo DNI: no posee 8 digitos.", iTransactions.AccountName.ToString().Trim()));
                                }
                                else if ((companies.codDoc01.Contains("114") || companies.codDoc01.Contains("02")) && companies.ccidf.Trim().Length != 11)
                                {

                                    errores.ErrorDetalle.Add(string.Format("El numero de documento del cliente {0} de tipo {1}: no posee 11 digitos.", iTransactions.AccountName.ToString(), (companies.codDoc01.Contains("114")) ? "NIF" : "RUC"));
                                }
                            }
                            else
                            {
                                errores.ErrorDetalle.Add(string.Format("El cliente {0} no registra un numero de documento", iTransactions.AccountName.ToString().Trim()));
                            }
                        }
                        else
                        {
                            errores.ErrorDetalle.Add(string.Format("El cliente {0}: no posee un tipo de documento.", iTransactions.AccountName.ToString().Trim()));
                        }
                        if (string.IsNullOrEmpty(companies.ruc01))
                        {
                            errores.ErrorDetalle.Add(string.Format("El cliente {0}: el campo de validacion de si es ruc esta vacio o nulo.", iTransactions.AccountName.ToString().Trim()));
                        }

                    }
                    else
                    {
                        errores.ErrorDetalle.Add(string.Format("El cliente {0}: no tiene numero de documento.", iTransactions.AccountName.ToString()));
                    }
                }
                #endregion

                if (string.IsNullOrEmpty(iTransactions.CurrencyType.ExternalSystemCode.ToString()))
                {
                    errores.ErrorDetalle.Add(string.Format("El documento tiene el codigo de moneda vacio."));
                }
                if (string.IsNullOrEmpty(iTransactions.CurrencyType.ShortName.ToString()))
                {
                    errores.ErrorDetalle.Add(string.Format("El documento tiene el nombre de moneda vacio."));
                }

                dynamic dynTransactionsLines = iTransactions.TransactionLines.Children();
                foreach (var iTransactionsLines in dynTransactionsLines)
                {
                    dynamic dynTransactionsLinesRevenueAllocations = iTransactionsLines.RevenueAllocations.Children();
                    foreach (var item in dynTransactionsLinesRevenueAllocations)
                    {
                        foreach (var iAccounting in item.AccountingCodes)
                        {
                            if (string.IsNullOrEmpty(iAccounting.AccountingCode.ToString()))
                            {

                                errores.ErrorDetalle.Add(string.Format("El producto {0}: no tiene numero de documento.", iAccounting.EntityName.ToString()));
                            }
                        }
                    }

                }

                if (errores.ErrorDetalle.Count > 0)
                {
                    errores.IdTransaccion = iTransactions.Id.ToString();
                    errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());

                    Correo correo = new Correo();

                    try
                    {
                        construirCorreoError(correo, iTransactions, errores);
                        EnviarCorreoElectronico(correo, true);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        log.Error("- Error al enviar correo con información de errores.", ex);
                        new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                string mensajeError = $"- Error al realizar la validación de información del documento. " +
                    $"CLIENTE: {iTransactions.AccountName.ToString().Trim()}." +
                    $"Nro. Transacción HELM: {iTransactions.TransactionNumber.ToString()}. Orden: {iTransactions.Order.OrderNumber.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
                //return false;
            }
        }

        private bool EsCentroBeneficio(string centroCosto)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(centroCosto) && centroCosto.Length >= 3)
            {
                result = 
                    char.ToUpper(centroCosto[2]).Equals('I') //Caso TFI-NTI-DVI (la tercera la debe ser "I")
                    || (centroCosto.ToUpper().Equals("TFA2499Z") //Casos de otros ingresos, con doble naturaleza
                        || centroCosto.ToUpper().Equals("TFA2699Z"));
            }

            return result;
        }

        //private bool EsCentroCosto(string centroCosto)
        //{
        //    bool result = false;
        //    var caracteres = "ABCDEFGHIJKLMN0PQRSTUV".ToList(); //WXYZ

        //    if (!string.IsNullOrEmpty(centroCosto) && centroCosto.Length >= 3)
        //    {
        //        result = centroCosto.StartsWith("TF") && caracteres.Contains(Char.ToUpper(centroCosto[2]));
        //    }

        //    return result;
        //}

        private void construirCorreoError(Correo correo, dynamic iTransactions, LogError errores)
        {
            try
            {
                var correoEnvio = ConfigurationManager.AppSettings["CorreoErrorEnvio"].ToString().Split('|');
                var correosPara = (ConfigurationManager.AppSettings["CorreoErrorPara"].ToString().IndexOf('|') > -1) ? ConfigurationManager.AppSettings["CorreoErrorPara"].ToString().Split('|') : new string[] { ConfigurationManager.AppSettings["CorreoErrorPara"].ToString() };
                var correosCC = (ConfigurationManager.AppSettings["CorreoErrorCC"].ToString().IndexOf('|') > -1) ? ConfigurationManager.AppSettings["CorreoErrorCC"].ToString().Split('|') : new string[] { ConfigurationManager.AppSettings["CorreoErrorCC"].ToString() };
                var correosCO = (ConfigurationManager.AppSettings["CorreoErrorCO"].ToString().IndexOf('|') > -1) ? ConfigurationManager.AppSettings["CorreoErrorCO"].ToString().Split('|') : new string[] { ConfigurationManager.AppSettings["CorreoErrorCO"].ToString() };
                List<StructMail> correosErrorPara = new List<StructMail>();
                List<StructMail> correosErrorCC = new List<StructMail>();
                List<StructMail> correosErrorCO = new List<StructMail>();

                correo.correoEmisor.mail = correoEnvio[0];
                correo.correoEmisor.password = correoEnvio[1];
                correo.correoEmisor.nameMail = correoEnvio[2];
                correosCC = (string.IsNullOrEmpty(correosCC[0])) ? null : correosCC;
                correosCO = (string.IsNullOrEmpty(correosCO[0])) ? null : correosCO;
                foreach (var correoPara in correosPara)
                {
                    if (correoPara.IndexOf(',') > -1)
                    {
                        var correoDeserealizado = correoPara.Split(',');
                        correo.correosPara.Add(new StructMail() { mail = correoDeserealizado[0], nameMail = correoDeserealizado[1], password = string.Empty });
                    }
                    else
                    {
                        correo.correosPara.Add(new StructMail() { mail = correoPara, nameMail = string.Empty, password = string.Empty });
                    }


                }
                if (correosCC != null)
                {
                    foreach (var correoCC in correosCC)
                    {
                        if (correoCC.IndexOf(',') > -1)
                        {
                            var correoDeserealizado = correoCC.Split(',');
                            correo.correosCC.Add(new StructMail() { mail = correoDeserealizado[0], nameMail = correoDeserealizado[1], password = string.Empty });
                        }
                        else
                        {
                            correo.correosCC.Add(new StructMail() { mail = correoCC, nameMail = string.Empty, password = string.Empty });
                        }

                    }
                }
                if (correosCO != null)
                {

                    foreach (var correoCO in correosCO)
                    {
                        if (correoCO.IndexOf(',') > -1)
                        {
                            var correoDeserealizado = correoCO.Split(',');
                            correo.correosCCO.Add(new StructMail() { mail = correoDeserealizado[0], nameMail = correoDeserealizado[1], password = string.Empty });
                        }
                        else
                        {
                            correo.correosCCO.Add(new StructMail() { mail = correoCO, nameMail = string.Empty, password = string.Empty });
                        }

                    }
                }

                correo.asunto = string.Format("Interface Ventas - Transacción Helm {0} tiene inconsistencia(s)", iTransactions.TransactionNumber.ToString());
                var cabecera = "<table border='0' align='left' cellpadding='0' cellspacing='0' style='width: 100%'>" +
                "<tr>" +
                    "<td align='left' valign='top'> Estimados,</td>" +
                "</tr>" +
                "<tr>" +
                    "<td align='left' valign='top'> La Transacción " + iTransactions.TransactionNumber.ToString() + " cuenta con las siguientes inconsistencia(s)</td>" +
                "</tr>" +
                "<tr>" +
                    "<td align = 'center' valign='top' style='font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#000000;padding:10px'>" +
                        "<table width='100%' border='0' cellspacing='0' cellpadding='0' style='margin-top:10px;padding-left:5px;'>";
                var detalle = string.Empty;
                foreach (var error in errores.ErrorDetalle)
                {
                    detalle += "<tr><td align='left' valign='top' style='font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#525252;'>" +
                                    "<p> <b> - " + error + "  </b></p> " +
                                "</td>" +
                                "</tr>";
                }

                var pie = "</table>" +
                    "</td>" +
                "</tr>" +
                "<tr>" +
                    "<td align='left' valign='top'> Por tal motivo no se trasladó a Efactura, favor corregir las inconsistencia(s), luego descontabilizar la transacción si tiene fecha de hoy, o revocar la transacción si tiene fecha mayor a hoy.</td>" +
                "</tr>" +
                "<tr style='padding:0px;' align='center'>" +
                    "<td align='left' valign='top'>" +
                        "<br> <span> Saluda atentamente, </span> <br>" +
                        "<b><span> " + "Equipo de Sistemas </span> <br>" +
                        "<b><span> " + "PSA Marine Perú </span> <br>" + @" </b></td></tr><br></table>";

                correo.cuerpo = cabecera + detalle + pie;
            }
            catch (Exception ex)
            {
                log.Error("- Error al construir correo con información de errores.", ex);
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
            }
        }

        private void construirCorreoErrorGeneral(Correo correo, string logDocumento, LogError errores)
        {
            try
            {
                var correoEnvio = ConfigurationManager.AppSettings["CorreoErrorEnvio"].ToString().Split('|');
                var correosPara = (ConfigurationManager.AppSettings["CorreoErrorPara"].ToString().IndexOf('|') > -1) ? ConfigurationManager.AppSettings["CorreoErrorPara"].ToString().Split('|') : new string[] { ConfigurationManager.AppSettings["CorreoErrorPara"].ToString() };
                var correosCC = (ConfigurationManager.AppSettings["CorreoErrorCC"].ToString().IndexOf('|') > -1) ? ConfigurationManager.AppSettings["CorreoErrorCC"].ToString().Split('|') : new string[] { ConfigurationManager.AppSettings["CorreoErrorCC"].ToString() };
                var correosCO = (ConfigurationManager.AppSettings["CorreoErrorCO"].ToString().IndexOf('|') > -1) ? ConfigurationManager.AppSettings["CorreoErrorCO"].ToString().Split('|') : new string[] { ConfigurationManager.AppSettings["CorreoErrorCO"].ToString() };
                List<StructMail> correosErrorPara = new List<StructMail>();
                List<StructMail> correosErrorCC = new List<StructMail>();
                List<StructMail> correosErrorCO = new List<StructMail>();

                correo.correoEmisor.mail = correoEnvio[0];
                correo.correoEmisor.password = correoEnvio[1];
                correo.correoEmisor.nameMail = correoEnvio[2];
                correosCC = (string.IsNullOrEmpty(correosCC[0])) ? null : correosCC;
                correosCO = (string.IsNullOrEmpty(correosCO[0])) ? null : correosCO;
                foreach (var correoPara in correosPara)
                {
                    if (correoPara.IndexOf(',') > -1)
                    {
                        var correoDeserealizado = correoPara.Split(',');
                        correo.correosPara.Add(new StructMail() { mail = correoDeserealizado[0], nameMail = correoDeserealizado[1], password = string.Empty });
                    }
                    else
                    {
                        correo.correosPara.Add(new StructMail() { mail = correoPara, nameMail = string.Empty, password = string.Empty });
                    }


                }
                if (correosCC != null)
                {
                    foreach (var correoCC in correosCC)
                    {
                        if (correoCC.IndexOf(',') > -1)
                        {
                            var correoDeserealizado = correoCC.Split(',');
                            correo.correosCC.Add(new StructMail() { mail = correoDeserealizado[0], nameMail = correoDeserealizado[1], password = string.Empty });
                        }
                        else
                        {
                            correo.correosCC.Add(new StructMail() { mail = correoCC, nameMail = string.Empty, password = string.Empty });
                        }

                    }
                }
                if (correosCO != null)
                {

                    foreach (var correoCO in correosCO)
                    {
                        if (correoCO.IndexOf(',') > -1)
                        {
                            var correoDeserealizado = correoCO.Split(',');
                            correo.correosCCO.Add(new StructMail() { mail = correoDeserealizado[0], nameMail = correoDeserealizado[1], password = string.Empty });
                        }
                        else
                        {
                            correo.correosCCO.Add(new StructMail() { mail = correoCO, nameMail = string.Empty, password = string.Empty });
                        }

                    }
                }

                string datosDocumento = string.Empty;
                if (!string.IsNullOrEmpty(logDocumento))
                {
                    datosDocumento =
                    "<tr>" +
                        "<td align='left' valign='top'><b>"
                            + logDocumento
                            + "</b></td>" +
                    "</tr>";
                }

                correo.asunto = string.Format("Interface Ventas - Se presentaron error(es) al migrar a la BDI y OFISIS");
                var cabecera = "<table border='0' align='left' cellpadding='0' cellspacing='0' style='width: 100%'>" +
                "<tr>" +
                    "<td align='left' valign='top'> Estimados,</td>" +
                "</tr>" +
                "<tr>" +
                    "<td align='left' valign='top'> Se presentaron los siguientes errores al realizar la migración del documento:</td>" +
                "</tr>" +

                datosDocumento +

                "<tr>" +
                    "<td align = 'center' valign='top' style='font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#000000;padding:10px'>" +
                        "<table width='100%' border='0' cellspacing='0' cellpadding='0' style='margin-top:10px;padding-left:5px;'>";
                var detalle = string.Empty;
                foreach (var error in errores.ErrorDetalle)
                {
                    detalle += "<tr><td align='left' valign='top' style='font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#525252;'>" +
                                    "<p> <b>" + error + "  </b></p> " +
                                "</td>" +
                                "</tr>";
                }

                var pie = "</table>" +
                    "</td>" +
                "</tr>" +
                "<tr>" +
                    "<td align='left' valign='top'> Por tal motivo no se trasladó la información del documento, favor corregir errores, para realizar la migración del documento nuevamente.</td>" +
                "</tr>" +
                "<tr style='padding:0px;' align='center'>" +
                    "<td align='left' valign='top'>" +
                        "<br> <span> Saluda atentamente, </span> <br>" +
                        "<b><span> " + "Equipo de Sistemas </span> <br>" +
                        "<b><span> " + "PSA Marine Perú </span> <br>" + @" </b></td></tr><br></table>";

                correo.cuerpo = cabecera + detalle + pie;
            }
            catch (Exception ex)
            {
                log.Error("- Error al construir correo con información de errores.", ex);
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
            }
        }

        private bool EnviarCorreoElectronico(Correo correo, bool esHTML,
            bool AcuseRecibo = true)
        {
            try
            {
                var mailMsg = new MailMessage();
                mailMsg.From = new MailAddress(correo.correoEmisor.mail, correo.correoEmisor.nameMail);
                foreach (StructMail correopara in correo.correosPara)
                    mailMsg.To.Add(new MailAddress(correopara.mail, correopara.nameMail));
                foreach (StructMail correocc in correo.correosCC)
                    mailMsg.CC.Add(new MailAddress(correocc.mail, correocc.nameMail));
                foreach (StructMail correocco in correo.correosCCO)
                    mailMsg.Bcc.Add(new MailAddress(correocco.mail, correocco.nameMail));
                mailMsg.Subject = correo.asunto;
                mailMsg.Body = correo.cuerpo;
                if (esHTML)
                {
                    AlternateView htmlView;
                    htmlView = AlternateView.CreateAlternateViewFromString(correo.cuerpo, Encoding.UTF8, "text/html");

                    mailMsg.AlternateViews.Add(htmlView);
                }

                var SmtpClient = new SmtpClient();
                try
                {
                    SmtpClient = new SmtpClient("smtp.office365.com", 587);
                    SmtpClient.Credentials = new NetworkCredential(correo.correoEmisor.mail, correo.correoEmisor.password);
                    SmtpClient.EnableSsl = true; //correoEmisor.SMTPSSL;
                }
                catch (Exception ex)
                {
                    log.Error("- Error al configurar la cuenta de correo electrónico.", ex);
                    new ErrorHandler(new LoggerTXT()).Handle(new Exception("No se pudo configurar la cuenta de correo electrónico. (Puerto)"));//Guardamos logErrorTXT
                }

                if (AcuseRecibo)
                {
                    //SOLICITAR ACUSE DE RECIBO Y LECTURA
                    mailMsg.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure | DeliveryNotificationOptions.OnSuccess | DeliveryNotificationOptions.Delay;
                    mailMsg.Headers.Add("Disposition-Notification-To", correo.correoEmisor.mail); //solicitar acuse de recibo al abrir mensaje
                    try
                    {
                        SmtpClient.Send(mailMsg);
                    }
                    catch (Exception ex)
                    {
                        log.Error("- Error al enviar correo electrónico con parámetros configurados, con acuse de recibo. DeliveryNotificationOptions: OnFailure.", ex);

                        //reenviando en caso de error
                        mailMsg.DeliveryNotificationOptions = DeliveryNotificationOptions.None;
                        mailMsg.Headers.Remove("Disposition-Notification-To");
                        try
                        {
                            SmtpClient.Send(mailMsg);
                        }
                        catch (Exception exc)
                        {
                            log.Error("- Error al enviar correo electrónico con parámetros configurados, con acuse de recibo. DeliveryNotificationOptions: None.", exc);
                            new ErrorHandler(new LoggerTXT()).Handle(exc);//Guardamos logErrorTXT
                        }
                    }
                }
                else
                {
                    try
                    {
                        SmtpClient.Send(mailMsg);
                    }
                    catch (Exception ex)
                    {
                        log.Error("- Error al enviar correo electrónico con parámetros configurados, sin acuse de recibo.", ex);
                        new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                log.Error("- Error general al enviar correo electrónico.", ex);
                return false;
            }
        }

        private List<StrucImposto> ValidListaImposto(List<StrucImposto> lstImposto, List<StructDetalle> lstDetalle)
        {
            StructDetalle detalle = new StructDetalle();
            List<StrucImposto> lretImposto = new List<StrucImposto>();
            StrucImposto auxImposto = new StrucImposto();

            try
            {
                foreach (var i in lstImposto)
                {
                    detalle = lstDetalle.Find(x => (x.nombreArticulo.ToUpper().Contains(i.descricao.Replace("IMPUESTO", "").Trim())) &&
                                                   (x.remolcador == i.remolcador) &&
                                                   (x.sequencial == i.sequencial));
                    if (!string.IsNullOrEmpty(detalle.id))
                    {
                        lretImposto.Add(i);
                    }
                    else
                    {
                        auxImposto = lretImposto.Find(z => (z.descricao == i.descricao) &&
                                                           (z.remolcador == i.remolcador) &&
                                                           (z.sequencial == 1));
                        lretImposto.Remove(auxImposto);
                        auxImposto.valor += i.valor;
                        lretImposto.Add(auxImposto);
                    }
                }
            }
            catch (Exception ex)
            {   
                _error.AppendLine($"[ValidaListaImposto] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = "- Error general al validar la lista de impuesto [ValidListaImposto].";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }

            return lretImposto;
        }

        private string GetCentroBeneficio(string servico, int sequencial, string orderNumber, List<StructAssets> lstAssets, JObject jobjOrder)
        {
            string retorno = "";
            string jobNumber = "";
            StructAssets assets = new StructAssets();
            try
            {
                dynamic dynOrderTrips = jobjOrder["Data"]["Page"][0]["Trips"].Children();
                foreach (var item in dynOrderTrips)
                {
                    if (servico.Contains(item.Triptype.Name.ToString()))
                    {
                        dynamic dynJobsTrips = item.Jobs.Children();
                        foreach (var itemJobs in dynJobsTrips)
                        {
                            jobNumber = itemJobs.JobNumber.ToString();
                            if (Convert.ToInt32(jobNumber.ToString().Substring(jobNumber.Length - 1)) == sequencial)
                            {
                                assets = lstAssets.Find(a => a.name == itemJobs.Resource.Name.ToString());
                                if (!string.IsNullOrEmpty(assets.id))
                                {
                                    retorno = assets.accountingCode;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {   
                _error.AppendLine($"[GetCentroBeneficio] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = "- Error al leer datos [GetCentroBeneficio].";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }

            return retorno;
        }
        private void SetPostedTransaction(HttpClient http, List<string> lstParameter)
        {
            string metodo = "api/v1/jobs/Transactions/SetPosted";
            int page = 1;
            int totalPages = (int)Decimal.Ceiling(lstParameter.Count / 100);
            int posicaoLista = 0;
            int registrosLidos = 1;
            try
            {
                do
                {
                    JTokenWriter writerJson = new JTokenWriter();

                    writerJson.WriteStartObject();
                    writerJson.WritePropertyName("Assignments");
                    writerJson.WriteStartArray();

                    for (int index = posicaoLista; index < lstParameter.Count; index++)
                    {
                        writerJson.WriteStartObject();
                        writerJson.WritePropertyName("TransactionId");
                        writerJson.WriteValue(lstParameter[index]);
                        writerJson.WritePropertyName("PostedDate");
                        writerJson.WriteValue(DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
                        writerJson.WriteEndObject();
                        registrosLidos++;
                        if (registrosLidos > 100)
                        {
                            posicaoLista += (registrosLidos - 1);
                            break;
                        }

                    }
                    writerJson.WriteEndArray();
                    writerJson.WriteEndObject();

                    JObject json = (JObject)writerJson.Token;
                    PostSyncApi(metodo, http, json);
                    page++;

                } while (page <= totalPages);
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[SetPostedTransaction] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error crítico al conectarse con método [SetPostedTransaction]. Metodo: {metodo}. HTTP: {http.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }
        }

        private void SetPostedTransactionxId(HttpClient http, string TransactionId)
        {
            string metodo = "api/v1/jobs/Transactions/SetPosted";
            //int page = 1;
            //int totalPages = (int)Decimal.Ceiling(lstParameter.Count / 100);
            //int posicaoLista = 0;
            //int registrosLidos = 1;
            try
            {
                //do
                //{
                JTokenWriter writerJson = new JTokenWriter();

                writerJson.WriteStartObject();
                writerJson.WritePropertyName("Assignments");
                writerJson.WriteStartArray();

                //for (int index = posicaoLista; index < lstParameter.Count; index++)
                //{
                writerJson.WriteStartObject();
                writerJson.WritePropertyName("TransactionId");
                writerJson.WriteValue(TransactionId);
                writerJson.WritePropertyName("PostedDate");
                writerJson.WriteValue(DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:sszzz"));
                writerJson.WriteEndObject();
                //    registrosLidos++;
                //    if (registrosLidos > 100)
                //    {
                //        posicaoLista += (registrosLidos - 1);
                //        break;
                //    }

                //}
                writerJson.WriteEndArray();
                writerJson.WriteEndObject();

                JObject json = (JObject)writerJson.Token;
                PostSyncApi(metodo, http, json);
                //page++;

                //} while (page <= totalPages);
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[SetPostedTransaction] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error crítico al conectarse con método [SetPostedTransactionxId]. Metodo: {metodo}. HTTP: {http.ToString()}. TransactionId: {TransactionId}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }
        }

        private void SetCompanies(List<StructCompanies> lstParameter, HttpClient httpParameter)
        {
            string codigoDocumento = "";
            string ccidf = "";
            string ruc01 = "";
            bool id = false;
            bool isMyCompany = false;
            int page = 1;

            try
            {
                JObject jobjCompany = JObject.Parse(GetSyncApi($"api/v1/jobs/companies/FindCompanies?page={page}", httpParameter));
                while (jobjCompany["Data"]["Page"].HasValues)
                {
                    dynamic dynCompanies = (JArray)jobjCompany["Data"]["Page"];
                    foreach (var iCompanies in dynCompanies)
                    {
                        id = false;
                        try
                        {
                            ccidf = iCompanies.UserDefined.CCIDF.ToString();
                        }
                        catch
                        {
                            ccidf = null;
                        }
                        try
                        {
                            ruc01 = iCompanies.UserDefined.RUC01.ToString();
                        }
                        catch
                        {
                            ruc01 = null;
                        }
                        try
                        {
                            codigoDocumento = iCompanies.UserDefined.CODDOC01.ToString();
                            codigoDocumento = codigoDocumento.Substring(0, 3).Trim();
                        }
                        catch
                        {
                            codigoDocumento = null;
                        }
                        isMyCompany = Convert.ToBoolean(iCompanies.IsMyCompany.ToString());

                        dynamic dynCompaniesAccounts = iCompanies.Accounts.Children();
                        foreach (var iAccount in dynCompaniesAccounts)
                        {
                            StructCompanies companies = new StructCompanies();

                            id = true;
                            companies.accountNumber = iAccount.AccountNumber.ToString();
                            companies.name = iAccount.Name.ToString();
                            companies.id = iAccount.Id.ToString();
                            companies.ruc01 = ruc01;
                            companies.ccidf = ccidf;
                            companies.codDoc01 = codigoDocumento;
                            companies.isMyCompany = isMyCompany;

                            dynamic dynCompaniesAddresses = iAccount.Addresses.Children();
                            foreach (var iAddress in dynCompaniesAddresses)
                            {
                                if (!string.IsNullOrEmpty(iAddress.Address.ToString()))
                                {
                                    companies.address = iAddress.Address.ToString();
                                }
                                else
                                {
                                    companies.address = ".";
                                }
                                companies.email = iAddress.Email.ToString();
                            }

                            lstParameter.Add(companies);
                        }
                        if (!id)
                        {
                            StructCompanies companies = new StructCompanies
                            {
                                ruc01 = ruc01,
                                ccidf = ccidf,
                                codDoc01 = codigoDocumento,
                                name = iCompanies.Name.ToString(),
                                id = iCompanies.Id.ToString()
                            };
                            lstParameter.Add(companies);
                        }
                    }
                    page++;
                    jobjCompany = JObject.Parse(GetSyncApi($"api/v1/jobs/companies/FindCompanies?page={page}", httpParameter));
                }
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[SetListAssets] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error al leer datos [SetCompanies]. HTTP: {httpParameter.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }
        }

        private void SetListAssets(List<StructAssets> lstAssets, HttpClient httpParameter)
        {
            int page = 1;

            try
            {
                JObject jobjAssets = JObject.Parse(GetSyncApi($"api/v1/core/assets/FindAssets?page={page}", httpParameter));
                while (jobjAssets["Data"]["Page"].HasValues)
                {
                    dynamic dynAssets = (JArray)jobjAssets["Data"]["Page"];
                    foreach (var item in dynAssets)
                    {
                        StructAssets assets = new StructAssets
                        {
                            id = item.Id.ToString(),
                            name = item.Name.ToString(),
                            shortName = item.ShortName.ToString(),
                            accountingCode = item.AccountingCode.ToString(),
                            vesselTypeNames = item.VesselTypeNames.ToString()
                        };
                        lstAssets.Add(assets);
                    }
                    page++;
                    jobjAssets = JObject.Parse(GetSyncApi($"api/v1/core/assets/FindAssets?page={page}", httpParameter));
                }
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[SetListAssets] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error al leer datos [SetListAssets]. HTTP: {httpParameter.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }
        }
        private void SetOrderUserDefined(JObject jobjParameter, Hashtable htParameter)
        {
            try
            {
                dynamic dynOrder = (JArray)jobjParameter["Data"]["Page"];
                foreach (var iOrder in dynOrder)
                {
                    try
                    {
                        htParameter.Add("BEN01", iOrder.UserDefined.BEN01.ToString());
                    }
                    catch
                    {
                        htParameter.Add("BEN01", null);
                    }

                    try
                    {
                        htParameter.Add("TRB01", iOrder.UserDefined.TRB01.ToString());
                    }
                    catch
                    {
                        htParameter.Add("TRB01", null);
                    }

                    try
                    {
                        htParameter.Add("ESL01", iOrder.UserDefined.ESL01.ToString());
                    }
                    catch
                    {
                        htParameter.Add("ESL01", null);
                    }
                }
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[SetOrderUserDefined] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error al leer datos [SetOrderUserDefined]. HTTP: {htParameter.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }
        }

        private string GetSyncApi(string metodo, HttpClient http)
        {
            string retorno = "";

            try
            {
                var response = http.GetAsync(metodo).Result;
                if (response.IsSuccessStatusCode)
                {
                    retorno = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    retorno = "";
                }
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[GetSyncApi] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error crítico al conectarse con método [GetSyncApi]. "
                    + $"API: {metodo}. HTTP: {http.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }

            return retorno;
        }
        private void PostSyncApi(string metodo, HttpClient http, JObject json)
        {
            try
            {
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                var response = http.PostAsync(metodo, content).Result;
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(response.Content.ReadAsStringAsync().Result);
                }
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[PostSyncApi] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error crítico al conectarse con método [PostSyncApi]. "
                    + $"API: {metodo}. HTTP: {http.ToString()}. JSON: {json.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }
        }

        // --------------------------------------------------------
        // Author: André Azevedo
        // Company: Helm Operations
        // Date: Apr-08-2020
        // Definition: return ship information.
        private JObject GetDataShip(string imoNumber, HttpClient httpParameter)
        {
            StringBuilder erro = new StringBuilder();
            JObject json = new JObject();

            try
            {
                json = JObject.Parse(GetSyncApi($"api/v1/jobs/ships/FindShips?page=1&IMO={imoNumber}", httpParameter));
                
            }
            catch (Exception ex)
            {
                erro.AppendLine("Módulo ClsApiRest-LoadShip:");
                erro.AppendLine(ex.Message.ToString());
                if (ex.InnerException != null)
                {
                    erro.AppendLine("");
                    erro.AppendLine($"Erro detalhado: {ex.InnerException.ToString()}");
                }
                erro.AppendLine($"Erro stacktrace: {ex.StackTrace.ToString()}");
                new ErrorHandler(new LoggerTXT()).Handle(ex);//Guardamos logErrorTXT

                string mensajeError = $"- Error al leer datos [GetDataShip]. "
                    + $"imoNumber: {imoNumber}. HTTP: {httpParameter.ToString()}";
                //log.Error(mensajeError, ex);
                throw new Exception(mensajeError, ex);
            }
            return json;
        }

        private string GetExceptionMessageFull(Exception ex)
        {
            List<string> listError = new List<string>();
            listError.Add(ex.Message);

            if (ex.InnerException != null)
            {
                if(!listError.Exists(e => e == ex.InnerException.Message))
                    listError.Add(ex.InnerException.Message);

                if (ex.InnerException.InnerException != null)
                {
                    if (!listError.Exists(e => e == ex.InnerException.InnerException.Message))
                        listError.Add(ex.InnerException.InnerException.Message);

                    if (ex.InnerException.InnerException.InnerException != null)
                    {
                        if (!listError.Exists(e => e == ex.InnerException.InnerException.InnerException.Message))
                            listError.Add(ex.InnerException.InnerException.InnerException.Message);
                    }
                }
            }

            return String.Join(" ", listError);
            //+ ex.StackTrace;
        }

        // --------------------------------------------------------
    }

    public class StructMail
    {
        public string mail { get; set; }
        public string password { get; set; }
        public string nameMail { get; set; }
    }
    public class Correo
    {
        public StructMail correoEmisor { get; set; }
        public List<StructMail> correosPara { get; set; }
        public List<StructMail> correosCC { get; set; }
        public List<StructMail> correosCCO { get; set; }
        public string asunto { get; set; }
        public string cuerpo { get; set; }
        public Correo()
        {
            correoEmisor = new StructMail() { mail = string.Empty, nameMail = string.Empty, password = string.Empty };
            correosPara = new List<StructMail>();
            correosCC = new List<StructMail>();
            correosCCO = new List<StructMail>();
            asunto = string.Empty;
            cuerpo = string.Empty;
        }
    }
    public class Integral
    {
        public Integral(string IdHelmIntegral, string NombreServicioIntegral)
        {
            this.IdHelmIntegral = IdHelmIntegral;
            this.NombreServicioIntegral = NombreServicioIntegral;
        }
        public string IdHelmIntegral { get; set; }
        public string NombreServicioIntegral { get; set; }
    }

    public class ViajeFactura
    {
        public ViajeFactura(string IdHelm, string TipoViaje)
        {
            this.IdHelm = IdHelm;
            this.TipoViaje = TipoViaje;
        }
        public string IdHelm { get; set; }
        public string TipoViaje { get; set; }
    }

    public class RecursoFactura
    {
        public RecursoFactura(string IdHelm, string NombreServicio, string TipoViaje, string NombreRecurso)
        {
            this.IdHelm = IdHelm;
            this.NombreServicio = NombreServicio;
            this.TipoViaje = TipoViaje;
            this.NombreRecurso = NombreRecurso;
        }
        public string IdHelm { get; set; }
        public string NombreServicio { get; set; }
        public string TipoViaje { get; set; }
        public string NombreRecurso { get; set; }
    }

    public class RecursoOrden
    {
        public RecursoOrden(string IdHelm, string TipoViaje, string NombreRecurso, string TipoRecurso, DateTime? Fecha)
        {
            this.IdHelm = IdHelm;
            this.TipoViaje = TipoViaje;
            this.NombreRecurso = NombreRecurso;
            this.TipoRecurso = TipoRecurso;
            this.Fecha = Fecha;
        }
        public string IdHelm { get; set; }
        public string TipoViaje { get; set; }
        public string NombreRecurso { get; set; }
        public string TipoRecurso { get; set; }
        public DateTime? Fecha { get; set; }
    }
}