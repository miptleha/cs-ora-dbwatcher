using Log;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                LogManager.Init();
                log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

                string tables = ConfigurationManager.AppSettings["Tables"];

                Console.WriteLine("\nTo many text for console?\nFull log here: bin\\Debug\\log\\DbWatcher.log\n");
                log.Debug("Scanning tables: " + tables);

                var conStr = ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;
                log.Debug("Connection string: " + conStr);

                var con = new OracleConnection(conStr);
                con.Open();

                var cmd = con.CreateCommand();

                var rowsSchema = new Dictionary<string, List<string>>();
                foreach (var t in tables.Split(';', ',', ' '))
                {
                    if (string.IsNullOrWhiteSpace(t))
                        continue;

                    cmd.CommandText = "select * from " + t;
                    IDataReader reader = null;
                    reader = cmd.ExecuteReader();
                    var schema = reader.GetSchemaTable();
                    rowsSchema.Add(t, new List<string>());
                    foreach (DataRow r in schema.Rows)
                    {
                        if (r["ProviderType"].ToString() == "116")
                            continue;
                        if (r["DataType"].ToString().Contains("Byte[]"))
                            continue;
                        rowsSchema[t].Add(r["ColumnName"].ToString());
                    }
                }


                var rowsCount = new Dictionary<string, int>();
                var rowsHash = new Dictionary<string, string>();
                while (true)
                {
                    foreach (var t in tables.Split(';', ',', ' '))
                    {
                        if (string.IsNullOrWhiteSpace(t))
                            continue;

                        cmd.CommandText = "select count(*) from " + t;
                        int cnt = int.Parse(cmd.ExecuteScalar().ToString());

                        if (!rowsCount.ContainsKey(t))
                        {
                            rowsCount.Add(t, cnt);
                            cmd.CommandText = "select rowid";
                            foreach (var f in rowsSchema[t])
                                cmd.CommandText += ", t." + f;
                            cmd.CommandText += " from " + t + " t";
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string row = "";
                                    for (int i = 1; i < r.FieldCount; i++)
                                    {
                                        if (row.Length > 0)
                                            row += ", ";
                                        row += rowsSchema[t][i - 1] + "=" + r[i].ToString();
                                    }
                                    row = row.Replace("\r", "").Replace("\n", " ");
                                    rowsHash[t + "_" + r[0].ToString()] = row;
                                }
                            }
                        }
                        else
                        {
                            int cnt1 = rowsCount[t];
                            if (cnt > cnt1)
                            {
                                //log.Debug(string.Format("Added {0} rows to table {1}", cnt - cnt1, t));
                            }
                            else if (cnt < cnt1)
                            {
                                log.Debug(string.Format("Deleted {0} rows from table {1}", cnt1 - cnt, t));
                            }
                            rowsCount[t] = cnt;

                            //show content of new rows
                            cmd.CommandText = "select rowid";
                            foreach (var f in rowsSchema[t])
                                cmd.CommandText += ", t." + f;
                            cmd.CommandText += " from " + t + " t";
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string row = "";
                                    for (int i = 1; i < r.FieldCount; i++)
                                    {
                                        if (row.Length > 0)
                                            row += ", ";
                                        row += rowsSchema[t][i - 1] + "=" + r[i].ToString();
                                    }
                                    row = row.Replace("\r", "").Replace("\n", " ");

                                    string status = null;
                                    if (!rowsHash.ContainsKey(t + "_" + r[0].ToString()))
                                        status = "new";
                                    else if (rowsHash[t + "_" + r[0].ToString()] != row)
                                        status = "update";

                                    if (status != null)
                                    {
                                        log.Debug(t + " (" + status + "): " + row);
                                        if (status == "update")
                                            log.Debug(t + " (before): " + rowsHash[t + "_" + r[0].ToString()]);
                                        rowsHash[t + "_" + r[0].ToString()] = row;
                                    }
                                }
                            }
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        static ILog log;
    }
}
