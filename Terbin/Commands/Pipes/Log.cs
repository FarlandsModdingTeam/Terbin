using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terbin.Pipes
{
    public struct Log
    {
        private LogLevel _level;
        private string _message;


        public LogLevel Level
        {
            get { return _level; }
            set { _level = value; }
        }
        public string Messager
        {
            get { return _message; }
            set { _message = value; }
        }


        public Log(string eMesager)
        {
            this._level = LogLevel.Info;
            this._message = eMesager;
        }
        public Log(LogLevel eLevel, string eMesager)
        {
            this._level = eLevel;
            this._message = eMesager;
        }
    }
}
