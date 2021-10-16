Weather Gif App v 1.5 - 
by Zirrrus
for Mavi

Changes
- added option to render a still image (png) of the last current weather frame
- additional options related to the still image
- now folder path and file names have to be specified seperately

Installation:
- not necessary

Run manually:
- just run the exe

Uninstall:
- delete it

To run regularly:
- start it, it generates a gif every 10 minutes (+ however many seconds it takes to generate the gif)

Settings:
- can be provided as starting parameters / arguments, as a space-separated list 
- example: path=C:\... delay=333 delay_last=2500 frames=10 crop=no res=1000x677 cross=500,300
- are all optional, defaults are shown in parentheses
- day background is bg_day.jpg, night background is bg_night.jpg

 folder_path (should by default be the desktop of the current user): where the resulting images are saved, needs to be a folder 
 gif_name (weather): name of the gif file, .gif is added automatically
 save_still (no): if a still image should be saved 
 still_name (weather_still): name of the still image, .png is added automatically
 delay (250): the time between frames in the gif, in milliseconds
 delay_last (same as delay): the time the last frame is shown for
 pred_delay (250): the time between prediction frames in the gif, in milliseconds
 pred_delay_last (same as pred_delay): the time in milliseconds the last prediction frame is shown for
 frames (6): how many frames the gif will contain
 pred_frames(6, max 6): how many of the prediction frames to load and show
 lightning (no): if set to 'yes' lightning data will be shown
 pred_opacity (50): how transparent the predicion frames are drawn, in %
 cross_pos (-100,-100): x and y position of where the little red cross is drawn in the image, this is in pixels from the upper left corner on the original uncropped image
 cross_size (300): how big the cross is drawn in % of original image size
 crop (no): if set to 'yes' the image will be cropped to the map area (2393x1518 in the lower left corner of the original 2729x1840 image)
 res (2729x1840): output size, width and height separated by 'x', original image will be scaled to the provided resolution, ignores aspect ratio
 day (7-19): the hours between which the original bright background is used, in 24h format. the end hour is inclusive, so 7-19 means 7:00 until 19:59:59

Check what the service is actually doing (in case of errors):
- read the console