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
                var rowsHash = new Dictionary<string, Dictionary<string, string>>();
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
                                    var row = new Dictionary<string, string>();
                                    for (int i = 1; i < r.FieldCount; i++)
                                        row.Add(rowsSchema[t][i - 1], r[i].ToString());
                                    rowsHash[t + "#" + r[0].ToString()] = row;
                                }
                            }
                        }
                        else
                        {
                            int cnt1 = rowsCount[t];
                            rowsCount[t] = cnt;

                            //show content of new rows
                            cmd.CommandText = "select rowid";
                            foreach (var f in rowsSchema[t])
                                cmd.CommandText += ", t." + f;
                            cmd.CommandText += " from " + t + " t";
                            var rowsFound = new Dictionary<string, Dictionary<string, string>>();
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    var row = new Dictionary<string, string>();
                                    for (int i = 1; i < r.FieldCount; i++)
                                        row.Add(rowsSchema[t][i - 1], r[i].ToString());

                                    string status = null;
                                    var hash = t + "#" + r[0].ToString();
                                    if (!rowsHash.ContainsKey(hash))
                                        status = "new";
                                    else if (RowText(rowsHash[hash]) != RowText(row))
                                        status = "update";

                                    if (status != null)
                                    {
                                        Console.WriteLine();
                                        if (status == "update")
                                            log.Debug(t + " (" + status + "): " + RowText(row, rowsHash[hash]));
                                        else
                                            log.Debug(t + " (" + status + "): " + RowText(row));
                                        rowsHash[hash] = row;
                                    }
                                    rowsFound.Add(hash, row);
                                }
                            }

                            var rowsRemove = new List<string>();
                            foreach (var hash in rowsHash.Keys)
                            {
                                if (!hash.StartsWith(t + "#"))
                                    continue;
                                if (!rowsFound.ContainsKey(hash))
                                {
                                    Console.WriteLine();
                                    log.Debug(t + " (delete): " + RowText(rowsHash[hash]));
                                    rowsRemove.Add(hash);
                                }
                            }
                            foreach (var hash in rowsRemove)
                                rowsHash.Remove(hash);
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

        private static string RowText(Dictionary<string, string> row, Dictionary<string, string> row1 = null)
        {
            var res = "";
            foreach (var f in row.Keys)
            {
                if (res.Length > 0)
                    res += ", ";
                res += f + "=" + row[f].ToString();
                if (row1 != null && row1.ContainsKey(f) && row[f].ToString() != row1[f].ToString())
                    res += " (" + row1[f].ToString() + ")";
            }
            res = res.Replace("\r", "").Replace("\n", " ");
            return res;
        }

        static ILog log;
    }
}
