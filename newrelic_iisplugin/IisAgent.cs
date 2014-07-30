using System;
using System.Collections.Generic;
using NewRelic.Platform.Sdk;
using System.Diagnostics;
using System.Management.Automation;
using System.Collections.ObjectModel;

namespace newrelic_iisplugin
{
    class IisAgent : Agent
    {
        public override string Guid { get { return "com.automatedops.iisplugin"; } }
        public override string Version { get { return "0.0.5"; } }

        private string Name { get; set; }
        private List<Object> Counters { get; set; }

        public IisAgent(string name, List<Object> paths)
        {
            Name = name;
            Counters = paths;         
        }

        public override string GetAgentName() {
            return Name;
        }

        public override void PollCycle()
        {
            foreach (Dictionary<string,Object> counter in Counters)
            {
                Collection<PSObject> results = new Collection<PSObject>();

                using (PowerShell ps = PowerShell.Create().AddCommand("Get-Counter").AddParameter("Counter", string.Format("\\\\{0}{1}", Name, counter["path"])))
                {
                    try
                    {
                        results = ps.Invoke();
                    }
                    catch (ArgumentNullException e)
                    {
                        Console.WriteLine("cmdlet is null.", e.Message);
                    }
                    catch (InvalidPowerShellStateException e)
                    {
                        Console.WriteLine("The PowerShell object cannot be changed in its current state.", e.Message);
                    }
                    catch (ObjectDisposedException e)
                    {
                        Console.WriteLine("The PowerShell object is disposed.", e.Message);
                    }
                    catch (RuntimeException e)
                    {
                        Console.WriteLine(
                                      "Runtime exception: {0}: {1}\n{2}",
                                      e.ErrorRecord.InvocationInfo.InvocationName,
                                      e.Message,
                                      e.ErrorRecord.InvocationInfo.PositionMessage);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unknown exception", e.Message);
                    }

                    foreach (PSObject result in results)
                    {
                        dynamic samples = result.Members["CounterSamples"].Value;

                        foreach (dynamic sample in samples)
                        {
                            string[] pathParts = sample.Path.Split('\\');

                            string genericPath = string.Concat(pathParts[3], "\\", pathParts[4]);

                            ReportMetric(genericPath, counter["unit"].ToString(), (float)sample.CookedValue);
                        }
                    }
                }
            }
        }
    }

    class IisAgentFactory : AgentFactory
    {
        public override Agent CreateAgentWithConfiguration(IDictionary<string, object> properties)
        {
            string name = (string)properties["name"];
            List<Object> counterlist = (List<Object>)properties["counterlist"];

            if (counterlist.Count == 0)
            {
                throw new Exception("'counterlist' is empty. Do you have a 'config/plugin.json' file?");
            }

            return new IisAgent(name, counterlist);
        }
    }
}
