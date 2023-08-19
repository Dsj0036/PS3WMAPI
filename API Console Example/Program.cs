using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PS3WMAPI;
using PS3WMAPI.PS3;
namespace API_Console_Example
{
    internal class Program
    {
        static Client client;
        static void Main(string[] args)
        {
            var ip = RequestInput("Specify Client Address for connection. E.g. 192.168.0.3");
            if (IPAddress.TryParse(ip, out IPAddress address))
            { 

                void Retry()
                {
                    var ping = new Ping();
                    var reply = ping.Send(address);
                    ping.Dispose();
                    bool ok = reply.Status == IPStatus.Success;
                    if (ok)
                    {
                        Console.WriteLine("Paired to server sucessfully.");
                        var displayName = RequestInput("Set a display name. ");
                        client = new Client(ip, displayName);
                        client.Initialize();
                        client.WServer.Retrieve((s, e) => Console.WriteLine("Data retrieving finished again. With no exceptions."));
                        client.OnStateChanged +=
                            (s, e) => Console.WriteLine(e ? "Server state triggered to true" : "Server state triggered to false");
                        client.OnProgressReport += (S, e) => EventReport(e, "Progression reported with value "+e.ProgressPercentage);
                        client.OnInitializationFinished += (s, e) => EventReport(e, "Initialization reported initialization with MSTATE: " + client.MState.ToString().ToUpper());
                         
                        LoopRead(Console.ReadLine());
                    }
                    else
                    {
                        var errorResp = RequestInput("Server unreachable. Write R to retry now else use other address");
                        if (errorResp == "R")
                        {
                            Retry();
                        }
                        else
                        {
                            Main(null);
                        }
                    }

                }
                Retry();

            }
            else 
            {
            
            }
        }
        static string RequestInput(string say)
        {
            Console.WriteLine(say);
            return Console.ReadLine();
        }
        static string LoopRead(string query)
        {
            if (query != string.Empty)
            {
                Task.Run(() =>
                {
                    var w = new WebClient();
                    var cmd = "http://"+client.Address +"/"+ query.Trim('/');
                    w.DownloadData(cmd);
                    w.Dispose();
                });
            }
            return LoopRead(Console.ReadLine());
        }
        static void EventReport<T>(T args, string saywhat) where T : EventArgs
        {
            var t = typeof(T);
             var propCount = t.GetProperties().Count();
            var name = t.FullName;
            Console.WriteLine("[Event] ({0}) : {1}", name, saywhat);
        }
    }
}
