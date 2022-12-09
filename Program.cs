using ConsoleApp1;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;

string version = "InDev Build 8";
ButtplugInt b = new ButtplugInt();
HttpServer h = new HttpServer("http://127.0.0.1:5050/");
ScriptPlayer sp = new ScriptPlayer(b);
ScriptParser sr = new ScriptParser();

List<StateInt> states = new List<StateInt>();
StateInt inactive = new InactiveState(b, sp, sr);
states.Add(new SceneState(b, sp, sr));
State state = State.Inactive;
StateInt activeState = inactive;

h.Hook((r) => {
	if (r.HttpMethod == "POST")
	{
		Console.WriteLine("Url: {0}", r.Url.AbsolutePath);
		//TODO: HttpServer.Error Value cannot be null. (Parameter 's')
		if (r.QueryString.HasKeys())
		{
			foreach (string s in r.QueryString.AllKeys)
			{
				Console.WriteLine("Var {0,-10}={1}", s, r.QueryString[s]);
			}
		}

		if (state == State.Inactive)
		{
			activeState = inactive;
		}

		if (r.Url.AbsolutePath.Equals("/game/pause"))
		{
			activeState.Pause();
			return "OK";
		}
		else if (r.Url.AbsolutePath.Equals("/game/resume"))
		{
			activeState.Resume();
			return "OK";
		}

		activeState.Update(r, ref state);

		//If it didn't exit
		if (state != State.Inactive) {
			return "OK";
		} else {
			foreach (var s in states)
			{
				//TODO: This isn't an entirely nice way to do this
				if (s.shouldEnter(r))
				{
					s.Enter(r, ref state);
					Console.WriteLine("Enter State: ", state.GetType().Name);
					activeState = s;
					return "OK";
				}
			}
		}
	}
	return "KO";
});

h.Start();
b.Start();

Console.WriteLine("Welcome to {0}!", version);
Console.WriteLine("Press enter to exit!");
Console.ReadLine();
h.Stop();
b.Stop();
