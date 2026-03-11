using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terbin.Pipes
{



    public class LoggerPipe
    {
        private readonly object _lock = new();


        public LoggerPipe()
        {

        }

        public void Init()
        {

        }

        private async Task communicateLog()
        {

        }

        public void Log(LogLevel level, string message)
        {
            lock (_lock)
            {
                // TODO: Determinar escribir
            }
        }
    }
}
