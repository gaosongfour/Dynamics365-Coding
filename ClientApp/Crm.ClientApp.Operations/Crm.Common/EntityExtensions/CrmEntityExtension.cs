using System;
using System.Text;
using Microsoft.Xrm.Sdk;

namespace Crm.Common
{
    public static class CrmEntityExtension
    {
        #region Print entity attributes information
        /// <summary>
        /// Print entity attributes information to the console
        /// </summary>
        /// <param name="entity"></param>
        public static string PrintAttributes(this Entity entity)
        {
            if (entity == null) throw new ArgumentNullException("entity is null");
            var text = new StringBuilder();
            text.AppendLine($"Print attributes for entity {entity.LogicalName} with id {entity.Id}");
            foreach (var attribute in entity.Attributes)
            {
                var attributeInfo = entity.PrintAttribute(attribute.Key, attribute.Value);
                text.AppendLine(attributeInfo);
            }

            Console.WriteLine(text.ToString());
            return text.ToString();
        }

        private static string PrintAttribute(this Entity entity, string attributeName, object attributeValue)
        {
            var attributeType = attributeValue.GetType();

            if (attributeType == typeof(AliasedValue))
                return PrintAttribute(entity, attributeName, ((AliasedValue)attributeValue).Value);

            if (attributeType == typeof(string)
                || attributeType == typeof(Guid)
                || attributeType == typeof(int)
                || attributeType == typeof(decimal)
                )
                return PrintAttributeInfo(attributeName, attributeType, attributeValue.ToString());

            if (attributeType == typeof(EntityReference))
            {
                var entityRef = entity.GetAttributeValue<EntityReference>(attributeName);
                return PrintAttributeInfo(attributeName, attributeType, $"(id:{entityRef.Id};type:{entityRef.LogicalName};name:{entityRef.Name})");
            }

            if (attributeType == typeof(OptionSetValue))
            {
                return PrintAttributeInfo(attributeName, attributeType, $"{entity.GetAttributeValue<OptionSetValue>(attributeName).Value}", entity.GetAttributeFormattedValue(attributeName));
            }

            if (attributeType == typeof(DateTime))
            {
                return PrintAttributeInfo(attributeName, attributeType, $"{entity.GetAttributeValue<DateTime>(attributeName).ToString("yyyy-MM-dd:HH:mm:ss")}", entity.GetAttributeFormattedValue(attributeName));
            }

            if (attributeType == typeof(bool))
            {
                return PrintAttributeInfo(attributeName, attributeType, $"{entity.GetAttributeValue<bool>(attributeName)}", entity.GetAttributeFormattedValue(attributeName));
            }

            if (attributeType == typeof(Money))
            {
                return PrintAttributeInfo(attributeName, attributeType, $"{entity.GetAttributeValue<Money>(attributeName)}", entity.GetAttributeFormattedValue(attributeName));
            }

            return PrintAttributeInfo(attributeName, attributeType, "unknow attribute type");
        }

        private static string PrintAttributeInfo(string attributeName, Type attributeType, string attributeValue, string attirbuteFormattedValue = null)
        {
            var attributeInfo = $"attribute=>{attributeName}|type=>{attributeType}|value=>{attributeValue}";
            if (!string.IsNullOrEmpty(attirbuteFormattedValue))
            {
                attributeInfo = $"{attributeInfo}|FormattedValue=>{attirbuteFormattedValue}";
            }
            return attributeInfo;
        }
        #endregion

        #region Print entity collection
        /// <summary>
        /// Print entity collection
        /// </summary>
        /// <param name="entityCollection"></param>
        /// <param name="displayAttributeName">string type AttributeName name to display, default value "name"</param>
        public static void PrintEntityCollection(this EntityCollection entityCollection, string displayAttributeName = "name")
        {
            if (entityCollection == null) throw new ArgumentNullException("entityCollection can not be null");

            Console.WriteLine($"Total {entityCollection.EntityName} => {entityCollection.Entities.Count} ");
            foreach (var entity in entityCollection.Entities)
                Console.WriteLine($"{entity.LogicalName}({entity.Id})=>{entity.GetAttributeValue<string>(displayAttributeName)}");
        }
        #endregion
    }
}