using Buttplug;
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
			// Handle requests
			listenTask = HandleButtplug();
		}
		public void Stop()
		{
			StopButtplug().Wait();
		}

		private async Task StopButtplug()
		{
			//            ButtplugFFILog.SetLogOptions(ButtplugLogLevel.Debug, true);
			await client.StopScanningAsync().ConfigureAwait(false);
			/*
            while (true) {
                foreach (var device in client.Devices) {
                    if (device.AllowedMessages.ContainsKey(ServerMessage.Types.MessageAttributeType.BatteryLevelCmd))
                    {
                        Console.WriteLine("Fetching Battery");
                        Console.WriteLine($"Battery: {await device.SendBatteryLevelCmd()}");
                        //await device.SendRawWriteCmd(Endpoint.Tx, Encoding.ASCII.GetBytes("Vibrate:10;"), false);
                        //await device.SendVibrateCmd(0.5);
                        await Task.Delay(500);
                        //await device.SendStopDeviceCmd();
                        //await device.SendRawWriteCmd(Endpoint.Tx, System.Text.Encoding.ASCII.GetBytes("Air:Level:3;"), false);
                    }
                }
            }
            */
			Trace.WriteLine("killing log?");
			ButtplugFFILog.SetLogOptions(ButtplugLogLevel.Off, true);
			client.Dispose();
			client = null;
		}

		private async Task HandleButtplug()
		{
			//ButtplugFFILog.LogMessage += (aObj, aMsg) => { Trace.WriteLine($"LOG: {aMsg}"); };
			//ButtplugFFILog.SetLogOptions(ButtplugLogLevel.Info, true);
			client = new ButtplugClient("Setaria Plugin");
			client.DeviceAdded += (obj, args) =>
			{
				var device = args.Device;
				Trace.WriteLine($"Device Added: {device.Name}");
				foreach (var msg in args.Device.AllowedMessages)
				{
					Trace.WriteLine($"{msg.Key} {msg.Value}");
					/*foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(msg.Value))
					{
						string name = descriptor.Name;
						object value = descriptor.GetValue(obj);
						Trace.WriteLine("{0}={1}", name, value);
					}*/
				}
				//await device.SendVibrateCmd(1.0);
			};
			client.DeviceRemoved += (obj, args) =>
			{
				Trace.WriteLine($"Device removed: {args.Device.Name}");
			};
			client.ScanningFinished += (obj, args) =>
			{
				Trace.WriteLine("Scanning finished.");
			};
			client.ServerDisconnect += (obj, args) =>
			{
				Trace.WriteLine("Server disconnected.");
			};
			ButtplugEmbeddedConnectorOptions options = null;
			//options.AllowRawMessages = true;
			await client.ConnectAsync(options).ConfigureAwait(false);
			await client.StartScanningAsync().ConfigureAwait(false);
			Trace.WriteLine($"Is Scanning: {client.IsScanning}");
		}
	}
}
