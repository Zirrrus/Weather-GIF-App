using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;

namespace Weather_GIF_App
{
	class WeatherGifCreator
	{
		private WeatherGifSettings s;

		private readonly string localFolder = "";
		private readonly string backgroundDayFileName = @"bg_day.jpg";
		private readonly string backgroundNightFileName = @"bg_night.jpg";

		private readonly string baseUrl = @"https://www.chmi.cz/files/portal/docs/meteo/rad/inca-cz/";

		string crossUrl = "img/krizek10.gif";
		//string scaleUrl = "scl/scl-dbz-mmh.png";

		private string meteoClouds = "data/czrad-z_max3d_masked/pacz2gmaps3.z_max3d.";
		private string meteoLightning = "data/celdn/pacz2gmaps3.blesk.";
		private string meteoDateFormat = "yyyyMMdd.HHmm";
		private string meteoFileExension = ".png";

		private string predictionPathStart = "data/czrad-z_max3d_fct_masked/";
		private string predictionPathMiddle = "/pacz2gmaps3.fct_z_max.";

		Bitmap backgroundImageDay;
		Bitmap backgroundImageNight;

		WebClient client;

		public WeatherGifCreator(WeatherGifSettings settings)
		{
			s = settings;
			Log(settings.ParsingOutput);

			localFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";
		}

		public void GenerateGif()
		{
			Console.WriteLine("");
			Log("GENERATING GIF\n");

			client = new WebClient();

			DateTime baseTime = CalculateBaseTime(DateTime.UtcNow);

			LoadBackgroundImages();
			Bitmap backgroundImage = ChooseBackgroundImage(s.DayStartHour, s.DayEndHour);
			backgroundImage = PlaceCrossOnBackground(backgroundImage, s.CrossPositionX, s.CrossPositionY, s.CrossSize);

			GenerateWeatherImageUrls(baseTime, s.FrameCount, s.ShowLightning, out List<string> cloudImageUrls, out List<string> lightningImageUrls);
			
			List<Bitmap> cloudFrames = GrabFrames(cloudImageUrls);

			List<Bitmap> frames = GenerateBaseFrames(cloudFrames.Count, backgroundImage);
			frames = MergeFrames(frames, cloudFrames);

			if (s.ShowLightning)
			{
				List<Bitmap> lightningFrames = GrabFrames(lightningImageUrls);
				frames = MergeFrames(frames, lightningFrames);
			}

			List<Bitmap> predictionFrames = new List<Bitmap>();
			if (s.PredictionFrameCount > 0)
			{
				GeneratePredictionImageUrls(baseTime, s.PredictionFrameCount, out List<string> predictionImageUrls);
				List<Bitmap> predicitionWeatherFrames = GrabFrames(predictionImageUrls);
				predictionFrames = GenerateBaseFrames(predicitionWeatherFrames.Count, backgroundImage);
				predictionFrames = MergeFrames(predictionFrames, predicitionWeatherFrames, s.PredictionOpacity);
			}

			if (s.CropToMap)
			{
				frames = CropFrames(frames, s.croppedWidth, s.croppedHeight);
				predictionFrames = CropFrames(predictionFrames, s.croppedWidth, s.croppedHeight);
			}

			if (s.OutputWidth > 0 && s.OutputHeight > 0)
			{
				frames = ResizeFrames(frames, s.OutputWidth, s.OutputHeight);
				predictionFrames = ResizeFrames(predictionFrames, s.OutputWidth, s.OutputHeight);
			}

			BuildAndSaveGif(s.OutputFilePath, frames, s.FrameDelay, s.FrameDelayLast, predictionFrames, s.PredictionFrameDelay, s.PredictionFrameDelayLast);

			// free up some memory

			client.Dispose();
			client = null;

			backgroundImage.Dispose();
			backgroundImageDay.Dispose();
			backgroundImageNight.Dispose();
		}

		private void SaveImage(string filePath, Bitmap image)
		{
			try
			{
				image.Save(filePath, ImageFormat.Jpeg);
			}
			catch (Exception e)
			{
				Log(" >> Exception while saving image");
			}
		}

		private void Log(string message)
		{
			string logTime = DateTime.Now.ToString("yyyy.MM.dd HH:mm = ");
			Console.WriteLine(logTime + message);
		}

		private DateTime CalculateBaseTime(DateTime currentTime)
		{
			DateTime baseTime = currentTime;
			baseTime = baseTime.Subtract(new TimeSpan(0, 1, 0));
			int leftoverMinutes = baseTime.Minute % 10;
			baseTime = baseTime.Subtract(new TimeSpan(0, leftoverMinutes, 0));

			bool validTime = false;
			do
			{
				string url = baseUrl + meteoClouds + baseTime.ToString(meteoDateFormat) + ".0" + meteoFileExension;
				validTime = TestUrl(url);

				if (!validTime)
				{
					Log("Could not get image for " + baseTime.ToString(meteoDateFormat) + ", trying 10 minutes earlier");
					baseTime = baseTime.Subtract(new TimeSpan(0, 10, 0));
				}
			} while (!validTime);

			Log("Found base time: " + baseTime.ToString(meteoDateFormat));

			return baseTime;
		}

		private void LoadBackgroundImages()
		{
			try
			{
				using (FileStream stream = new FileStream(localFolder + backgroundDayFileName, FileMode.Open))
				{
					backgroundImageDay = (Bitmap)Image.FromStream(stream);
				}
				using (FileStream stream = new FileStream(localFolder + backgroundNightFileName, FileMode.Open))
				{
					backgroundImageNight = (Bitmap)Image.FromStream(stream);
				}
			}
			catch (Exception e)
			{
				Log("Could not get background images from disk! Exception: " + e.GetType().Name);
			}
		}

		private Bitmap ChooseBackgroundImage(int dayStartHour, int dayEndHour)
		{
			if (DateTime.Now.Hour >= dayStartHour && DateTime.Now.Hour <= dayEndHour) 
			{
				return backgroundImageDay;
			}
			else
			{
				return backgroundImageNight;
			}
		}

		private Bitmap PlaceCrossOnBackground(Bitmap background, float posX, float posY, float size)
		{
			if (posX < 0 || posY < 0) { return background; }

			Log("Drawing cross at " + (int)posX + ", " + (int)posY + " at " + (int)(size * 100) + "% size");

			Bitmap cross = GetImageFromUrl(baseUrl + crossUrl);

			Bitmap combinedBgImage = new Bitmap(background.Width, background.Height);

			using (Graphics graphics = Graphics.FromImage(combinedBgImage))
			{
				graphics.DrawImage(background, 0, 0, background.Width, background.Height);
				
				float crossSize = cross.Width * size;
				float crossSizeHalf = crossSize * 0.5f;
				graphics.DrawImage(cross, posX - crossSizeHalf, posY - crossSizeHalf, crossSize, crossSize);
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
			if (frames.Count <= 0) { return baseFrames; }

			List<Bitmap> mergedFrames = new List<Bitmap>();

			for (int i = 0; i < baseFrames.Count; i++)
			{
				if (frames.Count > i)
				{
					mergedFrames.Add(MergeBitmaps(baseFrames[i], frames[i], mergeOpacity));
				}
				else
				{
					mergedFrames.Add(baseFrames[i]);
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

		private void GenerateWeatherImageUrls(DateTime currentTime, int frameCount, bool showLightning, out List<string> cloudImageUrls, out List<string> lightningImageUrls)
		{
			cloudImageUrls = new List<string>();
			lightningImageUrls = new List<string>();

			string cloudUrlOutput = "";
			string lightningUrlOutput = "";
			string spacing = "\n                     - ";

			for (int i = 0; i < frameCount; i++)
			{
				string date = currentTime.ToString(meteoDateFormat);

				string cloudUrl = baseUrl + meteoClouds + date + ".0" + meteoFileExension;
				cloudImageUrls.Insert(0, cloudUrl);
				cloudUrlOutput = spacing + cloudUrl + cloudUrlOutput;

				if (showLightning)
				{
					string lightningUrl = baseUrl + meteoLightning + date + meteoFileExension;
					lightningImageUrls.Insert(0, lightningUrl);
					lightningUrlOutput = spacing + lightningUrl + lightningUrlOutput;
				}

				currentTime = currentTime.Subtract(new TimeSpan(0, 10, 0));
			}

			Log("Cloud images:" + cloudUrlOutput);
			if (showLightning)
			{
				Log("Lightning images:" + lightningUrlOutput);
			}
		}

		private void GeneratePredictionImageUrls(DateTime currentTime, int predictionFrameCount, out List<string> predictionImageUrls)
		{
			predictionImageUrls = new List<string>();

			string predictionUrlOutput = "Prediction images:";
			string spacing = "\n                     - ";

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
				predictionUrlOutput += spacing + predictionImageUrl;
			}

			Log(predictionUrlOutput);
		}

		private Bitmap GetImageFromUrl(string imageUrl)
		{
			// TODO: when an exception occurs, the streams are apparently not properly closed

			Bitmap bitmap = null;

			try
			{
				using (Stream stream = client.OpenRead(imageUrl))
				{
					try
					{
						bitmap = new Bitmap(stream);
					}
					catch (Exception e)
					{
						Log(">>> Exception with the Stream: '" + e.GetType().Name + "'");
						stream.Close();

					}
				}
			}
			catch (Exception e)
			{
				Log(">>> Exception with the WebClient: '" + e.GetType().Name + "'");
				client.Dispose();
			}


			if (bitmap == null)
			{
				Log(">> Could not get image from '" + imageUrl + "'");
			}

			return bitmap;
		}

		private bool TestUrl(string url)
		{
			try
			{
				HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
				request.Method = "HEAD";
				HttpWebResponse response = request.GetResponse() as HttpWebResponse;
				response.Close();
				return (response.StatusCode == HttpStatusCode.OK);
			}
			catch (Exception e) 
			{
				return false;
			}
		}

		private List<Bitmap> CropFrames(List<Bitmap> frames, int croppedWidth, int croppedHeight)
		{
			Log("Cropping " + frames.Count + " frames to map area (" + croppedWidth + " x " + croppedHeight + ")");

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

		private List<Bitmap> ResizeFrames(List<Bitmap> frames, int outputWidth, int outputHeight)
		{
			if (outputWidth < 1 || outputHeight < 1) { return frames; }

			Log("Resizing " + frames.Count + " frames to " + outputWidth + " x " + outputHeight);

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

		private void BuildAndSaveGif(string outputPath, List<Bitmap> frames, int frameDelay, int frameDelayLast, List<Bitmap> predictionFrames, int predictionFrameDelay, int predictionFrameDelayLast)
		{
			Log("Creating gif with " + frames.Count + " frames and " + predictionFrames.Count + " prediction frames");
			try
			{
				using (var gif = AnimatedGif.AnimatedGif.Create(outputPath, frameDelay))
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
			Log("Done, gif should be at: " + outputPath);
		}
	}
}
