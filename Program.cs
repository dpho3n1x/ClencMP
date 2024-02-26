﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
namespace ClencMP;
class Program
{
    static void Main(string[] args)
    {
        //Publish optimized: dotnet publish -c RELEASE
        //Run: dotnet run
        //App Start
            Console.Title = "ClencMP v1.0";

            string inputFile;
            try
            {
                inputFile = args[0];
            }
            catch 
            {
                ShowTitle("Help to Clenc Multiplatform by Sho");

                ShowCyanText("\nArguments alignment: ", "ClencMP.exe [movie.mp4] [start-time] [end-time] [file size]\n");
                ShowCyanText("Example: ", "ClencMP.exe movie.mp4 00:00:00 00:00:20 25\n\n");
                ShowCyanText("Movie Location: ", "Movie location as: file.mp4, C:\\file.mp4, .\\folder\\file.mp4\n");
                ShowCyanText("Start Time: ", "00:01:01 or 61 (seconds). Default: 0\n");
                ShowCyanText("End Time: ", "00:01:01 or 61 (seconds). Default: until the end of original\n");
                ShowCyanText("Result File Size: ", "In Megabytes - 25 (MB, default)\n");

                Console.ResetColor();
                Console.WriteLine("\nIf app does not work properly, try:");
                Console.WriteLine("Windows: ");
                ShowCyanText("Make sure you have in the same directory: ", "ffmpeg.exe & ffprobe.exe\n");
                Console.ResetColor();
                Console.WriteLine("\nUbuntu 22.04+: ");
                ShowCyanText("Install dependencies: ", "sudo apt -y install ffmpeg dotnet-runtime-7.0");

                Console.ResetColor();
                Console.WriteLine("\n\nPress any key to exit...");
                Console.Write("===============================================");
                Console.ReadKey();
                Environment.Exit(2);
                return;
            }

            Console.WriteLine("Collecting Data...");

            decimal videoLength, startTime = 0;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            try
            {
                if (!decimal.TryParse(args[2], out videoLength))
                    videoLength = (decimal)(TimeSpan.Parse(args[2]).TotalSeconds);      
            }
            catch 
            {
                videoLength = Convert.ToDecimal(ProbeVideo("-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 " + '"' + inputFile + '"'));
            }

            try
            {
                if (!decimal.TryParse(args[1], out startTime))
                    startTime = (decimal)(TimeSpan.Parse(args[1]).TotalSeconds);                          
            }
            catch{ }

                videoLength -= startTime;
                if (videoLength < 0)
                    ShowError("Video length needs to be longer than 0.");

            ushort sizeMB = 25;

            try
            {
                sizeMB = ushort.Parse(args[3]);
            }
            catch { }

            byte audioSetting = 0;
            bool isAudioAvailable = false;
            if (ProbeVideo("-loglevel error -select_streams a -show_entries stream=codec_type -of csv=p=0 " + '"' + inputFile + '"') != string.Empty)
            {
                isAudioAvailable = true;
                audioSetting = 1;
            }

            string hwaccel = CheckHWAccel();

            //Set default settings
            bool enable_enhancedBitrate = true, lowPriority = true;
            string videoCopyright = "Sho#9398", customArguments = "-af afade=t=in:d=1,afade=t=out:d=1:st=" + (videoLength - 1) + " ", fileFormat = "webm";
            byte encoderSpeed = 1, maxAudioBitrate = 160;

        Summary:
            uint sizeKb = (uint)(sizeMB * 8192);
            uint fileBitrate = (uint)(sizeKb / videoLength);
            uint sizeB = (uint)(sizeMB * 1048576);

            byte audioMargin;
            switch (encoderSpeed)
            {
                case 0:
                    audioMargin = 32;
                    if (!enable_enhancedBitrate)
                        audioMargin = 0;
                    break;
                case 1:
                    audioMargin = 56;
                    break;
                default:
                    audioMargin = 64;
                    break;
            }
            uint videoBitrate = fileBitrate - audioMargin;

            Console.Clear();
            ShowTitle("Welcome to ClencMP! This is the SUMMARY:");

            ShowCyanText("\nVideo to encode: ", inputFile);
            ShowCyanText("\nExpected video size: ", sizeMB + "MB (" + fileBitrate + "kb/s)\n");
            ShowCyanText("\nVideo length: ", videoLength.ToString("#.000") + "s");

            ShowCyanText("\nEncoder speed: ", encoderSpeed.ToString() + " | Quality [");
            for (byte b = 0; b < 5; b++)
                if (b == encoderSpeed)
                    ShowCyanText(string.Empty, "#");
                else
                    ShowCyanText(string.Empty, "-");
                    
            ShowCyanText(string.Empty, "] Speed");

            Console.ResetColor();
            Console.WriteLine("\n[▲▼] Change encoder speed");

            if (encoderSpeed == 0 && enable_enhancedBitrate)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[E] Enhanced Bitrate\nIf result video size is lower than expected, encoding will be restarted with higher bitrate");
                Console.ResetColor();
            }

            Console.Write("\nAudio mode: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            switch (audioSetting)
            {
                case 0:
                    Console.Write("No Audio");
                    break;
                case 1:
                    Console.Write("Track 1 (default)");
                    break;
                case 2:
                    Console.Write("Track 2");
                    break;
            }
            Console.ResetColor();
            Console.WriteLine("\n[<>] Change audio mode");

            Console.WriteLine("\n[A] Advanced Settings");

            if (fileBitrate <= 64)
                ShowError("Expected video size is too low to encode it. Shorten the video or change expected video size.");

            Console.WriteLine("\nEverything ready! Press ENTER to proceed to the encoding!");
            Console.WriteLine("===========================================================");

        PressKey:
            ConsoleKeyInfo keyPress = Console.ReadKey(intercept: true);
            switch (keyPress.Key)
            {
                case ConsoleKey.UpArrow:
                    if (encoderSpeed < 4)
                    {
                        encoderSpeed++;
                        goto Summary;
                    }
                    else
                        ShowRedInfo("Can't do it faster. If you need higher speed, consider lowering the video resolution");
                    break;
                case ConsoleKey.DownArrow:
                    if (encoderSpeed > 0)
                    {
                        encoderSpeed--;
                        goto Summary;
                    }
                    else
                        ShowRedInfo("Can't do it slower");
                    break;
                case ConsoleKey.Enter:
                    goto EncodeVideo;
                case ConsoleKey.LeftArrow:
                    if (audioSetting != 0 && isAudioAvailable)
                    {
                        audioSetting--;
                        goto Summary;
                    }
                    else if (isAudioAvailable == false)
                        ShowRedInfo("No audio tracks in the source video");
                    break;
                case ConsoleKey.RightArrow:
                    if (audioSetting != 2 && isAudioAvailable)
                    {
                        audioSetting++;
                        goto Summary;
                    }
                    else if (isAudioAvailable == false)
                        ShowRedInfo("No audio tracks in the source video");
                    break;
                case ConsoleKey.A:
                    goto AdvancedSettings;
                case ConsoleKey.E:
                    enable_enhancedBitrate = !enable_enhancedBitrate;
                    goto Summary;
            }
            goto PressKey;


        AdvancedSettings:
            Console.Clear();
            ShowTitle("ADVANCED SETTINGS");

            Console.WriteLine("\n[C] Add custom FFMPEG arguments");
            if (customArguments != string.Empty && customArguments != " ")
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Applied: " + customArguments);
                Console.ResetColor();
            }

            ShowCyanText("\n[P] Use low priority for encoding", "\nApplied: " + lowPriority);

            ShowCyanText("\n\n[M] Switch output format to WEBM/MKV", "\nApplied: " + fileFormat);

            Console.ResetColor();
            Console.WriteLine("\n\n[H] Switch GPU acceleration");
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (hwaccel == string.Empty)
                Console.WriteLine("Applied: Off");
            else
                Console.WriteLine("Applied: " + hwaccel);
            Console.ResetColor();

            if (audioSetting != 0)
            {
                ShowCyanText("\n[▲▼] Maximal sound bitrate", "\nApplied: " + maxAudioBitrate + "kb/s\n");
                Console.ResetColor();
            }

            Console.WriteLine("\n[ESC] Go back to the Summary");

            Console.WriteLine("\nPress expected function key");

            Console.WriteLine("=============================");
        PressKeyAdvancedSettings:
            ConsoleKeyInfo keyPress2 = Console.ReadKey(intercept: true);
            switch (keyPress2.Key)
            {
                case ConsoleKey.C:
                    customArguments = AddCustomArguments(customArguments);
                    goto AdvancedSettings;
                case ConsoleKey.Escape:
                    goto Summary;
                case ConsoleKey.P:
                    lowPriority = !lowPriority;
                    goto AdvancedSettings;
                case ConsoleKey.UpArrow:
                    if (maxAudioBitrate < 231)
                    {
                        maxAudioBitrate += 16;
                        goto AdvancedSettings;
                    }
                    else
                        ShowRedInfo("Can't do more");
                    break;
                case ConsoleKey.DownArrow:
                    if (maxAudioBitrate > 17)
                    {
                        maxAudioBitrate -= 16;
                        goto AdvancedSettings;
                    }
                    else 
                        ShowRedInfo("Can't do less");
                    break;
                case ConsoleKey.H:
                    if (hwaccel != string.Empty)
                        hwaccel = string.Empty;
                    else
                        hwaccel = CheckHWAccel();
                    goto AdvancedSettings;
                case ConsoleKey.M:
                    if (fileFormat == "webm")
                        fileFormat = "mkv";
                    else
                        fileFormat = "webm";
                    goto AdvancedSettings;
            }
            goto PressKeyAdvancedSettings;

        EncodeVideo:
            ushort videoLengthSec = (ushort)videoLength;
            string enhancedBitrate = string.Empty;
            Console.Title = "ClencMP | Encoding Video Pass1 | Length: " + videoLengthSec + "s" + enhancedBitrate;

            Stopwatch encodingTime = new();
            encodingTime.Start();

            byte encoderTiles = 0, encoderLIF = 25, autoAltRef = 1;
            string arnrPlus = string.Empty;
            if (encoderSpeed == 0)
                arnrPlus = "-arnr_max_frames 15 -arnr_strength 6 -arnr_type 3 ";
            else if (encoderSpeed > 1)
                encoderTiles = 1;


            if (encoderSpeed == 4)
            {
                encoderLIF = 0;
                autoAltRef = 0;
            }

            Console.Clear();
            Console.ResetColor();
            if (lowPriority)
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            string encodeVideoCommand = "-hide_banner " + hwaccel + "-y -i " + '"' + inputFile + '"' + " -c:v libvpx-vp9 -row-mt 1 -tile-rows " + encoderTiles + " -tile-columns " + encoderTiles + " -g 600 -pix_fmt yuv420p -auto-alt-ref " + autoAltRef + " -lag-in-frames " + encoderLIF + " -map 0:v:0 " + arnrPlus + "-ss " + startTime + " -t " + videoLength + " -cpu-used " + encoderSpeed + " -b:v " + videoBitrate + "k -f webm " + customArguments + " -passlogfile " + '"' + Path.GetTempPath() + "ClencMP" + '"' + " -pass ";
            StartFFMPEGProcess(encodeVideoCommand + "1 " + '"' + Path.GetTempPath() + "video.webm" + '"');
            Console.Clear();

            Console.Title = "ClencMP | Encoding Video Pass2 | Length: " + videoLengthSec + "s" + enhancedBitrate;

            StartFFMPEGProcess(encodeVideoCommand + "2 " + '"' + Path.GetTempPath() + "video.webm" + '"');
            File.Delete(Path.GetTempPath() + "ClencMP-0.log");

            FileInfo Video = new FileInfo(Path.GetTempPath() + "video.webm");
            if (Video.Length > sizeB)
                ShowError("Result video exceeded expected file size (" + sizeMB + "MB)\n\nAvailable solutions:\n- Lower the resolution (Advanced Settings -> Video Effects)\n- Shorten the video\n- Lower the encoder speed (to QUALITY setting)");

            uint maxWielkoscAudio = (uint)(sizeB - Video.Length);
            ushort bitrateAudio = (ushort)(maxWielkoscAudio / 128 / videoLength + 23);
            if (bitrateAudio >= maxAudioBitrate - 23)
                if (encoderSpeed == 0 && enhancedBitrate == string.Empty && enable_enhancedBitrate)
                {
                    enhancedBitrate = " [EB]";
                    videoBitrate = fileBitrate + 100;
                    goto EncodeVideo;
                }
                else
                    bitrateAudio = maxAudioBitrate;
              
            if (audioSetting == 0)
                goto Muxing;

            Console.Title = "ClencMP | Encoding Audio";
            Console.Clear();

            ShowTitle("Almost finished! Encoding audio:");
            Console.WriteLine("");

            byte audioTrackNumber = 0;
            if (audioSetting == 2)
                audioTrackNumber = 1;

            string audioEncodeCommand = "-loglevel warning -y -ss " + startTime + " -i " + '"' + inputFile + '"' + " -c:a libopus -frame_duration 120 -map 0:a:" + audioTrackNumber + " -t " + videoLength + " -b:a ";
            do
            {
                Console.WriteLine("Checking audio: " + bitrateAudio + "kb/s.");
                StartFFMPEGProcess(audioEncodeCommand + bitrateAudio + "k " + '"' + Path.GetTempPath() + "audio.ogg" + '"');
                FileInfo Audio = new FileInfo(Path.GetTempPath() + "audio.ogg");
                if (Video.Length + Audio.Length < sizeB)
                    break;
                else
                    if (encoderSpeed == 0)
                    bitrateAudio--;
                else
                {
                    bitrateAudio -= encoderSpeed;
                    if (bitrateAudio < 8)
                        ShowError("Audio bitrate is too low. Change the encoder speed (applied: " + encoderSpeed + ") to lower, and try again.");
                }
            }
            while (true);

        Muxing:
            string secondFile = string.Empty;
            if (File.Exists("EncodedVideo." + fileFormat))
                secondFile = "2";

            if (audioSetting == 0)
                StartFFMPEGProcess("-loglevel warning -y -i " + '"' + Path.GetTempPath() + "video.webm" + '"' + " -map 0:v:0 -c copy -map_metadata -1 -metadata author=" + videoCopyright + " EncodedVideo" + secondFile + "." + fileFormat);
            else
            {
                StartFFMPEGProcess("-loglevel warning -y -i " + '"' + Path.GetTempPath() + "video.webm" + '"' + " -i " + '"' + Path.GetTempPath() + "audio.ogg" + '"' + " -map 0:v:0 -map 1:a:0 -c copy -map_metadata -1 -metadata author=" + videoCopyright + " EncodedVideo" + secondFile + "." + fileFormat);
                File.Delete(Path.GetTempPath() + "audio.ogg");
            }
            File.Delete(Path.GetTempPath() + "video.webm");

            Console.Clear();
            Console.Title = "ClencMP | Finished successfully";

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(" ENCODING FINISHED SUCCESSFULLY ");
            Console.ResetColor();

            ShowGreenText("\nLook for: ", "EncodedVideo." + fileFormat + "\n");

            FileInfo MuxedVideo = new FileInfo("EncodedVideo." + fileFormat);
            uint finalFileSize = (uint)(MuxedVideo.Length / 1024);

            ShowGreenText("\nResult file size: ", finalFileSize + "KB");
            encodingTime.Stop();
            ShowGreenText("\nEncoding time: ", (encodingTime.ElapsedMilliseconds / 1000) + "s");

            if (audioSetting != 0)
                ShowGreenText("\nAudio bitrate: ", bitrateAudio + "kb/s");
                
            Console.WriteLine("\n\nPress any key to exit...");
            Console.WriteLine("================================================");
            Console.ResetColor();
            Console.ReadKey();
        }

        private static void StartFFMPEGProcess(string commandArgs)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = commandArgs,
                UseShellExecute = false,
            };
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                ShowError("FFMPEG failed. Please check data.\n\nUsed command: ffmpeg " + commandArgs);
        }

        private static void ShowError(string errorDescription)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n Error while encoding! ");
            Console.ResetColor();
            ShowRedInfo("Description: " + errorDescription);
            Console.ResetColor();
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            Environment.Exit(13);
            return;
        }

        private static string AddCustomArguments(string customArguments)
        {
            Console.Clear();
            ShowTitle("Advanced Settings > Add custom video arguments");

            Console.WriteLine("\n###Video Examples###");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("#Scale#\n-vf scale=-2:1080 -sws_flags lanczos\n");
            Console.WriteLine("#Change Framerate#\n-r 25, -r ntsc (29.97)\n");
            Console.WriteLine("#Subtitles#\n-vf ass=subs.ass\n");
            Console.WriteLine("#Crop#\n-vf crop=1696:1036:148:44\n");
            Console.WriteLine("#Sharpen#\n-sharpness 2\n");

            Console.ResetColor();
            Console.WriteLine("###Audio Examples###");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("#Audio Fade#\n-af afade=t=in:d=1,afade=t=out:d=1:st=719.000\n");
            Console.WriteLine("#Enable 5.1 audio#\n-ac 6\n");
            Console.WriteLine("#Downmix to Stereo#\nFrom 5.1: -af lowpass=c=LFE:f=120,volume=1.6,pan=stereo|FL=0.5*FC+0.707*FL+0.707*BL+0.5*LFE|FR=0.5*FC+0.707*FR+0.707*BR+0.5*LFE\nFrom 7.1: -af lowpass=c=LFE:f=120,volume=1.6,pan=stereo|FL= 0.5*FC+0.3*FLC+0.3*FL+0.3*BL+0.3*SL+0.5*LFE|FR=0.5*FC+0.3*FRC+0.3*FR+0.3*BR+0.3*SR+0.5*LFE\n");

            Console.ResetColor();

            if (customArguments != string.Empty && customArguments != " ")
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Last setting: " + customArguments);
                Console.ResetColor();
            }

            Console.WriteLine("\nAfter entering all arguments, press ENTER to apply.");
            Console.Write(":");

            Console.ForegroundColor = ConsoleColor.Cyan;
            string input = Console.ReadLine() + " ";
            return input;
        }

        private static string ProbeVideo(string commandArgs)
        {
            Process processProbe = new Process();
            ProcessStartInfo startInfoProbe = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = commandArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            processProbe.StartInfo = startInfoProbe;
            processProbe.Start();
            processProbe.WaitForExit();

            if (processProbe.ExitCode != 0)
                ShowError("Can not open the video. Please check data.\n\nUsed command: ffprobe " + commandArgs);

            return processProbe.StandardOutput.ReadToEnd();
        }

        private static string CheckHWAccel()
        {
            if (Environment.OSVersion.Platform.ToString() == "Unix")
                return "-hwaccel vaapi -hwaccel_device /dev/dri/renderD128 ";
            else if (Environment.OSVersion.Platform.ToString() == "Win32NT")
                return "-hwaccel nvdec ";
            else
                return string.Empty;
        }

        private static void ShowCyanText(string standardOutput, string coloredOutput)
        {
            Console.ResetColor();
            Console.Write(standardOutput);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(coloredOutput);
        }

        private static void ShowGreenText(string standardOutput, string coloredOutput)
        {
            Console.ResetColor();
            Console.Write(standardOutput);
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(coloredOutput);
        }

        private static void ShowTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.WriteLine(" " + title + " ");
            Console.ResetColor();
        }

        private static void ShowRedInfo(string information)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(information);
        }
    }