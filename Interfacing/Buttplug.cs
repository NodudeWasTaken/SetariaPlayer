using Buttplug.Client;
using Buttplug.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SetariaPlayer
{
	class ButtplugInt
	{
		public ButtplugClient client { get; private set; }
		private Task listenTask;

		public void Start()
		{
			client = new ButtplugClient("Setaria Plugin");
			client.DeviceAdded += (obj, args) => {
				var device = args.Device;

				Trace.WriteLine($"Device Added: {device.Name}");
				foreach (var feature in args.Device.Features) {
					Trace.WriteLine($"Feature {feature.Key}: {feature.Value.FeatureDescription}");
				}
			};
			client.DeviceRemoved += (obj, args) => {
				Trace.WriteLine($"Device removed: {args.Device.Name}");
			};
			client.ScanningFinished += (obj, args) => {
				Trace.WriteLine("Scanning finished.");
			};
			client.ServerDisconnect += (obj, args) => {
				Trace.WriteLine("Server disconnected.");
			};

			listenTask = HandleButtplug();
		}
		public void Refresh() {
			listenTask = HandleConnect();
		}
		public void Stop()
		{
			StopButtplug().Wait();
		}

		private async Task StopButtplug()
		{
			try {
				await client.StopScanningAsync().ConfigureAwait(false);
			} catch (Exception e) {
				Trace.WriteLine(e);
			}
			client.Dispose();
			client = null;
		}

		private async Task HandleButtplug()
		{
			await HandleConnect();
			try {
				await client.StartScanningAsync().ConfigureAwait(true);
			} catch (Exception e) {
				Trace.WriteLine("Failed to start scanning.");
				Trace.WriteLine(e);
			}
		}
		public async Task HandleConnect() {
			Trace.WriteLine("Trying to connect Intiface");
			try {
				string url = String.IsNullOrEmpty(Config.cfg.intifaceUrl) ? "ws://localhost:12345" : Config.cfg.intifaceUrl;
				var connector = new ButtplugWebsocketConnector(new Uri(url));
				await client.ConnectAsync(connector).ConfigureAwait(true);
				Trace.WriteLine("Intiface Connected!");
			} catch (ButtplugClientConnectorException e) {
				Trace.WriteLine("Failed to connect to intiface!");
				Trace.WriteLine(e);
				return;
			} catch (ButtplugException e) {
				Trace.WriteLine("Buttplug error during connect.");
				Trace.WriteLine(e);
				return;
			}
		}
	}
}
