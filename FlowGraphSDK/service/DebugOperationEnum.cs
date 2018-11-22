using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    class DebugOperation
    {
        public static readonly string DebugEntry = "entry"; // 进入调试
        public static readonly string DebugContinue = "continue";  // 跳过继续
        public static readonly string DebugNext = "next";      // 单步执行
        public static readonly string DebugExit = "exit";     // 退出调试
        public static readonly string DebugAdd = "add";     // 添加一个断点
        public static readonly string DebugDelete = "delete";    // 删除一个断点
    }
}
