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

using Ntreev.Crema.Data.Xml.Schema;
using Ntreev.Crema.ServiceModel;
using Ntreev.Crema.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Text;

namespace Ntreev.Crema.Javascript.Methods.TypeTemplate
{
    [Export(typeof(IScriptMethod))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Category(nameof(TypeTemplate))]
    class GetTypeTemplateMemberPropertyMethod : DomainScriptMethodBase
    {
        [ImportingConstructor]
        public GetTypeTemplateMemberPropertyMethod(ICremaHost cremaHost)
            : base(cremaHost)
        {

        }

        protected override Delegate CreateDelegate()
        {
            return new Func<string, string, TypeMemberProperties, object>(this.GetTypeTemplateMemberProperty);
        }

        private object GetTypeTemplateMemberProperty(string domainID, string memberName, TypeMemberProperties propertyName)
        {
            var template = this.GetDomainHost<ITypeTemplate>(domainID);
            return template.Dispatcher.Invoke(() =>
            {
                var member = template[memberName];
                if (member == null)
                    throw new ItemNotFoundException(memberName);
                if (propertyName == TypeMemberProperties.Name)
                    return (object)member.Name;
                else if (propertyName == TypeMemberProperties.Value)
                    return (object)member.Value;
                else if (propertyName == TypeMemberProperties.Comment)
                    return (object)member.Comment;

                throw new NotImplementedException();
            });
        }
    }
}
