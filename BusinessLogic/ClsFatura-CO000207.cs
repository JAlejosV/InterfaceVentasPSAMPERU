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

namespace BusinessLogic
{
    public class ClsFatura
    {
        public string Error = "";

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

            try
            {
                CultureInfo usCulture = new CultureInfo("en-US");
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
                #endregion

                #region CargaInformacion
                ClsJSON json = new ClsJSON();

                List<string> lstTransactionId = new List<string>();
                List<StructCompanies> lstCompanies = new List<StructCompanies>();
                List<StructAssets> lstAssets = new List<StructAssets>();

                client.BaseAddress = new System.Uri(uriAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
                client.DefaultRequestHeaders.Add("API-Key", apiKey);

                SetCompanies(lstCompanies, client);
                SetListAssets(lstAssets, client);

                #endregion

                //JObject jobjTransaction = JObject.Parse(json.TesteJson_Erro());

                JObject jobjTransaction = JObject.Parse(GetSyncApi($"api/v1/Jobs/Transactions/Details?page={page}&posted=false", client));

                while (jobjTransaction["Data"]["Page"].HasValues)
                {
                    dynamic dynTransactions = (JArray)jobjTransaction["Data"]["Page"];
                    foreach (var iTransactions in dynTransactions)
                    {
                        
                        tipoTransacao = iTransactions.TransactionType.Name.ToString().ToUpper();
                        if (tipoTransacao != "COMISIONES")
                        {
                            var InformacionFacturaValida = ValidacionInformacionFactura(iTransactions, lstCompanies, lstAssets);
                            if (!InformacionFacturaValida)
                            {
                                if (setPosted)
                                {
                                    SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                }
                                continue;
                            }
                            List<StrucImposto> lstImposto = new List<StrucImposto>();
                            List<StructDetalle> lstDetalle = new List<StructDetalle>();
                            List<StrucDesconto> lstDesconto = new List<StrucDesconto>();
                            List<StructRebaje> lstRebaje = new List<StructRebaje>();
                            Hashtable htOrder = new Hashtable();

                            JObject jobjOrder = new JObject();

                            Documento documento = new Documento();
                            USR_FCRFAC usrfcrfac = new USR_FCRFAC();

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

                            //JAV 28122019
                            notaCredito = iTransactions.TransactionNumber.ToString().Contains("RV");

                            if (notaCredito)
                            {
                                if (setPosted)
                                {
                                    SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                }

                                continue;
                            }
                            //FIN JAV 28122019

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
                                SetOrderUserDefined(jobjOrder, htOrder);
                                if (!string.IsNullOrEmpty((string)htOrder["BEN01"]))
                                {
                                    StructCompanies company = lstCompanies.Find(x => x.id == (string)htOrder["BEN01"]);
                                    documento.NombreSolidario = company.name;
                                }
                                else
                                {
                                    documento.NombreSolidario = null;
                                }
                                documento.TRB = Math.Abs(Convert.ToDecimal(htOrder["TRB01"]));
                                documento.Eslora = Math.Abs(Convert.ToDecimal(htOrder["ESL01"]));
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

                                itemServico = iTransactionsLines.DetailDescription.ToString().ToUpper();
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
                                                        keyVessel += iAccounting.EntityName.ToString();
                                                        break;
                                                }
                                            }
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
                                                            keyVessel += iAccounting.EntityName.ToString();
                                                            break;
                                                    }
                                                }
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
                                                        keyVessel += iAccounting.EntityName.ToString();
                                                        codigoArticulo = iAccounting.AccountingCode.ToString();
                                                        break;
                                                }
                                            }
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
                                    stuDetalle.nombreArticulo = iTransactionsLines.DetailDescription.ToString();
                                    stuDetalle.cantidad = Math.Abs(Convert.ToDecimal(iTransactionsLines.Quantity.ToString()));
                                    stuDetalle.precioUnitario = Math.Abs(Convert.ToDecimal(iTransactionsLines.Rate.ToString()));
                                    stuDetalle.subTotal = Math.Abs(Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                    stuDetalle.remolcador = keyVessel;
                                    stuDetalle.sequencial = sequencialDetalle;

                                    //JAV-07/01/2020, Si Codigo de Producto es Blanco no enviar factura a Sunat, se postea para que el facturador la revoque
                                    if (string.IsNullOrEmpty(codigoProduto) && (!ordemNula))
                                    {
                                        if (setPosted)
                                        {
                                            SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                        }
                                        salir = true;
                                        break;
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

                                    //JAV - 07/01/2020, Si CB es Blanco no enviar factura a Sunat, se postea para que el facturador la revoque
                                    if (!ordemNula)
                                    {
                                        if (string.IsNullOrEmpty(codigoArticulo))
                                        {
                                            if (setPosted)
                                            {
                                                SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                            }
                                            salir = true;
                                            break;
                                        }
                                        else
                                        {
                                            salir = false;
                                        }

                                        //JAV-07/01/2020, Verificar si es CB de de Practico (Empieza en "TFI0299") validar segun su Sede si es el Correcto
                                        if (codigoArticulo.ToUpper().Contains("TFI0299"))
                                        {
                                            var query = (from t in context.entityHelm.CentroBeneficioPracticoSede
                                                         where (t.IdPuerto == documento.IdPuerto)
                                                         select t).ToList();

                                            var CentroBeneficio = query.First().CentroBeneficio.ToString();

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
                                }
                            }

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

                            if (codigoDocumento != 0)
                            {
                                documento.Numero = context.GetNumeroSequencial(codigoDocumento, areaName);
                                usrfcrfac.USR_NROFOR = Convert.ToInt32(documento.Numero);
                            }

                            //ADD JALEJOSV 26122019, Cliente relacionado en Ventas
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
                                documentoDetalhe.IdHelm = iTransactions.Id.ToString();
                                documentoDetalhe.ItemDetalle = numeroLinha;
                                documentoDetalhe.NombreArticulo = item.nombreArticulo;
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
                                                                             (x.remolcador == imposto.remolcador));
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

                                    //ADD JAV 27122019, Si es Recargo colocar el % de Descuento y llegue a Ofisis correctamente
                                    if (item.nombreArticulo.ToUpper().Contains("SURCHARGE") || item.nombreArticulo.ToUpper().Contains("RECARGO"))
                                    {
                                        desconto = lstDesconto.Find(x => (x.id == item.referenceId));
                                    }
                                    else
                                    {
                                        desconto = lstDesconto.Find(x => (x.id == item.id));
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
                                            usrfcrfai.USR_PCTBF1 = desconto.taxa;
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
                                usrfcrfai.USR_FCRFAI_NROFOR = usrfcrfac.USR_NROFOR;
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
                                        LogError errores = new LogError();
                                        errores.IdTransaccion = iTransactions.Id.ToString();
                                        errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());
                                        errores.ErrorDetalle.Add(string.Format("El servicio {0} no existe", item.nombreArticulo.ToUpper().Trim()));
                                        Correo correo = new Correo();
                                        
                                        construirCorreoError(correo, iTransactions, errores);
                                        try
                                        {
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
                                            throw;
                                        }
                                       
                                    }
                                }
                                else
                                {
                                    string servico = (item.nombreArticulo.ToUpper().Trim() == "CARGO DE ACCESO") ? "CARGO DE ACCESO" : "SERVICIOS DESDE HELM";
                                    var query = (from t in context.entityHelm.TarifaDetraccion
                                                 where (t.NombreArticulo.ToUpper() == servico.ToUpper())
                                                 select t).ToList();

                                    if (query.Count > 0)
                                    {
                                        codigoDetraccion = query[0].CodigoDetraccion;
                                        porcentajeDetraccion = (int)query[0].PorcentajeDetraccion;
                                        usrfcrfai.USR_ARTCOD = (item.nombreArticulo.ToUpper() == "CARGO DE ACCESO") ? query[0].CodigoProducto : item.codigoProduto;
                                        usrfcrfai.USR_CODDIS = item.codigoArticulo;
                                        documentoDetalhe.CodigoProducto = usrfcrfai.USR_ARTCOD;
                                        montoDTR = (moeda == "SOL") ? (decimal)query[0].TopeMinimoSoles : (decimal)query[0].TopeMinimoDolares;
                                    }
                                }

                                usrfcrfai.USR_IDHELM = iTransactions.TransactionNumber.ToString();
                                usrfcrfai.USR_MODCPT = "VT";
                                usrfcrfai.USR_TIPCPT = "A";

                                // Si es Recargo Tomar el Concepto de sus padres, siempre son los primeros servicios
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
                                            context.IncluirDocumentoDetalle(documentoDetalhe);
                                        }

                                        if (interfaceOfisisAtiva)
                                        {
                                            context.IncluirUsrFcrFai(usrfcrfai);
                                        }
                                    }
                                    else
                                    {
                                        if (somaValorIGV != 0)
                                        {
                                            if (interfaceFaturaAtiva)
                                            {
                                                context.IncluirDocumentoDetalle(documentoDetalhe);
                                            }

                                            if (interfaceOfisisAtiva)
                                            {
                                                context.IncluirUsrFcrFai(usrfcrfai);
                                            }
                                        }
                                    }
                                }
                            }

                            //JAV12012020, sale del bucle para Tx Manuales que no esten en la BD
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

                            //JAV 07/01/2019, Si es Boleta y su IGV es Cero no debe crear factura Sunat, solo debe contabilizar
                            if (documento.CodigoDocumento == 191 && documento.MontoIGV == 0)
                            {
                                LogError errores = new LogError();
                                errores.IdTransaccion = iTransactions.Id.ToString();
                                errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());
                                errores.ErrorDetalle.Add("El monto del IGV no puede ser 0 o vacio ");
                                Correo correo = new Correo();
                                construirCorreoError(correo, iTransactions, errores);
                                try
                                {
                                    EnviarCorreoElectronico(correo, true);
                                    if (setPosted)
                                    {
                                        SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                    }
                                    continue;
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                                
                            }

                            documento.MontoTotal = Math.Abs(somaFatura) + documento.MontoIGV;
                            if (documento.MontoTotal == 0)
                            {
                                LogError errores = new LogError();
                                errores.IdTransaccion = iTransactions.Id.ToString();
                                errores.Error = string.Format("Error en el documento de tipo {0} - Transaccion {1}", iTransactions.TransactionType.Name.ToString().ToUpper(), iTransactions.TransactionNumber.ToString());
                                errores.ErrorDetalle.Add("El monto total no puede ser 0");
                                Correo correo = new Correo();
                                construirCorreoError(correo, iTransactions, errores);
                                try
                                {
                                    EnviarCorreoElectronico(correo, true);
                                    if (setPosted)
                                    {
                                        SetPostedTransactionxId(client, iTransactions.Id.ToString());
                                    }
                                    continue;
                                }
                                catch (Exception)
                                {
                                    throw;
                                }
                            }
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

                            // ADD JAV 27122019, si tiene menor a 11 digitos y es DNI
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

                            if (documento.MontoTotal != 0)
                            {
                                context.SetNumeroSequencial(documento.CodigoDocumento.Value, areaName);
                                if (interfaceFaturaAtiva)
                                {
                                    context.IncluirDocumento(documento);
                                    context.PersistirFatura();
                                }
                                if (interfaceOfisisAtiva)
                                {
                                    context.IncluirUsrFcrFac(usrfcrfac);
                                    context.PersistirOfisis();
                                }
                            }

                            //lstTransactionId.Add(iTransactions.Id.ToString());
                            if (setPosted)
                            {
                                SetPostedTransactionxId(client, iTransactions.Id.ToString());
                            }
                        }
                    }
                    page++;
                    jobjTransaction = JObject.Parse(GetSyncApi($"api/v1/Jobs/Transactions/Details?page={page}&posted=false", client));
                }
                //if (lstTransactionId.Count > 0)
                //{
                //    if (setPosted)
                //    {
                //        SetPostedTransaction(client, lstTransactionId);
                //    }
                //}
            }
            catch (Exception ex)
            {
                _error.AppendLine($"[InterfaceFaturamento] - Erro: {ex.Message}");
                _error.AppendLine("");
                if (ex.InnerException != null)
                {
                    _error.AppendLine("Erro detalhado: " + ex.InnerException.ToString());
                }
                _error.AppendLine("Erro stacktrace: " + ex.StackTrace.ToString());
                _error.AppendLine("");
            }
            finally
            {
                client.Dispose();
            }

            Error = _error.ToString();
        }

       

        private bool ValidacionInformacionFactura(dynamic iTransactions, List<StructCompanies> lstCompanies, List<StructAssets> lstAssets)
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
            }else
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
                                }else if ((companies.codDoc01.Contains("114") || companies.codDoc01.Contains("02")) && companies.ccidf.Trim().Length != 11)
                                {
                                    
                                    errores.ErrorDetalle.Add(string.Format("El numero de documento del cliente {0} de tipo {1}: no posee 11 digitos.", iTransactions.AccountName.ToString(), (companies.codDoc01.Contains("114")) ? "NIF" : "RUC"));
                                }
                            }else
                            {
                                errores.ErrorDetalle.Add(string.Format("El cliente {0} no registra un numero de documento", iTransactions.AccountName.ToString().Trim()));
                            }
                    }else
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
                       if(string.IsNullOrEmpty(iAccounting.AccountingCode.ToString())) {
                            
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
                    throw;
                }
                
            }
            return true;
        }


        //private void construirCorreoError (StructMail correoEmisor, List<StructMail> correosPara, List<StructMail> correosCC, List<StructMail> correosCCO, string asunto, string cuerpo, dynamic iTransactions, LogError errores)
        private void construirCorreoError(Correo correo, dynamic iTransactions, LogError errores)
        {
            correo.correoEmisor.mail = "info_flota@tramarsa.com.pe";
            correo.correoEmisor.nameMail = "Informacion Tramarsa Flota";
            correo.correoEmisor.password = "x35#42T.";
            //correosPara.Add(new StructMail() { mail = "mmenacho@tramarsa.com.pe", nameMail = string.Empty, password = string.Empty });
            //correosPara.Add(new StructMail() { mail = "rmenachoch@tramarsa.com.pe", nameMail = string.Empty, password = string.Empty });
            //correosPara.Add(new StructMail() { mail = "AYamashiroC@tramarsa.com.pe", nameMail = string.Empty, password = string.Empty });
            //correosPara.Add(new StructMail() { mail = "JPrietoZ@tramarsa.com.pe", nameMail = string.Empty, password = string.Empty });
            correo.correosPara.Add(new StructMail() { mail = "jfuentes@dcp.pe", nameMail = "Jeyson", password = string.Empty });

            //correosCC.Add(new StructMail() { mail = "JAlejos@Tramarsa.com.pe", nameMail = "", password = "" });
            //correosCC.Add(new StructMail() { mail = "AVenturaAl@tramarsa.com.pe", nameMail = "", password = "" });
            correo.asunto = string.Format("Transacción Helm {0} tiene inconsistencia(s)", iTransactions.TransactionNumber.ToString());
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
            "<tr style='padding:10px;' align='center'>" +
                "<td align='left' valign='top'>" +
                    "<span> Saluda atentamente, </span> <br>" +
                    "<b> " + "Area de Informática <br>" +
                    "<b> " + "Tramarsa Flota <br>" + @" </b></td></tr><br></table>";

            correo.cuerpo = cabecera + detalle + pie;
            
        }
        

        private bool EnviarCorreoElectronico(Correo correo, bool esHTML,
            bool AcuseRecibo = true)
        {
            ////CONFIG CORREO
            //EmpresaBE empresa = EmpresaBR.GetInformacionEmpresa(IdEmpresa, 1);
            //string imgBase64 = empresa.Logo.Substring(empresa.Logo.IndexOf(',') + 1);
            //Bitmap b2 = new Bitmap(FunctionsImageBR.Base64StringToBitmap(imgBase64));
            //var ic2 = new ImageConverter();
            //var ba2 = (Byte[])ic2.ConvertTo(b2, typeof(Byte[]));
            //var logo2 = new MemoryStream(ba2);
            //var logoEmpresa = new LinkedResource(logo2);
            //logoEmpresa.ContentId = "LogoClEmpresa";

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
                htmlView = AlternateView.CreateAlternateViewFromString(correo.cuerpo, Encoding.UTF8, "text/html"/*, MediaTypeNames.Text.Html*/);
                //htmlView.LinkedResources.Add(logoEmpresa);
                mailMsg.AlternateViews.Add(htmlView);
            }

            //foreach (string file in rutaAdjuntos)
            //{
            //    Attachment data = new Attachment(file, MediaTypeNames.Application.Octet);
            //    ContentDisposition disposition = data.ContentDisposition;
            //    disposition.CreationDate = System.IO.File.GetCreationTime(file);
            //    disposition.ModificationDate = System.IO.File.GetLastWriteTime(file);
            //    disposition.ReadDate = System.IO.File.GetLastAccessTime(file);
            //    mailMsg.Attachments.Add(data);

            //}



            var SmtpClient = new SmtpClient();
            try
            {
                SmtpClient = new SmtpClient("smtp.office365.com", 587);
                SmtpClient.Credentials = new NetworkCredential(correo.correoEmisor.mail, correo.correoEmisor.password);
                SmtpClient.EnableSsl = true; //correoEmisor.SMTPSSL;
            }
            catch (Exception ex)
            {
                throw new Exception("No se pudo configurar la cuenta de correo electrónico. (Puerto)");
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
                    //reenviando en caso de error
                    mailMsg.DeliveryNotificationOptions = DeliveryNotificationOptions.None;
                    mailMsg.Headers.Remove("Disposition-Notification-To");
                    SmtpClient.Send(mailMsg);
                }
            }
            else
            {
                SmtpClient.Send(mailMsg);
            }
            return true;
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
            }
        }
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
        public Correo ()
        {
            correoEmisor = new StructMail() { mail = string.Empty, nameMail = string.Empty, password = string.Empty };
            correosPara = new List<StructMail>();
            correosCC = new List<StructMail>();
            correosCCO = new List<StructMail>();
            asunto = string.Empty;
            cuerpo = string.Empty;
        }
    }
}
