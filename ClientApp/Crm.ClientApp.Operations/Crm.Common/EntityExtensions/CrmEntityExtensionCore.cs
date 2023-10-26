using System;
using Microsoft.Xrm.Sdk;

namespace Crm.Common
{
    /// <summary>
    ///Basic Crm Entity related extension methods
    /// </summary>
    public static class CrmEntityExtensionCore
    {
        #region Crm Entity Attribute Extension Methods
        /// <summary>
        /// Get Crm Entity formatted attribute value, if entity does not contains attribute, return null
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static string GetAttributeFormattedValue(this Entity entity, string attributeName)
        {
            if (entity == null) throw new ArgumentNullException("entity can not be null");
            if (string.IsNullOrEmpty(attributeName)) throw new ArgumentNullException("attributeName can not be null or empty");

            if (entity.FormattedValues.ContainsKey(attributeName))
                return entity.FormattedValues[attributeName];
            else
                return null;
        }

        /// <summary>
        /// Get Crm Entity Aliased Attribute Value to T (string, money, lookup etc), if entity doesn't contain attributeName, return default value of T
        /// </summary>
        /// <typeparam name="T">the actual type of attribute that is wrapped by AliasedValue</typeparam>
        /// <param name="entity"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static T GetAliasedAttributeValue<T>(this Entity entity, string attributeName)
        {
            if (entity == null) throw new ArgumentNullException("entity can not be null");
            if (string.IsNullOrEmpty(attributeName)) throw new ArgumentNullException("attributeName can not be null or empty");

            if (!entity.Contains(attributeName)) return default(T);
            if (entity[attributeName].GetType() != typeof(AliasedValue)) throw new ArgumentException($"attribute {attributeName} is not aliased type");

            return (T)(entity.GetAttributeValue<AliasedValue>(attributeName).Value);
        }
        #endregion
    }
}