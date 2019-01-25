using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Schema;

namespace sqlscripter
{
    public class cov
    {        
        public string query_hash;
        //public string statement_text;
    }
    public class covdata: cov
    {        
        public long execution_count;
        public long total_worker_time; 
        public long total_logical_reads;
        public long total_physical_reads;
        public long total_logical_writes;
        public System.DateTime last_execution_time;
        public System.DateTime creation_time;
        public long statement_start_offset;
        public long statement_end_offset;
        public string sql_handle;
        public List<covdata> extra = new List<covdata>();
    }

    public class covinput : cov
    {

    }

    public class Result
    {
        public Dictionary<string, covdata> result;

        public Dictionary<string, covinput> input;

        public double executed = 0;

        public List<string> query_hash;

        public double total { get { return query_hash.Count; }}

        public string[] find_not_covered()
        {
            List<string> missing = new List<string>();
            foreach(string hash in query_hash)
            {
                if(null == result[hash])
                {
                    missing.Add(hash);
                }
            }

            return missing.ToArray();
        }

    }
    public class Coverage
    {
        XmlDocument _doc;
        
        string _sql;
        string _connection;
        string _db;
        Result _result;

        private const string PREFIX = "sqlscripter";
        private const string NS = "https://github.com/aseduto/sqlscript";
        public void compile(Database db, string sql)
        {
            _db = db.Name;
            _sql = sql;

            _connection = db.ExecutionManager.ConnectionContext.ConnectionString;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            using(SqlConnection conn = new SqlConnection(_connection))
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandTimeout = 1888;// * cmd.CommandTimeout;

                //System.Console.WriteLine("Timeout {0}", cmd.CommandTimeout);

                cmd.CommandText = $"USE {_db};"; 
                cmd.ExecuteNonQuery();

                cmd.CommandText = "SET SHOWPLAN_XML ON";
                cmd.ExecuteNonQuery();  // turns showplan_xml mode on

                cmd.CommandText = sql; 
                using(SqlDataReader dr = cmd.ExecuteReader())
                {
                    
                    while(dr.Read())
                    {
                        //System.Console.WriteLine(dr.FieldCount);
                        sb.Append(dr.GetSqlString(0).ToString());
                    }
                    
                }

                cmd.CommandText = "SET SHOWPLAN_XML OFF";
                cmd.ExecuteNonQuery();  // turns showplan_xml mode on

                conn.Close();
            }

            string plan = sb.ToString();

            //System.Console.Write(plan);

            _doc = new XmlDocument();
            _doc.LoadXml(plan);

            XmlNodeList l = _doc.SelectNodes("//*[@QueryHash]");

            //.SelectNodes("ancestor::*[@QueryHash]")

            
            _result = new Result();
            _result.query_hash = new List<string>();
            _result.input = new Dictionary<string, covinput>();

            foreach(XmlNode n in l)
            {
                string hash = n.Attributes.GetNamedItem("QueryHash").Value;

                if(!_result.input.ContainsKey(hash))
                {
                    _result.query_hash.Add(hash);
                    
                    System.Console.WriteLine(", {0}", hash);

                    //string statement = n.Attributes.GetNamedItem("StatementText").Value;
                    
                    covinput i = new covinput();
                    i.query_hash = hash;
                    //i.statement_text = statement;

                    _result.input[hash] = i;
                }

            }

#if DEBUG
            System.IO.File.WriteAllText("plan.xml", plan);
#endif
            
        }

        public Result Execute(bool execute)
        {

            if(null == _result.query_hash || 0 == _result.query_hash.Count)
                throw new ScripterException("No Compiled Hash. Please Review your Query");

            string sql1 = @"
                --DBCC FREEPROCCACHE
                SELECT 
                query_hash
                
                , execution_count 
                , total_worker_time 
                , total_logical_reads
                , total_physical_reads
                , total_logical_writes
                , qs.last_execution_time
                , creation_time
                , QS.statement_start_offset
                , QS.statement_end_offset
                , qs.sql_handle

                        FROM    sys.dm_exec_query_stats AS qs
                        CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS qt
                        

                WHERE query_hash in(";

                string sql2 = ")";

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                _result.result = new Dictionary<string, covdata>(_result.query_hash.Count);

                sb.Append(_result.query_hash[0]);
                _result.result[_result.query_hash[0]] = null;

                for(int idx = 1; idx < _result.query_hash.Count; idx++)
                {
                    sb.Append(", ");
                    sb.Append(_result.query_hash[idx]);
                    _result.result[_result.query_hash[idx]] = null;
                }

                string sql = $"{sql1} {sb.ToString()} {sql2}";

                System.Console.WriteLine("-------------");

                using(SqlConnection conn = new SqlConnection(_connection))
                {
                    conn.Open();
                    SqlCommand cmd = conn.CreateCommand();

                    cmd.CommandText = $"USE {_db};"; 
                    cmd.ExecuteNonQuery();

                    if(execute)
                    {
                        cmd.CommandText = _sql;
                        cmd.ExecuteNonQuery();
                    }

                    cmd.CommandText = sql; 
                    using(SqlDataReader dr = cmd.ExecuteReader())
                    {
                        
                        while(dr.Read())
                        {
                            int idx = 0;
                            string hash = System.BitConverter.ToString(dr.GetSqlBinary(idx++).Value);

                            hash = hash.Replace("-", "");
                            hash = "0x" + hash;

                            System.Console.WriteLine(hash);

                            covdata c = new covdata();

                            c.query_hash = hash;
                            
                            //c.statement_text = dr.GetSqlString(1).ToString();
                            c.execution_count = dr.GetSqlInt64(idx++).Value;

                            c.total_worker_time = dr.GetSqlInt64(idx++).Value;
                            c.total_logical_reads = dr.GetSqlInt64(idx++).Value;
                            c.total_physical_reads = dr.GetSqlInt64(idx++).Value; 
                            c.total_logical_writes = dr.GetSqlInt64(idx++).Value;

                            c.last_execution_time = dr.GetSqlDateTime(idx++).Value;
                            c.creation_time = dr.GetSqlDateTime(idx++).Value;

                            c.statement_start_offset = dr.GetSqlInt32(idx++).Value;
                            c.statement_end_offset  = dr.GetSqlInt32(idx++).Value;
                            c.sql_handle = System.BitConverter.ToString(dr.GetSqlBinary(idx++).Value);

                            c.sql_handle = c.sql_handle.Replace("-", "");
                            c.sql_handle = "0x" + c.sql_handle;

                            if(null == _result.result[hash])
                            {
                                _result.result[hash] = c;
                                _result.executed++;
                            }
                            else
                                _result.result[hash].extra.Add(c);

                        }
                        
                    }

                    conn.Close();
                }

           
              return _result;

        }

        private void add_attribute(XmlNode n, string name, string val)
        {
            XmlAttribute att = _doc.CreateAttribute(PREFIX, name, NS);
            att.Value = val;
            n.Attributes.Append(att);
        }
        
        public void Save(string path)
        {

            if(null == _doc)
                throw new ScripterException("no compile available. Invalid doc. Please call compile");

            XmlSchema schema = new XmlSchema();
            schema.Namespaces.Add(PREFIX, NS);

            _doc.Schemas.Add(schema);

            if(null == _result.result)
                throw new ScripterException("no result available. Invalid result. Please call execute");
             
            foreach(string hash in _result.result.Keys)
            {
                covdata cov = _result.result[hash];

                if(null == cov)
                    continue;

                string xpath = $"//*[@QueryHash='{cov.query_hash}']";
                
                XmlNode node = _doc.SelectSingleNode(xpath);

                if(null == node)
                    throw new ScripterException($"Invalid path. [{xpath}]");

                add_attribute(node, "creation_time", XmlConvert.ToString(cov.creation_time, XmlDateTimeSerializationMode.Utc));
                add_attribute(node, "last_execution_time", XmlConvert.ToString(cov.last_execution_time, XmlDateTimeSerializationMode.Utc));

                add_attribute(node, "execution_count", XmlConvert.ToString(cov.execution_count));
                add_attribute(node, "sql_handle", cov.sql_handle);
                add_attribute(node, "statement_end_offset", XmlConvert.ToString(cov.statement_end_offset));
                add_attribute(node, "statement_start_offset", XmlConvert.ToString(cov.statement_start_offset));
                add_attribute(node, "total_logical_reads", XmlConvert.ToString(cov.total_logical_reads));
                add_attribute(node, "total_logical_writes", XmlConvert.ToString(cov.total_logical_writes));
                add_attribute(node, "total_physical_reads", XmlConvert.ToString(cov.total_physical_reads));
                add_attribute(node, "total_worker_time", XmlConvert.ToString(cov.total_worker_time));
                //add_attribute(node, "execution_count", XmlConvert.ToString(cov.execution_count));

                



            }

            _doc.Save(path);
            
            
        }
        
        
    }                        

}