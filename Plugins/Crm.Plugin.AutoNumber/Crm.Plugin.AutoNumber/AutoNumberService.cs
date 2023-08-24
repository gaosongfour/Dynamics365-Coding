using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Crm.Plugin.AutoNumber
{
    public class AutoNumberService
    {
        private readonly string placeholderPrefix = "{PREFIX}";
        private readonly string placeholderDateTime = "{DATETIME}";
        private readonly string placeholderSeqNum = "{SEQNUM}";

        /// <summary>
        /// Genarate new number for entity according to the configuration
        /// </summary>
        /// <param name="service"></param>
        /// <param name="targetEntity"></param>
        public void SetNumber(IOrganizationService service, Entity targetEntity)
        {
            var autoNumberEntity = FindAutoNumberConfig(service, targetEntity.LogicalName);
            ValidateAutoNubmerConfig(autoNumberEntity);

            var autoNumberAttributeName = autoNumberEntity.GetAttributeValue<string>("new_autonumberattributename");
            //if the auto number has been already asigned the value, stop (skip) auto number
            //for example, if we copy the order, the order item seq number will be copied with the same value
            if (targetEntity.Contains(autoNumberAttributeName)) return;

            var newSeqNumber = GetNextSeqNumber(service, targetEntity, autoNumberEntity);

            var isAutoNumberAttributeInt = autoNumberEntity.GetAttributeValue<bool>("new_isautonumberattributeintegertype");
            if (isAutoNumberAttributeInt)
            {
                targetEntity[autoNumberAttributeName] = newSeqNumber;
            }
            // If text format, we refer to the format definition
            else
            {
                //ex: {PREFIX}-{DATETIME}-{SEQNUM}
                var textNumber = autoNumberEntity.GetAttributeValue<string>("new_numberformat");

                #region Prefix
                if (textNumber.Contains(placeholderPrefix))
                {
                    var prefixType = autoNumberEntity.GetAttributeValue<OptionSetValue>("new_prefixtype").Value;
                    var prefix = string.Empty;
                    if (prefixType == 1)
                    {
                        prefix = autoNumberEntity.GetAttributeValue<string>("new_prefixvalue");
                    }
                    if (prefixType == 11)
                    {
                        var lookupPrefixConfig = autoNumberEntity.GetAttributeValue<string>("new_prefixvalue");
                        var configs = lookupPrefixConfig.Split(new char[] { ',' });
                        if (configs.Length < 2)
                            throw new InvalidPluginExecutionException("Invalid format for lookup Prefix");

                        var lookupAttributeName = configs[0];
                        var queryAttribtueName = configs[1];

                        if (!targetEntity.Contains(lookupAttributeName))
                            throw new InvalidPluginExecutionException($"attribute {lookupAttributeName} value is required");

                        var lookupEntityRef = targetEntity.GetAttributeValue<EntityReference>(lookupAttributeName);
                        var lookupEntity = service.Retrieve(lookupEntityRef.LogicalName, lookupEntityRef.Id, new ColumnSet(queryAttribtueName));
                        prefix = lookupEntity.GetAttributeValue<string>(queryAttribtueName);
                    }
                    textNumber = textNumber.Replace(placeholderPrefix, prefix);
                }
                #endregion

                #region  DateTime
                if (textNumber.Contains(placeholderDateTime))
                {
                    if (!autoNumberEntity.Contains("new_datetimeformat"))
                        throw new InvalidPluginExecutionException("DateTimeFormat  value is null");

                    var dateTimeFormat = autoNumberEntity.GetAttributeValue<string>("new_datetimeformat");
                    textNumber = textNumber.Replace(placeholderDateTime, DateTime.Now.AddHours(8).ToString(dateTimeFormat));
                }
                #endregion

                #region  seq number
                var length = autoNumberEntity.GetAttributeValue<int>("new_seqnumberlength");
                var seqNumberText = newSeqNumber.ToString().PadLeft(length, '0');
                textNumber = textNumber.Replace(placeholderSeqNum, seqNumberText);
                #endregion

                targetEntity[autoNumberAttributeName] = textNumber;
            }
        }

        /// <summary>
        /// Find the latest config records by entityname
        /// </summary>
        /// <param name="service"></param>
        /// <param name="entityName"></param>
        /// <returns></returns>
        private Entity FindAutoNumberConfig(IOrganizationService service, string entityName)
        {
            var query = new QueryExpression("new_autonumberconfig");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria = new FilterExpression();
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("new_entityname", ConditionOperator.Equal, entityName);
            query.AddOrder("createdon", OrderType.Descending);

            var result = service.RetrieveMultiple(query);
            if (result.Entities.Count == 0)
                throw new InvalidPluginExecutionException($"AutoNumber Config not found for entity {entityName}");
            return result.Entities.First();
        }

        private void ValidateAutoNubmerConfig(Entity autoNumberEntity)
        {
            var isAutoNumberAttributeInt = autoNumberEntity.GetAttributeValue<bool>("new_isautonumberattributeintegertype");
            var requiredAttributes = isAutoNumberAttributeInt ?
                new string[] { "new_autonumberattributename", "new_startseqnumber", "new_incrementby" } :
                new string[] { "new_autonumberattributename", "new_startseqnumber", "new_incrementby", "new_prefixtype", "new_prefixvalue", "new_numberformat", "new_seqnumberlength", "new_datetimeformat" };
            foreach (var attribute in requiredAttributes)
            {
                if (!autoNumberEntity.Contains(attribute))
                    throw new InvalidPluginExecutionException($"Value is null for required attribute {attribute}, please check auto number config");
            }
        }

        private Entity FindAutoNumberCounter(IOrganizationService service, string counterKey)
        {
            var query = new QueryExpression("new_autonumbercounter");
            query.ColumnSet = new ColumnSet("new_seqnumber");
            query.Criteria = new FilterExpression();
            query.Criteria.AddCondition("new_name", ConditionOperator.Equal, counterKey);

            var result = service.RetrieveMultiple(query);
            return result.Entities.FirstOrDefault();
        }

        /// <summary>
        /// Get the Next seq number according to the auto number config and counter
        /// </summary>
        /// <param name="service"></param>
        /// <param name="targetEntity"></param>
        /// <param name="autoNumberEntity"></param>
        /// <returns></returns>
        private int GetNextSeqNumber(IOrganizationService service, Entity targetEntity, Entity autoNumberEntity)
        {
            var restartKey = string.Empty;
            if (autoNumberEntity.Contains("new_restartseqnumber"))
            {
                var restart = autoNumberEntity.GetAttributeValue<OptionSetValue>("new_restartseqnumber").Value;
                var today = DateTime.Now.AddHours(8);
                switch (restart)
                {
                    case 10:
                        restartKey = today.ToString("yyyy");
                        break;
                    case 20:
                        restartKey = today.ToString("yyyyMM");
                        break;
                    case 30:
                        restartKey = today.ToString("yyyyMMdd");
                        break;
                    //Parent lookup, used for child entity numbering
                    case 100:
                        var parentLookupAttributeName = autoNumberEntity.GetAttributeValue<string>("new_parentlookupattributename");
                        if (string.IsNullOrEmpty(parentLookupAttributeName))
                            throw new InvalidPluginExecutionException("Parent lookup attribtue name not definied");
                        if (!targetEntity.Contains(parentLookupAttributeName))
                            throw new InvalidPluginExecutionException($"Parent lookup value {parentLookupAttributeName} for record {targetEntity.LogicalName}{targetEntity.Id} is null");
                        restartKey = targetEntity.GetAttributeValue<EntityReference>(parentLookupAttributeName).Id.ToString();
                        break;
                }
            }
            var counterKey = $"{targetEntity.LogicalName.ToLower()}({restartKey})";
            var counterEntity = FindAutoNumberCounter(service, counterKey);

            var newSeqNumber = autoNumberEntity.GetAttributeValue<int>("new_startseqnumber");
            if (counterEntity == null)
            {
                CreateAutoNumberCounter(service, counterKey, autoNumberEntity);
            }
            else
            {
                //update the counter record so that it will be locked
                LockAutoNumberCounter(service, counterEntity.Id);
                //retrieve again the current seq number
                counterEntity = service.Retrieve("new_autonumbercounter", counterEntity.Id, new ColumnSet("new_seqnumber"));
                newSeqNumber = counterEntity.GetAttributeValue<int>("new_seqnumber") + autoNumberEntity.GetAttributeValue<int>("new_incrementby");
                UpdateAutoNumberCounterSeqNumber(service, counterEntity.Id, newSeqNumber);
            }
            return newSeqNumber;
        }

        private Guid CreateAutoNumberCounter(IOrganizationService service, string counterKey, Entity autoNumberEntity)
        {
            var entity = new Entity("new_autonumbercounter");
            entity["new_name"] = counterKey;
            entity["new_autonumberconfigid"] = autoNumberEntity.ToEntityReference();
            entity["new_seqnumber"] = autoNumberEntity["new_startseqnumber"];
            return service.Create(entity);
        }

        private void LockAutoNumberCounter(IOrganizationService service, Guid autoNumberCounterId)
        {
            var entity = new Entity("new_autonumbercounter", autoNumberCounterId);
            entity["new_lock"] = Guid.NewGuid().ToString();
            service.Update(entity);
        }

        private void UpdateAutoNumberCounterSeqNumber(IOrganizationService service, Guid autoNumberCounterId, int currentSeqNum)
        {
            var entity = new Entity("new_autonumbercounter", autoNumberCounterId);
            entity["new_seqnumber"] = currentSeqNum;
            service.Update(entity);
        }
    }
}