using System;
using System.Collections.Generic;
using System.Text;

namespace LeonetAutoAuthentication
{
    public class AppConfig
    {


        public AppConfig()
        {
            ConnectUrl = "http://#GATEWAY#/login.cgi";
            ConnectionTimeout = 30000;
            ViewMillisecond = 3000;
            StartupDelayTime = 20000;
        }

        public string ConnectUrl { get; set; }

        public string UserId { get; set; }

        public string Password { get; set; }

        public int ViewMillisecond { get; set; }

        public int ConnectionTimeout { get; set; }

        public int StartupDelayTime { get; set; }

    }
}
