using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Crm.Common.Query
{
    public static class CrmQueryExtension
    {
        #region RetrieveAllRecords
        /// <summary>
        /// Retrieve all records without the limit 5000
        /// </summary>
        /// <param name="service">IOrganizationService</param>
        /// <param name="basicQuery">QueryExpression prepared</param>
        /// <returns></returns>
        public static List<Entity> RetrieveAllRecords(this IOrganizationService service, QueryExpression basicQuery)
        {
            var entityList = new List<Entity>();
            int currentPageNumber = 1;
            int pageSize = 5000;
            var query = GetQuery(basicQuery, currentPageNumber, pageSize, null);

            while (true)
            {
                var result = service.RetrieveMultiple(query);
                entityList.AddRange(result.Entities);
                Console.WriteLine($"Current Page Number {currentPageNumber}| Page Size {pageSize} | Current Page Records Count {result.Entities.Count}");

                if (result.MoreRecords)
                {
                    currentPageNumber++;
                    query = GetQuery(basicQuery, currentPageNumber, pageSize, result.PagingCookie);
                }
                else
                    break;
            }

            return entityList;
        }

        private static QueryExpression GetQuery(QueryExpression query, int pageNumber, int pageSize, string pageCookie)
        {
            query.PageInfo = new PagingInfo { Count = pageSize, PageNumber = pageNumber, PagingCookie = pageCookie };
            return query;
        }
        #endregion

        #region RetrieveAllRecords using FetchXml
        /// <summary>
        /// Retrieve all the records using FetchXml
        /// </summary>
        /// <param name="crmService"></param>
        /// <param name="fetchXml">fetchxml query</param>
        /// <param name="pageSize">optional, default 5000</param>
        /// <returns></returns>
        public static List<Entity> RetrieveAllRecordsByFetchXml(this IOrganizationService crmService, string fetchXml, int pageSize = 5000)
        {
            var list = new List<Entity>();

            var page = 1;

            var fetchNode = XElement.Parse(fetchXml);
            fetchNode.SetAttributeValue("page", page);
            fetchNode.SetAttributeValue("count", pageSize);

            while (true)
            {
                var result = crmService.RetrieveMultiple(new FetchExpression(fetchNode.ToString()));
                Console.WriteLine($"Current Page Number {page}| Page Size {pageSize} | Current Page Records Count {result.Entities.Count}");
                list.AddRange(result.Entities);
                if (!result.MoreRecords)
                {
                    break;
                }

                fetchNode.SetAttributeValue("page", page++);
                fetchNode.SetAttributeValue("paging-cookie", result.PagingCookie);
            }

            return list;
        }
        #endregion
    }
}