using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace sqlscripter
{
    class Template
    {
        private Table _table;

        public Table Table { get => _table; set => _table = value; }

        public StringCollection Execute()
        {
           StringCollection pk_script = new StringCollection();
            //try
            //{
            //    var options = new JsonSerializerSettings
            //    {
                     
            //        MaxDepth = 1000
            //    };

            //    string p = Newtonsoft.Json.JsonConvert.SerializeObject(_table.Columns, options);

            //    Console.Write("");
            //}

            //catch(Exception e)
            //{
            //    Console.Write(e.StackTrace);
            //}


            string output1 = $"IF EXISTS(SELECT* FROM sys.objects WHERE object_id = OBJECT_ID(N'[{Table.Schema.ToString()}].[{Table.Name}_INSERT]') AND type in (N'P', N'PC'))" +
                $"\n\r\t\tDROP PROCEDURE [{Table.Schema.ToString()}].[{Table.Name}_INSERT];\n\rGO" +
                $"\n\rCREATE PROCEDURE [{Table.Schema.ToString()}].[{Table.Name}_INSERT]";

            string output2 = $"IF EXISTS(SELECT* FROM sys.objects WHERE object_id = OBJECT_ID(N'[{Table.Schema.ToString()}].[{Table.Name}_UPDATE]') AND type in (N'P', N'PC'))" +
                $"\n\r\t\tDROP PROCEDURE [{Table.Schema.ToString()}].[{Table.Name}_UPDATE];\n\rGO" +
                $"\n\rCREATE PROCEDURE [{Table.Schema.ToString()}].[{Table.Name}_UPDATE]";

            string output3 = $"IF EXISTS(SELECT* FROM sys.objects WHERE object_id = OBJECT_ID(N'[{Table.Schema.ToString()}].[{Table.Name}_DELETE]') AND type in (N'P', N'PC'))" +
                $"\n\r\t\tDROP PROCEDURE [{Table.Schema.ToString()}].[{Table.Name}_DELETE];\n\rGO" +
                $"\n\rCREATE PROCEDURE [{Table.Schema.ToString()}].[{Table.Name}_DELETE]";

            StringBuilder sb0 = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            StringBuilder sb3 = new StringBuilder();
            StringBuilder sb4 = new StringBuilder();
            StringBuilder sb5 = new StringBuilder();

            StringBuilder sb6 = new StringBuilder();
            
            foreach (Column c in _table.Columns)
            {
                if (!c.Identity)
                {
                    sb0.Append(string.Concat(sb0.Length > 0 ? ", " : "", $"\n\r\t@{c.Name.Replace(" ", "_")} ", GetDataType(c)));

                    sb1.Append(string.Concat(sb1.Length == 0 ? "(" : ",", $"\n\r\t[{c.Name}]"));

                    sb2.Append(string.Concat(sb2.Length == 0 ? "VALUES (" : ",", $"\n\r\t@{c.Name.Replace(" ", "_")}"));
                }

                sb3.Append(string.Concat(sb3.Length > 0 ? ", " : "", $"\n\r\t@{c.Name.Replace(" ", "_")} ", GetDataType(c)));
                if (!c.InPrimaryKey)
                {
                    sb4.Append(string.Concat("\n\r", sb4.Length == 0 ? "SET " : ", ", $"[{c.Name}]=@{c.Name.Replace(" ", "_")}"));

                }else
                {
                    sb6.Append(string.Concat(sb6.Length > 0 ? ", " : "", $"\n\r\t@{c.Name.Replace(" ", "_")} ", GetDataType(c)));
                    sb5.Append(string.Concat("\n\r", sb5.Length == 0 ? "WHERE " : " AND ", $"[{c.Name}]=@{c.Name.Replace(" ", "_")}"));
                }
            }

            // string sql = $"{sql1} {sb.ToString()} {sql2}";
            output1 = output1 + $"{sb0.ToString()}\n\rAS \n\rBEGIN \n\r\tSET NOCOUNT ON;\n\r\tINSERT INTO [{_table.Schema.ToString()}].[{_table.Name}]" +
                $"{sb1.ToString()})\n\r{sb2.ToString()})\n\rEND";

            output2 = output2 + $"{sb3.ToString()}\n\rAS \n\rBEGIN \n\r\tSET NOCOUNT ON;\n\r\tUPDATE [{_table.Schema.ToString()}].[{_table.Name}]" +
                $"{sb4.ToString()}\n\r{sb5.ToString()}\n\rEND";

            output3 = output3 + $"{sb6.ToString()}\n\rAS \n\rBEGIN \n\r\tSET NOCOUNT ON;\n\r\tDELETE FROM [{_table.Schema.ToString()}].[{_table.Name}]" +
                $"\n\r{sb5.ToString()}\n\rEND";

            pk_script.Add(output1);
            pk_script.Add(output2);
            pk_script.Add(output3);

            return pk_script;
        }

        private string GetDataType (Column column)
        {
            string output = $"{column.DataType.SqlDataType.ToString()}";
            SqlDataType type = column.DataType.SqlDataType;

            if (column.DataType.IsStringType)
            {
                if ((column.DataType.SqlDataType == SqlDataType.NVarCharMax) | (column.DataType.SqlDataType == SqlDataType.VarCharMax))
                {
                    output = $"{column.DataType.SqlDataType.ToString().Replace("Max", "", StringComparison.InvariantCultureIgnoreCase)}(MAX)";
                } else
                {
                    if (!((type == SqlDataType.Text) | (type == SqlDataType.NText)))
                    {
                        output = $"{column.DataType.SqlDataType.ToString()}({column.DataType.MaximumLength.ToString()})";
                    }
                }
            }
            if (column.DataType.IsNumericType)
            {
                if ( (type == SqlDataType.Decimal) | (type == SqlDataType.Numeric) )
                {
                    output = $"{column.DataType.SqlDataType.ToString()}({column.DataType.NumericPrecision.ToString()},{column.DataType.NumericScale.ToString()})";
                } 
            }

            if ( (type == SqlDataType.Binary) | (type == SqlDataType.VarBinary) )
            {
                output = $"{column.DataType.SqlDataType.ToString()}({column.DataType.MaximumLength.ToString()})";
            }

            if ((type == SqlDataType.DateTime2) | (type == SqlDataType.DateTimeOffset))
            {
                output = $"{column.DataType.SqlDataType.ToString()}({column.DataType.NumericScale.ToString()})";
            }

            if ((type == SqlDataType.VarBinaryMax))
            {
                output = $"{column.DataType.SqlDataType.ToString().Replace("Max", "", StringComparison.InvariantCultureIgnoreCase)}(MAX)";
            }

            return output;
        }
    }
}
