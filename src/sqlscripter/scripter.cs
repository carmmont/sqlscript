using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;

namespace sqlscripter{
class scripter {

    private static bool _do_version = false;
    private static string EXTENDED_PROPERTY_TEMPLATE = @"EXEC sys.sp_addextendedproperty @name=N'$NAME', @value=N'$VALUE' , @level0type=N'SCHEMA',@level0name=N'$SCHEMA', @level1type=N'$TYPE',@level1name=N'$TARGET';" + Environment.NewLine + "GO" + Environment.NewLine;
  
    private static string FILE_VERSION = "FILE VERSION";
    private static string DATABASE_VERSION = "DATABASE VERSION";
    private static string fixdefault(string sql, string defaultrx)
    {
        var m = System.Text.RegularExpressions.Regex.Match(sql, defaultrx);

        string defaultsql = $"ADD CONSTRAINT {m.Groups[2].Value} DEFAULT";

        return sql.Replace("ADD  DEFAULT", defaultsql);
    }

    private static string extended_type(obj_info oi)
    {
        string type = null;

        if ("Table" == oi.type)
        {
            type = "TABLE";
        }

        if ("StoredProcedure" == oi.type)
        {
            type = "PROCEDURE";
        }

        if ("View" == oi.type)
        {
            type = "VIEW";
        }

        if ("Synonym" == oi.type)
        {
           type = "SYNONYM"; 
        }

        if ("UserDefinedFunction" == oi.type)
        {
            type = "FUNCTION";
        }

        if ("UserDefinedType" == oi.type)
        {
            type = "TYPE";
        }

        return type;

    }

    private static string compile_extended_template(string property
        , string value
        , string schema
        , string type
        , string target
    )
    {
        string extended = EXTENDED_PROPERTY_TEMPLATE.Replace("$NAME", property);
       extended = extended.Replace("$VALUE", value);
       extended = extended.Replace("$SCHEMA", schema);
       extended = extended.Replace("$TYPE", type);
       extended = extended.Replace("$TARGET", target);
       
       return extended;
    }

    private static string get_extended_rx(string property)
    {
       string rx = "([^']+)";
       
       string extended = compile_extended_template(property, rx, rx, rx, rx);
              
       return extended;
    }

    private static string get_extended_value(string property, string sql)
    {
        var m = System.Text.RegularExpressions.Regex.Match(sql, get_extended_rx(property));

        if(m.Groups.Count > 1)
            return m.Groups[1].Value;

        return null;
    }

    private static string get_extended_version(string sql)
    {
        return get_extended_value(FILE_VERSION, sql);
    }

    private static string clean_up_version(string sql)
    {
        

        sql = System.Text.RegularExpressions.Regex.Replace(sql, get_extended_rx(FILE_VERSION), "");
        sql = System.Text.RegularExpressions.Regex.Replace(sql, get_extended_rx(DATABASE_VERSION), "");

        return sql;
    }

    private static bool is_version_valid(string version)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(version, @"\d+\.\d+\.\d+\.\d+");
    }
    //IF EXISTS (SELECT value FROM fn_listextendedproperty('FILE VERSION', 'schema', 'dbo', 'TABLE', 'IDX_HISTORY', NULL, NULL))
    private static string get_listextendedproperty(string property, obj_info oi)
    {
       //AGGREGATE, DEFAULT, FUNCTION, LOGICAL FILE NAME, PROCEDURE, QUEUE
        //, RULE, SEQUENCE, SYNONYM, TABLE, TABLE_TYPE, TYPE, VIEW, XML SCHEMA COLLECTION, and NULL.
        //db.ExecuteWithResults

        string type = extended_type(oi);

        
        if(null != type)
        {
            string sql = $"SELECT value FROM fn_listextendedproperty('{property}', 'schema', '{oi.schema}', '{type}', '{oi.name}', NULL, NULL)";
            return sql;
        } 

        return null;
    }
    private static string Script(Scripter scripter
        , SqlSmoObject[] objs
        , string file = null
        , string prefix = ""
        , string version = null
        , obj_info oi = null)
    {
        StringCollection sqls = scripter.Script(objs);

        string sql_return = "";
        
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

            sql = prefix + sql;
            sql += Environment.NewLine + "GO" + Environment.NewLine;

            sql_return = sql_return + sql;
            prefix = "";
        }

        string baseline = "----";
      
        if(null != version)
        {
            if(!is_version_valid(version))
                throw new ScripterException("invalid version " + version);

            if(null == oi)
                throw new ScripterException("version must specify oi");

            if(file != null)
            {                    
                
                if(System.IO.File.Exists(file))
                {
                    baseline = System.IO.File.ReadAllText(file);
                }

                string baseline_clean = clean_up_version(baseline);

                if(baseline_clean != sql_return)
                {
                    string[ ] semver = version.Split(".");
                    semver[2] = (Int16.Parse(semver[2]) + 1).ToString();

                    version = $"{semver[0]}.{semver[1]}.{semver[2]}.0";
                }
                
            }

            string type = extended_type(oi);
            if(null != type)
            {
                string extprop = compile_extended_template(FILE_VERSION, version, oi.schema, type , oi.name);
                       extprop += compile_extended_template(DATABASE_VERSION, "0.0.0.0", oi.schema, type, oi.name);

                if("TABLE" == type)
                {
                    string ext = get_listextendedproperty(FILE_VERSION, oi);

                    /*
                    string drop = extprop.Replace("sys.sp_addextendedproperty", "sys.sp_dropextendedproperty");
                           drop = drop.Replace("GO", "");

                    drop = System.Text.RegularExpressions.Regex.Replace(drop, @"@value=N'(\d+\.){3}\d+' ,", "");
                    */

                    extprop = $"IF NOT EXISTS({ext}){Environment.NewLine}BEGIN{Environment.NewLine}{extprop.Replace("GO", "")}{Environment.NewLine}END{Environment.NewLine}GO{Environment.NewLine}";
                }

                sql_return += extprop;
            }
        }
        

        if (file != null && baseline != sql_return)
        {
            if(System.IO.File.Exists(file))
            {                       
                System.IO.File.Delete(file);
            }

            //System.IO.File.AppendAllText(file, Environment.NewLine + "GO" + Environment.NewLine);
            //System.IO.File.AppendAllText(file, sql);
            System.IO.File.WriteAllText(file, sql_return);
        }
        

        return sql_return;

        
    }
    private static string ScriptDrop(Scripter scripter, SqlSmoObject[] objs)
        {
            scripter.Options.ScriptForCreateDrop = true;
            scripter.Options.ScriptDrops = true;

                    string sql = Script(scripter, objs);
                    
            scripter.Options.IncludeIfNotExists = false;
            scripter.Options.ScriptDrops = false;

            return sql;
        }

    private static void check_oi(SqlSmoObject obj, obj_info oi)
        {
            if(null == obj)
                throw new ScripterException(
                    string.Format("cannot find {0}: {2} {1}", oi.type, oi.name, oi.schema)
                );
        }

    
    private static string get_object_version(Database db, obj_info oi)
    {
        
        string sql = get_listextendedproperty(FILE_VERSION, oi);
        
        if(null != sql)
        {
             DataSet ds = db.ExecuteWithResults(sql);

            if(ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return ds.Tables[0].Rows[0][0].ToString();
            }
        }

        return null;
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

            string prefix = "";

            
            if (null != output)
            {
                file = util.FilePath(output, oi);
            
                //if (System.IO.File.Exists(file))
                    //System.IO.File.Delete(file);
            }

            string version = null;
            if(_do_version)
            {
                version = get_object_version(db, oi);

                if(null == version)
                    version = "0.0.0.0";

                if(null != file && System.IO.File.Exists(file))
                {
                    string sql_file = System.IO.File.ReadAllText(file);
                    string file_version = get_extended_version(sql_file);

                    if( null != file_version && 0 > version.CompareTo(file_version))
                    {
                        version = file_version;
                    }
                }
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
                prefix = ScriptDrop(scripter, objs);
            }

            if ("View" == oi.type)
            {
                objs[0] = db.Views[oi.name, oi.schema];
                check_oi(objs[0], oi);
                prefix = ScriptDrop(scripter, objs);
            }

            if ("Synonym" == oi.type)
            {
                objs[0] = db.Synonyms[oi.name, oi.schema];
                check_oi(objs[0], oi);
                prefix = ScriptDrop(scripter, objs);

            }

            if ("UserDefinedFunction" == oi.type)
            {
                objs[0] = db.UserDefinedFunctions[oi.name, oi.schema];
                check_oi(objs[0], oi);
                prefix = ScriptDrop(scripter, objs);
                
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

            Script(scripter, objs, file, prefix, version, oi);

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