﻿/*
    The MIT License (MIT)
    Copyright © 2022 David Zangger

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
    and associated documentation files (the “Software”), to deal in the Software without 
    restriction, including without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom 
    the Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or 
    substantial portions of the Software.

    THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
    INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
    PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
    FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
    OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
    DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Linq;
using Spectre.Console;
using FluentColorConsole;
using System.Text;

namespace HdHomerun
{
    internal class Program
    {
        public static bool Simulate { get; set; }
        public static bool Verbose { get; set; }

        private static string PaintValue(string value)
        {
            return PaintValue(value, "yellow");
        }

        private static string PaintValue(string value, string color)
        {
            return $"[{color}]" + value + "[/]";
        }

        /*
         * ShowInfo
         * Show the known device information
         */
        private static void ShowDeviceInfo()
        {
            var table3 = new Table();
            table3.AddColumns("Friendly Name", "Model Number", "Device ID", "Device Auth", "Tuners");
            table3.AddRow(PaintValue(Homerun.DiscoveryInfo.FriendlyName),
                            PaintValue(Homerun.DiscoveryInfo.ModelNumber),
                            PaintValue(Homerun.DiscoveryInfo.DeviceID),
                            PaintValue(Homerun.DiscoveryInfo.DeviceAuth),
                            PaintValue(Homerun.DiscoveryInfo.TunerCount.ToString()));

            AnsiConsole.Write(table3);

            // Create a table
            var table = new Table();

            // Add some columns
            table.AddColumns("Base URL", "Local IP", "Firmware Name", "Firmware Version");
            table.AddRow(PaintValue(Homerun.DeviceInfo.BaseURL),
                            PaintValue(Homerun.DeviceInfo.LocalIP),
                            PaintValue(Homerun.DiscoveryInfo.FirmwareName),
                            PaintValue(Homerun.DiscoveryInfo.FirmwareVersion));

            // Render the table to the console
            AnsiConsole.Write(table);

            // Create a table
            var table2 = new Table();

            table2.AddColumns("Discover URL", "Lineup URL");
            table2.AddRow(PaintValue(Homerun.DeviceInfo.DiscoverURL), 
                            PaintValue(Homerun.DiscoveryInfo.LineupURL));
            AnsiConsole.Write(table2);

            float FreePct = ((float)Homerun.DiscoveryInfo.FreeSpace / (float)Homerun.DiscoveryInfo.TotalSpace) * 100;
            var table4 = new Table();
            table4.AddColumns("Storage URL / Storage ID", "Usage - Total Space (" + Homerun.DiscoveryInfo.TotalSpace / 1024 / 1024 / 1024 + " GB)");
            table4.AddRow(new Markup(PaintValue(Homerun.DiscoveryInfo.StorageURL + Environment.NewLine + Homerun.DiscoveryInfo.StorageID)),
                 new BarChart()
                    .Width(50)
                    .Label("")
                    .CenterLabel()
                    .AddItem("Free (" + Homerun.DiscoveryInfo.FreeSpace/1024/1024/1024 + " GB)", Math.Round(FreePct), Color.Green)
                    .AddItem("Used (" + Homerun.DiscoveryInfo.UsedSpace/1024/1024/1024 + " GB)", 100 - Math.Round(FreePct), Color.Yellow));
            AnsiConsole.Write(table4);
        }

        /// <summary>
        /// Init the HDHomerun device.  
        /// </summary>
        private static void Initalize()
        {
            try
            {
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .Start("[green3_1]Initializing...[/]", ctx => {
                        Homerun.Init();
                        ctx.Status("[green3_1]Getting serials...[/]");
                        Homerun.GetAllSeries();

                        foreach (Serial serial in Homerun.Series)
                        {
                            ctx.Status($"[green3_1]Getting recordings for {serial.Title}...[/]");
                            Homerun.GetRecordingsForSerial(serial);
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                Environment.Exit(1);
            }

            // Show some info
            string deviceInfo = $"{Homerun.DiscoveryInfo.FriendlyName} [{Homerun.DeviceInfo.DeviceId}] @ {Homerun.DeviceInfo.BaseURL}";

            AnsiConsole.MarkupLine("[green3_1]" + deviceInfo.EscapeMarkup() + "[/]");
        }

        /// <summary>
        /// This is the prompt to capture the users input
        /// </summary>
        /// <returns>The command entered by the user in lower case</returns>
        private static string ShowPrompt()
        {
            Console.WriteLine();
            AnsiConsole.Markup("[green3_1 on black]HDHomerun" + (Simulate ? " ([white on red]sim[/])" : "") + (Verbose ? "([white on gray]ver[/])" : "") + " >[/]");
            string command = Console.ReadLine();
 
            // clean up the command before we return it
            return command.ToLower().Trim();
        }

        /*
         * Writes a value surrounded by brackets
         */
        private static void WriteBracketedText(string text)
        {
            WriteBracketedText("", text);
        }
        private static void WriteBracketedText(string caption, string text)
        {
            ColorConsole.WithWhiteText.Write((caption.Length > 0 ? caption.PadRight(20, ' ') + " [" : "["));
            ColorConsole.WithBlueBackground.AndWhiteText.Write(text);
            ColorConsole.WithWhiteText.Write("] ");
        }
        private static void WriteLineBracketedRedText(string text)
        {
            ColorConsole.WithWhiteText.Write("[");
            ColorConsole.WithRedBackground.AndWhiteText.Write(text);
            ColorConsole.WithWhiteText.WriteLine("]");
        }
  
        /// <summary>
        /// Shows all the serials 
        /// </summary>
        private static void ShowSerials()
        {
            // Create a table
            var table = new Table();
            table.Border = TableBorder.Horizontal;
            table.Title = new TableTitle("[bold cyan]Series[/]");

            // Add some columns
            table.AddColumns("Seq", "Title", "Series ID");
            table.AddColumn(new TableColumn("Category").RightAligned());
            table.AddColumn(new TableColumn("Recordings").RightAligned());
            table.AddColumn(new TableColumn("Keep").RightAligned());           

            foreach (Serial serial in Homerun.Series)
            {
                string recordingsCount = $"[green3_1]{serial.Recordings.Count}[/]";

                // If we aren't keeping all the episodes, determine if we have too many
                if (serial.EpisodesToKeep != null && serial.Recordings.Count > serial.EpisodesToKeep.Value)
                {
                    recordingsCount = $"[red]{serial.Recordings.Count}[/]";
                }               

                table.AddRow("[white on blue]" + serial.Seq.ToString("0#") + "[/]", 
                    PaintValue(serial.Title),
                    PaintValue(serial.SeriesID),
                    PaintValue(serial.Category, "cyan"),
                    recordingsCount,
                    (serial.EpisodesToKeep.HasValue ? $"[green3_1]{serial.EpisodesToKeep.ToString()}[/]" : "[white]∞[/]"));
            }
            
            // Render the table to the console
            AnsiConsole.Write(table);

            // Show the totals
            Console.Write(" ");
            WriteLineBracketedRedText(Homerun.Series.Count.ToString("0#"));
        }

        /// <summary>
        /// Shows all the channels available to the user
        /// </summary>
        private static void ShowChannels()
        {
            // Create a table
            var table = new Table();
            table.Border(TableBorder.Horizontal);
            table.Title = new TableTitle("[bold cyan]Channels[/]");

            // Add some columns
            table.AddColumn("Seq");
            table.AddColumn("Guide #");
            table.AddColumn("Guide Name");
            table.AddColumn("Audio Codec");
            table.AddColumn("Video Codec");
            table.AddColumn("URL");

            foreach (Channel channel in Homerun.Channels)
            {
                table.AddRow("[white on blue]" + channel.Seq.ToString("0#") + "[/]",
                    channel.GuideNumber,
                    channel.GuideName,
                    String.IsNullOrEmpty(channel.AudioCodec) ? "" : channel.AudioCodec,
                    channel.VideoCodec,
                    channel.URL);
            }

            AnsiConsole.Write(table);

            // Show the totals
            Console.Write(" ");
            WriteLineBracketedRedText(Homerun.Channels.Count.ToString("0#"));
        }

        /*
         * ShowRecordings
         * Show all the recordings for all the series
         */
        private static void ShowRecordings()
        {
            foreach (Serial ser in Homerun.Series)
                ShowRecordings(ser);
        }

        /*
         * ShowRecordings
         * Show all the recordings for the selected serial
         */
        private static void ShowRecordings(Serial serial)
        {
            // If we don't have any recordings, check again
            if (serial.Recordings.Count == 0)
            {
                Homerun.GetRecordingsForSerial(serial);
            }

            // Get the recordings
            if (serial.Recordings.Count > 0)
            {
                string color = "yellow";

                var table = new Table();
                table.Border = TableBorder.Horizontal;
                table.Title =  new TableTitle("[bold cyan]" + serial.Title + "[/]");
                table.AddColumns("Seq", "Start Time", "Episode", "Title", "Length");
                
                foreach (Recording recording in serial.Recordings)
                {
                    string episodeNumber = String.IsNullOrEmpty(recording.EpisodeNumber) ? "?" : recording.EpisodeNumber;

                    // If we have more recordings than we want to keep, show those in red
                    if (serial.EpisodesToKeep != null)
                    {
                        if (recording.Seq > serial.EpisodesToKeep.Value)
                            color = "red";
                    }

                    // Add the recording to the table
                    table.AddRow(
                        "[white on " + (recording.Deleted ? "red" : "blue") + "]" + recording.Seq.ToString("0#") + "[/]",
                        PaintValue(Homerun.ToLocalTime(recording.StartTime), color),
                        PaintValue(episodeNumber, color),
                        PaintValue(recording.EpisodeTitle != null ? recording.EpisodeTitle : serial.Title, color),
                        PaintValue(recording.GetFileSize().ToString("##0,###") + " MB", color)
                    );

                    // If verbose is on, and there is a synopsis, show it
                    if (Verbose && recording.Synopsis != null)
                        table.AddRow(" ", "", "", PaintValue(recording.Synopsis, "grey50"));
                }

                AnsiConsole.Write(table);
                Console.Write(" ");
                WriteLineBracketedRedText(serial.Recordings.Count.ToString("0#"));
            }
        }

        private static void WriteErrorMessage(string error)
        {
            ColorConsole.WithRedText.WriteLine(error);
        }

        /// <summary>
        /// Shows the help for the ser command
        /// </summary>
        private static void ShowSerialHelp()
        {
            var table = new Table();
            table.Title = new TableTitle("[bold cyan]Serial Help[/]");
            table.AddColumns("Command", "Example(s)", "Details");
            table.AddRow("ser ?", "[white]ser ?[/]", "[green3_1]Shows this help[/]");
            table.AddRow("ser", "[white]ser[/]", "[green3_1]Show all the serials[/]");            
            table.AddRow("ser #", "[white]ser 2[/]", "[green3_1]For serial 2, show all recordings[/]");
            table.AddRow("", "[white]ser *[/]", "[green3_1]For all serials, show all recordings[/]");
            table.AddRow("ser # del @", "[white]ser 9 del 3[/]", "[green3_1]For serial 9 delete recording 3[/]");
            table.AddRow("", "[white]ser 8 del *[/]", "[green3_1]For serial 8 delete all recordings[/]");
            table.AddRow("ser # keep @", "[white]ser 4 keep 5[/]", "[green3_1]For serial 4 keep at most 5 recordings[/]");
            table.AddRow("", "[white]ser 7 keep *[/]", "[green3_1]For serial 7 keep all recordings[/]");
            table.AddRow("ser # clean", "[white]ser 3 clean[/]", "[green3_1]Remove any recordings for serial 3 beyond what should be kept[/]");
            table.AddRow("", "[white]ser * clean[/]", "[green3_1]Clean up recordings for all serials[/]");

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Shows the command available to the user
        /// </summary>
        private static void ShowHelp()
        {
            var table = new Table();
            table.Title = new TableTitle("[bold cyan]Help[/]");
            table.AddColumns("Command", "Description");
            table.AddRow("help, ?", "Shows this help");         
            table.AddRow("info", "Shows detailed information about your HDHomerun");
            table.AddRow("chan", "Shows details about all of your channels");
            table.AddRow("ser", "Show information about serials and recordings");
            table.AddRow("ser ?", "Show detailed help about the ser command");
            table.AddRow("sim", "Turn simulation on or off. When on, nothing will be deleted");
            table.AddRow("ver", "Turn verbose on or off. If a recording has a synopsis, it will be displayed");
            table.AddRow("quit, q", "Quits the application");

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Process the ser command
        /// </summary>
        /// <param name="userCommand"></param>
        private static void ProcessSerialCommand(string userCommand)
        {
            // Parse out the command
            Command command = new Command(userCommand);

            if (Simulate)
                ColorConsole.WithDarkBlueBackground.AndWhiteText.WriteLine(command.ToString());

            if (command.Valid)
            {
                // Show the help?
                if (command.Help)
                {
                    ShowSerialHelp();
                }
                else
                {
                    try
                    {
                        Serial serial = null;

                        // Get the series if we don't have them yet
                        if (Homerun.Series.Count == 0)
                            Homerun.GetAllSeries(true);

                        // If we have a sequence number, find the serial
                        if (command.Seq != null)
                        {
                            serial = Homerun.Series.FirstOrDefault(s => s.Seq == command.Seq);

                            // Make sure we have some recordings
                            if (serial != null && serial.Recordings.Count == 0)
                                Homerun.GetRecordingsForSerial(serial);
                        }

                        if (command.Action == null)
                        {
                            // Show the list of series (nothing to act upon)
                            if (command.Seq == null && !command.All)
                            {
                                ShowSerials();
                            }

                            if (command.Seq == null && command.All)
                            {
                                ShowRecordings();
                            }

                            if (command.Seq != null && !command.All && serial != null)
                            {
                                ShowRecordings(serial); 
                            }
                        }
                        else
                        {
                            // Get the serial information                            
                            if (command.Action.Equals("keep"))
                            {
                                if (command.Count != null || command.All)
                                {
                                    Homerun.SetEpisodesToKeep(serial, command.Count);
                                    ShowSerials();
                                }
                                else
                                    WriteErrorMessage("A valid number is required after keep");                            
                            } 
                            else if (command.Action.Equals("del"))
                            {
                                foreach(Recording recording in serial.Recordings)
                                {
                                    if ((command.Count != null && command.Count == recording.Seq && !command.All) || (command.All))
                                        recording.Delete(!Simulate);
                                }

                                if (!Simulate)
                                {
                                    Homerun.GetRecordingsForSerial(serial);
                                    Homerun.DoDiscovery();
                                }

                                ShowRecordings(serial);
                            }                     
                            else if (command.Action.Equals("clean"))
                            {
                                // Delete the files beyond what we want to keep
                                foreach (Serial ser in Homerun.Series)
                                {
                                    if ((command.Seq != null && command.Seq == ser.Seq && !command.All) || (command.All))
                                    {
                                        // If something was cleaned, refresh the recordings
                                        if (ser.Clean(!Simulate) > 0)
                                        {
                                            if (!Simulate)
                                                Homerun.GetRecordingsForSerial(ser);

                                            ShowRecordings(ser);
                                        }
                                    }
                                }

                                if (!Simulate)
                                    Homerun.DoDiscovery();
                            }
                            else
                            {
                                ColorConsole.WithRedBackground.AndBlackText.WriteLine(command.Action);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ColorConsole.WithRedText.WriteLine("Serial matching that sequence was not found.");
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            // Initalize the properties
            Simulate = false;
            Verbose = false;
            bool done = false;

            // Was anything passed into the command prompt?
            foreach (string arg in args)
            {
                if (arg.Equals("/simulate", StringComparison.CurrentCultureIgnoreCase))
                    Simulate = true;

                if (arg.Equals("/verbose", StringComparison.CurrentCultureIgnoreCase))
                    Verbose = true;
            }

            // Init the device information
            Initalize();
            ShowSerials();

            do
            {
                string command = ShowPrompt();

                // Execute the command
                if (command.Equals("help") || command.Equals("?"))
                {
                    ShowHelp();
                }
                else if (command.Equals("quit") || command.Equals("q"))
                {
                    done = true;
                }
                else if (command.Equals("info"))
                {
                    ShowDeviceInfo();
                }
                else if (command.StartsWith("ver"))
                {
                    Verbose = !Verbose;
                }
                else if (command.StartsWith("sim"))
                {
                    Simulate = !Simulate;
                }
                else if (command.StartsWith("ser"))
                {
                    ProcessSerialCommand(command);
                }
                else if (command.StartsWith("chan"))
                {
                    try
                    {
                        Homerun.GetChannels(command.Contains("-f"));
                        ShowChannels();
                    }
                    catch (Exception ex)
                    {
                        ColorConsole.WithRedText.WriteLine(ex.Message);
                    }
                }
                else if (command.Length > 0)
                {
                    ColorConsole.WithRedText.WriteLine($"{command} is an unknown command");
                }

            } while (!done);
        }
    }
}