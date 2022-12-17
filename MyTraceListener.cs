﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static System.Net.Mime.MediaTypeNames;

namespace SetariaPlayer {
	public class MyTraceListener : TraceListener {
		private TextBoxBase output;

		public MyTraceListener(TextBoxBase output) {
			this.Name = "Trace";
			this.output = output;
		}


		public override void Write(string message) {
			Action append = delegate () {
				output.AppendText(string.Format("[{0}] ", DateTime.Now.ToString()));
				output.AppendText(message);
			};
			append();

		}

		public override void WriteLine(string message) {
			Write(message + Environment.NewLine);
		}
	}

	public class ScrollingTextBox : TextBox {
		protected override void OnInitialized(EventArgs e) {
			base.OnInitialized(e);
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
		}

		protected override void OnTextChanged(TextChangedEventArgs e) {
			//TODO: CaretIndex is expensive, update only in some limit
			base.OnTextChanged(e);
			CaretIndex = Text.Length;
			ScrollToEnd();
		}

	}
}
