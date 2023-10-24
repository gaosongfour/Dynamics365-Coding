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
        #endregion
    }
}