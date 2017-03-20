using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fleck;

namespace WebSocketServer
{
    class Program
    {
        static List<IWebSocketConnection> _allSockets;
		static Dictionary<string,decimal> _allMeans;
		static Dictionary<string,decimal> _allCounts;
	    private static decimal _localMean;
	    private static decimal _localCount;
	    private static decimal _localAggregate_count;
	    private static decimal _localAggregate_average;

	    static void Main(string[] args)
        {
			_allSockets = new List<IWebSocketConnection>();
			_allMeans = new Dictionary<string, decimal>();
			_allCounts = new Dictionary<string, decimal>();

			var serverAddresses = new Dictionary<string,string>();
			//serverAddresses.Add("USA-WestCoast", "ws://127.0.0.1:58951");
			//serverAddresses.Add("USA-EastCoast", "ws://127.0.0.1:58952");
			serverAddresses.Add("UK", "ws://127.0.0.1:58953");
			serverAddresses.Add("EU-North", "ws://127.0.0.1:58954");
			//serverAddresses.Add("EU-South", "ws://127.0.0.1:58955");
		    foreach (var serverAddress in serverAddresses)
		    {
				_allMeans.Add(serverAddress.Key, 0m);
				_allCounts.Add(serverAddress.Key, 0m);
			}

			var thisNodeName = ConfigurationSettings.AppSettings["thisNodeName"];   //for example "UK"
	        var serverSocketAddress = serverAddresses.First(x=>x.Key==thisNodeName);
			serverAddresses.Remove(thisNodeName);
			
			var websocketServer = new Fleck.WebSocketServer(serverSocketAddress.Value);
            
            websocketServer.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Open!");
                    _allSockets.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    _allSockets.Remove(socket);
                };
                socket.OnMessage = message =>
                {
					Console.WriteLine(message + " received");

					var parameters = message.Split('~');
	                var remoteHost = parameters[0];
	                var remoteMean = decimal.Parse(parameters[1]);
	                var remoteCount = decimal.Parse(parameters[2]);
	                _allMeans[remoteHost] = remoteMean;
	                _allCounts[remoteHost] = remoteCount;
					
					
                };
            });
            while (true)
            {
				//evaluate my local average and count
				Random rand = new Random(DateTime.Now.Millisecond);
	            _localMean = 234.00m + (rand.Next(0, 100) - 50)/10.0m;
	            _localCount = 222m + rand.Next(0, 100);

				//evaluate my local aggregate average using means and counts sent from all other nodes
				//could publish aggregate averages to other nodes, if you wanted to monitor disagreement between nodes
	            var total_mean_times_count = 0m;
	            var total_count = 0m;
	            foreach (var server in serverAddresses)
	            {
		            total_mean_times_count += _allCounts[server.Key]*_allMeans[server.Key];
					total_count += _allCounts[server.Key];
	            }
				//add on local mean and count which were removed from the server list earlier, so won't be processed
	            total_mean_times_count += (_localMean * _localCount);
	            total_count = total_count + _localCount;

	            _localAggregate_average = (total_mean_times_count/total_count);
	            _localAggregate_count = total_count;

				Console.WriteLine("local aggregate average = {0}", _localAggregate_average);

				System.Threading.Thread.Sleep(10000);
                foreach (var serverAddress in serverAddresses)
                {
	                using (var wscli = new ClientWebSocket())
	                {
		                var tokSrc = new CancellationTokenSource();
		                using (var task = wscli.ConnectAsync(new Uri(serverAddress.Value), tokSrc.Token))
		                {
			                task.Wait();
		                }

						using (var task = wscli.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(thisNodeName+"~"+_localMean + "~"+_localCount)),
							WebSocketMessageType.Text,
							false,
							tokSrc.Token
							))
						{
							task.Wait();
						}
	                }
                
                }
            }
        }


		
    }
}