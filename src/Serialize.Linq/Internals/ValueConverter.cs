﻿#region Copyright
//  Copyright, Sascha Kiefer (esskar)
//  Released under LGPL License.
//  
//  License: https://raw.github.com/esskar/Serialize.Linq/master/LICENSE
//  Contributing: https://github.com/esskar/Serialize.Linq
#endregion

using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Serialize.Linq.Internals
{
    public static class ValueConverter
    {
        private static readonly ConcurrentDictionary<Type, Func<object, Type, object>> _userDefinedConverters;
        private static readonly Regex _dateRegex = new Regex(@"/Date\((?<date>-?\d+)((?<offsign>[-+])((?<offhours>\d{2})(?<offminutes>\d{2})))?\)/", RegexOptions.Compiled);
        
        static ValueConverter()
        {
            _userDefinedConverters = new ConcurrentDictionary<Type, Func<object, Type, object>>();
        }

        public static void AddCustomConverter(Type convertTo, Func<object, object> converter)
        {
            AddCustomConverter(convertTo, (v, t) => converter(v));
        }

        public static void AddCustomConverter(Func<object, Type, object> converter)
        {
            AddCustomConverter(typeof(void), converter);
        }

        public static void AddCustomConverter(Type convertTo, Func<object, Type, object> converter)
        {
            if (convertTo == null)
                throw new ArgumentNullException("convertTo");

            if (converter == null)
                throw new ArgumentNullException("converter");

            if (!_userDefinedConverters.TryAdd(convertTo, converter))
                throw new Exception("Failed to add converter.");
        }

        public static void ClearCustomConverters()
        {
            _userDefinedConverters.Clear();
        }

        public static object Convert(object value, Type convertTo)
        {
            if (value == null)
                return convertTo.GetTypeInfo().IsValueType ? Activator.CreateInstance(convertTo) : null;

            if (convertTo.IsInstanceOfType(value))
                return value;

            object retval;
            if (TryCustomConvert(value, convertTo, out retval))
                return retval;

            if (convertTo.GetTypeInfo().IsEnum)
                return Enum.ToObject(convertTo, value);            

            // convert array types
            if (convertTo.IsArray && value.GetType().IsArray)
            {
                var elementType = convertTo.GetElementType();

                var valArray = (Array)value;
                var result = Array.CreateInstance(elementType, valArray.Length);
                for (var i = 0; i < valArray.Length; ++i)
                    result.SetValue(Convert(valArray.GetValue(i), elementType), i);
                return result;
            }

            // convert nullable types
            if (convertTo.GetTypeInfo().IsGenericType && convertTo.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var argumentTypes = convertTo.GetGenericArguments();
                if (argumentTypes.Length == 1)                
                    value = Convert(value, argumentTypes[0]);                
            }
            
            // TODO: think about a better way; exception could may have an critical impact on performance
            try
            {
                return System.Convert.ChangeType(value, convertTo);
            }
            catch (Exception)
            {
                return Activator.CreateInstance(convertTo, value);
            }            
        }

        private static bool TryCustomConvert(object value, Type convertTo, out object convertedValue)
        {
            Func<object, Type, object> converter;
            if (_userDefinedConverters.TryGetValue(convertTo, out converter) || _userDefinedConverters.TryGetValue(typeof(void), out converter))
            {
                convertedValue = converter(value, convertTo);
                return true;
            }

            if (convertTo == typeof(DateTime))
            {
                DateTime dateTime;
                if (TryConvertToDateTime(value, out dateTime))
                {
                    convertedValue = dateTime;
                    return true;
                }
            }

            convertedValue = null;
            return false;
        }

        private static bool TryConvertToDateTime(object value, out DateTime dateTime)
        {
            var stringValue = value.ToString();
            if (DateTime.TryParse(stringValue, out dateTime))
                return true;

            var match = _dateRegex.Match(stringValue);
            if (!match.Success)
                return false;

            // try to parse the string into a long. then create a datetime.
            long msFromEpoch;
            if (!long.TryParse(match.Groups["date"].Value, out msFromEpoch))
                return false;

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var fromEpoch = TimeSpan.FromMilliseconds(msFromEpoch);
            dateTime = epoch.Add(fromEpoch);

            var offsign = match.Groups["offsign"].Value;
            if (offsign.Length > 0)
            {
                var sign = offsign == "-" ? -1 : 1;

                var offhours = match.Groups["offhours"].Value;
                if (offhours.Length > 0)
                {
                    int hours;
                    if (!int.TryParse(offhours, out hours))
                        return false;
                    dateTime = dateTime.AddHours(hours*sign);
                }

                var offminutes = match.Groups["offminutes"].Value;
                if (match.Groups["offminutes"].Length > 0)
                {
                    int minutes;
                    if (!int.TryParse(offminutes, out minutes))
                        return false;
                    dateTime = dateTime.AddMinutes(minutes*sign);
                }
            }

            return true;
        }
    }
}
