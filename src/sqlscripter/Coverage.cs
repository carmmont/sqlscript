using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Collections.Generic;

namespace sqlscripter
{
    public class covdata
    {
        public string query_hash;
        public string statement_text;
        public long execution_count;
        public long total_worker_time; 
        public long total_logical_reads;
        public long total_physical_reads;
        public long total_logical_writes;
        public System.DateTime last_execution_time;
        public System.DateTime creation_time;

        public List<covdata> extra = new List<covdata>();
    }

    public class Result
    {
        public Dictionary<string, covdata> result;

        public double executed = 0;

        public List<string> query_hash;

        public double total { get { return query_hash.Count; }}


    }
    public class Coverage
    {
        XmlDocument _doc;
        
        string _sql;
        string _connection;
        string _db;

        Result _result;


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

            _result = new Result();
            _result.query_hash = new List<string>();

            foreach(XmlNode n in l)
            {
                string hash = n.Attributes.GetNamedItem("QueryHash").Value;
                _result.query_hash.Add(hash);
                System.Console.WriteLine(hash);
            }

            //System.IO.File.WriteAllText("plan.xml", plan);
            
        }

        public Result Execute()
        {

            if(null == _result.query_hash || 0 == _result.query_hash.Count)
                throw new ScripterException("No Compiled Hash. Please Review your Query");

            string sql1 = @"
                --DBCC FREEPROCCACHE
                SELECT 
                query_hash
                , 
                    SUBSTRING(qt.text, (QS.statement_start_offset/2) + 1,
                    ((CASE statement_end_offset 
                        WHEN -1 THEN DATALENGTH(qt.text)
                        WHEN 0  THEN DATALENGTH(qt.text)
                        ELSE QS.statement_end_offset END 
                            - QS.statement_start_offset)/2) + 1)
                AS statement_text
                , execution_count 
                , total_worker_time 
                , total_logical_reads
                , total_physical_reads
                , total_logical_writes
                , qs.last_execution_time
                , creation_time

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

                    cmd.CommandText = _sql;
                    cmd.ExecuteNonQuery();


                    cmd.CommandText = sql; 
                    using(SqlDataReader dr = cmd.ExecuteReader())
                    {
                        
                        while(dr.Read())
                        {
                            string hash = System.BitConverter.ToString(dr.GetSqlBinary(0).Value);

                            hash = hash.Replace("-", "");
                            hash = "0x" + hash;

                            System.Console.WriteLine(hash);

                            covdata c = new covdata();

                            c.query_hash = hash;
                            
                            c.statement_text = dr.GetSqlString(1).ToString();
                            c.execution_count = dr.GetSqlInt64(2).Value;

                            c.total_worker_time = dr.GetSqlInt64(3).Value;
                            c.total_logical_reads = dr.GetSqlInt64(4).Value;
                            c.total_physical_reads = dr.GetSqlInt64(5).Value; 
                            c.total_logical_writes = dr.GetSqlInt64(6).Value;

                            c.last_execution_time = dr.GetSqlDateTime(7).Value;
                            c.creation_time = dr.GetSqlDateTime(8).Value;

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
    }                        

}