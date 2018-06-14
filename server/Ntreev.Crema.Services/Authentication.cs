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

using Ntreev.Crema.ServiceModel;
using Ntreev.Crema.Services.Properties;
using Ntreev.Crema.Services.Users;
using Ntreev.Library;
using System;

namespace Ntreev.Crema.Services
{
    public sealed class Authentication : IAuthentication
    {
        private readonly static object lockobj = new object();
        internal static string SystemID = "system";
        internal static string SystemName = "System";
        internal static string AdminID = "admin";
        internal static string AdminName = "Administrator";

        private IAuthenticationProvider provider;
        private EventHandler expired;
        private bool isExpired;
        private Authentication child;

        private Authentication()
        {

        }

        internal Authentication(IAuthenticationProvider provider)
            : this(provider, Guid.Empty)
        {

        }

        internal Authentication(IAuthenticationProvider provider, Guid token)
        {
            this.provider = provider;
            this.Token = token;
        }

        public override string ToString()
        {
            return $"{this.provider.ID}({this.provider.Name})";
        }

        public Authentication BeginCommission()
        {
            if (this.Parent != null || this.isExpired == true)
                throw new InvalidOperationException(Resources.Exception_Expired);
            if (this.child != null)
                throw new InvalidOperationException(Resources.Exception_Commissioned);
            var authentication = new Authentication(new UserAuthenticationProvider(this.provider.ID, this.provider.Name, this.provider.Authority, this.provider.AuthenticationTypes), this.Token)
            {
                SignatureDate = this.SignatureDate,
                Parent = this,
            };
            this.child = authentication;
            return authentication;
        }

        public void EndCommission(Authentication authentication)
        {
            if (this.child == null)
                throw new InvalidOperationException();
            if (this.child != authentication)
                throw new InvalidOperationException();
            this.child.isExpired = true;
            this.child = null;
        }

        public string ID => this.provider.ID;

        public string Name => this.provider.Name;

        public Authority Authority => this.provider.Authority;

        public SignatureDate SignatureDate { get; private set; }

        public AuthenticationInfo AuthenticationInfo => new AuthenticationInfo()
        {
            ID = this.ID,
            Name = this.Name,
            Authority = this.Authority,
        };

        public event EventHandler Expired
        {
            add
            {
                lock (lockobj)
                {
                    if (this.Parent != null)
                        this.Parent.expired += value;
                    else
                        this.expired += value;
                }

            }
            remove
            {
                lock (lockobj)
                {
                    if (this.Parent != null)
                        this.expired -= value;
                    else
                        this.expired -= value;
                }
            }
        }

        internal Guid Token { get; }

        internal void InvokeExpiredEvent(string userID)
        {
            this.InvokeExpiredEvent(userID, string.Empty);
        }

        internal void InvokeExpiredEvent(string userID, string message)
        {
            lock (lockobj)
            {
                this.isExpired = true;
                this.expired?.Invoke(this, EventArgs.Empty);
            }
        }

        internal SignatureDate Sign()
        {
            return this.Sign(DateTime.UtcNow);
        }

        internal SignatureDate Sign(DateTime dateTime)
        {
            this.SignatureDate = new SignatureDate(this.ID, DateTime.UtcNow);
            return this.SignatureDate;
        }

        internal SignatureDateProvider GetSignatureDateProvider()
        {
            return new InternalSignatureDateProvider(this);
        }

        internal void Verify()
        {
            if (this.isExpired == true)
                throw new InvalidOperationException(Resources.Exception_Commissioned);
        }

        internal AuthenticationType Types
        {
            get { return this.provider.AuthenticationTypes; }
        }

        internal Authentication Parent { get; private set; }

        internal bool IsAdmin
        {
            get { return this.Types.HasFlag(AuthenticationType.Administrator); }
        }

        internal bool IsSystem
        {
            get { return this.Types.HasFlag(AuthenticationType.System); }
        }

        internal readonly static Authentication System = new Authentication(new SystemAuthenticationProvider(), Guid.Parse("62E5A6E9-D4BE-438F-A188-D5842C0ED65E"));

        #region IAuthentication

        AuthenticationType IAuthentication.Types
        {
            get { return this.Types; }
        }

        bool IAuthentication.IsAdmin
        {
            get { return this.Types.HasFlag(AuthenticationType.Administrator); }
        }

        bool IAuthentication.IsSystem
        {
            get { return this.Types.HasFlag(AuthenticationType.System); }
        }

        #endregion

        #region SystemAuthenticationProvider

        class SystemAuthenticationProvider : IAuthenticationProvider
        {
            public AuthenticationType AuthenticationTypes
            {
                get { return AuthenticationType.System; }
            }

            public string ID
            {
                get { return Authentication.SystemID; }
            }

            public string Name
            {
                get { return Authentication.SystemName; }
            }

            public Authority Authority
            {
                get { return Authority.None; }
            }
        }

        #endregion

        #region SignatureDateProvider

        class InternalSignatureDateProvider : SignatureDateProvider
        {
            private readonly Authentication authentication;

            public InternalSignatureDateProvider(Authentication authentication)
                : base(authentication.ID)
            {
                this.authentication = authentication;
            }

            protected override DateTime GetTime()
            {
                return this.authentication.SignatureDate.DateTime;
            }
        }

        #endregion
    }
}
