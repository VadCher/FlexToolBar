using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlexToolBar.Core
{
    public class LayoutSnapshot : ViewModelBase
    {
        [JsonInclude]
        internal Dictionary<string, FlexToolBarViewModel> Models { set; get; } = new();

        public string ActiveThemeName
        {
            get;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                RaiseAndSetIfChanged(ref field, value);
            }
        } = "Default";

        public double GroupSpacing
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        } = 6.0;

    }
    public class FlexLayoutManager : LayoutSnapshot
    {
        public static FlexLayoutManager Instance { get; } = new FlexLayoutManager();
        static FlexLayoutManager()
        {
            LoadLayout();
        }
        private const string LayoutFileName = "toolBarLayout.json";

        public bool IsEdited { get; private set; }

        private FlexLayoutManager()
        {
        }

        internal void SetIsEdited()
        {
            if (IsEdited) return;
            IsEdited = true;
            OnPropertyChanged(nameof(IsEdited));
        }

        internal void ResetIsEdited()
        {
            if (!IsEdited) return;
            IsEdited = false;
            OnPropertyChanged(nameof(IsEdited));
        }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static string GetFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LayoutFileName);
        }

        // 2. Factory Endpoint: Resolves or registers active workflow view models
        public static FlexToolBarViewModel? GetToolBar(string toolBarId)
        {
            if (string.IsNullOrEmpty(toolBarId)) return null;

            if (Instance.Models.TryGetValue(toolBarId, out var existingBar))
            {
                return existingBar;
            }

            var newBar = new FlexToolBarViewModel { IsNew = true };
            Instance.Models[toolBarId] = newBar;

            return newBar;
        }

        // 1. Monolithic Save: Commits the entire root manager instance structure to a single transaction file
        public static void SaveLayout()
        {
            Instance.ResetIsEdited();

            try
            {
                // Serializes the whole manager singleton object (including global theme and internal models dictionary)
                string json = JsonSerializer.Serialize(Instance, SerializerOptions);
                File.WriteAllText(GetFilePath(), json);
            }
            catch { }
        }

        // 2. Monolithic Load: Restores and deep-copies the entire state tree in a single root reflection pass
        public static bool LoadLayout()
        {
            Instance.ResetIsEdited();

            string filePath = GetFilePath();
            if (!File.Exists(filePath)) return false;

            try
            {
                string json = File.ReadAllText(filePath);

                // Reconstruct the structural layout blueprint configuration from disk
                var layoutSnapshot = JsonSerializer.Deserialize<LayoutSnapshot>(json, SerializerOptions);
                if (layoutSnapshot == null) return false;
                Instance.ActiveThemeName = layoutSnapshot.ActiveThemeName;
                Instance.GroupSpacing = layoutSnapshot.GroupSpacing;
                Instance.Models = layoutSnapshot.Models;
                // CopyProperties(loadedManager, Instance, typeof(LayoutSnapshot));

                Instance.ResetIsEdited();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static event Action? LayoutResetRequested;

        public static void DeleteLayout()
        {
            string filePath = GetFilePath();
            if (File.Exists(filePath)) File.Delete(filePath);

            LayoutResetRequested?.Invoke();
        }

        // private static void CopyProperties(object src, object target, Type? explicitType = default)
        // {
        //     if (src == null || target == null) return;

        //     Type typeToScan = explicitType ?? src.GetType();

        //     if (explicitType == null && src.GetType() != target.GetType())
        //     {
        //         throw new ArgumentException("Arguments have different runtime types and no explicit type contract was provided.");
        //     }

        //     foreach (PropertyInfo info in typeToScan.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        //     {
        //         if (info.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Length > 0) continue;
        //         if (!info.CanRead) continue;

        //         object? srcValue = info.GetValue(src, null);
        //         if (srcValue == null) continue;

        //         if (srcValue is IEnumerable srcEnum && info.PropertyType != typeof(string))
        //         {
        //             var targetEnum = info.GetValue(target, null) as IEnumerable;
        //             if (targetEnum != null)
        //             {
        //                 IEnumerator srcIterator = srcEnum.GetEnumerator();
        //                 IEnumerator targetIterator = targetEnum.GetEnumerator();

        //                 while (srcIterator.MoveNext() && targetIterator.MoveNext())
        //                 {
        //                     object srcCurrent = srcIterator.Current;
        //                     object targetCurrent = targetIterator.Current;

        //                     if (srcCurrent == null || targetCurrent == null) continue;

        //                     Type currentType = srcCurrent.GetType();
        //                     if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        //                     {
        //                         object? key = currentType.GetProperty("Key")?.GetValue(srcIterator.Current, null);
        //                         object? srcChild = currentType.GetProperty("Value")?.GetValue(srcIterator.Current, null);
        //                         object? targetChild = currentType.GetProperty("Value")?.GetValue(targetIterator.Current, null);

        //                         if (srcChild != null && targetChild != null)
        //                         {
        //                             CopyProperties(srcChild, targetChild, default);
        //                         }
        //                     }
        //                     else
        //                     {
        //                         CopyProperties(srcCurrent, targetCurrent, default);
        //                     }
        //                 }
        //             }
        //             continue;
        //         }

        //         if (info.CanWrite)
        //         {
        //             if (!info.PropertyType.IsPrimitive && info.PropertyType != typeof(string) && !info.PropertyType.IsValueType)
        //             {
        //                 object? targetValue = info.GetValue(target, null);
        //                 if (targetValue != null)
        //                 {
        //                     CopyProperties(srcValue, targetValue, default);
        //                     continue;
        //                 }
        //             }

        //             info.SetValue(target, srcValue, null);
        //         }
        //     }
        // }
    }
}
