using Microsoft.SqlServer.Management.Smo;
using System.Data;
using System.Collections.Generic;

/*




 */

 namespace sqlscripter{
     public class exporter
     {
         public static string [] get_modified_objects(Database db, int minutes)
         {
             string sql = @"SELECT 
                CASE [type]
                    WHEN 'P' THEN 'StoredProcedure'
                    WHEN 'U' THEN 'Table'
                    WHEN 'IF' THEN 'UserDefinedFunction'
                    WHEN 'FN' THEN 'UserDefinedFunction'
                    ELSE [type_desc]
                END As [TYPE]
                ,    

                '[' + S.Name + '].[' + O.Name + ']'
                    AS Name,
            create_date, modify_date
            , DATEDIFF(n, O.modify_date, GETDATE()) As Min
            , type, type_desc

            FROM sys.objects O INNER JOIN sys.schemas S on S.schema_id = O.schema_id
            WHERE type NOT IN('IT', 'S', 'D', 'PK', 'SQ')
            AND DATEDIFF(n, O.modify_date, GETDATE()) <= $N
            ORDER BY O.modify_date desc
            ";

            sql = sql.Replace("$N", minutes.ToString());

            //System.Console.WriteLine(sql);

            DataSet ds = db.ExecuteWithResults(sql);

            List<string> objs = new List<string>();

            foreach (DataRow r in ds.Tables[0].Rows)
            {
                string obj = r[0].ToString() + ":" + r[1].ToString();
               // System.Console.WriteLine(obj);
                objs.Add(obj);
            }

            return objs.ToArray();
         }
     }
 }