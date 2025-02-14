using Google.Cloud.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Finsight.Utilities
{
    public static class FirestoreMapper
    {

        public static Dictionary<string, object> ToDictionary<T>(T obj)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore // Ignore null values
            };
            var jsonString = JsonConvert.SerializeObject(obj, settings);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString) ?? throw new Exception("Firestore deserialization failed");
            foreach (var key in dictionary.Keys.ToList())
            {
                if (dictionary[key] is DateTime dateTime)
                {
                    dictionary[key] = Timestamp.FromDateTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
                }
            }
            return dictionary;
        }


        public static T MapTo<T>(DocumentSnapshot document) where T : new()
        {
            var model = new T();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var firestoreFieldName = Char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1); // Convert PascalCase to camelCase
                if (document.ContainsField(firestoreFieldName) && property.CanWrite)
                {
                    var value = document.GetValue<object>(firestoreFieldName);
                    if (value is Timestamp firestoreTimestamp)
                    {
                        value = firestoreTimestamp.ToDateTime();
                    }
                    Type propertyType = property.PropertyType;
                    Type? underlyingType = Nullable.GetUnderlyingType(propertyType);
                    if ((propertyType.IsEnum || (underlyingType != null && underlyingType.IsEnum)) && value is string stringValue)
                    {
                        Type enumType = underlyingType ?? propertyType; // Handle both nullable and non-nullable enums
                        if (Enum.TryParse(enumType, stringValue, true, out var enumValue))
                        {
                            property.SetValue(model, enumValue);
                        }
                        else
                        {
                            throw new InvalidCastException($"Invalid value '{stringValue}' for enum {property.Name}");
                        }
                    }
                    else
                    {
                        property.SetValue(model, Convert.ChangeType(value, property.PropertyType));
                    }
                }
            }
            return model;
        }
    }
}