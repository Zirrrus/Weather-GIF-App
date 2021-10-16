using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace Weather_GIF_App
{
	class WeatherGifSettings
	{
		public string OutputFolderPath { get; } = "";

		public string GifFileName { get; } = "weather";
		public const string GifFormat = "gif";

		public bool RenderStillImage { get; } = false;

		public string StillImageFileName { get; } = "weather_still";
		public readonly ImageFormat StillImageFormat = ImageFormat.Png;
		public const string StillImageFileExtension = "png";

		public int FrameDelay { get; } = 250;
		public int FrameDelayLast { get; } = -1;
		public int PredictionFrameDelay { get; } = 250;
		public int PredictionFrameDelayLast { get; } = -1;

		public int FrameCount { get; } = 6;
		public int PredictionFrameCount { get; } = 6;

		public bool ShowLightning { get; } = false;

		public float PredictionOpacity { get; } = 0.5f;

		public int CrossPositionX { get; } = -100;
		public int CrossPositionY { get; } = -100;
		public float CrossSize { get; } = 5.0f;

		public bool CropToMap { get; } = false;
		public readonly int croppedWidth = 2393;
		public readonly int croppedHeight = 1518;

		public int OutputWidth { get; } = -1;
		public int OutputHeight { get; } = -1;

		public int DayStartHour { get; } = 7;
		public int DayEndHour { get; } = 19;

		public string ParsingOutput { get; }

		private const string FOLDER_PATH = "folder_path";

		private const string GIF_NAME = "gif_name";

		private const string RENDER_STILL = "save_still";
		private const string STILL_NAME = "still_name";

		private const string DELAY = "delay";
		private const string DELAY_LAST = "delay_last";
		private const string PRED_DELAY = "pred_delay";
		private const string PRED_DELAY_LAST = "pred_delay_last";

		private const string FRAMES = "frames";
		private const string PREDICTION_FRAMES = "pred_frames";

		private const string LIGHTNING = "lightning";

		private const string PREDICITION_OPACITY = "pred_opacity";

		private const string CROSS_POSITION = "cross_pos";
		private const char CROSS_POS_DIVIDER = ',';
		private const string CROSS_SIZE = "cross_size";

		private const string CROP = "crop";

		private const string OUTPUT_SIZE = "res";
		private const char OUTPUT_SIZE_DIVIDER = 'x';

		private const string DAYTIME_RANGE = "day";
		private const char DAYTIME_RANGE_DIVIDER = '-';

		private const string YES = "yes";
		private const string NO = "no";

		public WeatherGifSettings(string[] args)
		{
			OutputFolderPath = $@"C:\Users\{Environment.UserName}\Desktop\";

			if (args.Length > 0)
			{
				string settingsOutput = "Settings from " + args.Length + " arguments:";
				string spacing = "\n                     - ";

				for (int i = 0; i < args.Length; i++)
				{
					string arg = args[i];
					string[] split = arg.Split('=');
					if (split.Length > 1)
					{
						string key = split[0].Trim();
						string value = split[1].Trim();


						if (key == FOLDER_PATH)
						{
							OutputFolderPath = value;
							settingsOutput += spacing + "folder path = " + OutputFolderPath;
						}
						else if (key == GIF_NAME)
						{
							GifFileName = value;
							settingsOutput += spacing + "gif file name = " + GifFileName + "." + GifFormat;
						}
						else if (key == RENDER_STILL)
						{
							RenderStillImage = (value == YES);
							settingsOutput += spacing + "rendering still image = " + RenderStillImage;
						}
						else if (key == STILL_NAME)
						{
							StillImageFileName = value;
							settingsOutput += spacing + "still image file name = " + GifFileName + "." + StillImageFormat;
						}					
						else if (key == DELAY)
						{
							if (int.TryParse(value, out int delay))
							{
								FrameDelay = Math.Max(10, delay);
								settingsOutput += spacing + "frame delay = " + FrameDelay;
							}
						}
						else if (key == DELAY_LAST)
						{
							if (int.TryParse(value, out int delayLast))
							{
								FrameDelayLast = Math.Max(10, delayLast);
								settingsOutput += spacing + "frame delay at end = " + FrameDelayLast;
							}
						}
						else if (key == PRED_DELAY)
						{
							if (int.TryParse(value, out int delay))
							{
								PredictionFrameDelay = Math.Max(10, delay);
								settingsOutput += spacing + "prediction frame delay = " + PredictionFrameDelay;
							}
						}
						else if (key == PRED_DELAY_LAST)
						{
							if (int.TryParse(value, out int delayLast))
							{
								PredictionFrameDelayLast = Math.Max(10, delayLast);
								settingsOutput += spacing + "prediction frame delay at end = " + PredictionFrameDelayLast;
							}
						}
						else if (key == FRAMES)
						{
							if (int.TryParse(value, out int numberOfFrames))
							{
								FrameCount = Math.Max(1, numberOfFrames);
								settingsOutput += spacing + "number of frames = " + FrameCount;
							}
						}
						else if (key == PREDICTION_FRAMES)
						{
							if (int.TryParse(value, out int numberOfFrames))
							{
								PredictionFrameCount = Math.Min(6, Math.Max(0, numberOfFrames));
								settingsOutput += spacing + "number of prediction frames = " + PredictionFrameCount;
							}
						}
						else if (key == CROP)
						{
							CropToMap = (value == YES);
							settingsOutput += spacing + "crop to map area = " + CropToMap;
						}
						else if (key == LIGHTNING)
						{
							ShowLightning = (value == YES);
							settingsOutput += spacing + "show lightning layer = " + ShowLightning;
						}
						else if (key == OUTPUT_SIZE && value.Contains(OUTPUT_SIZE_DIVIDER))
						{
							string[] resSplit = value.Split(OUTPUT_SIZE_DIVIDER);
							if (resSplit.Length > 1)
							{
								if (int.TryParse(resSplit[0].Trim(), out int width))
								{
									OutputWidth = (width > 0) ? width : -1;
								}
								if (int.TryParse(resSplit[1].Trim(), out int height))
								{
									OutputHeight = (height > 0) ? height : -1;
								}

								settingsOutput += spacing + "output size = " + OutputWidth + " x " + OutputHeight;
							}

						}
						else if (key == CROSS_POSITION && value.Contains(CROSS_POS_DIVIDER))
						{
							string[] resSplit = value.Split(CROSS_POS_DIVIDER);
							if (resSplit.Length > 1)
							{
								if (int.TryParse(resSplit[0].Trim(), out int xPos) && int.TryParse(resSplit[1].Trim(), out int yPos))
								{
									CrossPositionX = xPos;
									CrossPositionY = yPos;

									settingsOutput += spacing + "cross position = " + xPos + ", " + yPos;
								}
							}
						}
						else if (key == CROSS_SIZE)
						{
							if (int.TryParse(value.Trim(), out int size))
							{
								CrossSize = size / 100f;

								settingsOutput += spacing + "cross size = " + size + "%";
							}
						}
						else if (key == DAYTIME_RANGE && value.Contains(DAYTIME_RANGE_DIVIDER))
						{
							string[] daySplit = value.Split(DAYTIME_RANGE_DIVIDER);
							if (daySplit.Length > 1)
							{
								if (int.TryParse(daySplit[0].Trim(), out int start) && int.TryParse(daySplit[1].Trim(), out int end))
								{
									DayStartHour = start;
									DayEndHour = end - 1;

									settingsOutput += spacing + "day = from " + start + ":00 to " + end + ":59";
								}
							}
						}
						else if (key == PREDICITION_OPACITY)
						{
							if (int.TryParse(value.Trim(), out int opacity))
							{
								PredictionOpacity = opacity / 100f;

								settingsOutput += spacing + "prediction frame opacity = " + opacity + "%";
							}
						}
					}
				}
				ParsingOutput = settingsOutput;
			}
			else
			{
				ParsingOutput = "No arguments provided";
			}
		}
	}
}
