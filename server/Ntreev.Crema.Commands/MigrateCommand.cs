﻿//Released under the MIT License.
//
//Copyright (c) 2018 Ntreev Soft co., Ltd.
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//documentation files (the "Software"), to deal in the Software without restriction, including without limitation the 
//rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit 
//persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the 
//Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
//COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Ntreev.Crema.Commands;
using Ntreev.Crema.Services;
using Ntreev.Crema.ServiceModel;
using Ntreev.Library;
using Ntreev.Library.Commands;
using System;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using Ntreev.Crema.Commands.Consoles;
using System.Collections.Generic;
using Ntreev.Library.IO;

namespace Ntreev.Crema.Commands
{
    [Export(typeof(ICommand))]
    [Export]
    [ResourceDescription]
    class MigrateCommand : CommandBase
    {
        private readonly CremaBootstrapper application;
        private string repositoryModule;

        [ImportingConstructor]
        public MigrateCommand(CremaBootstrapper application)
            : base("migrate")
        {
            this.application = application;
        }

        [CommandProperty("path", IsRequired = true)]
        public string Path
        {
            get;
            set;
        }

        [CommandProperty("repo-module")]
        public string RepositoryModule
        {
            get => this.repositoryModule;
            set => this.repositoryModule = value;
        }

        [CommandProperty("file-type")]
        public string FileType
        {
            get;
            set;
        }

        [CommandPropertyArray]
        [Description("database list to migrate")]
        public string[] DataBaseList
        {
            get; set;
        }

        protected override void OnExecute()
        {
            var settings = new RepositoryMigrationSettings()
            {
                RepositoryModule = this.RepositoryModule,
                BasePath = this.Path,
                FileType = this.FileType,
            };
            this.application.MigrateRepository(settings);
        }
    }
}