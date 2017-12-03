<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Freedom.Backtesting._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h2>Backtesting</h2>
        <p class="lead">BTC/EUR CEX.IO</p>
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
                    <asp:Button ID="SimulateButton" runat="server" OnClick="SimulateButton_Click" Text="Sim" />
                </div>
            </div>
        </p>
        <p>
            <asp:PlaceHolder runat="server" ID="ChartPlaceHolder"></asp:PlaceHolder>
        </p>
    </div>

    <div class="row">
        <div class="col-md-4">
            <h3>Orders</h3>
            <p>
                <asp:ListBox ID="OrdersListBox" runat="server" OnSelectedIndexChanged="OrdersListBox_SelectedIndexChanged" SelectionMode="Multiple"></asp:ListBox>
            </p>
        </div>
        <div class="col-md-4">
            <h3>PnL</h3>
            <p>
                <asp:Label ID="PnLLabel" runat="server" Text="Label"></asp:Label>
            </p>
            <p>
                <asp:PlaceHolder ID="StatsPlaceHolder" runat="server"></asp:PlaceHolder>
            </p>
        </div>
        <div class="col-md-4">
        </div>
    </div>

</asp:Content>
