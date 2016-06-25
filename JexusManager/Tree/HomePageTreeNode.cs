﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JexusManager.Tree
{
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using JexusManager.Features.Main;

    using Microsoft.Web.Administration;
    using Microsoft.Web.Management.Server;

    internal sealed class HomePageTreeNode : ManagerTreeNode
    {
        public HomePageTreeNode()
            : base("Start Page", null)
        {
            SelectedImageIndex = 0;
            ImageIndex = 0;
        }

        public override string PathToSite
        {
            get { return string.Empty; }
        }

        public override string Folder
        {
            get { return string.Empty; }
        }

        public override string Uri
        {
            get { return null; }
        }

        public override ServerManager ServerManager { get; set; }


        public override void LoadPanels(MainForm mainForm, ServiceContainer serviceContainer, List<ModuleProvider> moduleProviders)
        {
            mainForm.LoadInner(new HomePage());
        }

        public async override Task HandleDoubleClick(MainForm mainForm)
        {
        }

        public async override Task Expand(MainForm mainForm)
        { }

        public async override Task AddApplication(ContextMenuStrip appMenu)
        {
        }

        public override void AddVirtualDirectory(ContextMenuStrip vDirMenu)
        {
        }
    }
}
