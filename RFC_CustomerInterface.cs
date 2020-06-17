using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MyLib;
using UPL.Common.BussinessControl.Logic;
using Ultimus.UWF.RFC.Entity;
using MyLib.Json;
using SAP.Middleware.Connector;

namespace Ultimus.UWF.RFC
{
    /// <summary>
    /// BPM供应商数据写入SAP（新建、更新、删除）
    /// </summary>
    public class RFC_CustomerInterface
    {
        string _sql = "";
        string _message = string.Empty;
        object _obj = null;
        bool _sucessflag = true;
        DataTable dt = new DataTable();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json">json字符串</param>
        /// <param name="CreateFunction">FRC接口名</param>
        /// <param name="OutTableName">SAP返回表名</param>
        /// <param name="sapconnect">SAP连接字符串</param>
        /// <returns></returns>
        public DataTable rfc_customerInterface(string json, string CreateFunction, string OutTableName, string sapconnect, string FORMID, string PROCESSNAME, string INCIDENT)
        {
            RFCCommon _rfccom = new RFCCommon();
            RfcInterface rfc = new RfcInterface();
            //初始化SAP连接
            RfcDestination destination = RfcDestinationManager.GetDestination(new SAPConfigConn().GetParameters(sapconnect));
            RfcRepository repo = destination.Repository;
            try
            {
                string SAPTYPE = "";
                string SAPMESSAGE = "";
                string RuntMassage = "";
                string cma_type = string.Empty;
                string project_classification = string.Empty;
                string type_of_account = string.Empty;
                string usercompany = string.Empty;
                string documentno = string.Empty;
                string CustomerCode = string.Empty;
                string companycode = string.Empty;
                _sql = "select  * from PROC_CMA where PROCESSNAME=@PROCESSNAME and INCIDENT=@INCIDENT ";
                DataTable dtMain = DataAccess.Instance("BizDB").ExecuteDataTable(_sql, PROCESSNAME, INCIDENT);
                if (dtMain.Rows.Count > 0 && dtMain != null)
                {
                    documentno = ConvertUtil.ToString(dtMain.Rows[0]["DOCUMENTNO"]);
                    cma_type = ConvertUtil.ToString(dtMain.Rows[0]["TYPE"]);
                    project_classification = ConvertUtil.ToString(dtMain.Rows[0]["PROJECT_CLASSIFICATION"]);
                    type_of_account = ConvertUtil.ToString(dtMain.Rows[0]["TYPE_OF_ACCOUNT"]);
                    usercompany = ConvertUtil.ToString(dtMain.Rows[0]["USERCOMPANY"]);
                    CustomerCode = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(
                    @"select top 1 SHOPCODE from MD_CUSTOMER_MASTER_APPLICATION_STORE_DETAIL_INFORMATION where CUSTOMER_CODE = @CUSTOMER_CODE",
                    ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMER_CODE"])));
                }

                //获取品牌CODE
                _sql = @"select code from COM_RESOURCE where type='TYPE_Brand' and EXT01='BD' and NAME=@NAME";
                string Division = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["BRAND_NAME"])));

                //处理公司、类型、品牌匹配项
                _sql = "select * from  [dbo].[MD_CUSTOMER_MASTER_SALES_AREA] where ISACTIVE=1";
                DataTable dtSales_Area = DataAccess.Instance("BizDB").ExecuteDataTable(_sql);

                //获取此单执行的成功日志
                _sql = "select * from COM_INTERFACE_LOG where PROCESSNAME=@PROCESSNAME and INCIDENT=@INCIDENT and FLAG='S' ";
                DataTable dtInterface_Log = DataAccess.Instance("BizDB").ExecuteDataTable(_sql, PROCESSNAME, INCIDENT);

                //处理_type_of_account，只需要01、90即可
                string type_of_accountS = string.Empty;
                foreach (string of_account in type_of_account.Split('\t'))
                {
                    string _of_account = of_account == "Watch" ? "01" : "90";
                    if (!type_of_accountS.Contains(_of_account))
                    {
                        type_of_accountS += _of_account + "\t";
                    }
                }
                type_of_accountS = type_of_accountS.TrimEnd('\t');

                string AG = string.Empty;
                string RE = string.Empty;
                string RG = string.Empty;
                string WE = string.Empty;
                string WC = string.Empty;
                string Z1 = string.Empty;
                string Z7 = string.Empty;
                string Z2 = string.Empty;
                string Z5 = string.Empty;
                string Z6 = string.Empty;
                string CS_WE = string.Empty;
                string VTWEG = string.Empty;
                string KZAZU = string.Empty;
                string KZTLF = string.Empty;
                string ANTLF = string.Empty;
                string PARVW = string.Empty;
                string STKZU = string.Empty;
                string REGIOGROUP = string.Empty;
                string KUNNR = string.Empty;
                Dictionary<string, string> RFCDict = new Dictionary<string, string>();
                ADR6 adr6 = new ADR6();
                ADRC adrc = new ADRC();
                KNA1 kna1 = new KNA1();
                KNB1 knb1 = new KNB1();
                KNBK knbk = new KNBK();
                KNVI knvi = new KNVI();
                KNVV knvv = new KNVV();
                KNVP knvp = new KNVP();

                if (cma_type == "Open New Customer" || cma_type == "Open New" || (cma_type == "Update" && project_classification == "Change Parent"))
                {
                    #region 操作类型（NEW）

                    #region Invoice mailing to /Address

                    foreach (string of_account in type_of_accountS.Split('\t'))
                    {
                        foreach (string company in usercompany.Split('\t'))
                        {
                            if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                            {
                                continue;
                            }

                            PARVW = (ConvertUtil.ToString(of_account) == "90" && ConvertUtil.ToString(type_of_account).Contains("Customer Service") && ConvertUtil.ToString(type_of_account).Contains("Spare Part")) ? "Z1,Z7" :
                                (ConvertUtil.ToString(of_account) == "90" && ConvertUtil.ToString(type_of_account).Contains("Customer Service")) ? "Z7" : "Z1";
                            companycode = ConvertUtil.ToString(company.Split('-')[0]);
                            RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, PARVW);
                            if (!string.IsNullOrEmpty(RuntMassage))
                            {
                                Z1 = RuntMassage;
                                Z7 = RuntMassage;
                                continue;
                            }

                            //SAP 获取编号
                            //string InvoiceAddressNO_Z7 = string.IsNullOrEmpty(CustomerCode) ? "" : SAP_InvoiceAddressNO_Z7(companycode, Division, of_account, CustomerCode);
                            //string InvoiceAddressNO_Z1 = string.IsNullOrEmpty(CustomerCode) ? "" : SAP_InvoiceAddressNO_Z1(companycode, Division, of_account, CustomerCode);
                            string InvoiceAddressNO_Z7 = "";
                            string InvoiceAddressNO_Z1 = "";
                            KUNNR = string.IsNullOrEmpty(Z7) ? InvoiceAddressNO_Z7 : Z7;
                            KUNNR = string.IsNullOrEmpty(Z1) ? InvoiceAddressNO_Z1 : Z1;

                            IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);

                            //ADRC
                            adrc = new ADRC();
                            _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                            REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_CITY"])));
                            adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["CHECK_COMPANY"]);
                            adrc.NAME2 = "";
                            adrc.NAME3 = "";
                            adrc.NAME4 = "";
                            adrc.SORT1 = "";
                            adrc.COUNTRY = "CN";
                            adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_PROVINCE"]);
                            adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_CITY_NAME"]).Split('-')[1] : "";
                            adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_COUNTY_NAME"]).Split('-')[0] : "";
                            adrc.REGIOGROUP = REGIOGROUP;
                            adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_ADDRESS"]), 60);
                            adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_ADDRESS"]), 60);
                            adrc.STR_SUPPL2 = ConvertUtil.ToString(dtMain.Rows[0]["RECIPIENT1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["RECIPIENT2"]);
                            adrc.STR_SUPPL3 = "";
                            adrc.LOCATION = "";
                            adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["POSTAL_CODE"]), 6);
                            adrc.LANGU = "ZH";
                            adrc.TEL_NUMBER = ConvertUtil.ToString(dtMain.Rows[0]["PHONE_NO1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["PHONE_NO2"]);
                            adrc.FAX_NUMBER = "";
                            adrc.EXTENSION1 = "";
                            adrc.EXTENSION2 = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                            //KNA1
                            kna1 = new KNA1();
                            kna1.KUNNR = KUNNR;
                            kna1.KTOKD = "SG05";
                            kna1.STCD5 = "";
                            kna1.STKZU = "";
                            kna1.UMSA1 = "0";
                            kna1.UWAER = "";
                            kna1.UMJAH = "";
                            kna1.KATR10 = "";
                            kna1.KDKG1 = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                            //KNVI
                            knvi = new KNVI();
                            knvi.TAXKD = "1";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                            //KNVV
                            knvv = new KNVV();
                            VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                            KZAZU = ConvertUtil.ToString(of_account) == "90" ? "" : (ConvertUtil.ToString(of_account) == "01" && (ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "39" || ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "40") ? "" : "X");
                            KZTLF = ConvertUtil.ToString(of_account) == "01" ? "B" : "";
                            ANTLF = ConvertUtil.ToString(of_account) == "01" ? "0" : "9";
                            knvv.VKORG = companycode;//按照公司循环
                            knvv.VTWEG = VTWEG;
                            knvv.SPART = Division;
                            knvv.BZIRK = "";
                            knvv.VKBUR = "";
                            knvv.VKGRP = "";
                            knvv.KDGRP = "";
                            knvv.WAERS = "";
                            knvv.KONDA = "";
                            knvv.KALKS = "";
                            knvv.VERSG = "";
                            knvv.LPRIO = "0";
                            knvv.KZAZU = KZAZU;
                            knvv.VSBED = "C1";
                            knvv.KZTLF = KZTLF;
                            knvv.ANTLF = ANTLF;
                            knvv.ZTERM = "";
                            knvv.KTGRD = "01";
                            knvv.KVGR5 = "";
                            knvv.AUFSD = "";
                            knvv.ZOPEN_DATE = "";
                            knvv.ZCLOSE_DATE = "";
                            knvv.ZSHOP_AREA = "0";
                            knvv.ZCUST_GL3 = "";
                            knvv.ZXY_POS_ID = "";
                            knvv.ZCTR_NO = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                            knvp = new KNVP();
                            knvp.KUNNR = KUNNR;
                            knvp.PARVW = "WE";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                            //调用RFC
                            dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                            foreach (DataRow item in dt.Rows)
                            {
                                SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                //添加执行日志
                                string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + PARVW;
                                _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                            }

                            if (SAPTYPE == "S")
                            {
                                Z1 = SAPMESSAGE.PadLeft(10, '0');
                                Z7 = SAPMESSAGE.PadLeft(10, '0');
                            }
                        }
                    }


                    #endregion

                    #region Warrenty Card

                    foreach (string of_account in type_of_accountS.Split('\t'))
                    {
                        if (of_account == "90")
                        {
                            //WC = "";
                            continue;
                        }
                        foreach (string company in usercompany.Split('\t'))
                        {
                            if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                            {
                                continue;
                            }
                            companycode = ConvertUtil.ToString(company.Split('-')[0]);
                            RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, "WC");
                            if (!string.IsNullOrEmpty(RuntMassage))
                            {
                                WC = RuntMassage;
                                continue;
                            }

                            //获取SAP号
                            //string WarrentyCardNo = string.IsNullOrEmpty(CustomerCode) ? "" : SAP_WarrentyCardNo(companycode, Division, of_account, CustomerCode);
                            string WarrentyCardNo = "";
                            KUNNR = string.IsNullOrEmpty(WC) ? WarrentyCardNo : WC;
                            IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                            //ADRC
                            adrc = new ADRC();
                            adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["STORE_NAME"]);
                            adrc.NAME2 = ConvertUtil.ToString(dtMain.Rows[0]["GROUP_NAME"]);
                            adrc.NAME3 = "";
                            adrc.NAME4 = "";
                            adrc.SORT1 = "";
                            adrc.COUNTRY = "CN";
                            adrc.REGION = "";
                            adrc.CITY1 = "";
                            adrc.CITY2 = "";
                            adrc.REGIOGROUP = "";
                            adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["STORE_PROVINCE_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_CITY_NAME"]).Split('-')[0] + "" +
                            ConvertUtil.ToString(dtMain.Rows[0]["STORE_COUNTY_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_DETAILED_ADDRESS"]).Split('-')[0], 60);
                            adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["STORE_PROVINCE_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_CITY_NAME"]).Split('-')[0] + "" +
                            ConvertUtil.ToString(dtMain.Rows[0]["STORE_COUNTY_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_DETAILED_ADDRESS"]).Split('-')[0], 60);
                            adrc.STR_SUPPL2 = "";
                            adrc.STR_SUPPL3 = "";
                            adrc.LOCATION = "";
                            adrc.POST_CODE1 = "";
                            adrc.LANGU = "ZH";
                            adrc.TEL_NUMBER = "";
                            adrc.FAX_NUMBER = "";
                            adrc.EXTENSION1 = "";
                            adrc.EXTENSION2 = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                            //KNA1
                            kna1 = new KNA1();
                            kna1.KUNNR = KUNNR;
                            kna1.KTOKD = "SG05";
                            kna1.STCD5 = "";
                            kna1.STKZU = "";
                            kna1.UMSA1 = "0";
                            kna1.UWAER = "";
                            kna1.UMJAH = "";
                            kna1.KATR10 = "";
                            kna1.KDKG1 = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                            //KNVI
                            knvi = new KNVI();
                            knvi.TAXKD = "1";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                            //KNVV
                            knvv = new KNVV();
                            VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                            knvv.VKORG = companycode;//按照公司循环
                            knvv.VTWEG = VTWEG;
                            knvv.SPART = Division;
                            knvv.BZIRK = "";
                            knvv.VKBUR = "";
                            knvv.VKGRP = "";
                            knvv.KDGRP = "";
                            knvv.WAERS = "";
                            knvv.KONDA = "";
                            knvv.KALKS = "";
                            knvv.VERSG = "";
                            knvv.LPRIO = "0";
                            knvv.KZAZU = "";
                            knvv.VSBED = "C1";
                            knvv.KZTLF = "B";
                            knvv.ANTLF = "0";
                            knvv.ZTERM = "";
                            knvv.KTGRD = "01";
                            knvv.KVGR5 = "";
                            knvv.AUFSD = "";
                            knvv.ZOPEN_DATE = "";
                            knvv.ZCLOSE_DATE = "";
                            knvv.ZSHOP_AREA = "0";
                            knvv.ZCUST_GL3 = "";
                            knvv.ZXY_POS_ID = "";
                            knvv.ZCTR_NO = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                            knvp = new KNVP();
                            knvp.KUNNR = KUNNR;
                            knvp.PARVW = "WE";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                            //调用RFC
                            dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                            foreach (DataRow item in dt.Rows)
                            {
                                SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                //添加执行日志
                                string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + "WC";
                                _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                            }
                            if (SAPTYPE == "S")
                            {
                                WC = SAPMESSAGE.PadLeft(10, '0');
                            }
                        }
                    }

                    #endregion

                    if (type_of_account.Split('\t').Length == 1 && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                    {
                        #region Ship to /Address 只勾选Customer Service

                        foreach (string of_account in type_of_accountS.Split('\t'))
                        {
                            foreach (string company in usercompany.Split('\t'))
                            {
                                if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                                {
                                    continue;
                                }
                                companycode = ConvertUtil.ToString(company.Split('-')[0]);
                                PARVW = "Z5";
                                RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, PARVW);
                                if (!string.IsNullOrEmpty(RuntMassage))
                                {
                                    Z5 = RuntMassage;
                                    continue;
                                }
                                KUNNR = string.IsNullOrEmpty(Z5) ? "" : Z5;
                                IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                                //ADRC
                                adrc = new ADRC();
                                //获取Reg. Struct. Grp.
                                _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                                REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY"])));
                                adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COMPANY_NAME"]);
                                adrc.NAME2 = "";
                                adrc.NAME3 = "";
                                adrc.NAME4 = "";
                                adrc.SORT1 = "";
                                adrc.COUNTRY = "CN";
                                adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PROVINCE"]);
                                adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-')[1] : "";
                                adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-')[0] : "";
                                adrc.REGIOGROUP = REGIOGROUP;
                                adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                adrc.STR_SUPPL2 = ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON2"]);
                                adrc.STR_SUPPL3 = "";
                                adrc.LOCATION = "";
                                adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_ZIP"]), 6);
                                adrc.LANGU = "ZH";
                                adrc.TEL_NUMBER = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PHONE_NUMBER"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["MOBILE_PHONE1"]);
                                adrc.FAX_NUMBER = "";
                                adrc.EXTENSION1 = "";
                                adrc.EXTENSION2 = "";
                                //ds.Tables.Add(ListToDatatable.ListToDataTable<ADRC>(adrcs));
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                                //KNA1
                                kna1 = new KNA1();
                                kna1.KUNNR = KUNNR;
                                kna1.KTOKD = "SG05";
                                kna1.STCD5 = "";
                                kna1.STKZU = "";
                                kna1.UMSA1 = "0";
                                kna1.UWAER = "";
                                kna1.UMJAH = "";
                                kna1.KATR10 = "";
                                kna1.KDKG1 = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                                //KNVI
                                knvi = new KNVI();
                                knvi.TAXKD = "1";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                                //KNVV
                                knvv = new KNVV();
                                VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                                KZAZU = ConvertUtil.ToString(of_account) == "90" ? "" : (ConvertUtil.ToString(of_account) == "01" && (ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "39" || ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "40") ? "" : "X");
                                KZTLF = ConvertUtil.ToString(of_account) == "01" ? "B" : "";
                                ANTLF = ConvertUtil.ToString(of_account) == "01" ? "0" : "9";
                                knvv.VKORG = companycode;//按照公司循环
                                knvv.VTWEG = VTWEG;
                                knvv.SPART = Division;
                                knvv.BZIRK = "";
                                knvv.VKBUR = "";
                                knvv.VKGRP = "";
                                knvv.KDGRP = "";
                                knvv.WAERS = "";
                                knvv.KONDA = "";
                                knvv.KALKS = "";
                                knvv.VERSG = "";
                                knvv.LPRIO = "0";
                                knvv.KZAZU = KZAZU;
                                knvv.VSBED = "C1";
                                knvv.KZTLF = KZTLF;
                                knvv.ANTLF = ANTLF;
                                knvv.ZTERM = "";
                                knvv.KTGRD = "01";
                                knvv.KVGR5 = "";
                                knvv.AUFSD = "";
                                knvv.ZOPEN_DATE = "";
                                knvv.ZCLOSE_DATE = "";
                                knvv.ZSHOP_AREA = "0";
                                knvv.ZCUST_GL3 = "";
                                knvv.ZXY_POS_ID = "";
                                knvv.ZCTR_NO = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                                knvp = new KNVP();
                                knvp.KUNNR = KUNNR;
                                knvp.PARVW = "WE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                                //调用RFC
                                dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                                foreach (DataRow item in dt.Rows)
                                {
                                    SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                    SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                    //添加执行日志
                                    string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + PARVW;
                                    _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                                }
                                if (SAPTYPE == "S")
                                {
                                    Z5 = SAPMESSAGE.PadLeft(10, '0');
                                }
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        #region Ship to /Address

                        foreach (string of_account in type_of_accountS.Split('\t'))
                        {
                            foreach (string company in usercompany.Split('\t'))
                            {
                                if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                                {
                                    continue;
                                }
                                companycode = ConvertUtil.ToString(company.Split('-')[0]);
                                PARVW = of_account == "01" ? "WE" : "WE,Z5";
                                RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, PARVW);
                                if (!string.IsNullOrEmpty(RuntMassage))
                                {
                                    if (PARVW == "WE")
                                        WE = RuntMassage;
                                    else
                                    {
                                        WE = RuntMassage;
                                        Z5 = RuntMassage;
                                    }
                                    continue;
                                }
                                KUNNR = string.IsNullOrEmpty(WE) ? "" : WE;
                                IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                                //ADRC
                                adrc = new ADRC();
                                //获取Reg. Struct. Grp.
                                _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                                REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY"])));
                                adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COMPANY_NAME"]);
                                adrc.NAME2 = "";
                                adrc.NAME3 = "";
                                adrc.NAME4 = "";
                                adrc.SORT1 = "";
                                adrc.COUNTRY = "CN";
                                adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PROVINCE"]);
                                adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-')[1] : "";
                                adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-')[0] : "";
                                adrc.REGIOGROUP = REGIOGROUP;
                                adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                adrc.STR_SUPPL2 = ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON2"]);
                                adrc.STR_SUPPL3 = "";
                                adrc.LOCATION = "";
                                adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_ZIP"]), 6);
                                adrc.LANGU = "ZH";
                                adrc.TEL_NUMBER = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PHONE_NUMBER"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["MOBILE_PHONE1"]);
                                adrc.FAX_NUMBER = "";
                                adrc.EXTENSION1 = "";
                                adrc.EXTENSION2 = "";
                                //ds.Tables.Add(ListToDatatable.ListToDataTable<ADRC>(adrcs));
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                                //KNA1
                                kna1 = new KNA1();
                                kna1.KUNNR = KUNNR;
                                kna1.KTOKD = "SG05";
                                kna1.STCD5 = "";
                                kna1.STKZU = "";
                                kna1.UMSA1 = "0";
                                kna1.UWAER = "";
                                kna1.UMJAH = "";
                                kna1.KATR10 = "";
                                kna1.KDKG1 = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                                //KNVI
                                knvi = new KNVI();
                                knvi.TAXKD = "1";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                                //KNVV
                                knvv = new KNVV();
                                VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                                KZAZU = ConvertUtil.ToString(of_account) == "90" ? "" : (ConvertUtil.ToString(of_account) == "01" && (ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "39" || ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "40") ? "" : "X");
                                KZTLF = ConvertUtil.ToString(of_account) == "01" ? "B" : "";
                                ANTLF = ConvertUtil.ToString(of_account) == "01" ? "0" : "9";
                                knvv.VKORG = companycode;//按照公司循环
                                knvv.VTWEG = VTWEG;
                                knvv.SPART = Division;
                                knvv.BZIRK = "";
                                knvv.VKBUR = "";
                                knvv.VKGRP = "";
                                knvv.KDGRP = "";
                                knvv.WAERS = "";
                                knvv.KONDA = "";
                                knvv.KALKS = "";
                                knvv.VERSG = "";
                                knvv.LPRIO = "0";
                                knvv.KZAZU = KZAZU;
                                knvv.VSBED = "C1";
                                knvv.KZTLF = KZTLF;
                                knvv.ANTLF = ANTLF;
                                knvv.ZTERM = "";
                                knvv.KTGRD = "01";
                                knvv.KVGR5 = "";
                                knvv.AUFSD = "";
                                knvv.ZOPEN_DATE = "";
                                knvv.ZCLOSE_DATE = "";
                                knvv.ZSHOP_AREA = "0";
                                knvv.ZCUST_GL3 = "";
                                knvv.ZXY_POS_ID = "";
                                knvv.ZCTR_NO = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                                knvp = new KNVP();
                                knvp.KUNNR = KUNNR;
                                knvp.PARVW = "WE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                                //调用RFC
                                dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                                foreach (DataRow item in dt.Rows)
                                {
                                    SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                    SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                    //添加执行日志
                                    string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + PARVW;
                                    _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                                }
                                if (SAPTYPE == "S")
                                {
                                    if (PARVW == "WE")
                                        WE = SAPMESSAGE.PadLeft(10, '0');
                                    else
                                    {
                                        WE = SAPMESSAGE.PadLeft(10, '0');
                                        Z5 = SAPMESSAGE.PadLeft(10, '0');
                                    }
                                }
                            }
                        }

                        #endregion
                    }

                    #region Sold to

                    foreach (string of_account in type_of_accountS.Split('\t'))
                    {
                        foreach (string company in usercompany.Split('\t'))
                        {
                            if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                            {
                                continue;
                            }
                            companycode = ConvertUtil.ToString(company.Split('-')[0]);
                            RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, "AG");
                            if (!string.IsNullOrEmpty(RuntMassage))
                            {
                                AG = RuntMassage;
                                continue;
                            }
                            string SOLDTONO = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["SOLDTONO"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["SOLDTONO"]).PadLeft(10, '0');
                            KUNNR = string.IsNullOrEmpty(AG) ? SOLDTONO : AG;
                            IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                            //ADR6
                            adr6.SMTP_ADDR = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_EMAIL"]);
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adr6));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADR6", rfcFunc);

                            //ADRC
                            adrc = new ADRC();
                            _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                            REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY"])));
                            adrc.NAME1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_CN"]), 40);
                            adrc.NAME2 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_CN"]), 40);
                            adrc.NAME3 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_EN"]), 40);
                            adrc.NAME4 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_EN"]), 40);
                            adrc.SORT1 = "";
                            adrc.COUNTRY = "CN";
                            adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_PROVINCE"]);
                            adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY_NAME"]).Split('-')[1] : "";
                            adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_COUNTY_NAME"]).Split('-')[0] : "";
                            adrc.REGIOGROUP = REGIOGROUP;
                            adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["STORE_ADDRESS"]), 60);
                            adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["STORE_ADDRESS"]), 60);
                            adrc.STR_SUPPL2 = "";
                            adrc.STR_SUPPL3 = "";
                            adrc.LOCATION = "";
                            adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["ZIP_CODE"]), 6);
                            adrc.LANGU = "ZH";
                            adrc.TEL_NUMBER = "";
                            adrc.FAX_NUMBER = "";
                            adrc.EXTENSION1 = "";
                            adrc.EXTENSION2 = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                            //KNA1
                            kna1 = new KNA1();
                            kna1.KUNNR = KUNNR;
                            kna1.KTOKD = "SG01";
                            kna1.STCD5 = "";
                            kna1.STKZU = "";
                            kna1.UMSA1 = "0";
                            kna1.UWAER = "";
                            kna1.UMJAH = "";
                            kna1.KATR10 = ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMERCATEGORY2"]);
                            kna1.KDKG1 = ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMERCATEGORY1"]);
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                            //KNB1
                            knb1 = new KNB1();
                            knb1.BUKRS = companycode;//按照公司循环
                            knb1.AKONT = "0013001100";
                            knb1.ALTKN = "";
                            knb1.ZUAWA = "001";
                            knb1.FDGRV = "ZD02";
                            knb1.ZTERM = "Z000";
                            knb1.XZVER = "X";
                            knb1.ZWELS = "";
                            knb1.XAUSZ = "2";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knb1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNB1", rfcFunc);

                            //KNVI
                            knvi = new KNVI();
                            knvi.TAXKD = "1";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                            //KNVV
                            knvv = new KNVV();
                            VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                            string KONDA = ConvertUtil.ToString(of_account) == "01" ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_WATCH"]) : (string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_CS_OR_SPARE_PARTS"])) ? ConvertUtil.ToString(dtMain.Rows[0]["LEVELOFSP"]).Split('-')[0] : ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_CS_OR_SPARE_PARTS"]));
                            string AUFSD = (KONDA == "99" && ConvertUtil.ToString(of_account) == "01") ? "C1" : "";
                            knvv.VKORG = companycode;//按照公司循环
                            knvv.VTWEG = VTWEG;
                            knvv.SPART = Division;
                            knvv.BZIRK = ConvertUtil.ToString(dtMain.Rows[0]["SALES_AREA"]);
                            knvv.VKBUR = "48" + Division;
                            knvv.VKGRP = ConvertUtil.ToString(dtMain.Rows[0]["SALESMAN_OR_CODE_NAME"]);
                            knvv.KDGRP = ConvertUtil.ToString(dtMain.Rows[0]["SALES_TERRITORY"]);
                            knvv.WAERS = "CNY";
                            knvv.KONDA = KONDA;
                            knvv.KALKS = "1";
                            knvv.VERSG = "1";
                            knvv.LPRIO = "0";
                            knvv.KZAZU = "";
                            knvv.VSBED = "C1";
                            knvv.KZTLF = "";
                            knvv.ANTLF = "0";
                            knvv.ZTERM = "Z000";
                            knvv.KTGRD = "01";
                            knvv.KVGR5 = ConvertUtil.ToString(dtMain.Rows[0]["STORE_TYPE"]);
                            if (of_account == "90")// Spare Part    Customer Service
                                knvv.KVGR5 = "X09";
                            knvv.AUFSD = AUFSD;
                            knvv.ZOPEN_DATE = ConvertUtil.ToString(ConvertUtil.ToDateTime(dtMain.Rows[0]["PLANNED_OPENNING_MONTH_DATE"]).ToString("yyyyMMdd"));
                            knvv.ZCLOSE_DATE = null;
                            knvv.LOEVM = "";
                            knvv.KVGR4 = "";
                            knvv.ZSHOP_AREA = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["SURFACE"])) ? "0" : ConvertUtil.ToString(dtMain.Rows[0]["SURFACE"]);
                            knvv.ZCUST_GL3 = "";
                            knvv.ZXY_POS_ID = ConvertUtil.ToString(dtMain.Rows[0]["OTHER2"]);
                            knvv.ZCTR_NO = ConvertUtil.ToString(dtMain.Rows[0]["CATEGORY2"]);
                            knvv.KVGR3 = of_account == "01" ? "" : ConvertUtil.ToString(dtMain.Rows[0]["LEVEL_OF_SPARE_PARTS_AUTHORIZATION"]) == "Omega-special" ? "D" : ConvertUtil.ToString(dtMain.Rows[0]["LEVEL_OF_SPARE_PARTS_AUTHORIZATION"]);
                            knvv.AWAHR = "100";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                            Z2 = of_account == "01" ? string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMER_CODE"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMER_CODE"]).PadLeft(10, '0') : "";
                            Z6 = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["RELATED_SOLD_TO_NO"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["RELATED_SOLD_TO_NO"]).PadLeft(10, '0');
                            RE = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["BILLTONO"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["BILLTONO"]).PadLeft(10, '0');
                            RG = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["CODE"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["CODE"]).PadLeft(10, '0');

                            //KNVP AG -- Sold-to party
                            knvp = new KNVP();
                            knvp.KUNNR = KUNNR;
                            knvp.PARVW = "AG";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                            //RE -- Bill-to party
                            if (!string.IsNullOrEmpty(RE))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = RE;
                                knvp.PARVW = "RE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //RG -- Payer
                            if (!string.IsNullOrEmpty(RG))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = RG;
                                knvp.PARVW = "RG";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //WE -- Ship-to party,If customer only for customer service, then ship-to assign as 0004005770 - DUMMY 只勾选CS = 0004005770
                            if (type_of_account.Split('\t').Length == 1 && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                //获取SAP号
                                string ShipAddressNo = SAP_ShipAddressNo(companycode, Division, of_account, ConvertUtil.ToString(dtMain.Rows[0]["SOLDTONO"]).PadLeft(10, '0'));
                                KUNNR = string.IsNullOrEmpty(ShipAddressNo) ? "0004005770" : ShipAddressNo;
                                if (!string.IsNullOrEmpty(KUNNR))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = KUNNR;
                                    knvp.PARVW = "WE";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }
                            else if (!string.IsNullOrEmpty(WE))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = WE;
                                knvp.PARVW = "WE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //WC -- Warranty card addr.,Only for Watch customer
                            if (!string.IsNullOrEmpty(WC) && of_account == "01")
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = WC;
                                knvp.PARVW = "WC";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //Z1 -- Invoice mailing add, Watch and spare part invoice sending address
                            if (!string.IsNullOrEmpty(Z1) && ((ConvertUtil.ToString(of_account) == "90" && ConvertUtil.ToString(type_of_account).Contains("Spare Part")) || ConvertUtil.ToString(of_account) == "01"))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z1;
                                knvp.PARVW = "Z1";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //Z7 -- Inv.Mail.Addr.Servi,Customer Service invoice sending address
                            if (!string.IsNullOrEmpty(Z7) && ConvertUtil.ToString(of_account) == "90" && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z7;
                                knvp.PARVW = "Z7";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //Z2 -- Previous cust. numb, Only for watch, capture orginal customer store # when one store convert from distributor A to B
                            if (!string.IsNullOrEmpty(Z2))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z2;
                                knvp.PARVW = "Z2";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //Z5 -- CS repair ship to,Customer Service goods sending address
                            if (!string.IsNullOrEmpty(Z5) && ConvertUtil.ToString(of_account) == "90" && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z5;
                                knvp.PARVW = "Z5";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            //Z6 -- Related Sold-to,For CS process, some pos store need to link to watch pos
                            if (!string.IsNullOrEmpty(Z6))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z6;
                                knvp.PARVW = "Z6";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //调用RFC
                            dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                            foreach (DataRow item in dt.Rows)
                            {
                                SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                //添加执行日志
                                string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + "AG";
                                _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                            }
                            if (SAPTYPE == "S")
                            {
                                AG = SAPMESSAGE.PadLeft(10, '0');
                            }
                        }
                    }

                    #endregion

                    if (SAPTYPE == "S" || !string.IsNullOrEmpty(AG))
                    {
                        //写入成功，更新客户申请数据
                        DataAccess.Instance("BizDB").ExecuteNonQuery("update MD_CUSTOMER_MASTER_APPLICATION_STORE_DETAIL_INFORMATION set SHOPCODE=@SHOPCODE where CUSTOMER_CODE=@DOCUMENTNO", AG, documentno);
                        DataAccess.Instance("BizDB").ExecuteNonQuery("update MD_CUSTOMER_MASTER_APPLICATION_BUSINESS_INFORMATION set SHOPCODE=@SHOPCODE where CUSTOMER_CODE=@DOCUMENTNO", AG, documentno);
                        DataAccess.Instance("BizDB").ExecuteNonQuery("update MD_CUSTOMER_MASTER_APPLICATION_PAYERS_COMPANY_INFORMATION set SHOPCODE=@SHOPCODE where CUSTOMER_CODE=@DOCUMENTNO", AG, documentno);
                        DataAccess.Instance("BizDB").ExecuteNonQuery("update MD_CUSTOMER_MASTER_APPLICATION_PROPOSAL set SHOPCODE=@SHOPCODE where CUSTOMER_CODE=@DOCUMENTNO", AG, documentno);
                        DataAccess.Instance("BizDB").ExecuteNonQuery("update MD_CUSTOMER_MASTER_APPLICATION_INFORMATION set POS_NO=@SHOPCODE,CUSTOMER_CODE=@SHOPCODE where PROC_DOCUMENTNO=@DOCUMENTNO", AG, documentno);
                        //写入成功，更新返回标识及返回信息
                        _rfccom.UpdateSyncSucess("COM_INTERFACE_MASTERDATA", "FORMID", FORMID, AG);
                    }
                    else
                    {
                        //执行错误，更新执行次数
                        _rfccom.UpdateSyncError("COM_INTERFACE_MASTERDATA", "FORMID", FORMID);
                    }

                    #endregion
                }
                else if (cma_type == "Update" && project_classification != "Change Parent")
                {
                    #region 操作类型（更新）

                    #region Invoice mailing to /Address
                    string FLAG1 = ConvertUtil.ToString(dtMain.Rows[0]["FLAG1"]); //1表示有修改，新建编号
                    if (FLAG1 == "1")
                    {
                        foreach (string of_account in type_of_accountS.Split('\t'))
                        {
                            foreach (string company in usercompany.Split('\t'))
                            {
                                if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                                {
                                    continue;
                                }
                                PARVW = (ConvertUtil.ToString(type_of_account).Contains("Watch") || ConvertUtil.ToString(type_of_account).Contains("Spare Part")) ? "Z1" : "Z7";
                                companycode = ConvertUtil.ToString(company.Split('-')[0]);
                                RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, PARVW);
                                if (!string.IsNullOrEmpty(RuntMassage))
                                {
                                    if (PARVW == "Z7")
                                        Z7 = RuntMassage;
                                    else if (PARVW == "Z1")
                                        Z1 = RuntMassage;
                                    continue;
                                }

                                if (PARVW == "Z7")
                                    KUNNR = string.IsNullOrEmpty(Z7) ? "" : Z7;
                                else if (PARVW == "Z1")
                                    KUNNR = string.IsNullOrEmpty(Z1) ? "" : Z1;
                                IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);

                                //ADRC
                                adrc = new ADRC();
                                _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                                REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_CITY"])));
                                adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["CHECK_COMPANY"]);
                                adrc.NAME2 = "";
                                adrc.NAME3 = "";
                                adrc.NAME4 = "";
                                adrc.SORT1 = "";
                                adrc.COUNTRY = "CN";
                                adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_PROVINCE"]);
                                adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_CITY_NAME"]).Split('-')[1] : "";
                                adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_COUNTY_NAME"]).Split('-')[0] : "";
                                adrc.REGIOGROUP = REGIOGROUP;
                                adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_ADDRESS"]), 60);
                                adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["COMPANY_ADDRESS"]), 60);
                                adrc.STR_SUPPL2 = ConvertUtil.ToString(dtMain.Rows[0]["RECIPIENT1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["RECIPIENT2"]);
                                adrc.STR_SUPPL3 = "";
                                adrc.LOCATION = "";
                                adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["POSTAL_CODE"]), 6);
                                adrc.LANGU = "ZH";
                                adrc.TEL_NUMBER = ConvertUtil.ToString(dtMain.Rows[0]["PHONE_NO1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["PHONE_NO2"]);
                                adrc.FAX_NUMBER = "";
                                adrc.EXTENSION1 = "";
                                adrc.EXTENSION2 = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                                //KNA1
                                kna1 = new KNA1();
                                kna1.KUNNR = KUNNR;
                                kna1.KTOKD = "SG05";
                                kna1.STCD5 = "";
                                kna1.STKZU = "";
                                kna1.UMSA1 = "0";
                                kna1.UWAER = "";
                                kna1.UMJAH = "";
                                kna1.KATR10 = "";
                                kna1.KDKG1 = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                                //KNVI
                                knvi = new KNVI();
                                knvi.TAXKD = "1";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                                //KNVV
                                knvv = new KNVV();
                                VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                                KZAZU = ConvertUtil.ToString(of_account) == "90" ? "" : (ConvertUtil.ToString(of_account) == "01" && (ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "39" || ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "40") ? "" : "X");
                                KZTLF = ConvertUtil.ToString(of_account) == "01" ? "B" : "";
                                ANTLF = ConvertUtil.ToString(of_account) == "01" ? "0" : "9";
                                knvv.VKORG = companycode;//按照公司循环
                                knvv.VTWEG = VTWEG;
                                knvv.SPART = Division;
                                knvv.BZIRK = "";
                                knvv.VKBUR = "";
                                knvv.VKGRP = "";
                                knvv.KDGRP = "";
                                knvv.WAERS = "";
                                knvv.KONDA = "";
                                knvv.KALKS = "";
                                knvv.VERSG = "";
                                knvv.LPRIO = "0";
                                knvv.KZAZU = KZAZU;
                                knvv.VSBED = "C1";
                                knvv.KZTLF = KZTLF;
                                knvv.ANTLF = ANTLF;
                                knvv.ZTERM = "";
                                knvv.KTGRD = "01";
                                knvv.KVGR5 = "";
                                knvv.AUFSD = "";
                                knvv.ZOPEN_DATE = "";
                                knvv.ZCLOSE_DATE = "";
                                knvv.ZSHOP_AREA = "0";
                                knvv.ZCUST_GL3 = "";
                                knvv.ZXY_POS_ID = "";
                                knvv.ZCTR_NO = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                                knvp = new KNVP();
                                knvp.KUNNR = KUNNR;
                                knvp.PARVW = "WE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                                //调用RFC
                                dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                                foreach (DataRow item in dt.Rows)
                                {
                                    SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                    SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                    //添加执行日志
                                    string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + PARVW;
                                    _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                                }

                                if (SAPTYPE == "S")
                                {
                                    if (PARVW == "Z7")
                                        Z7 = SAPMESSAGE.PadLeft(10, '0');
                                    else
                                        Z1 = SAPMESSAGE.PadLeft(10, '0');
                                }
                            }
                        }
                    }

                    #endregion

                    #region Warrenty Card
                    string FLAG3 = ConvertUtil.ToString(dtMain.Rows[0]["FLAG3"]); //1表示有修改，新建编号
                    if (FLAG3 == "1")
                    {
                        foreach (string of_account in type_of_accountS.Split('\t'))
                        {
                            if (of_account == "90")
                                continue;
                            foreach (string company in usercompany.Split('\t'))
                            {
                                if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                                {
                                    continue;
                                }
                                companycode = ConvertUtil.ToString(company.Split('-')[0]);
                                RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, "WC");
                                if (!string.IsNullOrEmpty(RuntMassage))
                                {
                                    WC = RuntMassage;
                                    continue;
                                }

                                KUNNR = string.IsNullOrEmpty(WC) ? "" : WC;
                                IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                                //ADRC
                                adrc = new ADRC();
                                adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["STORE_NAME"]);
                                adrc.NAME2 = ConvertUtil.ToString(dtMain.Rows[0]["GROUP_NAME"]);
                                adrc.NAME3 = "";
                                adrc.NAME4 = "";
                                adrc.SORT1 = "";
                                adrc.COUNTRY = "CN";
                                adrc.REGION = "";
                                adrc.CITY1 = "";
                                adrc.CITY2 = "";
                                adrc.REGIOGROUP = "";
                                adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["STORE_PROVINCE_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_CITY_NAME"]).Split('-')[0] + "" +
                                ConvertUtil.ToString(dtMain.Rows[0]["STORE_COUNTY_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_DETAILED_ADDRESS"]).Split('-')[0], 60);
                                adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["STORE_PROVINCE_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_CITY_NAME"]).Split('-')[0] + "" +
                                ConvertUtil.ToString(dtMain.Rows[0]["STORE_COUNTY_NAME"]).Split('-')[0] + "" + ConvertUtil.ToString(dtMain.Rows[0]["STORE_DETAILED_ADDRESS"]).Split('-')[0], 60);
                                adrc.STR_SUPPL2 = "";
                                adrc.STR_SUPPL3 = "";
                                adrc.LOCATION = "";
                                adrc.POST_CODE1 = "";
                                adrc.LANGU = "ZH";
                                adrc.TEL_NUMBER = "";
                                adrc.FAX_NUMBER = "";
                                adrc.EXTENSION1 = "";
                                adrc.EXTENSION2 = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                                //KNA1
                                kna1 = new KNA1();
                                kna1.KUNNR = KUNNR;
                                kna1.KTOKD = "SG05";
                                kna1.STCD5 = "";
                                kna1.STKZU = "";
                                kna1.UMSA1 = "0";
                                kna1.UWAER = "";
                                kna1.UMJAH = "";
                                kna1.KATR10 = "";
                                kna1.KDKG1 = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                                //KNVI
                                knvi = new KNVI();
                                knvi.TAXKD = "1";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                                //KNVV
                                knvv = new KNVV();
                                VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                                knvv.VKORG = companycode;//按照公司循环
                                knvv.VTWEG = VTWEG;
                                knvv.SPART = Division;
                                knvv.BZIRK = "";
                                knvv.VKBUR = "";
                                knvv.VKGRP = "";
                                knvv.KDGRP = "";
                                knvv.WAERS = "";
                                knvv.KONDA = "";
                                knvv.KALKS = "";
                                knvv.VERSG = "";
                                knvv.LPRIO = "0";
                                knvv.KZAZU = "";
                                knvv.VSBED = "C1";
                                knvv.KZTLF = "B";
                                knvv.ANTLF = "0";
                                knvv.ZTERM = "";
                                knvv.KTGRD = "01";
                                knvv.KVGR5 = "";
                                knvv.AUFSD = "";
                                knvv.ZOPEN_DATE = "";
                                knvv.ZCLOSE_DATE = "";
                                knvv.ZSHOP_AREA = "0";
                                knvv.ZCUST_GL3 = "";
                                knvv.ZXY_POS_ID = "";
                                knvv.ZCTR_NO = "";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                                knvp = new KNVP();
                                knvp.KUNNR = KUNNR;
                                knvp.PARVW = "WE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                                //调用RFC
                                dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                                foreach (DataRow item in dt.Rows)
                                {
                                    SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                    SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                    //添加执行日志
                                    string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + "WC";
                                    _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                                }
                                if (SAPTYPE == "S")
                                {
                                    WC = SAPMESSAGE.PadLeft(10, '0');
                                }
                            }
                        }
                    }

                    #endregion

                    #region Ship to /Address
                    string FLAG2 = ConvertUtil.ToString(dtMain.Rows[0]["FLAG2"]); //1表示有修改，新建编号
                    if (FLAG2 == "1")
                    {
                        if ((type_of_account.Split('\t').Length == 1 && ConvertUtil.ToString(type_of_account).Contains("Watch"))
                        || (type_of_account.Split('\t').Length == 1 && ConvertUtil.ToString(type_of_account).Contains("Spare Part")))
                        {
                            foreach (string of_account in type_of_accountS.Split('\t'))
                            {
                                foreach (string company in usercompany.Split('\t'))
                                {
                                    if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                                    {
                                        continue;
                                    }
                                    companycode = ConvertUtil.ToString(company.Split('-')[0]);
                                    PARVW = "WE";
                                    RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, PARVW);
                                    if (!string.IsNullOrEmpty(RuntMassage))
                                    {
                                        if (PARVW == "WE")
                                            WE = RuntMassage;
                                        continue;
                                    }
                                    //获取SAP号
                                    string ShipAddressNo = SAP_ShipAddressNo(companycode, Division, of_account, CustomerCode);
                                    if (ShipAddressNo == "0004005770")
                                    {
                                        continue;
                                    }

                                    KUNNR = string.IsNullOrEmpty(WE) ? "" : WE;
                                    IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                                    //ADRC
                                    adrc = new ADRC();
                                    //获取Reg. Struct. Grp.
                                    _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                                    REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY"])));
                                    adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COMPANY_NAME"]);
                                    adrc.NAME2 = "";
                                    adrc.NAME3 = "";
                                    adrc.NAME4 = "";
                                    adrc.SORT1 = "";
                                    adrc.COUNTRY = "CN";
                                    adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PROVINCE"]);
                                    adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-')[1] : "";
                                    adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-')[0] : "";
                                    adrc.REGIOGROUP = REGIOGROUP;
                                    adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                    adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                    adrc.STR_SUPPL2 = ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON2"]);
                                    adrc.STR_SUPPL3 = "";
                                    adrc.LOCATION = "";
                                    adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_ZIP"]), 6);
                                    adrc.LANGU = "ZH";
                                    adrc.TEL_NUMBER = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PHONE_NUMBER"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["MOBILE_PHONE1"]);
                                    adrc.FAX_NUMBER = "";
                                    adrc.EXTENSION1 = "";
                                    adrc.EXTENSION2 = "";
                                    //ds.Tables.Add(ListToDatatable.ListToDataTable<ADRC>(adrcs));
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                                    //KNA1
                                    kna1 = new KNA1();
                                    kna1.KUNNR = KUNNR;
                                    kna1.KTOKD = "SG05";
                                    kna1.STCD5 = "";
                                    kna1.STKZU = "";
                                    kna1.UMSA1 = "0";
                                    kna1.UWAER = "";
                                    kna1.UMJAH = "";
                                    kna1.KATR10 = "";
                                    kna1.KDKG1 = "";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                                    //KNVI
                                    knvi = new KNVI();
                                    knvi.TAXKD = "1";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                                    //KNVV
                                    knvv = new KNVV();
                                    VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                                    KZAZU = ConvertUtil.ToString(of_account) == "90" ? "" : (ConvertUtil.ToString(of_account) == "01" && (ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "39" || ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "40") ? "" : "X");
                                    KZTLF = ConvertUtil.ToString(of_account) == "01" ? "B" : "";
                                    ANTLF = ConvertUtil.ToString(of_account) == "01" ? "0" : "9";
                                    knvv.VKORG = companycode;//按照公司循环
                                    knvv.VTWEG = VTWEG;
                                    knvv.SPART = Division;
                                    knvv.BZIRK = "";
                                    knvv.VKBUR = "";
                                    knvv.VKGRP = "";
                                    knvv.KDGRP = "";
                                    knvv.WAERS = "";
                                    knvv.KONDA = "";
                                    knvv.KALKS = "";
                                    knvv.VERSG = "";
                                    knvv.LPRIO = "0";
                                    knvv.KZAZU = KZAZU;
                                    knvv.VSBED = "C1";
                                    knvv.KZTLF = KZTLF;
                                    knvv.ANTLF = ANTLF;
                                    knvv.ZTERM = "";
                                    knvv.KTGRD = "01";
                                    knvv.KVGR5 = "";
                                    knvv.AUFSD = "";
                                    knvv.ZOPEN_DATE = "";
                                    knvv.ZCLOSE_DATE = "";
                                    knvv.ZSHOP_AREA = "0";
                                    knvv.ZCUST_GL3 = "";
                                    knvv.ZXY_POS_ID = "";
                                    knvv.ZCTR_NO = "";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                                    knvp = new KNVP();
                                    knvp.KUNNR = KUNNR;
                                    knvp.PARVW = "WE";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                                    //调用RFC
                                    dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                                    foreach (DataRow item in dt.Rows)
                                    {
                                        SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                        SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                        //添加执行日志
                                        string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + PARVW;
                                        _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                                    }
                                    if (SAPTYPE == "S")
                                    {
                                        if (PARVW == "WE")
                                            WE = SAPMESSAGE.PadLeft(10, '0');
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (string of_account in type_of_accountS.Split('\t'))
                            {
                                foreach (string company in usercompany.Split('\t'))
                                {
                                    if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                                    {
                                        continue;
                                    }
                                    //string cs = (type_of_account.Split('\t').Length == 0 && ConvertUtil.ToString(type_of_account).Contains("Customer Service")) ? "0004005770" : "";
                                    companycode = ConvertUtil.ToString(company.Split('-')[0]);
                                    PARVW = "Z5";
                                    RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, PARVW);
                                    if (!string.IsNullOrEmpty(RuntMassage))
                                    {
                                        if (PARVW == "Z5")
                                            Z5 = RuntMassage;
                                        continue;
                                    }

                                    KUNNR = string.IsNullOrEmpty(Z5) ? "" : Z5;
                                    IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                                    //ADRC
                                    adrc = new ADRC();
                                    //获取Reg. Struct. Grp.
                                    _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                                    REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY"])));
                                    adrc.NAME1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COMPANY_NAME"]);
                                    adrc.NAME2 = "";
                                    adrc.NAME3 = "";
                                    adrc.NAME4 = "";
                                    adrc.SORT1 = "";
                                    adrc.COUNTRY = "CN";
                                    adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PROVINCE"]);
                                    adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_CITY_NAME"]).Split('-')[1] : "";
                                    adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_COUNTY_NAME"]).Split('-')[0] : "";
                                    adrc.REGIOGROUP = REGIOGROUP;
                                    adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                    adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_DETAILED_ADDRESS"]), 60);
                                    adrc.STR_SUPPL2 = ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON1"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["CONTRACT_PERSON2"]);
                                    adrc.STR_SUPPL3 = "";
                                    adrc.LOCATION = "";
                                    adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_ZIP"]), 6);
                                    adrc.LANGU = "ZH";
                                    adrc.TEL_NUMBER = ConvertUtil.ToString(dtMain.Rows[0]["RECIVING_PHONE_NUMBER"]) + "/" + ConvertUtil.ToString(dtMain.Rows[0]["MOBILE_PHONE1"]);
                                    adrc.FAX_NUMBER = "";
                                    adrc.EXTENSION1 = "";
                                    adrc.EXTENSION2 = "";
                                    //ds.Tables.Add(ListToDatatable.ListToDataTable<ADRC>(adrcs));
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                                    //KNA1
                                    kna1 = new KNA1();
                                    kna1.KUNNR = KUNNR;
                                    kna1.KTOKD = "SG05";
                                    kna1.STCD5 = "";
                                    kna1.STKZU = "";
                                    kna1.UMSA1 = "0";
                                    kna1.UWAER = "";
                                    kna1.UMJAH = "";
                                    kna1.KATR10 = "";
                                    kna1.KDKG1 = "";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                                    //KNVI
                                    knvi = new KNVI();
                                    knvi.TAXKD = "1";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                                    //KNVV
                                    knvv = new KNVV();
                                    VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                                    KZAZU = ConvertUtil.ToString(of_account) == "90" ? "" : (ConvertUtil.ToString(of_account) == "01" && (ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "39" || ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "40") ? "" : "X");
                                    KZTLF = ConvertUtil.ToString(of_account) == "01" ? "B" : "";
                                    ANTLF = ConvertUtil.ToString(of_account) == "01" ? "0" : "9";
                                    knvv.VKORG = companycode;//按照公司循环
                                    knvv.VTWEG = VTWEG;
                                    knvv.SPART = Division;
                                    knvv.BZIRK = "";
                                    knvv.VKBUR = "";
                                    knvv.VKGRP = "";
                                    knvv.KDGRP = "";
                                    knvv.WAERS = "";
                                    knvv.KONDA = "";
                                    knvv.KALKS = "";
                                    knvv.VERSG = "";
                                    knvv.LPRIO = "0";
                                    knvv.KZAZU = KZAZU;
                                    knvv.VSBED = "C1";
                                    knvv.KZTLF = KZTLF;
                                    knvv.ANTLF = ANTLF;
                                    knvv.ZTERM = "";
                                    knvv.KTGRD = "01";
                                    knvv.KVGR5 = "";
                                    knvv.AUFSD = "";
                                    knvv.ZOPEN_DATE = "";
                                    knvv.ZCLOSE_DATE = "";
                                    knvv.ZSHOP_AREA = "0";
                                    knvv.ZCUST_GL3 = "";
                                    knvv.ZXY_POS_ID = "";
                                    knvv.ZCTR_NO = "";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                                    knvp = new KNVP();
                                    knvp.KUNNR = KUNNR;
                                    knvp.PARVW = "WE";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                                    //调用RFC
                                    dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                                    foreach (DataRow item in dt.Rows)
                                    {
                                        SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                        SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                        //添加执行日志
                                        string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + PARVW;
                                        _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                                    }
                                    if (SAPTYPE == "S")
                                    {
                                        if (PARVW == "Z5")
                                            Z5 = SAPMESSAGE.PadLeft(10, '0');
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    #region Sold to

                    foreach (string of_account in type_of_accountS.Split('\t'))
                    {
                        foreach (string company in usercompany.Split('\t'))
                        {
                            if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                            {
                                continue;
                            }
                            companycode = ConvertUtil.ToString(company.Split('-')[0]);
                            RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, "AG");
                            if (!string.IsNullOrEmpty(RuntMassage))
                            {
                                AG = RuntMassage;
                                continue;
                            }
                            KUNNR = string.IsNullOrEmpty(AG) ? CustomerCode : AG;
                            IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                            //ADR6
                            adr6 = new ADR6();
                            adr6.SMTP_ADDR = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_EMAIL"]);
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adr6));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADR6", rfcFunc);

                            //ADRC
                            adrc = new ADRC();
                            _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                            REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY"])));
                            adrc.NAME1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_CN"]), 40);
                            adrc.NAME2 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_CN"]), 40);
                            adrc.NAME3 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_EN"]), 40);
                            adrc.NAME4 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_EN"]), 40);
                            adrc.SORT1 = "";
                            adrc.COUNTRY = "CN";
                            adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_PROVINCE"]);
                            adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY_NAME"]).Split('-')[1] : "";
                            adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_COUNTY_NAME"]).Split('-')[0] : "";
                            adrc.REGIOGROUP = REGIOGROUP;
                            adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["STORE_ADDRESS"]), 60);
                            adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["STORE_ADDRESS"]), 60);
                            adrc.STR_SUPPL2 = "";
                            adrc.STR_SUPPL3 = "";
                            adrc.LOCATION = "";
                            adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["ZIP_CODE"]), 6);
                            adrc.LANGU = "ZH";
                            adrc.TEL_NUMBER = "";
                            adrc.FAX_NUMBER = "";
                            adrc.EXTENSION1 = "";
                            adrc.EXTENSION2 = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                            //KNA1
                            kna1 = new KNA1();
                            kna1.KUNNR = KUNNR;
                            kna1.KTOKD = "SG01";
                            kna1.STCD5 = "";
                            kna1.STKZU = "";
                            kna1.UMSA1 = "0";
                            kna1.UWAER = "";
                            kna1.UMJAH = "";
                            kna1.KATR10 = ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMERCATEGORY2"]);
                            kna1.KDKG1 = ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMERCATEGORY1"]);
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                            //KNB1
                            knb1 = new KNB1();
                            knb1.BUKRS = companycode;//按照公司循环
                            knb1.AKONT = "0013001100";
                            knb1.ALTKN = "";
                            knb1.ZUAWA = "001";
                            knb1.FDGRV = "ZD02";
                            knb1.ZTERM = "Z000";
                            knb1.XZVER = "X";
                            knb1.ZWELS = "";
                            knb1.XAUSZ = "2";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knb1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNB1", rfcFunc);

                            //KNVI
                            knvi = new KNVI();
                            knvi.TAXKD = "1";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                            //KNVV
                            knvv = new KNVV();
                            VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                            string KONDA = ConvertUtil.ToString(of_account) == "01" ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_WATCH"]) : (string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_CS_OR_SPARE_PARTS"])) ? ConvertUtil.ToString(dtMain.Rows[0]["LEVELOFSP"]).Split('-')[0] : ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_CS_OR_SPARE_PARTS"]));
                            string AUFSD = (KONDA == "99" && ConvertUtil.ToString(of_account) == "01") ? "C1" : "";
                            knvv.VKORG = companycode;//按照公司循环
                            knvv.VTWEG = VTWEG;
                            knvv.SPART = Division;
                            knvv.BZIRK = ConvertUtil.ToString(dtMain.Rows[0]["SALES_AREA"]);
                            knvv.VKBUR = "48" + Division;
                            knvv.VKGRP = ConvertUtil.ToString(dtMain.Rows[0]["SALESMAN_OR_CODE_NAME"]);
                            knvv.KDGRP = ConvertUtil.ToString(dtMain.Rows[0]["SALES_TERRITORY"]);
                            knvv.WAERS = "CNY";
                            knvv.KONDA = KONDA;
                            knvv.KALKS = "1";
                            knvv.VERSG = "1";
                            knvv.LPRIO = "0";
                            knvv.KZAZU = "";
                            knvv.VSBED = "C1";
                            knvv.KZTLF = "";
                            knvv.ANTLF = "0";
                            knvv.ZTERM = "Z000";
                            knvv.KTGRD = "01";
                            knvv.KVGR5 = ConvertUtil.ToString(dtMain.Rows[0]["STORE_TYPE"]);
                            if (of_account == "90")// Spare Part    Customer Service
                                knvv.KVGR5 = "X09";
                            knvv.AUFSD = AUFSD;
                            knvv.ZOPEN_DATE = string.IsNullOrEmpty(ConvertUtil.ToString(ConvertUtil.ToDateTime(dtMain.Rows[0]["PLANNED_OPENNING_MONTH_DATE"]).ToString("yyyyMMdd")))
                                ? SAP_ZOPEN_DATE(companycode, Division, of_account, CustomerCode) : ConvertUtil.ToString(ConvertUtil.ToDateTime(dtMain.Rows[0]["PLANNED_OPENNING_MONTH_DATE"]).ToString("yyyyMMdd"));
                            knvv.ZCLOSE_DATE = null; //ConvertUtil.ToString(ConvertUtil.ToDateTime(dtMain.Rows[0]["OPENNING_YEAR_MONTH_DATE"]).ToString("yyyyMMdd"));
                            knvv.ZSHOP_AREA = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["SURFACE"])) ? "0" : ConvertUtil.ToString(dtMain.Rows[0]["SURFACE"]);
                            knvv.ZCUST_GL3 = "";
                            knvv.ZXY_POS_ID = ConvertUtil.ToString(dtMain.Rows[0]["OTHER2"]);
                            knvv.ZCTR_NO = ConvertUtil.ToString(dtMain.Rows[0]["CATEGORY2"]);
                            knvv.KVGR3 = of_account == "01" ? "" : ConvertUtil.ToString(dtMain.Rows[0]["LEVEL_OF_SPARE_PARTS_AUTHORIZATION"]) == "Omega-special" ? "D" : ConvertUtil.ToString(dtMain.Rows[0]["LEVEL_OF_SPARE_PARTS_AUTHORIZATION"]);
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                            Z2 = "";
                            Z6 = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["RELATED_SOLD_TO_NO"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["RELATED_SOLD_TO_NO"]).PadLeft(10, '0');
                            RE = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["BILLTONO"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["BILLTONO"]).PadLeft(10, '0');
                            RG = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["CODE"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["CODE"]).PadLeft(10, '0');

                            //KNVP AG -- Sold-to party
                            knvp = new KNVP();
                            knvp.KUNNR = KUNNR;
                            knvp.PARVW = "AG";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);

                            //RE -- Bill-to party
                            if (!string.IsNullOrEmpty(RE))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = RE;
                                knvp.PARVW = "RE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //RG -- Payer
                            if (!string.IsNullOrEmpty(RG))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = RG;
                                knvp.PARVW = "RG";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //WE -- Ship-to party,If customer only for customer service, then ship-to assign as 0004005770 - DUMMY 只勾选CS = 0004005770
                            string ShipAddressNo = SAP_ShipAddressNo(companycode, Division, of_account, CustomerCode);
                            if (ConvertUtil.ToString(type_of_account).Contains("Customer Service") && !string.IsNullOrEmpty(ShipAddressNo))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = ShipAddressNo;
                                knvp.PARVW = "WE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }
                            else
                            {
                                if (ShipAddressNo == "0004005770" || FLAG2 != "1")
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = ShipAddressNo;
                                    knvp.PARVW = "WE";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                                else if (!string.IsNullOrEmpty(WE))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = WE;
                                    knvp.PARVW = "WE";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }

                            //WC -- Warranty card addr.,Only for Watch customer
                            if (FLAG3 != "1" && ConvertUtil.ToString(type_of_account).Contains("Watch"))
                            {
                                string WarrentyCardNo = SAP_WarrentyCardNo(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(WarrentyCardNo))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = WarrentyCardNo;
                                    knvp.PARVW = "WC";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }
                            else if (!string.IsNullOrEmpty(WC) && ConvertUtil.ToString(type_of_account).Contains("Watch"))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = WC;
                                knvp.PARVW = "WC";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //Z1 -- Invoice mailing add, Watch and spare part invoice sending address
                            if (FLAG1 != "1" && (ConvertUtil.ToString(type_of_account).Contains("Spare Part") || ConvertUtil.ToString(type_of_account).Contains("Watch")))
                            {
                                string InvoiceAddressNO_Z1 = SAP_InvoiceAddressNO_Z1(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(InvoiceAddressNO_Z1))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = InvoiceAddressNO_Z1;
                                    knvp.PARVW = "Z1";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }
                            else if (!string.IsNullOrEmpty(Z1) && (ConvertUtil.ToString(type_of_account).Contains("Spare Part") || ConvertUtil.ToString(type_of_account).Contains("Watch")))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z1;
                                knvp.PARVW = "Z1";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //Z7 -- Inv.Mail.Addr.Servi,Customer Service invoice sending address
                            if (FLAG1 != "1" && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                string InvoiceAddressNO_Z7 = SAP_InvoiceAddressNO_Z7(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(InvoiceAddressNO_Z7))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = InvoiceAddressNO_Z7;
                                    knvp.PARVW = "Z7";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }
                            else if (!string.IsNullOrEmpty(Z7) && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z7;
                                knvp.PARVW = "Z7";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //Z2 -- Previous cust. numb, Only for watch, capture orginal customer store # when one store convert from distributor A to B
                            if (!string.IsNullOrEmpty(Z2))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z2;
                                knvp.PARVW = "Z2";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //Z5 -- CS repair ship to,Customer Service goods sending address
                            if (FLAG2 != "1" && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                string ShipAddressNo_Z5 = SAP_ShipAddressNo_Z5(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(ShipAddressNo_Z5))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = ShipAddressNo_Z5;
                                    knvp.PARVW = "Z5";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }
                            else if (!string.IsNullOrEmpty(Z5) && ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z5;
                                knvp.PARVW = "Z5";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //Z6 -- Related Sold-to,For CS process, some pos store need to link to watch pos
                            if (!string.IsNullOrEmpty(Z6))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z6;
                                knvp.PARVW = "Z6";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //调用RFC
                            dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                            foreach (DataRow item in dt.Rows)
                            {
                                SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                //添加执行日志
                                string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + "AG";
                                _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                            }
                            if (SAPTYPE == "S")
                            {
                                AG = SAPMESSAGE.PadLeft(10, '0');
                            }
                        }
                    }

                    #endregion

                    if (SAPTYPE == "S" || !string.IsNullOrEmpty(AG))
                    {
                        //写入成功，更新返回标识及返回信息
                        _rfccom.UpdateSyncSucess("COM_INTERFACE_MASTERDATA", "FORMID", FORMID, AG);
                    }
                    else
                    {
                        //执行错误，更新执行次数
                        _rfccom.UpdateSyncError("COM_INTERFACE_MASTERDATA", "FORMID", FORMID);
                    }

                    #endregion
                }
                else if (cma_type == "Close Account")
                {
                    #region 操作类型（关闭）

                    #region Sold to

                    foreach (string of_account in type_of_accountS.Split('\t'))
                    {
                        foreach (string company in usercompany.Split('\t'))
                        {
                            if (!is_SalesArea(dtSales_Area, company.Split('-')[0], Division, of_account))
                            {
                                continue;
                            }
                            companycode = ConvertUtil.ToString(company.Split('-')[0]);
                            RuntMassage = GetMassageValue(dtInterface_Log, companycode, of_account, "AG");
                            if (!string.IsNullOrEmpty(RuntMassage))
                            {
                                AG = RuntMassage;
                                continue;
                            }
                            KUNNR = string.IsNullOrEmpty(AG) ? CustomerCode : AG;
                            IRfcFunction rfcFunc = repo.CreateFunction(CreateFunction);
                            //ADR6
                            adr6 = new ADR6();
                            adr6.SMTP_ADDR = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_EMAIL"]);
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adr6));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADR6", rfcFunc);

                            //ADRC
                            adrc = new ADRC();
                            _sql = @"select EXT01 from MD_CITY where CITYCODE=@CITYCODE ";
                            REGIOGROUP = ConvertUtil.ToString(DataAccess.Instance("BizDB").ExecuteScalar(_sql, ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY"])));
                            adrc.NAME1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_CN"]), 40);
                            adrc.NAME2 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_CN"]), 40);
                            adrc.NAME3 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_EN"]), 40);
                            adrc.NAME4 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_NAME_EN"]), 40);
                            adrc.SORT1 = "";
                            adrc.COUNTRY = "CN";
                            adrc.REGION = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_PROVINCE"]);
                            adrc.CITY1 = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY_NAME"]).Split('-').Length > 1 ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CITY_NAME"]).Split('-')[1] : "";
                            adrc.CITY2 = ConvertUtil.ToString(dtMain.Rows[0]["SHOP_COUNTY_NAME"]).Split('-').Length > 0 ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_COUNTY_NAME"]).Split('-')[0] : "";
                            adrc.REGIOGROUP = REGIOGROUP;
                            adrc.STREET = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["STORE_ADDRESS"]), 60);
                            adrc.STR_SUPPL1 = _rfccom.SubStringRear(ConvertUtil.ToString(dtMain.Rows[0]["STORE_ADDRESS"]), 60);
                            adrc.STR_SUPPL2 = "";
                            adrc.STR_SUPPL3 = "";
                            adrc.LOCATION = "";
                            adrc.POST_CODE1 = _rfccom.SubStringBefore(ConvertUtil.ToString(dtMain.Rows[0]["ZIP_CODE"]), 6);
                            adrc.LANGU = "ZH";
                            adrc.TEL_NUMBER = "";
                            adrc.FAX_NUMBER = "";
                            adrc.EXTENSION1 = "";
                            adrc.EXTENSION2 = "";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(adrc));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "ADRC", rfcFunc);

                            //KNA1
                            kna1 = new KNA1();
                            kna1.KUNNR = KUNNR;
                            kna1.KTOKD = "SG01";
                            kna1.STCD5 = "";
                            kna1.STKZU = "";
                            kna1.UMSA1 = "0";
                            kna1.UWAER = "";
                            kna1.UMJAH = "";
                            kna1.KATR10 = ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMERCATEGORY2"]);
                            kna1.KDKG1 = ConvertUtil.ToString(dtMain.Rows[0]["CUSTOMERCATEGORY1"]);
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(kna1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNA1", rfcFunc);

                            //KNB1
                            knb1 = new KNB1();
                            knb1.BUKRS = companycode;//按照公司循环
                            knb1.AKONT = "0013001100";
                            knb1.ALTKN = "";
                            knb1.ZUAWA = "001";
                            knb1.FDGRV = "ZD02";
                            knb1.ZTERM = "Z000";
                            knb1.XZVER = "X";
                            knb1.ZWELS = "";
                            knb1.XAUSZ = "2";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knb1));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNB1", rfcFunc);

                            //KNVI
                            knvi = new KNVI();
                            knvi.TAXKD = "1";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvi));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVI", rfcFunc);

                            //KNVV
                            knvv = new KNVV();
                            VTWEG = ConvertUtil.ToString(dtMain.Rows[0]["BRAND"]) == "30" ? "94" : of_account;//根据类型循环
                            string KONDA = ConvertUtil.ToString(of_account) == "01" ? ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_WATCH"]) : (string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_CS_OR_SPARE_PARTS"])) ? ConvertUtil.ToString(dtMain.Rows[0]["LEVELOFSP"]).Split('-')[0] : ConvertUtil.ToString(dtMain.Rows[0]["SHOP_CLASS_FOR_CS_OR_SPARE_PARTS"]));
                            string AUFSD = "C2";
                            //关闭原因
                            string involuntary = ConvertUtil.ToString(dtMain.Rows[0]["INVOLUNTARY"]);
                            string involuntarycode = involuntary == "Landlord Issue" ? "X01" : involuntary == "Retaller Issue" ? "X02" : involuntary == "Others" ? "X03" : "";
                            string voluntary = ConvertUtil.ToString(dtMain.Rows[0]["VOLUNTARY"]);
                            string voluntarycode = voluntary == "Poor Sales Performance" ? "V01" : voluntary == "Business Cooperation Issue" ? "V02" :
                                voluntary == "Brand Strategy Change" ? "V03" : voluntary == "Transfer to CFS" ? "V04" : voluntary == "Others" ? "V05" : "";
                            string KVGR4 = string.IsNullOrEmpty(involuntarycode) ? voluntarycode : involuntarycode;
                            //按照公司循环
                            knvv.VKORG = companycode;
                            knvv.VTWEG = VTWEG;
                            knvv.SPART = Division;
                            knvv.BZIRK = ConvertUtil.ToString(dtMain.Rows[0]["SALES_AREA"]);
                            knvv.VKBUR = "48" + Division;
                            knvv.VKGRP = ConvertUtil.ToString(dtMain.Rows[0]["SALESMAN_OR_CODE_NAME"]);
                            knvv.KDGRP = ConvertUtil.ToString(dtMain.Rows[0]["SALES_TERRITORY"]);
                            knvv.WAERS = "CNY";
                            knvv.KONDA = KONDA;
                            knvv.KALKS = "1";
                            knvv.VERSG = "1";
                            knvv.LPRIO = "0";
                            knvv.KZAZU = "";
                            knvv.VSBED = "C1";
                            knvv.KZTLF = "";
                            knvv.ANTLF = "0";
                            knvv.ZTERM = "Z000";
                            knvv.KTGRD = "01";
                            knvv.KVGR5 = ConvertUtil.ToString(dtMain.Rows[0]["STORE_TYPE"]);
                            if (of_account == "90")// Spare Part    Customer Service
                                knvv.KVGR5 = "X09";
                            knvv.AUFSD = AUFSD;
                            knvv.ZOPEN_DATE = SAP_ZOPEN_DATE(companycode, Division, of_account, CustomerCode);
                            knvv.ZCLOSE_DATE = ConvertUtil.ToString(ConvertUtil.ToDateTime(dtMain.Rows[0]["OPENNING_YEAR_MONTH_DATE"]).ToString("yyyyMMdd"));
                            knvv.ZSHOP_AREA = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["SURFACE"])) ? "0" : ConvertUtil.ToString(dtMain.Rows[0]["SURFACE"]);
                            knvv.ZCUST_GL3 = "";
                            knvv.ZXY_POS_ID = ConvertUtil.ToString(dtMain.Rows[0]["OTHER2"]);
                            knvv.ZCTR_NO = ConvertUtil.ToString(dtMain.Rows[0]["CATEGORY2"]);
                            knvv.KVGR3 = of_account == "01" ? "" : ConvertUtil.ToString(dtMain.Rows[0]["LEVEL_OF_SPARE_PARTS_AUTHORIZATION"]) == "Omega-special" ? "D" : ConvertUtil.ToString(dtMain.Rows[0]["LEVEL_OF_SPARE_PARTS_AUTHORIZATION"]);
                            knvv.LOEVM = "X";
                            knvv.KVGR4 = KVGR4;
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvv));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVV", rfcFunc);

                            Z2 = "";
                            Z6 = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["RELATED_SOLD_TO_NO"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["RELATED_SOLD_TO_NO"]).PadLeft(10, '0');
                            RE = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["BILLTONO"])) ? SAP_BilltoNo(companycode, Division, of_account, CustomerCode) : ConvertUtil.ToString(dtMain.Rows[0]["BILLTONO"]).PadLeft(10, '0');
                            RG = string.IsNullOrEmpty(ConvertUtil.ToString(dtMain.Rows[0]["CODE"])) ? "" : ConvertUtil.ToString(dtMain.Rows[0]["CODE"]).PadLeft(10, '0');

                            //KNVP AG -- Sold-to party
                            knvp = new KNVP();
                            knvp.KUNNR = KUNNR;
                            knvp.PARVW = "AG";
                            RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                            rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);


                            //RE -- Bill-to party
                            if (!string.IsNullOrEmpty(RE))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = RE;
                                knvp.PARVW = "RE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //RG -- Payer
                            if (!string.IsNullOrEmpty(RG))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = RG;
                                knvp.PARVW = "RG";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //WE -- Ship-to party,If customer only for customer service, then ship-to assign as 0004005770 - DUMMY 只勾选CS = 0004005770
                            string ShipAddressNo = SAP_ShipAddressNo(companycode, Division, of_account, CustomerCode);
                            if (!string.IsNullOrEmpty(ShipAddressNo))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = ShipAddressNo;
                                knvp.PARVW = "WE";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //WC -- Warranty card addr.,Only for Watch customer
                            if (ConvertUtil.ToString(type_of_account).Contains("Watch"))
                            {
                                string WarrentyCardNo = SAP_WarrentyCardNo(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(WarrentyCardNo))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = WarrentyCardNo;
                                    knvp.PARVW = "WC";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }

                            //Z1 -- Invoice mailing add, Watch and spare part invoice sending address
                            if ((ConvertUtil.ToString(type_of_account).Contains("Spare Part") || ConvertUtil.ToString(type_of_account).Contains("Watch")))
                            {
                                string InvoiceAddressNO_Z1 = SAP_InvoiceAddressNO_Z1(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(InvoiceAddressNO_Z1))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = InvoiceAddressNO_Z1;
                                    knvp.PARVW = "Z1";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }

                            //Z7 -- Inv.Mail.Addr.Servi,Customer Service invoice sending address
                            if (ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                string InvoiceAddressNO_Z7 = SAP_InvoiceAddressNO_Z7(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(InvoiceAddressNO_Z7))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = InvoiceAddressNO_Z7;
                                    knvp.PARVW = "Z7";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }

                            //Z2 -- Previous cust. numb, Only for watch, capture orginal customer store # when one store convert from distributor A to B
                            if (!string.IsNullOrEmpty(Z2))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z2;
                                knvp.PARVW = "Z2";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //Z5 -- CS repair ship to,Customer Service goods sending address
                            if (ConvertUtil.ToString(type_of_account).Contains("Customer Service"))
                            {
                                string ShipAddressNo_Z5 = SAP_ShipAddressNo_Z5(companycode, Division, of_account, CustomerCode);
                                if (!string.IsNullOrEmpty(ShipAddressNo_Z5))
                                {
                                    knvp = new KNVP();
                                    knvp.KUNNR = ShipAddressNo_Z5;
                                    knvp.PARVW = "Z5";
                                    RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                    rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                                }
                            }

                            //Z6 -- Related Sold-to,For CS process, some pos store need to link to watch pos
                            if (!string.IsNullOrEmpty(Z6))
                            {
                                knvp = new KNVP();
                                knvp.KUNNR = Z6;
                                knvp.PARVW = "Z6";
                                RFCDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(knvp));
                                rfcFunc = rfc.rfcSetValue(RFCDict, "KNVP", rfcFunc);
                            }

                            //调用RFC
                            dt = rfc.rfcFunction(destination, rfcFunc, OutTableName);
                            foreach (DataRow item in dt.Rows)
                            {
                                SAPTYPE = ConvertUtil.ToString(item["TYPE"]);
                                SAPMESSAGE = ConvertUtil.ToString(item["MESSAGE"]);
                                //添加执行日志
                                string row = "Company:" + companycode + " & of_account:" + of_account + " & PARVW:" + "AG";
                                _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, row, "RFC", SAPTYPE, SAPMESSAGE);
                            }
                            if (SAPTYPE == "S")
                            {
                                AG = SAPMESSAGE.PadLeft(10, '0');
                            }
                        }
                    }

                    #endregion

                    if (SAPTYPE == "S" || !string.IsNullOrEmpty(AG))
                    {
                        //写入成功，更新返回标识及返回信息
                        _rfccom.UpdateSyncSucess("COM_INTERFACE_MASTERDATA", "FORMID", FORMID, AG);
                    }
                    else
                    {
                        //执行错误，更新执行次数
                        _rfccom.UpdateSyncError("COM_INTERFACE_MASTERDATA", "FORMID", FORMID);
                    }

                    #endregion
                }

            }
            catch (Exception ex)
            {
                //执行错误，更新执行次数
                _rfccom.UpdateSyncError("COM_INTERFACE_MASTERDATA", "FORMID", FORMID);
                //添加执行日志
                _rfccom.Insert_Interface_Log(FORMID, PROCESSNAME, INCIDENT, "", "RFC", "E", ex.Message);
                LogUtil.Error(ex.Message);
            }
            return dt;
        }

        /// <summary>
        /// 验证是否满足接口写入
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <returns></returns>
        public bool is_SalesArea(DataTable dt, string Companycode, string Division, string DistributionChannel)
        {
            bool isbool = false;
            try
            {
                DataRow[] rows = dt.Select("CompanyCode='" + Companycode + "' and BRAND='" + Division + "' and DistributionChannel='" + DistributionChannel + "' ");
                if (rows.Length > 0)
                {
                    return isbool = true;
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
                return isbool = false;
            }
            return isbool;
        }

        /// <summary>
        /// 重试时判断其中的类型是否成功，若成功直接取成功号
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="Companycode"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="Parvw"></param>
        /// <returns></returns>
        public string GetMassageValue(DataTable dt, string Companycode, string DistributionChannel, string Parvw)
        {
            string _massage = string.Empty;
            try
            {
                DataRow[] rows = dt.Select("ROWID='Company:" + Companycode + " & of_account:" + DistributionChannel + " & PARVW:" + Parvw + "'");
                if (rows.Length > 0)
                {
                    return _massage = ConvertUtil.ToString(rows[0]["MESSAGE"]);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _massage;
        }

        /// <summary>
        /// 获取SAP 开发票信息 编号
        /// </summary>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="CustomerCode"></param>
        /// <returns></returns>
        public string SAP_BilltoNo(string Companycode, string Division, string DistributionChannel, string CustomerCode)
        {
            string _str = string.Empty;
            try
            {
                _sql = @"select top 1 a.Kunn2  from SAP_KNVP a 
                inner join SAP_KNA1 b on a.kunn2 = b.kunnr 
                inner join SAP_ADRC c on b.Adrnr = c.addrnumber 
                where parvw = 'RE' and a.VKORG=@VKORG and SPART=@SPART and VTWEG=@VTWEG and a.Kunnr=@Kunnr  
                order by vkorg ";
                _str = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, Companycode, Division, DistributionChannel, CustomerCode));
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _str;
        }

        /// <summary>
        /// 获取SAP 发票邮寄地址 CS
        /// </summary>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="CustomerCode"></param>
        /// <returns></returns>
        public string SAP_InvoiceAddressNO_Z7(string Companycode, string Division, string DistributionChannel, string CustomerCode)
        {
            string _str = string.Empty;
            try
            {
                _sql = @"select top 1 a.Kunn2 from SAP_KNVP a 
                inner join SAP_KNA1 b on a.kunn2 = b.kunnr 
                inner join SAP_ADRC c on b.Adrnr = c.addrnumber
                where parvw = 'Z7' and a.VKORG=@VKORG and SPART=@SPART and VTWEG=@VTWEG and a.Kunnr=@Kunnr order by vkorg ";
                _str = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, Companycode, Division, DistributionChannel, CustomerCode));
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _str;
        }

        /// <summary>
        /// 获取SAP 发票邮寄地址 Watch
        /// </summary>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="CustomerCode"></param>
        /// <returns></returns>
        public string SAP_InvoiceAddressNO_Z1(string Companycode, string Division, string DistributionChannel, string CustomerCode)
        {
            string _str = string.Empty;
            try
            {
                _sql = @"select top 1 a.Kunn2 from SAP_KNVP a 
                inner  join SAP_KNA1 b on a.kunn2 = b.kunnr 
                inner join SAP_ADRC c on b.Adrnr = c.addrnumber
                where parvw = 'Z1' and a.VKORG=@VKORG and SPART=@SPART and VTWEG=@VTWEG and a.Kunnr=@Kunnr order by vkorg";
                _str = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, Companycode, Division, DistributionChannel, CustomerCode));
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _str;
        }

        /// <summary>
        /// 获取SAP 保修卡信息号
        /// </summary>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="CustomerCode"></param>
        /// <returns></returns>
        public string SAP_WarrentyCardNo(string Companycode, string Division, string DistributionChannel, string CustomerCode)
        {
            string _str = string.Empty;
            try
            {
                _sql = @"select top 1 a.Kunn2 from SAP_KNVP a 
                inner  join SAP_KNA1 b on a.kunn2 = b.kunnr 
                inner join SAP_ADRC c on b.Adrnr = c.addrnumber 
                where parvw = 'WC' and a.VKORG=@VKORG and SPART=@SPART and VTWEG=@VTWEG and a.Kunnr=@Kunnr order by vkorg ";
                _str = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, Companycode, Division, DistributionChannel, CustomerCode));
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _str;
        }

        /// <summary>
        /// 获取SAP 收货信息号(Watch\Spare Part)
        /// </summary>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="CustomerCode"></param>
        /// <returns></returns>
        public string SAP_ShipAddressNo(string Companycode, string Division, string DistributionChannel, string CustomerCode)
        {
            string _str = string.Empty;
            try
            {
                _sql = @"select top 1 a.Kunn2 from SAP_KNVP a 
                inner join SAP_KNA1 b on a.kunn2 = b.kunnr 
                inner join SAP_ADRC c on b.Adrnr = c.addrnumber 
                where parvw = 'WE' and a.VKORG=@VKORG and SPART=@SPART and VTWEG=@VTWEG and a.Kunnr=@Kunnr order by vkorg ";
                _str = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, Companycode, Division, DistributionChannel, CustomerCode));
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _str;
        }

        /// <summary>
        /// 获取SAP 收货信息号(只需要cs 才放)
        /// </summary>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="CustomerCode"></param>
        /// <returns></returns>
        public string SAP_ShipAddressNo_Z5(string Companycode, string Division, string DistributionChannel, string CustomerCode)
        {
            string _str = string.Empty;
            try
            {
                _sql = @"select top 1 a.Kunn2 from SAP_KNVP a 
                inner join SAP_KNA1 b on a.kunn2 = b.kunnr 
                inner join SAP_ADRC c on b.Adrnr = c.addrnumber 
                where parvw = 'Z5' and a.VKORG=@VKORG and SPART=@SPART and VTWEG=@VTWEG and a.Kunnr=@Kunnr order by vkorg ";
                _str = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, Companycode, Division, DistributionChannel, CustomerCode));
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _str;
        }


        /// <summary>
        /// 获取SAP 开店日期
        /// </summary>
        /// <param name="Companycode"></param>
        /// <param name="Division"></param>
        /// <param name="DistributionChannel"></param>
        /// <param name="CustomerCode"></param>
        /// <returns></returns>
        public string SAP_ZOPEN_DATE(string Companycode, string Division, string DistributionChannel, string CustomerCode)
        {
            string _str = string.Empty;
            try
            {
                _sql = @"select top 1  ZOPEN_DATE from SAP_KNVV a inner join SAP_KNA1 b on a.KUNNR=b.KUNNR 
                where  a.VKORG=@VKORG and SPART=@SPART and VTWEG=@VTWEG and a.Kunnr=@Kunnr order by vkorg ";
                _str = ConvertUtil.ToString(DataAccess.Instance("BizSAP").ExecuteScalar(_sql, Companycode, Division, DistributionChannel, CustomerCode));
            }
            catch (Exception ex)
            {
                LogUtil.Error(ex.Message);
            }
            return _str;
        }

    }
}
