﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ntreev.Crema.ServiceHosts
{
    public interface IHttpServiceHost
    {
        void Open(int port);
        void Close();
    }
}