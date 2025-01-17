﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Core {

    public class PathManager : SingletonBase<PathManager> {
        public PathManager() {
            RootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (OS.IsMacOS()) {
                HomePath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal), "Library", "OpenUtau");
                HomePathIsAscii = true;
            } else if (OS.IsLinux()) {
                HomePath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal), "OpenUtau");
                HomePathIsAscii = true;
            } else {
                HomePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                HomePathIsAscii = true;
                var etor = StringInfo.GetTextElementEnumerator(HomePath);
                while (etor.MoveNext()) {
                    string s = etor.GetTextElement();
                    if (s.Length != 1 || s[0] >= 128) {
                        HomePathIsAscii = false;
                        break;
                    }
                }
            }
            Log.Logger.Information($"Home path = {HomePath}");
        }

        public string RootPath { get; private set; }
        public string HomePath { get; private set; }
        public bool HomePathIsAscii { get; private set; }
        public string SingersPathOld => Path.Combine(HomePath, "Content", "Singers");
        public string SingersPath => Path.Combine(HomePath, "Singers");
        public string AdditionalSingersPath => Preferences.Default.AdditionalSingerPath;
        public string SingersInstallPath => Preferences.Default.InstallToAdditionalSingersPath
            && !string.IsNullOrEmpty(Preferences.Default.AdditionalSingerPath)
                ? AdditionalSingersPath
                : SingersPath;
        public string ResamplersPath => Path.Combine(HomePath, "Resamplers");
        public string PluginsPath => Path.Combine(HomePath, "Plugins");
        public string DictionariesPath => Path.Combine(HomePath, "Dictionaries");
        public string TemplatesPath => Path.Combine(HomePath, "Templates");
        public string LogFilePath => Path.Combine(HomePath, "Logs", "log.txt");
        public string PrefsFilePath => Path.Combine(HomePath, "prefs.json");
        public string NotePresetsFilePath => Path.Combine(HomePath, "notepresets.json");
        public string CachePath => Path.Combine(HomePath, "Cache");

        public string GetPartSavePath(string projectPath, int partNo) {
            var name = Path.GetFileNameWithoutExtension(projectPath);
            var dir = Path.GetDirectoryName(projectPath);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{name}-{partNo:D2}.ust");
        }

        public string GetExportPath(string exportPath, int trackNo) {
            var name = Path.GetFileNameWithoutExtension(exportPath);
            var dir = Path.GetDirectoryName(exportPath);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{name}-{trackNo:D2}.wav");
        }

        public void ClearCache() {
            var files = Directory.GetFiles(CachePath);
            foreach (var file in files) {
                try {
                    File.Delete(file);
                } catch (Exception e) {
                    Log.Error(e, $"Failed to delete file {file}");
                }
            }
            var dirs = Directory.GetDirectories(CachePath);
            foreach (var dir in dirs) {
                try {
                    Directory.Delete(dir, true);
                } catch (Exception e) {
                    Log.Error(e, $"Failed to delete dir {dir}");
                }
            }
        }

        readonly static string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        public string GetCacheSize() {
            if (!Directory.Exists(CachePath)) {
                return "0B";
            }
            var dir = new DirectoryInfo(CachePath);
            double size = dir.GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            int order = 0;
            while (size >= 1024 && order < sizes.Length - 1) {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##}{sizes[order]}";
        }
    }
}
