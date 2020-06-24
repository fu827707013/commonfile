<%@ WebHandler Language="C#" Class="InvoiceMatchHandler" %>

using MyLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using UPL.Common.BussinessControl;
using Ultimus.UWF.Workflow.Interface;
using Ultimus.UWF.Workflow.Entity;
using System.Web.SessionState;
using System.Text;
using Ultimus.UWF.Common.Logic;
using System.Data;
using System.Data.Common;

public class ResultEntity
{
    public string status;
    public string msg;
    public string success = "success";
    public string error = "error";
}

/// <summary>
/// ContractManagementHandler 的摘要说明
/// </summary>
public class InvoiceMatchHandler : IHttpHandler, IRequiresSessionState
{

    HttpRequest request;
    HttpResponse response;
    public string _lang;
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "text/plain";
        string method = context.Request["Method"] ?? "";
        switch (method)
        {
            //获取发票主信息
            case "GetInformation":
                GetInformation(context);
                break;
            //获取PO对应的 付款单
            case "GetPRDocument":
                GetPRDocument(context);
                break;
            //发票匹配时，PO明细数据
            case "GetDetailData":
                GetDetailData(context);
                break;
            //保存数据
            case "SaveData":
                SaveData(context);
                break;
            //SaveInvoice逻辑与SaveData保持一致，保存并发起付款
            case "SaveInvoice":
                SaveInvoice(context);
                break;
            //发起付款
            case "ToPR":
                ToPR(context);
                break;
            //获取PO单据信息，打开PO单
            case "GetPO":
                GetPO(context);
                break;
            //获取发票状态
            case "GetInvoice":
                GetInvoice(context);
                break;
            //点击发票列表，打开并展示发票信息
            case "GetDetailList":
                GetDetailList(context);
                break;
            //撤销匹配
            case "UndoMatch":
                UndoMatch(context);
                break;
            //获取发票状态
            case "GetInvoiceStatus":
                GetInvoiceStatus(context);
                break;
            //获取PO单据状态，主要用于保存时判断PO是否审批完成
            case "GetPoStatus":
                GetPoStatus(context);
                break;
            //付款完毕后，清空PO明细剩余金额
            case "CleaningBalance":
                GetCB(context);
                break;
            //获取付款信息
            case "GetPaymentDetail":
                GetPaymentDetail(context);
                break;
            //查询PO名下行
            case "SearchPaymentStatus":
                SearchPaymentStatus(context);
                break;
            case "GetFileUrl":
                GetFileUrl(context);
                break;
        }
    }

    //获取发票状态;防止流程匹配后未刷新页面后继续付款
    public void GetInvoiceStatus(HttpContext context)
    {
        string msg = "";
        try
        {
            //获取发票号码
            string _json = context.Request["Json"];
            DataTable dt = MyLib.SerializeUtil.Json2DataSet(_json).Tables["Main"];
            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string _InvoiceNo = ConvertUtil.ToString(dt.Rows[i]["INVOICENO"]);
                    string _PONO = ConvertUtil.ToString(dt.Rows[i]["PONO"]);
                    //                    //查询是否预提
                    //                    string sql_yt = string.Format(@"SELECT count(0)  FROM V_POACCRUEDLIST 
                    //where  guid not in(select parentguid  from MD_POACCRUEDITEM where status='1')  and 1=1 and PROC_DOCUMENTNO like N'%{0}%' ", _PONO);
                    //                    int str_yt_count = ConvertUtil.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(sql_yt));
                    //                    if (str_yt_count > 0)
                    //                    {
                    //                        msg = "【" + _PONO + "】暂未预提，请先进行预提/[" + _PONO + "] Not yet withholding, please pre-accept";
                    //                        msg += "\r";
                    //                        continue;
                    //                    }

                    //查询是否为已付款
                    string sql = string.Format(@"SELECT count(0) FROM PROC_PAYMENTREQUEST where formid in (
SELECT formid FROM PROC_PAYMENTREQUEST_DT  where invoiceno=N'{0}') and status in (1,2,3) and incident>0", _InvoiceNo);
                    int count = ConvertUtil.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(sql));
                    if (count > 0)
                    {
                        msg += "【" + _InvoiceNo + "】已付款/[" + _InvoiceNo + "] paid";
                        msg += "\r";
                        continue;
                    }
                }
            }

        }
        catch (Exception ex)
        {
            msg += "Error:" + ex.Message;
        }
        context.Response.Write(msg);
    }

    //撤销匹配
    public void UndoMatch(HttpContext context)
    {
        string msg = "";
        try
        {
            string _json = context.Request["Json"];
            DataTable dt = MyLib.SerializeUtil.Json2DataSet(_json).Tables["Main"];
            if (dt != null)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string Line = ConvertUtil.ToString(dt.Rows[i]["LINENO"]);
                    string InvoiceNO = ConvertUtil.ToString(dt.Rows[i]["INVOICENO"]);
                    //查询是否可以撤销
                    //是否付款
                    string sql = string.Format(@"SELECT count(0) FROM PROC_PAYMENTREQUEST where formid in (
SELECT formid FROM PROC_PAYMENTREQUEST_DT  where invoiceno=N'{0}') and status in (1,2,3) and incident>0", InvoiceNO);
                    int count = ConvertUtil.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(sql));
                    if (count > 0)
                    {
                        // msg += "第" + Line + "行，【" + InvoiceNO + "】已付款无法取消/Line " + Line + ", [" + InvoiceNO + "] Payment cannot be cancelled";
                        msg += "【" + InvoiceNO + "】已付款无法取消/[" + InvoiceNO + "] Payment cannot be cancelled";
                        msg += "\r";
                        continue;
                    }
                    //是否匹配
                    sql = string.Format(@"SELECT count(0) FROM MD_INVOICEPARKINGLIST_DT where invoiceno=N'{0}' ", InvoiceNO);
                    count = ConvertUtil.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(sql));
                    if (count == 0)
                    {
                        msg += "【" + InvoiceNO + "】未匹配/[" + InvoiceNO + "] Unmatched";
                        msg += "\r";
                        continue;
                    }
                    //是否匹配
                    sql = string.Format(@"SELECT count(0) FROM MD_INVOICEPARKINGLIST where invoiceno=N'{0}' and INVOICESTATUS=N'4' ", InvoiceNO);
                    count = ConvertUtil.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(sql));
                    if (count > 0)
                    {
                        msg += "【" + InvoiceNO + "】已经撤销，无法再次撤销/[" + InvoiceNO + "] Cancelled, cannot be revoked again";
                        msg += "\r";
                        continue;
                    }
                    //更新数据
                    int num = DataAccess.Instance("BizDB").ExecuteNonQuery("update MD_INVOICEPARKINGLIST set INVOICESTATUS = N'4' where INVOICENO = N'" + InvoiceNO + "'");
                    if (num > 0)
                    {
                        msg += "【" + InvoiceNO + "】撤销成功/[" + InvoiceNO + "] Successful cancellation";
                        msg += "\r";
                    }
                    else
                    {
                        msg += "【" + InvoiceNO + "】撤销失败/[" + InvoiceNO + "] Undo failed";
                        msg += "\r";
                    }
                    //删除以匹配数据 --abscop20190909

                    sql = @"--删除该发票下po行对应存在的CB行
                    delete from MD_INVOICEPARKINGLIST_DT where POITEM_ROWGUID in(select POITEM_ROWGUID from MD_INVOICEPARKINGLIST_DT where INVOICENO=@INVOICENO ) and BALANCESTATUS='CB' ;
                    --更新该发票下po行对应的已被更新成6po行改为1
                    update MD_PURCHASEORDERITEM set ISACTIVE='1' where guid in(select POITEM_ROWGUID from MD_INVOICEPARKINGLIST_DT where INVOICENO=@INVOICENO ) and ISACTIVE='6';
                    update MD_PURCHASEORDERITEM set ISACTIVE='1' where guid in(select POITEM_ROWGUID from MD_INVOICEPARKINGLIST_DT where INVOICENO=@INVOICENO ) and ISACTIVE='6';
                    update MD_PURCHASEORDERITEM set ISACTIVE='1' where guid in(select POITEM_ROWGUID from MD_INVOICEPARKINGLIST_DT where INVOICENO=@INVOICENO ) and ISACTIVE='6'; 
                    --删除该发票
                    delete from MD_INVOICEPARKINGLIST_DT where INVOICENO =@INVOICENO ";
                    int kk = DataAccess.Instance("BizDB").ExecuteNonQuery(sql, InvoiceNO);
                    if (kk > 0)
                    {
                        MyLib.LogUtil.Info("发票撤销：发票号：" + InvoiceNO + "   执行SQL：" + sql.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
        }
        context.Response.Write(msg);
    }

    //获取发票状态
    public void GetInvoice(HttpContext context)
    {
        string result = "";
        try
        {
            string invoiceNo = context.Request["invoiceNo"];
            string sql = "select count(0) from MD_INVOICEPARKINGLIST_DT where invoiceno=N'" + invoiceNo + "'";
            int i = Convert.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(sql));
            result = i.ToString();
        }
        catch (Exception ex)
        {
            result = "-1";
        }
        context.Response.Write(result);
    }

    //获取发票主信息
    public void GetInformation(HttpContext context)
    {
        try
        {
            string invoiceNo = context.Request["invoiceNo"];
            string sql = @"select           a.[INVOICEDATES],
                                            --(cast(REPLACE(a.[NETAMOUNT],',','') as decimal(20,2))+cast(replace(a.[VATAMOUNT],',','') as decimal(20,2))) as [INVOICEAMOUNTWITHVAT],
                                                (cast(REPLACE(a.[NETAMOUNT],',','') as decimal(20,2))) as [INVOICEAMOUNTWITHVAT],           
                                            a.[VATAMOUNT],
                                            a.[PONO],
											a.LEGALENTITY,
                                            d.[SUMRATEAMOUNT] as [POAMOUNTWITHOUTVAT],
                                            a.[TAXCODE],
                                            a.[VENDORNO],
                                            a.[VENDORNAME],
                                            a.[INVOICEAFTERPAYMENT],
                                            a.[INVOICESTATUS],
                                            c.[PAYMENTTYPE],
                                            a.[ID] as [ID],
                                            d.[GUID] as [GUID],
                                            d.[PROCURMENTTYPE] as [PROCURMENTTYPE],
                                            a.EXT15,
                                            a.EXT14,
                                            a.EXT13,
                                            A.EXT12,
                                            A.EXT11                                                            
                                from MD_INVOICEPARKINGLIST a left join V_POMAIN d on a.PONO=d.proc_documentno left join 
(select pe.* from (select POGUID,max(REQUESTDATE) as REQUESTDATE  FROM PROC_PAYMENTREQUEST where status in(1,2,3) group by  POGUID ) pg ---获取最新的一条付款信息
left join  PROC_PAYMENTREQUEST pe on pg.POGUID=pe.POGUID and pg.REQUESTDATE=pe.REQUESTDATE )  c on d.PROC_DOCUMENTNO = c.PONUMBER
   where 1=1 ";

            if (!string.IsNullOrEmpty(invoiceNo))
            {
                sql += " and ( INVOICENO = N'" + invoiceNo.TrimEnd(';') + "')";
            }
            else
            {
                sql += " and 1=2 ";
            }
            DataTable dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
            if (dt.Rows.Count > 0 && dt != null)
            {
                string jsonStr = MyLib.SerializeUtil.JsonSerialize(dt);
                context.Response.Write(jsonStr);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public void GetPRDocument(HttpContext context)
    {
        string invoiceNo = context.Request["invoiceNo"];
        string sql = @"select          
                   distinct c.DOCUMENTNO,c.FORMID,c.PROCESSNAME,c.INCIDENT
                    from MD_INVOICEPARKINGLIST a join V_POMAIN b 
                    on a.PONO = b.PROC_DOCUMENTNO
                    left join PROC_PAYMENTREQUEST c
                    on b.PROC_DOCUMENTNO = c.PONUMBER
                    where 1=1 and c.status IN (1,2,3) ";
        if (!string.IsNullOrEmpty(invoiceNo))
        {
            sql += " and ( INVOICENO = N'" + invoiceNo + "')";
        }
        else
        {
            sql += " and 1=2 ";
        }
        DataTable dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
        string jsonStr = "";
        if (dt.Rows.Count > 0 && dt != null)
        {
            jsonStr = MyLib.SerializeUtil.JsonSerialize(dt);
        }
        context.Response.Write(jsonStr);
    }

    public void GetDetailList(HttpContext context)
    {
        try
        {
            string _GUID = context.Request["GUID"];
            string _InvoiceNo = context.Request["invoiceNo"];
            DataTable dt_check = GetPoDetailInfo(_InvoiceNo, _GUID, true, false);
            if (dt_check != null && dt_check.Rows.Count > 0)
            {
                string jstr = MyLib.SerializeUtil.JsonSerialize(dt_check);
                context.Response.Write(jstr);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public void GetDetailData(HttpContext context)
    {
        try
        {
            string _GUID = context.Request["GUID"];
            string _InvoiceNo = context.Request["invoiceNo"];
            //string sql_where = @"   and guid not in (SELECT POITEM_ROWGUID FROM MD_INVOICEPARKINGLIST_DT WHERE BALANCESTATUS='CB') ";//已CN行不展示

            //jhlian 调整排除CB的数据
            string sql_where = "";
            string sql_where1 = @"   and (guid not in (SELECT guid FROM V_PODETAIL WHERE ISACTIVE=6) or INVOICENO is null) ";//已CN行不展示

            DataTable dt_check = GetPoDetailInfo(_InvoiceNo, _GUID, false, true);
            if (dt_check != null && dt_check.Rows.Count > 0)
            {
                string jstr = MyLib.SerializeUtil.JsonSerialize(dt_check);
                context.Response.Write(jstr);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    public void GetFileUrl(HttpContext context)
    {
        string _InvoiceNo = context.Request["invoiceNo"];
        // 获取发票影像
        string sql = "select *,filename as NEWNAME from MD_INVOICEPARKING01 where  invoiceno='" + _InvoiceNo + "'";
        DataTable dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
        if (dt != null && dt.Rows.Count > 0)
        {
            string fileName = ConvertUtil.ToString(dt.Rows[0]["FILENAME"]);
            string fileNameEncode = DESEncrypt.Encrypt(HttpUtility.UrlEncode(ConvertUtil.ToString(fileName)));
            string newName = ConvertUtil.ToString(dt.Rows[0]["NEWNAME"]);
            string funder = ConvertUtil.ToString(dt.Rows[0]["FUNDER"]);
            string path = GetUrl(newName, funder);
            dt.Columns.Add("FILENAMEENCODE", typeof(string));
            //dt.Columns.Add("FILENAME1", typeof(string));
            dt.Columns.Add("PATH", typeof(string));
            foreach (DataRow dr in dt.Rows)
            {
                dr["FILENAMEENCODE"] = fileNameEncode;
                //dr["FILENAME"] = fileName;
                dr["PATH"] = path;
            }
        }
        string jstr = MyLib.SerializeUtil.JsonSerialize(dt);
        context.Response.Write(jstr);
    }
    public string GetUrl(object fileName, object userid)
    {
        string path = string.Empty;
        if (MyLib.ConfigurationManager.AppSettings["AttachmentOptions"] == "1")
            path = MyLib.ConfigurationManager.AppSettings["FTPAttachmentServerIP"];
        else
            path = MyLib.ConfigurationManager.AppSettings["AttachmentOpenPath"];

        //string p = ConvertUtil.ToString(processname).TrimEnd();
        //string s = ConvertUtil.ToDateTime(createDate).ToString("yyyy\\\\MM\\\\dd") + "\\" + p + "\\" + ConvertUtil.ToString(newname) + ConvertUtil.ToString(fileType);
        //return DESEncrypt.Encrypt(HttpUtility.UrlEncode(path + s));

        // var fileUrl = MyLib.ConfigurationManager.AppSettings["RootPath"];
        string fileUrl = WebUtil.GetRootPath();
        //获取当前登录人登录名
        //string _LoginName = SessionLogic.GetLoginName();
        //var currentUser = _LoginName.TrimEnd().Replace("\\", "/").Split('/')[1];
        // var url = fileUrl + "\\File\\Invoice\\" + currentUser.Replace(currentDomin, "") + "\\out\\" + fileName;
        var url = path + "\\Invoice\\" + userid + "\\out\\" + fileName;
        return DESEncrypt.Encrypt(HttpUtility.UrlEncode(url));

    }

    //PO明细信息查询
    public DataTable GetDataInfo(string _InvoiceNo, string _GUID, string sql_where, string sql_where1, string _pono)
    {
        DataTable dt_check = new DataTable();
        try
        {
            if (!string.IsNullOrEmpty(_GUID))
            {
                string sql_po = string.Format("SELECT PROC_DOCUMENTNO FROM V_POMAIN  where guid='{0}' ", _GUID);
                _pono = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_po));
            }
            else if (!string.IsNullOrEmpty(_pono))
            {
                string sql_po = string.Format("SELECT guid FROM V_POMAIN  where PROC_DOCUMENTNO='{0}' ", _pono);
                _GUID = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_po));
            }
            string checkSql = string.Format(@"select poi.PARENT_GUID,poi.GUID,poi.POITEM,[DESCRIPTION],NETAMOUNT,poi.DATEFROM,poi.DATETO,
SUM(ISNULL(md1.PAYMENTAMOUNT,0.00))  UNPAYNETAMOUNT,SUM(ISNULL(md2.NETPAYMENTAMOUNT,0.00))-SUM(isnull(PY_IMAP.PAYMENTAMOUNT,0.00)) UNMATCHED,PAYNETAMOUNT,
(isnull(NETAMOUNT,0.00)- sum(isnull(md3.PAYMENTAMOUNT,0.00))-SUM(isnull(PY_IMAP.PAYMENTAMOUNT,0.00))) as [BALANCEAMOUNT]
,poi.PAYMENTTYPE ,md4.PAYMENTAMOUNT as UPAYMENTAMOUNT,md4.BALANCESTATUS,poi.LEGALENTITY,poi.YUTI,poi.PROCURMENTTYPE 
from V_INVOICEMATCHLIST poi 
left join 
(select POITEM_ROWGUID,sum(convert(decimal(18,2),isnull(PAYMENTAMOUNT,0.00))) AS PAYMENTAMOUNT from MD_INVOICEPARKINGLIST_DT 
WHERE BALANCESTATUS IN (N'KB') and PONO=N'{3}'  and  INVOICENO in (select INVOICENO from MD_INVOICEPARKINGLIST where INVOICESTATUS<>4) and
 INVOICENO NOT in (SELECT INVOICENO FROM  PROC_PAYMENTREQUEST LEFT JOIN PROC_PAYMENTREQUEST_DT ON PROC_PAYMENTREQUEST.FORMID=PROC_PAYMENTREQUEST_DT.FORMID
WHERE STATUS IN (1,2,3) and INVOICENO is not null) {2} group by POITEM_ROWGUID)  md1   on poi.GUID=md1.POITEM_ROWGUID 
left join (SELECT sum(ISNULL(PROC_PAYMENTREQUEST_DT.PAYAMOUNT,0.00)) AS NETPAYMENTAMOUNT,POROWID FROM  PROC_PAYMENTREQUEST LEFT JOIN PROC_PAYMENTREQUEST_DT ON PROC_PAYMENTREQUEST.FORMID=PROC_PAYMENTREQUEST_DT.FORMID
WHERE STATUS IN (1,2,3) AND ISNULL(INVOICENO,'')='' GROUP BY POROWID)  md2  on poi.GUID=md2.POROWID 
left join 
(select POITEM_ROWGUID,sum(convert(decimal(18,2),isnull(PAYMENTAMOUNT,0.00))) AS PAYMENTAMOUNT from
MD_INVOICEPARKINGLIST_DT WHERE   PONO=N'{3}' and INVOICENO in (select INVOICENO from MD_INVOICEPARKINGLIST where INVOICESTATUS<>4)  {2}  
group by POITEM_ROWGUID) md3  on poi.GUID=md3.POITEM_ROWGUID 
LEFT JOIN 
(select a.POITEM_ROWGUID,A.INVOICENO ,A.PAYMENTAMOUNT,(CASE WHEN ISNULL(B.BALANCESTATUS,'')='CB' THEN 'CB' ELSE A.BALANCESTATUS END) AS BALANCESTATUS
 from (select * from MD_INVOICEPARKINGLIST_DT where  PONO=N'{3}' and  BALANCESTATUS=N'KB') a 
	left join 
		(select * from MD_INVOICEPARKINGLIST_DT where  PONO=N'{3}' and  BALANCESTATUS=N'CB' and isnull(PAYMENTAMOUNT,0.00)<>0) b 
			on a.GUID =b.GUID AND A.INVOICENO=B.INVOICENO AND A.PONO=B.PONO AND A.POITEM=B.POITEM AND A.POITEM_ROWGUID=B.POITEM_ROWGUID
where  A.INVOICENO in (select INVOICENO from MD_INVOICEPARKINGLIST where INVOICESTATUS<>4) AND ISNULL(A.INVOICENO,'') =N'{0}' ) md4 ON poi.GUID=md4.POITEM_ROWGUID 
left join 
(SELECT POROWID,sum(isnull(PAYAMOUNT,0.00)) as PAYMENTAMOUNT FROM PROC_PAYMENTREQUEST_DT where INCIDENT=-99 and POROWID LIKE N'IMPAPO00_%' group by POROWID
) PY_IMAP on poi.GUID=PY_IMAP.POROWID 
where 1=1  and [PARENT_GUID] = N'{1}' {4} group by poi.PARENT_GUID,poi.GUID,poi.POITEM,[DESCRIPTION],poi.DATEFROM,poi.DATETO,NETAMOUNT,PAYNETAMOUNT,poi.PAYMENTTYPE
,md4.PAYMENTAMOUNT,poi.LEGALENTITY,poi.YUTI,poi.PROCURMENTTYPE,md4.BALANCESTATUS order by poi.POITEM ", _InvoiceNo, _GUID, sql_where, _pono, sql_where1);
            dt_check = DataAccess.Instance("BizDB").ExecuteDataTable(checkSql);

        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        return dt_check;
    }

    /// <summary>
    /// 获取Item 明细
    /// </summary>
    /// <param name="_InvoiceNo">发票号</param>
    /// <param name="_GUID">PO GUID</param>
    /// <param name="IS_CB">true:显示CB行，false：不显示CB行</param>
    /// <param name="IS_Invoice">true:排除当前发票金额，false：</param>
    /// <returns></returns>
    public DataTable GetPoDetailInfo(string _InvoiceNo, string _GUID, bool IS_CB, bool IS_Invoice)
    {
        DataTable dt_check = new DataTable();
        //形成列
        dt_check.Columns.Add("PARENT_GUID");//PO 唯一号
        dt_check.Columns.Add("GUID"); ///PO ITEM 唯一号
        dt_check.Columns.Add("POITEM"); // NO
        dt_check.Columns.Add("DESCRIPTION"); //描述
        dt_check.Columns.Add("NETAMOUNT"); //总金额
        dt_check.Columns.Add("DATEFROM");  //开始时间
        dt_check.Columns.Add("DATETO");//结束时间
        dt_check.Columns.Add("UNPAYNETAMOUNT");//已匹配 未付款金额
        dt_check.Columns.Add("UNMATCHED");//已付款未匹配金额
        dt_check.Columns.Add("PAYNETAMOUNT");//已付款金额
        dt_check.Columns.Add("BALANCEAMOUNT");//可匹配金额
        dt_check.Columns.Add("PAYMENTTYPE");//付款类型
        dt_check.Columns.Add("UPAYMENTAMOUNT");//匹配金额
        dt_check.Columns.Add("BALANCESTATUS");//匹配状态
        dt_check.Columns.Add("LEGALENTITY");//公司
        dt_check.Columns.Add("YUTI");//是否已预提
        dt_check.Columns.Add("PROCURMENTTYPE");//采购类型
        dt_check.Columns.Add("DOWNPAYMENTAMOUNT");//预付款金额
        try
        {
            //获取PO号
            string sql_po = string.Format("SELECT * FROM V_POMAIN  where guid='{0}' ", _GUID);
            DataTable dt_po = DataAccess.Instance("BizDB").ExecuteDataTable(sql_po);
            string _pono = "";
            string _procurMentType = "";
            if (dt_po != null)
            {
                _pono = ConvertUtil.ToString(dt_po.Rows[0]["PROC_DOCUMENTNO"]);
                _procurMentType = ConvertUtil.ToString(dt_po.Rows[0]["PROCURMENTTYPE"]);
            }
            if (string.IsNullOrEmpty(_pono))
            {
                return dt_check;
            }
            //根据GUID查询 PODetail行信息
            string sql_ItemList = string.Format("SELECT * FROM V_PODETAIL WHERE [PARENT_GUID] = N'{0}' ", _GUID);
            if (!IS_CB)
            {
                // sql_ItemList += " and ISACTIVE <> 6 "; ///预付款时，出现错误，去除
            }
            sql_ItemList += " order by POITEM ";
            DataTable dt_ItemList = DataAccess.Instance("BizDB").ExecuteDataTable(sql_ItemList);

            string sql_invoice = "";
            if (IS_Invoice)
            {
                sql_invoice = " AND INVOICENO<>N'" + _InvoiceNo + "' ";
            }
            //PO 已CB行。。需通过金额进行判断，是否存在手动CB
            string sql_PO_CB = string.Format(@"SELECT POITEM_ROWGUID,SUM(isnull(PAYMENTAMOUNT,0)) AS PAYMENTAMOUNT FROM MD_INVOICEPARKINGLIST_DT 
WHERE (ParentGUID IN (SELECT ID FROM MD_INVOICEPARKINGLIST where INVOICESTATUS<>4 and PONO=N'{0}' {1} )
OR (PONO=N'' AND ISNULL(INVOICENO,'')='{0}')) AND BALANCESTATUS=N'CB' group by POITEM_ROWGUID ", _pono, sql_invoice);

            DataTable dt_po_cb = DataAccess.Instance("BizDB").ExecuteDataTable(sql_PO_CB);

            //查询KB行金额,后续需要与CB行校验。进行金额清空操作
            string sql_MatchList = string.Format(@"SELECT INVOICENO,POITEM_ROWGUID,SUM(isnull(PAYMENTAMOUNT,0)) AS PAYMENTAMOUNT FROM MD_INVOICEPARKINGLIST_DT 
WHERE (ParentGUID IN (SELECT ID FROM MD_INVOICEPARKINGLIST where INVOICESTATUS<>4 and PONO=N'{0}'  {1} )
OR (PONO=N'' AND ISNULL(INVOICENO,'')='{0}')) AND BALANCESTATUS=N'KB' group by INVOICENO,POITEM_ROWGUID ", _pono, sql_invoice);
            DataTable dt_MatchList = DataAccess.Instance("BizDB").ExecuteDataTable(sql_MatchList);

            //查询付款总金额
            string sql_pay = string.Format(@"select POROWID,sum(isnull(PAYAMOUNT,0)) as PAYAMOUNT from PROC_PAYMENTREQUEST_DT where FORMID in(
SELECT FORMID from PROC_PAYMENTREQUEST where STATUS in (1,2,3) and (INCIDENT>0 or INCIDENT=-99) and PONUMBER=N'{0}' and PAYMENTTYPE!='Down Payment' ) group by POROWID", _pono);
            DataTable dt_pay = DataAccess.Instance("BizDB").ExecuteDataTable(sql_pay);

            //查询欲付款总金额
            string sql_paydo = string.Format(@"select POROWID,sum(isnull(PAYAMOUNT,0)) as PAYAMOUNT from PROC_PAYMENTREQUEST_DT where FORMID in(
SELECT FORMID from PROC_PAYMENTREQUEST where STATUS in (1,2,3) and (INCIDENT>0 or INCIDENT=-99) and PONUMBER=N'{0}' and PAYMENTTYPE='Down Payment' ) group by POROWID", _pono);
            DataTable dt_paydo = DataAccess.Instance("BizDB").ExecuteDataTable(sql_paydo);

            //查询最后一次付款类型
            string sql_pay_paytype = string.Format("SELECT top(1) PAYMENTTYPE from PROC_PAYMENTREQUEST where STATUS in (1,2,3) and (INCIDENT>0 or INCIDENT=-99) and PONUMBER=N'{0}' order by REQUESTDATE desc", _pono);
            string _payType = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_pay_paytype));

            //数据处理
            for (int i = 0; i < dt_ItemList.Rows.Count; i++)
            {
                DataRow dr = dt_check.NewRow();
                //公用字段
                string _item_guid = ConvertUtil.ToString(dt_ItemList.Rows[i]["GUID"]);
                dr["PARENT_GUID"] = ConvertUtil.ToString(dt_ItemList.Rows[i]["PARENT_GUID"]);//PO 唯一号
                dr["GUID"] = _item_guid; ///PO ITEM 唯一号
                dr["POITEM"] = ConvertUtil.ToString(dt_ItemList.Rows[i]["POITEM"]); // NO
                dr["DESCRIPTION"] = ConvertUtil.ToString(dt_ItemList.Rows[i]["DESCRIPTION"]); //描述
                dr["NETAMOUNT"] = ConvertUtil.ToString(dt_ItemList.Rows[i]["NETAMOUNT"]); //总金额
                dr["DATEFROM"] = ConvertUtil.ToString(dt_ItemList.Rows[i]["DATEFROM"]) == "" ? "" : ConvertUtil.ToDateTime(dt_ItemList.Rows[i]["DATEFROM"]).ToString("yyyy/MM/dd");  //开始时间
                dr["DATETO"] = ConvertUtil.ToString(dt_ItemList.Rows[i]["DATETO"]) == "" ? "" : ConvertUtil.ToDateTime(dt_ItemList.Rows[i]["DATETO"]).ToString("yyyy/MM/dd"); //结束时间
                dr["PROCURMENTTYPE"] = _procurMentType;//采购类型
                dr["PAYMENTTYPE"] = _payType;//付款类型

                DataRow[] dr_PAYDO = dt_paydo.Select(" POROWID='" + _item_guid + "' ");
                decimal _downpay_amount = 0.00m;
                if (dr_PAYDO.Length > 0)
                {
                    _downpay_amount = ConvertUtil.ToDecimal(dr_PAYDO[0]["PAYAMOUNT"], 0.00m);
                }
                dr["DOWNPAYMENTAMOUNT"] = _downpay_amount;

                DataRow[] dr_CB = dt_po_cb.Select(string.Format(@" POITEM_ROWGUID='{0}' ", _item_guid));

                decimal _match_cb = 0.00m; //已匹配金额
                dr["BALANCESTATUS"] = "KB";//匹配状态
                bool _is_cb = false;//是否为CB行
                if (dr_CB.Length > 0)
                {
                    _match_cb = ConvertUtil.ToDecimal(dr_CB[0]["PAYMENTAMOUNT"], 0.00m);
                    if (_match_cb > 0)
                    {
                        dr["BALANCESTATUS"] = "CB";
                        _is_cb = true;
                    }
                }
                if (!IS_CB)
                {
                    //string _isactive = ConvertUtil.ToString(dt_ItemList.Rows[i]["ISACTIVE"]);

                    //if (_isactive == "6")
                    //{ //先款后票 不做行排除
                    //    if (_payType != "Invoice After Payment" && _payType != "Down Payment")
                    //    {
                    //        _is_cb = true;
                    //    }
                    //}

                    if (_is_cb)
                    {
                        continue;
                    }
                }


                //公司
                string _profitCenterName = ConvertUtil.ToString(dt_ItemList.Rows[i]["PROFITCENTERNAME"]);   ///利润中心
                string _costCenterName = ConvertUtil.ToString(dt_ItemList.Rows[i]["COSTCENTERNAME"]); ///成本中心
                if (!string.IsNullOrEmpty(_profitCenterName))
                {
                    _profitCenterName = _profitCenterName.Split('-')[0];
                }
                if (!string.IsNullOrEmpty(_costCenterName))
                {
                    _costCenterName = _costCenterName.Split('-')[0];
                }
                string sql_cc = string.Format("SELECT EXT01+'-'+ companycode as LEGALENTITY from V_COM_PC_CC where PROFITCENTERCODE=N'{0}' OR code=N'{1}' ", _profitCenterName, _costCenterName);
                string _legaLentity = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_cc));
                dr["LEGALENTITY"] = _legaLentity;//公司
                //是否预提
                string sql_yuti = string.Format(@"select count(0) from V_POAccruedList where guid  in (select parentguid  from MD_POACCRUEDITEM where status='1') and guid=N'{0}'", _item_guid);
                string _yuti = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_yuti));
                dr["YUTI"] = "1";//是否已预提
                if (_yuti == "0")
                {
                    dr["YUTI"] = "0";
                }
                //获取付款金额
                DataRow[] dr_pay = dt_pay.Select(" POROWID='" + _item_guid + "' ");
                decimal _pay_amount = 0.00m;
                if (dr_pay.Length > 0)
                {
                    _pay_amount = ConvertUtil.ToDecimal(dr_pay[0]["PAYAMOUNT"], 0.00m);
                }
                dr["PAYNETAMOUNT"] = _pay_amount;//已付款金额

                //计算金额
                decimal _total = ConvertUtil.ToDecimal(dt_ItemList.Rows[i]["NETAMOUNT"], 0.00m);//总金额
                decimal _match_kb = 0.00m; //已匹配金额
                decimal _balance = 0.00m;//剩余金额
                if (IS_Invoice)
                {
                    dr["UPAYMENTAMOUNT"] = "0.00";
                }
                else
                {
                    DataRow[] dr_MatchList_Invoice = dt_MatchList.Select(" POITEM_ROWGUID='" + _item_guid + "' and  INVOICENO='" + _InvoiceNo + "'");
                    if (dr_MatchList_Invoice.Length > 0)
                    {
                        dr["UPAYMENTAMOUNT"] = ConvertUtil.ToDecimal(dr_MatchList_Invoice[0]["PAYMENTAMOUNT"], 0.00m);
                    }
                }
                DataRow[] dr_MatchList = dt_MatchList.Select(" POITEM_ROWGUID='" + _item_guid + "' ");
                if (dr_MatchList.Length > 0)
                {
                    for (int k = 0; k < dr_MatchList.Length; k++)
                    {
                        _match_kb += ConvertUtil.ToDecimal(dr_MatchList[k]["PAYMENTAMOUNT"], 0.00m);
                    }
                }
                dr["UNPAYNETAMOUNT"] = _match_kb - _pay_amount < 0 ? 0.00m : _match_kb - _pay_amount;//已匹配 未付款金额
                dr["UNMATCHED"] = _pay_amount - _match_kb - _match_cb < 0 ? 0.00m : _pay_amount - _match_kb - _match_cb;//已付款未匹配金额
                dr["BALANCEAMOUNT"] = _total - _match_kb - _match_cb;//可匹配金额

                dt_check.Rows.Add(dr);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        return dt_check;
    }

    decimal dc;

    public void SaveData(HttpContext context)
    {
        string result = "";
        bool flag = true;
        try
        {
            string PAYMENTAMOUNT = context.Request["paymentamount"];
            string INVOICEPOITEMSTATUS = context.Request["invoicepoitemstatus"];
            string _Json = context.Request["Json"];

            DataSet ds = MyLib.SerializeUtil.Json2DataSet(_Json);
            //定义数据
            string INVOICENO = "";//发票号
            string PONO = "";
            string INVOICEAFTERPAYMENT = "";
            string ParentID = context.Request["ParentID"];
            string InvoiceGuid = "";
            string PYNO = "";
            string PYTYPE = "";
            //主表
            DataTable dt_Main = ds.Tables["Main"];
            StringBuilder sqlMain = new StringBuilder();
            for (int m = 0; m < dt_Main.Rows.Count; m++)
            {
                INVOICENO = RToString(dt_Main.Rows[m]["INVOICENO"]);
                PONO = RToString(dt_Main.Rows[m]["PONO"]);
                INVOICEAFTERPAYMENT = RToString(dt_Main.Rows[m]["INVOICEAFTERPAYMENT"]);
                InvoiceGuid = RToString(dt_Main.Rows[m]["InvoiceGuid"]);
                PYNO = RToString(dt_Main.Rows[m]["PYNO"]);
                PYTYPE = RToString(dt_Main.Rows[m]["PYTYPE"]);
            }

            //查询PO是否已完成
            if (GetPoStatus(PONO) != "1")
            {
                result = "当前PO单据未审批完成，无法进行匹配/The current PO document cannot be matched after it has not been approved.";
                context.Response.Write(result);
                return;
            }

            //判断是否已经做过发票匹配
            string sql_invoiceno = "select INVOICENO from MD_INVOICEPARKINGLIST_DT where INVOICENO=@INVOICENO and PONO=@PONO";
            string _invoiceno = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_invoiceno, INVOICENO, PONO));
            if (!string.IsNullOrEmpty(_invoiceno))
            {
                result = "此发票已经匹配,不能重复匹配!";
                context.Response.Write(result);
                return;
            }

            bool _flag = true;
            //排除IMP单据
            string sql_guid = "SELECT guid FROM V_POMAIN WHERE PROC_DOCUMENTNO=N'" + PONO + "' ";
            string _poguid = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_guid));
            if (!string.IsNullOrEmpty(_poguid))
            {
                if (_poguid.Length > 3)
                {
                    if (_poguid.Substring(0, 3).Contains("IMP"))
                    {
                        _flag = false;
                    }
                }
            }

            StringBuilder sql_IPDel = new StringBuilder();
            StringBuilder sql_IPInsert = new StringBuilder();
            #region 写入MD_INVOICE_PAYMENT数据
            //先删除所有
            sql_IPDel.AppendLine();
            sql_IPDel.AppendFormat("delete from MD_INVOICE_PAYMENT WHERE 1=1 and INVOICENO = N'" + INVOICENO + "';");

            sql_IPInsert.AppendLine();
            //sql_IPInsert.AppendFormat("insert MD_INVOICE_PAYMENT (INVOICE_GUID,INVOICENO,PAYMENTNO) ");
            //sql_IPInsert.AppendFormat("values(N'" + ParentID + "',N'"+INVOICENO+"',N'"++"')");
            #endregion

            StringBuilder sql_MD = new StringBuilder();
            #region 更新MD_INVOICEPARKINGLIST信息
            sql_MD.AppendLine();
            string _INVOICESTATUS = "1";
            if (!string.IsNullOrEmpty(PYNO))
            {
                if (PYTYPE == "Invoice After Payment")
                {
                    _INVOICESTATUS = "5";
                }
            }
            //更新发票状态
            sql_MD.AppendFormat("update MD_INVOICEPARKINGLIST set INVOICESTATUS = N'" + _INVOICESTATUS + "',INVOICEAFTERPAYMENT = N'" + INVOICEAFTERPAYMENT + "',EXT14='" + PYNO + "',EXT15='" + PYTYPE + "'  where INVOICENO = N'" + INVOICENO + "'; ");
            #endregion

            #region 重新验证可用金额
            string sql_where = "";// @"   and guid not in (SELECT POITEM_ROWGUID FROM MD_INVOICEPARKINGLIST_DT WHERE BALANCESTATUS='CB') ";//已CN行不展示
            string sql_where1 = @"   and (guid not in (SELECT guid FROM V_PODETAIL WHERE ISACTIVE=6) or INVOICENO is null) ";//已CN行不展示
            DataTable dt_UseDetail = GetPoDetailInfo(INVOICENO, InvoiceGuid, false, true);
            #endregion

            //明细表
            DataTable dt_Detail = ds.Tables["Detail"];

            //拼接明细语句
            StringBuilder sqlDel = new StringBuilder();
            StringBuilder sqlInsert = new StringBuilder();

            StringBuilder sql_PT = new StringBuilder();
            if (!string.IsNullOrEmpty(PYNO))
            {
                if (PYTYPE == "Invoice After Payment")
                {
                    //更新付款明细发票号
                    sql_PT.AppendLine();
                    //                sql_PT.AppendFormat(string.Format(@"UPDATE PROC_PAYMENTREQUEST_DT SET INVOICENO=N'{0}' 
                    //                    WHERE FORMID IN (SELECT FORMID FROM PROC_PAYMENTREQUEST WHERE DOCUMENTNO in ({1})) AND POITEM=N'{2}' AND POROWID=N'{3}'", INVOICENO, "N'" + PYNO.Replace(",", "',N'") + "'", RToString(dt_Detail.Rows[i]["POItem"]), POITEM_ROWGUID));
                    sql_PT.AppendFormat(string.Format(@"UPDATE PROC_PAYMENTREQUEST_DT SET INVOICENO=ISNULL(INVOICENO,'')+N'{0},' 
                                    WHERE FORMID IN (SELECT FORMID FROM PROC_PAYMENTREQUEST WHERE DOCUMENTNO in ({1}))", INVOICENO, "N'" + PYNO.Replace(",", "',N'") + "'"));
                }
            }
            #region 明细操作
            sqlDel.AppendLine();
            //sqlDel.AppendFormat(string.Format("delete  from MD_INVOICEPARKINGLIST_DT where 1 = 1 and INVOICENO =N'{0}';delete from MD_INVOICEPARKINGLIST_DT where 1=1 and INVOICENO =N'{0}' and BALANCESTATUS='CB'", INVOICENO));//增加删除cb行操作
            //sqlDel.AppendFormat(string.Format("delete  from MD_INVOICEPARKINGLIST_DT where 1 = 1 and INVOICENO =N'{0}';delete from MD_INVOICEPARKINGLIST_DT where 1=1 and PONO =N'{1}' and BALANCESTATUS='CB'", INVOICENO, PONO));//增加删除cb行操作
            bool flag_dp = true;
            //写入数据
            DataTable dt_Balance = GetDataInfo(INVOICENO, "", " ", " ", PONO);
            string sql_Insert = "Values";
            sqlInsert.AppendFormat("insert MD_INVOICEPARKINGLIST_DT  ([ParentGUID],POITEM_ROWGUID,INVOICENO,PONO,POITEM,PAYMENTTYPE,PAYMENTAMOUNT,BALANCESTATUS,GUID,EXT09) ");
            for (int i = 0; i < dt_Detail.Rows.Count; i++)
            {
                string POITEM_ROWGUID = RToString(dt_Detail.Rows[i]["Guid"]);
                string _Action = RToString(dt_Detail.Rows[i]["Action"]);
                string _DESCRIPTION = RToString(dt_Detail.Rows[i]["DESCRIPTION"]).Replace("\"", "\\\"");
                string _BalanceAmount = RToString(dt_Detail.Rows[i]["BalanceAmount"]).Replace(",", "");
                string _UNMATCHED = RToString(dt_Detail.Rows[i]["UNMATCHED"]).Replace(",", "");
                string _PYAmount = RToString(dt_Detail.Rows[i]["PYAMOUNT"]).Replace(",", "");
                string _Amount = RToString(dt_Detail.Rows[i]["PaymentAmount"]).Replace(",", "");
                if (_Action.ToUpper() == "CB")
                {
                    DataRow[] dr1 = dt_Balance.Select(" GUID='" + POITEM_ROWGUID + "' ");
                    if (dr1.Length > 0)
                    {
                        if (_Amount != RToString(dr1[0]["BALANCEAMOUNT"]))
                        {
                            flag = false;
                            result += _DESCRIPTION + "可匹配金额发生变化；";
                            continue;
                        }
                    }
                }

                if (!Decimal.TryParse(_BalanceAmount, out dc))
                    _BalanceAmount = "0";

                DataRow[] dr = dt_UseDetail.Select(" GUID='" + POITEM_ROWGUID + "' ");

                if (dr.Length == 0)
                {
                    flag = false;
                    result += _DESCRIPTION + "可匹配金额发生变化；";
                    continue;
                }
                //查询是否为导入数据
                _flag = true;//取消排除IMP导入单据
                if (_flag)
                {
                    string YUTI = RToString(dr[0]["YUTI"]);//dr[0].BALANCEAMOUNT;
                    if (YUTI == "0")
                    {
                        flag = false;
                        result += _DESCRIPTION + "未进行预提，请先进行预提;";
                        continue;
                    }
                }

                string BALANCEAMOUNT = RToString(dr[0]["BALANCEAMOUNT"]);//dr[0].BALANCEAMOUNT;
                if (flag)
                {
                    if (!Decimal.TryParse(BALANCEAMOUNT, out dc))
                    {
                        BALANCEAMOUNT = "0";
                    }
                    if (Convert.ToDecimal(_BalanceAmount) > Convert.ToDecimal(BALANCEAMOUNT))
                    {
                        flag = false;
                        result += _DESCRIPTION + "可匹配金额发生变化;";
                    }
                }
                if (flag)
                {
                    // Invoice After Payment
                    if (Convert.ToDecimal(_UNMATCHED) != 0 && Convert.ToDecimal(_Amount) != 0)
                    {
                        // 付款金额不能大于已付款未匹配金额
                        if (Convert.ToDecimal(_Amount) > Convert.ToDecimal(_UNMATCHED))
                        {
                            flag = false;
                            result += _DESCRIPTION + "付款金额不能大于已付款未匹配金额;";
                        }
                    }
                }

                if (!flag)
                {
                    continue;
                }
                //sqlDel.AppendLine();
                //sqlDel.AppendFormat(string.Format("delete  from MD_INVOICEPARKINGLIST_DT where 1 = 1 and POITEM_ROWGUID =N'{0}' and INVOICENO =N'{1}';", POITEM_ROWGUID, INVOICENO));

                if (i > 0)
                {
                    sql_Insert += ",";
                }

                string _use_amount = _Amount;
                if (!string.IsNullOrEmpty(_PYAmount))
                {
                    _use_amount = Convert.ToString(Convert.ToDecimal(_Amount) - Convert.ToDecimal(_PYAmount));
                    if (Convert.ToDecimal(_Amount) != Convert.ToDecimal(_PYAmount))
                    {
                        flag_dp = false;
                    }
                }

                sql_Insert += "(N'" + RToString(ParentID) + "'";
                sql_Insert += ",N'" + POITEM_ROWGUID + "'";
                sql_Insert += ",N'" + INVOICENO + "'";
                sql_Insert += ",N'" + PONO + "'";
                sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["POItem"]) + "'";
                sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["PaymentType"]) + "'";
                sql_Insert += ",N'" + _Amount + "'";
                sql_Insert += ",N'KB'";
                //sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["Guid"]) + "'";
                sql_Insert += ",N'" + Guid.NewGuid() + "'";
                sql_Insert += ",N'" + _use_amount + "')";

                //如果为CB则写入CB之后，再次写入KB
                if (_Action.ToUpper() == "CB")
                {
                    if (!Decimal.TryParse(_Amount, out dc))
                    {
                        _Amount = "0";
                    }
                    Decimal _CB_AMOUNT = Convert.ToDecimal(_BalanceAmount) - Convert.ToDecimal(_Amount);
                    sql_Insert += ",(N'" + RToString(ParentID) + "'";
                    sql_Insert += ",N'" + POITEM_ROWGUID + "'";
                    sql_Insert += ",N'" + INVOICENO + "'";
                    sql_Insert += ",N'" + PONO + "'";
                    sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["POItem"]) + "'";
                    sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["PaymentType"]) + "'";
                    sql_Insert += ",N'" + _CB_AMOUNT + "'";
                    sql_Insert += ",N'" + _Action + "'";
                    sql_Insert += ",N'" + Guid.NewGuid() + "'";
                    sql_Insert += ",N'0')";
                    //查询是否已进行过CB，已存在CB则删除CB重新插入
                    string is_CB = string.Format("SELECT COUNT(0) FROM MD_INVOICEPARKINGLIST_DT  WHERE POITEM_ROWGUID='" + POITEM_ROWGUID + "' and BALANCESTATUS='CB' ");
                    int NUM = Convert.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(is_CB));
                    if (NUM > 0)
                    {
                        DataAccess.Instance("BizDB").ExecuteNonQuery("delete from MD_INVOICEPARKINGLIST_DT WHERE POITEM_ROWGUID=@POITEM_ROWGUID and BALANCESTATUS='CB' ", POITEM_ROWGUID);
                    }
                    string sqlTemp = @"INSERT INTO Log_CB (PONumber,InvoiceNo,[Name],Name1,PORowId,Msg,[LoginName])
                                VALUES ('" + PONO + "','" + INVOICENO + "','InvoiceMatch','SaveData 1','"
                                  + POITEM_ROWGUID + "','','" + SessionLogic.GetLoginName() + "')";
                    DataAccess.Instance("BizDB").ExecuteNonQuery(sqlTemp);
                }
                else
                {
                    //本次KB金额与余额相同时，写入CB
                    if (Convert.ToDecimal(_Amount) == Convert.ToDecimal(BALANCEAMOUNT))
                    {
                        sql_Insert += ",(N'" + RToString(ParentID) + "'";
                        sql_Insert += ",N'" + POITEM_ROWGUID + "'";
                        sql_Insert += ",N'" + INVOICENO + "'";
                        sql_Insert += ",N'" + PONO + "'";
                        sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["POItem"]) + "'";
                        sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["PaymentType"]) + "'";
                        sql_Insert += ",N'0.00'";
                        sql_Insert += ",N'CB'";
                        sql_Insert += ",N'" + Guid.NewGuid() + "'";
                        sql_Insert += ",N'0.00')";
                        //查询是否已进行过CB，已存在CB则删除CB重新插入
                        string is_CB = string.Format("SELECT COUNT(0) FROM MD_INVOICEPARKINGLIST_DT  WHERE POITEM_ROWGUID='" + POITEM_ROWGUID + "' and BALANCESTATUS='CB' ");
                        int NUM = Convert.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(is_CB));
                        if (NUM > 0)
                        {
                            DataAccess.Instance("BizDB").ExecuteNonQuery("delete from MD_INVOICEPARKINGLIST_DT WHERE POITEM_ROWGUID=@POITEM_ROWGUID and BALANCESTATUS='CB' ", POITEM_ROWGUID);
                        }

                        string sqlTemp = @"INSERT INTO Log_CB (PONumber,InvoiceNo,[Name],Name1,PORowId,Msg,[LoginName])
                                VALUES ('" + PONO + "','" + INVOICENO + "','InvoiceMatch','SaveData 2','"
                                      + POITEM_ROWGUID + "','','" + SessionLogic.GetLoginName() + "')";
                        DataAccess.Instance("BizDB").ExecuteNonQuery(sqlTemp);
                    }
                }
            }
            #endregion
            sqlInsert.AppendFormat(sql_Insert);

            //if (!string.IsNullOrEmpty(PYNO))
            //{
            //if (flag_dp)
            //{
            //if (PYTYPE == "Down Payment")
            //{
            // _INVOICESTATUS = "5";
            //更新发票状态
            // sql_MD.AppendFormat("update MD_INVOICEPARKINGLIST set INVOICESTATUS = N'" + _INVOICESTATUS + "',INVOICEAFTERPAYMENT = N'" + INVOICEAFTERPAYMENT + "',EXT14='" + PYNO + "',EXT15='" + PYTYPE + "'  where INVOICENO = N'" + INVOICENO + "'; ");
            // }
            //}
            //}

            if (!flag)
            {
                result = "Error：匹配失败，请刷新页面;ErrorMsg:" + result;
            }
            else
            {
                MyLib.DataAccess dac = new MyLib.DataAccess("BizDB");
                DbCommand cmd = dac.CreateCommand();
                //将SQL整合为一个
                StringBuilder sql = new StringBuilder();
                sql.AppendFormat(sql_IPDel.ToString());
                sql.AppendFormat(sql_IPInsert.ToString());
                sql.AppendFormat(sql_PT.ToString());
                sql.AppendFormat(sql_MD.ToString());
                sql.AppendFormat(sqlDel.ToString());
                sql.AppendFormat(sqlInsert.ToString());

                if (sql.Length > 0) //1.2 写入业务库
                {
                    cmd.CommandText = "SET XACT_ABORT ON;begin transaction;" + sql.ToString() + " commit transaction;";
                    MyLib.LogUtil.Info("发票匹配：发票号：" + INVOICENO + "   执行SQL：" + sql.ToString());
                    int count = dac.ExecuteNonQuery(cmd);
                    if (count <= 0)
                    {
                        result = "保存失败，未获取到数据/Error：No data obtained";
                    }
                    else
                    {
                        result = "保存数据成功/Sucess";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result = "操作失败，错误信息/Error：" + ex.Message;
        }
        context.Response.Write(result);
    }

    /// SaveInvoice逻辑与SaveData保持一致
    public void SaveInvoice(HttpContext context)
    {
        string result = "";
        bool flag = true;
        ResultEntity model = new ResultEntity();
        model.status = model.error;
        try
        {
            string PAYMENTAMOUNT = context.Request["paymentamount"];
            string INVOICEPOITEMSTATUS = context.Request["invoicepoitemstatus"];
            string _Json = context.Request["Json"];

            DataSet ds = MyLib.SerializeUtil.Json2DataSet(_Json);
            //定义数据
            string INVOICENO = "";//发票号
            string PONO = "";
            string INVOICEAFTERPAYMENT = "";
            string ParentID = context.Request["ParentID"];
            string InvoiceGuid = "";
            string PYNO = "";
            string PYTYPE = "";
            //主表
            DataTable dt_Main = ds.Tables["Main"];
            StringBuilder sqlMain = new StringBuilder();
            for (int m = 0; m < dt_Main.Rows.Count; m++)
            {
                INVOICENO = RToString(dt_Main.Rows[m]["INVOICENO"]);
                PONO = RToString(dt_Main.Rows[m]["PONO"]);
                INVOICEAFTERPAYMENT = RToString(dt_Main.Rows[m]["INVOICEAFTERPAYMENT"]);
                InvoiceGuid = RToString(dt_Main.Rows[m]["InvoiceGuid"]);
                PYNO = RToString(dt_Main.Rows[m]["PYNO"]);
                PYTYPE = RToString(dt_Main.Rows[m]["PYTYPE"]);
            }

            //查询PO是否已完成
            if (GetPoStatus(PONO) != "1")
            {
                model.msg = "当前PO单据未审批完成，无法进行匹配/The current PO document cannot be matched after it has not been approved.";
                result = MyLib.SerializeUtil.JsonSerialize(model);
                context.Response.Write(result);

                return;
            }

            //判断是否已经做过发票匹配
            string sql_invoiceno = "select INVOICENO from MD_INVOICEPARKINGLIST_DT where INVOICENO=@INVOICENO and PONO=@PONO";
            string _invoiceno = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_invoiceno, INVOICENO, PONO));
            if (!string.IsNullOrEmpty(_invoiceno))
            {
                model.msg = "此发票已经匹配,不能重复匹配!";
                result = MyLib.SerializeUtil.JsonSerialize(model);
                context.Response.Write(result);
                return;
            }

            bool _flag = true;
            //排除IMP单据
            string sql_guid = "SELECT guid FROM V_POMAIN WHERE PROC_DOCUMENTNO=N'" + PONO + "' ";
            string _poguid = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_guid));
            if (!string.IsNullOrEmpty(_poguid))
            {
                if (_poguid.Length > 3)
                {
                    if (_poguid.Substring(0, 3).Contains("IMP"))
                        _flag = false;
                }
            }

            StringBuilder sql_IPDel = new StringBuilder();
            StringBuilder sql_IPInsert = new StringBuilder();
            #region 写入MD_INVOICE_PAYMENT数据
            //先删除所有
            sql_IPDel.AppendLine();
            sql_IPDel.AppendFormat("delete from MD_INVOICE_PAYMENT WHERE 1=1 and INVOICENO = N'" + INVOICENO + "';");
            sql_IPInsert.AppendLine();
            #endregion

            StringBuilder sql_MD = new StringBuilder();
            #region 更新MD_INVOICEPARKINGLIST信息
            string _INVOICESTATUS = "1";
            sql_MD.AppendLine();
            //更新发票状态
            sql_MD.AppendFormat("update MD_INVOICEPARKINGLIST set INVOICESTATUS = N'" + _INVOICESTATUS + "',INVOICEAFTERPAYMENT = N'" + INVOICEAFTERPAYMENT + "',EXT14='" + PYNO + "',EXT15='" + PYTYPE + "'  where INVOICENO = N'" + INVOICENO + "'; ");
            #endregion

            // 重新验证可用金额
            DataTable dt_UseDetail = GetPoDetailInfo(INVOICENO, InvoiceGuid, false, true);
            //明细表
            DataTable dt_Detail = ds.Tables["Detail"];

            //拼接明细语句
            StringBuilder sqlDel = new StringBuilder();
            StringBuilder sqlInsert = new StringBuilder();
            StringBuilder sql_PT = new StringBuilder();
            if (!string.IsNullOrEmpty(PYNO) && PYTYPE == "Invoice After Payment")
            {
                //更新付款明细发票号
                sql_PT.AppendLine();
                sql_PT.AppendFormat(string.Format(@"UPDATE PROC_PAYMENTREQUEST_DT SET INVOICENO=ISNULL(INVOICENO,'')+N'{0},' 
                                    WHERE FORMID IN (SELECT FORMID FROM PROC_PAYMENTREQUEST WHERE DOCUMENTNO in ({1}))", INVOICENO, "N'" + PYNO.Replace(",", "',N'") + "'"));
            }

            #region 明细操作
            //写入数据
            sqlDel.AppendLine();
            string sql_Insert = "Values";
            DataTable dt_Balance = GetDataInfo(INVOICENO, "", " ", " ", PONO);
            sqlInsert.AppendFormat("insert MD_INVOICEPARKINGLIST_DT  ([ParentGUID],POITEM_ROWGUID,INVOICENO,PONO,POITEM,PAYMENTTYPE,PAYMENTAMOUNT,BALANCESTATUS,GUID,EXT09) ");
            for (int i = 0; i < dt_Detail.Rows.Count; i++)
            {
                string POITEM_ROWGUID = RToString(dt_Detail.Rows[i]["Guid"]);
                string _Action = RToString(dt_Detail.Rows[i]["Action"]);
                string _DESCRIPTION = RToString(dt_Detail.Rows[i]["DESCRIPTION"]).Replace("\"", "\\\"");
                string _BalanceAmount = RToString(dt_Detail.Rows[i]["BalanceAmount"]).Replace(",", "");
                string _UNMATCHED = RToString(dt_Detail.Rows[i]["UNMATCHED"]).Replace(",", "");
                string _PYAmount = RToString(dt_Detail.Rows[i]["PYAMOUNT"]).Replace(",", "");
                string _Amount = RToString(dt_Detail.Rows[i]["PaymentAmount"]).Replace(",", "");
                if (_Action.ToUpper() == "CB")
                {
                    DataRow[] dr1 = dt_Balance.Select(" GUID='" + POITEM_ROWGUID + "' ");
                    if (dr1.Length > 0)
                    {
                        if (_Amount != RToString(dr1[0]["BALANCEAMOUNT"]))
                        {
                            flag = false;
                            result += _DESCRIPTION + "可匹配金额发生变化；";
                            continue;
                        }
                    }
                }

                if (!Decimal.TryParse(_BalanceAmount, out dc))
                    _BalanceAmount = "0";

                DataRow[] dr = dt_UseDetail.Select(" GUID='" + POITEM_ROWGUID + "' ");
                if (dr.Length == 0)
                {
                    flag = false;
                    model.msg += _DESCRIPTION + "可匹配金额发生变化；";
                    continue;
                }

                //查询是否为导入数据
                _flag = true;//取消排除IMP导入单据
                if (_flag)
                {
                    string YUTI = RToString(dr[0]["YUTI"]);//dr[0].BALANCEAMOUNT;
                    if (YUTI == "0")
                    {
                        flag = false;
                        model.msg += _DESCRIPTION + "未进行预提，请先进行预提;";
                        continue;
                    }
                }

                string BALANCEAMOUNT = RToString(dr[0]["BALANCEAMOUNT"]);//dr[0].BALANCEAMOUNT;
                if (flag)
                {
                    if (!Decimal.TryParse(BALANCEAMOUNT, out dc))
                        BALANCEAMOUNT = "0";
                    if (Convert.ToDecimal(_BalanceAmount) > Convert.ToDecimal(BALANCEAMOUNT))
                    {
                        flag = false;
                        model.msg += _DESCRIPTION + "可匹配金额发生变化;";
                    }
                }
                if (flag)
                {
                    // Invoice After Payment
                    if (Convert.ToDecimal(_UNMATCHED) != 0 && Convert.ToDecimal(_Amount) != 0)
                    {
                        // 付款金额不能大于已付款未匹配金额
                        if (Convert.ToDecimal(_Amount) > Convert.ToDecimal(_UNMATCHED))
                        {
                            flag = false;
                            model.msg += _DESCRIPTION + "付款金额不能大于已付款未匹配金额;";
                        }
                    }
                }

                if (!flag)
                    continue;
                if (i > 0)
                    sql_Insert += ",";

                string _use_amount = _Amount;
                if (!string.IsNullOrEmpty(_PYAmount))
                    _use_amount = Convert.ToString(Convert.ToDecimal(_Amount) - Convert.ToDecimal(_PYAmount));

                sql_Insert += "(N'" + RToString(ParentID) + "'";
                sql_Insert += ",N'" + POITEM_ROWGUID + "'";
                sql_Insert += ",N'" + INVOICENO + "'";
                sql_Insert += ",N'" + PONO + "'";
                sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["POItem"]) + "'";
                sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["PaymentType"]) + "'";
                sql_Insert += ",N'" + _Amount + "'";
                sql_Insert += ",N'KB'";
                sql_Insert += ",N'" + Guid.NewGuid() + "'";
                sql_Insert += ",N'" + _use_amount + "')";

                //如果为CB则写入CB之后，再次写入KB
                if (_Action.ToUpper() == "CB")
                {
                    if (!Decimal.TryParse(_Amount, out dc))
                        _Amount = "0";
                    Decimal _CB_AMOUNT = Convert.ToDecimal(_BalanceAmount) - Convert.ToDecimal(_Amount);
                    sql_Insert += ",(N'" + RToString(ParentID) + "'";
                    sql_Insert += ",N'" + POITEM_ROWGUID + "'";
                    sql_Insert += ",N'" + INVOICENO + "'";
                    sql_Insert += ",N'" + PONO + "'";
                    sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["POItem"]) + "'";
                    sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["PaymentType"]) + "'";
                    sql_Insert += ",N'" + _CB_AMOUNT + "'";
                    sql_Insert += ",N'" + _Action + "'";
                    sql_Insert += ",N'" + Guid.NewGuid() + "'";
                    sql_Insert += ",N'0')";
                    //查询是否已进行过CB，已存在CB则删除CB重新插入
                    string is_CB = string.Format("SELECT COUNT(0) FROM MD_INVOICEPARKINGLIST_DT  WHERE POITEM_ROWGUID='" + POITEM_ROWGUID + "' and BALANCESTATUS='CB' ");
                    int NUM = Convert.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(is_CB));
                    if (NUM > 0)
                    {
                        DataAccess.Instance("BizDB").ExecuteNonQuery("delete from MD_INVOICEPARKINGLIST_DT WHERE POITEM_ROWGUID=@POITEM_ROWGUID and BALANCESTATUS='CB' ", POITEM_ROWGUID);
                    }
                    string sqlTemp = @"INSERT INTO Log_CB (PONumber,InvoiceNo,[Name],Name1,PORowId,Msg,[LoginName])
                                VALUES ('" + PONO + "','" + INVOICENO + "','InvoiceMatch','SaveInvoice 1','"
                                  + POITEM_ROWGUID + "','','" + SessionLogic.GetLoginName() + "')";
                    DataAccess.Instance("BizDB").ExecuteNonQuery(sqlTemp);
                }
                else
                {
                    //本次KB金额与余额相同时，写入CB
                    if (Convert.ToDecimal(_Amount) == Convert.ToDecimal(BALANCEAMOUNT))
                    {
                        sql_Insert += ",(N'" + RToString(ParentID) + "'";
                        sql_Insert += ",N'" + POITEM_ROWGUID + "'";
                        sql_Insert += ",N'" + INVOICENO + "'";
                        sql_Insert += ",N'" + PONO + "'";
                        sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["POItem"]) + "'";
                        sql_Insert += ",N'" + RToString(dt_Detail.Rows[i]["PaymentType"]) + "'";
                        sql_Insert += ",N'0.00'";
                        sql_Insert += ",N'CB'";
                        sql_Insert += ",N'" + Guid.NewGuid() + "'";
                        sql_Insert += ",N'0.00')";
                        //查询是否已进行过CB，已存在CB则删除CB重新插入
                        string is_CB = string.Format("SELECT COUNT(0) FROM MD_INVOICEPARKINGLIST_DT  WHERE POITEM_ROWGUID='" + POITEM_ROWGUID + "' and BALANCESTATUS='CB' ");
                        int NUM = Convert.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(is_CB));
                        if (NUM > 0)
                        {
                            DataAccess.Instance("BizDB").ExecuteNonQuery("delete from MD_INVOICEPARKINGLIST_DT WHERE POITEM_ROWGUID=@POITEM_ROWGUID and BALANCESTATUS='CB' ", POITEM_ROWGUID);
                        }
                        string sqlTemp = @"INSERT INTO Log_CB (PONumber,InvoiceNo,[Name],Name1,PORowId,Msg,[LoginName])
                                VALUES ('" + PONO + "','" + INVOICENO + "','InvoiceMatch','SaveInvoice 2','"
                                      + POITEM_ROWGUID + "','','" + SessionLogic.GetLoginName() + "')";
                        DataAccess.Instance("BizDB").ExecuteNonQuery(sqlTemp);
                    }
                }
            }
            #endregion
            sqlInsert.AppendFormat(sql_Insert);

            if (!flag)
            {
                model.msg = "Error：匹配失败，请刷新页面;ErrorMsg:" + model.msg;
                result = MyLib.SerializeUtil.JsonSerialize(model);
            }
            else
            {
                MyLib.DataAccess dac = new MyLib.DataAccess("BizDB");
                DbCommand cmd = dac.CreateCommand();
                //将SQL整合为一个
                StringBuilder sql = new StringBuilder();
                sql.AppendFormat(sql_IPDel.ToString());
                sql.AppendFormat(sql_IPInsert.ToString());
                sql.AppendFormat(sql_PT.ToString());
                sql.AppendFormat(sql_MD.ToString());
                sql.AppendFormat(sqlDel.ToString());
                sql.AppendFormat(sqlInsert.ToString());

                if (sql.Length > 0) //1.2 写入业务库
                {
                    cmd.CommandText = "SET XACT_ABORT ON;begin transaction;" + sql.ToString() + " commit transaction;";
                    MyLib.LogUtil.Info("发票匹配：发票号：" + INVOICENO + "   执行SQL：" + sql.ToString());
                    int count = dac.ExecuteNonQuery(cmd);
                    if (count <= 0)
                    {
                        model.msg = "保存失败，未获取到数据/Error：No data obtained";
                        result = MyLib.SerializeUtil.JsonSerialize(model);
                    }
                    else
                    {
                        model.status = model.success;
                        model.msg = "保存数据成功/Sucess";
                        result = MyLib.SerializeUtil.JsonSerialize(model);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            model.msg = "操作失败，错误信息/Error：" + ex.Message;
            result = MyLib.SerializeUtil.JsonSerialize(model);
        }
        context.Response.Write(result);
    }

    //字符类型转换
    public string RToString(object obj)
    {
        string _Value = "";
        try
        {
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(obj.ToString()))
                {
                    _Value = Convert.ToString(obj);
                }
            }
        }
        catch (Exception)
        {
            _Value = "";
        }
        return _Value;
    }

    public void ToPR(HttpContext context)
    {
        try
        {
            DataTable dt = new DataTable();
            string JosnString = string.Empty;
            context.Session["InvoiceNo"] = context.Request["InvoiceNo"];
            context.Session["PaymentType"] = context.Request["PaymentType"];
            context.Session["PoGuid"] = context.Request["PoGuid"];
            IWorkflow _workflow = ServiceContainer.Instance().GetService<IWorkflow>();
            string _userAccount = string.Empty;
            //_userAccount = SessionLogic.GetUltimusLoginName();
            _userAccount = SessionLogic.GetLoginName();
            List<TaskEntity> _initProcessList = new List<TaskEntity>();
            _initProcessList = _workflow.GetInitTaskList(_userAccount, "", null, "", 0, 1000);
            string taskid = string.Empty;
            string servername = string.Empty;
            for (int i = 0; i < _initProcessList.Count; i++)
            {
                if (_initProcessList[i].PROCESSNAME.Equals("Payment Request"))
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
            if (dt != null && dt.Rows.Count > 0)
            {
                JosnString = MyLib.SerializeUtil.JsonSerialize(dt);
                //context.Response.Write(JosnString);
                context.Response.Write(JosnString);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public void GetPoStatus(HttpContext context)
    {
        string result = "";
        try
        {
            string _PO = context.Request["PONO"];
            if (GetPoStatus(_PO) != "1")
            {
                result = "当前PO单据未审批完后才能，无法进行匹配/The current PO document cannot be matched after it has not been approved.";
            }
            //result = "当前PO单据未审批完后才能，无法进行匹配/The current PO document cannot be matched after it has not been approved.";
        }
        catch (Exception ex)
        {
            result = ex.Message;
        }
        context.Response.Write(result);
    }

    public string GetPoStatus(string PONO)
    {
        try
        {
            string sql_po_status = "SELECT ISACTIVE FROM V_POMAIN WHERE PROC_DOCUMENTNO=N'" + PONO + "' ";
            return ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(sql_po_status));
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public void GetPO(HttpContext context)
    {
        string result = "";
        try
        {
            string _PO = context.Request["PONO"];
            string sql = "SELECT * FROM PROC_PURCHASEORDER where DOCUMENTNO=N'" + _PO + "'";
            DataTable dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
            if (dt != null)
            {
                result = MyLib.SerializeUtil.JsonSerialize(dt);

            }
        }
        catch (Exception ex)
        {

        }
        context.Response.Write(result);
    }

    public void GetCB(HttpContext context)
    {
        string result = "";
        try
        {
            string poAccrueditemStatus = context.Request["poAccrueditemStatus"];
            string _PO = context.Request["PONO"];
            string invoiceNo = context.Request["invoiceNo"];
            string guid = context.Request["guid"];// PO GUID
            string Itemguid = context.Request["Itemguid"];
            string POITEM = context.Request["POITEM"];
            //string PaymentType = context.Request["PaymentType"];
            //查询发票是否已匹配
            //string InvoiceStatus = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(@"
            //select INVOICESTATUS from MD_INVOICEPARKINGLIST where invoiceNo='" + invoiceNo + "' "));
            //if (InvoiceStatus == "0" || InvoiceStatus == "4")
            //{
            //    if (Lang.GetLang() == "zh-CN")
            //    {
            //        throw new Exception("请您先进行发票匹配.");
            //    }
            //    else
            //    {
            //        throw new Exception("Please make an invoice match first.");
            //    }
            //}

            //查询是否已进行过CB，已存在CB则不能再次CB
            string is_CB = string.Format("SELECT COUNT(0) FROM MD_INVOICEPARKINGLIST_DT  WHERE POITEM_ROWGUID='" + Itemguid + "' and BALANCESTATUS='CB' ");
            int NUM = Convert.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(is_CB));
            if (NUM > 0)
            {
                if (Lang.GetLang() == "zh-CN")
                {
                    throw new Exception("当前行已经Cleaning Balance，无法再次Cleaning Balance.");
                }
                else
                {
                    throw new Exception("The current line is already Cleaning Balance and cannot be used again.");
                }
            }

            //判断发票全部匹配及付款了。
            string sql = @"select e.INVOICENO,d.POITEM_ROWGUID,d.POITEM   from MD_INVOICEPARKINGLIST c 
            left join  MD_INVOICEPARKINGLIST_DT d on  c.ID =d.ParentGUID and d.BALANCESTATUS='KB'
            left join (
            select b.* from  PROC_PAYMENTREQUEST_DT b 
            left join  PROC_PAYMENTREQUEST a  on a.FORMID=b.FORMID 
            where a.STATUS in (1,2,3)  
            ) e
            on c.INVOICENO=e.INVOICENO and e.POROWID=d.POITEM_ROWGUID
            where  c.PONO=@PONO and d.POITEM_ROWGUID=@POITEM_ROWGUID and e.INVOICENO is null";
            DataTable dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, _PO, Itemguid);
            if (dt != null && dt.Rows.Count > 0)
            {
                if (Lang.GetLang() == "zh-CN")
                {
                    throw new Exception("请您先进行发票匹配及付款.");
                }
                else
                {
                    throw new Exception("Please make an invoice match and payment first.");
                }
            }

            //判断是否存在付款进行中的单子，是不能进行CB的。
            sql = @"select a.FORMID from PROC_PAYMENTREQUEST a inner join PROC_PAYMENTREQUEST_DT b on a.FORMID=b.FORMID
            where a.STATUS in (1,3)  and PONUMBER=@PONUMBER and b.POROWID=@POROWID";
            dt = DataAccess.Instance("BizDB").ExecuteDataTable(sql, _PO, Itemguid);
            if (dt != null && dt.Rows.Count > 0)
            {
                if (Lang.GetLang() == "zh-CN")
                {
                    throw new Exception("PO行还有付款没有审批完成,不能Cleaning Balance.");
                }
                else
                {
                    throw new Exception("here is still payment in this PO line item that has not been approved,Not Cleaning Balance .");
                }
            }

            //根据发票号，获取发票ID
            string Invoiceguid = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar("select id from MD_INVOICEPARKINGLIST where invoiceNo='" + invoiceNo + "' "));
            DataTable dt_UseDetail = GetDataInfo(invoiceNo, guid, " ", " ", "");

            DataRow[] dr = dt_UseDetail.Select(" GUID='" + Itemguid + "' ");
            if (dr.Length > 0)
            {
                string BALANCEAMOUNT = RToString(dr[0]["BALANCEAMOUNT"]);//dr[0].BALANCEAMOUNT;
                StringBuilder sqlInsert = new StringBuilder();
                sqlInsert.AppendFormat("insert MD_INVOICEPARKINGLIST_DT  ([ParentGUID],POITEM_ROWGUID,INVOICENO,PONO,POITEM,PAYMENTAMOUNT,BALANCESTATUS,GUID) ");

                sqlInsert.AppendFormat(" VALUES (N'" + Invoiceguid + "'");
                sqlInsert.AppendFormat(",N'" + Itemguid + "'");
                sqlInsert.AppendFormat(",N'" + invoiceNo + "'");
                sqlInsert.AppendFormat(",N'" + _PO + "'");
                sqlInsert.AppendFormat(",N'" + POITEM + "'");
                //sqlInsert.AppendFormat(",N'" + PaymentType + "'");
                sqlInsert.AppendFormat(",N'" + BALANCEAMOUNT + "'");
                sqlInsert.AppendFormat(",N'CB'");
                sqlInsert.AppendFormat(",N'" + Guid.NewGuid() + "')");

                StringBuilder sqlCb = new StringBuilder();
                //由于明细位于不同的表，因此更新所有中间表
                sqlCb.AppendFormat(" update MD_PURCHASEORDERITEM set ISACTIVE='6' where guid='" + Itemguid + "' ; ");
                sqlCb.AppendFormat(" update MD_DETAILPO set ISACTIVE='6' where GUID='" + Itemguid + "' ; ");
                sqlCb.AppendFormat(" update MD_DETAILBATCHPURCHASE set ISACTIVE='6' where GUID='" + Itemguid + "'; ");

                int num = DataAccess.Instance("BizDB").ExecuteNonQuery(sqlInsert.ToString() + sqlCb.ToString());
                if (num > 0)
                {
                    //result = "Cleaning Balance Sucess";
                    if (poAccrueditemStatus == "1")
                    {
                        // 成功后，添加判断，新增一行预提状态为-1的数据：付义 2020/04/17
                        // 解决如下问题：优化clean balance：对9月账时，发现9月进行的clean balance操作，部分删除金额未在账面中体现。
                        //              （clean balance时，行项目不做payment，不填写金额，不填写0，无法反冲）
                        string sal = @"INSERT INTO MD_POACCRUEDITEM
                                    (GUID,PARENTGUID,PODOCUMENTNO,COSTCENTER,COSTCENTERNAME,PROFITNAME,PROFIT,DESCRIPTION,AMTNOVATA
                                    ,DATEFROM,DATETO,CURRENTMONTH,CREATEBY,CREATEBYNAME,CREATEDATE,STATUS,EXT01,EXT02,EXT03,EXT04
                                    ,EXT05,EXT06,EXT07,EXT08,EXT09,EXT10,SAPFILENAME,SAPFILENAMEOFFSET,SAPMCategoryCode,SAPCurrency
                                    ,SAPAccountCode,SAPGRAccount,SAPPostingDate)
                                    SELECT newid(),PARENTGUID,PODOCUMENTNO,COSTCENTER,COSTCENTERNAME,PROFITNAME,PROFIT,DESCRIPTION,AMTNOVATA
                                    ,DATEFROM,DATETO,CURRENTMONTH,CREATEBY,CREATEBYNAME,CREATEDATE,-1,EXT01,EXT02,EXT03,EXT04
                                    ,EXT05,EXT06,EXT07,EXT08,EXT09,EXT10,SAPFILENAME,SAPFILENAMEOFFSET,SAPMCategoryCode,SAPCurrency
                                    ,SAPAccountCode,SAPGRAccount,SAPPostingDate
                                    FROM MD_POACCRUEDITEM where PARENTGUID=@Itemguid and  STATUS=1";
                        int i = DataAccess.Instance("BizDB").ExecuteNonQuery(sal, Itemguid);
                    }
                    sql = @"INSERT INTO Log_CB (PONumber,InvoiceNo,[Name],Name1,PORowId,Msg,[LoginName])
                                VALUES ('" + _PO + "','" + invoiceNo + "','InvoiceMatch','GetCB','"
                                + Itemguid + "','" + poAccrueditemStatus + "','" + SessionLogic.GetLoginName() + "')";
                    DataAccess.Instance("BizDB").ExecuteNonQuery(sql);
                }
                else
                {
                    result = "Error";
                }
            }
            else
            {
                if (Lang.GetLang() == "zh-CN")
                {
                    result = "失败:余额不足，无法Cleaning Balance.";
                }
                else
                {
                    result = "Error:Insuffcient balance available,unable to operate.";
                }
            }
        }
        catch (Exception ex)
        {
            if (Lang.GetLang() == "zh-CN")
            {
                result = "失败:" + ex.Message;
            }
            else
            {
                result = "Error:" + ex.Message;
            }
        }
        context.Response.Write(result);
    }

    //获取付款明细金额总计
    public void GetPaymentDetail(HttpContext context)
    {
        string data = "";
        string errormsg = "获取Payemnt Request数据失败！/Failed to get [Payment Request] data";
        try
        {
            string _py = context.Request["PYNO"];
            // string sql = string.Format("SELECT * FROM PROC_PAYMENTREQUEST_DT WHERE FORMID IN (SELECT FORMID FROM PROC_PAYMENTREQUEST WHERE DOCUMENTNO=N'{0}')", _py);
            string sql = string.Format(@"SELECT POITEM,POROWID,SUM(ISNULL(PAYAMOUNT,0.00)) as NETPAYMENTAMOUNT FROM PROC_PAYMENTREQUEST_DT
WHERE FORMID IN (SELECT FORMID FROM PROC_PAYMENTREQUEST WHERE DOCUMENTNO in ({0}) AND STATUS=2) GROUP BY  POITEM,POROWID", "N'" + _py.Replace(",", "',N'") + "'");

            DataTable dt_py = DataAccess.Instance("BizDB").ExecuteDataTable(sql);
            if (dt_py != null && dt_py.Rows.Count > 0)
            {
                data = MyLib.SerializeUtil.JsonSerialize(dt_py);
                errormsg = "";
            }
        }
        catch (Exception ex)
        {
            errormsg = "Error:" + ex.Message;
        }
        string result = "{\"data\":" + data + ",\"error\":\"" + errormsg + "\"}";
        context.Response.Write(result);
    }

    //查询PO名下行
    public void SearchPaymentStatus(HttpContext context)
    {
        string RowGuid = context.Request["RowGuid"];
        string result = string.Empty;
        string sql = @"select count(*) from PROC_PAYMENTREQUEST a left join PROC_PAYMENTREQUEST_DT b
on a.FORMID=b.FORMID where a.STATUS in(1,3) and b.POROWID=@ROWGUID ";
        int i = Convert.ToInt32(DataAccess.Instance("BizDB").ExecuteScalar(sql, RowGuid));
        if (i > 0)
        {
            result = "Exist";
        }
        else
        {
            result = "NoExist";
        }
        context.Response.Write(result);
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}
