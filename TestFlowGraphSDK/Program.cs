using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestFlowGraphSDK
{
    class Program
    {
        static void Main(string[] args)
        {
            FlowGraphSDK.Startup();
            string str = "hello world";
            FlowGraphSDK.RegisterNodes(str);
            FlowGraphSDK.SetGraphsDirectory("X:\\lumberyard\\dev\\TDGame\\flowGrahps");
            Console.ReadLine();
        }
    }
}
