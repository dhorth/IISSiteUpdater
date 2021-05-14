using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteUpdater
{
    public class SiteUpdaterExceptionBase : Exception
    {
        public SiteUpdaterExceptionBase() : base() { }
        public SiteUpdaterExceptionBase(string msg, Exception ex) : base(msg, ex) { }
    }

    public class SiteUpdaterConfigurationException : SiteUpdaterExceptionBase
    {
        public SiteUpdaterConfigurationException(string optionName, Exception ex) : base($"CMD line arguement '{optionName}' error", ex) { }
    }
    public class SiteUpdaterConfigurationMissingArgumentsException : SiteUpdaterExceptionBase
    {
    }

}
