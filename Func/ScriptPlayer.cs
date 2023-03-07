using Buttplug;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Buttplug.ServerMessage.Types;

namespace SetariaPlayer
{
	class Script {
		//Some example actions
		public static Data vibrate_weak = new Data("vibrate","weak",0,700,false, 
		new List<(long, int)> {
			(350, 10),
			(700, 0),
		});
		public static Data vibrate_mid = new Data("vibrate", "mid", 0, 500, false,
		new List<(long, int)> {
			(250, 10),
			(500, 0),
		});
		public static Data vibrate_strong = new Data("vibrate", "strong", 0, 200, false,
		new List<(long, int)> {
			(100, 10),
			(200, 0),
		});
		public static Data vibrate_ultra = new Data("vibrate", "ultra", 0, 140, false,
		new List<(long, int)> {
			(70, 10),
			(140, 0),
		});

		//TODO: Move to funscript
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
		public bool paused = false;
		public bool playing = false;

		public ScriptPlayer(ButtplugInt bp) {
			butt = bp;
		}

		/*
		 * Converts a list of (Time,Position) to (Duration,Position) by previous index
		 */
		/*private IEnumerable<(int, double)> buttConverter(List<(long, int)> ac) {
			long rt = 0;
			foreach (var a in ac) {
				yield return (
					(int)(a.Item1 - rt),
					(double)(a.Item2 / 100.0)
				);
				rt = a.Item1;
			}
		}*/

		/*
		 * Converts a list of (Time,Position) to (Duration,Position) by timing
		 */
		private IEnumerable<(int, double)> buttTimer(List<(long, int)> ac, long stime) {
			for (var i=0; i<ac.Count; i++) {
				var a = ac[i];

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
		private void StopSig(ButtplugClientDevice device) {
			device.SendStopDeviceCmd();
			playing = false;
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

			//Start our task
			playTaskTS = new CancellationTokenSource();
			playTask = Task.Run(() => {
				//For all connected devices
				CancellationToken ct = playTaskTS.Token;
				playing = true;
				while (butt.client.Devices.Length <= 0 && !ct.IsCancellationRequested) {
					Thread.Sleep(1);
				}

				//Remember the old intensity, for reducing updates
				double oldIntensity = 0;

				//While should run
				while (!ct.IsCancellationRequested && playing)
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

						//Limit stroke range
						pos = (pos * (Config.cfg.strokeMax - Config.cfg.strokeMin)) + Config.cfg.strokeMin;

						//TODO: Limit acceleration

#if DEBUG
						//Trace.WriteLine($"DEBUG Action: {dur} {pos}");
#endif
						//If the action is too fast, ignore
						//double speed = VibrationConvert.ActionSpeed((old_pos, old_dur), (pos, dur));
						if (dur < 50)
							continue;

						if (MainWindow.started && !this.paused) {
							//Play action
							butt.client.Devices.AsParallel().ForAll(device => {
								if (device.AllowedMessages.ContainsKey(MessageAttributeType.LinearCmd)) {
									device.SendLinearCmd((uint)dur, pos);
								} else if (device.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd)) {
									//Only update vibration if difference is bigger than x%
									if (Utilities.diff(oldIntensity, intensity) > Config.cfg.vibrationUpdateDiff) {
										device.SendVibrateCmd(intensity);
										oldIntensity = intensity;
									}
								}/* else if (device.AllowedMessages.ContainsKey(MessageAttributeType.RotateCmd)) {
								}*/
							});
						}
#if DEBUG
						//Trace.WriteLine($"DEBUG Linear: {dur} {pos}");
						//Trace.WriteLine($"DEBUG Vibrate: {dur} {intensity}");
#endif
						//Wait until the action should be done
						long taken = Utilities.curtime() + dur;
						while (taken > Utilities.curtime() || paused) {
							Thread.Sleep(1);
							//If we should exit
							if (ct.IsCancellationRequested) {
								butt.client.Devices.AsParallel().ForAll(device => StopSig(device));
								return; 
							};
							//If we paused (cannot happen in Setaria)
							if (paused) { 
								taken += 1; 
								oldIntensity = 0.0; 
								//TODO: In Setaria this is impossible
								//Except in the gallery, where the animation doesnt pause
								//But the signal is still sent
								//Weird
							}
						}
					}

					//If user choose if should loop
					if (!current_script.Loop || (loopOverride != null && loopOverride == false)) {
						break;
					}

					//Update the time
					current_time += current_script.Duration();
				}

				//Make vibration devices stop
				butt.client.Devices.AsParallel().ForAll(device => StopSig(device));
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
			butt.client.Devices.AsParallel().ForAll(device => StopSig(device));
			buttvib.Clear();
		}
	}
}
