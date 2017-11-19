<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Freedom.Backtesting._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>Backtesting</h1>
        <p class="lead">BTC/EUR CEX.IO</p>
        <p><asp:PlaceHolder runat="server" ID="ChartPlaceHolder"></asp:PlaceHolder></p>
     </div>

    <div class="row">
        <div class="col-md-4">
            <h2>Orders</h2>
            <p>
                <asp:ListBox ID="OrdersListBox" runat="server"></asp:ListBox>
            </p>
        </div>
        <div class="col-md-4">
            <h2>PnL</h2>
            <p>
                <asp:Label ID="PnLLabel" runat="server" Text="Label"></asp:Label>
            </p>
        </div>
        <div class="col-md-4">
            <h2>Web Hosting</h2>
            <p>
                You can easily find a web hosting company that offers the right mix of features and price for your applications.
            </p>
            <p>
                <a class="btn btn-default" href="http://go.microsoft.com/fwlink/?LinkId=301950">Learn more &raquo;</a>
            </p>
        </div>
    </div>

</asp:Content>
