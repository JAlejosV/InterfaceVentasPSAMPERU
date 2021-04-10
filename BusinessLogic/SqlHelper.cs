using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class SqlHelper
    {
        private static int GetTimeOut()
        {
            return WebConfigReader.TimeOutSqlCommand;
        }

        public static SqlCommand CreateCommand()
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandTimeout = GetTimeOut();
            cmd.CommandType = CommandType.StoredProcedure;

            return cmd;
        }

        public static SqlCommand CreateCommand(string commandText)
        {
            SqlCommand cmd = new SqlCommand(commandText);
            cmd.CommandTimeout = GetTimeOut();
            cmd.CommandType = CommandType.StoredProcedure;

            return cmd;
        }

        public static SqlCommand CreateCommand(string commandText, SqlConnection sqlConnection, bool abrirConnection = false)
        {
            SqlCommand cmd = new SqlCommand(commandText, sqlConnection);
            cmd.CommandTimeout = GetTimeOut();
            cmd.CommandType = CommandType.StoredProcedure;
            if (abrirConnection)
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

            return cmd;
        }

        public static SqlCommand CreateCommandWithParameters(string commandText, SqlConnection sqlConnection, Dictionary<string, object> parametersIn, bool abrirConnection = false, Dictionary<string, object> parametersOut = null)
        {
            SqlCommand cmd = new SqlCommand(commandText, sqlConnection);
            cmd.CommandTimeout = GetTimeOut();
            cmd.CommandType = CommandType.StoredProcedure;


            foreach (KeyValuePair<string, object> p in parametersIn)
            {
                cmd.Parameters.AddWithValue(p.Key, p.Value);
            }

            if (parametersOut != null)
            {
                foreach (KeyValuePair<string, object> pout in parametersOut)
                {
                    cmd.Parameters.AddWithValue(pout.Key, pout.Value).Direction = ParameterDirection.Output;
                }
            }

            if (abrirConnection)
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

            return cmd;
        }


        public static SqlCommand CreateCommandWithParameterCollection(string commandText, SqlConnection sqlConnection, SqlParameterCollection parametersIn, bool abrirConnection = false, SqlParameterCollection parametersOut = null)
        {
            SqlCommand cmd = new SqlCommand(commandText, sqlConnection);
            cmd.CommandTimeout = GetTimeOut();
            cmd.CommandType = CommandType.StoredProcedure;


            foreach (SqlParameter p in parametersIn)
            {
                cmd.Parameters.Add(p);
            }

            if (parametersOut != null)
            {
                foreach (SqlParameter pout in parametersOut)
                {
                    cmd.Parameters.Add(pout);
                    //.Direction = ParameterDirection.Output;
                }
            }

            if (abrirConnection)
                if (sqlConnection.State != ConnectionState.Open)
                    sqlConnection.Open();

            return cmd;
        }

        public static void CloseConnection(SqlConnection sqlConnection)
        {
            if (sqlConnection.State == ConnectionState.Open)
                sqlConnection.Close();
        }

    }
}
