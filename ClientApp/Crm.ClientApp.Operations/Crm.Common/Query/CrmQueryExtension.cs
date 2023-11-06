using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

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
    }
}