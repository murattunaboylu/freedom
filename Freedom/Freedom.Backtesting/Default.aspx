<%@ Page Title="Backtesting" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Freedom.Backtesting._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <p class="lead" style="margin-top: 5px">BTC/EUR CEX.IO</p>
    <p class="lead">
        <div>
            <div style="display: inline-block; position: relative">
                From <input id="Start" type="text" value="20170701">                    
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                To <input id="End" type="text" value="20170725">                    
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                <select id="Interval">
                    <option>30</option>
                    <option>60</option>
                    <option selected>120</option>
                    <option>240</option>
                    <option>480</option>
                    <option>1440</option>
                </select>
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                <input type="button" ID="Simulate" Value="Sim" onclick="simulate();" />
            </div>
            <div style="display: inline-block; position: relative; margin-left: 10px">
                <input type="text" ID="StrategyParameters" value="rsiPeriod=2&rsiThreshold=5&stopLossRatio=0.02" style="width:500px;max-width:500px" />
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
