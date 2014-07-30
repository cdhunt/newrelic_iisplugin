﻿using System;
using NewRelic.Platform.Sdk;
using Topshelf;
using Topshelf.Runtime;

namespace newrelic_iisplugin
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<PluginService>(sc =>
                {
                    sc.ConstructUsing(() => new PluginService());

                    sc.WhenStarted(s => s.Start());
                    sc.WhenStopped(s => s.Stop());
                });
                x.SetServiceName("newrelic_iisplugin");
                x.SetDisplayName("NewRelic IIS Plugin");
                x.SetDescription("Sends IIS Metrics to NewRelic Platform");
                x.StartAutomatically();
                x.RunAsPrompt();
            });
        }
    }

    class PluginService
    {
        Runner _runner;

        public PluginService()
        {
            _runner = new Runner();
            _runner.Add(new IisAgentFactory());
        }

        public void Start()
        {
            Console.WriteLine("Starting service.");
            try
            {
                _runner.SetupAndRun();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occurred, unable to continue.\n", e.Message);
            }
        }

        public void Stop()
        {
            Console.WriteLine("Stopping service in 5 seconds.");
            System.Threading.Thread.Sleep(5000);
            
            _runner = null;
        }
    }
}
