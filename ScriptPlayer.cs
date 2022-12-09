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
		//Some example actions
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
	class ScriptPlayer {
		//The current worker task
		private Task playTask;
		private CancellationTokenSource playTaskTS;
		//Buttplug instance
		private ButtplugInt butt;
		//Heatmapper for vibration device compatibility
		private VibrationConvert buttvib = new VibrationConvert();
		//A timer with scale support
		private TimeStretcher time = new TimeStretcher();
		//.
		private bool paused = false;

		public ScriptPlayer(ButtplugInt bp) {
			butt = bp;
		}

		/*
		 * Converts a list of (Time,Position) to (Duration,Position) by previous index
		 */
		private IEnumerable<(int, double)> buttConverter(List<(long, int)> ac) {
			long rt = 0;
			foreach (var a in ac) {
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
		private IEnumerable<(int, double)> buttTimer(List<(long, int)> ac, long stime) {
			foreach (var a in ac) {
				//Get the time index
				long idx = a.Item1;
				//Get the scaling factor
				double scaleF = Math.Pow(time.getScale(), -1);
				//Calculate the duration
				int dur = (int)(((idx + stime) - time.get()) * scaleF);
				//Calculate the position in a 0-1 range
				double pos = (double)(a.Item2 / 100.0);

				//Feed the vibration converter
				buttvib.Update((idx, pos));
				//Yield
				yield return (dur,pos);
			}
		}
		//Does what it says
		public void setTimeScale(double scale) {
			time.setScale(scale);
		}
		/*
		 * Plays a given script with optional arguments
		 * 
		 * Args:
		 * Script data.
		 * If you want the script to loop.
		 * If you want an time offset.
		 */
		public void Play(Data current_script, bool? loopOverride=null, int offset=0) {
			//The start time which we should respect when playing the script
			long current_time =Utilities.curtime();
			//Reset the timer
			time.reset();

			//Was canceled, but isn't done yet
			if (playTask != null) {
				//Ask it to stop
				if (!playTaskTS.IsCancellationRequested)
					this.Stop();
				//Wait for it to stop
				playTask.Wait();
			}

			//TODO: Detect if too fast for linear device and vibrate instead
			//TODO: Timing drifts heavily at the end of finish animations

			//You cannot be paused if you want to play a script
			paused = false;
			//long pausefix = 0;

			//Start our task
			playTaskTS = new CancellationTokenSource();
			playTask = Task.Run(() => {
				//For all connected devices
				butt.client.Devices.AsParallel().ForAll(device => {
					CancellationToken ct = playTaskTS.Token;

					//Remember the old intensity, for reducing updates
					double oldIntensity = 0;
					//The globals should only be updated by 1 instance
					//In this case the first one
					bool ismaster = device.Index == butt.client.Devices[0].Index;

					//While should run
					while (!ct.IsCancellationRequested)
					{
						//For each action in the script
						foreach (var a in buttTimer(current_script.Actions, current_time + offset))
						{
							//Action duration
							int dur = a.Item1;
							//Action position
							double pos = a.Item2;
							//Action intensity (for vibration devices)
							double intensity = buttvib.Get();

#if DEBUG
							Console.WriteLine("DEBUG Action: {0} {1}", dur, pos);
#endif
							//If the action is too fast, ignore
							if (dur < 50)
								continue;

							//Play action
							if (device.AllowedMessages.ContainsKey(MessageAttributeType.LinearCmd)) {
								device.SendLinearCmd((uint)dur, pos);
							}
							else if (device.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd))
							{
								//Only update vibration if difference is bigger than 20%
								if (Utilities.diff(oldIntensity, intensity) > 0.2) { 
									device.SendVibrateCmd(intensity);
									oldIntensity = intensity;
								}
							}
#if DEBUG
							Console.WriteLine("DEBUG Linear: {0} {1}", dur, pos);
							Console.WriteLine("DEBUG Vibrate: {0} {1}", dur, intensity);
#endif
							//Wait until the action should be done
							long taken = Utilities.curtime() + dur;
							while (taken > Utilities.curtime() || paused) {
								Thread.Sleep(1);
								//If we should exit
								if (ct.IsCancellationRequested) return;
								//If we paused (cannot happen in Setaria)
								if (paused) { 
									taken += 1; 
									oldIntensity = 0.0; 
									//TODO: In Setaria this is impossible
									//Except in the gallery, where the animation doesnt pause
									//But the signal is still sent
									//Weird
									/*if (ismaster)
										pausefix += 1;*/
								}
							}
						}

						//If user choose if should loop
						if (loopOverride == null) {
							if (!current_script.Loop)
								return;
						} else {
							if (loopOverride == false)
								return;
						}

						//If we are the main process, update the time
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
		public void Stop() {
			if (playTaskTS != null)
				playTaskTS.Cancel();
			foreach (var i in butt.client.Devices)
				i.SendStopDeviceCmd();
			buttvib.Clear();
		}
	}
}
