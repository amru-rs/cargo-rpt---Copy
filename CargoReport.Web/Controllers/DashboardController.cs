using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.Mvc;
using CargoReport.Web.Helpers.JqGridHelpers.Models;
using CargoReport.Web.Models;

namespace CargoReport.Web.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: Dashboard
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetGridData(JqGridRequest request, string dateFrom, string dateTo)
        {
            var stDate = DateTime.ParseExact(dateFrom, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            var toDate = DateTime.ParseExact(dateTo, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            
            var pageIndex = request.page - 1;
            var pageSize = request.rows;

            const string sqlResult = @"
                SELECT 
                    DISTINCT 
                        OACT.OfficeBranchCode AS GsaCode, 
                        O.Name AS GsaName, 
                        (SELECT TOP 1 AB.AgentCode FROM citilinklive.dbo.AgentBranch AB WHERE AB.AgentSNo = AG.SNo) AS AgentCode, 
                        AG.AgentName, 
                        PR.Amount, 
                        PR.BankName, 
                        PR.ReceiptDate, 
                        CASE 
                            WHEN PR.RequestBy = 284 THEN 'AutoTopUp' 
                            ELSE (SELECT NAME FROM citilinklive.dbo.[Login] WHERE SNo = PR.RequestBy) 
                        END AS RequestBy, 
                        PR.RequestOn, 
                        CASE 
                            WHEN PR.RequestBy = 284 THEN 'AutoTopUp' 
                            ELSE (SELECT NAME FROM citilinklive.dbo.[Login] WHERE SNo = PR.ApprovedBy) END AS ApprovedBy, 
                        PR.ApprovedOn, 
                        PR.ApprovedRemarks, 
                        PR.PaymentReferenceNo, 
                        PR.PaymentRefId 
                FROM citilinklive.dbo.PaymentReceipt PR 
                    INNER JOIN citilinklive.dbo.OfficeAirlineCityTrans OACT ON PR.OfficeAirlineCitySNo = OACT.SNo 
                    INNER JOIN citilinklive.dbo.OfficeAirlineTrans OAT ON OACT.OfficeAirlineSNo = OAT.SNo 
                    INNER JOIN citilinklive.dbo.Office O ON O.SNo = OAT.OfficeSNo LEFT JOIN citilinklive.dbo.Agent AG ON PR.AgentSNo = AG.SNo 
                        WHERE 1 = 1 AND(PR.ApprovedBy IS NOT NULL AND PR.ApprovedBy <> 0) 
                            AND PR.IsRejected <> 1 AND(PR.RequestOn >= @ReqStartDate AND PR.RequestOn <= @ReqEndDate) 
                        ORDER BY PR.ApprovedOn ASC 
                    OFFSET @PageSize * @PageNumber ROWS 
                        FETCH NEXT @PageSize ROWS ONLY OPTION (RECOMPILE)";

            const string sqlTotalRecordResult = @"
                SELECT COUNT(*)
                    FROM (
                        SELECT 
                            DISTINCT 
                                OACT.OfficeBranchCode AS GsaCode, 
                                O.Name AS GsaName, 
                                (SELECT TOP 1 AB.AgentCode FROM citilinklive.dbo.AgentBranch AB WHERE AB.AgentSNo = AG.SNo) AS AgentCode, 
                                AG.AgentName, 
                                PR.Amount, 
                                PR.BankName, 
                                PR.ReceiptDate, 
                                CASE 
                                    WHEN PR.RequestBy = 284 THEN 'AutoTopUp' 
                                    ELSE (SELECT NAME FROM citilinklive.dbo.[Login] WHERE SNo = PR.RequestBy) 
                                END AS RequestBy, 
                                PR.RequestOn, 
                                CASE 
                                    WHEN PR.RequestBy = 284 THEN 'AutoTopUp' 
                                    ELSE (SELECT NAME FROM citilinklive.dbo.[Login] WHERE SNo = PR.ApprovedBy) END AS ApprovedBy, 
                                PR.ApprovedOn, 
                                PR.ApprovedRemarks, 
                                PR.PaymentReferenceNo, 
                                PR.PaymentRefId 
                        FROM citilinklive.dbo.PaymentReceipt PR 
                            INNER JOIN citilinklive.dbo.OfficeAirlineCityTrans OACT ON PR.OfficeAirlineCitySNo = OACT.SNo 
                            INNER JOIN citilinklive.dbo.OfficeAirlineTrans OAT ON OACT.OfficeAirlineSNo = OAT.SNo 
                            INNER JOIN citilinklive.dbo.Office O ON O.SNo = OAT.OfficeSNo LEFT JOIN citilinklive.dbo.Agent AG ON PR.AgentSNo = AG.SNo 
                                WHERE 1 = 1 AND(PR.ApprovedBy IS NOT NULL AND PR.ApprovedBy <> 0) 
                                    AND PR.IsRejected <> 1 AND(PR.RequestOn >= @ReqStartDate AND PR.RequestOn <= @ReqEndDate)
                    ) AS TotalRecord";

            var queryResult = _db.Database.SqlQuery<Report1ViewModel>(sqlResult,
                    new SqlParameter("@PageNumber", pageIndex),
                    new SqlParameter("@PageSize", pageSize),
                    new SqlParameter("@ReqStartDate", stDate),
                    new SqlParameter("@ReqEndDate", toDate)).ToList();

            var queryTotalRecordResult = _db.Database.SqlQuery<int>(sqlTotalRecordResult,
                    new SqlParameter("@ReqStartDate", stDate),
                    new SqlParameter("@ReqEndDate", toDate)).FirstOrDefault();

            var totalRecords = queryTotalRecordResult;
            var totalPages = (int)Math.Ceiling(totalRecords / (float)pageSize);

            return Json(new JqGridData<Guid>
            {
                Total = totalPages,
                Page = request.page,
                Records = totalRecords,
                Rows = (queryResult.Select(data => new JqGridRowData<Guid>
                {
                    Id = Guid.NewGuid(),
                    RowCells = new List<object>
                    {
                        data.GsaCode,
                        data.GsaName,
                        data.AgentCode,
                        data.AgentName,
                        data.Amount,
                        data.BankName,
                        data.ReceiptDate,
                        data.RequestBy,
                        data.RequestOn,
                        data.ApprovedBy,
                        data.ApprovedOn,
                        data.ApprovedRemarks,
                        data.PaymentReferenceNo,
                        data.PaymentRefId
                    }
                })).ToList()
            }, JsonRequestBehavior.AllowGet);
        }
    }
}