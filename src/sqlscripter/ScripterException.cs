using System;
using System.Collections.Generic;
using System.Text;

namespace sqlscripter
{
    class ScripterException: Exception
    {
        public ScripterException(string message) : base(message)
        {
        }
    }
}
