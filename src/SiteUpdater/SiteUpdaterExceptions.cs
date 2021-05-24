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
        public SiteUpdaterExceptionBase(string msg) : base(msg) { }
        public SiteUpdaterExceptionBase(string msg, Exception ex) : base(msg, ex) { }
    }

    public class SiteUpdaterConfigurationException : SiteUpdaterExceptionBase
    {
        public SiteUpdaterConfigurationException(string optionName, Exception ex) : base($"CMD line arguement '{optionName}' error", ex) { }
    }
    public class SiteUpdaterConfigurationMissingArgumentsException : SiteUpdaterExceptionBase
    {
    }
    public class SiteUpdaterMissingTargetsException : SiteUpdaterExceptionBase
    {
    }
    public class SiteUpdaterMissingDirectoryException : SiteUpdaterExceptionBase
    {
        public SiteUpdaterMissingDirectoryException(string dir) : base($"Missing '{dir}' setting in appSettings.json") { }
    }
    public class SiteUpdaterBadDirectoryException : SiteUpdaterExceptionBase
    {
        public SiteUpdaterBadDirectoryException(string dir) : base($"'{dir}' does not exists") { }
    }

}
