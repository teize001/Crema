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

using Ntreev.Crema.Data;
using Ntreev.Crema.ServiceModel;
using Ntreev.Crema.Services;

namespace Ntreev.Crema.Client.Framework
{
    public interface ITableDescriptor : IDescriptorBase
    {
        string Name { get; }

        string TableName { get; }

        string Path { get; }

        string DisplayName { get; }

        TableInfo TableInfo { get; }

        TableDetailInfo TableDetailInfo { get; }

        TableState TableState { get; }

        TableAttribute TableAttribute { get; }

        LockInfo LockInfo { get; }

        AccessInfo AccessInfo { get; }

        AccessType AccessType { get; }

        bool IsLocked { get; }

        bool IsLockInherited { get; }

        bool IsLockOwner { get; }

        bool IsPrivate { get; }

        bool IsAccessInherited { get; }

        bool IsAccessOwner { get; }

        bool IsAccessMember { get; }

        bool IsBeingEdited { get; }

        bool IsBeingEditedClient { get; }

        bool IsBeingSetup { get; }

        bool IsBeingSetupClient { get; }

        bool IsInherited { get; }

        bool IsBaseTemplate { get; }

        new ITable Target { get; }
    }
}
