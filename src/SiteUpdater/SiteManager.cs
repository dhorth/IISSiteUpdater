using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Linq;
using Serilog.Core;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;


namespace SiteUpdater
{
    public class SiteManager
    {
        private static Logger _log;
        private string _siteName;
        private int _port;
        private string _sourceDir;
        private string _destinationDir;
        private string sourcePath => $@"{_sourceDir}\{_siteName}";
        private string destinationPath => $@"{_destinationDir}\{_siteName}";

        public SiteManager(IConfiguration config, Logger log)
        {
            _log = log;

            _sourceDir = config.GetSection("Source").Value;
            if (string.IsNullOrWhiteSpace(_sourceDir))
                throw new SiteUpdaterMissingDirectoryException("Source");
            
            if(!Directory.Exists(_sourceDir))
                throw new SiteUpdaterBadDirectoryException("Source");

            _destinationDir = config.GetSection("Destination").Value;
            if (string.IsNullOrWhiteSpace(_destinationDir))
                throw new SiteUpdaterMissingDirectoryException("Destination");

            _log.Debug($"Site Update Source Dir = {sourcePath}");
            _log.Debug($"Site Update Desitnation Dir = {destinationPath}");
        }

        public int InstallSite(string siteName, int port)
        {
            var rc = 0;
            _siteName = siteName;
            _port = port;
            _log.Debug($"Install Site - {siteName}:{port}");

            rc = StopServer();
            rc += AddSite();
            rc += UpdateSite();
            rc += StartServer();


            _log.Information($"Complete Site - {siteName}:{port}");
            _log.Debug("".PadRight(50, '-'));
            return rc > 0 ? 1 : 0;
        }

        public int InstallSite(string siteName, int port, string source, string destination)
        {
            var rc = 0;
            _siteName = siteName;
            _port = port;
            _sourceDir = source;
            _destinationDir = destination;

            _log.Debug($"Install Site - {siteName}:{port}");
            _log.Debug($"Site Update Source Dir = {sourcePath}");
            _log.Debug($"Site Update Desitnation Dir = {destinationPath}");

            rc = StopServer();
            rc += AddSite();
            rc += UpdateSite();
            rc += StartServer();

            _log.Information($"Complete Site - {siteName}:{port}");
            _log.Debug("".PadRight(50, '-'));
            return rc > 0 ? 1 : 0;
        }

        private int StopServer()
        {
            var ret = 0;
            _log.Debug($"Start StopServer - {_siteName}");
            using (var sm = new ServerManager())
            {
                var site = sm.Sites.FirstOrDefault(a => a.Name == _siteName);
                if (site != null)
                {
                    _log.Verbose($"Stop site {_siteName}");
                    try
                    {
                        site.Stop();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error Stopping Site");
                        ret = 1;
                    }

                    _log.Verbose($"Stop AppPool {_siteName}");
                    try
                    {
                        var pool = sm.ApplicationPools.FirstOrDefault(a => a.Name == _siteName);
                        if (pool != null && (pool.State== ObjectState.Started || pool.State ==ObjectState.Starting))
                        {
                            pool.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error Stopping App Pool");
                        ret = 1;
                    }

                    _log.Verbose($"Commit Changes");
                    try
                    {
                        sm.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.ToString());
                        ret = 1;
                    }

                }
            }
            _log.Debug($"Complete StopServer - {_siteName}");
            return ret;
        }
        private int AddSite()
        {
            var ret = 0;
            _log.Debug($"Start AddSite - {_siteName}");
            using (var sm = new ServerManager())
            {
                if (!Directory.Exists(destinationPath))
                    Directory.CreateDirectory(destinationPath);

                if (!sm.Sites.Any(a => a.Name == _siteName))
                {
                    _log.Verbose($"Add site {_siteName}");
                    try
                    {
                        var site = sm.Sites.Add(_siteName, destinationPath, _port);
                        site.ServerAutoStart = true;
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.ToString());
                        ret = 1;
                    }


                    _log.Verbose($"Add AppPool {_siteName}");
                    try
                    {
                        if (!sm.ApplicationPools.Any(a => a.Name == _siteName))
                        {
                            var pool = sm.ApplicationPools.Add(_siteName);
                            pool.AutoStart = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.ToString());
                        ret = 1;
                    }

                    _log.Verbose($"AppPool {_siteName}");
                    try
                    {
                        var site = sm.Sites.FirstOrDefault(a => a.Name == _siteName);
                        if (site != null)
                            site.Applications.First().ApplicationPoolName = _siteName;
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex.ToString());
                        ret = 1;
                    }

                    _log.Verbose($"CommitChanges");
                    sm.CommitChanges();
                }
            }
            _log.Debug($"Complete AddSite - {_siteName}");
            return ret;
        }
        private int UpdateSite()
        {
            var ret = 0;
            _log.Debug($"Start UpdateSite - {_siteName}");
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            _log.Debug($"Update Site From: {sourcePath} Local: {destinationPath}");
            ret = CopyDirectory(sourcePath, destinationPath);
            _log.Debug($"Complete UpdateSite - {_siteName}");
            return ret;
        }
        private int StartServer()
        {
            var ret = 0;
            _log.Debug($"Start StartServer");
            using (var sm = new ServerManager())
            {
                var site = sm.Sites.FirstOrDefault(a => a.Name == _siteName);
                if (site != null)
                {
                    _log.Verbose($"Start site {_siteName}");
                    try
                    {
                        site.Start();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Starting Site");
                        ret = 1;
                    }

                    _log.Verbose($"Start appPool {_siteName}");
                    try
                    {
                        var pool = sm.ApplicationPools.FirstOrDefault(a => a.Name == _siteName);
                        if (pool != null)
                        {
                            pool.Start();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Starting App Pool");
                        ret = 1;
                    }

                    _log.Verbose($"Commit Changes");
                    try
                    {
                        sm.CommitChanges();
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Starting Server");
                        ret = 1;
                    }

                }
            }
            _log.Debug($"Complete StartServer");
            return ret;
        }
        private int CopyDirectory(string source, string dest)
        {
            var ret = 0;
            _log.Debug($"Start CopyDirectory");

            try
            {
                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                {
                    var dir = dirPath.Replace(source, dest);
                    _log.Verbose($"Create Directory {dir}");
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Creating Directory");
                ret = 1;
            }

            try
            {
                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                {
                    var file = newPath.Replace(source, dest);
                    _log.Verbose($"Copy File from {file} to {newPath}");
                    File.Copy(newPath, file, true);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Copying File");
                ret = 1;
            }
            _log.Debug($"Complete CopyDirectory");
            return ret;
        }
        public bool IsUserAdministrator()
        {
            _log.Debug($"Start IsUserAdministrator");
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                if (!isAdmin)
                    _log.Fatal("This program requires Administrative Privaleges, please restart with elevated rights");
            }
            catch (UnauthorizedAccessException ex)
            {
                _log.Error(ex, "IsUserAdministrator Access Error");
                isAdmin = false;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "IsUserAdministrator");
                isAdmin = false;
            }
            _log.Debug($"Complete IsUserAdministrator {isAdmin}");
            return isAdmin;
        }
    }
}
