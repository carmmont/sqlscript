using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Globalization;

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
        private static StringDictionary _pluralExceptionsTable =
                new StringDictionary
                {
                       // {"entry", "entries"},
                        {"security", "security"},
                        //{"utility", "utilities"},
                        //{"assembly", "assemblies"},
                        //{"index", "indexes"},
                };  

        public static void AddPluralException (string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
                throw new Exception("Invalid plural exception: key is null or empty");
            if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                throw new Exception("Invalid plural exception: value is null or empty");

            if (_pluralExceptionsTable.ContainsKey(key.ToLower()))
            {
                _pluralExceptionsTable[key] = value;

            } else
            {
                _pluralExceptionsTable.Add(key.Trim(), value.Trim());
            }
        }

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
            string prefix = GetPlural(oi.type);
            if (string.IsNullOrEmpty(prefix))
                prefix = oi.type + "s";

            if (_pluralExceptionsTable != null && _pluralExceptionsTable.Count>0 && _pluralExceptionsTable[oi.type.ToLower()] != null)
                prefix = _pluralExceptionsTable[oi.type.ToLower()];

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

        public static string GetPlural(string NounString)
        {
            NounString = NounString.Trim();

            //see also http://www.lovetolearnplace.com/Grammar/singular&pluralnouns.html#anchor1709890
            //Nouns ending in s, z, x, sh, and ch form the plural by adding - es
            //Nouns ending in - y preceded by a consonant is formed into a plural by changing - y to - ies.  
            //Nouns ending in y preceded by a vowel form their plurals by adding - s.  
            //      Example:  boy, boys; day, days
            //Most nouns ending in o preceded by a consonant is formed into a plural by adding es
            //Some nouns ending in f or fe are made plural by changing f or fe to - ves.  
            //      Example:  beef, beeves; wife, wives

            Regex g = new Regex(@"s\b|z\b|x\b|sh\b|ch\b");
            MatchCollection matches = g.Matches(NounString);
            if (matches.Count > 0)
                NounString += "es"; //Sketches
            else if (NounString.EndsWith("y", true, CultureInfo.InvariantCulture))
            {
                Regex g2 = new Regex(@"(ay|ey|iy|oy|uy)\b");
                if (g2.Matches(NounString).Count <= 0) //e.g. cities 
                    NounString = NounString.Substring(0, NounString.Length - 1) + "ies";
                else
                    NounString += "s";
            }
            else if (NounString.EndsWith("o", true, CultureInfo.InvariantCulture))
            {
                Regex g3 = new Regex(@"(ao|eo|io|oo|uo)\b");
                if (g3.Matches(NounString).Count <= 0) //e.g. heroes 
                    NounString += "es";
                else
                    NounString += "s";
            }
            else if (NounString.EndsWith("f", true, CultureInfo.InvariantCulture) && NounString.Length >= 1)
            {
                NounString = NounString.Substring(0, NounString.Length - 1) + "ves";
            }
            else if (NounString.EndsWith("fe", true, CultureInfo.InvariantCulture) && NounString.Length >= 2)
                NounString = NounString.Substring(0, NounString.Length - 2) + "ves";
            else
                NounString += "s";

            return NounString;
        }
    }

}