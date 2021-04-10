using System;
using System.IO;
using System.Text;
using System.Configuration;
using BusinessLogic;

using System.Runtime.InteropServices;

namespace HelmInterface
{
    class Program
    {
        static void Main(string[] args)
        {

            ClsFatura fatura = new ClsFatura();

            StringBuilder textoLog = new StringBuilder();
            
            try
            {
                textoLog.AppendLine("Arquivo de log - Interface de Faturamento Eletrônico");
                textoLog.Append("Início do processamento: ");
                textoLog.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                textoLog.AppendLine("");

                fatura.InterfaceFaturamento();

                if (!string.IsNullOrEmpty(fatura.Error))
                {
                    textoLog.AppendLine("");
                    textoLog.AppendLine("----------------------------------------");
                    textoLog.AppendLine($"Erro de processamento: {fatura.Error}");
                    textoLog.AppendLine("----------------------------------------");
                    textoLog.AppendLine("");
                }
            }
            catch(Exception ex)
            {
                string fileName = ConfigurationManager.AppSettings["pathFileLog"].ToString();
                fileName += $"HelmInterface_{DateTime.Now.ToString("yyyyMMddHHmmss")}.log";
                StreamWriter writerFileLog = new StreamWriter(fileName.ToString());

                textoLog.AppendLine("");
                textoLog.AppendLine("----------------------------------------");
                textoLog.AppendLine($"Erro crítico: {ex.Message}");
                textoLog.AppendLine("----------------------------------------");
                textoLog.AppendLine("");

                textoLog.Append("Fim do processamento: ");
                textoLog.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                writerFileLog.Write(textoLog);
                writerFileLog.Close();
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
