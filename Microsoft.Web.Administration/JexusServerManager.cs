﻿﻿// Copyright (c) Lex Li. All rights reserved. 
//  
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

using Microsoft.Web.Administration.Properties;
using System;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.Web.Administration
{
    public sealed partial class JexusServerManager : ServerManager
    {
        public JexusServerManager(string hostName, string credentials)
            : base(hostName, false)
        {
            Credentials = credentials;
        }

        protected override void PreInitialize()
        {
            base.PreInitialize();
            CreateCache();
        }

        private void CreateCache()
        {
            var name = HostName.Replace(':', '_');
            CacheFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Jexus Manager",
                "cache",
                name);
            FileName = Path.Combine(CacheFolder, "applicationHost.config");
            if (!Directory.Exists(CacheFolder))
            {
                Directory.CreateDirectory(CacheFolder);
            }

            File.WriteAllText(FileName, Resources.original);
        }

        internal string CacheFolder { get; set; }

        protected override void PostInitialize()
        {
            base.PostInitialize();
            AsyncHelper.RunSync(LoadAsync);
        }

        protected override void PostCommitChanges()
        {
            base.PostCommitChanges();
            foreach (Site site in Sites)
            {
                foreach (Application application in site.Applications)
                {
                    AsyncHelper.RunSync(() => SaveAsync(application));
                }
            }

            Save();
        }

        internal override bool GetSiteState(Site site)
        {
            return false;
        }

        internal override bool GetPoolState(ApplicationPool pool)
        {
            return true;
        }

        internal override void Start(Site site)
        {
        }

        internal override void Stop(Site site)
        {
        }

        internal override string GetPhysicalPath(string root)
        {
            return Path.Combine(CacheFolder, root.Replace('/', '_'));
        }

        internal override IEnumerable<string> GetSchemaFiles()
        {
            var environment = Path.Combine(
                Environment.ExpandEnvironmentVariables("%JEXUS_CONFIG%"),
                "schema");
            return Directory.Exists(environment) ? Directory.GetFiles(environment) : base.GetSchemaFiles();
        }

        internal override void CleanSiteFile(string file)
        {
            base.CleanSiteFile(file);
            File.Delete(file);
        }
    }
}