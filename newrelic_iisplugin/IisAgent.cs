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
        public override string Guid { get { return "iisplugin"; } }
        public override string Version { get { return "0.0.1"; } }

        private List<string> Counters { get; set; }

        public IisAgent(List<Object> paths)
        {
            List<string> newList = new List<string>();

            foreach (object p in paths)
            {
                try 
                {
                    newList.Add(p.ToString());
                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
            }

            Counters = newList;            
        }

        public override string GetAgentName() {
            return System.Environment.MachineName;
        }

        public override void PollCycle()
        {
            foreach (string counter in Counters)
            {
                Collection<PSObject> results = new Collection<PSObject>();

                using (PowerShell ps = PowerShell.Create().AddCommand("Get-Counter").AddParameter("Counter", counter))
                {
                    try
                    {
                        results = ps.Invoke();
                    }
                    catch (ArgumentNullException e)
                    {
                        Console.WriteLine("cmdlet is null.");
                    }
                    catch (InvalidPowerShellStateException e)
                    {
                        Console.WriteLine("The PowerShell object cannot be changed in its current state.");
                    }
                    catch (ObjectDisposedException e)
                    {
                        Console.WriteLine("The PowerShell object is disposed.");
                    }
                    catch (RuntimeException runtimeException)
                    {
                        Console.WriteLine(
                                      "Runtime exception: {0}: {1}\n{2}",
                                      runtimeException.ErrorRecord.InvocationInfo.InvocationName,
                                      runtimeException.Message,
                                      runtimeException.ErrorRecord.InvocationInfo.PositionMessage);
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e);
                    }

                    foreach (PSObject result in results)
                    {
                        dynamic samples = result.Members["CounterSamples"].Value;

                        foreach (dynamic sample in samples)
                        {
                            ReportMetric((string)sample.Path, "undefined", (float)sample.CookedValue);
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
            List<Object> counterlist = (List<Object>)properties["counterlist"];

            if (counterlist == null)
            {
                throw new Exception("'counterlist' is null. Do you have a 'config/plugin.json' file?");
            }

            return new IisAgent(counterlist);
        }
    }
}
