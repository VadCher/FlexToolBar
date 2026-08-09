using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlexToolBar.Core
{
    public class FlexLayoutManager
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private string GetFilePath(string autoSaveId)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{autoSaveId}.json");
        }

        public void SaveLayout(FlexToolBarViewModel viewModel, string autoSaveId)
        {
            if (viewModel == null || string.IsNullOrWhiteSpace(autoSaveId)) return;

            viewModel.ResetIsEdited();

            try
            {
                string json = JsonSerializer.Serialize(viewModel, SerializerOptions);
                File.WriteAllText(GetFilePath(autoSaveId), json);
            }
            catch { }
        }

        public bool LoadLayout(FlexToolBarViewModel viewModel, string autoSaveId)
        {
            if (viewModel == null || string.IsNullOrWhiteSpace(autoSaveId)) return false;

            viewModel.ResetIsEdited();

            string filePath = GetFilePath(autoSaveId);
            if (!File.Exists(filePath)) return false;

            try
            {
                string json = File.ReadAllText(filePath);

                var testViewModel = JsonSerializer.Deserialize<FlexToolBarViewModel>(json, SerializerOptions);
                if (testViewModel == null) return false;

                CopyProperties(testViewModel, viewModel);

                viewModel.ResetIsEdited();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        
        public void DeleteLayout(string autoSaveId)
        {
            if (string.IsNullOrWhiteSpace(autoSaveId)) return;
            try
            {
                string filePath = GetFilePath(autoSaveId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch { }
        }

        private static void CopyProperties(object src, object target)
        {
            if (src == null || target == null) return;

            Type type = src.GetType();
            if (type != target.GetType()) throw new ArgumentException("Arguments have different types.");

            foreach (PropertyInfo info in type.GetProperties())
            {
                if (info.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Length > 0) continue;
                if (!info.CanRead) continue;

                object? srcValue = info.GetValue(src, null);
                if (srcValue == null) continue;

                if (srcValue is IEnumerable srcEnum && info.PropertyType != typeof(string))
                {
                    var targetEnum = info.GetValue(target, null) as IEnumerable;
                    if (targetEnum != null)
                    {
                        IEnumerator srcIterator = srcEnum.GetEnumerator();
                        IEnumerator targetIterator = targetEnum.GetEnumerator();

                        while (srcIterator.MoveNext() && targetIterator.MoveNext())
                        {
                            object srcCurrent = srcIterator.Current;
                            object targetCurrent = targetIterator.Current;

                            if (srcCurrent == null || targetCurrent == null) continue;

                            Type currentType = srcCurrent.GetType();
                            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                            {
                                object? key = currentType.GetProperty("Key")?.GetValue(srcIterator.Current, null);
                                object? srcChild = currentType.GetProperty("Value")?.GetValue(srcIterator.Current, null);

                                object? targetChild = currentType.GetProperty("Value")?.GetValue(targetIterator.Current, null);

                                if (srcChild != null && targetChild != null)
                                {
                                    CopyProperties(srcChild, targetChild);
                                }
                            }
                            else
                            {
                                CopyProperties(srcCurrent, targetCurrent);
                            }
                        }
                    }
                    continue;
                }

                if (info.CanWrite)
                {
                    if (!info.PropertyType.IsPrimitive && info.PropertyType != typeof(string) && !info.PropertyType.IsValueType)
                    {
                        object? targetValue = info.GetValue(target, null);
                        if (targetValue != null)
                        {
                            CopyProperties(srcValue, targetValue);
                            continue;
                        }
                    }

                    info.SetValue(target, srcValue, null);
                }
            }
        }
    }
}
