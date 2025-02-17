using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace WorkSpace.Library
{
    public class ConnectServer
    {
        public ConnectServer()
        {
        }

        public DataSet Retreive(String sql, String Connection)
        {
            #region Code
            try
            {
                SqlConnection conn = new SqlConnection(Connection);

                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                conn.Open();

                SqlDataAdapter da = new SqlDataAdapter(sql, conn);

                DataSet ds = new DataSet();

                da.Fill(ds, "DATA");

                conn.Close();

                conn.Dispose();

                return ds;
            }
            catch (Exception ex)
            {
                ErrorLog = ex.Message;

                return null;
            }
            #endregion
        }

        public Boolean Execute(String[] sql, String Connection)
        {
            #region Code
            SqlTransaction transaction = null;

            try
            {
                SqlConnection conn = new SqlConnection(Connection);

                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                conn.Open();

                transaction = conn.BeginTransaction();

                for (Int32 il = 0; il < sql.Length; il++)
                {
                    new SqlCommand(sql[il], conn, transaction).ExecuteNonQuery();
                }

                transaction.Commit();

                return true;
            }
            catch (Exception ex)
            {
                ErrorLog = ex.Message;

                if (transaction != null)
                    transaction.Rollback();

                return false;
            }
            #endregion
        }

        public Boolean Execute(String sql, String Connection)
        {
            #region Code
            try
            {
                SqlConnection conn = new SqlConnection(Connection);

                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                conn.Open();

                SqlCommand comm = new SqlCommand();

                comm.CommandText = sql;

                comm.CommandType = System.Data.CommandType.Text;

                comm.Connection = conn;

                comm.ExecuteNonQuery();

                conn.Close();

                return true;
            }
            catch (Exception ex)
            {
                ErrorLog = ex.Message;

                return false;
            }
            #endregion
        }

        private String LastQuery = "";

        private String ErrorLog;

        public String _ErrorLog
        {
            set
            {
                ErrorLog = value;
            }
            get
            {
                return ErrorLog;
            }
        }
        
        public Boolean Execute(String SQL, String Parameter, String img, String Connection)
        {
            if (img != null)
            {
                try
                {
                    SqlConnection conn = new SqlConnection(Connection);

                    if (conn.State == System.Data.ConnectionState.Open)
                        conn.Close();

                    conn.Open();

                    SqlCommand cmd = new SqlCommand(SQL, conn);

                    SqlParameter pic = new SqlParameter(Parameter, SqlDbType.Text);

                    pic.Value = img;

                    cmd.Parameters.Add(pic);

                    cmd.Connection = conn;

                    cmd.CommandTimeout = 250;

                    cmd.ExecuteNonQuery();

                    conn.Close();

                    return true;
                }
                catch (Exception ex)
                {
                    ErrorLog = ex.Message;

                    return false;
                }
            }
            else
            {
                ErrorLog = "ไม่พบข้อมูล Byte[] ที่ส่งมา";

                return false;
            }
        }
    }
}
