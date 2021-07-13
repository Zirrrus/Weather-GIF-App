using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;

namespace Weather_GIF_App
{
	class WeatherGifCreator_Backup2
	{
		private string filePath = @"C:\Users\<USER>\Desktop\weather.gif";
		private int frameDelay = 250;
		private int frameDelayLast = -1;
		private int predictionFrameDelay = 250;
		private int predictionFrameDelayLast = -1;
		private int frameCount = 6;
		private int predictionFrameCount = 6;
		private Point crossPosition = new Point(-100, -100);
		private bool cropToMap = false;
		private bool showLightning = false;

		private int outputWidth = -1;
		private int outputHeight = -1;

		private float predictionFrameOpacity = 0.5f;

		private const string PATH = "path";

		private const string DELAY = "delay";
		private const string DELAY_LAST = "delay_last";
		private const string PRED_DELAY = "pred_delay";
		private const string PRED_DELAY_LAST = "pred_delay_last";

		private const string FRAMES = "frames";
		private const string PREDICTION_FRAMES = "pred_frames";

		private const string CROP = "crop";
		private readonly int croppedWidth = 2393;
		private readonly int croppedHeight = 1518;
		private const string LIGHTNING = "lightning";
		private const string YES = "yes";

		private const string OUTPUT_SIZE = "res";
		private const char OUTPUT_SIZE_DIVIDER = 'x';

		private const string CROSS_POSITION = "cross";
		private const char CROSS_POS_DIVIDER = ',';

		private string localFolder = "";
		private string backgroundDayFileName = @"bg_day.jpg";
		private string backgroundNightFileName = @"bg_night.jpg";

		private string baseUrl = @"https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/";
		private string backgroundImageUrl = @"und/pacz2gmaps6.oro_col_40med.jpg";

		string[] staticLayerUrls = new string[]
{
			"und/pacz2gmaps6.und2.png", /* map detail */
			"und/pacz2gmaps6.borders5.und.png", /* border */
			"img/pacz2gmaps3.frame.png", /* empty? */
			"img/pacz2gmaps3.bocni-4x4.png" /* lines for graphs */
		};
		string crossUrl = "img/krizek10.gif";
		//string scaleUrl = "scl/scl-dbz-mmh.png";

		private string meteoClouds = "data/czrad-z_max3d_masked/pacz2gmaps3.z_max3d.";
		private string meteoLightning = "data/celdn/pacz2gmaps3.blesk.";
		private string meteoDateFormat = "yyyyMMdd.HHmm";
		private string meteoFileExension = ".png";

		private string predictionPathStart = "data/czrad-z_max3d_fct_masked/";
		private string predictionPathMiddle = "/pacz2gmaps3.fct_z_max.";

		private string log = "";

		Bitmap backgroundImageDay;
		Bitmap backgroundImageNight;

		public WeatherGifCreator_Backup2(string[] args)
		{
			log += "Log - " + DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss");

			Log("= Starting, number of arguments: " + args.Length);

			localFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";
			filePath = filePath.Replace("<USER>", Environment.UserName);

			ParseArguments(args);
		}

		public void GenerateGif()
		{
			DateTime baseTime = CalculateBaseTime(DateTime.UtcNow);

			GenerateWeatherImageUrls(baseTime, out List<string> cloudImageUrls, out List<string> lightningImageUrls);

			List<Bitmap> cloudFrames = GrabFrames(cloudImageUrls);
			List<Bitmap> lightningFrames = GrabFrames(lightningImageUrls);

			LoadBackgroundImages();

			Bitmap backgroundImage = ChooseBackgroundImage();
			backgroundImage = PlaceCrossOnBackground(backgroundImage);

			List<Bitmap> frames = GenerateBaseFrames(frameCount, backgroundImage);

			frames = MergeFrames(frames, cloudFrames);
			if (showLightning)
			{
				frames = MergeFrames(frames, lightningFrames);
			}

			List<Bitmap> predictionFrames = new List<Bitmap>();
			if (predictionFrameCount > 0)
			{
				GeneratePredictionImageUrls(baseTime, out List<string> predictionImageUrls);
				List<Bitmap> predicitionWeatherFrames = GrabFrames(predictionImageUrls);
				predictionFrames = GenerateBaseFrames(predictionFrameCount, backgroundImage);
				predictionFrames = MergeFrames(predictionFrames, predicitionWeatherFrames, predictionFrameOpacity);
			}

			if (cropToMap)
			{
				frames = CropFrames(frames);
				predictionFrames = CropFrames(predictionFrames);
			}

			frames = ResizeFrames(frames);
			predictionFrames = ResizeFrames(predictionFrames);

			BuildAndSaveGif(frames, predictionFrames);

			// free up some memory

			backgroundImage.Dispose();
			backgroundImageDay.Dispose();
			backgroundImageNight.Dispose();

			cloudFrames.Clear();
			lightningFrames.Clear();
			predictionFrames.Clear();
		}

		private void Log(string message)
		{
			string logTime = DateTime.Now.ToString("yyyy.MM.dd HH:mm ");
			Console.WriteLine(logTime + message);
			//log += "\n\n" + message;
		}

		private void ParseArguments(string[] args)
		{
			if (args.Length > 0)
			{
				string settingsOutput = "= Reading settings from arguments:";
				string spacing = "\n                     - ";

				for (int i = 0; i < args.Length; i++)
				{
					string arg = args[i];
					string[] split = arg.Split('=');
					if (split.Length > 1)
					{
						string key = split[0].Trim();
						string value = split[1].Trim();


						if (key == PATH)
						{
							filePath = value;
							settingsOutput += spacing + "path = " + filePath;
						}
						else if (key == DELAY)
						{
							if (int.TryParse(value, out int delay))
							{
								frameDelay = Math.Max(10, delay);
								settingsOutput += spacing + "frame delay = " + frameDelay;
							}
						}
						else if (key == DELAY_LAST)
						{
							if (int.TryParse(value, out int delayLast))
							{
								frameDelayLast = Math.Max(10, delayLast);
								settingsOutput += spacing + "frame delay at end = " + frameDelayLast;
							}
						}
						else if (key == PRED_DELAY)
						{
							if (int.TryParse(value, out int delay))
							{
								predictionFrameDelay = Math.Max(10, delay);
								settingsOutput += spacing + "prediction frame delay = " + predictionFrameDelay;
							}
						}
						else if (key == PRED_DELAY_LAST)
						{
							if (int.TryParse(value, out int delayLast))
							{
								predictionFrameDelayLast = Math.Max(10, delayLast);
								settingsOutput += spacing + "prediction frame delay at end = " + predictionFrameDelayLast;
							}
						}
						else if (key == FRAMES)
						{
							if (int.TryParse(value, out int numberOfFrames))
							{
								frameCount = Math.Max(1, numberOfFrames);
								settingsOutput += spacing + "number of frames = " + frameCount;
							}
						}
						else if (key == PREDICTION_FRAMES)
						{
							if (int.TryParse(value, out int numberOfFrames))
							{
								predictionFrameCount = Math.Min(6, Math.Max(0, numberOfFrames));
								settingsOutput += spacing + "number of prediction frames = " + predictionFrameCount;
							}
						}
						else if (key == CROP)
						{
							cropToMap = (value == YES);
							settingsOutput += spacing + "crop to map area = " + cropToMap;
						}
						else if (key == LIGHTNING)
						{
							showLightning = (value == YES);
							settingsOutput += spacing + "show lightning layer = " + showLightning;
						}
						else if (key == OUTPUT_SIZE)
						{
							string[] resSplit = value.Split(OUTPUT_SIZE_DIVIDER);
							if (resSplit.Length > 1)
							{
								if (int.TryParse(resSplit[0].Trim(), out int width))
								{
									outputWidth = (width > 0) ? width : -1;
								}
								if (int.TryParse(resSplit[1].Trim(), out int height))
								{
									outputHeight = (height > 0) ? height : -1;
								}

								settingsOutput += spacing + "output size = " + outputWidth + " x " + outputHeight;
							}

						}
						else if (key == CROSS_POSITION)
						{
							string[] resSplit = value.Split(CROSS_POS_DIVIDER);
							if (resSplit.Length > 1)
							{
								if (int.TryParse(resSplit[0].Trim(), out int xPos) && int.TryParse(resSplit[1].Trim(), out int yPos))
								{
									crossPosition = new Point(xPos, yPos);

									settingsOutput += "\n  cross position = " + xPos + ", " + yPos;
								}
							}
						}
					}
				}

				Log(settingsOutput);
			}
		}

		private DateTime CalculateBaseTime(DateTime currentTime)
		{
			DateTime baseTime = currentTime;
			baseTime = baseTime.Subtract(new TimeSpan(0, 5, 0));
			int leftoverMinutes = baseTime.Minute % 10;
			baseTime = baseTime.Subtract(new TimeSpan(0, leftoverMinutes, 0));
			Log("= Base time, formatted: " + baseTime.ToString(meteoDateFormat));

			return baseTime;
		}

		private void LoadBackgroundImages()
		{
			try
			{
				backgroundImageDay = (Bitmap)Image.FromStream(new FileStream(localFolder + backgroundDayFileName, FileMode.Open));
				backgroundImageNight = (Bitmap)Image.FromStream(new FileStream(localFolder + backgroundNightFileName, FileMode.Open));
			}
			catch (Exception e)
			{
				Log("Could not get background images from disk! " + e.StackTrace);
			}
		}

		private Bitmap ChooseBackgroundImage()
		{
			if (DateTime.Now.Hour > 6 && DateTime.Now.Hour < 21) 
			{
				return backgroundImageDay;
			}
			else
			{
				return backgroundImageNight;
			}
		}

		private Bitmap PlaceCrossOnBackground(Bitmap background)
		{
			Bitmap cross = GetImageFromUrl(baseUrl + crossUrl);

			Bitmap combinedBgImage = new Bitmap(background.Width, background.Height);

			using (Graphics graphics = Graphics.FromImage(combinedBgImage))
			{
				graphics.DrawImage(background, System.Drawing.Point.Empty);
				graphics.DrawImage(cross, crossPosition);
			}

			return combinedBgImage;
		}

		private Bitmap GrabBackground()
		{
			Bitmap background = GetImageFromUrl(baseUrl + backgroundImageUrl);

			List<Bitmap> staticLayers = new List<Bitmap>();

			for (int i = 0; i < staticLayerUrls.Length; i++)
			{
				string layerUrl = baseUrl + staticLayerUrls[i];
				Bitmap layerImage = GetImageFromUrl(layerUrl);
				if (layerImage != null)
				{
					staticLayers.Add(layerImage);
				}
			}

			Bitmap cross = GetImageFromUrl(baseUrl + crossUrl);

			Bitmap combinedBgImage = new Bitmap(background.Width, background.Height);

			using (Graphics graphics = Graphics.FromImage(combinedBgImage))
			{
				graphics.DrawImage(background, System.Drawing.Point.Empty);

				for (int i = 0; i < staticLayers.Count; i++)
				{
					graphics.DrawImage(staticLayers[i], 0, 0, combinedBgImage.Width, combinedBgImage.Height);
				}

				graphics.DrawImage(cross, crossPosition);
			}

			return combinedBgImage;
		}

		private List<Bitmap> GenerateBaseFrames(int frameCount, Bitmap baseImage)
		{
			List<Bitmap> frames = new List<Bitmap>();
			for (int i = 0; i < frameCount; i++)
			{
				frames.Add(baseImage);
			}
			return frames;
		}

		private List<Bitmap> GrabFrames(List<string> urls)
		{
			List<Bitmap> frames = new List<Bitmap>();

			for (int i = 0; i < urls.Count; i++)
			{
				Bitmap image = GetImageFromUrl(urls[i]);

				if (image != null)
				{
					frames.Add(image);
				}
			}

			return frames;
		}

		private List<Bitmap> MergeFrames(List<Bitmap> baseFrames, List<Bitmap> frames, float mergeOpacity = 1f)
		{
			List<Bitmap> mergedFrames = new List<Bitmap>();

			for (int i = 0; i < baseFrames.Count; i++)
			{
				Bitmap mergedFrame = new Bitmap(baseFrames[i].Width, baseFrames[i].Height);

				if (frames.Count > i)
				{
					mergedFrames.Add(MergeBitmaps(baseFrames[i], frames[1], mergeOpacity));
				}
			}
			return mergedFrames;


		}

		private Bitmap MergeBitmaps(Bitmap bottom, Bitmap top, float opacity = 1f)
		{
			if (top == null || opacity <= 0) { return bottom; }

			Bitmap merged = new Bitmap(bottom.Width, bottom.Height);

			using (Graphics graphics = Graphics.FromImage(merged))
			{
				graphics.DrawImage(bottom, 0, 0, merged.Width, merged.Height);

				if (opacity < 1f)
				{
					ColorMatrix matrix = new ColorMatrix();
					matrix.Matrix33 = 0.5f;
					ImageAttributes attributes = new ImageAttributes();
					attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

					graphics.DrawImage(top, new Rectangle(0, 0, merged.Width, merged.Height), 0, 0, top.Width, top.Height, GraphicsUnit.Pixel, attributes);
				}
				else
				{
					graphics.DrawImage(top, 0, 0, merged.Width, merged.Height);
				}
			}
			return merged;
		}

		private void GenerateWeatherImageUrls(DateTime currentTime, out List<string> cloudImageUrls, out List<string> lightningImageUrls)
		{
			cloudImageUrls = new List<string>();
			lightningImageUrls = new List<string>();

			for (int i = 0; i < frameCount; i++)
			{
				string date = currentTime.ToString(meteoDateFormat);

				string cloudUrl = baseUrl + meteoClouds + date + ".0" + meteoFileExension;
				cloudImageUrls.Insert(0, cloudUrl);
				Log("--- Cloud image #" + (i + 1) + ": " + cloudUrl);

				if (showLightning)
				{
					string lightningUrl = baseUrl + meteoLightning + date + meteoFileExension;
					lightningImageUrls.Insert(0, lightningUrl);
					Log("--- Lightning image #" + (i + 1) + ": " + lightningUrl);
				}

				currentTime = currentTime.Subtract(new TimeSpan(0, 10, 0));
			}
		}

		private void GeneratePredictionImageUrls(DateTime currentTime, out List<string> predictionImageUrls)
		{
			predictionImageUrls = new List<string>();

			DateTime futureTime = currentTime;

			string currentTimeFormatted = currentTime.ToString(meteoDateFormat);
			string predictionPath = baseUrl + predictionPathStart + currentTimeFormatted + predictionPathMiddle;

			int addedMinutes = 0;
			for (int i = 0; i < predictionFrameCount; i++)
			{
				futureTime = futureTime.AddMinutes(10);
				addedMinutes += 10;

				string date = futureTime.ToString(meteoDateFormat);

				string predictionImageUrl = predictionPath + date + "." + addedMinutes.ToString() + meteoFileExension;
				predictionImageUrls.Add(predictionImageUrl);
				Log("--  Prediction image #" + (i + 1) + ": " + predictionImageUrl);
			}
		}

		private Bitmap GetImageFromUrl(string imageUrl)
		{
			using(WebClient client = new WebClient())
			{
				using (Stream stream = client.OpenRead(imageUrl))
				{
					Bitmap bitmap = new Bitmap(stream);
				}
			}

			//TODO: when the 
			try
			{
				WebClient client = new WebClient();
				Stream stream = client.OpenRead(imageUrl);
				Bitmap bitmap = new Bitmap(stream);

				stream.Flush();
				stream.Close();
				client.Dispose();

				return bitmap;
			}
			catch (Exception e)
			{
				Log(">> Could not get image from '" + imageUrl + "' " + e.StackTrace);
				//Log("Exception while getting an image from the url '" + imageUrl + "'\n" + e.StackTrace);
			}

			return null;
		}

		private List<Bitmap> CropFrames(List<Bitmap> frames)
		{
			Log("== Cropping " + frames.Count + " frames");

			for (int i = 0; i < frames.Count; i++)
			{
				Bitmap croppedFrame = new Bitmap(croppedWidth, croppedHeight);

				using (Graphics graphics = Graphics.FromImage(croppedFrame))
				{
					graphics.DrawImage(frames[i], new Point(0, croppedFrame.Height - frames[i].Height));
				}

				frames[i] = croppedFrame;
			}
			return frames;
		}

		private List<Bitmap> ResizeFrames(List<Bitmap> frames)
		{
			if (outputWidth < 1 || outputHeight < 1)
			{
				return frames;
			}

			Log("== Resizing " + frames.Count + " frames to " + croppedWidth + " x " + croppedHeight);

			for (int i = 0; i < frames.Count; i++)
			{
				Bitmap resizedFrame = new Bitmap(outputWidth, outputHeight);

				using (Graphics graphics = Graphics.FromImage(resizedFrame))
				{
					graphics.DrawImage(frames[i], 0, 0, outputWidth, outputHeight);
				}

				frames[i] = resizedFrame;
			}
			return frames;
		}

		private void BuildAndSaveGif(List<Bitmap> frames, List<Bitmap> predictionFrames)
		{
			Log("= Creating gif with " + frames.Count + " frames and " + predictionFrames.Count + " prediction frames");
			string path = filePath;
			try
			{
				using (var gif = AnimatedGif.AnimatedGif.Create(path, frameDelay))
				{
					for (int i = 0; i < frames.Count; i++)
					{
						if (i == frames.Count - 1 && frameDelayLast > 0)
						{
							gif.AddFrame(frames[i], frameDelayLast);
						}
						else
						{
							gif.AddFrame(frames[i], frameDelay);
						}
					}
					for (int j = 0; j < predictionFrames.Count; j++)
					{
						if (j == predictionFrames.Count - 1 && predictionFrameDelayLast > 0)
						{
							gif.AddFrame(predictionFrames[j], predictionFrameDelayLast);
						}
						else
						{
							gif.AddFrame(predictionFrames[j], predictionFrameDelay);
						}
					}
				}
			}
			catch (Exception e)
			{
				Log(">> Exception while creating the gif\n" + e.StackTrace);
			}
			Log("= Done, gif should be at: " + path);
		}
	}
}
