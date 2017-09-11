using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Library.Models
{
    public static class DbManager
    {
        public static string strConnection = ConfigurationManager.ConnectionStrings["LibraryConnectionString"].ConnectionString.ToString();

        public static DataTable GetTable(string sqlQuery)
        {
            DataTable table = new DataTable();
            using (SqlConnection sqlCon = new SqlConnection(strConnection))
            {                
                sqlCon.Open();
                SqlDataAdapter sqlDa = new SqlDataAdapter(sqlQuery, sqlCon);
                sqlDa.Fill(table);
            }
            return table;
        }

        public static void ExecuteSqlCommand(SqlCommand sqlCmd)
        {
            using (SqlConnection sqlCon = new SqlConnection(strConnection))
            {
                sqlCon.Open();
                sqlCmd.ExecuteNonQuery();
            }
        }
    }
}