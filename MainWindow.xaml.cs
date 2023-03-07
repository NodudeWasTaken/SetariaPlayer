using Buttplug;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static Buttplug.ServerMessage.Types;

namespace SetariaPlayer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string version = "InDev Build 17";
		public static bool started = false;
		private bool ready = false;
		private ButtplugInt b;
		private ScriptParser sr;
		private HttpServer h;
		private ScriptPlayer sp;

		private List<StateInt> states = new List<StateInt>();
		private StateInt inactive;
		private State state = State.Inactive;
		private StateInt activeState;
		public IList<string> ListOfDevices { 
			get {
				if (b == null || b.client == null) {
					return new List<string>();
				}
				return b.client.Devices.Select((d) => $"{d.Index}: {d.Name} ({MessageTypes(d)})").ToList();
			}
		}
		private string MessageTypes(ButtplugClientDevice d) {
			List<string> l = new List<string>();

			if (d.AllowedMessages.ContainsKey(MessageAttributeType.LinearCmd)){
				l.Add("Stroke");
			}
			if (d.AllowedMessages.ContainsKey(MessageAttributeType.VibrateCmd)){
				l.Add("Vibrate");
			}
			if (d.AllowedMessages.ContainsKey(MessageAttributeType.RotateCmd)) {
				l.Add("Rotate");
			}
			return l.Aggregate("", (current, next) => {
				string mid = current.Length == 0 ? "" : ", ";
				return $"{current}{mid}{next}";
			});
		}
		private void LogIt() {
			Trace.Listeners.Add(new TextWriterTraceListener("SetariaPlayer.log"));
			Trace.AutoFlush = true;
			Trace.Indent();
			Trace.WriteLine("Entering Main");
		}

		public MainWindow() {
			LogIt();
			InitializeComponent();
			this.DataContext = this;

			Config.cfg = Config.load();

			this.b = new ButtplugInt();
			b.Start();

			var handleItemUpdate = () => {
				this.Dispatcher.Invoke(() => {
					DeviceList.Items.Clear();
					foreach (var s in ListOfDevices) {
						DeviceList.Items.Add(s);
					}
				});
			};
			this.b.client.DeviceAdded += (obj, args) => { handleItemUpdate(); };
			this.b.client.DeviceRemoved += (obj, args) => { handleItemUpdate(); };

			this.sr = new ScriptParser();
			this.h = new HttpServer("http://127.0.0.1:5050/");
			this.sp = new ScriptPlayer(b);

			this.inactive = new InactiveState(b, sp, sr);
			this.states.Add(new SceneState(b, sp, sr));
			this.states.Add(new Fairy1State(b, sp, sr));
			this.state = State.Inactive;
			this.activeState = inactive;
			sp.Pause();

			this.h.Hook(HttpHookCallback);
			h.Start();

			Title += $" ({version})";
		}
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			Trace.WriteLine("Window_Loaded");

			BufferVal.Value = Config.cfg.vibrationBufferDuration / 1000;
			DiffVal.Value = Config.cfg.vibrationUpdateDiff * 100;
			DiffVal_Copy.Value = Config.cfg.vibrationCalcDiff * 100;
			SpeedVal.Value = Config.cfg.vibrationMaxSpeed;
			FillerCheckbox.IsChecked = Config.cfg.filler;
			FillerDuration.Value = Config.cfg.fillerDur;
			FillerHeight.Value = Config.cfg.fillerHeight;
			MaxStrokeLength.Value = Config.cfg.strokeMax / 100;
			MinStrokeLength.Value = Config.cfg.strokeMin / 100;
			//Trace.Listeners.Add(new MyTraceListener(Logs));

			foreach (var f in Directory.GetFiles(".", "*.funscript"))
			{
				ListBoxItem itm = new ListBoxItem();
				itm.Content = f;
				Scripts.Items.Add(itm);
			}


			ready = true;
		}
		private void ImDying(Action a) {
			if (ready) {
				a.Invoke();
				if (this.activeState != null) {
					this.activeState.Update();
				}
			}
		}
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			ImDying(() => {
				Config.cfg.vibrationBufferDuration = (int)(e.NewValue * 1000);
			});
		}
		private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e) {
			ImDying(() => {
				Config.cfg.vibrationUpdateDiff = e.NewValue / 100.0;
			});
		}
		private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e) {
			ImDying(() => {
				Config.cfg.vibrationMaxSpeed = e.NewValue;
			});
		}
		private void Slider_ValueChanged_3(object sender, RoutedPropertyChangedEventArgs<double> e) {
			ImDying(() => {
				Config.cfg.vibrationCalcDiff = e.NewValue / 100.0;
			});
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Trace.WriteLine(StartButton.Content.ToString());
			if (StartButton.Content.ToString() == "Stop") {
				//TODO: Use some force-stop mechanism instead?
				started = false;
				//sp.Pause();
				StartButton.Content = "Start";
			} else {
				started = true;
				//sp.Resume();
				StartButton.Content = "Stop";
			}
		}
		private void Button2_Click(object sender, RoutedEventArgs e) {
			Trace.WriteLine("Config save");
			Config.cfg.save();
		}
		private string HttpHookCallback(HttpListenerRequest r)
		{
			if (r.HttpMethod == "POST")
			{
				Trace.WriteLine($"Url: {r.Url.AbsolutePath}");
				//TODO: HttpServer.Error Value cannot be null. (Parameter 's')
				if (r.QueryString.HasKeys()) {
					foreach (string s in r.QueryString.AllKeys) {
						Trace.WriteLine($"Var {s}={r.QueryString[s]}");
					}
				}

				if (r.Url.AbsolutePath.Equals("/game/pause")) {
					activeState.Pause();
					return "OK";
				} else if (r.Url.AbsolutePath.Equals("/game/resume")) {
					activeState.Resume();
					return "OK";
				}

				State oldstate = state;
				activeState.Update(r, ref state);
				if (oldstate != State.Inactive && state == State.Inactive) {
					Trace.WriteLine($"Exit State: {activeState.name}");
					sp.Stop();
					//activeState.Exit(r, ref state);
					activeState = inactive;
					activeState.Enter(r, ref state);
					StatusBarText.Text = "Player State: " + activeState.name;
				}


				//If it didn't exit
				if (state != State.Inactive) {
					return "OK";
				} else {
					foreach (var st in states) { 
						//TODO: This isn't an entirely nice way to do this
						if (st.shouldEnter(r)) {
							activeState.Exit(r, ref state);
							st.Enter(r, ref state);
							Trace.WriteLine($"Enter State: {st.name}");
							activeState = st;
							StatusBarText.Text = "Player State: " + st.name;
							return "OK";
						}
					}
				}
			}
			return "KO";
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e) {
			ImDying(() => {
				Config.cfg.filler = FillerCheckbox.IsChecked == true;
			});
		}

		private void FillerDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			ImDying(() => {
				Config.cfg.fillerDur = (int)FillerDuration.Value;
			});
		}

		private void FillerHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			ImDying(() => { 
				Config.cfg.fillerHeight = (int)FillerHeight.Value; 
			});
		}
		private void MaxStrokeLength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			ImDying(() => {
				Config.cfg.strokeMax = MaxStrokeLength.Value / 100;
			});
		}
		private void MinStrokeLength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			ImDying(() => {
				Config.cfg.strokeMin = MinStrokeLength.Value / 100;
			});
		}

		private void Button_Click_1(object sender, RoutedEventArgs e) {
			new Task(async () => {
				await this.b.client.StopScanningAsync();
				await this.b.client.StartScanningAsync();
			}).Start();
		}

		private void Scripts_MouseDown(object sender, MouseButtonEventArgs e) {
			var item = ItemsControl.ContainerFromElement(sender as ListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
			if (item != null) {
				string script = (string)item.Content;
				Trace.WriteLine($"Load script: {script}");
				Config.cfg.scriptPath = script;
				this.sr.Load(script);
			}
		}

		private void ConnectionURL_TextChanged(object sender, TextChangedEventArgs e) {
			Config.cfg.intifaceUrl = ConnectionURL.Text;
		}
	}
}
