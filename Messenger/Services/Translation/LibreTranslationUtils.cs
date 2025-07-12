using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Services.Translation;
public unsafe static class LibreTranslationUtils
{
    public static readonly Dictionary<string, string> Languages = new()
    {
        {"English", "en"},
        {"Arabic", "ar"},
        {"Azerbaijani", "az"},
        {"Chinese", "zh"},
        {"Czech", "cs"},
        {"Danish", "da"},
        {"Dutch", "nl"},
        {"Esperanto", "eo"},
        {"Finnish", "fi"},
        {"French", "fr"},
        {"German", "de"},
        {"Greek", "el"},
        {"Hebrew", "he"},
        {"Hindi", "hi"},
        {"Hungarian", "hu"},
        {"Indonesian", "id"},
        {"Irish", "ga"},
        {"Italian", "it"},
        {"Japanese", "ja"},
        {"Korean", "ko"},
        {"Persian", "fa"},
        {"Polish", "pl"},
        {"Portuguese", "pt"},
        {"Russian", "ru"},
        {"Slovak", "sk"},
        {"Spanish", "es"},
        {"Swedish", "sv"},
        {"Turkish", "tr"},
        {"Ukrainian", "uk"}
    };

    public static Process? StartLibreTranslateServer(string pythonPath = "python.exe", string host = "127.0.0.1", int port = 5000)
    {
        try
        {
            string? pythonFullPath = LocateExecutable(pythonPath);
            if(pythonFullPath == null)
            {
                PluginLog.Warning("python.exe not found.");
                return null;
            }

            string scriptsDir = Path.Combine(Path.GetDirectoryName(pythonFullPath)!, "Scripts");
            string libreExePath = Path.Combine(scriptsDir, "libretranslate.exe");

            if(!File.Exists(libreExePath))
                libreExePath = LocateExecutable("libretranslate.exe");

            if(libreExePath == null)
            {
                PluginLog.Warning("libretranslate.exe not found.");
                return null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = libreExePath,
                Arguments = $"--host {host} --port {port}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (s, e) => { if(e.Data != null) PluginLog.Information(e.Data); };
            process.ErrorDataReceived += (s, e) => { if(e.Data != null) PluginLog.Warning(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            PluginLog.Information($"LibreTranslate server started at http://{host}:{port}");
            return process;
        }
        catch(Exception ex)
        {
            PluginLog.Warning($"Error starting LibreTranslate: {ex.Message}");
            return null;
        }
    }

    private static string? LocateExecutable(string exeName)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach(var dir in paths)
        {
            try
            {
                string fullPath = Path.Combine(dir.Trim(), exeName);
                if(File.Exists(fullPath))
                    return Path.GetFullPath(fullPath);
            }
            catch { }
        }
        return null;
    }
}