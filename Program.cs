using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SerialToHttp
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive && args.Length == 0)
            {
                // visual studio run / console run
                RunInteractive(args);
            }
            else if (args.Length == 0)
            {
                // running as a windows service
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new Service()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                ProcessArgument(args[0].ToLower());
            }
        }

        private static void ProcessArgument(string argument)
        {
            switch (argument)
            {
                case "--install":
                    RunInstallUtil(true);
                    break;

                case "--uninstall":
                    RunInstallUtil(false);
                    break;

                default:
                    System.Console.WriteLine("Unknown argument: " + argument);
                    break;
            }
        }

        private static void RunInstallUtil(bool install)
        {
            string arguments = "\"" + Assembly.GetExecutingAssembly().Location + "\"";
            if (!install)
            {
                arguments = "/u " + arguments;
            }
            Process process = new Process();
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = RuntimeEnvironment.GetRuntimeDirectory() + "InstallUtil.exe";
            processStartInfo.Arguments = arguments;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
        }

        private static void RunInteractive(string[] args)
        {
            Service service = new Service();
            service.TestStartupAndStop(args);
        }
    }
}
