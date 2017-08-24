﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Celtic_Guardian
{
    public static class Utilities
    {
        public enum Event
        {
            Warning = 0,
            Information = 1,
            Error = 2,
            Alert = 3
        }

        public static long DirSize(DirectoryInfo Directory)
        {
            var FileInfo = Directory.GetFiles();
            var Size = FileInfo.Sum(Info => Info.Length);
            var DirSized = Directory.GetDirectories();
            Size += DirSized.Sum(Dir => DirSize(Dir));

            return Size;
        }

        private static readonly string[] SizeSuffixes =
            {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};

        public static int IsAligned(int Number)
        {
            if (Number % 4 == 0) return Number;

            while (Number % 4 != 0)
                Number = Number + 1;

            return Number;
        }

        public static void CreateDummyFile(string FileName, long Length)
        {
            using (var Stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Stream.SetLength(Length);
            }
        }

        public static void Log(string Message, Event LogLevel, bool ShouldQuit = false, int ExitCode = 0)
        {
            switch (LogLevel)
            {
                case Event.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("[Warning]: ");
                    Console.ResetColor();
                    break;

                case Event.Information:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("[Information]: ");
                    Console.ResetColor();
                    break;

                case Event.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[Error]: ");
                    Console.ResetColor();
                    break;

                case Event.Alert:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[Information]: ");
                    Console.ResetColor();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(LogLevel), LogLevel, null);
            }
            Console.WriteLine(Message);

            if (ShouldQuit)
                Environment.Exit(ExitCode);
        }

        public static bool IsExt(string File, string Extension)
        {
            return new FileInfo(File).Extension.ToLower() == Extension;
        }

        public static int HexToDec(string HexValue,bool CheckAlignment=false)
        {
            var Number = Int32.Parse(HexValue, NumberStyles.HexNumber);
            if (CheckAlignment)
                Number = IsAligned(Number);

            return Number;
        }

        public static int HexToDec(byte[] Data, bool CheckAlignment = false)
        {
            return HexToDec(BitConverter.ToString(Data).Replace("-", ""),true);
        }

        public static string DecToHex(string DecValue)
        {
            return Int32.Parse(DecValue).ToString("x");
        }

        public static string GetText(byte[] Message, bool RemoveNull = true)
        {
            var StrContent = Encoding.ASCII.GetString(Message);

            if (RemoveNull)
                StrContent = StrContent.Replace("\0", String.Empty);

            return StrContent;
        }

        public static string GiveFileSize(long Value, int DecimalPlaces = 1)
        {
            if (Value < 0)
                return "-" + GiveFileSize(-Value);
            var I = 0;
            decimal DValue = Value;
            while (Math.Round(DValue, DecimalPlaces) >= 1000)
            {
                DValue /= 1024;
                I++;
            }
            return string.Format("{0:n" + DecimalPlaces + "} {1}", DValue, SizeSuffixes[I]);
        }

        [STAThread]
        public static string GetInstallDir()
        {
            string InstallDir;
            try
            {
                using (var Root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var Key =
                        Root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 480650"))
                    {
                        InstallDir = Key?.GetValue("InstallLocation").ToString();
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception("Game Not Found");
            }
            return InstallDir;
        }

        public static string GetHashOfFile(string FileName)
        {
            using (var Hash = MD5.Create())
            {
                using (var Stream = File.OpenRead(FileName))
                {
                    return BitConverter.ToString(Hash.ComputeHash(Stream)).Replace("-", String.Empty).ToLower();
                }
            }
        }

        
    }
}