using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Xname.DeveloperTools.Error;

public sealed class ErrorHandler
{
    public ErrorHandler()
    {
        _serverErrorLogsDirectoryPath = Path.Combine(Plugin.Handler.PluginDirectoryPath, Server.Port.ToString());

        if (!Directory.Exists(_serverErrorLogsDirectoryPath) && Plugin.Config.LoggingToFileEnabled)
            Directory.CreateDirectory(_serverErrorLogsDirectoryPath);
    }

    ~ErrorHandler()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
        _initialized = false;
        _errorLoggingThread?.Abort();
    }

    private static StreamWriter NewLog => new(Path.Combine(
        _serverErrorLogsDirectoryPath,
        $"Error Log {(DateTime.Now - Round.Duration):yyyy-MM-dd HH.mm.ss}.txt"),
        true);

    private static readonly List<ErrorLog> _logs = new();
    private static readonly object _lock = new();
    private static string _serverErrorLogsDirectoryPath;
    private static Thread _errorLoggingThread;
    private static bool _initialized = false;
    private static bool _newRound = false;

    [PluginEvent]
    private void OnMapGenerated(MapGeneratedEvent ev)
    {
        if (_initialized)
        {
            _newRound = true;
            return;
        }

        _initialized = true;

        if (Plugin.Config.LoggingToFileEnabled)
        {
            _errorLoggingThread = new Thread(new ThreadStart(WriteLogs))
            {
                Name = "Error Logging Thread",
                IsBackground = true,
            };

            _errorLoggingThread.Start();
        }

        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type is not LogType.Error and not LogType.Exception)
            return;

        if (stackTrace.Contains("Clutter") || condition.Contains("A scripted object"))
            return;

        if (Plugin.Config.LoggingToConsoleEnabled)
        {
            Log.Error($"Catched Unity Message of type {type}:");
            Log.Error(condition + "\n" + stackTrace);
        }

        if (Plugin.Config.LoggingToFileEnabled)
        {
            lock (_lock)
                _logs.Add(new(condition, stackTrace, type));
        }
    }

    private void WriteLogs()
    {
        StreamWriter file = null;

        while (_initialized)
        {
            if (_newRound)
            {
                file?.Dispose();
                file = null;
            }

            lock (_lock)
            {
                if (Plugin.Config.LogBlacklist?.Count > 0)
                {
                    foreach (ErrorLog log in _logs.ToArray())
                    {
                        foreach (string blacklisted in Plugin.Config.LogBlacklist)
                        {
                            if (log.Condition.Contains(blacklisted, StringComparison.OrdinalIgnoreCase) ||
                                log.StackTrace.Contains(blacklisted, StringComparison.OrdinalIgnoreCase))
                                _logs.Remove(log);
                        }
                    }
                }

                if (_logs.Count > 0 && file is null)
                    file = NewLog;

                foreach (ErrorLog log in _logs)
                {
                    string toWrite = $"[{DateTime.Now:yyyy-MM-dd HH.mm.ss.fff zzz}] Catched Unity Message of type {log.Type}:\n{log.Condition}\n{log.StackTrace}";
                    file.WriteLine(toWrite);
                }

                file?.Flush();
                _logs.Clear();
            }

            Thread.Sleep(1000);
        }

        file?.Dispose();
    }
}
