<%@ Page Language="C#" AutoEventWireup="true" CodeFile="StationeryList.aspx.cs" Inherits="UPL.Common.MasterDataReport.StationeryList" %>
<%@ Import Namespace="Ultimus.UWF.Common.Logic" %>
<%@ Register Assembly="Ultimus.UWF.Form" Namespace="Ultimus.UWF.Form.WebControls" TagPrefix="ult" %>
<%@ Register Assembly="AspNetPager" Namespace="Wuqi.Webdiyer" TagPrefix="webdiyer" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="description" content="Ultimus BPM , Ultimus Business Process Management">
    <meta name="keywords" content="ultimus, bpm, workflow, business process management" />
    <title>文具列表</title>
    <link href="../../../common/assets/css/font-awesome.min.css" rel="stylesheet" />
    <link href="../../../common/assets/css/bootstrap3.3.2.css" rel="stylesheet" />
    <link href="../../../common/assets/css/shortcuts.css" rel="stylesheet" />
    <link href="../../../common/assets/css/report.css" rel="stylesheet" />
    <link href="../../../common/assets/css/root.css" type="text/css" rel="stylesheet" />
    <style type="text/css">
        #TABLE1 tr td {
            text-align: center;
            vertical-align: middle;
            /*word-wrap: break-word;
            word-break: break-all;*/
            word-break: keep-all; /* 不换行 */
            white-space: nowrap; /* 不换行 */
            overflow: hidden; /* 内容超出宽度时隐藏超出部分的内容 */
            text-overflow: ellipsis; /*省略号*/
        }

        #TABLE1 tr th {
            text-align: center;
            vertical-align: middle;
            word-break: keep-all;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server" formborderstyle="FixedSingle">
        <div class="form-content">
            <!-- Panel Header -->
            <div class="page-header">
                <h1 class="title ">
                    <span class="btn btn-rounded btn-default btn-icon cursor-default">
                        <i class="fa fa-bars"></i>
                    </span>
                    <%=Lang.Get("UWF.Process.StationeryList.StationeryList") %>
                </h1>
                <div class="right">
                    <div class="btn-group" role="group" aria-label="...">
                        <a href="javascript:location.href=location.href;" class="btn btn-light"><i class="fa fa-refresh"></i></a>
                        <a onclick="$('#searchPanel').toggle();" class="btn btn-light"><i class="fa fa-search"></i></a>
                        <a class="btn btn-light" data-toggle="modal" data-target=".bs-example-modal-lg"><i class="fa fa-folder-open"></i></a>
                        <a  id="btn_Import" class="btn btn-light btn-sm" data-toggle="modal" data-target="#myModal">
                            <li class="fa fa-file-excel-o"></li>
                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Import") %></a>
                        <a id="btn_Export" runat="server" class="btn btn-light btn-sm" onserverclick="btn_Export_Click">
                            <li class="fa fa-file-excel-o"></li>
                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Export") %>
                        </a>
                    </div>
                </div>
            </div>
            <!-- Panel Search -->
            <div class="container-default">
                <div class="row" id="searchPanel">
                    <div class="col-md-12">
                        <div class="panel panel-default">
                            <!-- Panel Search Title -->
                            <div class="panel-title">
                                <i class="fa fa-search"></i>
                                <%= Lang.Get("UWF.Process.ECatalogLanguageEdit.QueryConditions") %>
                                <ul class="panel-tools">
                                    <li><a class="icon minimise-tool"><i class="fa fa-minus"></i></a></li>
                                    <li><a class="icon expand-tool"><i class="fa fa-expand"></i></a></li>
                                </ul>
                            </div>

                            <div class="panel-body">
                                <div class="col-md-4 col-sm-12 col-xs-12">
                                    <div class="form-group">
                                        <div class="col-md-4">
                                            <%= Lang.Get("UWF.Process.ECatalogLanguageEdit.Description") %>
                                        </div>
                                        <div class="col-md-8">
                                            <asp:TextBox ID="txt_Search" runat="server" CssClass="form-control" Style="width: 100%"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-4 col-sm-12 col-xs-12">
                                    <div class="form-group">
                                        <div class="col-md-4">
                                            <%= Lang.Get("UWF.Process.ECatalogLanguageEdit.Vendor") %>
                                        </div>
                                        <div class="col-md-8">
                                            <asp:TextBox ID="txt_Vendor" runat="server" CssClass="form-control" Style="width: 100%"></asp:TextBox>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-4 col-sm-12 col-xs-12">
                                    <asp:Button ID="btn_Search" runat="server" Text="Search" CssClass="btn btn-toolbar" Style="" OnClick="btn_Search_Click" />
                                    <asp:Button ID="Button2" runat="server" Text="Create" CssClass="btn btn-default hidden "  OnClick="Button2_Click" Style="" />
                                </div>

                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <!-- Table -->
            <div class="row">
                <div class="col-md-12">
                    <div class="panel panel-default">
                        <div class="panel-title">
                            <i class="fa fa-bars"></i>
                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Information") %>
                            <ul class="panel-tools">
                                <li><a class="icon minimise-tool"><i class="fa fa-minus"></i></a></li>
                                <li><a class="icon expand-tool"><i class="fa fa-expand"></i></a></li>
                            </ul>
                        </div>
                        <div class="panel-body" >
                            <table class="table table-condensed table-bordered " id="TABLE1" style="table-layout: fixed;">
                                <thead>
                                    <tr>
                                        <th >NO
                                        </th>
                                        <th class="hidden"></th>
                                        <th >
                                            <%=Lang.Get("UWF.Process.StationeryList.StationeryType") %>
                                        </th>
                                        <th >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.ItemNumber") %>
                                        </th>
                                        <th >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Description") %>
                                        </th>
                                        <th >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Unit") %>
                                        </th>
                                        <th >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.UnitPrice") %>
                                        </th>
                                        <th >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Vendor") %> CODE
                                        </th>
                                        <th  >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Vendor") %>
                                        </th>
                                        <th >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.Status") %>
                                        </th>
                                        <th  >
                                            <%=Lang.Get("UWF.Process.ECatalogLanguageEdit.TheOperator") %>
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <ult:Repeater ID="Repeater1" runat="server" PagerID="AspNetPager1">
                                        <itemtemplate>
                                <tr style="cursor: pointer">
                                    <td>
                                        <%# Container.ItemIndex+1 %>
                                    </td>
                                    <td class="hidden"><%#Eval("ID") %></td>
                                    <td>
                                        <asp:Label ID="lbl_ITEMTYPE" runat="server" Text='<%#Eval("ITEMTYPE") %>'></asp:Label>
                                    </td>
                                    <td>
                                        <asp:Label ID="lbl_ITEMNO" runat="server" Text='<%#Eval("ITEMNO") %>'></asp:Label>
                                    </td>
                                    <td>
                                        <asp:Label ID="lbl_DESCRIPTION" runat="server" Text='<%#Eval("DESCRIPTION") %>'></asp:Label>
                                    </td>
                                    <td>
                                        <asp:Label ID="lbl_UNIT" runat="server" Text='<%#Eval("UNIT") %>'></asp:Label>
                                    </td>
                                    <td>
                                        <asp:Label ID="lbl_UNITPRICE" runat="server" Text='<%#Eval("UNITPRICE") %>'></asp:Label>
                                    </td>
                                    <td>
                                        <asp:Label ID="lbl_VENDORCODE" runat="server" Text='<%#Eval("VENDORCODE") %>'></asp:Label>
                                    </td>
                                    <td>
                                        <asp:Label ID="lbl_VENDORNAME" runat="server" Text='<%#Eval("VENDORNAME") %>'></asp:Label>
                                    </td>
                                    <td>
                                      <asp:Label ID="lbl_STATUS" runat="server"  Text='<%#Eval("ISACTIVE") %>'></asp:Label>
                                    </td>
                                    <td>
                                      <asp:Label ID="lbl_TheOperator" runat="server"  Text='<%#Eval("TheOperator") %>'></asp:Label>
                                    </td>
                                </tr>
                            </itemtemplate>
                                    </ult:Repeater>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
            <!-- Pager -->
            <div class="row">
                <div class="panel panel-default">
                    <div class="panel-title">
                        <div class="pull-right">
                            <webdiyer:AspNetPager ID="AspNetPager1" runat="server" CssClass="asppager"
                                NumericButtonCount="5" CurrentPageButtonClass="btn"
                                FirstPageText="<i class='fa fa-step-backward'></i>" PrevPageText="<i class='fa fa-chevron-left'></i>"
                                NextPageText="<i class='fa fa-chevron-right'></i>" LastPageText="<i class='fa fa-step-forward'></i>"
                                AlwaysShow="false" PageSize="5">
                            </webdiyer:AspNetPager>
                        </div>
                    </div>
                </div>
            </div>
        </div>


        <!-- Modal -->
        <div class="modal fade" id="myModal" tabindex="-1" role="dialog" aria-labelledby="myModalLabel">
            <div class="modal-dialog" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        <h4 class="modal-title" id="myModalLabel"><%=Lang.Get("UWF.Process.ECatalogLanguageEdit.ExcelImport") %></h4>
                    </div>
                    <div class="modal-body" style="width: 100%; height: 80px;">
                        <div class="file-loading">
                            <asp:FileUpload ID="fupExcel" runat="server" multiple />
                        </div>
                        <div id="kartik-file-errors"></div>

                    </div>
                    <div class="modal-footer">
                        <asp:LinkButton ID="LinkButton1" class="btn btn-default" runat="server" OnClick="LinkButton1_Click"><%=Lang.Get("UWF.Process.ECatalogLanguageEdit.DownloadTemplate") %></asp:LinkButton>
                        <asp:Button ID="btn_Save" runat="server" CssClass="btn btn-primary" Text="Save changes" OnClick="btn_Save_Click" />
                    </div>
                </div>
            </div>
        </div>

        <div class="modal fade bs-example-modal-lg" tabindex="-1" role="dialog" aria-labelledby="myLargeModalLabel">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header">
                        <%--<button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>--%>
                        <asp:Button ID="btn_ImgFile" runat="server" CssClass="btn btn-primary" Text="Save changes" OnClick="btn_ImgFile_Click" style="float:right" />
                        <h4 class="modal-title" id="H1">图片上传</h4>
                    </div>

                    <div class="modal-body" style="width: 100%; height: 400px;">
                        <div class="file-loading">
                            <asp:FileUpload ID="ImgFile" runat="server" multiple style="width: 100%" />
                            </div>
                        <div id="kartik-file-errors1"></div>
                    </div>
                    <div class="modal-footer">
                        
                    </div>
                </div>
            </div>
        </div>

    </form>
    <%=WebUtil.IncludeJsV3() %>
    <script type="text/javascript">
        $(document).ready(function () {
            $(".asppager a").addClass("btn");
            $("[name='ddl_CategoryI']").find("option:eq(0)").val("");
            $("[name='ddl_CategoryII']").find("option:eq(0)").val("");
            //初始化上传按钮
            fulImage();
            Edit();
            Status();
            Tableellipsis();
            datepicker();
            for (var i = 0; i < $("#TABLE1 tbody tr").length; i++) {
                if ($("#TABLE1 tbody tr:eq(" + i + ") td:eq(18)").text().trim() == "Enable") {
                    $("#TABLE1 tbody tr:eq(" + i + ") td a:eq(1)").text("Disable");
                }
                if ($("#TABLE1 tbody tr:eq(" + i + ") td:eq(18)").text().trim() == "Disable") {
                    $("#TABLE1 tbody tr:eq(" + i + ") td a:eq(1)").text("Enable");
                }
            }

        });



        function Tableellipsis() {
            var $th = $("thead").children("tr");
            var $thArr = $th.eq(0).find("th");//获取每一行中的td
            for (var j = 0; j < $thArr.length; j++) {
                var $th_text = $thArr.eq(j).text(); //获取td中的文本
                $thArr.eq(j).attr("title", $th_text); //将该值赋给title属性
            }

            var $trList = $("tbody").children("tr");//获取所有的tr行
            for (var i = 0; i < $trList.length; i++) {//遍历每一行
                var $tdArr = $trList.eq(i).find("td");//获取每一行中的td
                for (var j = 0; j < $tdArr.length; j++) {
                    //遍历td
                    if (j == $tdArr.length - 1) {
                        continue;
                    }
                    else {
                        var $td_text = $tdArr.eq(j).text(); //获取td中的文本
                        $tdArr.eq(j).attr("title", $td_text); //将该值赋给title属性
                    }
                }
            }
        }

        //初始化FileUpload
        function fulImage() {
            var lang = "";
            if ($("#div_lang").attr("data-lang") == "en-US") {
                lang = "";
            }
            if ($("#div_lang").attr("data-lang") == "zh-CN") {
                lang = "zh";
            }
            $("#fupExcel").fileinput({
                language: lang,//控件语言选择，此处为zh:汉化
                showPreview: false,//文件预览显示，默认为true:显示
                showUpload: false,//文件上传按钮显示，默认为true:显示
                elErrorContainer: '#kartik-file-errors',//错误提示
                allowedFileExtensions: ["xls", "xlsm", "xlsx", "xltx"],//允许选择文件格式
                browseClass: "btn btn-primary", //按钮样式
            });
            $("#ImgFile").fileinput({
                language: lang,//控件语言选择，此处为zh:汉化
                showPreview: true,//文件预览显示，默认为true:显示
                showUpload: false,//文件上传按钮显示，默认为true:显示
                elErrorContainer: '#kartik-file-errors1',//错误提示
                allowedFileExtensions: ["jpg", "bmp", "png", "tif", "pcx", "gif", "webp"],//允许选择文件格式
                browseClass: "btn btn-primary", //按钮样式
            });

        }

        function Edit() {
            $("#TABLE1 tbody tr td a[name=edit]").click(function () {
                window.open("/Solution/UWF.Process.PurchaseOrder/E-Catalog/CategoryAttributes.aspx?method=" + $(this).parent().parent().find("td:eq(4)").text());
            });
        }

        function Status() {
            $("#TABLE1 tbody tr td a[name=Status]").click(function () {
                var param = {};
                param.method = "ECatalogStatus";
                param.Code = $(this).parent().parent().find("td:eq(4)").text();
                param.Status = $(this).parent().parent().find("td:eq(18)").text();
                $.post("../Ajax/PurchaseOrderHandler.ashx", param, function (data) {
                    if (data == "Disablesuccess") {
                        alert("已禁用");
                    }
                    if (data == "Enablesuccess") {
                        alert("已启用");
                    }
                    window.location.reload();
                });
            });
        }

        //datepicker时间控件初始化
        function datepicker() {
            var lang = "";
            if ($("#div_lang").attr("data-lang") == "en-US") {
                lang = "en";
                //$("#ddl_CategoryI").text("Please");
                $("[name='ddl_CategoryI']").find("option:eq(0)").text("Please Select");
                $("[name='ddl_CategoryII']").find("option:eq(0)").text("Please Select");
                $("[name='ddl_CategoryIII']").find("option:eq(0)").text("Please Select");
                $("#btn_Save").val("Upload");
                $("#btn_ImgFile").val("Upload");
            }
            if ($("#div_lang").attr("data-lang") == "zh-CN") {
                lang = "zh-CN";
                $("[name='ddl_CategoryI']").find("option:eq(0)").text("请选择");
                $("[name='ddl_CategoryII']").find("option:eq(0)").text("请选择");
                $("[name='ddl_CategoryIII']").find("option:eq(0)").text("请选择");
                $("#btn_Save").val("上传");
                $("#btn_ImgFile").val("上传");
            }
            $("#txt_PeriodBegin").datetimepicker({
                minView: "month", //选择日期后，不会再跳转去选择时分秒 
                language: lang,
                format: 'yyyy/mm/dd',
                todayBtn: 1,
                autoclose: 1,
            });
            $("#txt_PeriodEnd").datetimepicker({
                minView: "month", //选择日期后，不会再跳转去选择时分秒 
                language: 'zh-CN',
                format: 'yyyy/mm/dd',
                todayBtn: 1,
                autoclose: 1,
            });

            $("#txt_PeriodBegin").attr("placeholder", "YYYY/MM/DD");
            $("#txt_PeriodBegin").attr("data-type", "date");
            $("#txt_PeriodBegin").attr("onkeydown", "return false");
            $("#txt_PeriodBegin").attr("onclick", "$(this).blur()");

            $("#txt_PeriodEnd").attr("placeholder", "YYYY/MM/DD");
            $("#txt_PeriodEnd").attr("data-type", "date");
            $("#txt_PeriodEnd").attr("onkeydown", "return false");
            $("#txt_PeriodEnd").attr("onclick", "$(this).blur()");
        }
    </script>
    <link href='<%=WebUtil.GetRootPath()%>/Solution/UPL.Common.BussinessControl/css/fileinput.css?t=f754874c-5205-4c93-8507-fa7a996574fc' rel="stylesheet" />
    <script type="text/javascript" src='<%=WebUtil.GetRootPath()%>/Solution/UPL.Common.BussinessControl/Script/fileinput.js?t=f754874c-5205-4c93-8507-fa7a996574fc'></script>
    <script type="text/javascript" src='<%=WebUtil.GetRootPath()%>/Solution/UPL.Common.BussinessControl/Script/locales/zh.js?t=f754874c-5205-4c93-8507-fa7a996574fc'></script>
    <script type='text/javascript' src='<%=WebUtil.GetRootPath()%>/Solution/UPL.Common.BussinessControl/Script/BussinessCommon.js?t=f754874c-5205-4c93-8507-fa7a996574fc'></script>
    <div id='div_lang' data-lang='<%=Lang.GetLang() %>'></div>
</body>
</html>
