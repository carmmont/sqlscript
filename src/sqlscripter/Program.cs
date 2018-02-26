using McMaster.Extensions.CommandLineUtils;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace sqlscripter
{
    struct obj_info
    {
        public string type;
        public string name;
        public string schema;
        public bool is_type;
    }
    class Program
    {
        private static void drawTextProgressBar(int progress, int total)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31 ; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
        }
        static void add_urns_from_collection(SchemaCollectionBase coll, UrnCollection urns)
        {
            Console.WriteLine("PROCESSING {0}", coll.GetType().Name);

            int count = coll.Count;
            int idx = 1;

            foreach (SqlSmoObject ob in coll)
            {
                drawTextProgressBar(idx++, count);

                PropertyInfo isSystem = ob.GetType().GetProperty("IsSystemObject");

                if (null == isSystem
                    || (!(bool)isSystem.GetValue(ob))
                    )
                {
                    urns.Add(ob.Urn);
                }
 
            }

            Console.WriteLine("PROCESSED {0}", coll.GetType().Name);
        }

        static string NormalizeUrn(string urn)
        {
            //Server[@Name='4f4c6527222b']/Database[@Name='MONITORING']/Table[@Name='Procedures' and @Schema='Gathering']
            var m = System.Text.RegularExpressions.Regex.Match(urn, @"Server\[@Name='[^']+'\]/Database\[@Name='([^']+)'\]/([^\[]+)\[@Name='([^']+)' and @Schema='([^']+)'\]");

            return $"{m.Groups[2].Value}:[{m.Groups[4].Value}].[{m.Groups[3].Value}]";
        }

        static obj_info ObjectInfo(string obj)
        {
            if (string.IsNullOrEmpty(obj))
            {
                return new obj_info() { type = "null" };
            }

            if (obj.StartsWith("#"))
            {
                return new obj_info() { type = "comment" };
            }

            var m = System.Text.RegularExpressions.Regex.Match(obj, @"([^:]+):\[([^\]]+)\]\.\[([^\]]+)\]");

            var r = new obj_info()
            {
                type = m.Groups[1].Value
                , name = m.Groups[3].Value
                , schema = m.Groups[2].Value
                , is_type = true
            };

            return r;
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
            
            
            commandLineApplication.Command("dbindex", command =>
            {
                command.Options.AddRange(command.Parent.Options);

                var indexfile = command.Option("-i | --index", "Generate Index File", CommandOptionType.SingleValue);

                command.OnExecute( () =>
                {                   

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

                    Console.WriteLine("CONNECTED");

                    //server.GetSmoObject
                     
                    TableCollection tc = server.Databases[sqldb.Value()].Tables;

                    add_urns_from_collection(tc, urns);            

                    var sp = server.Databases[sqldb.Value()].StoredProcedures;
                                        
                    //StoredProcedure s;
                    //s.IsSystemObject

                    add_urns_from_collection(sp, urns);     
                     
                    var vs = server.Databases[sqldb.Value()].Views;

                    add_urns_from_collection(vs, urns);    
                    
                    var ss = server.Databases[sqldb.Value()].Synonyms;

                    add_urns_from_collection(ss, urns); 

                    var ff = server.Databases[sqldb.Value()].UserDefinedFunctions;

                    add_urns_from_collection(ff, urns); 

                    var tt = server.Databases[sqldb.Value()].UserDefinedTypes;

                    add_urns_from_collection(tt, urns); 

                    Console.WriteLine("DISCOVERING");

                    //scripter.DiscoveryProgress += Scripter_DiscoveryProgress;
                    DependencyTree tr = scripter.DiscoverDependencies(urns, true);

                    Console.WriteLine("DEPENDENCY");

                    DependencyCollection dc = scripter.WalkDependencies(tr);

                    string path = indexfile.Value();
                    
                    foreach (DependencyCollectionNode j in dc)
                    {
                        //Console.Write("\ttree\t");
                        //    Console.Write(j.Urn);
                        //        Console.Write("\t");
                        //            Console.WriteLine(j.IsRootNode);

                        if (null != path)
                        {
                            System.IO.File.AppendAllText(path, NormalizeUrn(j.Urn));
                        }
                        else
                        {
                            Console.Write(NormalizeUrn(j.Urn));
                        }
                    }

                    //scripter.Script(

                    return 0;
                });
            });

            commandLineApplication.Command("urn", command =>
            {
                var urn = command.Option("-u | --urn", "Sql Urn", CommandOptionType.SingleValue);

                command.OnExecute(() => {
                    Console.WriteLine(NormalizeUrn(urn.Value()));
                    return 0;
                });
                
            });

            commandLineApplication.Command("script", command =>
            {
                command.Options.AddRange(command.Parent.Options);

                var target = command.Option("-t | --target", "Sql target Object", CommandOptionType.MultipleValue);
                var output = command.Option("-o | --output", "Script Output", CommandOptionType.SingleValue);
                var file = command.Option("-f | -i | --file", "Input File", CommandOptionType.SingleValue);
                
                command.OnExecute(() =>
                {
                
                    ServerConnection serverConnection = new ServerConnection(sqlserver.Value(), sqluser.Value(), sqlpsw.Value());
                    Server server = new Server(serverConnection);

                    Scripter scripter = new Scripter(server);
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

                    string[] objs = target.Values.ToArray();

                    if (null != file.Value())
                    {
                        objs = System.IO.File.ReadAllLines(file.Value());
                    }

                    string outputdir = output.Value() ?? "./";
                                           
                                        
                   Script(sqldb, objs, server, scripter, outputdir);

                    
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

                            string[] types = System.IO.File.ReadAllLines(indexfilepath);
                            
                            foreach (string tt in types)
                            {
                                obj_info oi = ObjectInfo(tt);

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

            commandLineApplication.HelpOption("-h | --help");

            try
            {

                int r = commandLineApplication.Execute(args);

                return r;

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 99;
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

        private static void Script(CommandOption sqldb, string[] target, Server server
        , Scripter scripter, string output)
        {
            SqlSmoObject[] objs = new SqlSmoObject[1];
          

            int count = target.Length;
            int jdx = 1;
            
            foreach (string obname in target)
            {
            
                obj_info oi = ObjectInfo(obname);

                scripter.Options.IncludeIfNotExists = true;
                scripter.Options.ScriptForCreateDrop = false;
                

                if ("Table" == oi.type)
                {
                    objs[0] = server.Databases[sqldb.Value()].Tables[oi.name, oi.schema];
                    

                }

                if ("StoredProcedure" == oi.type)
                {
                    objs[0] = server.Databases[sqldb.Value()].StoredProcedures[oi.name, oi.schema];
                    

                    scripter.Options.IncludeIfNotExists = false;
                    scripter.Options.ScriptForCreateDrop = true;
                }

                if ("View" == oi.type)
                {
                    objs[0] = server.Databases[sqldb.Value()].Views[oi.name, oi.schema];
                    

                    scripter.Options.IncludeIfNotExists = false;
                    scripter.Options.ScriptForCreateDrop = true;
                }

                if ("Synonym" == oi.type)
                {
                   objs[0] = server.Databases[sqldb.Value()].Synonyms[oi.name, oi.schema];
                    
                }

                if ("UserDefinedFunction" == oi.type)
                {
                    objs[0] = server.Databases[sqldb.Value()].UserDefinedFunctions[oi.name, oi.schema];
                    

                    scripter.Options.IncludeIfNotExists = false;
                    scripter.Options.ScriptForCreateDrop = true;
                }

                if ("UserDefinedType" == oi.type)
                {
                    objs[0] = server.Databases[sqldb.Value()].UserDefinedTypes[oi.name, oi.schema];
                    
                }

                //DependencyTree tr = scripter.DiscoverDependencies(objs, true);
                //DependencyCollection dc = scripter.WalkDependencies(tr);

                StringCollection sqls = scripter.Script(objs);

                string file = null;

                if (null != output)
                {
                    file = FilePath(output, oi);
                    drawTextProgressBar(jdx++, count);

                    if(System.IO.File.Exists(file))
                        System.IO.File.Delete(file);
                }

                for (int idx = 0; idx < sqls.Count; idx++)
                {
                    string sql = sqls[idx];

                    if (file == output)
                    {
                        Console.WriteLine("GO");
                        //Console.WriteLine($"------- START {prefix} {idx} [{oi.schema}].[{oi.name}]-------");
                        Console.WriteLine(sql);
                        //Console.WriteLine($"------- END {prefix} {idx} [{oi.schema}].[{oi.name}] -------");
                    }
                    else
                    {
                        System.IO.File.AppendAllText(file,  Environment.NewLine +  "GO" + Environment.NewLine);
                        System.IO.File.AppendAllText(file, sql);
                    }
                }

            }

        }

        private static string FilePath(string output, obj_info oi, bool dooutput = true)
        {
            string prefix = oi.type + "s";

            string dir = System.IO.Path.GetFullPath(
                                        System.IO.Path.Combine(output, prefix)
                                        );

            if(dooutput)
                System.IO.Directory.CreateDirectory(dir);

            string file = System.IO.Path.Combine(dir, $"{oi.schema}.{oi.name}.sql");
            
            return file;
        }

        
    }
}
