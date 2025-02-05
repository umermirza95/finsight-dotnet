using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Finsight.Utilities
{
    public static class FirestoreMapper
    {
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
                    if (property.PropertyType.IsEnum && value is string stringValue)
                    {
                        if (Enum.TryParse(property.PropertyType, stringValue, true, out var enumValue))
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