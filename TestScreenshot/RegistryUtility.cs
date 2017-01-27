using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.Windows.Forms;
using System.Globalization;

namespace TestScreenshot
{
    public static class RegistryUtility
    {
        static string GetKeyName(string path)
        {
            string appName = Application.ProductName;

            string keyName = "Software\\" + appName;
            if (!String.IsNullOrEmpty(path))
            {
                keyName += "\\" + path;
            }
            return keyName;
        }

        static RegistryKey OpenSubKey(string fullPath, bool writable)
        {
            string[] dirs = fullPath.Split('\\');
            RegistryKey key = Registry.CurrentUser;
            foreach (var dir in dirs)
            {
                var nextKey = key.OpenSubKey(dir, writable);
                if (nextKey == null)
                {
                    if (!writable)
                    {
                        key.Close();
                        return null;
                    }
                    else
                    {
                        nextKey = key.CreateSubKey(dir);
                    }
                }
                key.Close();
                key = nextKey;
            }
            return key;
        }

        static bool SubKeyExists(string fullPath)
        {
            using (var key = OpenSubKey(fullPath, false))
            {
                if (key == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static string GetSetting(string settingName, string path, string defaultValue)
        {
            string value;
            if (TryGetSetting(settingName, path, out value))
            {
                return value;
            }
            return defaultValue;
        }

        public static bool TryGetSetting(string settingName, string path, out string value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, false))
            {
                if (k1 != null)
                {
                    if (k1.GetValueNames().Contains(settingName))
                    {
                        var kind = k1.GetValueKind(settingName);
                        if (kind == RegistryValueKind.String)
                        {
                            value = (string)k1.GetValue(settingName);
                            return true;
                        }
                        if (kind == RegistryValueKind.MultiString)
                        {
                            string[] arr = (string[])k1.GetValue(settingName);
                            if (arr.Length > 0)
                            {
                                if (arr.Length == 1)
                                {
                                    value = arr[0];
                                }
                                else
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (var line in arr)
                                    {
                                        if (sb.Length > 0)
                                        {
                                            sb.AppendLine();
                                        }
                                        sb.Append(line);
                                    }
                                    value = sb.ToString();
                                }
                            }
                            else
                            {
                                value = string.Empty;
                            }
                            return true;
                        }
                        if (kind == RegistryValueKind.DWord)
                        {
                            int intValue = (int)k1.GetValue(settingName);
                            value = intValue.ToString();
                            return true;
                        }
                    }
                }
            }
            value = null;
            return false;
        }

        public static bool TryGetSetting(string settingName, string path, ICollection<string> value)
        {
            string[] stringArray;
            if (TryGetSetting(settingName, path, out stringArray))
            {
                value.Clear();
                foreach (var item in stringArray)
                {
                    value.Add(item);
                }
                return true;
            }
            return false;
        }

        public static bool TryGetSetting(string settingName, string path, out string[] value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, false))
            {
                if (k1 != null)
                {
                    if (k1.GetValueNames().Contains(settingName))
                    {
                        var kind = k1.GetValueKind(settingName);
                        if (kind == RegistryValueKind.String)
                        {
                            value = ((string)k1.GetValue(settingName)).Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                            return true;
                        }
                        if (kind == RegistryValueKind.MultiString)
                        {
                            value = (string[])k1.GetValue(settingName);
                            return true;
                        }
                    }
                }
            }
            value = null;
            return false;
        }

        public static int GetSetting(string settingName, string path, int defaultValue)
        {
            int value;
            if (TryGetSetting(settingName, path, out value))
            {
                return value;
            }
            return defaultValue;
        }
        public static bool TryGetSetting(string settingName, string path, out int value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, false))
            {
                if (k1 != null)
                {
                    if (k1.GetValueNames().Contains(settingName))
                    {
                        var kind = k1.GetValueKind(settingName);
                        if (kind == RegistryValueKind.DWord)
                        {
                            value = (int)k1.GetValue(settingName);
                            return true;
                        }
                        if (kind == RegistryValueKind.String)
                        {
                            int intValue = 0;
                            if (Int32.TryParse((string)k1.GetValue(settingName), out intValue))
                            {
                                value = intValue;
                                return true;
                            }
                        }
                    }
                }
            }
            value = 0;
            return false;
        }

        public static bool GetSetting(string settingName, string path, bool defaultValue)
        {
            bool value;
            if (TryGetSetting(settingName, path, out value))
            {
                return value;
            }
            return defaultValue;
        }
        public static bool TryGetSetting(string settingName, string path, out bool value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, false))
            {
                if (k1 != null)
                {
                    if (k1.GetValueNames().Contains(settingName))
                    {
                        var kind = k1.GetValueKind(settingName);
                        if (kind == RegistryValueKind.DWord)
                        {
                            value = ((int)k1.GetValue(settingName)) != 0;
                            return true;
                        }
                        if (kind == RegistryValueKind.String)
                        {
                            int intValue = 0;
                            if (Int32.TryParse((string)k1.GetValue(settingName), out intValue))
                            {
                                value = intValue != 0;
                                return true;
                            }
                            bool boolValue;
                            if (bool.TryParse((string)k1.GetValue(settingName), out boolValue))
                            {
                                value = boolValue;
                                return true;
                            }
                        }
                    }
                }
            }
            value = false;
            return false;
        }

        public static string GetSetting(string settingName, string defaultValue)
        {
            return GetSetting(settingName, "", defaultValue);
        }
        public static int GetSetting(string settingName, int defaultValue)
        {
            return GetSetting(settingName, "", defaultValue);
        }
        public static bool GetSetting(string settingName, bool defaultValue)
        {
            return GetSetting(settingName, "", defaultValue);
        }

        public static void SaveSetting(string settingName, string path, string value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, true))
            {
                k1.SetValue(settingName, value, RegistryValueKind.String);
            }
        }
        public static void SaveSetting(string settingName, string path, IEnumerable<string> value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, true))
            {
                k1.SetValue(settingName, value.ToArray(), RegistryValueKind.MultiString);
            }
        }
        public static void SaveSetting(string settingName, string path, int value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, true))
            {
                k1.SetValue(settingName, value, RegistryValueKind.DWord);
            }
        }
        public static void SaveSetting(string settingName, string path, bool value)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, true))
            {
                k1.SetValue(settingName, value, RegistryValueKind.DWord);
            }
        }
        public static void SaveSetting(string settingName, string value)
        {
            SaveSetting(settingName, "", value);
        }
        public static void SaveSetting(string settingName, int value)
        {
            SaveSetting(settingName, "", value);
        }
        public static void SaveSetting(string settingName, bool value)
        {
            SaveSetting(settingName, "", value);
        }

        public static string[] ListKeys(string path)
        {
            string keyName = GetKeyName(path);
            using (var k1 = OpenSubKey(keyName, true))
            {
                if (k1 == null)
                {
                    return new string[0];
                }
                return k1.GetSubKeyNames();
            }
        }

        public static bool PathExists(string path)
        {
            return SubKeyExists(GetKeyName(path));
        }

        public static void GetObject(string path, object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    if (property.PropertyType == typeof(string))
                    {
                        string stringValue;
                        if (TryGetSetting(property.Name, path, out stringValue))
                        {
                            property.SetValue(obj, stringValue, null);
                        }
                    }
                    if (property.PropertyType == typeof(int))
                    {
                        int intValue;
                        if (TryGetSetting(property.Name, path, out intValue))
                        {
                            property.SetValue(obj, intValue, null);
                        }
                    }
                    if (property.PropertyType == typeof(float))
                    {
                        string stringValue;
                        if (TryGetSetting(property.Name, path, out stringValue))
                        {
                            double doubleValue;
                            if (double.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out doubleValue))
                            {
                                float floatValue = (float)doubleValue;
                                property.SetValue(obj, floatValue, null);
                            }
                        }
                    }
                    if (property.PropertyType == typeof(double))
                    {
                        string stringValue;
                        if (TryGetSetting(property.Name, path, out stringValue))
                        {
                            double doubleValue;
                            if (double.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out doubleValue))
                            {
                                property.SetValue(obj, doubleValue, null);
                            }
                        }
                    }
                    if (property.PropertyType == typeof(bool))
                    {
                        bool boolValue;
                        if (TryGetSetting(property.Name, path, out boolValue))
                        {
                            property.SetValue(obj, boolValue, null);
                        }
                    }
                }
                if (property.CanRead)
                {
                    if (typeof(ICollection<string>).IsAssignableFrom(property.PropertyType))
                    {
                        string[] multiStringValue;
                        if (TryGetSetting(property.Name, path, out multiStringValue))
                        {
                            if (typeof(string[]) == property.PropertyType)
                            {
                                property.SetValue(obj, multiStringValue, null);
                            }
                            else
                            {
                                ICollection<string> oldValue = (ICollection<string>)property.GetValue(obj, null);
                                if (oldValue == null && property.CanWrite)
                                {
                                    ICollection<string> newValue;
                                    if (property.PropertyType.IsInterface)
                                    {
                                        newValue = new List<string>();
                                    }
                                    else
                                    {
                                        newValue = (ICollection<string>)Activator.CreateInstance(property.PropertyType);
                                    }
                                    oldValue = newValue;
                                    property.SetValue(obj, newValue, null);
                                }
                                if (!property.CanWrite && (oldValue == null || oldValue.IsReadOnly))
                                {
                                    continue;
                                }

                                oldValue.Clear();
                                foreach (var item in multiStringValue)
                                {
                                    oldValue.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var field in fields)
            {
                if (!field.IsInitOnly)
                {
                    if (field.FieldType == typeof(string))
                    {
                        string stringValue;
                        if (TryGetSetting(field.Name, path, out stringValue))
                        {
                            field.SetValue(obj, stringValue);
                        }
                    }
                    if (field.FieldType == typeof(int))
                    {
                        int intValue;
                        if (TryGetSetting(field.Name, path, out intValue))
                        {
                            field.SetValue(obj, intValue);
                        }
                    }
                    if (field.FieldType == typeof(float))
                    {
                        string stringValue;
                        if (TryGetSetting(field.Name, path, out stringValue))
                        {
                            double doubleValue;
                            if (double.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out doubleValue))
                            {
                                float floatValue = (float)doubleValue;
                                field.SetValue(obj, floatValue);
                            }
                        }
                    }
                    if (field.FieldType == typeof(double))
                    {
                        string stringValue;
                        if (TryGetSetting(field.Name, path, out stringValue))
                        {
                            double doubleValue;
                            if (double.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out doubleValue))
                            {
                                field.SetValue(obj, doubleValue);
                            }
                        }
                    } if (field.FieldType == typeof(bool))
                    {
                        bool boolValue;
                        if (TryGetSetting(field.Name, path, out boolValue))
                        {
                            field.SetValue(obj, boolValue);
                        }
                    }
                }
                if (typeof(ICollection<string>).IsAssignableFrom(field.FieldType))
                {
                    string[] multiStringValue;
                    if (TryGetSetting(field.Name, path, out multiStringValue))
                    {
                        if (typeof(string[]) == field.FieldType)
                        {
                            field.SetValue(obj, multiStringValue);
                        }
                        else
                        {
                            ICollection<string> oldValue = (ICollection<string>)field.GetValue(obj);
                            if (oldValue == null && !field.IsInitOnly)
                            {
                                ICollection<string> newValue;
                                if (field.FieldType.IsInterface)
                                {
                                    newValue = new List<string>();
                                }
                                else
                                {
                                    newValue = (ICollection<string>)Activator.CreateInstance(field.FieldType);
                                }
                                oldValue = newValue;
                                field.SetValue(obj, newValue);
                            }
                            if (field.IsInitOnly && (oldValue == null || oldValue.IsReadOnly))
                            {
                                continue;
                            }
                            oldValue.Clear();
                            foreach (var item in multiStringValue)
                            {
                                oldValue.Add(item);
                            }
                        }
                    }

                }
            }
        }

        public static void SaveObject(string path, object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string))
                {
                    string stringValue = (string)property.GetValue(obj, null);
                    SaveSetting(property.Name, path, stringValue);
                }
                if (property.PropertyType == typeof(int))
                {
                    int intValue = (int)property.GetValue(obj, null);
                    SaveSetting(property.Name, path, intValue);
                }
                if (property.PropertyType == typeof(float))
                {
                    float floatValue = (float)property.GetValue(obj, null);
                    SaveSetting(property.Name, path, ((double)floatValue).ToString(CultureInfo.InvariantCulture));
                }
                if (property.PropertyType == typeof(double))
                {
                    double doubleValue = (double)property.GetValue(obj, null);
                    SaveSetting(property.Name, path, doubleValue.ToString(CultureInfo.InvariantCulture));
                }
                if (property.PropertyType == typeof(bool))
                {
                    bool boolValue = (bool)property.GetValue(obj, null);
                    SaveSetting(property.Name, path, boolValue);
                }
                if (typeof(IEnumerable<string>).IsAssignableFrom(property.PropertyType))
                {
                    IEnumerable<string> stringCollection = (IEnumerable<string>)property.GetValue(obj, null);
                    if (stringCollection != null)
                    {
                        var stringArray = stringCollection.ToArray();
                        SaveSetting(property.Name, path, stringArray);
                    }
                }
            }

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    string stringValue = (string)field.GetValue(obj);
                    SaveSetting(field.Name, path, stringValue);
                }
                if (field.FieldType == typeof(int))
                {
                    int intValue = (int)field.GetValue(obj);
                    SaveSetting(field.Name, path, intValue);
                }
                if (field.FieldType == typeof(float))
                {
                    float floatValue = (float)field.GetValue(obj);
                    SaveSetting(field.Name, path, ((double)floatValue).ToString(CultureInfo.InvariantCulture));
                }
                if (field.FieldType == typeof(double))
                {
                    double doubleValue = (double)field.GetValue(obj);
                    SaveSetting(field.Name, path, doubleValue.ToString(CultureInfo.InvariantCulture));
                }
                if (field.FieldType == typeof(bool))
                {
                    bool boolValue = (bool)field.GetValue(obj);
                    SaveSetting(field.Name, path, boolValue);
                }
                if (typeof(IEnumerable<string>).IsAssignableFrom(field.FieldType))
                {
                    IEnumerable<string> stringCollection = (IEnumerable<string>)field.GetValue(obj);
                    if (stringCollection != null)
                    {
                        var stringArray = stringCollection.ToArray();
                        SaveSetting(field.Name, path, stringArray);
                    }
                }
            }
        }

        public static void DeletePath(string path)
        {
            string fullPath = GetKeyName(path);

            string[] dirs = fullPath.Split('\\');
            RegistryKey key = Registry.CurrentUser;

            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                if (i == dirs.Length - 1)
                {
                    key.DeleteSubKeyTree(dir);
                    key.Close();
                }
                else
                {
                    var nextKey = key.OpenSubKey(dir, true);
                    if (nextKey == null)
                    {
                        return;
                    }

                    key.Close();
                    key = nextKey;
                }
            }
        }
    }
}
