using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;

namespace SetariaPlayer
{
	class HttpServer
	{
		private HttpListener listener;
		private Task listenTask;
		private bool enabled = true;
		private Func<HttpListenerRequest, string> hook;

		public HttpServer(string url) {
			listener = new HttpListener();
			listener.Prefixes.Add(url);

		}
		public async Task HandleIncomingConnections() {
			while (enabled) {
				HttpListenerContext ctx = await listener.GetContextAsync();
				HttpListenerRequest req = ctx.Request;
				HttpListenerResponse resp = ctx.Response;

				string responseString = "Internal Error";

				try {
					responseString = hook.Invoke(req);
				} catch (Exception ex) {
					Trace.WriteLine($"HttpServer.Error {ex.Message}");
				}

				byte[] data = System.Text.Encoding.UTF8.GetBytes(responseString);
				await resp.OutputStream.WriteAsync(data, 0, data.Length);
				resp.Close();
			}
		}

		public void Start() {
			listener.Start();

			// Handle requests
			listenTask = HandleIncomingConnections();
		}
		public void Hook(Func<HttpListenerRequest, string> hook) {
			this.hook = hook;
		}
		public void Stop() {
			enabled = false;

			// Close the listener
			listener.Stop();
			listener.Close();

			//TODO: Cannot stop
			//listenTask.Wait();
		}
	}
}