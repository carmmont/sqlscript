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
    class obj_info
    {
        public string type;
        public string name;
        public string schema;
        public bool is_type;
    }
    class Program
    {

        private static bool disable_console = false;
        private static bool disable_console_error = false;

        private static void drawTextProgressBar(int progress, int total, string info = "")
        {
            string output = progress.ToString() + " of " + total.ToString() + " " + info;

            int width = 80;

            if (output.Length > width)
            {
                output = output.Substring(0, width);
            }

            if (output.Length < width)
            {
                output = output.PadRight(width);
            }

            if (!disable_console)
            {
                if (!disable_console_error)
                {

                    ConsoleColor original = Console.BackgroundColor;

                    try
                    {
                        //draw empty progress bar
                        Console.CursorLeft = 0;
                        Console.Write("["); //start
                        Console.CursorLeft = 32;
                        Console.Write("]"); //end
                        Console.CursorLeft = 1;
                        float onechunk = 31.0f / total;

                        //draw filled part
                        int position = 1;
                        for (int i = 0; i < onechunk * progress; i++)
                        {
                            Console.BackgroundColor = ConsoleColor.Gray;
                            Console.CursorLeft = position++;
                            Console.Write(" ");
                        }

                        //draw unfilled part
                        for (int i = position; i <= 31; i++)
                        {
                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.CursorLeft = position++;
                            Console.Write(" ");
                        }

                        //draw totals
                        Console.CursorLeft = 35;
                        Console.BackgroundColor = original;



                        Console.Write(output); //blanks at the end remove any excess
                                               //Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
                        if (progress >= total)
                            Console.WriteLine();


                        return;

                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("CONSOLE PROGRESS NOT SUPPORTED: " + ex.ToString());
                        disable_console_error = true;
                    }

                }
                
                System.Console.WriteLine(output);

            }

        }

        static void add_urn_from_query(Database db, string obj, Func<string, string, string> geturn, UrnCollection urns, bool progress = true)
        {
            var dt = DateTime.UtcNow;
            Console.WriteLine("PROCESSING {0}", obj);

            DataSet ds = db.ExecuteWithResults(@"select s.name as [schema], o.name from sys.objects o
                    inner join sys.schemas s
                    on o.schema_id = s.schema_id where type = '" + obj + "'");

            int count = ds.Tables[0].Rows.Count;
            int idx = 0;
           
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                    if(progress)
                        drawTextProgressBar(++idx, count);
                //urns.Add(db.StoredProcedures[r[1].ToString(), r[0].ToString()].Urn);
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
                    drawTextProgressBar(++idx, count);

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

        static obj_info ObjectInfo(string obj)
        {
            obj_info r = null;

            if (string.IsNullOrEmpty(obj))
            {
                r = new obj_info() { type = "null" };
            }

            if (obj.StartsWith("#"))
            {
                r = new obj_info() { type = "comment" };
            }
            
            if(null == r)
            {
                string rx = @"([^:]+):\[([^\]]*)\]\.\[([^\]]+)\]";

                if (!System.Text.RegularExpressions.Regex.IsMatch(obj, rx))
                {
                    throw new ScripterException($"Invalid Object Name used: {obj} does not match {rx}");
                }


                var m = System.Text.RegularExpressions.Regex.Match(obj, rx);

                r = new obj_info()
                {
                    type = m.Groups[1].Value
                    , name = m.Groups[3].Value
                    , schema = m.Groups[2].Value
                    , is_type = true
                };
            }

            return r;
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

        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var commandLineApplication = new CommandLineApplication();
            commandLineApplication.Name = "sqlscripter";
            commandLineApplication.Description = "Sqlscripter";

            var sqlserver = commandLineApplication.Option("-S | --server", "Sql Server", CommandOptionType.SingleValue);
            var sqluser = commandLineApplication.Option("-U | --user", "Sql User", CommandOptionType.SingleValue);
            var sqlpsw = commandLineApplication.Option("-P | --psw", "Sql Password", CommandOptionType.SingleValue);
            var sqldb  = commandLineApplication.Option("-d | --database", "Sql Database", CommandOptionType.SingleValue);
            var nouseprogress = commandLineApplication.Option("--no-progress", "Disable progress bar", CommandOptionType.NoValue);

            commandLineApplication.Command("info", command =>
            {
                    command.Options.AddRange(command.Parent.Options);
                    command.Description = $"{command.Name} render server information";

                    command.OnExecute( () =>
                    {                   
                    
                        ServerConnection serverConnection = new ServerConnection(sqlserver.Value(), sqluser.Value(), sqlpsw.Value());
                        Server server = new Server(serverConnection);

                        System.Console.WriteLine("Databases:");
                        foreach(var db in server.Databases)
                        {
                            System.Console.WriteLine($"\t{db}");
                        }

                    });
                    
            });
            
            commandLineApplication.Command("dbindex", command =>
            {
                command.Options.AddRange(command.Parent.Options);

                command.Description = $"{command.Name} allow to connect to a database and build an ordered index of all objects";

                
                var indexfile = command.Option("-i | --index", "Generate Index File", CommandOptionType.SingleValue);
                var querymode = command.Option("--query-mode", "Use object query for objects", CommandOptionType.NoValue);
                
                command.OnExecute( () =>
                {

                    if(!validate_connection(sqlserver, sqldb, sqluser, sqlpsw))
                    {
                        return 2;
                    }

                    DateTime pinned = DateTime.UtcNow;

                    disable_console = nouseprogress.HasValue();

                    ServerConnection serverConnection = new ServerConnection(sqlserver.Value(), sqluser.Value(), sqlpsw.Value());
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

                    Console.WriteLine("CONNECTED ({0}ms)", DateTime.UtcNow.Subtract(pinned).TotalMilliseconds);
                    pinned = DateTime.UtcNow;

                    //bool display_progress = (!useprogress.HasValue()) && System.Console.h

                    bool fast = querymode.HasValue();

                    Database db = server.Databases[sqldb.Value()];

                    //server.GetSmoObject

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
                        add_urn_from_query(db, "P", (sp, sch) => db.StoredProcedures[sp, sch].Urn, urns, (!nouseprogress.HasValue()));
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

                    Console.WriteLine("DISCOVERING ({0}ms)", DateTime.UtcNow.Subtract(pinned).TotalMilliseconds);
                    pinned = DateTime.UtcNow;

                    //scripter.DiscoveryProgress += Scripter_DiscoveryProgress;
                    DependencyTree tr = scripter.DiscoverDependencies(urns, true);

                    Console.WriteLine("DEPENDENCY ({0}ms)", DateTime.UtcNow.Subtract(pinned).TotalMilliseconds);
                    pinned = DateTime.UtcNow;

                    DependencyCollection dc = scripter.WalkDependencies(tr);

                    string path = indexfile.Value();

                    if (null != path)
                    {
                        if(System.IO.File.Exists(path))
                            System.IO.File.Delete(path);

                        System.IO.File.AppendAllText(path, "#file auto-generated" + Environment.NewLine);
                    }

                    foreach (Microsoft.SqlServer.Management.Sdk.Sfc.Urn urn in preobjects)
                    {
                        UrnToIndex(path, urn);
                    }
                    
                    foreach (DependencyCollectionNode j in dc)
                    {

                        Microsoft.SqlServer.Management.Sdk.Sfc.Urn urn = j.Urn;
                        UrnToIndex(path, urn);
                    }



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

                var target = command.Option("-t | --target", "Sql target Object", CommandOptionType.MultipleValue);
                var output = command.Option("-o | --output", "Script Output", CommandOptionType.SingleValue);
                var file = command.Option("-f | -i | --file", "Input File", CommandOptionType.SingleValue);
                
                command.OnExecute(() =>
                {
                
                    disable_console = nouseprogress.HasValue();

                    if(!validate_connection(sqlserver, sqldb, sqluser, sqlpsw))
                    {
                        return 2;
                    }
                    
                    ServerConnection serverConnection = new ServerConnection(sqlserver.Value(), sqluser.Value(), sqlpsw.Value());
                    Server server = new Server(serverConnection);

                    

                    string[] objs = target.Values.ToArray();

                    if (null != file.Value())
                    {
                        objs = System.IO.File.ReadAllLines(file.Value());
                    }

                    string outputdir = output.Value() ?? "./";
                                           
                    
                   Script(objs, server.Databases[sqldb.Value()], outputdir, (!nouseprogress.HasValue()));

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
                            string indexfilepath = System.IO.Path.GetFullPath(indexfile);

                            System.Console.WriteLine("Adding " + System.IO.Path.GetFileName(indexfile));

                            string[] types = System.IO.File.ReadAllLines(indexfilepath);

                            int types_count = 0;
                            
                            foreach (string tt in types)
                            {
                                obj_info oi = ObjectInfo(tt);

                                drawTextProgressBar(++types_count, types.Length, $" ({tt}) ");

                                if (oi.is_type)
                                {
                                    if (!excludetyes.Values.Contains(oi.type))
                                    {
                                        string source = FilePath(basep, oi, false);
                                        string content = System.IO.File.ReadAllText(source);

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

                command.OnExecute(() =>
                {
                
                    disable_console = nouseprogress.HasValue();

                    ServerConnection serverConnection = new ServerConnection(sqlserver.Value(), sqluser.Value(), sqlpsw.Value());
                    Server server = new Server(serverConnection);

                    Database db = server.Databases[sqldb.Value()];

                    if(free_proccache.HasValue())
                    {
                        db.ExecuteNonQuery("DBCC FREEPROCCACHE");
                    }

                    foreach (string statement in statements.Values)
                    {                        
                        string sql = statement;

                        handle_coverage(db, sql); 
                    }

                    foreach (string indexfile in indexfiles.Values)
                    {
                        string[] lines = System.IO.File.ReadAllLines(indexfile);
                        string sql = string.Join("\r\n", lines);

                        handle_coverage(db, sql);
                        
                    }

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
                Console.Error.WriteLine(ex.ToString());
                return 99;
            }
        }

        private static void handle_coverage(Database db, string sql)
        {
            Coverage coverage = new Coverage();

            coverage.compile(db, sql);

            Result res = coverage.Execute();

            double p = (res.executed / res.total) * 100;

            System.Console.WriteLine("Coverage {0}% executed {1} of {2}", p, res.executed, res.total);
        }
        private static void UrnToIndex(string path, Microsoft.SqlServer.Management.Sdk.Sfc.Urn urn)
        {
            if (null != path)
            {
                System.IO.File.AppendAllText(path, NormalizeUrn(urn) + Environment.NewLine);

            }
            else
            {
                Console.Write(NormalizeUrn(urn));
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

        private static void Script(string[] target, Database db
        , string output, bool progress)
        {
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
                    };

                    scripter.Options = op;

            Script(target, db, scripter, output,progress);
            
        }
        private static void Script(string[] target, Database db
        , Scripter scripter, string output, bool progress)
        {
            SqlSmoObject[] objs = new SqlSmoObject[1];
          
            int count = target.Length;
            int jdx = 0;
            
            
            foreach (string obname in target)
            {
                obj_info oi = ObjectInfo(obname);

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
                    file = FilePath(output, oi);
               
                    if (System.IO.File.Exists(file))
                        System.IO.File.Delete(file);
                }


                if ("Table" == oi.type)
                {
                    scripter.Options.DriDefaults = true;
                    objs[0] = db.Tables[oi.name, oi.schema];
                }

                if ("StoredProcedure" == oi.type)
                {
                    objs[0] = db.StoredProcedures[oi.name, oi.schema];
                                        
                    ScriptDrop(scripter, objs, file);
                }

                if ("View" == oi.type)
                {
                    objs[0] = db.Views[oi.name, oi.schema];
                    
                    ScriptDrop(scripter, objs, file);
                }

                if ("Synonym" == oi.type)
                {
                    objs[0] = db.Synonyms[oi.name, oi.schema];

                }

                if ("UserDefinedFunction" == oi.type)
                {
                    objs[0] = db.UserDefinedFunctions[oi.name, oi.schema];

                    ScriptDrop(scripter, objs, file);
                    
                }

                if ("UserDefinedType" == oi.type)
                {
                    objs[0] = db.UserDefinedTypes[oi.name, oi.schema];

                }

                if ("Schema" == oi.type)
                {
                    objs[0] = db.Schemas[oi.name];
                }
                                

                //DependencyTree tr = scripter.DiscoverDependencies(objs, true);
                //DependencyCollection dc = scripter.WalkDependencies(tr);

                if (null != output && progress)
                {                    
                    drawTextProgressBar(++jdx, count);
                }

                Script(scripter, objs, file);

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

        private static void Script(Scripter scripter, SqlSmoObject[] objs, string file)
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

        private static string fixdefault(string sql, string defaultrx)
        {
            var m = System.Text.RegularExpressions.Regex.Match(sql, defaultrx);

            string defaultsql = $"ADD CONSTRAINT {m.Groups[2].Value} DEFAULT";

            return sql.Replace("ADD  DEFAULT", defaultsql);

        }

        private static string FilePath(string output, obj_info oi, bool dooutput = true)
        {
            
            string prefix = oi.type + "s";

            string dir = System.IO.Path.GetFullPath(
                                        System.IO.Path.Combine(output, prefix)
                                        );

            if (dooutput)
                System.IO.Directory.CreateDirectory(dir);
            

            return System.IO.Path.Combine(dir, $"{oi.schema}.{oi.name}.sql");
            
            //return file;
        }

        
    }
}
