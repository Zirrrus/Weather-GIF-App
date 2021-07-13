using System;
using System.Threading;

namespace Weather_GIF_App
{
	class Program
	{
		static int intervals = 10;
		static int intervalSleepTime = 60000;

		static void Main(string[] args)
		{
			Console.WindowWidth = 200;

			int counter = intervals;
			while(true)
			{
				if (counter >= intervals)
				{
					WeatherGifSettings settings = new WeatherGifSettings(args);
					WeatherGifCreator wgc = new WeatherGifCreator(settings);
					wgc.GenerateGif();
					GC.Collect();
					counter = 0;
				}

				Console.WriteLine(" - " + (intervals - counter) + " minutes until next gif");
				counter++;
				Thread.Sleep(intervalSleepTime);
			}
		}
	}
}
