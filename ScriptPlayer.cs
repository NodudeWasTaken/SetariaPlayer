using Buttplug;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Buttplug.ServerMessage.Types;

namespace ConsoleApp1
{
	class Script {
		public static Data vibrate_weak = new Data("vibrate","weak",0,0,false, 
		new List<(long, int)> {
			(350, 10),
			(700, 0),
		});
		public static Data vibrate_mid = new Data("vibrate", "mid", 0, 0, false,
		new List<(long, int)> {
			(250, 10),
			(500, 0),
		});
		public static Data vibrate_strong = new Data("vibrate", "strong", 0, 0, false,
		new List<(long, int)> {
			(100, 10),
			(200, 0),
		});
		public static Data vibrate_ultra = new Data("vibrate", "ultra", 0, 0, false,
		new List<(long, int)> {
			(70, 10),
			(140, 0),
		});

		//TODO: Compile scripts
	}
	class ScriptPlayer
	{
		private Task playTask;
		private CancellationTokenSource playTaskTS;
		private ButtplugInt butt;
		private VibrationConvert buttvib = new VibrationConvert();
		private TimeStretcher time = new TimeStretcher();
		private bool paused = false;
		public ScriptPlayer(ButtplugInt bp) {
			butt = bp;
		}

		/*
		 * Converts a list of (Time,Position) to (Duration,Position) by previous index
		 */
		private IEnumerable<(int, double)> buttConverter(List<(long, int)> ac)
		{
			long rt = 0;
			foreach (var a in ac)
			{
				yield return (
					(int)(a.Item1 - rt),
					(double)(a.Item2 / 100.0)
				);
				rt = a.Item1;
			}
		}

		/*
		 * Converts a list of (Time,Position) to (Duration,Position) by timing
		 */
		private IEnumerable<(int, double)> buttTimer(List<(long, int)> ac, long time, Func<long> curtime)
		{
			foreach (var a in ac)
			{
				long idx = a.Item1;
				int dur = (int)((idx + time) - curtime());
				double pos = (double)(a.Item2 / 100.0);

				buttvib.Update((idx, pos));
				yield return (dur,pos);
			}
		}
		public void setTimeScale(double scale) {
			time.setScale(scale);
		}
		public void Play(Data current_script, bool? loopOverride=null, int offset=0)
		{
			//TODO: Offset support
			long current_time =Utilities.curtime();
			time.reset();

			//Was canceled, but isn't done yet
			if (playTask != null) {
				if (!playTaskTS.IsCancellationRequested)
					this.Stop();
				playTask.Wait();
			}

			//TODO: Detect if too fast for linear device and vibrate instead

			paused = false;
			long pausefix = 0;

			playTaskTS = new CancellationTokenSource();
			playTask = Task.Run(() => {
				butt.client.Devices.AsParallel().ForAll(device => {
					CancellationToken ct = playTaskTS.Token;
					double oldIntensity = 0;
					//The globals should only be updated by 1 instance
					//In this case the first one
					bool ismaster = device.Index == butt.client.Devices[0].Index;

					while (!ct.IsCancellationRequested)
					{
						//TODO: We only prolong the time,
						//Meaning the initial actions will have something close to the original duration
						//And slowmotion only takes effect some actions into the script
						foreach (var a in buttTimer(current_script.Actions, current_time, () => time.get() - pausefix))
						{
							int dur = a.Item1;
							double pos = a.Item2;
							double intensity = buttvib.Get();

#if DEBUG
							Console.WriteLine("DEBUG Action: {0} {1}", dur, pos);
#endif
							if (dur < 50)
								continue;

							if (device.AllowedMessages.ContainsKey(MessageAttributeType.LinearCmd))
							{
								device.SendLinearCmd((uint)dur, pos);
							}
							else if (device.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd))
							{
								if (Utilities.diff(oldIntensity, intensity) > 0.2) { 
									device.SendVibrateCmd(intensity);
									oldIntensity = intensity;
								}
							}
#if DEBUG
							Console.WriteLine("DEBUG Linear: {0} {1}", dur, pos);
							Console.WriteLine("DEBUG Vibrate: {0} {1}", dur, intensity);
#endif
							long taken = Utilities.curtime() + dur;
							while (taken > Utilities.curtime() || paused)
							{
								Thread.Sleep(1);
								if (ct.IsCancellationRequested) { return; }
								//TODO: Fucks up if multiple devices
								if (paused) { 
									taken += 1; 
									oldIntensity = 0.0; 
									if (ismaster)
										pausefix += 1; 
								}
							}
						}

						if (loopOverride == null) {
							if (!current_script.Loop)
								return;
						} else {
							if (loopOverride == false)
								return;
						}

						//TODO: Fucks up if multiple devices
						if (ismaster)
							current_time += current_script.End - current_script.Start;
					}

					//Make vibration devices stop
					device.SendStopDeviceCmd();
				});
			});
		}
		public void Pause() { 
			paused = true;
			foreach (var i in butt.client.Devices)
				if (i.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd))
					i.SendStopDeviceCmd();
		}
		public void Resume() { 
			paused = false; 
		}
		public void Stop()
		{
			if (playTaskTS != null)
				playTaskTS.Cancel();
			foreach (var i in butt.client.Devices)
				i.SendStopDeviceCmd();
			buttvib.Clear();
		}
	}
}
