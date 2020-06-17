using MyLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using MyLib.Data;
using System.Threading.Tasks;
using System.Linq;

namespace Ultimus.UWF.SyncSAPData
{
    /// <summary>
    /// 同步客户信息
    /// </summary>
    public class Sync_Customer
    {
        string _sql = "";
        public void SyncCustomer()
        {
            try
            {
                // 异步执行全量更新SAP中间表数据
                Task.Factory.StartNew(() =>
                 {
                     CreateSAPTable();
                 });
                //取得所有的客户店铺信息，做循环
                string IsSyncSingle = ConvertUtil.ToString(ConfigurationManager.AppSettings["IsSyncSingle"]);
                _sql = @"select a.Kunnr,a.KTOKD from SAP_KNA1 a
                       where KTOKD = 'SG01' ";
                if (IsSyncSingle == "1")
                {
                    string SyncSingleNo = ConvertUtil.ToString(ConfigurationManager.AppSettings["SyncSingleNo"]);
                    _sql += "and Kunnr in(" + SyncSingleNo + ")";
                }
                // and Kunnr in('0002006081','0002005830','0002004219') 
                DataTable dt = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql);
                int i = 0;
                // 省份，市，县
                _sql = "select citycode,cityname,ext01,CNNAME,ENNAME,CITYTYPE from md_city where ISACTIVE=1";
                DataTable dtRegion = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
                // 品牌
                _sql = "select * from com_resource where type = 'TYPE_Brand' and EXT01='BD' ";
                DataTable dtBrand = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
                // 部门
                _sql = "select * from ORG_DEPARTMENT where ISACTIVE=1";
                DataTable dtdep = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
                // 销售大区
                _sql = "select * from com_resource where type = 'Type_Territory'";
                DataTable dtSalesRegion = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
                // 销售区域
                _sql = "select * from com_resource where type = 'Type_Area'";
                DataTable dtSalesArea = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
                // watch class
                _sql = "select * from com_resource where type = 'Watch_Class'";
                DataTable dtWatchClass = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);
                // (销售员姓名/编号)
                _sql = "SELECT NAME,VALUE,REMARK FROM COM_RESOURCE WHERE TYPE = 'SALESMAN_OR_CODE'";
                List<ResourceEntity> rModel = DataAccess.Instance("BizDB").ExecuteList<ResourceEntity>(_sql);
                List<MappingField> fields = new List<MappingField>();
                List<string> pks = new List<string>();

                foreach (DataRow row in dt.Rows)
                {
                    //单个店铺code
                    string posCode = ConvertUtil.ToString(row["Kunnr"]);
                    //客户类型
                    string customerType = ConvertUtil.ToString(row["KTOKD"]) == "SG01" ? "SG01 Third Party Customer" : "SG08 SGCN Corporate Store";
                    //零售商code
                    //string customerCode = ConvertUtil.ToString(row["Kunnr"]);
                    string cuscode = string.Empty; //经销商CODE
                    try
                    {
                        #region 零售商信息（已注释）
                        ////零售商信息
                        //_sql = "select a.VKORG ,VTWEG,SPART,a.Kunnr,a.Kunn2, c.* from SAP_KNVP a inner " +
                        //     " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                        //     " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                        //    "where parvw = 'RG' and a.Kunnr = @p1  order by a.VKORG ";
                        //DataTable dtcustomer = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        //fields = new List<MappingField>();
                        //string inid = Guid.NewGuid().ToString();
                        //if (dtcustomer.Rows.Count > 0)
                        //{
                        //    DataRow dr = dtcustomer.Rows[0];
                        //    customerCode = ConvertUtil.ToString(dr["Kunn2"]);
                        //    //基本信息,零售商
                        //    fields.Add(new MappingField("INID", DbType.String, inid, false, true));
                        //    fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                        //    fields.Add(new MappingField("Customer_Code", DbType.String, customerCode)); //零售商号
                        //    fields.Add(new MappingField("Customer_Name", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                        //    fields.Add(new MappingField("Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                        //    fields.Add(new MappingField("Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                        //    fields.Add(new MappingField("City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                        //    fields.Add(new MappingField("City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                        //    fields.Add(new MappingField("County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                        //    fields.Add(new MappingField("County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                        //    fields.Add(new MappingField("Detailed_address", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                        //    //fields.Add(new MappingField("UserCompany", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                        //    //fields.Add(new MappingField("Brand", DbType.String, GetDepID(dtdep, GetResourceName(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                        //    //fields.Add(new MappingField("Brand_Name", DbType.String, GetResourceName(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                        //    DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_INFORMATION", "Customer_Code", null, fields);
                        //}
                        #endregion

                        LogUtil.Info((i + 1).ToString() + "-posCode:" + posCode);

                        #region 零售商信息、店铺信息
                        //基本信息，零售商信息、店铺信息
                        _sql = "select top 1 a.VKORG ,VTWEG,SPART,a.Kunnr,a.kunn2, c.* from SAP_KNVP a inner " +
                             " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                             " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                            "where parvw = 'AG' and a.Kunnr = @p1 order by vkorg desc";
                        DataTable dtBasic = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        fields = new List<MappingField>();
                        string inid = Guid.NewGuid().ToString();
                        if (dtBasic.Rows.Count > 0)
                        {
                            DataRow dr = dtBasic.Rows[0];
                            //基本信息,零售商信息
                            fields.Add(new MappingField("INID", DbType.String, inid, false, true));
                            fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                            //fields.Add(new MappingField("PROC_DOCUMENTNO", DbType.String, posCode)); 
                            fields.Add(new MappingField("Customer_Code", DbType.String, posCode)); //店铺号
                            fields.Add(new MappingField("POS_NO", DbType.String, posCode)); //店铺号
                            fields.Add(new MappingField("Customer_Name", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                            fields.Add(new MappingField("Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                            fields.Add(new MappingField("Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                            fields.Add(new MappingField("City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                            fields.Add(new MappingField("City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                            fields.Add(new MappingField("County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                            fields.Add(new MappingField("County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                            fields.Add(new MappingField("Detailed_address", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                            //fields.Add(new MappingField("UserCompany", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                            //fields.Add(new MappingField("Brand", DbType.String, GetDepID(dtdep, GetResourceName(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                            //fields.Add(new MappingField("Brand_Name", DbType.String, GetResourceName(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                            //店铺面积
                            _sql = "select a.ZSHOP_AREA from SAP_KNVV a inner join SAP_KNA1 b on a.KUNNR=b.KUNNR where a.KUNNR=@p1 ";
                            string ZSHOP_AREA = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, posCode));
                            fields.Add(new MappingField("SURFACE", DbType.String, ZSHOP_AREA));
                            DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_INFORMATION", "Customer_Code", null, fields);

                            //店铺信息
                            fields = new List<MappingField>();
                            fields.Add(new MappingField("SHOPID", DbType.String, Guid.NewGuid().ToString(), false, true));
                            fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                            fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                            //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode));//零售商code
                            fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));//店铺编号
                            fields.Add(new MappingField("Shop_Name_CN", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                            fields.Add(new MappingField("Shop_Name_EN", DbType.String, ConvertUtil.ToString(dr["NAME3"]) + ConvertUtil.ToString(dr["NAME4"])));
                            fields.Add(new MappingField("Shop_Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                            fields.Add(new MappingField("Shop_Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                            fields.Add(new MappingField("Shop_City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                            fields.Add(new MappingField("Shop_City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                            fields.Add(new MappingField("Shop_County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                            fields.Add(new MappingField("Shop_County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                            fields.Add(new MappingField("Store_Address", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                            fields.Add(new MappingField("ZIP_CODE", DbType.String, ConvertUtil.ToString(dr["POST_CODE1"])));
                            fields.Add(new MappingField("TELEPHONENUM", DbType.String, ConvertUtil.ToString(dr["TEL_NUMBER"])));
                            fields.Add(new MappingField("FAXNUM", DbType.String, ConvertUtil.ToString(dr["FAX_NUMBER"])));
                            string addr = ConvertUtil.ToString(dr["addrnumber"]);
                            if (!string.IsNullOrEmpty(addr))
                            {
                                //get email
                                string name = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(
                                    "select SMTP_ADDR from SAP_ADR6 where addrnumber =@p1", addr));
                                fields.Add(new MappingField("SHOP_EMAIL", DbType.String, name));
                            }
                            pks = new List<string>();
                            //pks.Add("Customer_Code");
                            pks.Add("SHOPCODE");
                            DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_STORE_DETAIL_INFORMATION", pks, null, fields);
                        }

                        #endregion

                        #region 公司经营信息，收货Watch
                        //公司经营信息，收货
                        _sql = "select a.VKORG ,VTWEG,SPART,a.Kunnr, c.* from SAP_KNVP a inner " +
                             " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                             " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                            "where parvw = 'WE' and VTWEG='01' and a.Kunnr = @p1 order by vkorg  desc";
                        DataTable dtOperation = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        fields = new List<MappingField>();
                        for (int j = 0; j < dtOperation.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            try
                            {
                                DataRow dr = dtOperation.Rows[j];
                                fields.Add(new MappingField("BUSINESSID", DbType.String, Guid.NewGuid().ToString(), false, true));
                                fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                                //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode));//零售商
                                fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));
                                //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                                fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                                fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                                fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                                fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                                fields.Add(new MappingField("RECIVING_COMPANY_NAME", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                                fields.Add(new MappingField("RECIVING_Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                                fields.Add(new MappingField("RECIVING_Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                                fields.Add(new MappingField("RECIVING_City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("RECIVING_City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("RECIVING_County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("RECIVING_County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("RECIVING_DETAILED_ADDRESS", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                                string[] STR_SUPPL2 = ConvertUtil.ToString(dr["STR_SUPPL2"]).Split('/');
                                string CONTRACT_PERSON1 = ConvertUtil.ToString(STR_SUPPL2[0]);
                                string CONTRACT_PERSON2 = STR_SUPPL2.Length > 1 ? ConvertUtil.ToString(STR_SUPPL2[1]) : "";
                                fields.Add(new MappingField("CONTRACT_PERSON1", DbType.String, CONTRACT_PERSON1));
                                fields.Add(new MappingField("CONTRACT_PERSON2", DbType.String, CONTRACT_PERSON2));
                                string[] TEL_NUMBER = ConvertUtil.ToString(dr["TEL_NUMBER"]).Split('/');
                                string RECIVING_PHONE_NUMBER = ConvertUtil.ToString(TEL_NUMBER[0]);
                                string MOBILE_PHONE1 = TEL_NUMBER.Length > 1 ? ConvertUtil.ToString(TEL_NUMBER[1]) : "";
                                fields.Add(new MappingField("MOBILE_PHONE1", DbType.String, MOBILE_PHONE1));
                                //fields.Add(new MappingField("MOBILE_PHONE2", DbType.String, MOBILE_PHONE2));
                                fields.Add(new MappingField("RECIVING_ZIP", DbType.String, ConvertUtil.ToString(dr["POST_CODE1"])));
                                fields.Add(new MappingField("RECIVING_PHONE_NUMBER", DbType.String, RECIVING_PHONE_NUMBER));
                                fields.Add(new MappingField("RECIVING_FAX", DbType.String, ConvertUtil.ToString(dr["FAX_NUMBER"])));
                                pks = new List<string>();
                                //pks.Add("Customer_Code");
                                pks.Add("SHOPCODE");
                                //pks.Add("COMPANYCODE");
                                pks.Add("TYPE_OF_ACCOUNT");
                                pks.Add("Brand");
                                DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_BUSINESS_INFORMATION", pks, null, fields);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error("公司经营信息，收货同步失败!" + ex.Message);
                            }
                        }
                        //if (dtOperation.Rows.Count > 0)
                        //{

                        //}
                        #endregion

                        #region 公司经营信息，收货 CS
                        //公司经营信息，收货
                        _sql = "select a.VKORG ,VTWEG,SPART,a.Kunnr, c.* from SAP_KNVP a inner " +
                             " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                             " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                            "where parvw = 'Z5' and VTWEG='90' and a.Kunnr = @p1 order by vkorg desc ";
                        DataTable dtOperationCS = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        fields = new List<MappingField>();
                        for (int j = 0; j < dtOperationCS.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            try
                            {
                                DataRow dr = dtOperationCS.Rows[j];
                                fields.Add(new MappingField("BUSINESSID", DbType.String, Guid.NewGuid().ToString(), false, true));
                                fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                                //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode));//零售商
                                fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));
                                //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                                fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                                fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                                fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                                fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                                fields.Add(new MappingField("RECIVING_COMPANY_NAME", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                                fields.Add(new MappingField("RECIVING_Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                                fields.Add(new MappingField("RECIVING_Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                                fields.Add(new MappingField("RECIVING_City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("RECIVING_City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("RECIVING_County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("RECIVING_County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("RECIVING_DETAILED_ADDRESS", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                                string[] STR_SUPPL2 = ConvertUtil.ToString(dr["STR_SUPPL2"]).Split('/');
                                string CONTRACT_PERSON1 = ConvertUtil.ToString(STR_SUPPL2[0]);
                                string CONTRACT_PERSON2 = STR_SUPPL2.Length > 1 ? ConvertUtil.ToString(STR_SUPPL2[1]) : "";
                                fields.Add(new MappingField("CONTRACT_PERSON1", DbType.String, CONTRACT_PERSON1));
                                fields.Add(new MappingField("CONTRACT_PERSON2", DbType.String, CONTRACT_PERSON2));
                                string[] TEL_NUMBER = ConvertUtil.ToString(dr["TEL_NUMBER"]).Split('/');
                                string RECIVING_PHONE_NUMBER = ConvertUtil.ToString(TEL_NUMBER[0]);
                                string MOBILE_PHONE1 = TEL_NUMBER.Length > 1 ? ConvertUtil.ToString(TEL_NUMBER[1]) : "";
                                fields.Add(new MappingField("MOBILE_PHONE1", DbType.String, MOBILE_PHONE1));
                                //fields.Add(new MappingField("MOBILE_PHONE2", DbType.String, MOBILE_PHONE2));
                                fields.Add(new MappingField("RECIVING_ZIP", DbType.String, ConvertUtil.ToString(dr["POST_CODE1"])));
                                fields.Add(new MappingField("RECIVING_PHONE_NUMBER", DbType.String, RECIVING_PHONE_NUMBER));
                                fields.Add(new MappingField("RECIVING_FAX", DbType.String, ConvertUtil.ToString(dr["FAX_NUMBER"])));
                                pks = new List<string>();
                                //pks.Add("Customer_Code");
                                pks.Add("SHOPCODE");
                                //pks.Add("COMPANYCODE");
                                pks.Add("TYPE_OF_ACCOUNT");
                                pks.Add("Brand");
                                DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_BUSINESS_INFORMATION", pks, null, fields);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error("公司经营信息，收货同步失败!" + ex.Message);
                            }
                        }
                        //if (dtOperation.Rows.Count > 0)
                        //{

                        //}
                        #endregion

                        #region 公司经营信息，保修卡

                        //公司经营信息，保修卡
                        _sql = "select a.VKORG ,VTWEG,SPART,a.Kunnr, c.* from SAP_KNVP a inner " +
                             " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                             " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                            "where parvw = 'WE' and VTWEG='01' and a.Kunnr = @p1 order by vkorg desc ";
                        DataTable dtRepaire = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        for (int j = 0; j < dtRepaire.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            try
                            {
                                DataRow dr = dtRepaire.Rows[j];
                                fields.Add(new MappingField("BUSINESSID", DbType.String, Guid.NewGuid().ToString(), false, true));
                                fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                                //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode)); //零售商号
                                fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));//店铺号
                                //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                                fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                                fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                                fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                                fields.Add(new MappingField("STORE_NAME", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                                fields.Add(new MappingField("Store_Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                                fields.Add(new MappingField("Store_Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                                fields.Add(new MappingField("Store_City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("Store_City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("Store_County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("Store_County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("STORE_DETAILED_ADDRESS", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                                pks = new List<string>();
                                //pks.Add("Customer_Code");
                                pks.Add("SHOPCODE");
                                //pks.Add("COMPANYCODE");
                                pks.Add("TYPE_OF_ACCOUNT");
                                pks.Add("Brand");
                                DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_BUSINESS_INFORMATION", pks, null, fields);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error("公司经营信息，保修卡同步失败!" + ex.Message);
                            }
                        }
                        //if (dtRepaire.Rows.Count > 0)
                        //{

                        //}
                        #endregion

                        #region 付款公司信息,付款

                        //付款公司信息,付款
                        _sql = "select a.VKORG ,VTWEG,SPART,a.Kunnr,a.Kunn2,b.STCD5, c.* from SAP_KNVP a inner " +
                             " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                             " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                            "where parvw = 'RG' and a.Kunnr = @p1 order by vkorg desc ";
                        DataTable dtPayment = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        for (int j = 0; j < dtPayment.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            DataRow dr = dtPayment.Rows[j];
                            fields.Add(new MappingField("PAYERSID", DbType.String, Guid.NewGuid().ToString(), false, true));
                            fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                            //fields.Add(new MappingField("Customer_Code", DbType.String, posCode));
                            fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));//店铺号
                            //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                            fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                            fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                            fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                            fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                            fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                            fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                            fields.Add(new MappingField("CODE", DbType.String, ConvertUtil.ToString(dr["Kunn2"])));
                            fields.Add(new MappingField("PAYERS_ADDRESS", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                            fields.Add(new MappingField("PAYERS_Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                            fields.Add(new MappingField("PAYERS_Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                            fields.Add(new MappingField("PAYERS_City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                            fields.Add(new MappingField("PAYERS_City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                            fields.Add(new MappingField("PAYERS_County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                            fields.Add(new MappingField("PAYERS_County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                            fields.Add(new MappingField("ZIP", DbType.String, ConvertUtil.ToString(dr["POST_CODE1"])));
                            fields.Add(new MappingField("TELEPHONE", DbType.String, ConvertUtil.ToString(dr["TEL_NUMBER"])));
                            fields.Add(new MappingField("FAX", DbType.String, ConvertUtil.ToString(dr["FAX_NUMBER"])));
                            pks = new List<string>();
                            //pks.Add("Customer_Code");
                            pks.Add("SHOPCODE");
                            //pks.Add("COMPANYCODE");
                            pks.Add("TYPE_OF_ACCOUNT");
                            pks.Add("Brand");
                            DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_PAYERS_COMPANY_INFORMATION", pks, null, fields);
                        }
                        //if (dtPayment.Rows.Count > 0)
                        //{

                        //}
                        #endregion

                        #region 付款公司信息,开票

                        //付款公司信息,开票
                        _sql = "select a.VKORG ,VTWEG,SPART,a.Kunnr,a.Kunn2,b.STKZU,b.STCD5, c.* from SAP_KNVP a inner " +
                             " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                             " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                            "where parvw = 'RE' and a.Kunnr = @p1 order by vkorg desc ";
                        DataTable dtInvoice = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        for (int j = 0; j < dtInvoice.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            try
                            {
                                DataRow dr = dtInvoice.Rows[j];
                                fields.Add(new MappingField("PAYERSID", DbType.String, Guid.NewGuid().ToString(), false, true));
                                fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                                cuscode = ConvertUtil.ToString(dr["Kunn2"]);//经销商CODE
                                //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode)); //零售商号
                                fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));//店铺号
                                //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                                fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                                fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                                fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                                fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                                string STKZU = ConvertUtil.ToString(dr["STKZU"]) == "X" ? "General VAT Payer-Special VAT Invoice to be issued(一般纳税人-开具专用发票)" : "Small scale VAT Payer-General VAT Invoice to be issued(小规模纳税人-开具普票)";
                                fields.Add(new MappingField("BILL_TYPE", DbType.String, STKZU));
                                fields.Add(new MappingField("COMPANY_NAME", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                                fields.Add(new MappingField("BILL_COMPANY_ADDRESS", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                                fields.Add(new MappingField("PHONE_NUMBER", DbType.String, ConvertUtil.ToString(dr["TEL_NUMBER"])));
                                string bankkey = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar("select BANKL from SAP_KNBK where KUNNR=@p1", cuscode));
                                string bankAc = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar("select BANKN from SAP_KNBK where KUNNR=@p1", cuscode));
                                fields.Add(new MappingField("BANK_AC", DbType.String, bankAc));
                                //获取bankname
                                if (!string.IsNullOrEmpty(bankkey))
                                {
                                    string bankname = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar("select bankname from md_bank where bankkey=@p1", bankkey));
                                    fields.Add(new MappingField("BANK_NAME", DbType.String, bankname));
                                }
                                fields.Add(new MappingField("PAYERS_COMPANY_NAME", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                                fields.Add(new MappingField("BUSINESS_LICENSE_NO", DbType.String, ConvertUtil.ToString(dr["STCD5"])));
                                pks = new List<string>();
                                //pks.Add("Customer_Code");
                                pks.Add("SHOPCODE");
                                //pks.Add("COMPANYCODE");
                                pks.Add("TYPE_OF_ACCOUNT");
                                pks.Add("Brand");
                                DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_PAYERS_COMPANY_INFORMATION", pks, null, fields);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error("付款公司信息,开票同步失败!" + ex.Message);
                            }
                        }
                        //if (dtInvoice.Rows.Count > 0)
                        //{

                        //}
                        #endregion

                        #region 付款公司信息,Watch发票邮寄信息

                        //付款公司信息,Watch发票
                        _sql = "select a.VKORG ,VTWEG,SPART,a.Kunnr, c.* from SAP_KNVP a inner " +
                             " join SAP_KNA1 b on a.kunn2 = b.kunnr inner " +
                             " join SAP_ADRC c on b.Adrnr = c.addrnumber " +
                            "where parvw = 'Z1' and VTWEG='01' and a.Kunnr = @p1 order by vkorg desc ";
                        DataTable dtwatch = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        for (int j = 0; j < dtwatch.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            try
                            {
                                DataRow dr = dtwatch.Rows[j];
                                fields.Add(new MappingField("PAYERSID", DbType.String, Guid.NewGuid().ToString(), false, true));
                                fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                                //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode));
                                fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));//店铺号
                                //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                                fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                                fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                                fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                                fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                                fields.Add(new MappingField("CHECK_COMPANY", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));//收票公司名称
                                fields.Add(new MappingField("COMPANY_ADDRESS", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                                string[] STR_SUPPL2 = ConvertUtil.ToString(dr["STR_SUPPL2"]).Split('/');
                                string RECIPIENT1 = ConvertUtil.ToString(STR_SUPPL2[0]);
                                string RECIPIENT2 = STR_SUPPL2.Length > 1 ? ConvertUtil.ToString(STR_SUPPL2[1]) : "";
                                fields.Add(new MappingField("RECIPIENT1", DbType.String, RECIPIENT1));
                                fields.Add(new MappingField("RECIPIENT2", DbType.String, RECIPIENT2));
                                fields.Add(new MappingField("COMPANY_PROVINCE", DbType.String, ConvertUtil.ToString(dr["REGION"])));//省
                                fields.Add(new MappingField("COMPANY_PROVINCE_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                                fields.Add(new MappingField("COMPANY_City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("COMPANY_City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("COMPANY_County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("COMPANY_County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("POSTAL_CODE", DbType.String, ConvertUtil.ToString(dr["POST_CODE1"])));
                                string[] TEL_NUMBER = ConvertUtil.ToString(dr["TEL_NUMBER"]).Split('/');
                                string PHONE_NO1 = ConvertUtil.ToString(TEL_NUMBER[0]);
                                string PHONE_NO2 = TEL_NUMBER.Length > 1 ? ConvertUtil.ToString(TEL_NUMBER[1]) : "";
                                fields.Add(new MappingField("PHONE_NO1", DbType.String, PHONE_NO1));
                                fields.Add(new MappingField("PHONE_NO2", DbType.String, PHONE_NO2));
                                pks = new List<string>();
                                //pks.Add("Customer_Code");
                                pks.Add("SHOPCODE");
                                //pks.Add("COMPANYCODE");
                                pks.Add("TYPE_OF_ACCOUNT");
                                pks.Add("Brand");
                                DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_PAYERS_COMPANY_INFORMATION", pks, null, fields);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error("付款公司信息,Watch发票同步失败!" + ex.Message);
                            }
                        }
                        //if (dtwatch.Rows.Count > 0)
                        //{

                        //}
                        #endregion

                        #region 付款公司信息,CS发票邮寄信息

                        //付款公司信息,CS发票
                        _sql = @"select a.VKORG ,VTWEG,SPART,a.Kunnr, c.* from SAP_KNVP a 
                                inner join SAP_KNA1 b on a.kunn2 = b.kunnr 
                                inner join SAP_ADRC c on b.Adrnr = c.addrnumber
                                where parvw = 'Z7' and VTWEG='90' and a.Kunnr = @p1 order by vkorg desc ";
                        DataTable dtcs = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        for (int j = 0; j < dtcs.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            try
                            {
                                DataRow dr = dtcs.Rows[j];
                                fields.Add(new MappingField("PAYERSID", DbType.String, Guid.NewGuid().ToString(), false, true));
                                fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                                //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode));
                                fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));//店铺号
                                //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                                fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                                fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                                fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                                fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                                fields.Add(new MappingField("CHECK_COMPANY", DbType.String, ConvertUtil.ToString(dr["NAME1"]) + ConvertUtil.ToString(dr["NAME2"])));
                                fields.Add(new MappingField("COMPANY_ADDRESS", DbType.String, ConvertUtil.ToString(dr["STREET"]) + ConvertUtil.ToString(dr["STR_SUPPL1"])));
                                string[] STR_SUPPL2 = ConvertUtil.ToString(dr["STR_SUPPL2"]).Split('/');
                                string RECIPIENT1 = ConvertUtil.ToString(STR_SUPPL2[0]);
                                string RECIPIENT2 = STR_SUPPL2.Length > 1 ? ConvertUtil.ToString(STR_SUPPL2[1]) : "";
                                fields.Add(new MappingField("RECIPIENT1", DbType.String, RECIPIENT1));
                                fields.Add(new MappingField("RECIPIENT2", DbType.String, RECIPIENT2));
                                fields.Add(new MappingField("COMPANY_Province", DbType.String, ConvertUtil.ToString(dr["REGION"])));
                                fields.Add(new MappingField("COMPANY_Province_NAME", DbType.String, GetRegion(dtRegion, ConvertUtil.ToString(dr["REGION"]))));
                                fields.Add(new MappingField("COMPANY_City", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("COMPANY_City_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY1"]), "Level3")));
                                fields.Add(new MappingField("COMPANY_County", DbType.String, GetCityCode(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("COMPANY_County_NAME", DbType.String, GetCityName(dtRegion, ConvertUtil.ToString(dr["CITY2"]), "Level4")));
                                fields.Add(new MappingField("POSTAL_CODE", DbType.String, ConvertUtil.ToString(dr["POST_CODE1"])));
                                string[] TEL_NUMBER = ConvertUtil.ToString(dr["TEL_NUMBER"]).Split('/');
                                string PHONE_NO1 = ConvertUtil.ToString(TEL_NUMBER[0]);
                                string PHONE_NO2 = TEL_NUMBER.Length > 1 ? ConvertUtil.ToString(TEL_NUMBER[1]) : "";
                                fields.Add(new MappingField("PHONE_NO1", DbType.String, PHONE_NO1));
                                fields.Add(new MappingField("PHONE_NO2", DbType.String, PHONE_NO2));
                                pks = new List<string>();
                                //pks.Add("Customer_Code");
                                pks.Add("SHOPCODE");
                                //pks.Add("COMPANYCODE");
                                pks.Add("TYPE_OF_ACCOUNT");
                                pks.Add("Brand");
                                DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_PAYERS_COMPANY_INFORMATION", pks, null, fields);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error("付款公司信息,Watch发票同步失败!" + ex.Message);
                            }
                        }
                        //if (dtcs.Rows.Count > 0)
                        //{

                        //}
                        #endregion

                        #region 销售建议
                        //销售建议
                        _sql = "select b.KATR10,b.KDKG1,a.KONDA,b.KATR10,a.* from SAP_KNVV a inner join SAP_KNA1 b on a.KUNNR=b.KUNNR where a.KUNNR=@p1 ";
                        DataTable dtSales = DataAccess.Instance("BizSAP").ExecuteDataTable(_sql, posCode);
                        for (int j = 0; j < dtSales.Rows.Count; j++)
                        {
                            fields = new List<MappingField>();
                            try
                            {
                                DataRow dr = dtSales.Rows[j];
                                fields.Add(new MappingField("PROPOSALID", DbType.String, Guid.NewGuid().ToString(), false, true));
                                fields.Add(new MappingField("PARENT_INID", DbType.String, inid, false, true));
                                //fields.Add(new MappingField("Customer_Code", DbType.String, customerCode));
                                fields.Add(new MappingField("SHOPCODE", DbType.String, posCode));//店铺号
                                //fields.Add(new MappingField("COMPANYCODE", DbType.String, ConvertUtil.ToString(dr["VKORG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNT", DbType.String, ConvertUtil.ToString(dr["VTWEG"])));
                                fields.Add(new MappingField("TYPE_OF_ACCOUNTNAME", DbType.String, ConvertUtil.ToString(dr["VTWEG"]) == "01" ? "Watch" : "Customer Service"));
                                fields.Add(new MappingField("Brand", DbType.String, ConvertUtil.ToString(dr["SPART"])));
                                fields.Add(new MappingField("DEPARTMENTID", DbType.String, GetDepID(dtdep, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"])))));
                                fields.Add(new MappingField("DEPARTMENTNAME", DbType.String, GetResourceCode(dtBrand, ConvertUtil.ToString(dr["SPART"]))));
                                fields.Add(new MappingField("ISACTIVE", DbType.String, "1"));
                                fields.Add(new MappingField("SALESMAN_OR_CODE_NAME", DbType.String, ConvertUtil.ToString(dr["VKGRP"])));
                                fields.Add(new MappingField("SALESMAN_OR_CODE", DbType.String, rModel.FirstOrDefault(p => p.VALUE.Contains(ConvertUtil.ToString(dr["VKGRP"])))));
                                fields.Add(new MappingField("SALES_TERRITORY", DbType.String, ConvertUtil.ToString(dr["KDGRP"])));
                                fields.Add(new MappingField("SALES_TERRITORY_NAME", DbType.String, GetResourceName(dtSalesRegion, ConvertUtil.ToString(dr["KDGRP"]))));
                                fields.Add(new MappingField("SALES_AREA_NAME", DbType.String, GetResourceName(dtSalesArea, ConvertUtil.ToString(dr["BZIRK"]))));
                                fields.Add(new MappingField("SALES_AREA", DbType.String, ConvertUtil.ToString(dr["BZIRK"])));
                                //商店类型
                                string store_Type = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(
                                @"select VALUE from COM_RESOURCE where type=@type and left(NAME,3)=@KVGR5", customerType + "02",
                                ConvertUtil.ToString(dr["KVGR5"])));
                                fields.Add(new MappingField("STORE_TYPE", DbType.String, store_Type));
                                //客户分类1
                                string customerCategory1Name = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(
                                @"select NAME from COM_RESOURCE where type='TYPE_CustomerGroupLevel' and value=@value and ISACTIVE=1", ConvertUtil.ToString(dr["KDKG1"])));
                                fields.Add(new MappingField("CUSTOMERCATEGORY1", DbType.String, ConvertUtil.ToString(dr["KDKG1"])));
                                fields.Add(new MappingField("CUSTOMERCATEGORY1_NAME", DbType.String, customerCategory1Name));
                                //客户分类2
                                string customerCategory2Name = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(
                                @"select NAME from COM_RESOURCE where PARENTID in
                                (select RESOURCEID from COM_RESOURCE where type='TYPE_CustomerGroupLevel' and VALUE=@VALUE) and VALUE=@VALUE0 and ISACTIVE=1",
                                ConvertUtil.ToString(dr["KDKG1"]), ConvertUtil.ToString(dr["KATR10"])));
                                fields.Add(new MappingField("CUSTOMERCATEGORY2", DbType.String, ConvertUtil.ToString(dr["KATR10"])));
                                fields.Add(new MappingField("CUSTOMERCATEGORY2_NAME", DbType.String, customerCategory2Name));
                                //商店级别
                                if (ConvertUtil.ToString(dr["VTWEG"]) == "01")
                                {
                                    string shop_class_for_watch_name = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(
                                    @"select NAME from COM_RESOURCE where type='Watch_Class' and ISACTIVE=1 and VALUE=@VALUE", ConvertUtil.ToString(dr["KONDA"])));
                                    fields.Add(new MappingField("SHOP_CLASS_FOR_WATCH", DbType.String, ConvertUtil.ToString(dr["KONDA"])));
                                    fields.Add(new MappingField("SHOP_CLASS_FOR_WATCH_NAME", DbType.String, shop_class_for_watch_name));
                                    fields.Add(new MappingField("SHOP_CLASS_FOR_CS_OR_SPARE_PARTS", DbType.String, ""));
                                }
                                else
                                {
                                    string SHOP_CLASS_FOR_CS_OR_SPARE_PARTS = ConvertUtil.ToString(dr["KONDA"]) == "11" ? "11-Grade C" : "23-Strategic customer";
                                    fields.Add(new MappingField("SHOP_CLASS_FOR_CS_OR_SPARE_PARTS", DbType.String, SHOP_CLASS_FOR_CS_OR_SPARE_PARTS));
                                    fields.Add(new MappingField("SHOP_CLASS_FOR_WATCH", DbType.String, ""));
                                    fields.Add(new MappingField("SHOP_CLASS_FOR_WATCH_NAME", DbType.String, ""));
                                }
                                string level_of_spare_parts_authorization = ConvertUtil.ToString(dr["KVGR3"]) == "D" ? "Omega-special" : ConvertUtil.ToString(dr["KONDA"]);
                                //零配件授权等级
                                fields.Add(new MappingField("LEVEL_OF_SPARE_PARTS_AUTHORIZATION", DbType.String, level_of_spare_parts_authorization));
                                //客户类型
                                fields.Add(new MappingField("CUSTOMER_TYPE", DbType.String, customerType));
                                pks = new List<string>();
                                //pks.Add("Customer_Code");
                                pks.Add("SHOPCODE");
                                //pks.Add("COMPANYCODE");
                                pks.Add("TYPE_OF_ACCOUNT");
                                pks.Add("Brand");
                                DataAccess.Instance("BizDB").SaveEntity("MD_CUSTOMER_MASTER_APPLICATION_PROPOSAL", pks, null, fields);
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error("销售建议同步失败!" + ex.Message);
                            }
                        }
                        //if (dtSales.Rows.Count > 0)
                        //{

                        //}
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error(ex.Message);
                        LogUtil.Error(ex);
                    }
                    i++;
                    //判断customerCode 是否为空，若为空直接获取店铺号
                    try
                    {
                        _sql = @"update MD_CUSTOMER_MASTER_APPLICATION_INFORMATION set PROC_DOCUMENTNO = CUSTOMER_CODE 
                        where isnull(PROC_DOCUMENTNO,'')='' and CUSTOMER_CODE=@CUSTOMER_CODE ";
                        DataAccess.Instance("BizDB").ExecuteNonQuery(_sql, posCode);
                        _sql = @"update MD_CUSTOMER_MASTER_APPLICATION_STORE_DETAIL_INFORMATION set CUSTOMER_CODE = SHOPCODE 
                        where isnull(CUSTOMER_CODE,'')='' and SHOPCODE=@SHOPCODE ";
                        DataAccess.Instance("BizDB").ExecuteNonQuery(_sql, posCode);
                        _sql = @"update MD_CUSTOMER_MASTER_APPLICATION_PAYERS_COMPANY_INFORMATION set CUSTOMER_CODE = SHOPCODE 
                        where isnull(CUSTOMER_CODE,'')='' and SHOPCODE=@SHOPCODE ";
                        DataAccess.Instance("BizDB").ExecuteNonQuery(_sql, posCode);
                        _sql = @"update MD_CUSTOMER_MASTER_APPLICATION_BUSINESS_INFORMATION set CUSTOMER_CODE = SHOPCODE 
                        where isnull(CUSTOMER_CODE,'')='' and SHOPCODE=@SHOPCODE ";
                        DataAccess.Instance("BizDB").ExecuteNonQuery(_sql, posCode);
                        _sql = @"update MD_CUSTOMER_MASTER_APPLICATION_PROPOSAL set CUSTOMER_CODE = SHOPCODE 
                        where isnull(CUSTOMER_CODE,'')='' and SHOPCODE=@SHOPCODE ";
                        DataAccess.Instance("BizDB").ExecuteNonQuery(_sql, posCode);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error("customer Code为空直接获取店铺号" + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
                LogUtil.Error(ex);
            }
        }

        public string GetRegion(DataTable dt, string code)
        {
            DataRow[] rows = dt.Select("citycode='" + code + "'");
            if (rows.Length > 0)
            {
                return ConvertUtil.ToString(rows[0]["cityname"]);
            }

            return "";
        }

        /// <summary>
        /// 通过名称查code
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="name"></param>
        /// <param name="Level">层级，Level1、Level2、Level3、Level4对应：国家省市县</param>
        /// <returns></returns>
        public string GetCityCode(DataTable dt, string name, string Level)
        {
            DataRow[] rows = dt.Select("CITYTYPE='" + Level + "' and (CNNAME='" + name + "' or ENNAME='" + name + "') ");
            if (rows.Length > 0)
            {
                return ConvertUtil.ToString(rows[0]["CITYCODE"]);
            }

            return "";
        }

        public string GetCityName(DataTable dt, string name, string Level)
        {
            DataRow[] rows = dt.Select("CITYTYPE='" + Level + "' and (CNNAME='" + name + "' or ENNAME='" + name + "') ");
            if (rows.Length > 0)
            {
                return ConvertUtil.ToString(rows[0]["CITYNAME"]);
            }

            return "";
        }

        public string GetRegionGroup(DataTable dt, string code)
        {
            DataRow[] rows = dt.Select("citycode='" + code + "'");
            if (rows.Length > 0)
            {
                return ConvertUtil.ToString(rows[0]["ext01"]);
            }

            return "";
        }

        public string GetResourceCode(DataTable dt, string value)
        {
            DataRow[] rows = dt.Select("CODE='" + value + "'");
            if (rows.Length > 0)
            {
                return ConvertUtil.ToString(rows[0]["name"]);
            }

            return "";
        }

        public string GetResourceName(DataTable dt, string value)
        {
            DataRow[] rows = dt.Select("VALUE='" + value + "'");
            if (rows.Length > 0)
            {
                return ConvertUtil.ToString(rows[0]["name"]);
            }

            return "";
        }

        public string GetDepID(DataTable dt, string value)
        {
            DataRow[] rows = dt.Select("DEPARTMENTNAME='" + value + "'");
            if (rows.Length > 0)
            {
                return ConvertUtil.ToString(rows[0]["DEPARTMENTID"]);
            }

            return "";
        }


        public void CreateSAPTable()
        {
            DataAccess DA = DataAccess.Instance("BizDB");
            System.Data.Common.DbCommand cmd = DA.CreateCommand();
            cmd.CommandTimeout = 3000;
            // SAP_KNVV
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVV]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV
                                        SELECT * INTO SAP_BPM_KNVV FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVV";
            DA.ExecuteNonQuery(cmd);

            // SAP_ADRC
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_ADRC]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_ADRC
                                        SELECT * INTO SAP_BPM_ADRC FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_ADRC";
            DA.ExecuteNonQuery(cmd);

            // SAP_KNA1
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNA1]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNA1
                                        SELECT * INTO SAP_BPM_KNA1 FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNA1";
            DA.ExecuteNonQuery(cmd);

            // SAP_KNVP开始  AG
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVP_AG]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV_AG
                                        SELECT * INTO SAP_BPM_KNVP_AG FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVP where parvw = 'AG'";
            DA.ExecuteNonQuery(cmd);

            // WE
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVP_WE]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV_WE
                                        SELECT * INTO SAP_BPM_KNVP_WE FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVP where parvw = 'WE'";
            DA.ExecuteNonQuery(cmd);

            // Z5
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVP_Z5]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV_Z5
                                        SELECT * INTO SAP_BPM_KNVP_Z5 FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVP where parvw = 'Z5'";
            DA.ExecuteNonQuery(cmd);

            // RG
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVP_RG]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV_RG
                                        SELECT * INTO SAP_BPM_KNVP_RG FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVP where parvw = 'RG'";
            DA.ExecuteNonQuery(cmd);

            // RE
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVP_RE]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV_RE
                                        SELECT * INTO SAP_BPM_KNVP_RE FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVP where parvw = 'RE'";
            DA.ExecuteNonQuery(cmd);

            // Z1
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVP_Z1]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV_Z1
                                        SELECT * INTO SAP_BPM_KNVP_Z1 FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVP where parvw = 'Z1'";
            DA.ExecuteNonQuery(cmd);

            // Z7
            cmd.CommandText = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SAP_BPM_KNVP_Z7]') AND type in (N'U'))
                                        DROP TABLE SAP_BPM_KNVV_Z7
                                        SELECT * INTO SAP_BPM_KNVP_Z7 FROM [SGRCNSH010].[SGRCNDC].dbo.SAP_KNVP where parvw = 'Z7'";
            DA.ExecuteNonQuery(cmd);
        }

        public class ResourceEntity
        {
            /// <summary>
            /// 
            /// </summary>
            public string NAME { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string VALUE { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string REMARK { get; set; }
        }
    }
}
