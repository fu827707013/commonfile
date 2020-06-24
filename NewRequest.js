// UWF.Process.PurchaseOrder
/*
                             _ooOoo_
                            o8888888o
                            88" . "88
                            (| -_- |)
                            O\  =  /O
                         ____/`---'\____
                       .'  \\|     |//  `.
                      /  \\|||  :  |||//  \
                     /  _||||| -:- |||||-  \
                     |   | \\\  -  /// |   |
                     | \_|  ''\---/''  |   |
                     \  .-\__  `-`  ___/-. /
                   ___`. .'  /--.--\  `. . __
                ."" '<  `.___\_<|>_/___.'  >'"".
               | | :  `- \`.;`\ _ /`;.`/ - ` : | |
               \  \ `-.   \_ __\ /__ _/   .-` /  /
          ======`-.____`-.___\_____/___.-`____.-'======
                             `=---='
          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                     佛祖保佑        永无BUG(以开光)
*/
var lang = $("#div_lang").attr("data-lang");
var MCategoryDis = [];
function beforeSubmit() {
    var res = true;
    var categoryArr = [];
    // 验证
    $("#tb_PURCHASEORDER_DT tbody .td_MERCHANDISECATEGORYCODE").each(function () {
        var category = $(this).find("input").val();
        if (MCategoryDis.indexOf(category) > -1) {
            if (categoryArr.indexOf("008") < 0)
                categoryArr.push("008");
        } else {
            if (categoryArr.indexOf("001") < 0)
                categoryArr.push("001");
        }
        if (categoryArr.length >= 2) {
            if (lang != "en-US")
                alert("GB008不能和其他商品类别在一个PO单中!");
            else
                alert("GB008 cannot be in one PO list with other commodity categories!");
            res = false;
            return false;
        }
    })
    if (!res)
        return res;
    //合同管理数据引号转义问题
    if ($("#fld_CONTRACTMANAGEMENTDATA").val() != "") {
        $("#fld_CONTRACTMANAGEMENTDATA").val($("#fld_CONTRACTMANAGEMENTDATA").val().replaceAll("&quot;", "\""))
    }
    //主表存入商品类别判断  0不存在Media,Architect 1存在Media 2存在Architect
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
        JudgeCategory();
        $("input[name=fld_CENTRALPROCUREMENT]:checked").attr("checked", false);
    }
    //判断是否存在猎头费
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "General Procurement") {
        headHuntingJundge();
    }
    var temp = true;
    if ($("#fld_MAINCOSTCENTERNAME").val() == "" && $("input[name=fld_PROCURMENTTYPE]:checked").val() != "CER") {
        var vLanague = $("#div_lang").attr("data-lang");
        if (vLanague == "zh-CN") {
            alert("请选择主要成本中心！");
        }
        else {
            alert("Please select the main cost center !");
        }
        temp = false;
    }

    //提交前为每一个明细行成本中心找到品牌
    rowFillBrand();
    //报价过程判断
    if ($("input[name=fld_QUOTATIONCOMPARISON]:checked").val() == "Yes" && $("#fld_QUTOTATIONDATA").val() == "" && temp) {
        //供应商申请单号报价比较单查找
        var vLanague = $("#div_lang").attr("data-lang");
        //中英文转换
        if ($("#fld_ORRELATEDVENDORAPPLICATIONNO").val() != "") {
            var kk = vendorquotationdata();
            if (kk == 3) {
                temp = true;
            }
            else if (kk == 2) {
                temp = false;
                if (vLanague == "zh-CN") {
                    alert("请填写报价比较！");
                }
                else {
                    alert("Please fill in the Quotation Comparison!");
                }
            }
        }
        else {
            temp = false;
            if (vLanague == "zh-CN") {
                alert("请填写报价比较！");
            }
            else {
                alert("Please fill in the Quotation Comparison!");
            }
        }
    }

    //cer项目预算控制
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER" && temp) {
        var i = budgetControl();
        if (i != "0") {
            temp = false;
            var vLanague = $("#div_lang").attr("data-lang");
            //中英文转换
            if (vLanague == "zh-CN") {
                alert("预算超出！");
            }
            else {
                alert("The budget is beyond!");
            }
        }
    }
    //marketing品牌预算控制
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense" && temp) {
        var i = marketingbudgetControl();
        if (i != "0") {
            temp = false;
            var vLanague = $("#div_lang").attr("data-lang");
            //中英文转换
            if (vLanague == "zh-CN") {
                alert("品牌:" + i[0].name + "  年份:" + i[0].year + " 预算超出！");
            }
            else {
                alert("Brand:" + i[0].name + "  Year:" + i[0].year + " The budget is beyond!");
            }
        }
    }

    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration" && request("Type") != "MYTASK" && temp) {
        var k = sisPoSubmitControl();
        //0说明存在一单po正在进行中
        if (k == 0) {
            temp = false;
            var vLanague = $("#div_lang").attr("data-lang");
            //中英文转换
            if (vLanague == "zh-CN") {
                alert("该SIS PO 已在待办任务里");
            }
            else {
                alert("This SIS PO is on the to-do list !");
            }
        }
    }
    //集中采购po,Ecatlog带品牌控制
    if ($("input[name=fld_CENTRALPROCUREMENT]:checked").val() == "Yes" && $("#fld_ECATLOGBRANDCODE").val() == "" && temp) {
        temp = false;
        var vLanague = $("#div_lang").attr("data-lang");
        //中英文转换
        if (vLanague == "zh-CN") {
            alert("集中采购必须E-Catlog带入");
        }
        else {
            alert("E-Catlog is required for Central Procurement !");
        }
    }

    if (temp) {
        //sis触发的po将1改成0
        if ($("#fld_SISPOSTATUS").val() == 1) {
            $("#fld_SISPOSTATUS").val(2);
        }
        //税码防错设置
        $("#tb_PURCHASEORDER_DT tbody tr").not("last").each(function () {
            if ($(this).find("input[id$=fld_VATRATE_CODE]").val() == "") {
                $(this).find("input[id$=fld_VATRATE_CODE]").val($(this).find("input[id$=fld_VATRATE_NAME]").val().substring(0, 2));
            }
        });
    };
    return temp;
}

var MCategoryStatus = "";
//初始化页面
$(function () {
    if ($("#UserInfo1_fld_DEPARTMENT").val() != "IT") {
        MCategoryStatus = " and ISNULL(ISHEAD,&apos;&apos;)<>1 ";
    }
    loadCommonData();
    Page_Load();
})

//Begin
//=============================================================Begin 自定义事件方法=============================================================//
function Page_Load() {
    Page_Main();
    Page_Details();
    //千分位
    if (request("Type") == "MYREQUEST" || request("Type") == "MYAPPROVAL" || request("Type") == "report") {
        var arymain = ["div_field_NETAMOUNTMAIN", "div_field_GROSSAMOUNTMAIN"];
        var arytb = [["tb_PURCHASEORDER_DT", "td_UNITPRICE", "td_NETAMOUNT", "td_GROSSAMOUNT", "td_PROJECTBUDGETBALANCE"], ["tb_PURCHASEORDER_PAYMENT_DT", "td_AMOUNTDT"]];
        //var aryother = ["fld_NETAMOUNTAmount", "fld_GROSSAMOUNTAmount", "fld_PROJECTBUDGETBALANCEAmount"];
        var aryother = [];
        autoNumberAddClass(arymain, arytb, aryother);
    }
    $("#div_field_NETAMOUNTMAIN").removeClass("hidden")
    $("#div_field_GROSSAMOUNTMAIN").removeClass("hidden")
}

/// <summary>
/// 默认加载主表
///</summary>
function Page_Main() {
    //$("body").append("<script src='../../../Common/Assets/js/input-autocomplete.js' type='text/javascript'></script>");
    //$("body").append("<link href='../../../Common/Assets/css/input-autocomplete.css' type='text/css' rel='stylesheet'>");

    $("#div_panel_PURCHASEORDER_PAYMENT_DT .panel-title").addClass("hidden");
    $("#div_panel_PURCHASEORDER_PAYMENT_DT .panel").attr("style", "padding-top: 0px;");
    $("#div_panel_PurchaseOrderInformation ul").prepend("<li style='text-decoration:underline;color:blue;font-weight: bold;' id='txt_ECATALOG' class='icon minimise-tool'>Search E-Catalog</li>")
    $("#Button_CONTRACTMANAGEMENT").attr("style", "width:85%");

    var vLanague = $("#div_lang").attr("data-lang");
    var lanhistory;
    var lanbut;
    //中英文转换
    if (vLanague == "zh-CN") {
        lanhistory = "历史价格";
        lanbut = "报价比较";
        lanconbut = "合同管理";
    }
    else {
        lanhistory = "History Price";
        lanbut = "Quotation Comparison";
        lanconbut = "Contract Management";
    }

    //历史价格暂时隐藏
    //$("#tb_PURCHASEORDER_DT .td_ITEMNO").not(":first").next().append("<div><a style='text-decoration:underline;color:blue; font-size:10px' class='icon minimise-tool' onclick=HistoryPriceOnClick(this)>" + lanhistory + "</a></div>");

    $("#div_field_QUOTATIONCOMPARISON").find("div .form-ctl").append("&nbsp&nbsp&nbsp&nbsp&nbsp<input type='button' id='Button_QUOTATIONCOMPARISONBUTTON' title='' class='btn btn-icon btn-default hidden-print btnJson validate[required] ' value='" + lanbut + "' data-prompt-position='bottomLeft' style='width:50%;margin-left:75px'>");
    $("#div_field_QUOTATIONCOMPARISON").attr("class", "col-lg-6 col-sm-6 col-xs-12 form-cell ");
    $("#div_field_ORRELATEDVENDORAPPLICATIONNO").attr("class", "col-lg-6 col-sm-6 col-xs-12 form-cell ");
    AddTableScroll("tb_PURCHASEORDER_DT", "210%", "250%");

    //申请页面默认值添加控制 和 sis申请待办控制
    if (request("Type") == "NEWREQUEST" || (request("Type") == "MYTASK" || $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration")) {
        if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == undefined) {
            $("#fld_PROCURMENTTYPE_0").prop("checked", true);
        }
        if ($("input[name=fld_CHARGEOTHERCOSTCENTER]:checked").val() == undefined) {
            $("#fld_CHARGEOTHERCOSTCENTER_1").prop("checked", true);
        }
        if ($("input[name=fld_CONTRACTTYPE]:checked").val() == undefined) {
            $("#fld_CONTRACTTYPE_0").prop("checked", true);
        }
        if ($("input[name=fld_CENTRALPROCUREMENT]:checked").val() == undefined) {
            $("#fld_CENTRALPROCUREMENT_1").prop("checked", true);
        }
        if ($("input[name=fld_QUOTATIONCOMPARISON]:checked").val() == undefined) {
            $("#fld_QUOTATIONCOMPARISON_0").prop("checked", true);
        }
    }

    //货物接收默认值设置
    if ($("#fld_GOODSRECEIVER").val() == "") {
        $("#fld_GOODSRECEIVER").val($("#UserInfo1_fld_CREATEBY").val());
        $("#fld_GOODSRECEIVER_VALUE").val($("#UserInfo1_fld_CREATEBYACCOUNT").val());
    }

    //界面初始化。
    tableShow();
    //供应商
    $("#fld_VENDORNO").attr("onclick", "OnClickvendor()");
    //采购类型控制
    JudgeProcurmentType(0);
    //报价比较控制
    JudgeQuotationComparison();

    //表单点击事件添加
    //报价比较
    $("input[name=fld_QUOTATIONCOMPARISON]").attr("onclick", "JudgeQuotationComparison()");
    //采购类型
    $("input[name=fld_PROCURMENTTYPE]").attr("onclick", "JudgeProcurmentType(1)");

    //分摊至其它成本中心
    //$("input[name=fld_CHARGEOTHERCOSTCENTER]").attr("onclick", "JudgeChangeCostCenter();CostCenterSearch(0);");
    //E-catlog
    if ((request("Type") == "NEWREQUEST") || (request("Type") == "MYTASK") || (request("Type") == "Draft")) {
        $("#txt_ECATALOG").attr("onclick", "OnClickListInformation()");
    }

    //报价比较按钮
    $("#Button_QUOTATIONCOMPARISONBUTTON").attr("onclick", "OnClickQuotation()");
    //货币汇率
    $("#fld_CURRENCY").attr("onchange", "curtypeOnChange(1)");
    curtypeOnChange(0);//默认值
    //动态添加合同管理按钮事件
    OnClickContract();
    //保留两位小数
    Fun_OnblurToFixed();
    //采购类型租赁po隐藏
    $("#fld_PROCURMENTTYPE_4").parent().hide();
    //合同管理按钮值修改
    $("#Button_CONTRACTMANAGEMENT").val(lanconbut);
    //成本中心模糊查询
    $("#fld_BRANDDIVISION").attr("onchange", "setddlName(this);CostCenterSearch(0);marketingbrandclear();");
    //页面加载成本中心模糊查询
    CostCenterSearch(1);
    //去除成本中心浏览器历史查询框
    $("#fld_MAINCOSTCENTERNAME").attr("autocomplete", "off");
    //理由下拉框添加
    //$("#div_field_REASON").find(".form-ctl").css({ "height": "100%", "max-height": "100%", "overflow-y": "scroll" });
    if ($("#fld_CONTRACTMANAGEMENTDATA").val() != "" || $("#fld_CONTRACTMANAGEMENTDATA").val() != null) {
        var a = $("#fld_CONTRACTMANAGEMENTDATA").val().replaceAll("&quot;", "\"").replaceAll("&lt;", "<").replaceAll("&gt;", ">");
        $("#fld_CONTRACTMANAGEMENTDATA").val(a);
    }
}

/// <summary>
/// 默认加载明细行
///</summary>
function Page_Details() {
    $("#tb_PURCHASEORDER_PAYMENT_DT_UploadExcel").next().hide();
    //上传明细行设置
    if (((request("Type") == "NEWREQUEST") || (request("Type") == "MYTASK") || (request("Type") == "Draft")) && $("input[name=fld_PROCURMENTTYPE]:checked").val() != "Retailer POS Decoration") {
        ExcelImportInit("tb_PURCHASEORDER_DT", "PurchaseOrderItem.xlsx", "../../UWF.Process.PurchaseOrder/ExeclTemplate/PurchaseOrderItem.xlsx", false);
    }
    //明细行添加汇总
    addTotalAmount("tb_PURCHASEORDER_DT", 15);
    $("#btn_PURCHASEORDER_DT").attr("onclick", "addRowT('tb_PURCHASEORDER_DT');return false;");
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        $(this).find("td:last a").attr("onclick", "if(confirm('您是否要删除?？')){deleteRowT('tb_PURCHASEORDER_DT',this);}return false;");
    });

    //明细行计算
    //数量
    $("#tb_PURCHASEORDER_DT td.td_QTY").not(":first").each(function () {
        $(this).find("input[id*=fld_QTY]").attr("onblur", "checkExpression(this),Fun_OnblurToFixed(),addTotalAmountFun();getmoney()");
    });
    //单价
    $("#tb_PURCHASEORDER_DT td.td_UNITPRICE").not(":first").each(function () {
        $(this).find("input[id*=fld_UNITPRICE]").attr("onkeyup", "this.value=this.value.replace(/[^\\-?\\d.\\d\\d]/g,'')");//可以输入负数正则表达式
        $(this).find("input[id*=fld_UNITPRICE]").attr("onblur", "checkExpression(this),Fun_OnblurToFixedString(this),addTotalAmountFun();getmoney()");
        //$(this).find("input[id*=fld_UNITPRICE]").attr("step", "0.01");
    });
    //税率
    $("#tb_PURCHASEORDER_DT td.td_VATRATE").not(":first").each(function () {
        $(this).find("select[id$=fld_VATRATE]").attr("onchange", "checkExpression(this),Fun_OnblurToFixed(),addTotalAmountFun()");
    });
    //描述控制
    $("#tb_PURCHASEORDER_DT td.td_DESCRIPTION").not(":first").each(function () {
        $(this).find("input[id$=fld_DESCRIPTION]").attr("onchange", "clearitemo(this)");
    });
    //返回总计值
    if ($("#fld_NETAMOUNTMAIN").val() != "" && $("#fld_GROSSAMOUNTMAIN").val() != "") {
        if (request("Type") == "MYREQUEST" || request("Type") == "MYAPPROVAL" || request("Type") == "report") {
            $("#fld_NETAMOUNTAmount").text($("#fld_NETAMOUNTMAIN").val());
            $("#fld_GROSSAMOUNTAmount").text($("#fld_GROSSAMOUNTMAIN").val());
        }
        else {
            $("#fld_NETAMOUNTAmount").val($("#fld_NETAMOUNTMAIN").val());
            $("#fld_GROSSAMOUNTAmount").val($("#fld_GROSSAMOUNTMAIN").val());
        }
    }


    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        //明细行月控制
        $(this).find("input[id$=fld_DATEFROM]").attr("onchange", "monthcontrol(this)");
        $(this).find("input[id$=fld_DATETO]").attr("onchange", "monthcontrol(this)");
        //获取税率字段code
        $(this).find("select[id$=fld_VATRATE]").attr("onblur", "setddlName(this),GetDropEXTDataTax(this, 'type_taxcode', '[{\\'id\\':\\'fld_VATRATE_CODE\\',\\'COLUMN\\':\\'CODE\\'}]')");
        $(this).find("input[id$=fld_COSTCENTERNAME]").attr("autocomplete", "off");
        $(this).find("input[id$=fld_COSTCENTERNAME]").attr("onblur", "costmaincostrow(this)");
        $(this).find("input[id$=fld_DESCRIPTION]").next().remove();
    });

    //明细行付款控制
    $("#tb_PURCHASEORDER_PAYMENT_DT tbody tr").each(function () {
        $(this).find("input[id$=fld_PERCENTAGEDT]").attr("onblur", "checkExpression(this),Fun_OnblurToFixed(),payAmountFun(this)");
    });

    //明细行税率赋值
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        if ($(this).find("input[id$=fld_VATRATE_NAME]").val() != "") {
            var name = $(this).find("input[id$=fld_VATRATE_NAME]").val();
            $(this).find("select[id$=fld_VATRATE]").find("option").each(function () {
                if ($(this).text() == name) {
                    $(this).attr("selected", true);
                    return false;
                }
            });
        }
    });

    if ((request("Type") == "MYAPPROVAL") || (request("Type") == "MYREQUEST")) {
        $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
            $(this).find("select[id$=fld_VATRATE]").next().text($(this).find("input[id$=fld_VATRATE_NAME]").val())
        });
    }
    budgetbalancetotal();
    $("#tb_PURCHASEORDER_PAYMENT_DT tbody tr .td_CATEGORYDT").find("select").each(function () {
        $(this).attr("onchange", "downPaymentOnchange(this)");
    });
    var vLanague = $("#div_lang").attr("data-lang");
    var lan2;
    //中英文转换
    if (vLanague == "zh-CN") {
        lan2 = "外币不含税总金额";
    }
    else {
        lan2 = "Foreign Total Amount Without VAT";
    }
    var html = '<div class="col-lg-4 col-sm-6 col-xs-12 form-cell" id="div_field_FOREIONCURRENCY" style="height:"> <div class="form-label">' + lan2 + ':</div>';
    html += '<div class="form-field"><div class="form-ctl" style="word-break: break-all;"><input name="fld_FOREIONCURRENCY" type="text" id="fld_FOREIONCURRENCY"'
    html += 'class="form-control validate[custom[number]] " data-type="number" title="" onblur="" data-field="FOREIONCURRENCY" data-prompt-position="bottomLeft" step="0.01"></div></div></div>'
    $("#div_field_GROSSAMOUNTMAIN").after(html);
    getmoney();
    //人民币含税总价和外币含税总价样式
    $("#fld_NETAMOUNTMAIN").attr("readonly", true)
    $("#fld_FOREIONCURRENCY").attr("readonly", true)
    $("#fld_GROSSAMOUNTMAIN").attr("readonly", true)
}

//=============================================================End 自定义事件方法=============================================================//
//End
//汇总外币总金额
function getmoney() {
    var netamount = 0;
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        if ($(this).find("input[id$=fld_UNITPRICE]").val() != "" && $(this).find("span[id$=fld_QTY]").val() != "") {
            netamount += parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($(this).find("input[id$=fld_QTY]").val());
        }
    });
    $("#fld_FOREIONCURRENCY").val(GetMoney(netamount));
}
//报价比较表单控制
function JudgeQuotationComparison() {
    if ($("input[name=fld_QUOTATIONCOMPARISON]:checked").val() == "Yes") {
        $("#Button_QUOTATIONCOMPARISONBUTTON").show();
        $("#fld_REASON").attr("class", "form-control ");
    }
    else {
        $("#Button_QUOTATIONCOMPARISONBUTTON").hide();
        $("#fld_REASON").attr("class", "form-control validate[required] border-left-color");
    }
}

//表单采购类型控制
//i为0 刷新 
//i为1 控制
function JudgeProcurmentType(i) {

    setddlLinkbranddivsion();
    if (i == 1) {
        CostCenterSearch(0);

        //清空ecatlog品牌
        $("#fld_ECATLOGBRANDCODE").val("");
    }
    /////////////////////////////////////
    //General Procurement
    /////////////////////////////////////
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "General Procurement") {
        setddlLinkTax(0);
        cerHide(i);
        sisShow();
        if ((request("Type") == "NEWREQUEST") || (request("Type") == "MYTASK") || (request("Type") == "Draft")) {
            $("input[name=fld_CENTRALPROCUREMENT]").removeAttr("disabled", "disabled");
            if ($("input[name=fld_CENTRALPROCUREMENT]:checked").val() == undefined) {
                $("#fld_CENTRALPROCUREMENT_1").prop("checked", true);
            }
        }
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_MERCHANDISECATEGORY]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'MERCHANDISECATEGORYCODE',dataSource:'Purchase Order-Merchandise Category',filter:' Type=&apos;General&apos; and ext01=1 " + MCategoryStatus + " ',single:true,IsMethod: true});");
            if (i == 1) {
                $(this).val("");
            }
            //$(this).attr("class", "form-control ");
        });
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_WBSELEMENT]").each(function () {
            $(this).attr("class", "form-control");
        });
        //wbs 弹出框控制
        wbsControl(i);
        //描述弹出框
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_DESCRIPTION]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'ITEMNO,UNIT_NAME,QTY,UNITPRICE',dataSource:'Purchase Order-DESCRIPTION',filter:'',single:true,IsMethod: true});");
            $(this).removeAttr("disabled", "disabled");
        });
        //cer分摊成本中心控制
        $("input[name=fld_CHARGEOTHERCOSTCENTER]").removeAttr("disabled");
        //sis表单控件控制
        noSisControl(i);
    }
    /////////////////////////////////////
    //Marketing Expense
    /////////////////////////////////////
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
        setddlLinkTax(1);
        cerHide(i);
        sisShow();
        if ((request("Type") == "NEWREQUEST") || (request("Type") == "MYTASK") || (request("Type") == "Draft")) {
            $("input[name=fld_CENTRALPROCUREMENT]").attr("disabled", "disabled");
            $("input[name=fld_CENTRALPROCUREMENT]:checked").attr("checked", false);
        }
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_MERCHANDISECATEGORY]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'MERCHANDISECATEGORYCODE',dataSource:'Purchase Order-Merchandise Category',filter:' Type=&apos;Marketing&apos; and ext01=1 " + MCategoryStatus + "  ',single:true,IsMethod: true});");
            if (i == 1) {
                $(this).val("");
            }
            //$(this).attr("class", "form-control validate[required] border-left-color");
        });
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_WBSELEMENT]").each(function () {
            $(this).attr("class", "form-control validate[required] border-left-color");
        });
        //wbs 弹出框控制
        wbsControl(i);
        //描述弹出框
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_DESCRIPTION]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'ITEMNO,UNIT_NAME,QTY,UNITPRICE',dataSource:'Purchase Order-DESCRIPTION',filter:'',single:true,IsMethod: true});");
            $(this).removeAttr("disabled", "disabled");
        });
        //cer分摊成本中心控制
        $("input[name=fld_CHARGEOTHERCOSTCENTER]").removeAttr("disabled");
        //sis表单控件控制
        noSisControl(i);
        if (i == 1) {
            $("#fld_MAINCOSTCENTERNAME").val("");
            $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
                $(this).find("input[id$=fld_DATEFROM]").val("");
                $(this).find("input[id$=fld_DATETO]").val("");
                $(this).find("input[id$=fld_COSTCENTERNAME]").val("");
            });

        }
    }
    /////////////////////////////////////
    //CER
    /////////////////////////////////////
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        setddlLinkTax(2);
        cerShow(i);
        sisShow();
        if ((request("Type") == "NEWREQUEST") || (request("Type") == "MYTASK") || (request("Type") == "Draft")) {
            $("input[name=fld_CENTRALPROCUREMENT]").removeAttr("disabled", "disabled");
            if ($("input[name=fld_CENTRALPROCUREMENT]:checked").val() == undefined) {
                $("#fld_CENTRALPROCUREMENT_1").prop("checked", true);
            }
        }
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_MERCHANDISECATEGORY]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'MERCHANDISECATEGORYCODE',dataSource:'Purchase Order-Merchandise Category',filter:' Type=&apos;CER&apos; and ext01=1 " + MCategoryStatus + "  ',single:true,IsMethod: true});");
            if (i == 1) {
                $(this).val("");
            }
            //$(this).attr("class", "form-control validate[required] border-left-color");
        });
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_WBSELEMENT]").each(function () {
            $(this).attr("class", "form-control validate[required] border-left-color");
        });
        //wbs 弹出框控制
        wbsControl(i);
        //描述弹出框
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_DESCRIPTION]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'ITEMNO,UNIT_NAME,QTY,UNITPRICE',dataSource:'Purchase Order-DESCRIPTION',filter:'',single:true,IsMethod: true});");
            $(this).removeAttr("disabled", "disabled");
        });
        //cer分摊成本中心控制
        $("input[name=fld_CHARGEOTHERCOSTCENTER]").attr("disabled", "disabled");
        $("#fld_CHARGEOTHERCOSTCENTER_1").prop("checked", true);
        //sis表单控件控制
        noSisControl(i);
    }
    /////////////////////////////////////
    //SIS
    /////////////////////////////////////
    else {
        setddlLinkTax(3);
        cerHide(i);
        sisHide();
        if ((request("Type") == "NEWREQUEST") || (request("Type") == "MYTASK") || (request("Type") == "Draft")) {
            $("input[name=fld_CENTRALPROCUREMENT]").attr("disabled", "disabled");
            $("input[name=fld_CENTRALPROCUREMENT]:checked").attr("checked", false);
        }
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_MERCHANDISECATEGORY]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'WBSELEMENT,EXPENSECATEGORY,,,MERCHANDISECATEGORYCODE',dataSource:'Purchase Order-Merchandise Category(SIS)',filter:' Type=&apos;SIS&apos; and ext01=1 " + MCategoryStatus + "  ',single:true,IsMethod: true});");
            if (i == 1) {
                $(this).val("");
            }
            //$(this).attr("class", "form-control validate[required] border-left-color");
        });
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_WBSELEMENT]").each(function () {
            $(this).attr("class", "form-control validate[required] border-left-color");
        });
        //wbs 弹出框控制
        wbsControl(i);
        //描述弹出框
        $("#tb_PURCHASEORDER_DT").find("input[id$=fld_DESCRIPTION]").each(function () {
            $(this).next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'',dataSource:'Purchase Order-DESCRIPTION(SIS)',filter:'',single:true,IsMethod: true });");
            $(this).attr("disabled", "disabled");
        });
        if (i == 1) {
            //清空描述
            $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
                $(this).find("input[id$=fld_DESCRIPTION]").val("");
                $(this).find("select[id$=fld_UNIT]").val("");
                $(this).find("input[id$=fld_UNIT_NAME]").val("");
                $(this).find("input[id$=fld_QTY]").val("");
                $(this).find("input[id$=fld_UNITPRICE]").val("");
                $(this).find("input[id$=fld_ITEMNO]").val("");
                $(this).find("input[id$=fld_NETAMOUNT]").val("");
                $(this).find("input[id$=fld_GROSSAMOUNT]").val("");
            });
            $("#fld_NETAMOUNTAmount").val("");
            $("#fld_GROSSAMOUNTAmount").val("");
            $("#fld_NETAMOUNTMAIN").val("");
            $("#fld_GROSSAMOUNTMAIN").val("");
        }
        //cer分摊成本中心控制
        $("input[name=fld_CHARGEOTHERCOSTCENTER]").removeAttr("disabled");
        //sis表单控件控制
        SisControl(i);
    }
    //跨月控制
    JudgePurchaseTypeMonth();
    //切换采购类型税率清空控制
    if (i == 1 && $("#tb_PURCHASEORDER_DT tbody tr").length > 2) {
        taxclear();

        //明细行清空控制
        var vLanague = $("#div_lang").attr("data-lang");
        var lan1;
        //中英文转换
        if (vLanague == "zh-CN") {
            lan1 = "是否清除所有明细行？";
        }
        else {
            lan1 = "Clear all detail lines?";
        }

        if (confirm(lan1)) {
            $("#tb_PURCHASEORDER_DT tbody tr").eq(0).find("td").not(":eq(0),:eq(1)").each(function () {
                $(this).find("input").val("");
                $(this).find("select").find("option").eq(0).prop("selected", "selected");
            });
            $("#tb_PURCHASEORDER_DT tbody tr").not(":eq(0),:last").remove();
        }
    }
    //利润中心控制
    profitCenterControl();
    //markerting控制
    markertingDisabled();
    //清除预算余额总额
    $("#fld_PROJECTBUDGETBALANCEAmount").val("");

}

//表单控制
function tableShow() {
    //var text1 = "<div class='col-lg-4 col-sm-6 col-xs-12 form-cell hidden-sm hidden-xs' ><div class='form-label text-right'></div><div class='form-field' style='height:45px;'></div></div>";
    //$("#div_field_CONTRACTMANAGEMENT").after(text1);
    $("#div_panel_PurchaseOrderInformation").find("div .hidden-sm").hide();
    //$("#div_field_CURRENCY_NAME").hide();
    $("#fld_MAINPROFITCENTERNAME").attr("disabled", "disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_PROFITCENTERNAME").each(function () {
        $(this).find("input[id$=fld_PROFITCENTERNAME]").attr("disabled", "disabled");
    });
    if (request("type").toLocaleLowerCase() == "myrequest" || request("type").toLocaleLowerCase() == "myapproval"
        || request("type").toLocaleLowerCase() == "myunread" || request("type").toLocaleLowerCase() == "myread"
        || request("type").toLocaleLowerCase() == "report") {
        $("#fld_MAINPROFITCENTERNAME").parent().append("<span style=\"word-break:break-all;word-wrap:break-word;width:" + $("#fld_MAINPROFITCENTERNAME").css("width") + ";text-align:center;\">" + $("#fld_MAINPROFITCENTERNAME").val() + "</span>");
        $("#fld_MAINPROFITCENTERNAME").next().html(FilterHtmls($("#fld_MAINPROFITCENTERNAME").val()));
        $("#fld_MAINPROFITCENTERNAME").css("display", "none");

        $("#tb_PURCHASEORDER_DT tbody .td_PROFITCENTERNAME ").find("input[id$=fld_PROFITCENTERNAME]").each(function () {
            $(this).parent().append("<span style=\"word-break:break-all;word-wrap:break-word;width:" + $(this).css("width") + ";text-align:center;\">" + $(this).val() + "</span>");
            $(this).next().html(FilterHtmls($(this).val()));
            $(this).css("display", "none");
        });
    }
}

//利润中心控制
function profitCenterControl() {

    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        $("#div_field_MAINCOSTCENTERNAME").addClass("hidden");
        $("#div_field_MAINPROFITCENTERNAME").removeClass("hidden");
        $("#tb_PURCHASEORDER_DT .td_COSTCENTERNAME").addClass("hidden");
        $("#tb_PURCHASEORDER_DT .td_PROFITCENTERNAME").removeClass("hidden");

        $("#fld_MAINCOSTCENTERNAME").val("");
        $("#tb_PURCHASEORDER_DT tbody .td_COSTCENTERNAME").each(function () {
            $(this).find("input[id$=fld_COSTCENTERNAME]").val("");
        });
    }
    else {
        $("#div_field_MAINCOSTCENTERNAME").removeClass("hidden");
        $("#div_field_MAINPROFITCENTERNAME").addClass("hidden");
        $("#tb_PURCHASEORDER_DT .td_COSTCENTERNAME").removeClass("hidden");
        $("#tb_PURCHASEORDER_DT .td_PROFITCENTERNAME").addClass("hidden");

        $("#fld_MAINPROFITCENTERNAME").val("");
        $("#tb_PURCHASEORDER_DT tbody .td_PROFITCENTERNAME").each(function () {
            $(this).find("input[id$=fld_PROFITCENTERNAME]").val("");
        });
    }

}

//markerting控制
function markertingDisabled() {
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
        $("#fld_MAINCOSTCENTERNAME").attr("disabled", "disabled");
        $("#tb_PURCHASEORDER_DT tbody .td_COSTCENTERNAME").each(function () {
            $(this).find("input[id$=fld_COSTCENTERNAME]").attr("disabled", "disabled");
        });
    }
    else {
        $("#fld_MAINCOSTCENTERNAME").removeAttr("disabled", "disabled");
        $("#tb_PURCHASEORDER_DT tbody .td_COSTCENTERNAME").each(function () {
            $(this).find("input[id$=fld_COSTCENTERNAME]").removeAttr("disabled", "disabled");
        });
    }
}

//sis表单显示控制
function sisShow() {
    $("#btn_PURCHASEORDER_DT").removeClass("hidden");
    $("#btn_PURCHASEORDER_DT").next().removeClass("hidden");
    $("#uploadifive-tb_PURCHASEORDER_DT_UploadExcel").removeClass("hidden");
    $("#txt_ECATALOG").show();
}

//sis表单隐藏控制
function sisHide() {
    $("#btn_PURCHASEORDER_DT").addClass("hidden");
    $("#btn_PURCHASEORDER_DT").next().addClass("hidden");
    $("#uploadifive-tb_PURCHASEORDER_DT_UploadExcel").addClass("hidden");
    $("#txt_ECATALOG").hide();
}

//cer表单显示控制
//i为0 刷新 
//i为1 控制
function cerShow(i) {
    var vLanague = $("#div_lang").attr("data-lang");
    var lancode1;
    var lanname1;
    //中英文转换
    if (vLanague == "zh-CN") {
        lancode1 = "项目编码:";
        lanname1 = "项目名称:";
    }
    else {
        lancode1 = "Project Code:";
        lanname1 = "Project Name:";
    }
    $("#div_field_PROJECTCODE .form-label").text(lancode1);
    $("#div_field_PROJECTNAME .form-label").text(lanname1);
    $("#div_field_PROJECTCODE").show();
    $("#div_field_PROJECTNAME").show();
    if (i == 1) {
        $("#fld_PROJECTCODE").val("");
        $("#fld_PROJECTNAME").val("");
    }
    //CER绑定项目编号
    $("#fld_PROJECTCODE").attr("onclick", "addProjectNo(this)");
    //$("#div_field_PROJECTBUDGET").show();
    //$("#div_field_MAINCOSTCENTERNAME").attr("class", "col-lg-4 col-sm-6 col-xs-12 form-cell ");

    $("#div_field_SAMEASCER").show();
    //$("#div_field_CHARGEOTHERCOSTCENTER").hide();
    if ($("input[name=fld_SAMEASCER]:checked").val() == undefined) {
        $("#fld_SAMEASCER_0").prop("checked", true);
    }
}

//动态品牌项目编号添加
function addProjectNo(obj) {
    var brandcode = $("#fld_BRANDDIVISION").val();
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        selectDataSource({ element: obj, title: dataselectlanguage(), fields: 'fld_PROJECTNAME', dataSource: 'Purchase Order-Project', filter: ' ProjectType=&apos;CER&apos; and BRANDORDIVISIONCODE=&apos;' + brandcode + '&apos; and Status=1', single: true, IsMethod: true });
    }
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration") {
        selectDataSource({ element: obj, title: dataselectlanguage(), fields: 'fld_PROJECTNAME,fld_VENDORNO,fld_VENDORNAME,fld_SISPOGUID,fld_BRANDDIVISION,fld_BRANDDIVISION_NAME,fld_CURRENCY,fld_EXCHANGERATE,fld_NETAMOUNTMAIN,fld_GROSSAMOUNTMAIN ', dataSource: 'Purchase Order-SIS APPLICATION', filter: ' BRANDDIVISION=&apos;' + brandcode + '&apos;', size: 'wide', single: true, IsMethod: true });
    }
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
        selectDataSource({ element: obj, title: dataselectlanguage(), fields: 'fld_PROJECTNAME', dataSource: 'Purchase Order-Project', filter: ' ProjectType like &apos;Marketing%&apos; and BRANDORDIVISIONCODE=&apos;' + brandcode + '&apos; and Status=1 ', single: true, IsMethod: true });
    }
}

//cer表单隐藏控制
function cerHide(i) {
    var brandcode = $("#fld_BRANDDIVISION").val();
    var vLanague = $("#div_lang").attr("data-lang");
    var lancode1;
    var lanname1;
    var lancode2;
    var lanname2;
    //中英文转换
    if (vLanague == "zh-CN") {
        lancode1 = "项目编码:";
        lanname1 = "项目名称:";
        lancode2 = "SIS编码:";
        lanname2 = "SIS名称:";
    }
    else {
        lancode1 = "Project Code:";
        lanname1 = "Project Name:";
        lancode2 = "Retailer POS Decoration Code:";
        lanname2 = "Retailer POS Decoration Name:";
    }
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration") {
        $("#div_field_PROJECTCODE .form-label").text(lancode2);
        $("#div_field_PROJECTNAME .form-label").text(lanname2);
        $("#div_field_PROJECTCODE").show();
        $("#div_field_PROJECTNAME").show();
        if (i == 1) {
            $("#fld_PROJECTCODE").val("");
            $("#fld_PROJECTNAME").val("");
        }
        $("#fld_PROJECTCODE").attr("onclick", "addProjectNo(this)");
    }
    //else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
    //    $("#div_field_PROJECTCODE .form-label").text(lancode1);
    //    $("#div_field_PROJECTNAME .form-label").text(lanname1);
    //    $("#div_field_PROJECTCODE").show();
    //    $("#div_field_PROJECTNAME").show();
    //    if (i == 1) {
    //        $("#fld_PROJECTCODE").val("");
    //        $("#fld_PROJECTNAME").val("");
    //    }
    //    $("#fld_PROJECTCODE").attr("onclick", "addProjectNo(this)");
    //}
    else {
        $("#div_field_PROJECTCODE .form-label").text(lancode1);
        $("#div_field_PROJECTNAME .form-label").text(lanname1);
        $("#div_field_PROJECTCODE").hide();
        $("#div_field_PROJECTNAME").hide();
        if (i == 1) {
            $("#fld_PROJECTCODE").val("");
            $("#fld_PROJECTNAME").val("");
        }
        $("#fld_PROJECTCODE").removeAttr("onclick");
    }
    // $("#div_field_PROJECTBUDGET").hide();
    //$("#div_field_MAINCOSTCENTERNAME").attr("class", "col-lg-8 col-sm-6 col-xs-12 form-cell ");

    $("#div_field_SAMEASCER").hide();
    // $("#div_field_CHARGEOTHERCOSTCENTER").show();
    //if ($("input[name=fld_CHARGEOTHERCOSTCENTER]:checked").val() == undefined) {
    //    $("#fld_CHARGEOTHERCOSTCENTER_1").prop("checked", true);
    //}
    $("input[name=fld_SAMEASCER]:checked").attr("checked", false);
}

//wbs 弹出框控制
function wbsControl(i) {
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense" || $("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        if (i == 1) {
            $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
                //cer和marketing的wbs弹窗控制
                $(this).find("input[id$=fld_WBSELEMENT]").next().attr("onclick", "cerWBSControl(this)");
                $(this).find("input[id$=fld_WBSELEMENT]").val("");
                $(this).find("input[id$=fld_EXPENSECATEGORY]").val("");
                $(this).find("input[id$=fld_PROJECTBUDGETBALANCE]").val("");
                $(this).find("input[id$=fld_MERCHANDISECATEGORYCODE]").val("");
            });
        }
        //页面刷新
        else {
            $("#tb_PURCHASEORDER_DT tbody tr").each(function () {
                //cer和marketing的wbs弹窗控制
                $(this).find("input[id$=fld_WBSELEMENT]").next().attr("onclick", "cerWBSControl(this)");
            });
        }
    }
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration") {
        if (i == 1) {
            $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
                //sis的wbs弹窗控制
                $(this).find("input[id$=fld_WBSELEMENT]").next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'EXPENSECATEGORY,MERCHANDISECATEGORY,,,MERCHANDISECATEGORYCODE',dataSource:'Purchase Order-WBS Element(SIS)',filter:' Type=&apos;SIS&apos; and ext01=1 ',single:true,IsMethod: true});");
                $(this).find("input[id$=fld_WBSELEMENT]").val("");
                $(this).find("input[id$=fld_EXPENSECATEGORY]").val("");
                $(this).find("input[id$=fld_PROJECTBUDGETBALANCE]").val("");
                $(this).find("input[id$=fld_MERCHANDISECATEGORYCODE]").val("");
            });
        }
        //页面刷新
        else {
            $("#tb_PURCHASEORDER_DT tbody tr").each(function () {
                //sis的wbs弹窗控制
                $(this).find("input[id$=fld_WBSELEMENT]").next().attr("onclick", "selectDataSource({element:this.previousElementSibling,title:'" + dataselectlanguage() + "',fields:'EXPENSECATEGORY,MERCHANDISECATEGORY,,,MERCHANDISECATEGORYCODE',dataSource:'Purchase Order-WBS Element(SIS)',filter:' Type=&apos;SIS&apos; and ext01=1 ',single:true,IsMethod: true});");
            });
        }
    }
    else {
        if (i == 1) {
            $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
                $(this).find("input[id$=fld_WBSELEMENT]").next().removeAttr("onclick");
                $(this).find("input[id$=fld_WBSELEMENT]").val("");
                $(this).find("input[id$=fld_EXPENSECATEGORY]").val("");
                $(this).find("input[id$=fld_PROJECTBUDGETBALANCE]").val("");
                $(this).find("input[id$=fld_MERCHANDISECATEGORYCODE]").val("");
            });
        }
        //页面刷新
        else {
            $("#tb_PURCHASEORDER_DT tbody tr").each(function () {
                $(this).find("input[id$=fld_WBSELEMENT]").next().removeAttr("onclick");
            });
        }
    }
}

//获取汇率
function curtypeOnChange(i) {
    if (i == 1) {
        GetExchangeRate($("#fld_EXCHANGERATE"), $("#fld_CURRENCY").val());
        //改变汇率重新计算
        addTotalAmountFun();
    }
    else {
        //默认值加载
        if ($("#fld_CURRENCY").val() == "") {
            $("#fld_CURRENCY").val("CNY");
            GetExchangeRate($("#fld_EXCHANGERATE"), $("#fld_CURRENCY").val());
        }
    }
}

//E-Catalog弹出窗口
function OnClickListInformation() {
    var title = "";
    if ($("#div_lang").attr("data-lang") == "en-US") {
        title = "E-Catalog List";
    }
    if ($("#div_lang").attr("data-lang") == "zh-CN") {
        title = "E-Catalog列表";
    }
    var vendorno = $("#fld_VENDORNO").val();
    var url = "../E-Catalog/GetECatalogList.aspx?VENDORNO=" + vendorno;
    var dialog = { title: title, size: 'Large ' };
    var iframe = { id: 'frameWindow', src: '"' + url + '"', scrolling: "yes" };
    var buttons = { method: 'ConfirmOnclick()' };
    var dia = { dialog: dialog, iframe: iframe, buttons: buttons };
    showDialog(dia);

}

//电子目录弹出框返回值
function ReturnECatalogData(id) {
    //通过电子目录itemno返回值查找信息
    var objs;
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getECatalogList";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, ID: id },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    //主表赋值

    //查询出第1行是yes的集中采购，没有则以第一行数据为准
    var jud = 0;
    for (var j = 0; j < objs.length; j++) {
        if (objs[j]["CENTRALPROCUREMENT"] == "Yes") {
            jud = j;
            break;
        }
    }

    $("#fld_VENDORNO").val(objs[jud]["VENDORCODE"]);
    $("#fld_VENDORNAME").val(objs[jud]["VENDORNAME"]);

    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() != "Marketing Expense") {
        if (objs[jud]["CENTRALPROCUREMENT"] == "Yes") {
            $("#fld_CENTRALPROCUREMENT_0").prop("checked", true);

            //是集中采购品牌赋值 -赋隐藏品牌值
            $("#fld_ECATLOGBRANDCODE").val(objs[jud]["Brand"]);
        }
        else {
            $("#fld_CENTRALPROCUREMENT_1").prop("checked", true);
        }
    }

    $("#fld_CURRENCY").val(objs[jud]["CURRENCY"]);
    GetExchangeRate($("#fld_EXCHANGERATE"), $("#fld_CURRENCY").val());

    //明细行赋值
    var j = objs.length - $("#tb_PURCHASEORDER_DT tbody tr").length + 1;
    if (j > 0) {
        for (var m = 0; m < j; m++) {
            addRowT('tb_PURCHASEORDER_DT');
        }
    }
    else if (j < 0) {
        for (var m = 0; m < -j; m++) {
            $("#tb_PURCHASEORDER_DT tbody tr").eq($("#tb_PURCHASEORDER_DT tbody tr").length - 2).remove();
        }
    }
    var i = 0;
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        $(this).find("input[id$=fld_DESCRIPTION]").val(objs[i]["DESCRIPTION"]);
        $(this).find("input[id$=fld_QTY]").val(objs[i]["QTY"]);

        $(this).find("input[id$=fld_UNIT_NAME]").val(objs[i]["UNIT"]);
        $(this).find("select[id$=fld_UNIT]").find("option").each(function () {
            if ($(this).text() == objs[i]["UNIT"]) {
                $(this).attr("selected", true);
                return false;
            }
        });
        $(this).find("input[id$=fld_ITEMNO]").val(objs[i]["ITEMNO"]);
        $(this).find("input[id$=fld_UNITPRICE]").val(objs[i]["UNITPRICE"]);
        //$(this).find("input[id$=fld_NETAMOUNT]").val(parseInt($(this).find("input[id$=fld_QTY]").val()) * parseFloat($(this).find("input[id$=fld_UNITPRICE]").val())
        //        / (1 + parseFloat($(this).find("select[id$=fld_VATRATE]").val())));
        //$(this).find("input[id$=fld_GROSSAMOUNT]").val(parseInt($(this).find("input[id$=fld_QTY]").val()) * parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()));
        Fun_OnblurToFixed();
        i++;
    });
    addTotalAmountFun();
}

//报价比较弹出窗口
function OnClickQuotation() {
    var Subject = $("#UserInfo1_fld_PROCESSSUMMARY").val();
    window.open("/Solution/UWF.Process.VendorMasterApplication/ParityForm/QuotationComparison.aspx?Subject=" + Subject);
}

//供应商弹出窗口
function OnClickvendor() {
    var vLanague = $("#div_lang").attr("data-lang");
    var vendorname;
    //中英文转换
    if (vLanague == "zh-CN") {
        vendorname = "供应商选择";
    }
    else {
        vendorname = "Select Vendor";
    }
    var strgetPamaters = $("#UserInfo1_fld_COMPANY").val();
    var url = "../E-Catalog/SelectVendor.aspx?company=" + strgetPamaters;
    var dialog = { title: vendorname, size: "Wide" };
    var iframe = { id: 'frameWindow', src: '"' + url + '"', scrolling: "yes", height: '400px' };
    var buttons = { method: 'ConfirmOnclick()' };
    var dia = { dialog: dialog, iframe: iframe, buttons: buttons };
    showDialog(dia);
}

//供应商返回窗口
function MaterialCodeback(CODE, CONAME, id, PAYMENTTERM, PAYMENTTERM_NAME, PAYMENTMETHOD, PAYMENTMETHOD_NAME, PAYMENTCURRENCY) {
    $("#fld_VENDORNO").val(CODE);
    $("#fld_VENDORNAME").val(CONAME);
    $("#fld_PAYMENTTERM").val(PAYMENTTERM);
    $("#fld_PAYMENTTERM_NAME").val(PAYMENTTERM_NAME);
    $("#fld_PAYMENTMETHOD").val(PAYMENTMETHOD);
    $("#fld_PAYMENTMETHOD_NAME").val($("#fld_PAYMENTMETHOD :checked").text());
    $("#fld_CURRENCY").val(PAYMENTCURRENCY);

    curtypeOnChange(1);
}

//历史价格弹出框
function HistoryPriceOnClick(obj) {
    var itemno = $(obj).parent().parent().prev().find("input[id$=fld_ITEMNO]").val();
    var url = "../E-Catalog/GetHistoryPrice.aspx?ITEMNO=" + itemno;
    var dialog = { title: 'History Price', size: 'Normal' };
    var iframe = { id: 'frameWindow', src: '"' + url + '"', scrolling: "yes", height: '300px' };
    var buttons = { method: '', num: '1' };
    var dia = { dialog: dialog, iframe: iframe, buttons: buttons };
    showDialog(dia);
}

//添加行汇总。
function addTotalAmount(tabId, tdno) {
    var td = "";
    for (var i = 0; i < tdno; i++) {
        td += "<td style='word-break: break-all;'></td>";
    }
    $("#" + tabId + " tbody").append("<tr>" + td + "</tr>");
    var vLanague = $("#div_lang").attr("data-lang");
    var totalamount;
    //中英文转换
    if (vLanague == "zh-CN") {
        totalamount = "总计";
    }
    else {
        totalamount = "Total";
    }
    $("#" + tabId + " tbody tr:last").find("td").eq(9).append("<span class='' style='font-weight: bold;'>" + totalamount + "</span>");
    if (request("Type") == "MYREQUEST" || request("Type") == "MYAPPROVAL" || request("Type") == "report") {
        $("#" + tabId + " tbody tr:last").find("td").eq(10).append("<span id='fld_NETAMOUNTAmount' class='' style='word -break: break-all; word - wrap: break-word; width: 20px; text - align: center;'></span>");
        $("#" + tabId + " tbody tr:last").find("td").eq(11).append("<span id='fld_GROSSAMOUNTAmount' class='' style='word -break: break-all; word - wrap: break-word; width: 20px; text - align: center;'></span>");
        $("#" + tabId + " tbody tr:last").find("td").eq(14).append("<span id='fld_PROJECTBUDGETBALANCEAmount'class='hidden' style='word -break: break-all; word - wrap: break-word; width: 20px; text - align: center;'></span>");
    }
    else {
        $("#" + tabId + " tbody tr:last").find("td").eq(10).append("<input type='text' id='fld_NETAMOUNTAmount' class='item-control   ReadOnly' readonly='readonly' data-prompt-position='bottomLeft' style='background-color: rgb(245, 245, 245);'>");
        $("#" + tabId + " tbody tr:last").find("td").eq(11).append("<input type='text' id='fld_GROSSAMOUNTAmount' class='item-control   ReadOnly' readonly='readonly' data-prompt-position='bottomLeft' style='background-color: rgb(245, 245, 245);'>");
        $("#" + tabId + " tbody tr:last").find("td").eq(14).append("<input type='text' id='fld_PROJECTBUDGETBALANCEAmount' class='item-control hidden  ReadOnly' readonly='readonly' data-prompt-position='bottomLeft' style='background-color: rgb(245, 245, 245);'>");
    }
    budgetbalancetotal();
}

//行汇总计算
function addTotalAmountFun() {
    var netamount = 0;
    var grossamount = 0;
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        //添加汇率计算
        $(this).find("input[id$=fld_NETAMOUNT]").val(GetMoney(parseFloat($(this).find("input[id$=fld_QTY]").val()) * parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($("#fld_EXCHANGERATE").val())));
        $(this).find("input[id$=fld_GROSSAMOUNT]").val(GetMoney(parseFloat($(this).find("input[id$=fld_QTY]").val()) * parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($("#fld_EXCHANGERATE").val())
            * (1 + parseFloat($(this).find("select[id$=fld_VATRATE]").val()))));
        if ($(this).find("input[id$=fld_NETAMOUNT]").val() != "") {
            netamount += parseFloat($(this).find("input[id$=fld_QTY]").val()) * parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($("#fld_EXCHANGERATE").val())
                ;
        }
        if ($(this).find("input[id$=fld_GROSSAMOUNT]").val() != "") {
            grossamount += parseFloat($(this).find("input[id$=fld_QTY]").val()) * parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($("#fld_EXCHANGERATE").val())
                * (1 + parseFloat($(this).find("select[id$=fld_VATRATE]").val()));
        }
    });
    $("#fld_NETAMOUNTAmount").val(GetMoney(netamount));
    $("#fld_GROSSAMOUNTAmount").val(GetMoney(grossamount));
    $("#fld_NETAMOUNTMAIN").val(GetMoney(netamount));
    $("#fld_GROSSAMOUNTMAIN").val(GetMoney(grossamount));

    $("#tb_PURCHASEORDER_PAYMENT_DT tbody tr").each(function () {
        if ($(this).find("input[id*=fld_PERCENTAGEDT]").val() != "") {
            $(this).find("input[id$=fld_AMOUNTDT]").val(GetMoney(parseFloat($(this).find("input[id*=fld_PERCENTAGEDT]").val()) / 100 * GetMoney(grossamount)));
        }
    });
}

//百分比总额计算
function payAmountFun(obj) {
    var per = obj.value;
    var pert = 0;
    $("#tb_PURCHASEORDER_PAYMENT_DT tbody tr").each(function () {
        if ($(this).find("input[id*=fld_PERCENTAGEDT]").val() != "") {
            pert += parseFloat($(this).find("input[id*=fld_PERCENTAGEDT]").val());
        }
    });
    if (pert > 100) {
        var vLanague = $("#div_lang").attr("data-lang");
        var lan;
        //中英文转换
        if (vLanague == "zh-CN") {
            lan = "百分比%不能大于100";
        }
        else {
            lan = "Percentage % cannot be greater than 100";
        }
        alert(lan);
        obj.value = 0;
        $("#" + obj.id).parent().next().find("input").val(0);
    }
    else {
        var to = $("#fld_GROSSAMOUNTMAIN").val();
        if (per != "" && to != "") {
            $("#" + obj.id).parent().next().find("input").val(GetMoney(parseFloat(per) / 100 * to));
        }
    }
}

// 重写添加空白行
function addRowT(tabId) {
    try {
        var tabCtl = document.getElementById(tabId);
        var modelTr = tabCtl.rows[tabCtl.rows.length - 2];
        var newRow = modelTr.cloneNode(true);
        //var rowIndex = newRow.rowIndex - 1;
        var rowIndex = tabCtl.rows.length - 2;
        newRow = changeRowID(newRow, rowIndex);
        clearRow(newRow);
        if ($(tabCtl.rows[1]).attr("class") == "hidden") {
            $(newRow).find(".index").html(rowIndex);
            $(newRow).find(".index").val(rowIndex);
        }
        else {
            $(newRow).find(".index").html(rowIndex + 1);
            $(newRow).find(".index").val(rowIndex + 1);
        }

        //$(tabCtl).find("tbody")[0].appendChild(newRow);
        $(modelTr).after(newRow);
        $("#" + tabId + "_rowCount").val(rowIndex + 1);

        //设置H5控件
        InitDateControls();

        var ubtn = $(newRow).find(".uploadifive-button")[0];
        if (ubtn) {
            $(ubtn).attr("id", $(ubtn).attr("id").replace("uploadifive-", ""));
            $(ubtn).attr("class", $(ubtn).attr("class").replace("uploadifive-button", "attachment"));
            $(ubtn).empty();
        }

        attachUpload($(newRow).find(".attachment")[0]);
        $(newRow).removeClass("hidden");
        if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
            //projectcostcenterrow();
        }
        else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
            projectprofitcenterrow();
        }
        else {
            //明细行单个成本中心查询数据添加
            var costcenterid = $(newRow.getElementsByClassName("td_COSTCENTERNAME")).children().attr("id");
            singleCostCenterSearch(costcenterid);
        }
        reActiveCss();
    }
    catch (e) {
    }
}

//分摊至成本中心跨月控制
function JudgeChangeCostCenter() {
    if (!($("input[name=fld_CHARGEOTHERCOSTCENTER]:checked").val() == "No" && $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration")) {
        $("#tb_PURCHASEORDER_DT").each(function () {
            var date1 = $(this).find("input[id$=fld_DATEFROM]").val();
            var date2 = $(this).find("input[id$=fld_DATETO]").val();
            if (date1 != "" && date2 != "") {
                var date1ary = date1.split("/");
                var date2ary = date2.split("/");
                if (date1ary[1] != date2ary[1] || ((date1ary[0] != date2ary[0]) && date1ary[1] == date2ary[1])) {
                    var vLanague = $("#div_lang").attr("data-lang");
                    var lan1;
                    //中英文转换
                    if (vLanague == "zh-CN") {
                        lan1 = "明细行有跨月！";
                    }
                    else {
                        lan1 = "Detail line has cross month!";
                    }
                    alert(lan1);
                    $("#fld_CHARGEOTHERCOSTCENTER_1").prop("checked", true);
                    return false;
                }
            }
        });
    }
}

//采购类型跨月控制
function JudgePurchaseTypeMonth() {
    if (!($("input[name=fld_CHARGEOTHERCOSTCENTER]:checked").val() == "No" && $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration")) {
        $("#tb_PURCHASEORDER_DT").each(function () {
            var date1 = $(this).find("input[id$=fld_DATEFROM]").val();
            var date2 = $(this).find("input[id$=fld_DATETO]").val();
            if (date1 != "" && date2 != "") {
                var date1ary = date1.split("/");
                var date2ary = date2.split("/");
                if (date1ary[1] != date2ary[1] || ((date1ary[0] != date2ary[0]) && date1ary[1] == date2ary[1])) {
                    $(this).find("input[id$=fld_DATETO]").val("");
                }
            }
        });
    }
}

//日期控制，不能跨月
function monthcontrol(obj) {
    var name = obj.name.split("$");
    var date1 = $("#" + name[0] + "_" + name[1] + "_fld_DATEFROM").val();
    var date2 = $("#" + name[0] + "_" + name[1] + "_fld_DATETO").val();
    var year = new Date().getFullYear();
    var month = new Date().getMonth() + 1;
    var day = new Date().getDate();
    var datenow = year + "/" + month + "/" + day;
    var vLanague = $("#div_lang").attr("data-lang");
    var lan1;
    var lan2;
    var lan3;
    //中英文转换
    if (vLanague == "zh-CN") {
        lan1 = "无法跨月，请分行填写";
        lan2 = "交货日期早于开始日期";
        lan3 = "开始\\交货日期早于当前日期";
        lan4 = "Marketing Expense:请先选择WBS元素";
        lan5 = "请确认项目年份";
    }
    else {
        lan1 = "Cannot cross the month,please fill it out in the branch";
        lan2 = "Delivery date earlier than start date";
        lan3 = "Start date\\Delivery date earlier than current date";
        lan4 = "Marketing Expense:Please select WBS Element First";
        lan5 = "Please confirm the year of project ";
    }
    var date1ary = date1.split("/");
    var date2ary = date2.split("/");

    //Marketing Expense采购类型 有年份控制
    //if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense" &&
    //    ($("#" + name[0] + "_" + name[1] + "_fld_WBSELEMENT").val() == "" || $("#" + name[0] + "_" + name[1] + "_fld_WBSELEMENT").val() == null)) {
    //    alert(lan4);
    //    obj.value = "";
    //}
    //else {
    //开始日期小于当前日期
    //if (Date.parse(datenow) > Date.parse(date1) || Date.parse(datenow) > Date.parse(date2)) {
    //    alert(lan3);
    //    obj.value = "";
    //}
    //else {
    //    //marketing年份控制
    //    var wbs = $("#" + name[0] + "_" + name[1] + "_fld_WBSELEMENT").val();
    //    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense" && (wbs.split('-Y')[1] != obj.value.substring(2, 4))) {
    //        alert(lan5);
    //        obj.value = "";
    //    }
    //}
    //else {
    if (name[2] == "fld_DATEFROM") {
        if ($("#" + name[0] + "_" + name[1] + "_fld_DATEFROM").val() != "" && $("#" + name[0] + "_" + name[1] + "_fld_DATETO").val() == "") {
            var lastdate = getCurrentMonthLast(new Date($("#" + name[0] + "_" + name[1] + "_fld_DATEFROM").val()))
            $("#" + name[0] + "_" + name[1] + "_fld_DATETO").val(lastdate);
        }
    }
    if (!($("input[name=fld_CHARGEOTHERCOSTCENTER]:checked").val() == "No" && $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration")) {
        if (date1 != "" && date2 != "") {
            if (date1ary[1] != date2ary[1] || ((date1ary[0] != date2ary[0]) && date1ary[1] == date2ary[1])) {
                alert(lan1);
                obj.value = "";
            }
            else {
                if (Date.parse(date1) > Date.parse(date2)) {
                    alert(lan2);
                    obj.value = "";
                }
            }
        }
    }
    else {
        if (date1 != "" && date2 != "") {
            if (Date.parse(date1) > Date.parse(date2)) {
                alert(lan2);
                obj.value = "";
            }
        }
    }
    //}
    //}
}

//重写上传明细行
function LoadExcelData(TableID, data, IsReturnsMethod) {
    var vLanague = $("#div_lang").attr("data-lang");
    if (($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") && $("#fld_PROJECTCODE").val() == "") {
        if (vLanague == "zh-CN") {
            alert("请先填写项目编号");
        }
        else {
            alert("Please fill in the project code first");
        }
    }
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "General Procurement" && $("#fld_MAINCOSTCENTERNAME").val() == "") {
        if (vLanague == "zh-CN") {
            alert("请先填写主要成本中心");
        }
        else {
            alert("Please fill in the Main Cost Center Name first");
        }
    }
    else {
        var ClearOrAdd = 0;
        if (data) {
            //首先清除明细行
            var lan1;
            //中英文转换
            if (vLanague == "zh-CN") {
                lan1 = "清除明细行导入？";
            }
            else {
                lan1 = "Clear the detail line import?";
            }

            if (confirm(lan1)) {
                $("#" + TableID + " tbody tr").eq(0).find("td").not(":eq(0),:eq(1)").each(function () {
                    $(this).find("input").val("");
                    $(this).find("select").find("option").eq(0).prop("selected", "selected");
                });
                $("#" + TableID + " tbody tr").not(":eq(0),:last").remove();
                ClearOrAdd = 0;
            }
            else {
                ClearOrAdd = 1;
                var haverow = -1;
                $("#" + TableID + " tbody tr").each(function () {
                    haverow++;
                });
            }
            //data 数据处理
            var objs = eval(data);
            var row1col = objs[0];//JSON.stringify(objs[0]);
            var rows = objs.length;
            var siz = 0, key;
            for (key in row1col) {
                if (row1col.hasOwnProperty(key)) siz++;
            }
            var colums = siz;
            if (ClearOrAdd == 0) {
                for (var i = 0; i < rows - 1; i++) {
                    addRowT(TableID);
                }

                for (var i = 0; i < rows; i++) {
                    var tablerow = $("#" + TableID + " tbody tr").eq(i);
                    var z = 1;
                    for (var key in objs[i]) {
                        z++;
                        var tablecolums = $(tablerow).find("td").eq(z);
                        if ($(tablecolums).find("input").attr("id") != undefined) {
                            $(tablecolums).find("input").val(objs[i][key]);
                        } else if ($(tablecolums).find("select").attr("id") != undefined) {
                            $(tablecolums).find("select").find("option[value='" + objs[i][key] + "']").prop("selected", "selected");
                        }
                    }
                }
            } else {
                for (var i = 0; i < rows; i++) {
                    addRowT(TableID);
                }
                for (var i = 0; i < rows; i++) {
                    var tablerow = $("#" + TableID + " tbody tr").eq(i + haverow);
                    var z = 1;
                    for (var key in objs[i]) {
                        z++;
                        var tablecolums = $(tablerow).find("td").eq(z);
                        if ($(tablecolums).find("input").attr("id") != undefined) {
                            $(tablecolums).find("input").val(objs[i][key]);
                        } else if ($(tablecolums).find("select").attr("id") != undefined) {
                            $(tablecolums).find("select").find("option[value='" + objs[i][key] + "']").prop("selected", "selected");
                        }
                    }
                }
            }
            if (IsReturnsMethod) {
                ExcelImportReturnsMethod();
            }
            //EXECL 验证
            execlValidation();
        }
    }
}

//isDeleteFirstLine 是否删除第一行 bool类型（重写，最后一行总计掠过）
function deleteRowT(tabId, ele, isDeleteFirstLine) {
    var tabCtl = document.getElementById(tabId);
    var tabRows = tabCtl.rows;
    var rowIndex = $(ele).parent().parent()[0].rowIndex;
    var length = $("#" + tabId + " .fa-trash").length;
    if (length == 1 && isDeleteFirstLine != true) {
        clearRow($(ele).parent().parent()[0]);
    }
    else {
        tabCtl.deleteRow(rowIndex);
    }
    $("#" + tabId + "_rowCount").val(tabRows.length - 1);

    tabCtl = document.getElementById(tabId);
    tabRows = tabCtl.rows;
    for (var i = 1; i < tabRows.length - 1; i++) {
        changeRowID(tabRows[i], i - 1);

        $(tabRows[i]).find(".index").html(i);
        $(tabRows[i]).find(".index").val(i);

    }
    addTotalAmountFun();
    budgetbalancetotal();
}

//合同管理点击事件
function OnClickContract() {
    //打开合同管理新建页面
    if (request("Type") == "NEWREQUEST" || request("Type") == "Draft" || (request("Type") == "MYTASK" && $("#fld_CONTRACTMANAGEMENTDATA").val() == "")) {
        var objs;
        var loginuser = $("#UserInfo1_var_ApplicantAccount").val();
        var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
        var method = "getContracturl";
        $.ajax({
            url: url,
            type: "POST",
            async: false,
            dataType: "json",
            data: { Method: method, Loginuser: loginuser },
            success: function (data) {
                if (data != null && data != "") {
                    objs = eval(data);
                    $("#Button_CONTRACTMANAGEMENT").attr("onclick", "javascript:openForm('" + objs[0].taskid + "','','','Contract Management','',this);");
                }
            }
        });
    }
    else {
        //打开合同管理report页面
        if ($("#fld_CONTRACTMANAGEMENTDATA").val() != "") {
            var datatotal = JSON.parse(document.getElementById("fld_CONTRACTMANAGEMENTDATA").value.replaceAll("&quot;", "\""));
            var objs;
            var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
            var method = "getContractIncident";
            $.ajax({
                url: url,
                type: "POST",
                async: false,
                dataType: "json",
                data: { Method: method, Formid: datatotal.hideData.FORMID },
                success: function (data) {
                    if (data != null && data != "") {
                        objs = eval(data);
                        $("#Button_CONTRACTMANAGEMENT").attr("onclick", "javascript:objReport.openForm('" + datatotal.hideData.FORMID + "','" + datatotal.hideData.PROCESSNAME + "'," + objs[0].incident + ");");
                    }
                }
            });
        }
        //无合同管理员页面
        else {
            var vLanague = $("#div_lang").attr("data-lang");
            var contractjudge;
            //中英文转换
            if (vLanague == "zh-CN") {
                contractjudge = "无合同管理！";
            }
            else {
                contractjudge = "No contract management！";
            }
            $("#Button_CONTRACTMANAGEMENT").attr("onclick", "alert('" + contractjudge + "');");
        }
    }
}

//打开合同页面表单
function openForm(taskId, type, serverName, ele) {
    var sheight = screen.height - 150;
    var swidth = screen.width - 10;
    var winoption = "left=0,top=0,height=" + sheight + ",width=" + swidth + ",toolbar=yes,menubar=yes,location=yes,status=yes,scrollbars=yes,resizable=yes";
    //s = window.open('/Portal/Ultimus.UWF.Workflow/OpenForm.aspx?ServerName=' + serverName + '&TaskId=' + taskId + '&Type=' + type + '', '', winoption);
    s = window.open('/Portal/Ultimus.UWF.Workflow/OpenForm.aspx?TaskId=' + taskId + '&Type=NEWREQUEST&ServerName=', '', winoption);
    s.focus();
}

//主表存入商品类别判断
function JudgeCategory() {
    var n = 0;
    var temp = "";
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        temp += $(this).find("input[id$=fld_MERCHANDISECATEGORYCODE]").val() + "$";
    });

    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "JudgeCategory";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, Temp: temp },
        success: function (data) {
            if (data != null && data != "") {
                if (data == "m") {
                    n = "Media";
                }
                else if (data == "a") {
                    n = "Architect";
                }
            }
        }
    });

    $("#fld_MARKETINGCATEGORYJUDGE").val(n);
}

//cer和marketing的wbs弹窗控制
function cerWBSControl(obj) {
    var projectno = $("#fld_PROJECTCODE").val();
    var brandcode = $("#fld_BRANDDIVISION").val();
    //项目编号控制
    if (projectno == "" && $("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        var vLanague = $("#div_lang").attr("data-lang");
        //中英文转换
        if (vLanague == "zh-CN") {
            alert("请先选择项目编号!");
        }
        else {
            alert("Please select the project code first!");
        }
    }
    else {
        var filter1 = "";
        if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
            filter1 = "PROJECTTYPE =&apos;CER&apos; and ACCOUNTASSTELEM=&apos;X&apos; and PROJECTCODE=&apos;" + projectno + "&apos; ";
            selectDataSource({ element: obj.previousElementSibling, title: dataselectlanguage(), fields: 'EXPENSECATEGORY', dataSource: 'Purchase Order-WBS Element', filter: filter1, single: true, IsMethod: true });
        } else {
            filter1 = "PROJECTTYPE like &apos;Marketing%&apos; and BRANDORDIVISIONCODE=&apos;" + brandcode + "&apos; and (WBSELEMENT!=&apos;&apos; or WBSELEMENT is not null) ";
            selectDataSource({ element: obj.previousElementSibling, title: dataselectlanguage(), fields: 'EXPENSECATEGORY', dataSource: 'Purchase Order-WBS Element(Marketing)', filter: filter1, single: true, IsMethod: true, size: 'wide' });
        }
    }
}

//窗口回调函数
function OpenerPageIsMethod(obj) {
    //回调函数预算余额计算
    if (obj.match("fld_WBSELEMENT")) {
        //CER和Marketing
        if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER" || $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
            projectcostcenterrow(obj);
            budgetCalculate(obj);
        }
        //SIS
        else {
            budgetCalculateSis(obj);
        }
    }
    if (obj.match("fld_MERCHANDISECATEGORY")) {
        budgetCalculateSis(obj);
    }
    //回调函数项目选择清初明细行数据
    if (obj == "fld_PROJECTCODE") {
        if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
            //CER项目POitem赋值
            CERProjectCode();
            //cer成本中心控制(没用了)
            CostCenterSearch(0);
            //明细行利润中心赋值
            projectprofitcenterrow();
        }
        else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration") {
            //SIS带入
            $("#fld_NETAMOUNTMAIN").val(GetMoney($("#fld_NETAMOUNTMAIN").val()));
            $("#fld_GROSSAMOUNTMAIN").val(GetMoney($("#fld_GROSSAMOUNTMAIN").val()));
            SisProjectCode();
        }
        else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
            //Marketing明细行成本中心赋值
            //projectcostcenterrow();
            clearProjectCode();
        }
        else {
            clearProjectCode();
        }
    }
    //回调函数明细行描述控制
    if (obj.match("fld_DESCRIPTION")) {
        $("#" + obj.replace("DESCRIPTION", "UNIT")).find("option").each(function () {
            if ($(this).text() == ($("#" + obj.replace("DESCRIPTION", "UNIT_NAME")).val())) {
                $(this).attr("selected", true);
                return false;
            }
        });
        addTotalAmountFun();
    }

}

//明细行预算余额计算(CER,Markerting)
function budgetCalculate(obj) {
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        var projectcode = $("#fld_PROJECTCODE").val();
        var elementno = $("#" + obj).val();
    }
    else {
        var projectcode = $("#" + obj).val().split('-')[0];
        var elementno = $("#" + obj).val().split('-')[1];
        var year = "20" + $("#" + obj).val().split('-Y')[1];
    }
    var objs;
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getBudgetBalance";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, ProjectCode: projectcode, ElementNo: elementno, Year: year, Type: $("input[name=fld_PROCURMENTTYPE]:checked").val() },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    $("#" + obj.replace("fld_WBSELEMENT", "fld_PROJECTBUDGETBALANCE")).val(GetMoney(objs));
    //预算总额计算，除General
    budgetbalancetotal();
}

//所有明细行预算余额计算(CER,Markerting)
function budgetCalculateAll() {
    $("#tb_PURCHASEORDER_DT tbody tr ").not(":last").each(function () {
        if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
            var projectcode = $("#fld_PROJECTCODE").val();
            var elementno = $(this).find("input[id$=fld_WBSELEMENT]").val();
        }
        else {
            var projectcode = $(this).val().split('-')[0];
            var elementno = $(this).val().split('-')[1];
            var year = "20" + $(this).val().split('-Y')[1];
        }
        var objs;
        var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
        var method = "getBudgetBalance";
        $.ajax({
            url: url,
            type: "POST",
            async: false,
            dataType: "json",
            data: { Method: method, ProjectCode: projectcode, ElementNo: elementno, Year: year, Type: $("input[name=fld_PROCURMENTTYPE]:checked").val() },
            success: function (data) {
                if (data != null && data != "") {
                    objs = eval(data);
                }
            }
        });
        $(this).find("input[id$=fld_PROJECTBUDGETBALANCE]").val(GetMoney(objs));
    });
    //预算总额计算，除General
    budgetbalancetotal();
}

//提交前预算控制CER
//根据项目编号总金额控制预算
function budgetControl() {
    var projectcode = $("#fld_PROJECTCODE").val();
    var netamount = $("#fld_NETAMOUNTMAIN").val();

    var formid = $("#UserInfo1_fld_FORMID").val();        //记录单号用于排除退回时占用的预算
    //var elementno = new Array();
    //var grossamount = new Array();
    ////统计占用预算，相同的element合并
    //$("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
    //    //第一条直接赋值
    //    if (elementno.length == 0) {
    //        elementno[0] = $(this).find("input[id$=fld_WBSELEMENT]").val();
    //        grossamount[0] = parseFloat($(this).find("input[id$=fld_QTY]").val()) *
    //                    parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($("#fld_EXCHANGERATE").val());
    //    }
    //    else {
    //        //多条循环，如果相同，则直接金额相加。跳出循环。temp记录是否存在相同项。
    //        var temp = 0;
    //        for (var i = 0; i < elementno.length; i++) {
    //            if (elementno[i] == $(this).find("input[id$=fld_WBSELEMENT]").val()) {
    //                grossamount[i] += parseFloat($(this).find("input[id$=fld_QTY]").val()) *
    //                    parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($("#fld_EXCHANGERATE").val());
    //                temp = 1;
    //                break;
    //            }
    //        }
    //        //都不相同的情况
    //        if (temp == 0) {
    //            elementno[elementno.length] = $(this).find("input[id$=fld_WBSELEMENT]").val();
    //            grossamount[grossamount.length] = parseFloat($(this).find("input[id$=fld_QTY]").val()) *
    //                        parseFloat($(this).find("input[id$=fld_UNITPRICE]").val()) * parseFloat($("#fld_EXCHANGERATE").val());
    //        }

    //    }
    //});
    //var strelementno = "";
    //var strgrossamount = "";
    //for (var j = 0; j < elementno.length; j++) {
    //    strelementno += elementno[j] + "|";
    //    strgrossamount += grossamount[j] + "|";
    //}
    var objs;
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getBudgetControl";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, ProjectCode: projectcode, NetAmount: netamount, formid: formid },
        success: function (data) {
            if (data != null && data != "") {
                objs = data;
            }
        }
    });
    return objs;
}

//提交前预算控制marketing
function marketingbudgetControl() {
    var formid = $("#UserInfo1_fld_FORMID").val();
    var data = getmarkertnorepeat();
    var brand = "";
    var year = "";
    var netamount = "";
    for (var i = 0; i < data.length; i++) {
        brand += data[i].brand + "|";
        year += data[i].year + "|";
        netamount += data[i].value + "|";
    }
    var objs;
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getMarketingBudgetControl";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, Brand: brand, Year: year, NetAmount: netamount, formid: formid },
        success: function (data) {
            if (data != null && data != "") {
                objs = data;
            }
        }
    });
    return objs;
}

//项目选择明细行值清空
function clearProjectCode() {
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        $(this).find("input[id$=fld_WBSELEMENT]").val("");
        $(this).find("input[id$=fld_EXPENSECATEGORY]").val("");
        $(this).find("input[id$=fld_PROJECTBUDGETBALANCE]").val("");
        $(this).find("input[id$=fld_MERCHANDISECATEGORYCODE]").val("");
    });
}

//项目编号POitem赋值
function CERProjectCode() {
    $("#tb_PURCHASEORDER_DT tbody tr").eq(0).find("td").not(":eq(0),:eq(1)").each(function () {
        $(this).find("input").val("");
        $(this).find("select").find("option").eq(0).prop("selected", "selected");
    });
    $("#tb_PURCHASEORDER_DT tbody tr").not(":eq(0),:last").remove();

    var projectcode = $("#fld_PROJECTCODE").val();
    var objs;
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getCERPOItem";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, ProjectCode: projectcode },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
                for (var j = 1; j < objs.length; j++) {
                    addRowT('tb_PURCHASEORDER_DT');
                }
                var k = 0;
                $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
                    $(this).find("input[id$=fld_WBSELEMENT]").val(objs[k].WBSELEMENT);
                    $(this).find("input[id$=fld_EXPENSECATEGORY]").val(objs[k].PLANNINGDESCRIPTION);
                    $(this).find("input[id$=fld_MERCHANDISECATEGORY]").val(objs[k].Description);
                    $(this).find("input[id$=fld_MERCHANDISECATEGORYCODE]").val(objs[k].Code);
                    k++;
                });
            }
        }
    });
    budgetCalculateAll();
}

//sis 预算总计
function budgetCalculateSis(obj) {
    var temp = obj.split('fld_MERCHANDISECATEGORY');
    if (temp.length == 2) {
        var elementno = $("#" + temp[0] + "fld_WBSELEMENT").val();
    }
    else {
        var elementno = $("#" + obj).val();
    }
    if (elementno == "S0101") {
        var objs;
        var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
        var method = "getBudgetBalanceSis";
        var brand = $("#fld_BRANDDIVISION").val();
        $.ajax({
            url: url,
            type: "POST",
            async: false,
            dataType: "json",
            data: { Method: method, Brand: brand },
            success: function (data) {
                if (data != null && data != "") {
                    objs = eval(data);
                }
            }
        });
        if (temp.length == 2) {
            $("#" + temp[0] + "fld_PROJECTBUDGETBALANCE").val(objs);
        }
        else {
            $("#" + obj.replace("fld_WBSELEMENT", "fld_PROJECTBUDGETBALANCE")).val(objs);
        }
    }
}

//返回合同号明细行数据
function SendSonData() {
    var senddata = new Array();
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        var unitprice = GetMoney(parseFloat($(this).find("input[id$=fld_UNITPRICE]").val())
            * parseFloat($("#fld_EXCHANGERATE").val()));
        var rowdata = {
            Description: $(this).find("input[id$=fld_DESCRIPTION]").val(),
            Unit: $(this).find("select[id$=fld_UNIT]").val(),
            UnitName: $(this).find("input[id$=fld_UNIT_NAME]").val(),
            UnitPrice: unitprice,
            QTY: $(this).find("input[id$=fld_QTY]").val(),
            NetAmount: $(this).find("input[id$=fld_NETAMOUNT]").val()
        };
        senddata.push(rowdata);
    });
    return senddata;
}

//costcenter模糊查询(页面刷新和点击)(markreting cer 排除)
//type:1 页面刷新触发
//type:0 点击触发
function CostCenterSearch(type) {
    //多语言控制
    var vLanague = $("#div_lang").attr("data-lang");
    var tit;
    var tit2;
    var tit4;
    //中英文转换
    if (vLanague == "zh-CN") {
        tit = "该品牌/部门无成本中心！";
        tit2 = "请先选择主要成本中心！";
        tit4 = "请先选择wbs元素！";
    }
    else {
        tit = "This brand/division has no cost center!";
        tit2 = "Please select the main cost center first!";
        tit4 = "Please select WBS Element first!";
    }
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "General Procurement" || $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration") {
        //主表品牌编号
        var branddivisioncode = $("#fld_BRANDDIVISION").val();

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //主表成本中心
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //将值暂时保存，在重新绑定下拉数据后，将值赋上(主表)
        //页面刷新，草稿箱，待办任务触发
        var maincostrecord;
        if (type == 1 && $("#fld_MAINCOSTCENTERNAME").val() != "") {
            maincostrecord = $("#fld_MAINCOSTCENTERNAME").val();
        }
        //页面点击触发
        else if (type == 0) {
            $("#fld_MAINCOSTCENTERNAME").val("");
            $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
                $(this).find("input[id$=fld_COSTCENTERNAME]").val("");
            });
        }
        //主表成本中心模糊查询添加
        var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
        var method = "getCostCenter";
        var value1 = null;
        var maindata = costCenterMainData(url, method, branddivisioncode, "");
        $("#fld_MAINCOSTCENTERNAME").bigAutocomplete({
            data: maindata,
            width: 700,
            custom: false,  //不允许用户自定义值
            callback: function (row, param) {//(回调事件，只有点击后触发)
                //定义custom:false后,如果用户清空值,row就是null
                if (row != null) {
                    value1 = row.value;
                    console.log(value1);
                    //明细行默认值添加(点击触发)
                    rowdefluatcostcenter(row.title);
                    //明细行下拉绑定(点击触发)
                    allCostCenterSearch();
                } else {
                    //value1 = row.value;//这样会异常
                    value1 = null;
                    console.log(value1);
                }
            }
        });
        //如果没有数据
        if (maindata[0].title == "") {
            $("#fld_MAINCOSTCENTERNAME").attr("placeholder", tit);
        }
        else {
            $("#fld_MAINCOSTCENTERNAME").removeAttr("placeholder");
        }
        //页面刷新且有值时，将值暂时保存，在重新绑定下拉数据后，将值赋上(主表)
        //页面刷新，草稿箱，待办任务触发
        if (type == 1 && maincostrecord != "") {
            $("#fld_MAINCOSTCENTERNAME").val(maincostrecord);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //明细表成本中心
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //成本中心存在
        if ($("#fld_MAINCOSTCENTERNAME").val() != "") {
            //将值暂时保存，在重新绑定下拉数据后，将值赋上(明细表)
            //页面刷新，草稿箱，待办任务触发
            var aryrecord = new Array();
            if (type == 1) {
                var ii = 0;
                $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
                    aryrecord[ii] = $(this).find("input[id$=fld_COSTCENTERNAME]").val();
                    ii++;
                });
            }
            //获取主表成本中心公司编码
            var companycode = getcompanycode($("#fld_MAINCOSTCENTERNAME").val());
            detailmethod = "getShortCostCenter";
            var value2 = null;
            var rowdata = costCenterRowData(url, detailmethod, branddivisioncode, companycode, "");
            $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
                $(this).find("input[id$=fld_COSTCENTERNAME]").bigAutocomplete({
                    data: rowdata,
                    width: 250,
                    custom: false,  //不允许用户自定义值
                    callback: function (row, param) {
                        //定义custom:false后,如果用户清空值,row就是null
                        if (row != null) {
                            value2 = row.value;
                            console.log(value1);
                        } else {
                            //value1 = row.value;//这样会异常
                            value2 = null;
                            console.log(value1);
                        }
                    }
                });
                //如果没有数据
                if (rowdata[0].title == "") {
                    $(this).find("input[id$=fld_COSTCENTERNAME]").attr("placeholder", tit);
                }
                else {
                    $(this).find("input[id$=fld_COSTCENTERNAME]").removeAttr("placeholder");
                }
            });
            //页面刷新且有值时，将值暂时保存，在重新绑定下拉数据后，将值赋上(明细表)
            if (type == 1) {
                var ii = 0;
                $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
                    $(this).find("input[id$=fld_COSTCENTERNAME]").val(aryrecord[ii]);
                    ii++;
                });
            }
        }
        //成本中心不存在
        else {
            $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
                $(this).find("input[id$=fld_COSTCENTERNAME]").attr("placeholder", tit2);
            });
        }
    }
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
        $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
            $(this).find("input[id$=fld_COSTCENTERNAME]").attr("placeholder", tit4);
        });
    }
}

//明细行所有成本中心查询数据添加
function allCostCenterSearch() {
    //多语言控制
    var vLanague = $("#div_lang").attr("data-lang");
    var tit;
    var tit2;
    //中英文转换
    if (vLanague == "zh-CN") {
        tit = "该品牌/部门无成本中心！";
        tit2 = "请先选择主要成本中心！";
    }
    else {
        tit = "This brand/division has no cost center!";
        tit2 = "Please select the main cost center first!";
    }
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "General Procurement" || $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration") {
        //成本中心存在
        if ($("#fld_MAINCOSTCENTERNAME").val() != "") {
            var companycode = getcompanycode($("#fld_MAINCOSTCENTERNAME").val());
            var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
            var detailmethod = "getShortCostCenter";
            var rowdata = costCenterRowData(url, detailmethod, $("#fld_BRANDDIVISION").val(), companycode, "");
            var value1 = null;
            $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
                $(this).find("input[id$=fld_COSTCENTERNAME]").bigAutocomplete({
                    data: rowdata,
                    width: 250,
                    custom: false,  //不允许用户自定义值
                    callback: function (row, param) {
                        //定义custom:false后,如果用户清空值,row就是null
                        if (row != null) {
                            value1 = row.value;
                            console.log(value1);
                        } else {
                            //value1 = row.value;//这样会异常
                            value1 = null;
                            console.log(value1);
                        }
                    }
                });
                //如果没有数据
                if (rowdata[0].title == "") {
                    $(this).find("input[id$=fld_COSTCENTERNAME]").attr("placeholder", tit);
                }
                else {
                    $(this).find("input[id$=fld_COSTCENTERNAME]").removeAttr("placeholder");
                }
            });
        }
        //成本中心不存在
        else {
            $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
                $(this).find("input[id$=fld_COSTCENTERNAME]").attr("placeholder", tit2);
            });
        }
    }
}

//明细行单个成本中心查询数据添加
function singleCostCenterSearch(id) {
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "General Procurement" || $("input[name=fld_PROCURMENTTYPE]:checked").val() == "Retailer POS Decoration") {
        //多语言控制
        var vLanague = $("#div_lang").attr("data-lang");
        var tit;
        var tit2;
        //中英文转换
        if (vLanague == "zh-CN") {
            tit = "该品牌/部门无成本中心！";
            tit2 = "请先选择主要成本中心！";
        }
        else {
            tit = "This brand/division has no cost center!";
            tit2 = "Please select the main cost center first!";
        }
        if ($("#fld_MAINCOSTCENTERNAME").val() != "") {
            //获取主表成本中心公司
            var companycode = getcompanycode($("#fld_MAINCOSTCENTERNAME").val());
            var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
            var detailmethod = "getShortCostCenter";
            var rowdata = costCenterRowData(url, detailmethod, $("#fld_BRANDDIVISION").val(), companycode, "");
            var value1 = null;
            $("#" + id).bigAutocomplete({
                data: rowdata,
                width: 250,
                custom: false,  //不允许用户自定义值
                callback: function (row, param) {
                    //定义custom:false后,如果用户清空值,row就是null
                    if (row != null) {
                        value1 = row.value;
                        console.log(value1);
                    } else {
                        //value1 = row.value;//这样会异常
                        value1 = null;
                        console.log(value1);
                    }
                }
            });
            //如果没有数据
            if (rowdata[0].title == "") {
                $("#" + id).attr("placeholder", tit);
            }
            else {
                $("#" + id).removeAttr("placeholder");
            }
            singlerowdefluatcostcenter($("#fld_MAINCOSTCENTERNAME").val(), id);
        }
        else {
            $("#" + id).attr("placeholder", tit2);
        }
    }
}

//获取成本中心公司
function getcompanycode(maincostcenter) {
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var obj = "";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: "Getcompanycode", Maincostcenter: maincostcenter },
        success: function (data) {
            if (data != null && data != "") {
                obj = data[0].companycode;
            }
        }
    });
    return obj;
}

//主表成本中心数据查询
function costCenterMainData(url, method, branddivisioncode, projectcode) {
    var objs;
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, BRANDDIVISIONCODE: branddivisioncode, ProjectCode: projectcode },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    return objs;
}

//明细表成本中心数据查询
function costCenterRowData(url, method, branddivisioncode, companycode, projectcode) {
    var objs;
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, BRANDDIVISIONCODE: branddivisioncode, CCompanycode: companycode, ProjectCode: projectcode },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    return objs;
}

//手写描述清楚itemno
function clearitemo(obj) {
    var itemid = obj.id.replace("DESCRIPTION", "ITEMNO");
    $("#" + itemid).val("");
}

//单价格式重写
function Fun_OnblurToFixedString(obj) {
    if ($(obj).val() != "" && $(obj).val() >= 0) {
        $(obj).val(FormatNum($(obj).val(), 2));
    } else if ($(obj).val() < 0) {
        //$(this).val(FormatNum(Math.abs($(this).val()), 2));
        $(obj).val(FormatNum($(obj).val(), 2));

    } else {
        $(obj).val("");
    }
}

//供应商申请单号报价比较单查找
function vendorquotationdata() {
    var vendordocumentno = $("#fld_ORRELATEDVENDORAPPLICATIONNO").val();
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getvendorquotationdata";
    var objs;
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, VendorDocumentNo: vendordocumentno },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    return objs;
}

//获取当前月份最后一天
function getCurrentMonthLast(obj) {
    var date = obj;
    var currentMonth = date.getMonth();
    var nextMonth = ++currentMonth;
    var nextMonthFirstDay = new Date(date.getFullYear(), nextMonth, 1);
    var oneDay = 1000 * 60 * 60 * 24;
    var lastTime = new Date(nextMonthFirstDay - oneDay);
    var month = parseInt(lastTime.getMonth() + 1);
    var day = lastTime.getDate();
    if (month < 10) {
        month = '0' + month
    }
    if (day < 10) {
        day = '0' + day
    }

    return (date.getFullYear() + '/' + month + '/' + day);

}

//sis中间表数据带入
function SisProjectCode() {
    var sispoguid = $("#fld_SISPOGUID").val();
    var objs;
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getSISPOItem";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, Sispoguid: sispoguid },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    $("#tb_PURCHASEORDER_DT tbody tr").not(":eq(0),:last").remove();
    $("#tb_PURCHASEORDER_DT tbody tr").eq(0).find("td").not(":eq(0),:eq(1)").each(function () {
        $(this).find("input").val("");
        $(this).find("select").find("option").eq(0).prop("selected", "selected");
    });

    for (var j = 1; j < objs.length; j++) {
        addRowT('tb_PURCHASEORDER_DT');
    }
    var k = 0;
    //明细
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        $(this).find("input[id$=fld_BRAND]").val(objs[k].BRAND);
        $(this).find("input[id$=fld_BRANDNAME]").val(objs[k].BRANDNAME);
        $(this).find("input[id$=fld_MERCHANDISECATEGORY]").val(objs[k].MERCHANDISECATEGORY);
        $(this).find("input[id$=fld_MERCHANDISECATEGORYCODE]").val(objs[k].MERCHANDISECATEGORYCODE);
        $(this).find("input[id$=fld_WBSELEMENT]").val(objs[k].WBSELEMENT);
        $(this).find("input[id$=fld_EXPENSECATEGORY]").val(objs[k].EXPENSECATEGORY);
        $(this).find("input[id$=fld_DESCRIPTION]").val(objs[k].DESCRIPTION);
        $(this).find("input[id$=fld_QTY]").val(objs[k].QTY);
        $(this).find("input[id$=fld_UNITPRICE]").val(GetMoney(objs[k].UNITPRICE));
        $(this).find("select[id$=fld_VATRATE]").val(objs[k].VATRATE);
        $(this).find("input[id$=fld_VATRATE_NAME]").val(objs[k].VATRATE_NAME);
        $(this).find("input[id$=fld_VATRATE_CODE]").val(objs[k].VATRATE_CODE);
        $(this).find("input[id$=fld_NETAMOUNT]").val(GetMoney(objs[k].NETAMOUNT));
        $(this).find("input[id$=fld_GROSSAMOUNT]").val(GetMoney(objs[k].GROSSAMOUNT));
        $(this).find("input[id$=fld_DATEFROM]").val(objs[k].DATEFROM);
        $(this).find("input[id$=fld_DATETO]").val(objs[k].DATETO);
        $(this).find("input[id$=fld_PROJECTBUDGETBALANCE]").val(GetMoney(objs[k].PROJECTBUDGETBALANCE));
        k++;
    });

    $("#fld_NETAMOUNTAmount").val($("#fld_NETAMOUNTMAIN").val());
    $("#fld_GROSSAMOUNTAmount").val($("#fld_GROSSAMOUNTMAIN").val());
    budgetbalancetotal();
}

//sis表单控件控制
//i为0 刷新 
//i为1 控制
function SisControl(i) {
    //从sis带入
    if ($("#fld_SISPOSTATUS").val() == 2 || $("#fld_SISPOSTATUS").val() == 1) {
        $("input[name=fld_PROCURMENTTYPE]").attr("disabled", "disabled");
        $("#fld_PROJECTCODE").removeAttr("onclick");
    }
    //手动选择
    else {
    }
    //公共控制
    $("#fld_VENDORNO").removeAttr("onclick");
    $("#fld_CURRENCY").attr("disabled", "disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_MERCHANDISECATEGORY").find("span").removeAttr("onclick");
    $("#tb_PURCHASEORDER_DT tbody .td_WBSELEMENT").find("span").removeAttr("onclick");
    $("#tb_PURCHASEORDER_DT tbody .td_DESCRIPTION").find("span").removeAttr("onclick");
    $("#tb_PURCHASEORDER_DT tbody .td_QTY").find("input").attr("disabled", "disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_UNITPRICE").find("input").attr("disabled", "disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_VATRATE").find("select").attr("disabled", "disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_DATEFROM").find("input").attr("disabled", "disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_DATEFROM").find("span").removeAttr("onclick");
    $("#tb_PURCHASEORDER_DT tbody .td_DATETO").find("input").attr("disabled", "disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_DATETO").find("span").removeAttr("onclick");

}

//sis表单控件控制（手动选择）
//i为0 刷新 
//i为1 控制
function noSisControl(i) {
    //公共控制

    //供应商
    $("#fld_VENDORNO").attr("onclick", "OnClickvendor()");
    $("#fld_CURRENCY").removeAttr("disabled");

    $("#tb_PURCHASEORDER_DT tbody .td_QTY").find("input").removeAttr("disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_UNITPRICE").find("input").removeAttr("disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_VATRATE").find("select").removeAttr("disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_DATEFROM").find("input").removeAttr("disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_DATEFROM").find("span.iconTime").attr("onclick", "$(this.previousElementSibling).click()");
    $("#tb_PURCHASEORDER_DT tbody .td_DATEFROM").find("span.iconRemove").attr("onclick", "this.parentNode.firstElementChild.value=''");
    $("#tb_PURCHASEORDER_DT tbody .td_DATETO").find("input").removeAttr("disabled");
    $("#tb_PURCHASEORDER_DT tbody .td_DATEFROM").find("span.iconTime").attr("onclick", "$(this.previousElementSibling).click()");
    $("#tb_PURCHASEORDER_DT tbody .td_DATETO").find("span.iconRemove").attr("onclick", "this.parentNode.firstElementChild.value=''");
}

//提交前为每一个明细行成本中心找到品牌
function rowFillBrand() {
    var costcenter = "";
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() != "CER") {
        $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
            costcenter = $(this).find("input[id$=fld_COSTCENTERNAME]").val();
            var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
            var method = "getcostcenterbrand";
            var objs;
            $.ajax({
                url: url,
                type: "POST",
                async: false,
                dataType: "json",
                data: { Method: method, Costcenter: costcenter },
                success: function (data) {
                    if (data != null && data != "") {
                        objs = eval(data);
                    }
                }
            });
            $(this).find("input[id$=fld_BRAND]").val(objs[0].BRANDDIVISIONCODE);
            $(this).find("input[id$=fld_BRANDNAME]").val(objs[0].DEPARTMENTNAME);
        });
    }
    else {
        $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
            $(this).find("input[id$=fld_BRAND]").val($("#fld_BRANDDIVISION").val());
            $(this).find("input[id$=fld_BRANDNAME]").val($("#fld_BRANDDIVISION_NAME").val());
        });
    }
}

//统计market行数据
function getmarkertnorepeat() {
    var data = new Array();
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        var totalprice = parseFloat($(this).find("input[id$=fld_QTY]").val()) * parseFloat($(this).find("input[id$=fld_UNITPRICE]").val())
            * parseFloat($("#fld_EXCHANGERATE").val());
        var rowdata = {
            "brand": $(this).find("input[id$=fld_BRAND]").val(),
            "year": $(this).find("input[id$=fld_DATETO]").val().substring(0, 4),
            "value": parseFloat(totalprice.toFixed(6))
        };
        data.push(rowdata);
    });
    var newdata = GroupBy(data, ["brand", "year"], 0);
    return newdata;
}

//将数组group相加，仅适用于key两个字段
//data 需要group的数组
//key 根据group的字段
//callback 再说 
function GroupBy(data, key, callback) {
    debugger;
    if (key.length > 0) {
        for (var j = 0; j < data.length; j++) {
            for (var i = 0; i < data.length - j - 1; i++) {
                if (data[j][key[0]] == data[j + i + 1][key[0]] && data[j][key[1]] == data[j + i + 1][key[1]]) {
                    data[j].value = parseFloat(data[j].value) + parseFloat(data[j + i + 1].value);
                    //eval("var data" + j + "=new Array()");
                    data.splice(j + i + 1, 1);
                    i--;
                }
            }
        }
        //key.splice(0, 1);
        //if (key.length != 0) {
        //    data = GroupBy(data, key, callback);
        //}
    }
    return data;
}

//数据选择多语言
function dataselectlanguage() {
    var vLanague = $("#div_lang").attr("data-lang");
    var tit;
    //中英文转换
    if (vLanague == "zh-CN") {
        tit = "数据选择";
    }
    else {
        tit = "Data Selection";
    }
    return tit;
}

//税率绑定
function setddlLinkTax(purchasetype) {
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getTaxRate";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, PurchaseType: purchasetype },
        success: function (data) {
            if (data != null && data != "") {
                $("#tb_PURCHASEORDER_DT").find("select[id$=fld_VATRATE]").each(function () {
                    $(this).empty();
                    $(this).append("<option value=''></option>");
                    var objs = eval(data);
                    for (var i = 0; i < objs.length; i++) {
                        $(this).append("<option value='" + objs[i].VALUE + "'>" + objs[i].NAME + "</option>");
                    }
                });
            }
        }
    });
}

//切换税率清空操作
function taxclear() {
    $("#tb_PURCHASEORDER_DT tbody tr ").not(":last").each(function () {
        $(this).find("input[id$=fld_VATRATE]").val("");
        $(this).find("input[id$=fld_VATRATE_NAME]").val("");
        $(this).find("input[id$=fld_VATRATE_CODE]").val("");
        $(this).find("input[id$=fld_GROSSAMOUNT]").val("");
    });
    $("#fld_GROSSAMOUNTAmount").val("");
    $("#fld_GROSSAMOUNTMAIN").val("");
}

//行成本中心赋值
function rowdefluatcostcenter(value1) {
    var costCenterCode = value1.split("-");
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getrowdefaultcostcenter";
    var objs;
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, costCenterCode: costCenterCode[0] },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });

    $("#tb_PURCHASEORDER_DT tbody tr ").not(":last").each(function () {
        $(this).find("input[id$=fld_BRAND]").val(objs[0].VALUE);
        $(this).find("input[id$=fld_BRANDNAME]").val(objs[0].BRAND);
        $(this).find("input[id$=fld_COSTCENTERNAME]").val(objs[0].CODE + "-" + objs[0].NAME);
    });
}

//单个行成本中心赋值
function singlerowdefluatcostcenter(value1, id) {
    var costCenterCode = value1.split("-");
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "getrowdefaultcostcenter";
    var objs;
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, costCenterCode: costCenterCode[0] },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    $("#" + id).val(objs[0].CODE + "-" + objs[0].NAME);
}

//EXECL 验证
function execlValidation() {
    //成本中心验证
    CostCenterValidation();
    //商品类别验证
    MerchandiseCategoryValidation();
    //wbs验证
    WBSValidation();

    //税率验证
    taxValidation();
    //日期验证
    dateValidation();
    //金额计算
    addTotalAmountFun();
    //所有预算计算
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense" || $("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        budgetCalculateAll();
    }

}

//成本中心(利润中心)验证
function CostCenterValidation() {
    var vLanague = $("#div_lang").attr("data-lang");
    var code = $("#fld_BRANDDIVISION").val();
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "General Procurement") {
        var i = 1;
        var companycode = getcompanycode($("#fld_MAINCOSTCENTERNAME").val());
        $("#tb_PURCHASEORDER_DT ").find("input[id$=fld_COSTCENTERNAME]").each(function () {
            if ($(this).val() != "") {
                var objs;
                var _this = this;
                var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
                var method = "costcenterValidation";
                $.ajax({
                    url: url,
                    type: "POST",
                    async: false,
                    dataType: "json",
                    data: { Method: method, Costcenter: $(_this).val().substring(0, 10), Brand: code, Companycode: companycode },
                    success: function (data) {
                        if (data != null && data != "") {
                            objs = eval(data);
                        }
                    }
                });
                if (objs == 2) {
                    if (vLanague == "zh-CN") {
                        alert("第" + i + "行成本中心填写错误");
                    }
                    else {
                        alert("The cost center in line " + i + " is filled in incorrectly");
                    }
                    $(_this).val("");
                }
                else {
                    $(_this).val(objs[0].sn);
                }
            }
            i++;
        });
    }
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        projectprofitcenterrow();
    }
}

//商品类别验证
function MerchandiseCategoryValidation() {
    var vLanague = $("#div_lang").attr("data-lang");
    var typep = $("input[name=fld_PROCURMENTTYPE]:checked").val();
    if (typep == "General Procurement")
        typep = "General";
    else if (typep == "Marketing Expense")
        typep = "Marketing";
    var i = 1;
    $("#tb_PURCHASEORDER_DT ").find("input[id$=fld_MERCHANDISECATEGORY]").each(function () {
        if ($(this).val() != "") {
            var objs;
            var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
            var method = "MerchandiseCategoryValidation";
            var _this = this;
            $.ajax({
                url: url,
                type: "POST",
                async: false,
                dataType: "json",
                data: { Method: method, Typep: typep, MerchandiseCategory: $(_this).val() },
                success: function (data) {
                    if (data != null && data != "") {
                        objs = eval(data);
                    }
                    if (objs == 2) {
                        if (vLanague == "zh-CN") {
                            alert("第" + i + "行商品类别填写错误");
                        }
                        else {
                            alert("The Merchandise Category in line " + i + " is filled in incorrectly");
                        }
                        $(_this).val("");
                    }
                    else {
                        $(_this).parent().parent().next().find("input[id$=fld_MERCHANDISECATEGORYCODE]").val(objs[0].code);
                    }
                }
            });
        }
        i++;
    });
}

//wbs验证
function WBSValidation() {
    var vLanague = $("#div_lang").attr("data-lang");
    var typep = $("input[name=fld_PROCURMENTTYPE]:checked").val();
    var projectno = $("#fld_PROJECTCODE").val();
    if (typep == "General Procurement") {
        $("#tb_PURCHASEORDER_DT ").find("input[id$=fld_WBSELEMENT]").val("");
        //if (vLanague == "zh-CN") {
        //    alert("该采购类型无wbs元素");
        //}
        //else {
        //    alert("This purchase type has no WBS elements");
        //}
    }
    else {
        if (typep == "Marketing Expense")
            typep = "Marketing";
        var i = 1;
        $("#tb_PURCHASEORDER_DT ").find("input[id$=fld_WBSELEMENT]").each(function () {
            if ($(this).val() != "") {
                var wbs = $(this).val();
                if ((wbs.split('-').length - 1) == 2) {  //验证格式
                    var objs;
                    var _this = this;
                    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
                    var method = "WBSValidation";
                    $.ajax({
                        url: url,
                        type: "POST",
                        async: false,
                        dataType: "json",
                        data: { Method: method, Typep: typep, WBS: $(_this).val(), ProjectCode: projectno, Brand: $("#fld_BRANDDIVISION").val() },
                        success: function (data) {
                            if (data != null && data != "") {
                                objs = eval(data);
                            }
                            if (objs == 2) {
                                if (vLanague == "zh-CN") {
                                    alert("第" + i + "行WBS元素填写错误");
                                }
                                else {
                                    alert("The WBS element in line " + i + " is filled in incorrectly");
                                }
                                $(_this).val("");
                            }
                            else if (objs == 3) {
                                if (vLanague == "zh-CN") {
                                    alert("第" + i + "行WBS元素与品牌不匹配");
                                }
                                else {
                                    alert("The WBS element in line " + i + " does not match brand");
                                }
                                $(_this).val("");
                            }
                            else {
                                if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
                                    $(_this).parent().parent().next().find("input[id$=fld_EXPENSECATEGORY]").val(objs[0].PLANNINGDESCRIPTION);
                                    $(_this).parent().parent().prev().prev().find("input[id$=fld_MERCHANDISECATEGORY]").val(objs[0].Description);
                                    $(_this).parent().parent().prev().find("input[id$=fld_MERCHANDISECATEGORYCODE]").val(objs[0].Code);
                                }
                                else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
                                    $(_this).parent().parent().next().find("input[id$=fld_EXPENSECATEGORY]").val(objs[0].PLANNINGDESCRIPTION);
                                }
                            }
                        }
                    });
                }
                else {
                    if (vLanague == "zh-CN") {
                        alert("第" + i + "行WBS元素填写错误");
                    }
                    else {
                        alert("The WBS element in line " + i + " is filled in incorrectly");
                    }
                    $(this).val("");
                }
                i++;
            }
        });
    }
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
        projectcostcenterrow(1);
    }
}

//税率验证
function taxValidation() {
    var vLanague = $("#div_lang").attr("data-lang");
    var i = 1;
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        var _this = this;
        if ($(_this).find("input[id$=fld_VATRATE_NAME]").val() != "") {
            $(_this).find("select[id$=fld_VATRATE]").val("");
            var name = $(_this).find("input[id$=fld_VATRATE_NAME]").val();
            var temp = 0;
            $(_this).find("select[id$=fld_VATRATE]").find("option").each(function () {
                if ($(this).text() == name) {
                    $(this).prop("selected", "selected");
                    temp = 1;
                    return false;
                }
            });
            if (temp == 0) {
                if (vLanague == "zh-CN") {
                    alert("第" + i + "行税率填写错误");
                }
                else {
                    alert("The Tax Rate in line " + i + " is filled in incorrectly");
                }
                $(_this).find("input[id$=fld_VATRATE_NAME]").val("");
                $(_this).find("input[id$=fld_VATRATE_CODE]").val("");
                $(_this).find("select[id$=fld_VATRATE]").val("");
            }
        }
        i++;
    });
}

//日期验证
function dateValidation() {
    var vLanague = $("#div_lang").attr("data-lang");
    var i = 1;
    $("#tb_PURCHASEORDER_DT tbody tr").not(":last").each(function () {
        var date1 = $(this).find("input[id$=fld_DATEFROM]").val();
        var date2 = $(this).find("input[id$=fld_DATETO]").val();
        var year = new Date().getFullYear();
        var month = new Date().getMonth() + 1;
        var day = new Date().getDate();
        var datenow = year + "/" + month + "/" + day;
        var date1ary = date1.split("/");
        var date2ary = date2.split("/");
        var temp = true;

        if ((isNaN(Date.parse(date1)) && date1 != "") || (isNaN(Date.parse(date2)) && date2 != "")) {
            $(this).find("input[id$=fld_DATEFROM]").val("");
            $(this).find("input[id$=fld_DATETO]").val("");
            if (vLanague == "zh-CN") {
                alert("第" + i + "行日期填写错误");
            }
            else {
                alert("The Date in line " + i + " is filled in incorrectly");
            }
            temp = false;
        }
        //开始日期小于当前日期
        //if (date1 != "" && temp) {
        //    if (Date.parse(datenow) > Date.parse(date1)) {
        //        $(this).find("input[id$=fld_DATEFROM]").val("");
        //        $(this).find("input[id$=fld_DATETO]").val("");
        //        if (vLanague == "zh-CN") {
        //            alert("第" + i + "行日期填写错误");
        //        }
        //        else {
        //            alert("The Date in line " + i + " is filled in incorrectly");
        //        }
        //        temp = false;
        //    }
        //}
        //if (date2 != "" && temp) {
        //    if (Date.parse(datenow) > Date.parse(date2)) {
        //        $(this).find("input[id$=fld_DATEFROM]").val("");
        //        $(this).find("input[id$=fld_DATETO]").val("");
        //        if (vLanague == "zh-CN") {
        //            alert("第" + i + "行日期填写错误");
        //        }
        //        else {
        //            alert("The Date in line " + i + " is filled in incorrectly");
        //        }
        //        temp = false;
        //    }
        //}
        if (date1 != "" && date2 != "" && temp) {
            if (date1ary[1] != date2ary[1] || ((date1ary[0] != date2ary[0]) && date1ary[1] == date2ary[1])) {
                if (vLanague == "zh-CN") {
                    alert("第" + i + "行日期填写错误");
                }
                else {
                    alert("The Date in line " + i + " is filled in incorrectly");
                }
                $(this).find("input[id$=fld_DATEFROM]").val("");
                $(this).find("input[id$=fld_DATETO]").val("");
            }
            else {
                if (Date.parse(date1) > Date.parse(date2)) {
                    if (vLanague == "zh-CN") {
                        alert("第" + i + "行日期填写错误");
                    }
                    else {
                        alert("The Date in line " + i + " is filled in incorrectly");
                    }
                    $(this).find("input[id$=fld_DATEFROM]").val("");
                    $(this).find("input[id$=fld_DATETO]").val("");
                }
            }
        }
        i++;
    });
}

//明细行利润中心赋值
function projectprofitcenterrow() {
    var projectcode = $("#fld_PROJECTCODE").val();
    var method = "projectprofitcenterrow";
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var objs;
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, ProjectCode: projectcode },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    $("#fld_MAINPROFITCENTERNAME").val(objs[0].ln);
    $("#tb_PURCHASEORDER_DT tbody tr .td_PROFITCENTERNAME").each(function () {
        $(this).find("input[id$=fld_PROFITCENTERNAME]").val(objs[0].sn);
    });
    $("#tb_PURCHASEORDER_DT tbody tr .td_COSTCENTERNAME").each(function () {
        $(this).find("input[id$=fld_COSTCENTERNAME]").val("");
    });
}

//Marketing明细行成本中心赋值
function projectcostcenterrow(obj) {
    if (obj != "1") {
        var projectcode = $("#" + obj).val().split('-')[0];
        var method = "projectcostcenterrow";
        var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
        var objs;
        $.ajax({
            url: url,
            type: "POST",
            async: false,
            dataType: "json",
            data: { Method: method, ProjectCode: projectcode },
            success: function (data) {
                if (data != null && data != "") {
                    objs = eval(data);
                    //$("#fld_MAINCOSTCENTERNAME").val(objs[0].ln);
                    $("#" + obj.replace("fld_WBSELEMENT", "fld_COSTCENTERNAME")).val(objs[0].sn);
                    $("#" + obj.replace("fld_WBSELEMENT", "fld_PROFITCENTERNAME")).find("input[id$=fld_PROFITCENTERNAME]").val("");
                    //if ($("#fld_MAINCOSTCENTERNAME").val() == "") {
                    $("#fld_MAINCOSTCENTERNAME").val(objs[0].ln);
                    //}
                }
            }
        });
    }
    else {
        $("#tb_PURCHASEORDER_DT tbody tr .td_WBSELEMENT").find("input").each(function () {
            var projectcode = $(this).val().split('-')[0];
            var method = "projectcostcenterrow";
            var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
            var objs;
            var _this = this;
            $.ajax({
                url: url,
                type: "POST",
                async: false,
                dataType: "json",
                data: { Method: method, ProjectCode: projectcode },
                success: function (data) {
                    if (data != null && data != "") {
                        objs = eval(data);
                    }
                    //$("#fld_MAINCOSTCENTERNAME").val(objs[0].ln);
                    $("#" + _this.id.replace("fld_WBSELEMENT", "fld_COSTCENTERNAME")).val(objs[0].sn);
                    $("#" + _this.id.replace("fld_WBSELEMENT", "fld_PROFITCENTERNAME")).find("input[id$=fld_PROFITCENTERNAME]").val("");
                    if ($("#fld_MAINCOSTCENTERNAME").val() == "") {
                        $("#fld_MAINCOSTCENTERNAME").val(objs[0].ln);
                    }
                }
            });
        });
    }
}

//品牌部门下拉框绑定
function setddlLinkbranddivsion() {
    var brand = $("#fld_BRANDDIVISION").val();
    var brandname = $("#fld_BRANDDIVISION_NAME").val();
    if (BdorDhJudge() == "DH") {
        var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
        var method = "getbranddivsion";
        $.ajax({
            url: url,
            type: "POST",
            async: false,
            dataType: "json",
            data: { Method: method, PurchaseType: $("input[name$=fld_PROCURMENTTYPE]:checked").val() },
            success: function (data) {
                if (data != null && data != "") {
                    $("#fld_BRANDDIVISION").empty();
                    $("#fld_BRANDDIVISION").append("<option value=''></option>");
                    var objs = eval(data);
                    for (var i = 0; i < objs.length; i++) {
                        $("#fld_BRANDDIVISION").append("<option value='" + objs[i].DEPARTMENTID + "'>" + objs[i].DEPARTMENTNAME + "</option>");
                    }
                }
            }
        });

        //品牌默认值设置
        if (brand == "" || brand == null) {
            for (var i = 0; i < $("#fld_BRANDDIVISION option").length; i++) {
                if ($("#fld_BRANDDIVISION option").eq(i).text() == $("#UserInfo1_fld_DEPARTMENT").val()) {
                    $("#fld_BRANDDIVISION option").eq(i).attr("selected", true);
                    break;
                }
            }
            $("#fld_BRANDDIVISION_NAME").val($("#UserInfo1_fld_DEPARTMENT").val());
        }
        else {
            $("#fld_BRANDDIVISION").val(brand);
            $("#fld_BRANDDIVISION_NAME").val(brandname);
        }
    }
    else {
        $("#fld_BRANDDIVISION").attr("disabled", "disabled");
        if (brand == "" || brand == null) {
            for (var i = 0; i < $("#fld_BRANDDIVISION option").length; i++) {
                if ($("#fld_BRANDDIVISION option").eq(i).text() == $("#UserInfo1_fld_DEPARTMENT").val()) {
                    $("#fld_BRANDDIVISION option").eq(i).attr("selected", true);
                    break;
                }
            }
            $("#fld_BRANDDIVISION_NAME").val($("#UserInfo1_fld_DEPARTMENT").val());
        }
    }
}

//marketing/cer 切换品牌清空明细行操作
function marketingbrandclear() {
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "Marketing Expense") {
        $("#fld_MAINCOSTCENTERNAME").val("");
        $("#tb_PURCHASEORDER_DT tbody tr").not(":eq(0),:last").remove();
        $("#tb_PURCHASEORDER_DT tbody tr").eq(0).find("td").not(":eq(0),:eq(1)").each(function () {
            $(this).find("input").val("");
            $(this).find("select").find("option").eq(0).prop("selected", "selected");
        });
        $("#fld_PROJECTBUDGETBALANCEAmount").val("");
    }
    else if ($("input[name=fld_PROCURMENTTYPE]:checked").val() == "CER") {
        $("#fld_PROJECTCODE").val("");
        $("#fld_PROJECTNAME").val("");
        $("#fld_MAINPROFITCENTERNAME").val("");
        $("#fld_MAINCOSTCENTERNAME").val("");
        $("#tb_PURCHASEORDER_DT tbody tr").not(":eq(0),:last").remove();
        $("#tb_PURCHASEORDER_DT tbody tr").eq(0).find("td").not(":eq(0),:eq(1)").each(function () {
            $(this).find("input").val("");
            $(this).find("select").find("option").eq(0).prop("selected", "selected");
        });
        $("#fld_PROJECTBUDGETBALANCEAmount").val("");
    }
}

//预算余额汇总
function budgetbalancetotal() {
    if ($("input[name=fld_PROCURMENTTYPE]:checked").val() != "General Procurement") {
        var total = 0;
        $("#tb_PURCHASEORDER_DT tbody tr .td_PROJECTBUDGETBALANCE").each(function () {
            if ($(this).find("input").val() != "")
                total += parseFloat($(this).find("input").val());
        });
        if (request("Type") == "MYREQUEST" || request("Type") == "MYAPPROVAL" || request("Type") == "report") {
            $("#fld_PROJECTBUDGETBALANCEAmount").text(GetMoney(total));
        }
        else {
            $("#fld_PROJECTBUDGETBALANCEAmount").val(GetMoney(total));
        }
    }
}

//判断猎头费
function headHuntingJundge() {
    var n = 0;
    $("#tb_PURCHASEORDER_DT tbody .td_MERCHANDISECATEGORYCODE").each(function () {
        if ($(this).find("input").val() == "GH002") { //|| $(this).find("input").val() == "GH009"
            n = "IsHeadHunting";
            return false;
        }
    });

    $("#fld_MARKETINGCATEGORYJUDGE").val(n);
}

//预付款下拉控制
function downPaymentOnchange(obj) {
    if (obj.value == "Down Payment") {
        $("#" + obj.id.replace("fld_CATEGORYDT", "fld_PERCENTAGEDT")).attr("Class", "item-control validate[required,custom[number]] border-left-color ");
    }
    else {
        $("#" + obj.id.replace("fld_CATEGORYDT", "fld_PERCENTAGEDT")).attr("Class", "item-control validate[custom[number]] ");
    }
}

//sispo提交前控制
function sisPoSubmitControl() {
    var projectcode = $("#fld_PROJECTCODE").val();
    var vendorno = $("#fld_VENDORNO").val();
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "sisPoSubmitControl";
    var objs = 1;
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, Projectcode: projectcode, Vendorno: vendorno },
        success: function (data) {
            if (data != null && data != "") {
                objs = 0;
            }
        }
    });
    return objs;
}

//品牌部门判断
function BdorDhJudge() {
    var objs;
    var url = "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx";
    var method = "BdorDhJudge";
    $.ajax({
        url: url,
        type: "POST",
        async: false,
        dataType: "json",
        data: { Method: method, DEPARTMENTID: $("#UserInfo1_fld_DEPARTMENTID").val() },
        success: function (data) {
            if (data != null && data != "") {
                objs = eval(data);
            }
        }
    });
    return objs[0].DEPARTMENTTYPE;
}

//明细行成本中心验证
function costmaincostrow(obj) {
    if ($("#fld_MAINCOSTCENTERNAME").val() == "") {
        var vLanague = $("#div_lang").attr("data-lang");
        if (vLanague == "zh-CN") {
            alert("请先选择主要成本中心！");
        }
        else {
            alert("Please select the main cost center first!");
        }
        obj.value = "";
    }
}

function loadCommonData() {
    $.ajax({
        url: "/Solution/UWF.Process.PurchaseOrder/Ajax/PurchaseOrderHandler.ashx",
        type: "POST",
        async: true,
        dataType: "json",
        data: {
            Method: "GetMerchandiseCategory", col: "ext05", val: "-1"
        },
        success: function (data) {
            $(data).each(function (index, item) {
                MCategoryDis.push(item.Code);
            })
        }
    });
}
//Begin
//=============================================================Begin OnChange事件=============================================================//


//=============================================================End OnChange事件=============================================================//
//End