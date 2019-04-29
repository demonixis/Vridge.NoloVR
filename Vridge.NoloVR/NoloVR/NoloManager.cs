using System;
using NoloClientCSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace TestClientSharpDemo
{
    class NoloManager
    {

        public void StartNoloDevice()
        {
            var path = @"NoloServer\NoloServer.exe";
            if (File.Exists(path))
                NoloClientLib.StartNoloServer(@"NoloServer\NoloServer.exe");

            NoloClientLib.OpenNoloZeroMQ();
        }
    }
}
