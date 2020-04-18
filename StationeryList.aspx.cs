using MyLib;
using MyLib.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Ultimus.UWF.Common.Logic;
using UPL.Common.BussinessControl;
using Ultimus.UWF.Workflow.Logic;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Net;


namespace UPL.Common.MasterDataReport
{
    public partial class StationeryList : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Ultimus.UWF.Form.WebControls.Repeater rpt = Page.FindControl("Repeater1") as Ultimus.UWF.Form.WebControls.Repeater;
                rpt.Source = @"BizDB.select *,(CASE STATUS WHEN '1' THEN 'Enable' WHEN '0' THEN 'Disable' ELSE '其他' END) as ISACTIVE from MD_STATIONERYLIST where STATUS=1 ";
                rpt.DataBind();
            }
        }

        public void Button2_Click(object sender, EventArgs e)
        {
            //String url = WebUtil.GetRootPath() + "/Solution/UWF.Process.PurchaseOrder/E-Catalog/CategoryAttributes.aspx?method=";
            //Page.ClientScript.RegisterStartupScript(this.GetType(), "", "<script> window.open('" + url + "')</script>");
        }


        #region 导入Excel
        /// <summary>
        /// Excel Import
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btn_Save_Click(object sender, EventArgs e)
        {
            FileUpload fupExcel = Page.FindControl("fupExcel") as FileUpload;
            if (!fupExcel.HasFile)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "res", "alert('未选择Excel或文件格式不正确!');", true);
                return;
            }

            try
            {

                StringBuilder importInfo = new StringBuilder();
                StringBuilder importError = new StringBuilder();
                int importRow = 0;
                int errorRow = 0;
                importInfo.AppendLine("------------- 导入数据开始 -------------");
                //读取Excel中内容
                Stream ExcelFileStream = fupExcel.FileContent;
                DataTable dt = ExcelHelper.RenderDataTableFromExcel_X(ExcelFileStream, 0, 0);
                //导入数据
                Import(dt, out importRow, out errorRow, out importError);
                importInfo.AppendLine("导入成功行:【" + importRow.ToString() + "】-----");
                importInfo.Append("导入失败行:【" + errorRow.ToString() + "】注：失败关键字搜索【FailInfo】【Error】");
                importInfo.AppendLine(" ");
                importInfo.AppendLine("------------- 如下是导入失败详细信息 -------------");
                importInfo.AppendLine(" ");
                importInfo.AppendLine(importError.ToString());
                importInfo.AppendLine("------------- 导入数据结束 -------------");
                //抛出导入结果
                if (!string.IsNullOrEmpty(importInfo.ToString()))
                {
                    //获取登录人
                    string loginname = ConvertUtil.ToString(SessionLogic.GetLoginName()).Replace("CustomOC\\", "");
                    string RootPath = MyLib.ConfigurationManager.AppSettings["RootPhysicalPath"].ToString(); //网址的根路径。用于页面文件引用及邮件提醒。
                    string fileName = loginname + "-" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".txt"; //定义文件名称
                    string path = "/File/ImportFile/StationeryList/"; //定义文件存储路径
                    if (!Directory.Exists(RootPath + path))
                    {
                        Directory.CreateDirectory(RootPath + path);
                    }
                    System.IO.File.WriteAllText(Server.MapPath(path + fileName), importInfo.ToString(), Encoding.UTF8);
                    ClientScript.RegisterStartupScript(this.GetType(), "res", "window.open(\"" + path + fileName + "\");", true);
                }
            }
            catch (Exception ex)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "res", "alert('导入失败,错误信息：" + ex.Message + "');", true);
            }
        }

        /// <summary>
        /// 导入数据库
        /// </summary>
        /// <param name="dt">Excel 内容</param>
        /// <param name="importRow">成功行</param>
        /// <param name="errorRow">失败行</param>
        /// <param name="importInfo">导入信息</param>
        public void Import(DataTable dt, out int importRow, out int errorRow, out StringBuilder importError)
        {
            string _sql = string.Empty;
            importError = new StringBuilder();
            importRow = 0;
            errorRow = 0;
            //获取登录人
            string loginname = ConvertUtil.ToString(SessionLogic.GetLoginName()).Replace("CustomOC\\", "");
            //供应商数据
            _sql = "select * from com_resource where type = 'VendorType' and ISACTIVE=1 ";
            DataTable dtVendor = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
            foreach (DataRow dr in dt.Rows)
            {
                string errorMessage = string.Empty;
                //获取Excel中行号
                string index = ConvertUtil.ToString(dt.Rows.IndexOf(dr) + 1);

                #region 验证行所有数据是否满足,不满足记录日志,结束当前行执行
                try
                {
                    //验证物料号
                    if (string.IsNullOrEmpty(ConvertUtil.ToString(dr["Item No"])))
                    {
                        errorMessage = "FailInfo:---Excel中第【" + index + "】行【物料号:" + ConvertUtil.ToString(dr["Item No"]) + "】不能为空值!";
                        importError.AppendLine(errorMessage);
                    }
                    //验证文具类型
                    if (string.IsNullOrEmpty(ConvertUtil.ToString(dr["Stationery Type"])))
                    {
                        errorMessage = "FailInfo:---Excel中第【" + index + "】行【物料号:" + ConvertUtil.ToString(dr["Stationery Type"]) + "】不能为空值!";
                        importError.AppendLine(errorMessage);
                    }
                    //验证供应商
                    string VendorCode = GetResource(dtVendor, "CODE", ConvertUtil.ToString(dr["Vendor Code"]), "CODE");
                    if (string.IsNullOrEmpty(VendorCode))
                    {
                        errorMessage = "FailInfo:---Excel中第【" + index + "】行【供应商:" + ConvertUtil.ToString(dr["Vendor Code"]) + "】验证失败!";
                        importError.AppendLine(errorMessage);
                    }

                    //errorMessage 不为空则此行验证导入失败
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        errorRow++;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    errorRow++;
                    importError.AppendLine("Error:---Excel中第【" + index + "】行验证行数据抛错:" + ex.Message);
                    continue;
                }
                #endregion

                #region 插入数据是否成功,不成功记录日志,结束当前行执行
                try
                {
                    List<MappingField> fields = new List<MappingField>();
                    int ID = DataAccess.Instance("BizDB").GetMaxNo("MD_STATIONERYLIST", "ID");
                    fields.Add(new MappingField("ID", DbType.String, ID, false, true));
                    fields.Add(new MappingField("ITEMTYPE", DbType.String, ConvertUtil.ToString(dr["Stationery Type"])));
                    fields.Add(new MappingField("ITEMNO", DbType.String, ConvertUtil.ToString(dr["Item No"])));
                    fields.Add(new MappingField("DESCRIPTION", DbType.String, ConvertUtil.ToString(dr["Description"])));
                    fields.Add(new MappingField("UNIT", DbType.String, ConvertUtil.ToString(dr["Unit"])));
                    fields.Add(new MappingField("UNITPRICE", DbType.String, ConvertUtil.ToString(dr["Unit Price"])));
                    fields.Add(new MappingField("VENDORCODE", DbType.String, ConvertUtil.ToString(dr["Vendor Code"])));
                    fields.Add(new MappingField("VENDORNAME", DbType.String, GetResource(dtVendor, "CODE", ConvertUtil.ToString(dr["Vendor Code"]), "NAME")));
                    string Status = ConvertUtil.ToString(dr["Status"]) == "启用" ? "1" : "0";
                    fields.Add(new MappingField("STATUS", DbType.String, Status));
                    fields.Add(new MappingField("IMPORTDATE", DbType.DateTime, DateTime.Now));
                    fields.Add(new MappingField("TheOperator", DbType.String, loginname));
                    List<string> pks = new List<string>();
                    pks.Add("ITEMNO");
                    DataAccess.Instance("BizDB").SaveEntity("MD_STATIONERYLIST", pks, null, fields);
                    importRow++;
                }
                catch (Exception ex)
                {
                    errorRow++;
                    importError.AppendLine("Error:---Excel中第【" + index + "】行插入行数据抛错:" + ex.Message);
                }
                #endregion

            }
        }

        /// <summary>
        /// 获取数据源结果
        /// </summary>
        /// <param name="dt">数据源</param>
        /// <param name="key">查询字段</param>
        /// <param name="value">查询字段对应值</param>
        /// <param name="returnkey">返回字段</param>
        /// <returns></returns>
        public string GetResource(DataTable dt, string key, string value, string returnkey)
        {
            try
            {
                DataRow[] rows = dt.Select("" + key + "='" + value + "'");
                if (rows.Length > 0)
                {
                    return ConvertUtil.ToString(rows[0]["" + returnkey + ""]);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
                return "";
            }
            return "";
        }
        #endregion


        #region Excel导出


        /// <summary>
        /// Export
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btn_Export_Click(object sender, EventArgs e)
        {
            string FileName = "ECatalog" + DateTime.Now.ToString("yyyyMMddHHmmssfff").Substring(8, 6) + ".xlsx"; // 文件名称
            string urlPath = "/Solution/UWF.Process.PurchaseOrder/E-Catalog/Excel/" + FileName; // 文件下载的URL地址，供给前台下载
            string filePath = HttpContext.Current.Server.MapPath(urlPath); // 文件路径
            DataTable dt = new DataTable();
            dt.TableName = "ECatalog";
            dt.Columns.Add("Category I");
            dt.Columns.Add("Category II");
            dt.Columns.Add("Item Name");
            dt.Columns.Add("Item Number");
            dt.Columns.Add("Description");
            dt.Columns.Add("Unit Price");
            dt.Columns.Add("Qty");
            dt.Columns.Add("Currency");
            dt.Columns.Add("Tax");
            dt.Columns.Add("Vendor");
            dt.Columns.Add("Period");
            dt.Columns.Add("Brand");
            dt.Columns.Add("Central Procurement");
            dt.Columns.Add("Status");
            dt.Columns.Add("TheOperator");
            Repeater rpt = Page.FindControl("Repeater1") as Repeater;
            foreach (RepeaterItem item in rpt.Items)
            {
                Label lbl_CategoryI = item.FindControl("lbl_CategoryI") as Label;
                Label lbl_CategoryII = item.FindControl("lbl_CategoryII") as Label;
                Label lbl_ITEMNAME = item.FindControl("lbl_ITEMNAME") as Label;
                Label lbl_ITEMNO = item.FindControl("lbl_ITEMNO") as Label;
                Label lbl_DESCRIPTION = item.FindControl("lbl_DESCRIPTION") as Label;
                Label lbl_UNITPRICE = item.FindControl("lbl_UNITPRICE") as Label;
                Label lbl_QTY = item.FindControl("lbl_QTY") as Label;
                Label lbl_CURRENCY = item.FindControl("lbl_CURRENCY") as Label;
                Label lbl_TAX = item.FindControl("lbl_TAX") as Label;
                Label lbl_VENDORNAME = item.FindControl("lbl_VENDORNAME") as Label;
                Label lbl_PeriodBegin = item.FindControl("lbl_PeriodBegin") as Label;
                Label lbl_PeriodEnd = item.FindControl("lbl_PeriodEnd") as Label;
                Label lbl_Brand = item.FindControl("lbl_Brand") as Label;
                Label lbl_CENTRALPROCUREMENT = item.FindControl("lbl_CENTRALPROCUREMENT") as Label;
                Label lbl_STATUS = item.FindControl("lbl_STATUS") as Label;
                Label lbl_TheOperator = item.FindControl("lbl_TheOperator") as Label;
                //创建数据行
                DataRow row = dt.NewRow();
                row["Category I"] = lbl_CategoryI.Text;
                row["Category II"] = lbl_CategoryII.Text;
                row["Item Name"] = lbl_ITEMNAME.Text;
                row["Item Number"] = lbl_ITEMNO.Text;
                row["Description"] = lbl_DESCRIPTION.Text;
                row["Unit Price"] = lbl_UNITPRICE.Text;
                row["Qty"] = lbl_QTY.Text;
                row["Currency"] = lbl_CURRENCY.Text;
                row["Tax"] = lbl_TAX.Text;
                row["Vendor"] = lbl_VENDORNAME.Text;
                row["Period"] = lbl_PeriodBegin.Text + "-" + lbl_PeriodEnd.Text;
                row["Brand"] = lbl_Brand.Text;
                row["Central Procurement"] = lbl_CENTRALPROCUREMENT.Text;
                row["Status"] = lbl_STATUS.Text;
                row["TheOperator"] = lbl_TheOperator.Text;
                dt.Rows.Add(row);
            }
            TableToExcel(dt, filePath, FileName);
        }

        /// <summary>
        /// Datable导出成Excel
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="file">导出路径(包括文件名与扩展名)</param>
        public static void TableToExcel(DataTable dt, string file, string FileName)
        {
            //实例工作簿
            IWorkbook workbook;
            string fileExt = Path.GetExtension(file).ToLower();
            if (fileExt == ".xlsx") { workbook = new XSSFWorkbook(); } else if (fileExt == ".xls") { workbook = new HSSFWorkbook(); } else { workbook = null; }
            if (workbook == null) { return; }
            //创建Sheet
            ISheet sheet = string.IsNullOrEmpty(dt.TableName) ? workbook.CreateSheet("Sheet1") : workbook.CreateSheet(dt.TableName);

            //表头  
            IRow row = sheet.CreateRow(0);
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                ICell cell = row.CreateCell(i);
                cell.SetCellValue(dt.Columns[i].ColumnName);
            }
            //数据  
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                IRow row1 = sheet.CreateRow(i + 1);
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    ICell cell = row1.CreateCell(j);
                    cell.SetCellValue(dt.Rows[i][j].ToString());
                }
            }

            //转为字节数组  
            MemoryStream stream = new MemoryStream();
            workbook.Write(stream);
            var buf = stream.ToArray();

            //保存为Excel文件  
            using (FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                fs.Write(buf, 0, buf.Length);
                fs.Flush();
                HttpContext.Current.Response.ContentType = "application/octet-stream;charset=gb2321";
                HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(FileName, System.Text.Encoding.UTF8));
                HttpContext.Current.Response.BinaryWrite(buf);
                HttpContext.Current.Response.Flush();
                HttpContext.Current.Response.End();
            }
        }
        #endregion

        /// <summary>
        /// 条件查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btn_Search_Click(object sender, EventArgs e)
        {
            TextBox txt_Search = Page.FindControl("txt_Search") as TextBox;
            TextBox txt_Vendor = Page.FindControl("txt_Vendor") as TextBox;
            Ultimus.UWF.Form.WebControls.Repeater rpt = Page.FindControl("Repeater1") as Ultimus.UWF.Form.WebControls.Repeater;
            StringBuilder sb = new StringBuilder();
            sb.Append(@"BizDB.select *,(CASE STATUS WHEN '1' THEN 'Enable' WHEN '0' THEN 'Disable' ELSE '其他' END) as ISACTIVE  from MD_STATIONERYLIST where 1=1 ");
            if (!string.IsNullOrEmpty(txt_Search.Text))
            {
                sb.Append("and DESCRIPTION like N'%" + txt_Search.Text + "%'");
            }
            else if (!string.IsNullOrEmpty(txt_Vendor.Text))
            {
                sb.Append("and VENDORCODE like N'%" + txt_Search.Text + "%' or VENDORNAME like N'%" + txt_Search.Text + "%' ");
            }
            rpt.Source = sb.ToString();
        }


        /// <summary>
        /// 下载Excel模板
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void LinkButton1_Click(object sender, EventArgs e)
        {
            string strResult = string.Empty;
            string filename = "文具列表模板.xlsx";
            string strPath = Server.MapPath("/Solution/UPL.Common.MasterDataReport/ExcelTemplate/" + filename);

            using (FileStream fs = new FileStream(strPath, FileMode.OpenOrCreate))
            {
                byte[] bytes = new byte[(int)fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                fs.Flush();
                fs.Close();
                Response.ContentType = "application/octet-stream";
                Response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(filename, System.Text.Encoding.UTF8));
                Response.BinaryWrite(bytes);
                Response.Flush();
                Response.End();
            }
        }

        /// <summary>
        /// 图片批量上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btn_ImgFile_Click(object sender, EventArgs e)
        {
            HttpFileCollection uploadFiles = Request.Files;
            //文具列表数据
            string _sql = @"select * from MD_STATIONERYLIST where status=1";
            DataTable dt = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
            //供应商数据
            _sql = "select * from com_resource where type = 'VendorType' and ISACTIVE=1 ";
            DataTable dtVendor = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
            string loginname = ConvertUtil.ToString(SessionLogic.GetLoginName()).Replace("CustomOC\\", "");

            StringBuilder importInfo = new StringBuilder();
            StringBuilder importError = new StringBuilder();
            int importRow = 0;
            int errorRow = 0;
            importInfo.AppendLine("------------- 导入图片开始 -------------");
            for (int i = 1; i < uploadFiles.Count; i++)
            {
                bool power = false;
                string mess = "";
                HttpPostedFile postedFile = uploadFiles[i];
                try
                {
                    if (postedFile.ContentLength > 0)
                    {
                        string FileType = System.IO.Path.GetExtension(postedFile.FileName);
                        string Itemno = postedFile.FileName.Replace(FileType, "");
                        string admin = ConfigurationManager.AppSettings["AdminAccount"].ToString();
                        if (admin != loginname)
                        {
                            //判断是否有权限导入图片
                            string ext06 = GetResource(dt, "CODE", Itemno, "EXT06"); //配置的权限人
                            foreach (string str in ext06.Split(','))
                            {
                                power = true;
                                if (str == loginname)
                                {
                                    power = false;
                                    break;
                                }
                            }
                        }

                        //无权限导入
                        if (power)
                        {
                            mess = "FailInfo:---文具图片【" + Itemno + "】无权限导入,导入失败!";
                            importError.AppendLine(mess);
                            continue;
                        }
                        //判断物料是否存在，存在则导入，不存在则不导入
                        string item = GetResource(dt, "ITEMNO", Itemno, "ITEMNO");
                        string vendoeCode = GetResource(dt, "ITEMNO", Itemno, "VENDORCODE");
                        if (!string.IsNullOrEmpty(item))
                        {
                            string RootPath = MyLib.ConfigurationManager.AppSettings["RootPhysicalPath"].ToString(); //网址的根路径。用于页面文件引用及邮件提醒。
                            string path = "/File/Image/StationeryList/" + vendoeCode + "/"; //定义文件存储路径
                            if (!Directory.Exists(RootPath + path))
                            {
                                Directory.CreateDirectory(RootPath + path);
                            }

                            string filepath = Server.MapPath(path) + "\\";
                            DataAccess.Instance("BizDB").ExecuteNonQuery("update MD_STATIONERYLIST set IMAGEADRESS=@IMAGEADRESS where ITEMNO=@ITEMNO", path+ System.IO.Path.GetFileName(postedFile.FileName), Itemno);
                            postedFile.SaveAs(filepath + System.IO.Path.GetFileName(postedFile.FileName));
                            importRow++;
                        }
                        else
                        {
                            errorRow++;
                            mess = "FailInfo:---文具图片【" + Itemno + "】不存在,导入失败!";
                            importError.AppendLine(mess);
                        }
                    }
                }
                catch (Exception ex)
                {
                    importError.AppendLine("Error:---导入图片抛错:" + ex.Message);
                    errorRow++;
                }
            }
            importInfo.AppendLine("导入成功数:【" + importRow.ToString() + "】-----");
            importInfo.Append("导入失败数:【" + errorRow.ToString() + "】注：失败关键字搜索【FailInfo】【Error】");
            importInfo.AppendLine(" ");
            importInfo.AppendLine("------------- 如下是导入失败详细信息 -------------");
            importInfo.AppendLine(" ");
            importInfo.AppendLine(importError.ToString());
            importInfo.AppendLine("------------- 导入图片结束 -------------");
            //抛出导入结果
            if (!string.IsNullOrEmpty(importInfo.ToString()))
            {
                string RootPath = MyLib.ConfigurationManager.AppSettings["RootPhysicalPath"].ToString(); //网址的根路径。用于页面文件引用及邮件提醒。
                string fileName = loginname + "-img-" + DateTime.Now.ToString("yyyyMMddhhmmss") + ".txt"; //定义文件名称
                string path = "/File/ImportFile/StationeryList/"; //定义文件存储路径
                if (!Directory.Exists(RootPath + path))
                {
                    Directory.CreateDirectory(RootPath + path);
                }
                System.IO.File.WriteAllText(Server.MapPath(path + fileName), importInfo.ToString(), Encoding.UTF8);
                ClientScript.RegisterStartupScript(this.GetType(), "res", "window.open(\"" + path + fileName + "\");", true);
            }
        }
    }
}