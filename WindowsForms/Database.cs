using System.Data;
using Microsoft.Data.SqlClient;

namespace Window_Forms
{
    public static class Database
    {
        public const string ConnectionString = "Your Connection String;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using SqlConnection conn = GetConnection();
            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);

            DataTable table = new DataTable();
            using SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            adapter.Fill(table);
            return table;
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object? Scalar(string sql, params SqlParameter[] parameters)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            object? value = cmd.ExecuteScalar();
            return value == DBNull.Value ? null : value;
        }

        public static object ValueOrDbNull(object? value)
        {
            if (value == null)
                return DBNull.Value;

            if (value is string text && string.IsNullOrWhiteSpace(text))
                return DBNull.Value;

            return value;
        }
        public static DataTable QueryProcedure(string procedureName)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlCommand cmd = new SqlCommand(procedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            DataTable dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }

        public static DataTable QueryProcedure(string procedureName, params SqlParameter[] parameters)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlCommand cmd = new SqlCommand(procedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            DataTable dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt;
        }
        public static void ExecuteProcedure(string procedureName, params SqlParameter[] parameters)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlCommand cmd = new SqlCommand(procedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
        }
        public static int ExecuteProcedureWithCount(string procedureName, params SqlParameter[] parameters)
        {
            using SqlConnection conn = GetConnection();
            conn.Open();
            using SqlCommand cmd = new SqlCommand(procedureName, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }
    }
}
