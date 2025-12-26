using System;
using System.IO;
using System.Text.Json;
using TReader.Models;

namespace TReader.Services;

/// <summary>
/// 设置服务实现
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TReader"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
    }

    public ReaderSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new ReaderSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<ReaderSettings>(json, JsonOptions) ?? new ReaderSettings();
        }
        catch
        {
            return new ReaderSettings();
        }
    }

    public void Save(ReaderSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    public void EnsureValid()
    {
        if (!File.Exists(_settingsPath))
        {
            // 配置文件不存在，创建默认配置
            Save(new ReaderSettings());
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<ReaderSettings>(json, JsonOptions);
            if (settings == null)
            {
                // 反序列化失败，重置为默认
                Save(new ReaderSettings());
            }
        }
        catch
        {
            // JSON 解析失败，重置为默认
            Save(new ReaderSettings());
        }
    }

    public void ResetToDefault()
    {
        Save(new ReaderSettings());
    }
}
