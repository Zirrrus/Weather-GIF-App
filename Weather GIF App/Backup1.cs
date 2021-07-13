using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

namespace Weather_GIF_App
{
	class Backup1
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
		string scaleUrl = "scl/scl-dbz-mmh.png";

		private string meteoClouds = "data/czrad-z_max3d_masked/pacz2gmaps3.z_max3d.";
		private string meteoLightning = "data/celdn/pacz2gmaps3.blesk.";
		private string meteoDateFormat = "yyyyMMdd.HHmm";
		private string meteoFileExension = ".png";

		private string predictionPathStart = "data/czrad-z_max3d_fct_masked/";
		private string predictionPathMiddle = "/pacz2gmaps3.fct_z_max.";

		//https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/data/czrad-z_max3d_masked/pacz2gmaps3.z_max3d.20210704.1310.0.png
		//https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/data/czrad-z_max3d_fct_masked/20210704.1340/pacz2gmaps3.fct_z_max.20210704.1350.10.png
		//https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/data/celdn/pacz2gmaps3.blesk.20210704.1310.png

		string log = "";

		public Backup1(string[] args)
		{
			log += "Log - " + DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss");

			Log("Starting, number of arguments: " + args.Length);

			filePath = filePath.Replace("<USER>", Environment.UserName);

			ParseArguments(args);
		}

		public void GenerateGif()
		{
			DateTime baseTime = CalculateBaseTime(DateTime.UtcNow);

			Bitmap background = GrabBackground();

			List<Bitmap> frames = GrabWeatherFrames(baseTime, frameCount, background);

			List<Bitmap> predicionFrames = GrabPredictionFrames(baseTime, predictionFrameCount, background);

			if (cropToMap)
			{
				frames = CropFrames(frames);
				predicionFrames = CropFrames(predicionFrames);
			}

			frames = ResizeFrames(frames);
			predicionFrames = ResizeFrames(predicionFrames);

			BuildAndSaveGif(frames, predicionFrames, frameDelay);
		}

		private void Log(string message)
		{
			Console.WriteLine(message);
			//log += "\n\n" + message;
		}

		private void ParseArguments(string[] args)
		{
			if (args.Length > 0)
			{
				string settingsOutput = "Reading settings from arguments:";

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
							settingsOutput += "\n  path = " + filePath;
						}
						else if (key == DELAY)
						{
							if (int.TryParse(value, out int delay))
							{
								frameDelay = Math.Max(10, delay);
								settingsOutput += "\n  frame delay = " + frameDelay;
							}
						}
						else if (key == DELAY_LAST)
						{
							if (int.TryParse(value, out int delayLast))
							{
								frameDelayLast = Math.Max(10, delayLast);
								settingsOutput += "\n  frame delay at end = " + frameDelayLast;
							}
						}
						else if (key == PRED_DELAY)
						{
							if (int.TryParse(value, out int delay))
							{
								predictionFrameDelay = Math.Max(10, delay);
								settingsOutput += "\n  prediction frame delay = " + predictionFrameDelay;
							}
						}
						else if (key == PRED_DELAY_LAST)
						{
							if (int.TryParse(value, out int delayLast))
							{
								predictionFrameDelayLast = Math.Max(10, delayLast);
								settingsOutput += "\n  prediction frame delay at end = " + predictionFrameDelayLast;
							}
						}
						else if (key == FRAMES)
						{
							if (int.TryParse(value, out int numberOfFrames))
							{
								frameCount = Math.Max(1, numberOfFrames);
								settingsOutput += "\n  number of frames = " + frameCount;
							}
						}
						else if (key == PREDICTION_FRAMES)
						{
							if (int.TryParse(value, out int numberOfFrames))
							{
								predictionFrameCount = Math.Min(6, Math.Max(0, numberOfFrames));
								settingsOutput += "\n  number of prediction frames = " + predictionFrameCount;
							}
						}
						else if (key == CROP)
						{
							cropToMap = (value == YES);
							settingsOutput += "\n  crop to map area = " + cropToMap;
						}
						else if (key == LIGHTNING)
						{
							showLightning = (value == YES);
							settingsOutput += "\n  show lightning layer = " + showLightning;
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

								settingsOutput += "\n  output size = " + outputWidth + " x " + outputHeight;
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
			Log("Base time, formatted: " + baseTime.ToString(meteoDateFormat));

			return baseTime;
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

		private List<Bitmap> GenerateBaseFrames(int frames, Bitmap baseImage)
		{
			List<Bitmap> frameList = new List<Bitmap>();
			for (int i = 0; i < frames; i++)
			{
				frameList.Add(baseImage);
			}
			return frameList;
		}

		private List<Bitmap> GrabWeatherFrames(DateTime baseTime, int frameCount, Bitmap background)
		{
			GenerateWeatherImageUrls(baseTime, frameCount, out List<string> cloudImageUrls, out List<string> lightningImageUrls);

			List<Bitmap> weatherFrames = new List<Bitmap>();


			for (int i = 0; i < cloudImageUrls.Count; i++)
			{
				Bitmap frame = new Bitmap(background.Width, background.Height);

				using (Graphics graphics = Graphics.FromImage(frame))
				{
					graphics.DrawImage(background, Point.Empty);

					Bitmap cloudImage = GetImageFromUrl(cloudImageUrls[i]);
					graphics.DrawImage(cloudImage, 0, 0, frame.Width, frame.Height);

					if (showLightning)
					{
						Bitmap lightningImage = GetImageFromUrl(lightningImageUrls[i]);
						graphics.DrawImage(lightningImage, 0, 0, frame.Width, frame.Height);
					}
				}

				weatherFrames.Add(frame);
			}

			return weatherFrames;
		}

		private void GenerateWeatherImageUrls(DateTime currentTime, int frames, out List<string> cloudImageUrls, out List<string> lightningImageUrls)
		{
			cloudImageUrls = new List<string>();
			lightningImageUrls = new List<string>();

			for (int i = 0; i < frames; i++)
			{
				string date = currentTime.ToString(meteoDateFormat);

				string cloudUrl = baseUrl + meteoClouds + date + ".0" + meteoFileExension;
				cloudImageUrls.Insert(0, cloudUrl);
				Log("  Cloud image #" + (i + 1) + ":     " + cloudUrl);

				if (showLightning)
				{
					string lightningUrl = baseUrl + meteoLightning + date + meteoFileExension;
					lightningImageUrls.Insert(0, lightningUrl);
					Log("  Lightning image #" + (i + 1) + ": " + lightningUrl);
				}

				currentTime = currentTime.Subtract(new TimeSpan(0, 10, 0));
			}
		}

		private List<Bitmap> GrabPredictionFrames(DateTime baseTime, int frameCount, Bitmap background)
		{
			GeneratePredictionImageUrls(baseTime, frameCount, out List<string> predictionImageUrls);

			List<Bitmap> predictionFrames = new List<Bitmap>();


			for (int i = 0; i < predictionImageUrls.Count; i++)
			{
				Bitmap frame = new Bitmap(background.Width, background.Height);

				using (Graphics graphics = Graphics.FromImage(frame))
				{
					graphics.DrawImage(background, Point.Empty);

					ColorMatrix matrix = new ColorMatrix();
					matrix.Matrix33 = 0.5f;
					ImageAttributes attributes = new ImageAttributes();
					attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

					Bitmap predicitionImage = GetImageFromUrl(predictionImageUrls[i]);
					if (predicitionImage == null)
					{
						continue;
					}

					graphics.DrawImage(predicitionImage, new Rectangle(0, 0, frame.Width, frame.Height), 0, 0, predicitionImage.Width, predicitionImage.Height, GraphicsUnit.Pixel, attributes);
				}

				predictionFrames.Add(frame);
			}

			return predictionFrames;
		}

		private void GeneratePredictionImageUrls(DateTime currentTime, int frames, out List<string> predictionImageUrls)
		{
			predictionImageUrls = new List<string>();

			DateTime futureTime = currentTime;

			string currentTimeFormatted = currentTime.ToString(meteoDateFormat);
			string predictionPath = baseUrl + predictionPathStart + currentTimeFormatted + predictionPathMiddle;

			int addedMinutes = 0;
			for (int i = 0; i < frames; i++)
			{
				futureTime = futureTime.AddMinutes(10);
				addedMinutes += 10;

				string date = futureTime.ToString(meteoDateFormat);

				string predictionImageUrl = predictionPath + date + "." + addedMinutes.ToString() + meteoFileExension;
				predictionImageUrls.Add(predictionImageUrl);
				Log("  Prediction image #" + (i + 1) + ": " + predictionImageUrl);
			}
		}

		private Bitmap GetImageFromUrl(string imageUrl)
		{
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
				Log("Exception while getting an image from the url '" + imageUrl + "'\n" + e.StackTrace);
			}

			return new Bitmap(1, 1);
		}

		private List<Bitmap> CropFrames(List<Bitmap> frames)
		{
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

		private void BuildAndSaveGif(List<Bitmap> frames, List<Bitmap> predictionFrames, int delay)
		{
			Log("Creating gif with " + frames.Count + " frames and " + predictionFrames.Count + " prediction frames");
			string path = filePath;
			try
			{
				using (var gif = AnimatedGif.AnimatedGif.Create(path, delay))
				{
					for (int i = 0; i < frames.Count; i++)
					{
						if (i == frames.Count - 1 && frameDelayLast > 0)
						{
							gif.AddFrame(frames[i], frameDelayLast);
						}
						else
						{
							gif.AddFrame(frames[i]);
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
				Log("Exception while creating the gif\n" + e.StackTrace);
			}
			Log("Done, gif should be at: " + path);
		}
	}
}
