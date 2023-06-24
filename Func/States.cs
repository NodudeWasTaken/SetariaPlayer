using SetariaPlayer.Func;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Buttplug.DeviceMessage.Types;
using static Buttplug.ServerMessage.Types;

namespace SetariaPlayer
{
	enum State
	{
		Inactive,
		ScenePlaying,
	};
	class StateInt
	{
		public string name;
		public string urlPrefix;
		protected ButtplugInt b;
		protected ScriptPlayer sp;
		protected ScriptParser sr;
		public StateInt(ButtplugInt b, ScriptPlayer sp, ScriptParser sr) {
			this.b = b;
			this.sp = sp;
			this.sr = sr;
		}
		public virtual void Enter(HttpListenerRequest r, ref State s) { }
		public virtual bool shouldEnter(HttpListenerRequest r)
		{
			return r.Url.AbsolutePath.Equals(this.urlPrefix);
		}
		public virtual void Update(HttpListenerRequest r, ref State s) { }
		public virtual void Update() { }
		public virtual void Pause() { sp.Pause(); }
		public virtual void Resume() { sp.Resume(); }
		public virtual void Exit(HttpListenerRequest r, ref State s) { }
	}
	class InactiveState : StateInt
	{
		public float hp;
		public float mp;
		private Filler fi;

		public InactiveState(ButtplugInt b, ScriptPlayer sp, ScriptParser sr) : base(b, sp, sr) {
			this.name = "Inactive";
			this.urlPrefix = "/game/player_damage";
			fi = new Filler(sp, () => Config.cfg.filler);
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			if (Config.cfg.filler)
				fi.Start();
		}
		public override void Update() {
			base.Update();

			if (!Config.cfg.filler) {
				if (fi.Running())
					fi.Stop();
			} else {
				if (!fi.Running())
					fi.Start();
			}
			//TODO: oDeathRoom and oCyclops
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			this.Update();
			if (r.QueryString.HasKeys()) {
				foreach (string qs in r.QueryString.AllKeys) {
					if (qs == "hp")
						hp = float.Parse(r.QueryString[qs], CultureInfo.InvariantCulture);
					if (qs == "mp")
						mp = float.Parse(r.QueryString[qs], CultureInfo.InvariantCulture);
				}
			}

			if (r.Url.AbsolutePath.Equals("/game/fire"))
				fi.Fire();
			if (r.Url.AbsolutePath.Equals("/game/fire_lazer"))
				fi.Lazer();
			if (r.Url.AbsolutePath.Equals("/game/player_damage"))
				fi.Damage();
		}
		public override void Pause() {
			base.Pause();
			fi.Stop();
		}
		public override void Resume() {
			base.Resume();
			if (Config.cfg.filler)
				fi.Start();
		}
		public override void Exit(HttpListenerRequest r, ref State s) {
			base.Exit(r, ref s);
			fi.Stop();
			sp.Stop();
		}
	}
	class DeathRoomState : StateInt {
		private long time = 0;
		public DeathRoomState(ButtplugInt b, ScriptPlayer sp, ScriptParser sr) : base(b, sp, sr) {
			this.name = "DeathRoom";
			this.urlPrefix = "/game/custom_DeathRoomGirl";
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			this.Update(r, ref s);
			sp.Play(sr.get("sHFairy1", "start"), false);
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			if (r.Url.AbsolutePath.Equals("/game/custom_DeathRoomGirl_stop") || r.Url.AbsolutePath.Equals("/game/gallery_stop")) {
				this.Exit(r, ref s);
				return;
			}

			if (r.Url.AbsolutePath.Equals("/game/custom_DeathRoomGirl_action")) {
				float animflag = float.Parse(r.QueryString["anim_flag"], CultureInfo.InvariantCulture);
				if (animflag == 7) {
					//Max update time is 1/10 a second
					if ((time + Config.cfg.fillerAModDamageLength) < Utilities.curtime()) {
						this.sp.Play(Filler.getDamageScript());
						time = Utilities.curtime();
					}
				}
			}
		}
	}
	class Fairy1State : StateInt {
		private float max = -569;
		private float min = -469;
		private long time = 0;
		public Fairy1State(ButtplugInt b, ScriptPlayer sp, ScriptParser sr) : base(b, sp, sr) {
			this.name = "Fairy1 player";
			this.urlPrefix = "/game/custom_HFairy1";
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			this.Update(r, ref s);
			sp.Play(sr.get("sHFairy1", "start"), false);
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			if (r.Url.AbsolutePath.Equals("/game/custom_HFairy1_stop") || r.Url.AbsolutePath.Equals("/game/gallery_stop")) {
				this.Exit(r, ref s);
				return;
			}

			if (r.Url.AbsolutePath.Equals("/game/custom_HFairy1_finish")) {
				sp.Play(sr.get("sHFairy1", "finish"), false);
				return;
			}

			/*if (!r.QueryString.HasKeys()) {
				Trace.WriteLine("Missing query string!");
				return;
			}*/

			if (r.Url.AbsolutePath.Equals("/game/custom_HFairy1_action")) {
				float bonepos = float.Parse(r.QueryString["bone_pos_y"], CultureInfo.InvariantCulture);
				max = Math.Min(max, bonepos);
				min = Math.Max(min, bonepos);

				float p = 1-Math.Abs((bonepos - min) / (max - min));

				//Max update time is 1/10 a second
				if (time + 100 < Utilities.curtime()) {
					this.b.client.Devices.AsParallel().ForAll(device => {
						if (device.AllowedMessages.ContainsKey(MessageAttributeType.LinearCmd)) {
							Trace.WriteLine($"HFairy1 action {p}");
							//TODO: We can probably calculate accceleration
							device.SendLinearCmd(150, p);
						} else if (device.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd)) {
							device.SendVibrateCmd(p);
						}
					});
					time = Utilities.curtime();
				}
			}
		}
		public override void Exit(HttpListenerRequest r, ref State s) {
			base.Exit(r, ref s);
			sp.Stop();
		}
	}
	class SceneState : StateInt {
		private string lastmob = "";
		private Data curscript = null;
		public SceneState(ButtplugInt b, ScriptPlayer sp, ScriptParser sr) : base(b,sp,sr) {
			this.name = "Scene player";
			this.urlPrefix = "/game/gallery";
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			base.Enter(r, ref s);
			s = State.ScenePlaying;
			lastmob = "";
			this.Update(r,ref s);
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			base.Update(r, ref s);
			if (r.Url.AbsolutePath.Equals("/game/gallery_stop") || 
				(!r.Url.AbsolutePath.StartsWith("/game/gallery") && !r.Url.AbsolutePath.StartsWith("/game/player_damage2"))
			) {
				//player_damage2 is end animation damage
				this.Exit(r, ref s);
				return;
			}
			if (!r.QueryString.HasKeys()) {
				Trace.WriteLine("Missing query string!");
				return;
			}

			string mob = r.QueryString["anim_name"];
			string animation_scene = r.QueryString["anim_scene"];
			float transition_time = 0;
			if (r.QueryString.AllKeys.Contains("anim_dur"))
				transition_time = float.Parse(r.QueryString["anim_dur"], CultureInfo.InvariantCulture);
			float animation_speed = float.Parse(r.QueryString["anim_speed"], CultureInfo.InvariantCulture);
			/*
	Var enemy_id  =302
	Var enemy_name=GHMojya
	Var enemy_skin=default
	Var anim_id   =184
	Var anim_name =sHMojya
	Var anim_speed=1
	Var anim_scene=finish
	Var anim_dur  =0.50
	Var hp        =3
	Var mp        =1
	Var armor_state=3
	Var anim_flag =7
			*/

			/*
			 * Sandwitch trap hotfix
			 * It slows down the animation speed instead of having a finish scene
			 * So we here detect this and overwrite it.
			 */
			if (mob == "sHSandWitchTrap" && r.QueryString["enemy_skin"] == "finish") {
				mob = "sHSandWitchTrap;enemy_skin=finish";
				animation_scene = "scene1";
				animation_speed = 1f;
			}

			var script = sr.get(mob, animation_scene);
			if (script == null) {
				Trace.WriteLine($"Missing script for {mob} {animation_scene}!");
				sp.Stop();
				return;
			}

			if (curscript != null && curscript.GetId() == script.GetId()) {
				Trace.WriteLine($"Already playing {mob} {animation_scene}!");
				sp.setTimeScale(animation_speed);
				return;
			}

			//If this wasn't a scene-switch, dont delay
			if (mob != lastmob) {
				transition_time = 0;
			}
			if (lastmob != "") {
				sp.Stop();
			}
			lastmob = mob;

			foreach (var d in b.client.Devices) {
				Trace.WriteLine($"Device name: {d.Name}");
			}

			int shift = (int)(transition_time * 1000.0);
			//TODO: Fix
			shift = 0;
			sp.Play(script, null, shift);
			sp.setTimeScale(animation_speed);
			curscript = script;
		}
		public override void Exit(HttpListenerRequest r, ref State s) {
			base.Exit(r, ref s);
			sp.Stop();
			s = State.Inactive;
			lastmob = "";
			curscript = null;
		}
	}
}
