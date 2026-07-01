using EthanTcm.Domain.Common;
using EthanTcm.Domain.Enums;

namespace EthanTcm.Domain.Entities;

public sealed class SystemSetting : AuditableEntity
{
    private SystemSetting()
    {
    }

    public SystemSetting(string key, string value, SystemSettingValueType valueType, string? description = null)
    {
        Key = EntityGuards.Required(key, nameof(Key));
        Value = EntityGuards.Required(value, nameof(Value));
        ValueType = valueType;
        Description = description;
    }

    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public SystemSettingValueType ValueType { get; private set; }
    public string? Description { get; private set; }

    public void ChangeValue(string value, DateTimeOffset timestamp)
    {
        Value = EntityGuards.Required(value, nameof(value));
        MarkUpdated(timestamp);
    }
}
