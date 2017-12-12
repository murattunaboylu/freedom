<%@ Page Title="Backtesting" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Freedom.Backtesting._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <p class="lead" style="margin-top: 5px">BTC/EUR CEX.IO</p>
    <p class="lead">
        <div>
            <div style="display: inline-block; position: relative">
                From
                    <asp:TextBox ID="StartDateTextBox" runat="server"></asp:TextBox>
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                To
                    <asp:TextBox ID="EndDateTextBox" runat="server"></asp:TextBox>
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                <asp:DropDownList ID="IntervalDropDownList" runat="server">
                    <asp:ListItem>5</asp:ListItem>
                    <asp:ListItem>10</asp:ListItem>
                    <asp:ListItem>30</asp:ListItem>
                    <asp:ListItem>60</asp:ListItem>
                    <asp:ListItem>120</asp:ListItem>
                    <asp:ListItem>240</asp:ListItem>
                    <asp:ListItem>480</asp:ListItem>
                    <asp:ListItem>1440</asp:ListItem>
                </asp:DropDownList>
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                <asp:Button ID="SimulateButton" runat="server" Text="Sim" />
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                <asp:TextBox ID="StrategyParametersTextBox" runat="server"></asp:TextBox>
            </div>

        </div>
    </p>

    <div id="chartdiv" style="width: 100%; height: 600px;"></div>
    <p>
        <asp:PlaceHolder runat="server" ID="ChartPlaceHolder"></asp:PlaceHolder>
    </p>


    <div class="row">
        <div class="col-md-4">
            <h3>Orders</h3>
            <div id="Orders"></div>
        </div>
        <div class="col-md-4">
            <h3>PnL</h3>
            <div id="PnL"></div>
            <div id="Stats"></div>
        </div>
        <div class="col-md-4">
        </div>
    </div>

</asp:Content>
