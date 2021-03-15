using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SerialToHttpPoC
{
    public partial class Service : ServiceBase
    {
        private Listener listener;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            listener = new Listener();
            listener.Start();
        }

        protected override void OnStop()
        {
            if (listener != null)
            {
                listener.Stop();
            }
        }
    }
}
