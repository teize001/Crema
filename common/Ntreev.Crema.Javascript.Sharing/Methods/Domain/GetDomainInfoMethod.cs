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

using Ntreev.Crema.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using Ntreev.Crema.Data.Xml.Schema;
using System.ComponentModel;

namespace Ntreev.Crema.Javascript.Methods.Domain
{
    [Export(typeof(IScriptMethod))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Category(nameof(Domain))]
    class GetDomainInfoMethod : DomainScriptMethodBase
    {
        [ImportingConstructor]
        public GetDomainInfoMethod(ICremaHost cremaHost)
            : base(cremaHost)
        {

        }

        protected override Delegate CreateDelegate()
        {
            return new Func<string, IDictionary<string, object>>(GetDomainInfo);
        }

        private IDictionary<string, object> GetDomainInfo(string domainID)
        {
            var domain = this.GetDomain(domainID);
            var domainInfo = domain.DomainInfo;
            var props = new Dictionary<string, object>
            {
                { nameof(domainInfo.DomainID), $"{domainInfo.DomainID}" },
                { nameof(domainInfo.DataBaseID), $"{domainInfo.DataBaseID}" },
                { nameof(domainInfo.ItemPath), domainInfo.ItemPath },
                { nameof(domainInfo.ItemType), $"{domainInfo.ItemType}" },
                { nameof(domainInfo.DomainType), $"{domainInfo.DomainType}" },
                { nameof(domainInfo.CategoryPath), $"{domainInfo.CategoryPath}" },
                { CremaSchema.Creator, domainInfo.CreationInfo.ID },
                { CremaSchema.CreatedDateTime, domainInfo.CreationInfo.DateTime },
                { CremaSchema.Modifier, domainInfo.ModificationInfo.ID },
                { CremaSchema.ModifiedDateTime, domainInfo.ModificationInfo.DateTime }
            };
            return props;
        }
    }
}
