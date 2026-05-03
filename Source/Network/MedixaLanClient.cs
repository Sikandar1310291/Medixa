using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace PharmaBilling.Source.Network
{
    /// <summary>
    /// HTTP client that connects to MedixaLanServer on the Server PC (port 5000).
    /// Used by CLIENT-mode DbHelper to access the remote database via HTTP.
    /// This eliminates all SQLite-over-SMB problems (per SQLite documentation).
    /// </summary>
    public class MedixaLanClient
    {
        public string ServerIP { get; private set; }
        private readonly string _baseUrl;
        private readonly JavaScriptSerializer _json;
        private const string SecretKey = "MEDIXA_LAN_2025";
        private const int TimeoutMs = 15000;

        public MedixaLanClient(string serverIP)
        {
            this.ServerIP = serverIP;
            _baseUrl = string.Format("http://{0}:{1}", serverIP, MedixaLanServer.Port);
            _json = new JavaScriptSerializer();
            _json.MaxJsonLength = int.MaxValue;
        }

        /// <summary>
        /// Tests if the Medixa LAN server is running on the given IP.
        /// Returns null on success, error message on failure.
        /// </summary>
        public string Ping()
        {
            try
            {
                string result = HttpGet("/ping");
                var resp = _json.Deserialize<Dictionary<string, object>>(result);
                return resp.ContainsKey("status") && resp["status"].ToString() == "ok"
                    ? null
                    : "Server returned unexpected response";
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return "Cannot reach server at " + _baseUrl + ". Is Medixa open in SERVER mode there?";
                return "Server error: " + ex.Message;
            }
            catch (Exception ex)
            {
                return "Connection error: " + ex.Message;
            }
        }

        /// <summary>Runs a SELECT query and returns a populated DataTable.</summary>
        public DataTable GetDataTable(string sql, SQLiteParameter[] parameters)
        {
            var body = BuildBody(sql, parameters);
            string json = HttpPost("/query", body);

            var resp = _json.Deserialize<Dictionary<string, object>>(json);
            if (resp.ContainsKey("error"))
                throw new Exception("Server query error: " + resp["error"]);

            return DeserializeDataTable(resp);
        }

        /// <summary>Executes INSERT/UPDATE/DELETE and returns rows affected.</summary>
        public int ExecuteNonQuery(string sql, SQLiteParameter[] parameters)
        {
            var body = BuildBody(sql, parameters);
            string json = HttpPost("/execute", body);
            var resp = _json.Deserialize<Dictionary<string, object>>(json);
            if (resp.ContainsKey("error"))
                throw new Exception("Server execute error: " + resp["error"]);
            return Convert.ToInt32(resp["rowsAffected"]);
        }

        /// <summary>Runs a scalar query returning a single value.</summary>
        public object ExecuteScalar(string sql, SQLiteParameter[] parameters)
        {
            var body = BuildBody(sql, parameters);
            string json = HttpPost("/scalar", body);
            var resp = _json.Deserialize<Dictionary<string, object>>(json);
            if (resp.ContainsKey("error"))
                throw new Exception("Server scalar error: " + resp["error"]);
            return resp.ContainsKey("value") ? resp["value"] : null;
        }

        /// <summary>Runs multiple SQL commands in a single atomic transaction.</summary>
        public bool ExecuteTransaction(List<Tuple<string, SQLiteParameter[]>> commands)
        {
            var cmdList = new List<Dictionary<string, object>>();
            foreach (var cmd in commands)
            {
                cmdList.Add(new Dictionary<string, object>
                {
                    { "sql", cmd.Item1 },
                    { "parameters", ParametersToList(cmd.Item2) }
                });
            }

            var body = new Dictionary<string, object> { { "commands", cmdList } };
            string json = HttpPost("/transaction", body);
            var resp = _json.Deserialize<Dictionary<string, object>>(json);
            if (resp.ContainsKey("error"))
                throw new Exception("Server transaction error: " + resp["error"]);
            return resp.ContainsKey("success") && Convert.ToBoolean(resp["success"]);
        }

        // ── PRIVATE HELPERS ──────────────────────────────────────────────────────

        private Dictionary<string, object> BuildBody(string sql, SQLiteParameter[] parameters)
        {
            return new Dictionary<string, object>
            {
                { "sql", sql },
                { "parameters", ParametersToList(parameters) }
            };
        }

        private List<Dictionary<string, object>> ParametersToList(SQLiteParameter[] parameters)
        {
            var list = new List<Dictionary<string, object>>();
            if (parameters == null) return list;
            foreach (var p in parameters)
            {
                list.Add(new Dictionary<string, object>
                {
                    { "name", p.ParameterName },
                    { "value", (p.Value == null || p.Value is DBNull) ? null : p.Value }
                });
            }
            return list;
        }

        private DataTable DeserializeDataTable(Dictionary<string, object> resp)
        {
            var dt = new DataTable();

            var cols = resp["columns"] as ArrayList;
            if (cols != null)
                foreach (object col in cols)
                    dt.Columns.Add(col.ToString(), typeof(object));

            var rows = resp["rows"] as ArrayList;
            if (rows == null) return dt;

            foreach (Dictionary<string, object> rowDict in rows)
            {
                var dr = dt.NewRow();
                foreach (DataColumn col in dt.Columns)
                    dr[col.ColumnName] = rowDict.ContainsKey(col.ColumnName) && rowDict[col.ColumnName] != null
                        ? rowDict[col.ColumnName]
                        : DBNull.Value;
                dt.Rows.Add(dr);
            }
            return dt;
        }

        private string HttpPost(string endpoint, object body)
        {
            string jsonBody = _json.Serialize(body);
            byte[] data = Encoding.UTF8.GetBytes(jsonBody);

            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.ContentLength = data.Length;
            req.Timeout = TimeoutMs;
            req.ReadWriteTimeout = TimeoutMs;
            req.Headers["X-Medixa-Key"] = SecretKey;

            using (var stream = req.GetRequestStream())
                stream.Write(data, 0, data.Length);

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private string HttpGet(string endpoint)
        {
            var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
            req.Method = "GET";
            req.Timeout = 5000;
            req.Headers["X-Medixa-Key"] = SecretKey;

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return reader.ReadToEnd();
        }
    }
}
