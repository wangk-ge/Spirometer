﻿using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spirometer
{
    public partial class Form1 : Form
    {
        private PlotModel m_plotModelCV; // 容积-流量图Model
        private PlotModel m_plotModelCT; // 容积-时间图Model
        private PlotModel m_plotModelVT; // 流量-时间图Model

        public Form1()
        {
            InitializeComponent();
        }

        /* 枚举可用串口并更新列表控件 */
        private void EnumSerialPorts()
        {
            toolStripComboBoxCom.Items.Clear();

            SerialPort _tempPort;
            String[] Portname = SerialPort.GetPortNames();

            //create a loop for each string in SerialPort.GetPortNames
            foreach (string str in Portname)
            {
                try
                {
                    _tempPort = new SerialPort(str);
                    _tempPort.Open();

                    //if the port exist and we can open it
                    if (_tempPort.IsOpen)
                    {
                        toolStripComboBoxCom.Items.Add(str);
                        _tempPort.Close();
                    }
                }
                //else we have no ports or can't open them display the 
                //precise error of why we either don't have ports or can't open them
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString(), "Error - No Ports available", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine(ex);
                }
            }

            toolStripComboBoxCom.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /* 枚举可用串口并更新列表控件 */
            EnumSerialPorts();

            /* 容积-流量图 */
            m_plotModelCV = new PlotModel()
            {
                Title = "容积-流量图",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black
            };

            //X轴
            var xAxisCV = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 1000
            };
            m_plotModelCV.Axes.Add(xAxisCV);

            //Y轴
            var yAxisCV = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left
            };
            m_plotModelCV.Axes.Add(yAxisCV);

            // 原始数据
            var seriesCV = new LineSeries()
            {
                Color = OxyColors.Green,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelCV.Series.Add(seriesCV);

            plotViewCV.Model = m_plotModelCV;

            /* 容积-时间图 */
            m_plotModelCT = new PlotModel()
            {
                Title = "容积-时间图",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black
            };

            //X轴
            var xAxisCT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 1000
            };
            m_plotModelCT.Axes.Add(xAxisCT);

            //Y轴
            var yAxisCT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left
            };
            m_plotModelCT.Axes.Add(yAxisCT);

            // 原始数据
            var seriesCT = new LineSeries()
            {
                Color = OxyColors.Green,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelCT.Series.Add(seriesCT);

            plotViewCT.Model = m_plotModelCT;

            /* 流量-时间图 */
            m_plotModelVT = new PlotModel()
            {
                Title = "流量-时间图",
                LegendTitle = "图例",
                LegendOrientation = LegendOrientation.Horizontal,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.Beige,
                LegendBorder = OxyColors.Black
            };

            //X轴
            var xAxisVT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Bottom,
                Minimum = 0,
                Maximum = 1000
            };
            m_plotModelVT.Axes.Add(xAxisVT);

            //Y轴
            var yAxisVT = new LinearAxis()
            {
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                IsZoomEnabled = true,
                IsPanEnabled = true,
                Position = AxisPosition.Left
            };
            m_plotModelVT.Axes.Add(yAxisVT);

            // 原始数据
            var seriesVT = new LineSeries()
            {
                Color = OxyColors.Green,
                StrokeThickness = 1,
                MarkerSize = 1,
                MarkerStroke = OxyColors.DarkGreen,
                MarkerType = MarkerType.Circle,
                Title = "Data"
            };
            m_plotModelVT.Series.Add(seriesVT);

            plotViewVT.Model = m_plotModelVT;
        }

        private void toolStripButtonConnect_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButtonScan_Click(object sender, EventArgs e)
        {
            /* 枚举可用串口并更新列表控件 */
            EnumSerialPorts();
        }
    }
}
