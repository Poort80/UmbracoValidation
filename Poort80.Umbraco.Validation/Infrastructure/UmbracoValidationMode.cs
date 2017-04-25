using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poort80.Umbraco.Validation.Infrastructure
{
    [Flags]
    public enum UmbracoValidationMode
    {
        None = 0,
        Validation = 1,
        Name = 2,
        All = Validation | Name
    }
}
