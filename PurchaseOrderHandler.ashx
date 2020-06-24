<%@ WebHandler Language="C#" Class="PurchaseOrderHandler" %>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MyLib;
using System.Data;
using Ultimus.UWF.Workflow.Interface;
using Ultimus.UWF.Workflow.Entity;

public class PurchaseOrderHandler : IHttpHandler
{
    HttpRequest request;
    HttpResponse response;
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "text/plain";
        this.request = context.Request;
        this.response = context.Response;
        string JosnString = string.Empty;
        string method = this.request["method"] ?? "";
        string sql = string.Empty;
        DataTable dt = new DataTable();

        switch (method)
        {
            case "getECatalogList":
                string ECatalogid = this.request["ID"] ?? "";
                if (ECatalogid != "")
                {
                    string itemnonew = ECatalogid.Replace("$", "','");
                    sql = "select * from MD_PURCHASEITEM where ID in('" + itemnonew.Substring(0, itemnonew.Length - 2) + ") ";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
                }
                break;
            case "getContracturl":
                IWorkflow _workflow = ServiceContainer.Instance().GetService<IWorkflow>();
                string _userAccount = string.Empty;
                _userAccount = this.request["Loginuser"] ?? "";
                List<TaskEntity> _initProcessList = new List<TaskEntity>();
                _initProcessList = _workflow.GetInitTaskList(_userAccount, "", null, "", 0, 1000);
                string taskid = string.Empty;
                string servername = string.Empty;
                for (int i = 0; i < _initProcessList.Count; i++)
                {
                    if (_initProcessList[i].PROCESSNAME.Equals("Contract Management"))
                    {
                        taskid = _initProcessList[i].TASKID;
                        servername = _initProcessList[i].SERVERNAME;
                        break;
                    }
                }
                dt.Columns.Add("taskid");
                dt.Columns.Add("servername");
                DataRow dr = dt.NewRow();
                dr["taskid"] = taskid;
                dr["servername"] = servername;
                dt.Rows.Add(dr);
                break;
            case "getContractIncident":
                string formid = this.request["Formid"] ?? "";
                sql = "select incident from PROC_CONTRACTGENERALINFORMATION where FORMID=@FORMID ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, formid);
                break;
            case "GetMerchandiseCategory":
                string col = this.request["col"] ?? "";
                string val = this.request["val"] ?? "";
                if (string.IsNullOrEmpty(col))
                    sql = "select * from MD_MerchandiseCategory ";
                else
                    sql = "select * from MD_MerchandiseCategory where " + col + "='" + val + "'";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
                break;
            case "getBudgetBalance":
                string projectcode = this.request["ProjectCode"] ?? "";
                string elementno = this.request["ElementNo"] ?? "";
                string gBByear = this.request["Year"] ?? "";
                string gBBtype = this.request["Type"] ?? "";
                double planningamount = 0;     //当前项目总预算
                double usedbudget = 0;         //po占用金额
                double usedbudget2 = 0;        //合成po占用金额
                if (gBBtype.Equals("CER"))
                {
                    sql = @"select isnull(Planningamount,0) from MD_PROJECTAPPLICATION left join MD_PROJECTAPPLICATIONITEM on MD_PROJECTAPPLICATION.GUID=MD_PROJECTAPPLICATIONITEM.PARENT_GUID  
where ACCOUNTASSTELEM='X' and PROJECTCODE=@projectcode and  WBSELEMENT=@elementno ";
                    //当前项目总预算
                    planningamount = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, projectcode, elementno));

                    sql = @"select isnull(sum(CONVERT(decimal(18,6),isnull(a.EXCHANGRATE,0))*CONVERT(decimal(18,2),isnull(b.UNITPRICE,0))* CONVERT(decimal(18,2),isnull(b.QTY,0))),0) 
from MD_PURCHASEORDER a left join MD_PURCHASEORDERITEM b on a.GUID=b.PARENT_GUID 
where a.ISACTIVE<>0 and b.ISACTIVE<>0 and a.PROJECTCODE=@projectcode and b.WBSELEMENT=@elementno ";
                    //po占用金额
                    usedbudget = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, projectcode, elementno));

                    sql = @"select isnull(sum(CONVERT(decimal(18,6),isnull(a.EXCHANGRATE,0))*CONVERT(decimal(18,2),isnull(b.UNITPRICE,0))* CONVERT(decimal(18,2),isnull(b.QTY,0))),0) 
from MD_PURCHASEORDER_CONBITION a left join MD_DETAILPO b on a.GUID=b.PARENTGUID 
where a.ISACTIVE<>0 and b.ISACTIVE<>0 and a.PROJECTCODE=@projectcode and b.WBSELEMENT=@elementno ";
                    //合成po占用金额
                    usedbudget2 = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, projectcode, elementno));
                }
                else
                {
                    sql = @"select isnull(Planningamount,0) from MD_PROJECTAPPLICATION left join MD_PROJECTAPPLICATIONITEM on MD_PROJECTAPPLICATION.GUID=MD_PROJECTAPPLICATIONITEM.PARENT_GUID  
where ACCOUNTASSTELEM='X' and PROJECTCODE=@projectcode and  WBSELEMENT=@elementno and YEAR=@YEAR ";
                    //当前项目总预算
                    planningamount = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, projectcode, elementno, gBByear));

                    sql = @"select isnull(sum(CONVERT(decimal(18,6),isnull(a.EXCHANGRATE,0))*CONVERT(decimal(18,2),isnull(b.UNITPRICE,0))* CONVERT(decimal(18,2),isnull(b.QTY,0))),0) 
from MD_PURCHASEORDER a left join MD_PURCHASEORDERITEM b on a.GUID=b.PARENT_GUID 
where a.ISACTIVE<>0 and b.ISACTIVE<>0 and a.PROJECTCODE=@projectcode and b.WBSELEMENT=@elementno and DATETO like'" + gBByear + "%' ";
                    //po占用金额
                    usedbudget = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, projectcode, elementno));

                    //marketing 无集中采购
                    //                    sql = @"select isnull(sum(CONVERT(decimal(18,6),isnull(a.EXCHANGRATE,0))*CONVERT(decimal(18,2),isnull(b.UNITPRICE,0))* CONVERT(decimal(18,2),isnull(b.QTY,0))),0) 
                    //from MD_PURCHASEORDER_CONBITION a left join MD_DETAILPO b on a.GUID=b.PARENTGUID 
                    //where a.ISACTIVE<>0 and b.ISACTIVE<>0 and a.PROJECTCODE=@projectcode and b.WBSELEMENT=@elementno ";
                    //                    //合成po占用金额
                    //                    usedbudget2 = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, projectcode, elementno, year));
                }

                //预算余额 （总额-占用金额）
                double budgetbalance = planningamount - usedbudget - usedbudget2;
                JosnString = MyLib.SerializeUtil.JsonSerialize(budgetbalance);
                this.response.Write(JosnString);
                break;
            case "getBudgetControl":
                string bprojectcode = this.request["ProjectCode"] ?? "";
                string bnetamount = this.request["NetAmount"] ?? "";
                string formid1 = this.request["formid"] ?? "";
                string returnvalue = string.Empty;

                //                for (int i = 0; i < belementnoary.Length - 1; i++)
                //                {
                //                    sql = @"select isnull(PLANNINGAMOUNT,0) from MD_PROJECTAPPLICATION left join MD_PROJECTAPPLICATIONITEM on PROC_FORMID=PARENT_FORMID  
                //where ACCOUNTASSTELEM='X' and PROJECTCODE=@projectcode and  WBSELEMENT=@elementno ";
                //                    //当前项目总预算
                //                    double cplanningamount = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, bprojectcode, belementnoary[i]));
                //                    sql = @"select isnull(sum(CONVERT(decimal(18,2),isnull(GROSSAMOUNT,0))),0) from MD_PURCHASEORDER a left join MD_PURCHASEORDERITEM b on a.PROC_FORMID=b.PROC_FORMID 
                //where a.ISACTIVE<>0 and b.ISACTIVE=1 and a.PROJECTCODE=@projectcode and b.WBSELEMENT=@elementno ";
                //                    //po占用金额
                //                    double cusedbudget = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, bprojectcode, belementnoary[i]));
                //                    sql = @"select isnull(sum(CONVERT(decimal(18,2),isnull(GROSSAMOUNT,0))),0) from MD_PURCHASEORDER_CONBITION a left join MD_DETAILPO b on a.GUID=b.PARENTGUID 
                //where a.ISACTIVE<>0 and b.ISACTIVE=1 and a.PROJECTCODE=@projectcode and b.WBSELEMENT=@elementno ";
                //                    //合成po占用金额
                //                    double cusedbudget2 = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, bprojectcode, belementnoary[i]));
                //                    //预算余额 （总额-占用金额）
                //                    double cbudgetbalance = cplanningamount - cusedbudget - cusedbudget2 - ConvertUtil.ToDouble(bgrossamountary[i]);
                //                    if (cbudgetbalance < 0)
                //                    {
                //                        returnvalue = belementnoary[i];
                //                        break;
                //                    }
                //                }
                sql = @"select isnull(APPROVALBUDGET,0) from MD_PROJECTAPPLICATION  where  PROJECTCODE=@PROJECTCODE AND Status=1";
                //当前项目总预算
                double cplanningamount = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, bprojectcode));
                sql = @"select isnull(sum(CONVERT(decimal(18,6),isnull(a.EXCHANGRATE,0))*CONVERT(decimal(18,2),isnull(b.UNITPRICE,0))* CONVERT(decimal(18,2),isnull(b.QTY,0))),0) 
from MD_PURCHASEORDER a left join MD_PURCHASEORDERITEM b on a.GUID=b.PARENT_GUID 
where a.ISACTIVE<>0 and b.ISACTIVE<>0 and a.PROJECTCODE=@PROJECTCODE and b.proc_formid<>'" + formid1 + "'";//排除掉本单。
                                                                                                           //po占用金额
                double cusedbudget = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, bprojectcode));
                sql = @"select isnull(sum(CONVERT(decimal(18,6),isnull(a.EXCHANGRATE,0))*CONVERT(decimal(18,2),isnull(b.UNITPRICE,0))* CONVERT(decimal(18,2),isnull(b.QTY,0))),0) 
from MD_PURCHASEORDER_CONBITION a left join MD_DETAILPO b on a.GUID=b.PARENTGUID 
where a.ISACTIVE<>0 and b.ISACTIVE<>0 and a.PROJECTCODE=@PROJECTCODE ";
                //合成po占用金额
                double cusedbudget2 = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, bprojectcode));
                //预算余额 （总额-占用金额）
                double cbudgetbalance = cplanningamount - cusedbudget - cusedbudget2 - ConvertUtil.ToDouble(bnetamount);
                if (cbudgetbalance < 0)
                {
                    returnvalue = bprojectcode;
                    break;
                }

                if (string.IsNullOrEmpty(returnvalue))
                {
                    returnvalue = "0";
                }
                JosnString = MyLib.SerializeUtil.JsonSerialize(returnvalue);
                this.response.Write(JosnString);
                break;
            case "getMarketingBudgetControl":
                string brand = this.request["Brand"] ?? "";
                string year = this.request["Year"] ?? "";
                string netamount = this.request["NetAmount"] ?? "";
                string formid2 = this.request["formid"] ?? "";
                string[] brandary = brand.Split('|');
                string[] yearary = year.Split('|');
                string[] netamountary = netamount.Split('|');
                for (int i = 0; i < brandary.Length - 1; i++)
                {
                    sql = @"select balance from V_MARKETBUDGET where value=@value and year=@year ";
                    double balance = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, brandary[i], yearary[i]));
                    double judge = balance - ConvertUtil.ToDouble(netamountary[i]);
                    //求出本单子修改之前的汇总
                    string str = "select isnull(sum(CONVERT(decimal(18,6),isnull(a.EXCHANGRATE,0))*CONVERT(decimal(18,2), " +
                        "isnull(b.UNITPRICE,0))* CONVERT(decimal(18,2),isnull(b.QTY,0))),0)   " +
                        "from MD_PURCHASEORDER a left join MD_PURCHASEORDERITEM b on a.GUID=b.PARENT_GUID " +
                        "where b.PROC_FORMID=@p1";
                    double formamount = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(str, formid2));
                    if (judge + formamount < 0) //把这单金额重新加上。
                    {
                        sql = @"select name from V_MARKETBUDGET where value=@value ";
                        string TYPE_Brand = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql, brandary[i]));

                        DataRow drr = dt.NewRow();
                        dt.Columns.Add("name");
                        dt.Columns.Add("year");
                        drr["year"] = yearary[i];
                        drr["name"] = TYPE_Brand;
                        dt.Rows.Add(drr);
                        break;
                    }
                }
                if (dt == null || dt.Rows.Count == 0)
                {
                    string cango = "0";
                    JosnString = MyLib.SerializeUtil.JsonSerialize(cango);
                    this.response.Write(JosnString);
                }
                break;
            case "ECatalogStatus":
                string Code = this.request["Code"].Trim();
                string Status = this.request["Status"].Trim();
                if (Status == "Enable")
                {
                    sql = "update MD_PURCHASEITEM set [STATUS] = 0 where ID = " + Code + "";
                    int i = DataAccess.Instance("BizDB").ExecuteNonQuery(sql);
                    this.response.Write("Disablesuccess");
                }
                if (Status == "Disable")
                {
                    sql = "update MD_PURCHASEITEM set [STATUS] = 1 where ID = " + Code + "";
                    int i = DataAccess.Instance("BizDB").ExecuteNonQuery(sql);
                    this.response.Write("Enablesuccess");
                }
                break;
            case "getBudgetBalanceSis":
                string sisbrand = this.request["Brand"] ?? "";
                sql = @"select BUDGET from [MD_MARKETBUDGET] where id=@brand ";
                //市场品牌预算
                double sisbrandbudget = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, sisbrand));

                sql = @"select isnull(sum(CONVERT(decimal(18,2),isnull(GROSSAMOUNT,0))),0) from MD_PURCHASEORDER a left join MD_PURCHASEORDERITEM b on a.PROC_FORMID=b.PROC_FORMID 
where a.ISACTIVE<>0 and b.ISACTIVE=1 and BRANDDIVISION=@brand and PROCURMENTTYPE='Marketing Expense' 
or(PROCURMENTTYPE='Retailer POS Decoration' and b.WBSELEMENT='S0101') ";
                //po占用金额
                double sismarketingcusedbudget = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, sisbrand));

                sql = @"select isnull(sum(CONVERT(decimal(18,2),isnull(GROSSAMOUNT,0))),0) from MD_PURCHASEORDER_CONBITION a left join MD_DETAILPO b on a.GUID=b.PARENTGUID 
where a.ISACTIVE<>0 and b.ISACTIVE=1 and BRANDDIVISION=@brand and PROCURMENTTYPE='Marketing Expense' 
or(PROCURMENTTYPE='Retailer POS Decoration' and b.WBSELEMENT='S0101') ";
                //合成po占用金额
                double sismarketingcusedbudget2 = ConvertUtil.ToDouble(DataAccess.Instance("BizDB").ExecuteScalar(sql, sisbrand));

                //预算余额 （总额-占用金额）
                double sismbudgetbalance = sisbrandbudget - sismarketingcusedbudget - sismarketingcusedbudget2;
                JosnString = MyLib.SerializeUtil.JsonSerialize(sismbudgetbalance);
                this.response.Write(JosnString);

                break;
            case "getProjectUrl":
                string pprojectcode = this.request["ProjectCode"] ?? "";
                string projecttype = this.request["Type"] ?? "";
                if (projecttype.Equals("Retailer POS Decoration"))
                {
                    sql = @"select top 1 DOCUMENTNO,FORMID,INCIDENT,PROCESSNAME from PROC_SISRETAILFURNITURE where DOCUMENTNO=@DOCUMENTNO";
                }
                else
                {
                    sql = @"select top 1 projectcode,FORMID,INCIDENT,PROCESSNAME from PROC_PROJECTAPPLICATION where PROJECTCODE=@pprojectcode";
                }
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, pprojectcode);
                break;
            case "getVendorUrl":
                string vendorcode = this.request["VendorCode"] ?? "";
                sql = @"select top 1 DOCUMENTNO,FORMID,INCIDENT from PROC_VENDORMASTERAPPLICATION where DOCUMENTNO=@DOCUMENTNO";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, vendorcode);
                break;
            case "getCostCenter":
                string branddivisioncode = this.request["BRANDDIVISIONCODE"] ?? "";
                string costcenterprojectcode = this.request["ProjectCode"] ?? "";
                if (string.IsNullOrEmpty(costcenterprojectcode))
                {
                    sql = @"select (code+'-'+LongName) as title from ORG_COSTCENTER where IsActive=1 and code is not null and Longname is not null and branddivisioncode=@branddivisioncode";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, branddivisioncode);
                }
                else
                {
                    sql = @"select top 1 PROFITCENTERCODE from [dbo].[MD_PROJECTAPPLICATION] where PROJECTCODE=@PROJECTCODE ";
                    string profitcentercode = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql, costcenterprojectcode));
                    if (!string.IsNullOrEmpty(profitcentercode))
                    {
                        string[] profitcentercodecc = profitcentercode.Split('-');
                        sql = @"select (code+'-'+LongName) as title from ORG_COSTCENTER where IsActive=1 and code is not null and Longname is not null 
and branddivisioncode=@branddivisioncode and PROFITCODE=@profitcentercode";
                        dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, branddivisioncode, profitcentercodecc[0]);
                    }
                }
                if (dt == null || dt.Rows.Count == 0)
                {
                    DataRow drr = dt.NewRow();
                    drr["title"] = "";
                    dt.Rows.Add(drr);
                }
                break;
            case "getShortCostCenter":
                string detailbranddivisioncode = this.request["BRANDDIVISIONCODE"] ?? "";
                string ccompanycode = this.request["CCompanycode"] ?? "";
                string rowcostcenterprojectcode = this.request["ProjectCode"] ?? "";
                if (string.IsNullOrEmpty(rowcostcenterprojectcode))
                {
                    sql = @"select (code+'-'+Name) as title from ORG_COSTCENTER where IsActive=1 and code is not null and name is not null and branddivisioncode=@branddivisioncode and companycode=@ccompanycode";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, detailbranddivisioncode, ccompanycode);
                }
                else
                {
                    sql = @"select top 1 PROFITCENTERCODE from [dbo].[MD_PROJECTAPPLICATION] where PROJECTCODE=@PROJECTCODE ";
                    string profitcentercode = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql, rowcostcenterprojectcode));
                    if (!string.IsNullOrEmpty(profitcentercode))
                    {
                        string[] profitcentercodecc = profitcentercode.Split('-');
                        sql = @"select (code+'-'+Name) as title from ORG_COSTCENTER where IsActive=1 and code is not null and name is not null 
and branddivisioncode=@branddivisioncode and companycode=@ccompanycode and PROFITCODE=@profitcentercode";
                        dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, detailbranddivisioncode, ccompanycode, profitcentercodecc[0]);
                    }
                }
                if (dt == null || dt.Rows.Count == 0)
                {
                    DataRow drr = dt.NewRow();
                    drr["title"] = "";
                    dt.Rows.Add(drr);
                }
                break;
            case "Getcompanycode":
                string maincostcenter = this.request["Maincostcenter"] ?? "";
                string[] maincostcenterary = maincostcenter.Split('-');
                string costcode = maincostcenterary[0];
                sql = @"select companycode from ORG_COSTCENTER where  IsActive=1 and code=@code ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, costcode);
                break;
            case "ddl_CategoryI":
                string Id = context.Request["Id"].Trim();
                sql = string.Format("select ID,CATEGORYNAME from MD_PURCHASE_ECATALOGCATEGORY where PARENTID='{0}'", Id);
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
                break;
            case "getCatagoryCode":
                string Ids = context.Request["Id"].Trim();
                string sqlcode = "select CATEGORYCODE from  MD_PURCHASE_ECATALOGCATEGORY where ID =  " + Ids + "";
                DataTable dts = DataAccess.Instance("BizDB").ExecuteDataTable(sqlcode);
                sql = string.Format("select top 1 ITEMNO from MD_PURCHASEITEM  where ITEMNO like '{0}%' order by ITEMNO  desc", dts.Rows[0][0].ToString().Trim());
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
                if (dt.Rows.Count <= 0 || dt == null)
                {
                    DataRow dtr = dt.NewRow();
                    dtr["ITEMNO"] = dts.Rows[0][0].ToString().Trim() + "0000";
                    dt.Rows.Add(dtr);
                }
                break;
            case "getCERPOItem":
                string cerprojectcode = this.request["ProjectCode"] ?? "";
                sql = @"select WBSELEMENT,PLANNINGDESCRIPTION,PLANNINGAMOUNT,Code,c.Description from MD_PROJECTAPPLICATION a 
left join MD_PROJECTAPPLICATIONITEM b on a.GUID=b.PARENT_GUID 
left join MD_MerchandiseCategory c on b.WBSELEMENT=c.Code
where 
PROJECTCODE=@PROJECTCODE and 
ACCOUNTASSTELEM='X' and PROJECTTYPE='CER' and (PLANNINGAMOUNT is not null and PLANNINGAMOUNT!='' and CONVERT(decimal(18,2),PLANNINGAMOUNT)<>0 ) ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, cerprojectcode);
                break;
            case "getvendorquotationdata":
                string vendordocumentno = this.request["VendorDocumentNo"] ?? "";
                sql = @"select top 1 QUTOTATIONDATA from PROC_VENDORMASTERAPPLICATION where DOCUMENTNO=@DOCUMENTNO ";
                string qutotationdata = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql, vendordocumentno));
                if (string.IsNullOrEmpty(qutotationdata))
                {
                    JosnString = MyLib.SerializeUtil.JsonSerialize(2);
                    this.response.Write(JosnString);
                }
                else
                {
                    JosnString = MyLib.SerializeUtil.JsonSerialize(3);
                    this.response.Write(JosnString);
                }
                break;
            case "getcostcenterbrand":
                string strCostcenter = this.request["Costcenter"] ?? "";
                string[] sArray = strCostcenter.Split('-');
                string code = sArray[0];
                if (!string.IsNullOrEmpty(code))
                {
                    sql = @"select BRANDDIVISIONCODE,ORG_DEPARTMENT.DEPARTMENTNAME from ORG_COSTCENTER left join ORG_DEPARTMENT on ORG_COSTCENTER.BRANDDIVISIONCODE=ORG_DEPARTMENT.DEPARTMENTID where  ORG_COSTCENTER.IsActive=1 and ORG_COSTCENTER.CODE =@code";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, code);
                }
                break;
            case "getTaxRate":
                string PurchaseType = this.request["PurchaseType"] ?? "";
                string type = string.Empty;
                if (PurchaseType.Equals("0"))
                {
                    type = "General Procurement";
                }
                else if (PurchaseType.Equals("1"))
                {
                    type = "Marketing Expense";
                }
                else if (PurchaseType.Equals("2"))
                {
                    type = "CER";
                }
                else
                {
                    type = "Retailer POS Decoration";
                }
                sql = @"select VALUE,NAME from COM_RESOURCE where TYPE='type_taxcode' and  ISACTIVE=1 and EXT01 like '%" + type + "%' order by ORDERNO ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
                break;
            case "getrowdefaultcostcenter":
                string costCenterCode = this.request["costCenterCode"] ?? "";
                sql = @"select ORG_COSTCENTER.CODE,ORG_COSTCENTER.NAME,DEPARTMENTID,ORG_DEPARTMENT.DEPARTMENTNAME as BRAND from ORG_COSTCENTER left join ORG_DEPARTMENT 
on BRANDDIVISIONCODE=DEPARTMENTID  where ORG_COSTCENTER.IsActive=1 and ORG_COSTCENTER.CODE=@CODE ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, costCenterCode);
                break;
            case "costcenterValidation":
                string brandVal = this.request["Brand"] ?? "";
                string costcenterVal = this.request["Costcenter"] ?? "";
                string companycodeVal = this.request["Companycode"] ?? "";
                sql = @"select (code+'-'+Name) as sn,(code+'-'+LONGNAME) as ln from ORG_COSTCENTER where IsActive=1 and code=@code and branddivisioncode=@branddivisioncode and companycode=@ccompanycode ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, costcenterVal, brandVal, companycodeVal);
                if (dt == null || dt.Rows.Count == 0)
                {
                    JosnString = MyLib.SerializeUtil.JsonSerialize(2);
                    this.response.Write(JosnString);
                }
                break;
            case "MerchandiseCategoryValidation":
                string typeP = this.request["Typep"] ?? "";
                string strMerchandiseCategory = this.request["MerchandiseCategory"] ?? "";
                sql = @"select code from MD_MerchandiseCategory where Type=@Type and Description=@Description ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, typeP, strMerchandiseCategory);
                if (dt == null || dt.Rows.Count == 0)
                {
                    JosnString = MyLib.SerializeUtil.JsonSerialize(2);
                    this.response.Write(JosnString);
                }
                break;
            case "WBSValidation":
                string typeW = this.request["Typep"] ?? "";
                string strWBS = this.request["WBS"] ?? "";
                string strprojectcode = this.request["ProjectCode"] ?? "";
                string wBrand = this.request["Brand"] ?? "";
                if (typeW.Equals("CER"))
                {
                    sql = @"select WBSELEMENT,PLANNINGDESCRIPTION,PLANNINGAMOUNT,PROJECTTYPE,PROJECTCODE,Code,MD_MerchandiseCategory.Description from MD_PROJECTAPPLICATION 
left join MD_PROJECTAPPLICATIONITEM on MD_PROJECTAPPLICATION.GUID=MD_PROJECTAPPLICATIONITEM.PARENT_GUID
left join MD_MerchandiseCategory  on MD_PROJECTAPPLICATIONITEM.WBSELEMENT=MD_MerchandiseCategory.Code
where PROJECTTYPE like '%" + typeW + "%' and PROJECTCODE=@PROJECTCODE and WBSELEMENT=@WBSELEMENT ";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, strprojectcode, strWBS);
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        JosnString = MyLib.SerializeUtil.JsonSerialize(2);
                        this.response.Write(JosnString);
                    }
                }
                else
                {
                    string projectcode11 = strWBS.Split('-')[0];
                    string wbscode11 = strWBS.Split('-')[1];
                    string year11 = "20" + strWBS.Substring(strWBS.Length - 2, 2);

                    sql = @"select * from MD_PROJECTAPPLICATION where BRANDORDIVISIONCODE=@BRANDORDIVISIONCODE and PROJECTCODE=@PROJECTCODE ";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, wBrand, projectcode11);
                    if (dt == null || dt.Rows.Count == 0)
                    {
                        JosnString = MyLib.SerializeUtil.JsonSerialize(3);
                        this.response.Write(JosnString);
                    }
                    else
                    {
                        sql = @"select WBSELEMENT,PLANNINGDESCRIPTION,PLANNINGAMOUNT,PROJECTTYPE,PROJECTCODE from MD_PROJECTAPPLICATION 
left join MD_PROJECTAPPLICATIONITEM on MD_PROJECTAPPLICATION.GUID=MD_PROJECTAPPLICATIONITEM.PARENT_GUID
where WBSELEMENT=@WBSELEMENT and PROJECTCODE=@PROJECTCODE and YEAR=@YEAR and PROJECTTYPE like '%" + typeW + "%'";
                        dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, wbscode11, projectcode11, year11);
                        if (dt == null || dt.Rows.Count == 0)
                        {
                            JosnString = MyLib.SerializeUtil.JsonSerialize(2);
                            this.response.Write(JosnString);
                        }
                    }
                }
                break;
            case "getSISPOItem":
                string sispoguid = this.request["Sispoguid"] ?? "";
                sql = @"select MERCHANDISECATEGORY,MERCHANDISECATEGORYCODE,WBSELEMENT,EXPENSECATEGORY,DESCRIPTION,QTY
,UNITPRICE,VATRATE,VATRATE_NAME,VATRATE_CODE,NETAMOUNT,GROSSAMOUNT,DATEFROM,DATETO,PROJECTBUDGETBALANCE,BRAND,BRANDNAME
from MD_PURCHASEORDERITEM where PARENT_GUID=@PARENT_GUID order by POITEM";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, sispoguid);
                break;
            case "projectprofitcenterrow":
                string pProjectCode = this.request["ProjectCode"] ?? "";
                sql = @"select  PROFITCENTERCODE+'-'+PROFITCENTERSHORTNAME as sn,PROFITCENTERCODE+'-'+PROFITCENTERLONGNAME as ln  from ORG_PROFITCENTER where PROFITCENTERCODE=
(select top 1 substring(PROFITCENTERCODE,0,11) from MD_PROJECTAPPLICATION where PROJECTCODE=@PROJECTCODE)";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, pProjectCode);
                break;
            case "projectcostcenterrow":
                string pcProjectCode = this.request["ProjectCode"] ?? "";
                sql = @"select  CODE+'-'+NAME as sn,CODE+'-'+LONGNAME as ln  from ORG_COSTCENTER where IsActive=1 and CODE=
(select top 1 substring(COSTCENTERNAME,0,11) from MD_PROJECTAPPLICATION where PROJECTCODE=@PROJECTCODE )";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, pcProjectCode);
                break;
            case "getbranddivsion":
                string bdPurchaseType = this.request["PurchaseType"] ?? "";
                if (bdPurchaseType.Equals("Marketing Expense"))
                {
                    sql = @"select * from ORG_DEPARTMENT where DEPARTMENTTYPE='BD' and ISACTIVE=1  and ext02=1 order by ext01 asc";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
                }
                else
                {
                    sql = @"select * from ORG_DEPARTMENT where DEPARTMENTTYPE in ('DH','BD') and ISACTIVE=1 and ext02=1 order by DEPARTMENTTYPE,ext01,DEPARTMENTNAME asc";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
                }
                break;
            case "sisPoSubmitControl":
                string spscprojectcode = this.request["Projectcode"] ?? "";
                string spscvendorno = this.request["Vendorno"] ?? "";
                if (!string.IsNullOrEmpty(spscprojectcode))
                {
                    sql = @"select * from PROC_PURCHASEORDER where PROJECTCODE=@PROJECTCODE and VENDORNO=@VENDORNO and status in (1,2,3) ";
                    dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, spscprojectcode, spscvendorno);
                }
                break;
            case "BdorDhJudge":
                string BDJDEPARTMENTID = this.request["DEPARTMENTID"] ?? "";
                sql = @"select top 1 DEPARTMENTTYPE from ORG_DEPARTMENT where DEPARTMENTID=@id ";
                dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, BDJDEPARTMENTID);
                break;
            case "JudgeCategory":
                string jccode = this.request["Temp"] ?? "";
                string[] jccodeary = jccode.Split('$');
                string jcbool = "ma";
                for (int i = 0; i < jccodeary.Length - 1; i++)
                {
                    sql = @"select top 1 AccountCode from MD_MerchandiseCategory where (AccountCode like '00782%' or AccountCode='0078602700') and Code=@Code";
                    string jcmedia = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql, jccodeary[0]));
                    if (string.IsNullOrEmpty(jcmedia))                //不存在media
                    {
                        sql = @"select top 1 AccountCode from MD_MerchandiseCategory where AccountCode like '00788%' and Code=@Code";
                        string jcarchitect = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql, jccodeary[0]));
                        if (string.IsNullOrEmpty(jcarchitect))            //不存在Architect
                        {
                            continue;
                        }
                        else
                        {
                            jcbool = "a";
                            break;
                        }
                    }
                    else
                    {
                        jcbool = "m";
                        break;
                    }
                }
                JosnString = MyLib.SerializeUtil.JsonSerialize(jcbool);
                this.response.Write(JosnString);
                break;
            default:
                break;
        }

        if (dt != null && dt.Rows.Count > 0)
        {
            JosnString = MyLib.SerializeUtil.JsonSerialize(dt);
            this.response.Write(JosnString);
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}