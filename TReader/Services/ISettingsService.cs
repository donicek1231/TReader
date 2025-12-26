using TReader.Models;

namespace TReader.Services;

/// <summary>
/// 设置服务接口
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 加载设置
    /// </summary>
    ReaderSettings Load();

    /// <summary>
    /// 保存设置
    /// </summary>
    void Save(ReaderSettings settings);

    /// <summary>
    /// 确保配置文件存在且有效，如果不存在或无效则创建默认配置
    /// </summary>
    void EnsureValid();

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    void ResetToDefault();
}
