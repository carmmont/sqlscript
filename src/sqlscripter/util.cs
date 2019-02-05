using System;


namespace sqlscripter{

    class obj_info
    {
        public string type;
        public string name;
        public string schema;
        public bool is_type;
    }

    class util{

        public static bool disable_console = false;
        private static bool disable_console_error = false;
        public static void drawTextProgressBar(int progress, int total, string info = "")
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
        public static string FilePath(string output, obj_info oi, bool dooutput = true)
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

        public static obj_info ObjectInfo(string obj)
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
    }

}