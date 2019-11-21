using McMaster.Extensions.CommandLineUtils;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;
using System.Data;

namespace sqlscripter
{
    
    class Program
    {
        static void add_urn_from_query(Database db, string obj
            , Func<string, string, string> geturn
            , UrnCollection urns
            , bool progress = true
            , Func<string, string, bool> validate = null
            )
        {
            var dt = DateTime.UtcNow;
            Console.WriteLine("PROCESSING {0}", obj);

            /*
            select CAST(
 case 
    when sp.is_ms_shipped = 1 then 1
    when (
        select 
            major_id 
        from 
            sys.extended_properties 
        where 
            major_id = sp.object_id and 
            minor_id = 0 and 
            class = 1 and 
            name = N'microsoft_database_tools_support') 
        is not null then 1
    else 0
end          
             AS bit) AS [IsSystemObject],
             * from sys.objects sp 
             where type = 'P'
            */

            DataSet ds = db.ExecuteWithResults(@"select s.name as [schema], o.name from sys.objects o
                    inner join sys.schemas s
                    on o.schema_id = s.schema_id where type = '" + obj + "'");

            int count = ds.Tables[0].Rows.Count;
            int idx = 0;
           
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                if(progress)
                    util.drawTextProgressBar(++idx, count);
                
                bool add = true;

                if(null != validate)
                    add = validate(r[1].ToString(), r[0].ToString());

                //urns.Add(db.StoredProcedures[r[1].ToString(), r[0].ToString()].Urn);
                if(add)
                    urns.Add(geturn(r[1].ToString(), r[0].ToString()));
                        
            }

            Console.WriteLine("PROCESSED {0} {1}ms", obj, DateTime.UtcNow.Subtract(dt).TotalMilliseconds);
        }

        static void add_urns_from_collection(SchemaCollectionBase coll, UrnCollection urns, bool progress = true)
        {
            var dt = DateTime.UtcNow;
            Console.WriteLine("PROCESSING {0}", coll.GetType().Name);

            int count = coll.Count;
            int idx = 0;

            foreach (SqlSmoObject ob in coll)
            {
                if(progress)
                    util.drawTextProgressBar(++idx, count, NormalizeUrn( ob.Urn));
                    
                if (ob is StoredProcedure)
                {
                    if (!((StoredProcedure)ob).IsSystemObject)
                    {
                        urns.Add(ob.Urn);
                    }
                }
                else
                {
                    PropertyInfo isSystem = ob.GetType().GetProperty("IsSystemObject");

                    if (null == isSystem
                        || (!(bool)isSystem.GetValue(ob))
                        )
                    {
                        urns.Add(ob.Urn);
                    }
                }
 
            }

            Console.WriteLine("PROCESSED {0} {1}ms", coll.GetType().Name, DateTime.UtcNow.Subtract(dt).TotalMilliseconds);
        }

        static string NormalizeUrn(string urn)
        {
            //Server[@Name='4f4c6527222b']/Database[@Name='MONITORING']/Table[@Name='Procedures' and @Schema='Gathering']
            //var m = System.Text.RegularExpressions.Regex.Match(urn, @"Server\[@Name='[^']+'\]/Database\[@Name='([^']+)'\]/([^\[]+)\[@Name='([^']+)' and @Schema='([^']+)'\]");

            var m = System.Text.RegularExpressions.Regex.Match(urn, @"Server\[@Name='[^']+'\]/Database\[@Name='([^']+)'\]/([^\[]+)\[@Name='([^']+)'(?: and @Schema='([^']+)'){0,1}\]");
            return $"{m.Groups[2].Value}:[{m.Groups[4].Value}].[{m.Groups[3].Value}]";
        }

        

        static bool validate_connection(CommandOption sqlserver , 
            CommandOption sqldb, CommandOption sqluser, CommandOption  sqlpsw  )
            {
                if(!sqlserver.HasValue())
                {
                    System.Console.Error.WriteLine("Missing Server Name.");
                    return false;
                }
                if(!sqldb.HasValue())
                {
                    System.Console.Error.WriteLine("Missing Database Name.");
                    return false;
                }

                return true;
            }

        static ServerConnection get_server_connection(CommandOption sqlserver , 
            CommandOption sqldb, CommandOption sqluser, CommandOption  sqlpsw  )
            {
                if(!validate_connection(sqlserver, sqldb, sqluser, sqlpsw))
                {
                    return null;
                }

                if(string.IsNullOrEmpty(sqluser.Value()))
                {
                    return new ServerConnection(sqlserver.Value());

                }

                return new ServerConnection(sqlserver.Value(), sqluser.Value(), sqlpsw.Value());
            }

        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var commandLineApplication = new CommandLineApplication();
            commandLineApplication.Name = "sqlscripter";
            commandLineApplication.Description = "Sqlscripter";

            var sqlserver = commandLineApplication.Option("-S | --server", "Sql Server", CommandOptionType.SingleValue);
            var sqluser = commandLineApplication.Option("-U | --user", "Sql User. Do not use in order to switch to integrated authentication.", CommandOptionType.SingleValue);
            var sqlpsw = commandLineApplication.Option("-P | --psw", "Sql Password", CommandOptionType.SingleValue);
            var sqldb  = commandLineApplication.Option("-d | --database", "Sql Database", CommandOptionType.SingleValue);
            var nouseprogress = commandLineApplication.Option("--no-progress", "Disable progress bar", CommandOptionType.NoValue);

            commandLineApplication.Command("info", command =>
            {
                    command.Options.AddRange(command.Parent.Options);
                    command.Description = $"{command.Name} render server information";

                    command.OnExecute( () =>
                    {                   
                    
                        ServerConnection serverConnection = get_server_connection(sqlserver, sqldb, sqluser, sqlpsw);
                        if(null == serverConnection)
                            return 2;

                        Server server = new Server(serverConnection);

                        System.Console.WriteLine("Databases:");
                        foreach(var db in server.Databases)
                        {
                            System.Console.WriteLine($"\t{db}");
                        }

                        return 0;

                    });
                    
            });
            
            commandLineApplication.Command("dbindex", command =>
            {
                command.Options.AddRange(command.Parent.Options);

                command.Description = $"{command.Name} allow to connect to a database and build an ordered index of all objects";
                
                var indexfile = command.Option("-i | --index", "Generate Index File", CommandOptionType.SingleValue);
                var querymode = command.Option("--query-mode", "Use object query for objects", CommandOptionType.NoValue);
                var one_stored = command.Option("--one-stored", "Generate one stored dependency", CommandOptionType.SingleValue);
                
                command.OnExecute( () =>
                {

                    DateTime pinned = DateTime.UtcNow;

                    util.disable_console = nouseprogress.HasValue();

                    ServerConnection serverConnection = get_server_connection(sqlserver, sqldb, sqluser, sqlpsw);
                    if(null == serverConnection)
                        return 2;

                    Server server = new Server(serverConnection);

                    Scripter scripter = new Scripter(server);
                    ScriptingOptions op = new ScriptingOptions
                    {
                         AllowSystemObjects = false
                         , WithDependencies = true

                    };

                    scripter.Options = op;

                    UrnCollection urns = new UrnCollection();
                    List<Microsoft.SqlServer.Management.Sdk.Sfc.Urn> preobjects = new List<Microsoft.SqlServer.Management.Sdk.Sfc.Urn>();

                    Console.WriteLine("CONNECTED ({0})", DateTime.UtcNow.Subtract(pinned));
                    pinned = DateTime.UtcNow;

                    //bool display_progress = (!useprogress.HasValue()) && System.Console.h

                    bool fast = querymode.HasValue();

                    Database db = server.Databases[sqldb.Value()];

                    //add all or just one sp
                    if(one_stored.HasValue())
                    {
                        var sp = db.StoredProcedures[one_stored.Value()];
                        urns.Add(sp.Urn);
                    }
                    else
                    {

                        SchemaCollection sc = db.Schemas;

                        foreach (Schema schema in sc)
                        {
                            if (!schema.IsSystemObject)
                            {
                                preobjects.Add(schema.Urn);
                            }
                        }                                            
                        
                        TableCollection tc = db.Tables;

                        add_urns_from_collection(tc, urns, (!nouseprogress.HasValue()));


                        if (fast)
                        {
                            add_urn_from_query(db, "P", (sp, sch) => db.StoredProcedures[sp, sch].Urn, urns, (!nouseprogress.HasValue())
                            , (sp, sch) => !db.StoredProcedures[sp, sch].IsSystemObject );
                        }
                        else
                        {

                            var sp = server.Databases[sqldb.Value()].StoredProcedures;
                            add_urns_from_collection(sp, urns);
                        }

                        //--------------------------------


                        if (fast)
                        {
                            add_urn_from_query(db, "V", (sp, sch) => db.Views[sp, sch].Urn, urns, (!nouseprogress.HasValue()));
                        }
                        else
                        {

                            var vs = server.Databases[sqldb.Value()].Views;

                            add_urns_from_collection(vs, urns);
                        }
                        
                        var ss = server.Databases[sqldb.Value()].Synonyms;

                        add_urns_from_collection(ss, urns);

                        if (fast)
                        {
                            add_urn_from_query(db, "IF", (sp, sch) => db.UserDefinedFunctions[sp, sch].Urn, urns, (!nouseprogress.HasValue()));
                        }
                        else
                        {

                            var ff = server.Databases[sqldb.Value()].UserDefinedFunctions;

                            add_urns_from_collection(ff, urns);
                        }

                        var tt = server.Databases[sqldb.Value()].UserDefinedTypes;

                        add_urns_from_collection(tt, urns);
                    } 

                    Console.WriteLine("DISCOVERING ({0})", DateTime.UtcNow.Subtract(pinned));
                    pinned = DateTime.UtcNow;

                    //scripter.DiscoveryProgress += Scripter_DiscoveryProgress;
                    DependencyTree tr = scripter.DiscoverDependencies(urns, true);

                    Console.WriteLine("DEPENDENCY ({0})", DateTime.UtcNow.Subtract(pinned));
                    pinned = DateTime.UtcNow;

                    DependencyCollection dc = scripter.WalkDependencies(tr);

                    Console.WriteLine("WALKED ({0})", DateTime.UtcNow.Subtract(pinned));
                    pinned = DateTime.UtcNow;

                    dependency_index index = dependency.index(tr);

                    Console.WriteLine("INDEXED ({0})", DateTime.UtcNow.Subtract(pinned));
                    pinned = DateTime.UtcNow;

                    string path = indexfile.Value();

                    if (null != path)
                    {
                        if(System.IO.File.Exists(path))
                            System.IO.File.Delete(path);

                        System.IO.File.AppendAllText(path, "#file auto-generated" + Environment.NewLine);
                    }

                    foreach (Microsoft.SqlServer.Management.Sdk.Sfc.Urn urn in preobjects)
                    {
                        UrnToIndex(db.Name, path, urn, index);
                    }
                    
                    foreach (DependencyCollectionNode j in dc)
                    {
                        
                        Microsoft.SqlServer.Management.Sdk.Sfc.Urn urn = j.Urn;
                        UrnToIndex(db.Name, path, urn, index);
                       
                    }

                    Console.WriteLine("EXPORTED ({0})", DateTime.UtcNow.Subtract(pinned));
                    

                    return 0;
                });
            });

            commandLineApplication.Command("urn", command =>
            {
                var urn = command.Option("-u | --urn", "Sql Urn", CommandOptionType.SingleValue);
                command.Description = @"Normalize an Input. 
                From Server[@Name='4f4c6527222b']/Database[@Name='MONITORING']/Table[@Name='Procedures' and @Schema='Gathering'] 
                to Table:[Gathering].[Procedures]";

                command.OnExecute(() => {
                    Console.WriteLine(NormalizeUrn(urn.Value()));
                    return 0;
                });
                
            });

            commandLineApplication.Command("script", command =>
            {
                command.Options.AddRange(command.Parent.Options);

                command.Description = $"{command.Name} allows to script objects listed in a file or in the command line";

                var target = command.Option("-t | --target", "Sql target Object. For instance Table:[dbo].[Table_1]", CommandOptionType.MultipleValue);
                var output = command.Option("-o | --output", "Scripts Directory Output", CommandOptionType.SingleValue);
                var file = command.Option("-f | -i | --file", "Input File", CommandOptionType.SingleValue);
                var version = command.Option("--sql-version", "Sql Version Generation Target", CommandOptionType.SingleValue); 
                var file_version = command.Option("--file-version", "Enable object version support", CommandOptionType.NoValue);
                var modified = command.Option("--modified", "Export all object modified in the last <input> minutes. Es 1440 last day", CommandOptionType.SingleValue);
                
                command.OnExecute(() =>
                {
                
                    util.disable_console = nouseprogress.HasValue();

                    ServerConnection serverConnection = get_server_connection(sqlserver, sqldb, sqluser, sqlpsw);
                    if(null == serverConnection)
                        return 2;
                    
                    Server server = new Server(serverConnection);

                    Database db = server.Databases[sqldb.Value()];
                   
                    //TODO: ALLOW MULTIPLE TARGETS AND MULTIPLE FILES
                    List<string> objs = new List<string>();
                    objs.AddRange(target.Values.ToArray());

                    if (null != file.Value())
                    {
                        string [] lines = System.IO.File.ReadAllLines(file.Value());
                        objs.AddRange(lines);
                          
                    }

                    if(modified.HasValue())
                    {
                        int minutes = int.Parse(modified.Value());
                        string [] mods = exporter.get_modified_objects(db, minutes);

                        foreach(string obj in mods)
                            Console.WriteLine(string.Format("\t\tMODIFIED:\t{0}", obj));

                        objs.AddRange(mods);
                    }

                    string outputdir = output.Value() ?? "./";
                
                   SqlServerVersion sql_version = SqlServerVersion.Version100;

                   if(version.HasValue())
                   {
                       sql_version = (SqlServerVersion) Enum.Parse(typeof(SqlServerVersion), version.Value());
                   }
                    
                   scripter.Script(objs.ToArray(), db
                   , outputdir, (!nouseprogress.HasValue())
                   , sql_version
                   , file_version.HasValue());

                   return 0;
                    
                });

                //scripter.Script(

            });

            commandLineApplication.Command("build", command => 
            {    
                
                command.Options.AddRange(command.Parent.Options);

                var indexfiles = command.Option("-i | --index", "Input Index File", CommandOptionType.MultipleValue);
                var excludetyes = command.Option("-x | --exclude-types", "Types to exclude from the index", CommandOptionType.MultipleValue);
                var output = command.Option("-o | --output", "Script Build Output", CommandOptionType.SingleValue);
                var basepath = command.Option("-b | --basepath", "Root of files referenced by index", CommandOptionType.SingleValue);
                var database_version = command.Option("--database-version", "Insert database version in script with object version", CommandOptionType.SingleValue);
                    
                    command.OnExecute(()=>
                    {
                        string outputfile = output.Value();
                        if (null != outputfile)
                        {
                            if (System.IO.File.Exists(outputfile))
                                System.IO.File.Delete(outputfile);
                        }

                        //ProcessDirs(pretypes.Values.ToArray(), outputfile);

                        string basep = basepath.Value();
                        string main_index = indexfiles.Values[0];
                        
                        if(null == basep)
                            basep = System.IO.Path.GetDirectoryName(main_index);

                        foreach (string indexfile in indexfiles.Values)
                        {
                            string indexfilepath = System.IO.Path.GetFullPath(System.IO.Path.Join(basep, indexfile));
                            if(!System.IO.File.Exists(indexfilepath))
                                indexfilepath = System.IO.Path.GetFullPath(indexfile);


                            System.Console.WriteLine("Adding " + System.IO.Path.GetFileName(indexfile));

                            string[] types = System.IO.File.ReadAllLines(indexfilepath);

                            int types_count = 0;
                            
                            foreach (string tt in types)
                            {
                                obj_info oi = util.ObjectInfo(tt);

                                util.drawTextProgressBar(++types_count, types.Length, $" ({tt}) ");

                                if (oi.is_type)
                                {
                                    if (!excludetyes.Values.Contains(oi.type))
                                    {
                                        string source = util.FilePath(basep, oi, false);
                                        string content = System.IO.File.ReadAllText(source);

                                        if(database_version.HasValue())
                                            content = scripter.insert_database_version(content, database_version.Value());

                                        if (null != outputfile)
                                        {
                                            System.IO.File.AppendAllText(outputfile, content);
                                        }
                                        else
                                            Console.Write(content);
                                    }
                                }

                            }
                        }

                        //ProcessDirs(posttypes.Values.ToArray(), outputfile);
                    });
            });

            commandLineApplication.Command("coverage", command => 
            {    
                
                command.Options.AddRange(command.Parent.Options);
                command.Description = @"Run sql stetament from files or command line and track coverage";

                var indexfiles = command.Option("-i | --input", "Input Coverage File", CommandOptionType.MultipleValue);
                var statements = command.Option("-s | --statement", "Input Coverage Statement", CommandOptionType.MultipleValue);
                var free_proccache = command.Option("-f | --free-proccache", @"Run DBCC FREEPROCCACHE before your test in order
                 to count only what you are running and not previous runs.
                 Do Not use in a production system.", CommandOptionType.NoValue);
                var no_exec = command.Option("-n | --no-exec", @"Do not Run the procedure.", CommandOptionType.NoValue);
                var datail = command.Option("--detail", @"Provide the list of not covered query_hash", CommandOptionType.NoValue);
                var save = command.Option("--save", @"save a test result with performance and coverage", CommandOptionType.SingleValue);
                command.OnExecute(() =>
                {
                
                    util.disable_console = nouseprogress.HasValue();

                    ServerConnection serverConnection = get_server_connection(sqlserver, sqldb, sqluser, sqlpsw);
                    if(null == serverConnection)
                        return 2;
                    
                    Server server = new Server(serverConnection);

                    Database db = server.Databases[sqldb.Value()];
                    if(null == db)
                        throw new ScripterException("Invalid database");

                    string save_path = null;

                    if(save.HasValue())
                        save_path = save.Value();

                    if(free_proccache.HasValue())
                    {
                        db.ExecuteNonQuery("DBCC FREEPROCCACHE");
                    }

                    foreach (string statement in statements.Values)
                    {                        
                        string sql = statement;

                        handle_coverage(db, sql, !no_exec.HasValue(), datail.HasValue(), save_path); 
                    }

                    foreach (string indexfile in indexfiles.Values)
                    {
                        string[] lines = System.IO.File.ReadAllLines(indexfile);
                        string sql = string.Join("\r\n", lines);

                        handle_coverage(db, sql, !no_exec.HasValue(), datail.HasValue(), save_path);
                        
                    }

                    return 0;

                });

            
            });

            commandLineApplication.Command("template", command =>
            {

                command.Options.AddRange(command.Parent.Options);
                command.Description = @"Run sql statement from files or command line and track coverage";

                //var indexfiles = command.Option("-i | --input", "Input Coverage File", CommandOptionType.MultipleValue);
                //var statements = command.Option("-s | --statement", "Input Coverage Statement", CommandOptionType.MultipleValue);
                //var free_proccache = command.Option("-f | --free-proccache", @"Run DBCC FREEPROCCACHE before your test in order
                // to count only what you are running and not previous runs.
                // Do Not use in a production system.", CommandOptionType.NoValue);
                //var no_exec = command.Option("-n | --no-exec", @"Do not Run the procedure.", CommandOptionType.NoValue);
                //var datail = command.Option("--detail", @"Provide the list of not covered query_hash", CommandOptionType.NoValue);
                //var save = command.Option("--save", @"save a test result with performance and coverage", CommandOptionType.SingleValue);

                var table = command.Option("-t | --table", "the table to genarate CRUD", CommandOptionType.MultipleValue);
                var output = command.Option("-o | --output", "Scripts Directory Output", CommandOptionType.SingleValue);
                //var file = command.Option("-f | -i | --file", "Input File", CommandOptionType.SingleValue);

                command.OnExecute(() =>
                {
                    //string outputdir = output.Value() ?? "./StoredProcedures";

                    util.disable_console = nouseprogress.HasValue();

                    ServerConnection serverConnection = get_server_connection(sqlserver, sqldb, sqluser, sqlpsw);
                    if (null == serverConnection)
                        return 2;

                    Server server = new Server(serverConnection);

                    Database db = server.Databases[sqldb.Value()];
                    if (null == db)
                        throw new ScripterException("Invalid database");

                    foreach (string t in table.Values)
                    {
                        Table db_table;

                        if (!t.Contains ("."))
                        {
                            db_table = db.Tables[t];
                        } else
                        {
                            string a = t.Split('.')[0];
                            string b = t.Split('.')[1];
                            db_table = db.Tables[b, a];
                        }
                        
                        Template temp = new Template();

                        temp.Table = db_table;

                        //Console.Write(temp.Execute());

                        StringCollection sc = temp.Execute();

                        foreach (string s in sc)
                        {
                            Console.Write(s);

                            db.ExecuteNonQuery(s);
                        }
                    }

                    return 0;

                });
            });

            commandLineApplication.HelpOption("-h | --help", inherited: true);

            try
            {

                int r = commandLineApplication.Execute(args);

                return r;

            }
            catch(CommandParsingException ex)
            {
                Console.Error.Write("Invalid Command Line: ");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(commandLineApplication.GetHelpText());
                return 22;
            }
            catch (Exception ex)
            {
                ConsoleColor color = Console.ForegroundColor;
                try{
                    
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Error.WriteLine(ex.ToString());
                }
                finally
                {
                    Console.ForegroundColor = color;
                }
                
                return 99;
            }
        }

        private static void handle_coverage(Database db, string sql, bool exec, bool detail = false, string save_path = null)
        {
            Coverage coverage = new Coverage();

            coverage.compile(db, sql);

            Result res = coverage.Execute(exec);

            double p = (res.executed / res.total) * 100;

            System.Console.WriteLine("Coverage {0}% executed {1} of {2}", p.ToString("0.00"), res.executed, res.total);
            
            if(detail)
            {
                string[] missing = res.find_not_covered();

                foreach(string missed in missing)
                {
                    System.Console.WriteLine("\t, {0} ", missed);
                }
            }

            if(null != save_path)
            {
                coverage.Save(save_path);
            }           
            
        }

        //private static 

        private static void UrnToIndex(string target_db
        , string path
        , Microsoft.SqlServer.Management.Sdk.Sfc.Urn urn
        , dependency_index index)
        {
            string regex = $"Database\\[@Name='{target_db}";

            bool trouble = false;
            string output_line = "";

            if(!System.Text.RegularExpressions.Regex.IsMatch(urn.Value, regex
                , System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                output_line = string.Format("##External-Entity-Database\t{0}", urn.Value);
                trouble = true;
            }
            else
            {
                string str_info = NormalizeUrn(urn);
                obj_info info = util.ObjectInfo(str_info);
                output_line = str_info;

                if("UnresolvedEntity" == info.type)
                {
                    output_line = string.Format("##UnresolvedEntity\t{0}\t{1}", urn, str_info);
                    trouble = true;
                }
            }

            if(trouble)
            {
                string[] parents = index.get_parents(urn.Value);
                if(null == parents)
                {
                    output_line += "\t->NO-PARENTS";
                }
                else
                {
                    output_line = string.Format("{1}\t->PARENTS:\t{0}", string.Join('\t', parents), output_line); 
                }
            }
            
            if (null != path)
            {
                System.IO.File.AppendAllText(path, output_line + Environment.NewLine);
            }
            else
            {
                Console.WriteLine(output_line);
            }
        }

        /*
        private static void ProcessDirs(string[] pretypes, string outputfile)
        {
            foreach (string dir in pretypes)
            {
                string[] files = System.IO.Directory.GetFiles(dir, "*.sql");

                foreach (string file in files)
                {
                    string source = System.IO.Path.Combine(dir, file);
                    string content = System.IO.File.ReadAllText(source);

                    if (null != outputfile)
                        System.IO.File.AppendAllText(outputfile, content);
                    else
                        Console.Write(content);
                }

            }
        }
        */

        

        

        

        

        

        

        
    }
}
