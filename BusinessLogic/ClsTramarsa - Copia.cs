using DataAccess;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;

namespace BusinessLogic
{
    public class ClsTramarsa
    {
        public string Error = "";

        private struct StrucImposto
        {
            public string Id { get; set; }
            public string Descricao { get; set; }
            public string Remolcador { get; set; }
            public decimal Taxa { get; set; }
        }

        private struct StructDetalle
        {
            public string CodigoArticulo { get; set; }
            public string NombreArticulo { get; set; }
            public string Remolcador { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal SubTotal { get; set; }
        }

        private struct StructCompanies
        {
            public string Id { get; set; }
            public string AccountNumber { get; set; }
            public string Name { get; set; }
            public string Ruc01 { get; set; }
            public string CCIDF { get; set; }
            public string CodDoc01 { get; set; }
            public string Address { get; set; }
            public string Email { get; set; }
        }

        StringBuilder _error = new StringBuilder();

        public void InterfaceFaturamento()
        {           
            HttpClient client = new HttpClient();
            ClsDal context = new ClsDal();
            
            try
            {
                CultureInfo usCulture = new CultureInfo("en-US");
                
                byte tipoVenda = 0;

                bool temImposto;
                bool setPosted = Convert.ToBoolean(ConfigurationManager.AppSettings["setPosted"].ToString());
                bool notaCredito;

                string uriAddress = ConfigurationManager.AppSettings["uriBase"].ToString();
                string mediaType = ConfigurationManager.AppSettings["mediaType"].ToString();
                string apiKey = ConfigurationManager.AppSettings["apiKey"].ToString();
                string metodoTransaction = "";
                string metodoOrder = "";
                string itemServico = "";
                string tipoTransacao = "";
                string clienteNacional = "";
                string prefixoSerie;
                string keyVessel;
                string keyRebaje;
                string idAlternativo = "";
                string codigoArticulo = "";

                int codigoDocumento;
                int numeroLinha;
                int qtdDias = 0;
                int page = 1;
                int totalPages = 0;

                decimal montoDTR = 0;
                decimal totalRegistros = 0;
                decimal somaDesconto;
                decimal somaValorIGV;
                decimal somaFatura;
                decimal valorBruto;
                
                List<string> lstTransactionId = new List<string>();
              
                List<StructCompanies> lstCompanies = new List<StructCompanies>();
                                             
                client.BaseAddress = new System.Uri(uriAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
                client.DefaultRequestHeaders.Add("API-Key", apiKey);

                try
                {
                    montoDTR = Convert.ToDecimal(ConfigurationManager.AppSettings["montoDTR"].ToString().Replace(",", "."), usCulture);
                }
                catch
                {
                    montoDTR = 0;
                }

                GetCompanies(lstCompanies, client);

                metodoTransaction = $"api/v1/Jobs/Transactions/Details?page={page}&posted=false";
                JObject jobjTransaction = JObject.Parse(GetSyncApi(metodoTransaction, client));
                totalRegistros = Convert.ToInt32(jobjTransaction["Data"]["TotalCount"].ToString());
                totalPages = (int)Decimal.Ceiling(totalRegistros / 100);
                do
                {
                    dynamic dynTransactions = (JArray)jobjTransaction["Data"]["Page"];
                    foreach (var iTransactions in dynTransactions)
                    {
                        List<StrucImposto> lstImposto = new List<StrucImposto>();

                        tipoTransacao = iTransactions.TransactionType.Name.ToString().ToUpper();
                        if (tipoTransacao != "COMISIONES")
                        {
                            Documento documento = new Documento();
                            Hashtable htOrder = new Hashtable();

                            somaDesconto = 0;
                            somaValorIGV = 0;
                            somaFatura = 0;
                            codigoDocumento = 0;
                            numeroLinha = 0;
                            tipoVenda = 0;
                            valorBruto = 0;
                            temImposto = false;
                            prefixoSerie = "";

                            StructCompanies companies = lstCompanies.Find(x => x.AccountNumber == iTransactions.AccountNumber.ToString());
                            if (!String.IsNullOrEmpty(companies.AccountNumber))
                            {
                                
                                documento.DireccionCliente = companies.Address;
                                documento.NumeroDocumentoCliente = companies.CCIDF;
                                documento.CodigoDocumentoCliente = Convert.ToInt32(companies.CodDoc01);                               
                                documento.EmailCliente = companies.Email;
                                clienteNacional = companies.Ruc01;
                                if (String.IsNullOrEmpty(clienteNacional))
                                {
                                    clienteNacional = "";
                                }
                            }

                            if (!String.IsNullOrEmpty(iTransactions.Order.ToString()))
                            {
                                metodoOrder = $"api/v1/Jobs/Orders/FindOrders?page=1&OrderNumber={iTransactions.Order.OrderNumber.ToString()}";
                                GetOrderUserDefined(metodoOrder, client, htOrder);
                                if (!String.IsNullOrEmpty((string)htOrder["BEN01"]))
                                {
                                    StructCompanies idCompany = lstCompanies.Find(x => x.Id == (string)htOrder["BEN01"]);
                                    documento.NombreSolidario = idCompany.Name;
                                }
                                else
                                {
                                    documento.NombreSolidario = null;
                                }
                                documento.TRB = Math.Abs(Convert.ToDecimal(htOrder["TRB01"]));
                                documento.Eslora = Math.Abs(Convert.ToDecimal(htOrder["ESL01"]));
                            }

                            documento.IdHelm = iTransactions.Id.ToString();
                            documento.NombreCliente = iTransactions.AccountName.ToString();
                            documento.CodigoMoneda = Convert.ToInt32(iTransactions.CurrencyType.ExternalSystemCode.ToString());
                            documento.TransaccionHelm = iTransactions.TransactionNumber.ToString();
                            documento.SerieReferencia = iTransactions.Area.ExternalSystemCode.ToString();
                            documento.FechaEmision = Convert.ToDateTime(iTransactions.TransactionDate.ToString());
                            documento.SerieReferencia2 = null;
                            documento.NumeroReferencia2 = null;

                            notaCredito = iTransactions.TransactionNumber.ToString().Contains("RV");

                            if (!String.IsNullOrEmpty(iTransactions.Order.ToString()))
                            {
                                documento.OrdenCompra = iTransactions.Order.OrderNumber.ToString();
                                documento.NombreNave = $"{iTransactions.Order.ShipName.ToString()}-{iTransactions.Order.VoyageNumber.ToString()}";
                            }

                            documento.Observaciones = iTransactions.Note.ToString();

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
                                qtdDias = Convert.ToInt32(iTransactions.AccountingTerm.NumberOfDays.ToString());
                                documento.FormaPago = "CE0" + iTransactions.AccountingTerm.NumberOfDays.ToString();
                            }
                            catch
                            {
                                documento.FormaPago = "CE000";
                            }

                            Dictionary<string, StructDetalle> dicDetalle = new Dictionary<string, StructDetalle>();
                            Dictionary<string, decimal> dicDescuento = new Dictionary<string, decimal>();
                            Dictionary<string, decimal> dicRebaje = new Dictionary<string, decimal>();
                            decimal valorDescontos = 0;
                            decimal valorRibajes = 0;

                            dynamic dynTransactionsLines = iTransactions.TransactionLines.Children();
                            foreach (var iTransactionsLines in dynTransactionsLines)
                            {
                                StructDetalle stuDetalle = new StructDetalle();
                                StrucImposto stuImposto = new StrucImposto();

                                idAlternativo = "";
                                codigoArticulo = "";
                                keyRebaje = "";
                                keyVessel = "";
                                
                                itemServico = iTransactionsLines.DetailDescription.ToString().ToUpper();
                                if (itemServico.Contains("REBAJE"))
                                {
                                    dynamic dynAccountingCodes = iTransactionsLines.RevenueAllocations[0].AccountingCodes.Children();
                                    foreach (var iAccountingCodes in dynAccountingCodes)
                                    {
                                        if ((iAccountingCodes.ReferenceType.ToString().ToUpper() == "VESSEL") || 
                                            (iAccountingCodes.ReferenceType.ToString().ToUpper() == "RESOURCE"))
                                        {
                                            keyVessel = iAccountingCodes.EntityName.ToString();
                                            break;
                                        }
                                    }

                                    keyRebaje = itemServico.Replace(".", "");
                                    if (dicRebaje.ContainsKey(keyRebaje + keyVessel))
                                    {
                                        valorRibajes = Math.Abs(dicRebaje[keyRebaje + keyVessel]);
                                        valorRibajes += Math.Abs(Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                        dicRebaje.Remove(keyRebaje + keyVessel);
                                        dicRebaje.Add(keyRebaje + keyVessel, valorRibajes * -1);
                                    }
                                    else
                                    {
                                        dicRebaje.Add(keyRebaje + keyVessel,
                                                      Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                    }
                                }
                                else if (itemServico.Contains("IMPUESTO") || itemServico.Contains("TAX") || itemServico.Contains("DESCUENTO") || itemServico.Contains("DISCOUNT"))
                                {
                                    if (itemServico.Contains("DESCUENTO") || itemServico.Contains("DISCOUNT"))
                                    {
                                        if (String.IsNullOrEmpty(iTransactionsLines.RevenueAllocations[0].ReferenceTransactionLineId.ToString()))
                                        {
                                            idAlternativo = iTransactionsLines.RevenueAllocations[0].Id.ToString();
                                        }
                                        else
                                        {
                                            idAlternativo = iTransactionsLines.RevenueAllocations[0].ReferenceTransactionLineId.ToString();
                                        }
                                        if (dicDescuento.ContainsKey(idAlternativo))
                                        {
                                            valorDescontos = Math.Abs(dicDescuento[idAlternativo]);
                                            valorDescontos += Math.Abs(Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                            dicDescuento.Remove(idAlternativo);
                                            dicDescuento.Add(idAlternativo,
                                                             valorDescontos * -1);
                                        }
                                        else
                                        {
                                            dicDescuento.Add(idAlternativo,
                                                         Convert.ToDecimal(iTransactionsLines.Amount.ToString()));
                                        }
                                    }
                                    else
                                    {
                                        dynamic dynAccountingCodes = iTransactionsLines.RevenueAllocations[0].AccountingCodes.Children();
                                        foreach (var iAccountingCodes in dynAccountingCodes)
                                        {
                                            if ((iAccountingCodes.ReferenceType.ToString().ToUpper() == "VESSEL") ||
                                                (iAccountingCodes.ReferenceType.ToString().ToUpper() == "RESOURCE"))
                                            {
                                                keyVessel = iAccountingCodes.EntityName.ToString();
                                                break;
                                            }
                                        }

                                        stuImposto.Id = iTransactionsLines.Id.ToString();
                                        stuImposto.Descricao = itemServico;
                                        stuImposto.Taxa = Convert.ToDecimal(iTransactionsLines.Rate.ToString());
                                        stuImposto.Remolcador = keyVessel;
                                        lstImposto.Add(stuImposto);
                                        temImposto = true;
                                    }
                                }
                                else
                                {
                                    stuDetalle.NombreArticulo = iTransactionsLines.DetailDescription.ToString();
                                    stuDetalle.Cantidad = Math.Abs(Convert.ToDecimal(iTransactionsLines.Quantity.ToString()));
                                    stuDetalle.PrecioUnitario = Math.Abs(Convert.ToDecimal(iTransactionsLines.Rate.ToString()));
                                    stuDetalle.SubTotal = Math.Abs(Convert.ToDecimal(iTransactionsLines.Amount.ToString()));

                                    dynamic dynTransactionsLinesRevenueAllocations = iTransactionsLines.RevenueAllocations[0].AccountingCodes.Children();
                                    foreach (var item in dynTransactionsLinesRevenueAllocations)
                                    {
                                        if ((item.AccountingCode.ToString().ToUpper() == "BV") && (codigoDocumento == 0))
                                        {
                                            codigoDocumento = 191;
                                            prefixoSerie = "B";
                                        }
                                        else if ((item.AccountingCode.ToString().ToUpper() == "FAC") && (codigoDocumento == 0))
                                        {
                                            codigoDocumento = 184;
                                            prefixoSerie = "F";
                                        }

                                        if ((String.IsNullOrEmpty(codigoArticulo)) &&
                                            (item.AccountingCode.ToString().ToUpper() != "FAC") &&
                                            (item.AccountingCode.ToString().ToUpper() != "BV"))
                                        {
                                            codigoArticulo = item.AccountingCode.ToString();
                                        }

                                        if ((item.ReferenceType.ToString().ToUpper() == "VESSEL") || 
                                            (item.ReferenceType.ToString().ToUpper() == "RESOURCE"))
                                        {
                                            stuDetalle.Remolcador = item.EntityName.ToString();
                                        }
                                    }

                                    stuDetalle.CodigoArticulo = codigoArticulo;

                                    dicDetalle.Add(iTransactionsLines.Id.ToString(), stuDetalle);
                                }
                            }

                            foreach (KeyValuePair<string, StructDetalle> item in dicDetalle)
                            {
                                DocumentoDetalle documentoDetalhe = new DocumentoDetalle();
                                string servicoImposto = "";
                                string servicoRedutorImposto = "";
                                decimal redutorImposto = 0;

                                numeroLinha += 1;
                                documentoDetalhe.IdHelm = iTransactions.Id.ToString();
                                documentoDetalhe.ItemDetalle = numeroLinha;
                                documentoDetalhe.NombreArticulo = item.Value.NombreArticulo;
                                documentoDetalhe.Cantidad = item.Value.Cantidad;
                                documentoDetalhe.PrecioUnitario = item.Value.PrecioUnitario;
                                documentoDetalhe.CodigoArticulo = item.Value.CodigoArticulo;
                                documentoDetalhe.EsDescuento = false;
                                documentoDetalhe.MontoIGV = 0;
                                documentoDetalhe.Descuento = 0;
                                documentoDetalhe.TipoPrecio = 2;

                                if (temImposto)
                                {
                                    StrucImposto imposto = lstImposto.Find(x => (x.Descricao == "IMPUESTO " + item.Value.NombreArticulo.ToUpper()) && (x.Remolcador == item.Value.Remolcador));
                                    if (!String.IsNullOrEmpty(imposto.Id))
                                    {
                                        documentoDetalhe.MontoIGV = Math.Round(Math.Abs(item.Value.SubTotal) * Math.Abs(imposto.Taxa), 5);
                                        somaValorIGV += documentoDetalhe.MontoIGV.Value;
                                        documentoDetalhe.TipoPrecio = 1;
                                    }
                                    else
                                    {
                                        foreach (var i in lstImposto)
                                        {
                                            servicoImposto = i.Descricao.Replace("IMPUESTO ", "").Trim();
                                            if (item.Value.NombreArticulo.ToUpper().Contains(servicoImposto))
                                            {
                                                if ((item.Value.NombreArticulo.ToUpper().Contains(servicoImposto)) &&
                                                    (item.Value.Remolcador == i.Remolcador))
                                                {
                                                    if (dicRebaje.Count > 0)
                                                    {
                                                        servicoRedutorImposto = "REBAJE IMPUESTO POR DESCUENTO " + servicoImposto + i.Remolcador;
                                                        if (dicRebaje.ContainsKey(servicoRedutorImposto))
                                                        {
                                                            redutorImposto = Math.Abs(dicRebaje[servicoRedutorImposto]);
                                                        }
                                                    }
                                                    documentoDetalhe.MontoIGV = (Math.Round(Math.Abs(item.Value.SubTotal) * Math.Abs(i.Taxa), 5)) - redutorImposto;
                                                    somaValorIGV += documentoDetalhe.MontoIGV.Value;
                                                    documentoDetalhe.TipoPrecio = 1;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (dicDescuento.Count > 0)
                                {
                                    if (dicDescuento.ContainsKey(item.Key))
                                    {
                                        documentoDetalhe.Descuento = Math.Abs(dicDescuento[item.Key]);
                                        somaDesconto += Math.Abs(dicDescuento[item.Key]);
                                        documentoDetalhe.EsDescuento = true;
                                    }
                                }

                                documentoDetalhe.MontoBruto = Math.Abs(item.Value.SubTotal);
                                documentoDetalhe.SubTotal = Math.Abs(item.Value.SubTotal);
                                documentoDetalhe.NombreUnidadMedida = "Unidad";
                                documentoDetalhe.SiglaUnidadMedida = "UN";
                                documentoDetalhe.UnidadMedidaSunat = "ZZ";
                                documentoDetalhe.MontoMinimoConsumidorFinal = 0;
                                documentoDetalhe.MontoISC = 0;
                                documentoDetalhe.TasaISC = 0;
                                documentoDetalhe.TipoISC = 0;
                                documentoDetalhe.TipoPercepcion = 0;
                                documentoDetalhe.ImportePercepcion = 0;
                                documentoDetalhe.PorcentajePercepcionArticulo = 0;
                                documentoDetalhe.PorcentajePercepcionVenta = 0;

                                somaFatura += documentoDetalhe.MontoBruto.Value - documentoDetalhe.Descuento.Value;
                                valorBruto += documentoDetalhe.MontoBruto.Value;

                                context.IncluirDocumentoDetalle(documentoDetalhe);
                            }

                            if (somaValorIGV == 0)
                            {
                                documento.MontoInafecto = Math.Abs(valorBruto);
                                documento.MontoAfecto = 0;
                            }
                            else
                            {
                                documento.MontoAfecto = Math.Abs(valorBruto) - Math.Abs(somaDesconto);
                                documento.MontoInafecto = 0;
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
                                            break;
                                        case "NOTA CREDITO":
                                            codigoDocumento = 15;
                                            prefixoSerie = "NC";
                                            break;
                                        case "NOTA CREDITO ELECTRONICA":
                                            codigoDocumento = 186;
                                            prefixoSerie = "NC";
                                            break;
                                        case "NOTA DEBITO":
                                            codigoDocumento = 16;
                                            prefixoSerie = "ND";
                                            break;
                                        case "NOTA DEBITO ELECTRONICA":
                                            codigoDocumento = 187;
                                            prefixoSerie = "ND";
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                codigoDocumento = 186;
                                prefixoSerie = "NC";
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
                            documento.MontoIGV = Math.Abs(somaValorIGV);
                            documento.TotalDescuento = Math.Abs(somaDesconto);
                            documento.DescuentoGlobal = Math.Abs(somaDesconto);
                            documento.MontoTotal = Math.Abs(somaFatura) + Math.Abs(somaValorIGV);
                            documento.MontoISC = 0;
                            documento.MontoExonerado = 0;
                            documento.MontoDonacion = 0;
                            documento.MontoRegalo = 0;

                            if (iTransactions.Area.Name.ToString().ToUpper() == "TALARA")
                            {
                                documento.Serie =  prefixoSerie + "002";
                            }
                            else
                            {
                                documento.Serie = prefixoSerie + "001";
                            }

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
                            if (montoDTR != -1)
                            {
                                if ((clienteNacional.ToUpper() == "SÍ") &&
                                    (documento.MontoIGV.Value > 0) &&
                                    (documento.MontoTotal.Value > montoDTR))
                                {
                                    documento.CodigoDetraccion = "037";
                                    documento.PorcentajeDetraccion = 12;
                                    documento.GlosaDetraccion = "OPERACION SUJETA A SPOT";
                                    documento.AfectoDetraccion = true;
                                }
                            }
                            
                            context.PreencherNumeroSequencial(documento);
                            context.IncluirDocumento(documento);
                            context.Persistir();

                            lstTransactionId.Add(iTransactions.Id.ToString());
                        }                       
                    }
                    page++;

                    if (page <= totalPages)
                    {
                        metodoTransaction = $"api/v1/Jobs/Transactions/Details?page={page}&posted=false";
                        jobjTransaction = JObject.Parse(GetSyncApi(metodoTransaction, client));
                    }

                } while (page <= totalPages);

                if (setPosted)
                {
                    SetPostedTransaction(client, lstTransactionId);
                }
            }
            catch (Exception ex)
            {
                _error.AppendLine($"Erro genérico: {ex.Message}");
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

        private void SetPostedTransaction(HttpClient http, List<string> lstParameter)
        {
            string metodo = "api/v1/jobs/Transactions/SetPosted";
            int page = 1;
            int totalPages = (int)Decimal.Ceiling(lstParameter.Count / 100);
            int posicaoLista = 0;
            int registrosLidos = 1;

            do
            {
                JTokenWriter writerJson = new JTokenWriter();

                writerJson.WriteStartObject();
                writerJson.WritePropertyName("Assignments");
                writerJson.WriteStartArray();
                
                for (int index = posicaoLista; index < lstParameter.Count; index++ )
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

        private void GetCompanies(List<StructCompanies> lstParameter, HttpClient httpParameter)
        {
            string metodoCompany = "";
            string codigoDocumento = "";
            string ccidf = "";
            string ruc01 = "";

            bool id = false;

            int page = 1;
            int totalPages = 0;

            decimal totalRegistros = 0;

            metodoCompany = $"api/v1/jobs/companies/FindCompanies?page={page}";
            JObject jobjCompany = JObject.Parse(GetSyncApi(metodoCompany, httpParameter));
            totalRegistros = Convert.ToInt32(jobjCompany["Data"]["TotalCount"].ToString());
            totalPages = (int)Decimal.Ceiling(totalRegistros / 100);
            do
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

                    dynamic dynCompaniesAccounts = iCompanies.Accounts.Children();
                    foreach (var iAccount in dynCompaniesAccounts)
                    {
                        StructCompanies companies = new StructCompanies();
                        id = true;

                        companies.AccountNumber = iAccount.AccountNumber.ToString();
                        companies.Name = iAccount.Name.ToString();
                        companies.Id = iAccount.Id.ToString();
                        companies.Ruc01 = ruc01;
                        companies.CCIDF = ccidf;
                        companies.CodDoc01 = codigoDocumento;

                        dynamic dynCompaniesAddresses = iAccount.Addresses.Children();
                        foreach (var iAddress in dynCompaniesAddresses)
                        {                           
                            if (!String.IsNullOrEmpty(iAddress.Address.ToString()))
                            {
                                companies.Address = iAddress.Address.ToString();
                            }
                            else
                            {
                                companies.Address = ".";
                            }
                            companies.Email = iAddress.Email.ToString();
                        }

                        lstParameter.Add(companies);
                    }
                    if (!id)
                    {
                        StructCompanies companies = new StructCompanies
                        {
                            Ruc01 = ruc01,
                            CCIDF = ccidf,
                            CodDoc01 = codigoDocumento,
                            Name = iCompanies.Name.ToString(),
                            Id = iCompanies.Id.ToString()
                        };

                        lstParameter.Add(companies);
                    }
                }
                page++;

                if (page <= totalPages)
                {
                    metodoCompany = $"api/v1/jobs/companies/FindCompanies?page={page}";
                    jobjCompany = JObject.Parse(GetSyncApi(metodoCompany, httpParameter));
                }
            } while (page <= totalPages);
        }

        private void GetOrderUserDefined(string metodoParameter, HttpClient httpParameter ,Hashtable htParameter)
        {
            JObject jobjCompany = JObject.Parse(GetSyncApi(metodoParameter, httpParameter));

            dynamic dynOrder = (JArray)jobjCompany["Data"]["Page"];
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
            catch(Exception ex)
            {
                _error.AppendLine($"Erro crítico (GetSyncApi) - {metodo}: {ex.Message}");
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
                _error.AppendLine($"Erro crítico (GetSyncApi) - {metodo}: {ex.Message}");
            }
        }
    }
}
