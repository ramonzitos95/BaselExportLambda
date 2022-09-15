using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselExportLambda
{
    public class Parameter
    {
        public string Environment { get; set; }
        public Parameter(string environment)
        {
            Environment = environment;
        }
    }
}
