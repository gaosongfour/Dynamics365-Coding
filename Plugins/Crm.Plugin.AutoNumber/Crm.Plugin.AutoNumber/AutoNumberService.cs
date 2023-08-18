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

            //{PREFIX}-{DATETIME}-{SEQNUM}
            var numberFormat = autoNumberEntity.GetAttributeValue<string>("new_numberformat");

            #region Prefix
            if (numberFormat.Contains(placeholderPrefix))
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
                numberFormat = numberFormat.Replace(placeholderPrefix, prefix);
            }
            #endregion

            #region  DateTime
            if (numberFormat.Contains(placeholderDateTime))
            {
                if (!autoNumberEntity.Contains("new_datetimeformat"))
                    throw new InvalidPluginExecutionException("DateTimeFormat  value is null");

                var dateTimeFormat = autoNumberEntity.GetAttributeValue<string>("new_datetimeformat");
                numberFormat = numberFormat.Replace(placeholderDateTime, DateTime.Now.AddHours(8).ToString(dateTimeFormat));
            }
            #endregion

            #region  seq number           
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
                    default: throw new InvalidPluginExecutionException($"Unsupported restart seq number value {restart}");
                }
            }
            var counterKey = $"{targetEntity.LogicalName.ToLower()}{restartKey}";
            var counterEntity = FindAutoNumberCounter(service, counterKey);
            var length = autoNumberEntity.GetAttributeValue<int>("new_seqnumberlength");
            var newSeqNumber = 1;
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
                newSeqNumber = counterEntity.GetAttributeValue<int>("new_seqnumber") + 1;
                UpdateAutoNumberCounterSeqNumber(service, counterEntity.Id, newSeqNumber);
            }

            var seqNumberText = newSeqNumber.ToString().PadLeft(length, '0');
            numberFormat = numberFormat.Replace(placeholderSeqNum, seqNumberText);
            #endregion

            var autoNumberAttributeName = autoNumberEntity.GetAttributeValue<string>("new_autonumberattributename");
            targetEntity[autoNumberAttributeName] = numberFormat;
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
            var requiredAttributes = new string[] { "new_autonumberattributename", "new_prefixtype", "new_prefixvalue", "new_numberformat", "new_seqnumberlength", "new_datetimeformat" };
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

        private Guid CreateAutoNumberCounter(IOrganizationService service, string counterKey, Entity autoNumberEntity)
        {
            var entity = new Entity("new_autonumbercounter");
            entity["new_name"] = counterKey;
            entity["new_autonumberconfigid"] = autoNumberEntity.ToEntityReference();
            entity["new_seqnumber"] = 1;
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