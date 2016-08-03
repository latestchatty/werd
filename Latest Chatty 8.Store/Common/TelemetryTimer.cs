
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Latest_Chatty_8.Common
{
	//HockeyApp doesn't currently provide any metric similar to this, so I guess we're not going to track timing on anything...
//	public class TelemetryTimer
//	{
//		private string name;
//		private Stopwatch timer;
//		Dictionary<string, string> additionalTelemetryInfo = null;

//        public TelemetryTimer(string name)
//			:this(name, null) {}

//		public TelemetryTimer(string name, Dictionary<string, string> additionalTelemetryInfo)
//		{
//			this.name = name;
//			this.timer = new Stopwatch();
//			this.additionalTelemetryInfo = additionalTelemetryInfo;
//		}

//		public void Start()
//		{
//			this.timer.Start();
//		}

//		public void Stop()
//		{
//			this.timer.Stop();
//			//var tc = new TelemetryClient();
//			//tc.TrackMetric(this.name, this.timer.ElapsedMilliseconds, this.additionalTelemetryInfo);
//		}

//	}
}
