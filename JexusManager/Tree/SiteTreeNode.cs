﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JexusManager.Tree
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using JexusManager.Dialogs;
    using JexusManager.Features.Main;
    using JexusManager.Services;

    using Microsoft.Web.Administration;
    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Server;

    internal sealed class SiteTreeNode : ManagerTreeNode
    {
        private bool _loaded;

        public SiteTreeNode(IServiceProvider serviceProvider, Site site)
            : base(site.Name, serviceProvider)
        {
            ImageIndex = 4;
            SelectedImageIndex = 4;
            Tag = site;
            Site = site;
            Nodes.Add("temp");
            ServerManager = site.Server;
        }

        public Site Site { get; }

        public override string PathToSite
        {
            get { return "/"; }
        }

        public override string Uri
        {
            get { return Site.Bindings[0].ToUri(); }
        }

        public override ServerManager ServerManager { get; set; }

        public override void LoadPanels(MainForm mainForm, ServiceContainer serviceContainer, List<ModuleProvider> moduleProviders)
        {
            serviceContainer.RemoveService(typeof(IConfigurationService));
            serviceContainer.RemoveService(typeof(IControlPanel));
            var panel = new SitePage(Site, mainForm);

            var scope = ManagementScope.Site;
            serviceContainer.AddService(typeof(IControlPanel), new ControlPanel());
            serviceContainer.AddService(typeof(IConfigurationService),
                new ConfigurationService(mainForm, Site.GetWebConfiguration(), scope, null, Site, Site.Applications[0],
                    null, null, Site.Name));
            foreach (var provider in moduleProviders)
            {
                if (!provider.SupportsScope(scope))
                {
                    continue;
                }

                var definition = provider.GetModuleDefinition(null);
                var type = Type.GetType(definition.ClientModuleTypeName);
                if (type == null)
                {
                    continue;
                }

                if (!typeof(Module).IsAssignableFrom(type))
                {
                    continue;
                }

                var module = (Module)Activator.CreateInstance(type);
                module.Initialize(serviceContainer, null);
            }

            IModulePage page = panel;
            var mainModule = new MainModule();
            mainModule.Initialize(serviceContainer, null);
            page.Initialize(mainModule, null, null);
            mainForm.LoadPage(page);
        }

        public async override Task Expand(MainForm mainForm)
        {
            if (_loaded)
            {
                return;
            }

            Nodes.Clear();
            var rootApp = Site.Applications[0];
            var rootFolder = rootApp.VirtualDirectories[0].PhysicalPath.ExpandIisExpressEnvironmentVariables();
            LoadChildren(rootApp, 0, rootFolder, PathToSite, mainForm.PhysicalDirectoryMenu,
                mainForm.VirtualDirectoryMenu, mainForm.ApplicationMenu);
            _loaded = true;
        }

        public override async Task AddApplication(ContextMenuStrip appMenu)
        {
            var dialog = new NewApplicationDialog(ServiceProvider, Site, PathToSite, Site.Applications[0].ApplicationPoolName, null);
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            dialog.Application.Save();
            AddToParent(this, new ApplicationTreeNode(ServiceProvider, dialog.Application) { ContextMenuStrip = appMenu });
        }

        public override void AddVirtualDirectory(ContextMenuStrip vDirMenu)
        {
            var dialog = new NewVirtualDirectoryDialog(ServiceProvider, null, PathToSite, Site.Applications[0]);
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            //await dialog.VirtualDirectory.SaveAsync();
            AddToParent(this, new VirtualDirectoryTreeNode(ServiceProvider, dialog.VirtualDirectory) { ContextMenuStrip = vDirMenu });
        }

        public override string Folder
        {
            get
            {
                var rootApp = Site.Applications[0];
                return rootApp.VirtualDirectories[0].PhysicalPath.ExpandIisExpressEnvironmentVariables();
            }
        }

        public async override Task HandleDoubleClick(MainForm mainForm)
        { }
    }
}
