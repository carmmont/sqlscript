using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace sqlscripter{
class scripter {

    private static bool _do_version = false;
    private static string fixdefault(string sql, string defaultrx)
    {
        var m = System.Text.RegularExpressions.Regex.Match(sql, defaultrx);

        string defaultsql = $"ADD CONSTRAINT {m.Groups[2].Value} DEFAULT";

        return sql.Replace("ADD  DEFAULT", defaultsql);

    }
        private static void Script(Scripter scripter
            , SqlSmoObject[] objs
            , string file)
        {
            StringCollection sqls = scripter.Script(objs);
            
            for (int idx = 0; idx < sqls.Count; idx++)
            {
                string sql = sqls[idx];

                if(sql == "SET ANSI_NULLS ON" 
                    || sql == "SET QUOTED_IDENTIFIER ON"
                    )
                    continue;

                string defaultrx = @"IF NOT EXISTS \(SELECT \* FROM sys.objects WHERE object_id = OBJECT_ID\(N'\[([^\]]+)\]\.\[([^\]]+)\]'\) AND type = 'D'\)";

                if (System.Text.RegularExpressions.Regex.IsMatch(sql, defaultrx))
                {
                    sql = fixdefault(sql, defaultrx);
                }

                if (file == null)
                {
                    Console.WriteLine("GO");
                    //Console.WriteLine($"------- START {prefix} {idx} [{oi.schema}].[{oi.name}]-------");
                    Console.WriteLine(sql);
                    //Console.WriteLine($"------- END {prefix} {idx} [{oi.schema}].[{oi.name}] -------");
                }
                else
                {
                    System.IO.File.AppendAllText(file, Environment.NewLine + "GO" + Environment.NewLine);
                    System.IO.File.AppendAllText(file, sql);
                }
            }

            
        }
    private static void ScriptDrop(Scripter scripter, SqlSmoObject[] objs, string file)
        {
            scripter.Options.ScriptForCreateDrop = true;
            scripter.Options.ScriptDrops = true;

                    Script(scripter, objs, file);
                    
            scripter.Options.IncludeIfNotExists = false;
            scripter.Options.ScriptDrops = false;
        }

    private static void check_oi(SqlSmoObject obj, obj_info oi)
        {
            if(null == obj)
                throw new ScripterException(
                    string.Format("cannot find {0}: {2} {1}", oi.type, oi.name, oi.schema)
                );
        }
    
    private static void Script(string[] target, Database db
        , Scripter scripter, string output, bool progress)
        {
            SqlSmoObject[] objs = new SqlSmoObject[1];
          
            int count = target.Length;
            int jdx = 0;
                        
            foreach (string obname in target)
            {
                obj_info oi = util.ObjectInfo(obname);

                scripter.Options.IncludeIfNotExists = true;
                scripter.Options.ScriptForCreateDrop = false;
                
                if ("null" == oi.type || "comment" == oi.type)
                {
                    jdx++;
                    continue;
                }

                string file = null;

                if (null != output)
                {
                    file = util.FilePath(output, oi);
               
                    if (System.IO.File.Exists(file))
                        System.IO.File.Delete(file);
                }


                if ("Table" == oi.type)
                {
                    scripter.Options.DriDefaults = true;
                    objs[0] = db.Tables[oi.name, oi.schema];
                    check_oi(objs[0], oi);
                }

                if ("StoredProcedure" == oi.type)
                {
                    objs[0] = db.StoredProcedures[oi.name, oi.schema];
                    check_oi(objs[0], oi);                    
                    ScriptDrop(scripter, objs, file);
                }

                if ("View" == oi.type)
                {
                    objs[0] = db.Views[oi.name, oi.schema];
                    check_oi(objs[0], oi);
                    ScriptDrop(scripter, objs, file);
                }

                if ("Synonym" == oi.type)
                {
                    objs[0] = db.Synonyms[oi.name, oi.schema];
                    check_oi(objs[0], oi);
                    ScriptDrop(scripter, objs, file);

                }

                if ("UserDefinedFunction" == oi.type)
                {
                    objs[0] = db.UserDefinedFunctions[oi.name, oi.schema];
                    check_oi(objs[0], oi);
                    ScriptDrop(scripter, objs, file);
                    
                }

                if ("UserDefinedType" == oi.type)
                {
                    objs[0] = db.UserDefinedTypes[oi.name, oi.schema];
                    check_oi(objs[0], oi);
                }

                if ("Schema" == oi.type)
                {
                    objs[0] = db.Schemas[oi.name];
                    check_oi(objs[0], oi);
                }
                                
                if(null == objs[0])
                {
                    throw new ScripterException(string.Format("Invalid type: {0} {1}", oi.type, obname));
                }
                //DependencyTree tr = scripter.DiscoverDependencies(objs, true);
                //DependencyCollection dc = scripter.WalkDependencies(tr)

                if (null != output && progress)
                {                    
                    util.drawTextProgressBar(++jdx, count, obname);
                }

                Script(scripter, objs, file);

            }

        }

    public static void Script(string[] target, Database db
        , string output, bool progress, SqlServerVersion sql_version
        , bool do_version)//.Version100)
        {
            if(null == db)
                throw new ScripterException("Invalid Database");

            _do_version = do_version;

            Scripter scripter = new Scripter(db.Parent);
                    ScriptingOptions op = new ScriptingOptions
                    {
                        AllowSystemObjects = false
                         ,
                        WithDependencies = false
                        , ClusteredIndexes = true
                        , Indexes = true
                        , DriAllConstraints = true

                        //, 

                        //, DriAll = true
                        , TargetServerVersion = sql_version
                    };

                    System.Console.WriteLine("Target Version {0}", op.TargetServerVersion);

                    scripter.Options = op;

            Script(target, db, scripter, output,progress);
            
        }
    
}
}