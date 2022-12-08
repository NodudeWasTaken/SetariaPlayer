using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	enum State
	{
		Inactive,
		ScenePlaying,
	};
	class StateInt
	{
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
		public virtual void Pause() { sp.Pause(); }
		public virtual void Resume() { sp.Resume(); }
		public virtual void Exit(HttpListenerRequest r, ref State s) { }
	}
	class InactiveState : StateInt
	{
		public float hp;
		public float mp;
		public InactiveState(ButtplugInt b, ScriptPlayer sp, ScriptParser sr) : base(b, sp, sr)
		{
			this.urlPrefix = "/game/player_damage";
		}
		public override void Update(HttpListenerRequest r, ref State s)
		{
			if (r.QueryString.HasKeys()) {
				foreach (string qs in r.QueryString.AllKeys) {
					if (qs == "hp") {
						hp = float.Parse(r.QueryString[qs]);
					}
					if (qs == "mp") {
						mp = float.Parse(r.QueryString[qs]);
					}
				}
			}

			if (r.Url.AbsolutePath.Equals("/game/fire"))
			{
				sp.Play(Script.vibrate_strong);
			}
			if (r.Url.AbsolutePath.Equals("/game/player_damage"))
			{
				sp.Play(Script.vibrate_ultra);
			}
			//player_damage2 happens at the end of animations
			/*if (r.Url.AbsolutePath.Equals("/game/player_damage2"))
			{
				Console.WriteLine("How did you trigger this?");
			}*/
		}
		public override void Exit(HttpListenerRequest r, ref State s)
		{
			sp.Stop();
		}
	}
	class SceneState : StateInt
	{
		private string lastmob = "";
		private Data curscript = null;
		public SceneState(ButtplugInt b, ScriptPlayer sp, ScriptParser sr) : base(b,sp,sr) {
			this.urlPrefix = "/game/gallery";
		}
		public override void Enter(HttpListenerRequest r, ref State s) {
			s = State.ScenePlaying;
			lastmob = "";
			this.Update(r,ref s);
		}
		public override void Update(HttpListenerRequest r, ref State s) {
			if (r.Url.AbsolutePath.Equals("/game/gallery_stop") || 
				(!r.Url.AbsolutePath.StartsWith("/game/gallery") && !r.Url.AbsolutePath.StartsWith("/game/player_damage2"))
			) {
				this.Exit(r, ref s);
				return;
			}
			if (!r.QueryString.HasKeys())
			{
				Console.WriteLine("Missing query string!");
				return;
			}

			string mob = r.QueryString["anim_name"];
			string animation_scene = r.QueryString["anim_scene"];
			float transition_time = float.Parse(r.QueryString["anim_dur"], CultureInfo.InvariantCulture);
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
			var script = sr.get(mob, animation_scene);
			if (script == null)
			{
				Console.WriteLine("Missing script for {0} {1}!", mob, animation_scene);
				sp.Stop();
				return;
			}

			if (curscript != null && curscript.GetId() == script.GetId())
			{
				Console.WriteLine("Already playing {0} {1}!", mob, animation_scene);
				sp.setTimeScale(animation_speed);
				return;
			}

			//If this wasn't a scene-switch, dont delay
			if (mob != lastmob)
			{
				transition_time = 0;
			}
			if (lastmob != "")
			{
				sp.Stop();
			}
			lastmob = mob;

			foreach (var d in b.client.Devices)
			{
				Console.WriteLine("Device name: " + d.Name);
			}

			int shift = (int)(transition_time * 1000.0);
			sp.Play(script, null, shift);
			sp.setTimeScale(animation_speed);
			curscript = script;
		}
		public override void Exit(HttpListenerRequest r, ref State s)
		{
			sp.Stop();
			s = State.Inactive;
			lastmob = "";
			curscript = null;
		}
	}
}
