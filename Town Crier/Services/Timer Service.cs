using System;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TownCrier.Services
{
	public class Timer_Service
	{
		private Timer Clock { get; set; } = new Timer();
		/// <summary>
		/// This event is fired every 15 minutes after the bot goes online.
		/// </summary>
		/// <param name="sender">Current DateTime that the event was fired at.</param>
		/// <param name="e">Service provider, you can get all available singletons form here.</param>
		public event EventHandler<IServiceProvider> OnClockInterval;
		private readonly IServiceProvider _provider;
		private readonly IConfiguration _config;
		public Timer_Service(IServiceProvider provider,IConfiguration config)
		{
			_provider = provider;
			_config = config;

			Clock.Interval = int.Parse(_config["timerInterval"]);
			Clock.AutoReset = true;
			Clock.Elapsed += TickTock;
			Clock.Start();
		}

		private void TickTock(object sender, ElapsedEventArgs e)
		{
			OnClockInterval?.Invoke(DateTime.Now, _provider);
		}
	}
}
