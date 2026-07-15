using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace BossKey.Utils
{
    /// <summary>
    /// 标记该字段不参与配置的加载与保存。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NotConfigAttribute : Attribute { }

    /// <summary>
    /// 简单的 JSON 配置文件加载/保存工具，仅支持 bool、int、double、string 四种字段类型。
    /// </summary>
    public static class ConfigLoader
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// 从 JSON 文件加载配置到对象 o 的所有字段和可读写属性中。
        /// </summary>
        /// <typeparam name="T">配置对象类型</typeparam>
        /// <param name="path">JSON 文件路径</param>
        /// <param name="o">要填充的配置对象</param>
        /// <returns>加载成功返回 true，文件不存在或解析失败返回 false</returns>
        public static bool Load<T>(string path, T o)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                string json = File.ReadAllText(path);
                using JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return false;

                Type type = typeof(T);

                // 处理字段
                foreach (FieldInfo field in type.GetFields(MemberFlags))
                {
                    if (field.IsDefined(typeof(CompilerGeneratedAttribute)))
                        continue;
                    if (field.IsDefined(typeof(NotConfigAttribute)))
                        continue;

                    if (!doc.RootElement.TryGetProperty(field.Name, out JsonElement element))
                        continue;

                    LoadMember(element, field.FieldType, v => field.SetValue(o, v));
                }

                // 处理可读写属性（自动属性）
                foreach (PropertyInfo prop in type.GetProperties(MemberFlags))
                {
                    if (!prop.CanRead || !prop.CanWrite)
                        continue;
                    if (prop.IsDefined(typeof(CompilerGeneratedAttribute)))
                        continue;
                    if (prop.IsDefined(typeof(NotConfigAttribute)))
                        continue;

                    if (!doc.RootElement.TryGetProperty(prop.Name, out JsonElement element))
                        continue;

                    LoadMember(element, prop.PropertyType, v => prop.SetValue(o, v));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将对象 o 的所有字段保存为 JSON 文件。
        /// </summary>
        /// <typeparam name="T">配置对象类型</typeparam>
        /// <param name="path">JSON 文件路径</param>
        /// <param name="o">要保存的配置对象</param>
        /// <returns>保存成功返回 true，失败返回 false</returns>
        public static bool Save<T>(string path, T o)
        {
            // 要确保保存目录存在
            string? dir = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);

                writer.WriteStartObject();

                Type type = typeof(T);

                // 处理字段
                foreach (FieldInfo field in type.GetFields(MemberFlags))
                {
                    if (field.IsDefined(typeof(CompilerGeneratedAttribute)))
                        continue;
                    if (field.IsDefined(typeof(NotConfigAttribute)))
                        continue;

                    object? value = field.GetValue(o);
                    SaveMember(writer, field.Name, value, field.FieldType);
                }

                // 处理可读写属性（自动属性）
                foreach (PropertyInfo prop in type.GetProperties(MemberFlags))
                {
                    if (!prop.CanRead || !prop.CanWrite)
                        continue;
                    if (prop.IsDefined(typeof(CompilerGeneratedAttribute)))
                        continue;
                    if (prop.IsDefined(typeof(NotConfigAttribute)))
                        continue;

                    object? value = prop.GetValue(o);
                    SaveMember(writer, prop.Name, value, prop.PropertyType);
                }

                writer.WriteEndObject();
                writer.Flush();

                string json = Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断目标类型是否可接受 null 值（string 或 Nullable&lt;T&gt;）。
        /// </summary>
        private static bool CanAcceptNull(Type type)
        {
            return type == typeof(string)
                || Nullable.GetUnderlyingType(type) is not null
                || typeof(IStringifyable).IsAssignableFrom(type);
        }

        /// <summary>
        /// 从 JsonElement 加载一个成员（字段或属性），自动处理 null 和 Nullable&lt;T&gt;。
        /// </summary>
        private static void LoadMember(JsonElement element, Type targetType, Action<object?> setter)
        {
            // JSON null → 仅对 string 或 Nullable<T> 设置为 null
            if (element.ValueKind == JsonValueKind.Null)
            {
                if (CanAcceptNull(targetType))
                    setter(null);

                return;
            }

            Type? underlying = Nullable.GetUnderlyingType(targetType);
            Type actualType = underlying ?? targetType;

            object? value = null;

            if (actualType == typeof(bool))
            {
                if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    value = element.GetBoolean();
            }
            else if (actualType == typeof(int))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int v))
                    value = v;
            }
            else if (actualType == typeof(double))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out double v))
                    value = v;
            }
            else if (actualType == typeof(string))
            {
                if (element.ValueKind == JsonValueKind.String)
                    value = element.GetString();
            }
            else if (typeof(IStringifyable).IsAssignableFrom(actualType))
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    string? s = element.GetString();

                    if (s is not null)
                    {
                        var obj = (IStringifyable)RuntimeHelpers.GetUninitializedObject(actualType)!;
                        obj.Deserialize(s);
                        value = obj;
                    }
                }
            }

            if (value is not null)
                setter(value);
        }

        /// <summary>
        /// 将一个成员（字段或属性）的值写入 Utf8JsonWriter，自动处理 null 和 Nullable&lt;T&gt;。
        /// </summary>
        private static void SaveMember(Utf8JsonWriter writer, string name, object? value, Type fieldType)
        {
            if (value is null)
            {
                if (CanAcceptNull(fieldType))
                    writer.WriteNull(name);

                return;
            }

            Type? underlying = Nullable.GetUnderlyingType(fieldType);
            Type actualType = underlying ?? fieldType;

            if (actualType == typeof(bool))
            {
                writer.WriteBoolean(name, (bool)value);
            }
            else if (actualType == typeof(int))
            {
                writer.WriteNumber(name, (int)value);
            }
            else if (actualType == typeof(double))
            {
                writer.WriteNumber(name, (double)value);
            }
            else if (actualType == typeof(string))
            {
                writer.WriteString(name, (string)value);
            }
            else if (typeof(IStringifyable).IsAssignableFrom(actualType))
            {
                var obj = (IStringifyable)value;
                writer.WriteString(name, obj.Serialize());
            }

            // 其他类型：跳过，不写入 JSON
        }
    }

    public interface IStringifyable
    {
        string Serialize();

        void Deserialize(string s);
    }
}
