﻿// Copyright (c) Lex Li. All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Tests.IsapiCgiRestriction
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using global::JexusManager.Features.IsapiCgiRestriction;
    using global::JexusManager.Services;

    using Microsoft.Web.Administration;
    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Client.Win32;
    using Microsoft.Web.Management.Server;

    using Moq;

    using Xunit;

    public class IsapiCgiRestrictionFeatureSiteTestFixture
    {
        private IsapiCgiRestrictionFeature _feature;

        private ServerManager _server;

        private const string Current = @"applicationHost.config";

        public void SetUp()
        {
            const string Original = @"original.config";
            const string OriginalMono = @"original.mono.config";
            if (Helper.IsRunningOnMono())
            {
                File.Copy("Website1/original.config", "Website1/web.config", true);
                File.Copy(OriginalMono, Current, true);
            }
            else
            {
                File.Copy("Website1\\original.config", "Website1\\web.config", true);
                File.Copy(Original, Current, true);
            }

            Environment.SetEnvironmentVariable(
                "JEXUS_TEST_HOME",
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            _server = new IisExpressServerManager(Current);

            var serviceContainer = new ServiceContainer();
            serviceContainer.RemoveService(typeof(IConfigurationService));
            serviceContainer.RemoveService(typeof(IControlPanel));
            var scope = ManagementScope.Site;
            serviceContainer.AddService(typeof(IControlPanel), new ControlPanel());
            serviceContainer.AddService(
                typeof(IConfigurationService),
                new ConfigurationService(
                    null,
                    _server.Sites[0].GetWebConfiguration(),
                    scope,
                    null,
                    _server.Sites[0],
                    null,
                    null,
                    null, _server.Sites[0].Name));

            serviceContainer.RemoveService(typeof(IManagementUIService));
            var mock = new Mock<IManagementUIService>();
            mock.Setup(
                action =>
                action.ShowMessage(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<MessageBoxButtons>(),
                    It.IsAny<MessageBoxIcon>(),
                    It.IsAny<MessageBoxDefaultButton>())).Returns(DialogResult.Yes);
            serviceContainer.AddService(typeof(IManagementUIService), mock.Object);

            var module = new IsapiCgiRestrictionModule();
            module.TestInitialize(serviceContainer, null);

            _feature = new IsapiCgiRestrictionFeature(module);
            _feature.Load();
        }

        [Fact]
        public void TestBasic()
        {
            SetUp();
            Assert.Equal(4, _feature.Items.Count);
        }

        [Fact]
        public void TestRemoveInherited()
        {
            SetUp();

            const string Expected = @"expected_add.site.config";
            var document = XDocument.Load(Current);
            var node = new XElement("location");
            node.SetAttributeValue("path", "WebSite1");
            document.Root?.Add(node);
            var web = new XElement("system.webServer");
            node.Add(web);
            var security = new XElement("security");
            web.Add(security);
            var ip = new XElement("isapiCgiRestriction");
            security.Add(ip);
            var remove = new XElement("remove");
            remove.SetAttributeValue("path", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\webengine4.dll");
            ip.Add(remove);
            document.Save(Expected);

            _feature.SelectedItem = _feature.Items[0];
            Assert.Equal("ASP.NET_v4.0", _feature.SelectedItem.Description);
            _feature.Remove();
            Assert.Null(_feature.SelectedItem);
            Assert.Equal(3, _feature.Items.Count);

            XmlAssert.Equal(Expected, Current);
            XmlAssert.Equal(Path.Combine("Website1", "original.config"), Path.Combine("Website1", "web.config"));
        }

        [Fact]
        public void TestRemove()
        {
            SetUp();

            const string Expected = @"expected_add.site.config";
            var document = XDocument.Load(Current);
            var node = new XElement("location");
            node.SetAttributeValue("path", "WebSite1");
            document.Root?.Add(node);
            document.Save(Expected);

            var item = new IsapiCgiRestrictionItem(null);
            item.Description = "test";
            item.Path = "c:\\test.dll";
            _feature.AddItem(item);

            Assert.Equal("test", _feature.SelectedItem.Description);
            Assert.Equal(5, _feature.Items.Count);
            _feature.Remove();
            Assert.Null(_feature.SelectedItem);
            Assert.Equal(4, _feature.Items.Count);

            XmlAssert.Equal(Expected, Current);
            XmlAssert.Equal(Path.Combine("Website1", "original.config"), Path.Combine("Website1", "web.config"));
        }

        [Fact]
        public void TestEditInherited()
        {
            SetUp();

            const string Expected = @"expected_add.site.config";
            var document = XDocument.Load(Current);
            var node = new XElement("location");
            node.SetAttributeValue("path", "WebSite1");
            document.Root?.Add(node);
            var web = new XElement("system.webServer");
            node.Add(web);
            var security = new XElement("security");
            web.Add(security);
            var ip = new XElement("isapiCgiRestriction");
            security.Add(ip);
            var remove = new XElement("remove");
            remove.SetAttributeValue("path", @"%windir%\Microsoft.NET\Framework64\v4.0.30319\webengine4.dll");
            ip.Add(remove);
            var add = new XElement("add");
            add.SetAttributeValue("allowed", "true");
            add.SetAttributeValue("path", "c:\\test.dll");
            add.SetAttributeValue("description", "ASP.NET_v4.0");
            ip.Add(add);
            document.Save(Expected);

            _feature.SelectedItem = _feature.Items[0];
            Assert.Equal("ASP.NET_v4.0", _feature.SelectedItem.Description);
            var item = _feature.SelectedItem;
            item.Path = "c:\\test.dll";
            _feature.EditItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("c:\\test.dll", _feature.SelectedItem.Path);

            XmlAssert.Equal(Expected, Current);
            XmlAssert.Equal(Path.Combine("Website1", "original.config"), Path.Combine("Website1", "web.config"));
        }

        [Fact]
        public void TestEdit()
        {
            SetUp();

            const string Expected = @"expected_add.site.config";
            var document = XDocument.Load(Current);
            var node = new XElement("location");
            node.SetAttributeValue("path", "WebSite1");
            document.Root?.Add(node);
            var web = new XElement("system.webServer");
            node.Add(web);
            var security = new XElement("security");
            web.Add(security);
            var ip = new XElement("isapiCgiRestriction");
            security.Add(ip);
            var add = new XElement("add");
            add.SetAttributeValue("allowed", "false");
            add.SetAttributeValue("path", "c:\\test.exe");
            add.SetAttributeValue("description", "test");
            ip.Add(add);
            document.Save(Expected);

            var item = new IsapiCgiRestrictionItem(null);
            item.Description = "test";
            item.Path = "c:\\test.dll";
            _feature.AddItem(item);

            Assert.Equal("test", _feature.SelectedItem.Description);
            Assert.Equal(5, _feature.Items.Count);
            item.Path = "c:\\test.exe";
            _feature.EditItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("c:\\test.exe", _feature.SelectedItem.Path);
            Assert.Equal(5, _feature.Items.Count);

            XmlAssert.Equal(Expected, Current);
            XmlAssert.Equal(Path.Combine("Website1", "original.config"), Path.Combine("Website1", "web.config"));
        }

        [Fact]
        public void TestAdd()
        {
            SetUp();

            const string Expected = @"expected_add.site.config";
            var document = XDocument.Load(Current);
            var node = new XElement("location");
            node.SetAttributeValue("path", "WebSite1");
            document.Root?.Add(node);
            var web = new XElement("system.webServer");
            node.Add(web);
            var security = new XElement("security");
            web.Add(security);
            var ip = new XElement("isapiCgiRestriction");
            security.Add(ip);
            var add = new XElement("add");
            add.SetAttributeValue("allowed", "false");
            add.SetAttributeValue("path", "c:\\test.dll");
            add.SetAttributeValue("description", "test");
            ip.Add(add);
            document.Save(Expected);

            var item = new IsapiCgiRestrictionItem(null);
            item.Description = "test";
            item.Path = "c:\\test.dll";
            _feature.AddItem(item);
            Assert.NotNull(_feature.SelectedItem);
            Assert.Equal("test", _feature.SelectedItem.Description);

            XmlAssert.Equal(Expected, Current);
            XmlAssert.Equal(Path.Combine("Website1", "original.config"), Path.Combine("Website1", "web.config"));
        }
    }
}
