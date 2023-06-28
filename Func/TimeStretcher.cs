using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetariaPlayer
{
	class TimeStretcher
	{
		/*
		 * Keeps time with respect to a timescale
		 */
		private long m_time;
		private double m_scale = 1.0;
		private long m_offset = 0;
		public TimeStretcher() {
			this.m_time = Utilities.curtime();
			this.m_offset = this.m_time;
		}
		/*
		 * Gets the current time
		 */
		public long Get() {
			this.Update();
			return this.m_time;
		}
		/*
		 * Set the timescale and update the current time
		 */
		public void SetScale(double scale) {
			this.Update();
			this.m_scale = scale;
		}
		/*
		 * Get the timescale
		 */
		public double GetScale() {
			return this.m_scale;
		}
		/*
		 * Calculates the time since last update in realtime
		 */
		private long Diff() {
			return this.m_time + (Utilities.curtime() - this.m_offset);
		}
		/*
		 * Updates the stretched time
		 */
		public void Update() {
			this.m_time += (long)((Diff() - this.m_time) * this.m_scale);
			this.m_offset = Utilities.curtime();
		}
		/*
		 * Reset the current time
		 */
		public void Reset() {
			this.m_time = Utilities.curtime();
			this.m_offset = this.m_time;
			this.m_scale = 1.0;
		}
		public void Test() {
			TimeStretcher t = new TimeStretcher();
			Trace.WriteLine(string.Format("t0 {0}", Utilities.curtime() - t.Get()));
			t.SetScale(0.5);
			Thread.Sleep(1000);
			Trace.WriteLine(string.Format("t1 {0}", Utilities.curtime() - t.Get()));
			t.SetScale(1);
			Thread.Sleep(1000);
			Trace.WriteLine(string.Format("t2 {0}", Utilities.curtime() - t.Get()));
			t.SetScale(1.5);
			Thread.Sleep(1000);
			Trace.WriteLine(string.Format("t3 {0}", Utilities.curtime() - t.Get()));
			t.SetScale(1);
			Thread.Sleep(1000);
			Trace.WriteLine(string.Format("t4 {0}", Utilities.curtime() - t.Get()));
		}
	}
}
