﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LimsProject.BusinessLayer;
using LimsProject.BusinessLayer.Modules;

namespace LimsProject
{
    public delegate void signCalibs(CSolution_interm e);
    public delegate void newCalib(CSolution_interm e);

    public partial class UcCalibStd : UserControl
    {
        #region Attributes

        ModCalibStd oModCalibStd = new ModCalibStd();
        BindingList<CCustomCalibStd> LstAllCalibs = new BindingList<CCustomCalibStd>();
        List<ChartPoint> lstChartPoint_serie1 = new List<ChartPoint>();
        List<ChartPoint> lstChartPoint_serie2 = new List<ChartPoint>();
        Dictionary<int, CCalib_std> DicCurve = new Dictionary<int, CCalib_std>();

        public int Idtemplate_method
        {
            get {
                return idtemplate_method;
            }
            set {
                idtemplate_method = value;
            }
        }

        int idtemplate_method = 0;
        int idelement = 0;
        short idmr_detail = 0;
        int? idreactive_medium = 0;
        int? idreactive_modif = 0;        

        public DateTime Date_init { get; set; }
        public DateTime Date_end { get; set; }
        public string UserSign { get; set; }
        public DateTime DateSign { get; set; }

        CSolution_interm solution_interm = new CSolution_interm();

        public event signCalibs onSignCalibs;
        public event newCalib onNewCalib;

        #endregion

        #region properties

        public List<CCustomCalibStd> LstCalibrations
        {
            get
            {
                return new List<CCustomCalibStd>(LstAllCalibs.Where(c => c.Idtemplate_method == idtemplate_method).ToList());
            }
        }

        public CSolution_interm Solution_interm
        {
            get
            {
                return solution_interm;
            }
            set
            {
                solution_interm = value;                
            }
        }

        #endregion

        #region constructores

        #endregion

        #region constructor

        public UcCalibStd()
        {
            InitializeComponent();
        }        

        #endregion

        #region methods

        public void Init(int idsolution_interm, CTemplate_method_aa template_method_aa)
        {
            Idtemplate_method = template_method_aa.Idtemplate_method;
            rgSelectGraphic.SelectedIndex = 1;

            CReactiveFactory faReactive = new CReactiveFactory();
            List<CReactive> lstReactive = faReactive.GetAll();

            lstReactive.Add(new CReactive() { Idreactive = 0, Reactive_name = "---", Chemical_formula = "---" });            

            cbMedium.Properties.DataSource = lstReactive;
            cbMedium.Properties.ValueMember = "Idreactive";
            cbMedium.Properties.DisplayMember = "Reactive_name";

            cbModif.Properties.DataSource = lstReactive;
            cbModif.Properties.ValueMember = "Idreactive";
            cbModif.Properties.DisplayMember = "Reactive_name";

            LstAllCalibs = new BindingList<CCustomCalibStd>(oModCalibStd.GetCustomCalibStd(idsolution_interm, template_method_aa));

            CSet_calibs oSet_calibs = new CSet_calibsFactory()
                .GetAll()
                .Where(x => x.Idsolution_interm == idsolution_interm 
                    && x.Idtemplate_method == template_method_aa.Idtemplate_method
                    && x.Last_calib == true)
                .FirstOrDefault();

            if (oSet_calibs != null)
            {
                ucTraceUser1.UserSign = oSet_calibs.Usersign;
                ucTraceUser1.DateSign = Convert.ToDateTime(oSet_calibs.Datesign);
            }

            gcCalibStd.DataSource = LstAllCalibs;

            PlotChart();
        }

        void InitXYDiagram()
        {
            chartCalib.Series[0].Points.Clear();
            chartCalib.Series[1].Points.Clear();

            chartCalib.Series[0].LegendText = "---";
            chartCalib.Series[1].LegendText = "---";

            string serie = "{0}, {1}";

            RegressionLineal oRL_serie1 = new RegressionLineal();
            RegressionLineal oRL_serie2 = new RegressionLineal();
            
            foreach (ChartPoint item in lstChartPoint_serie1)
            {
                DevExpress.XtraCharts.SeriesPoint punto = new DevExpress.XtraCharts.SeriesPoint();
                punto.Argument = item.X.ToString();
                punto.Values = new double[] { Convert.ToDouble(item.Y) };

                oRL_serie1.AddPoint(Convert.ToDouble(item.X), Convert.ToDouble(item.Y));

                chartCalib.Series[0].Points.Add(punto);

                chartCalib.Series[0].LegendText = string.Format(serie, oRL_serie1.getEcuation(), oRL_serie1.getR());
                
            }

            foreach (ChartPoint item in lstChartPoint_serie2)
            {
                DevExpress.XtraCharts.SeriesPoint punto = new DevExpress.XtraCharts.SeriesPoint();
                punto.Argument = item.X.ToString();
                punto.Values = new double[] { Convert.ToDouble(item.Y) };

                oRL_serie2.AddPoint(Convert.ToDouble(item.X), Convert.ToDouble(item.Y));

                chartCalib.Series[1].Points.Add(punto);

                chartCalib.Series[1].LegendText = string.Format(serie, oRL_serie2.getEcuation(), oRL_serie2.getR());                
            }

            try
            {                
                if (!double.IsNaN(oRL_serie2.r()))
                    tbRexp2.BackColor = oRL_serie2.r() > Convert.ToDouble(tbRexp2.Value) ? Color.White : Color.Red;

                if (!double.IsNaN(oRL_serie1.r()))
                    tbRexp2.BackColor = oRL_serie1.r() > Convert.ToDouble(tbRexp2.Value) ? Color.White : Color.Red;
            }
            catch
            {
            }
            
        }

        void PlotChart()
        {
            lstChartPoint_serie1.Clear();
            lstChartPoint_serie2.Clear();

            // --- recuperar metodo actual
            if (gvCalibStd.GetFocusedRowCellValue(gcCal_Idtemplate_method) != null)
            {
                CTemplate_method_aa currentTemplate_method_aa =
                    new CTemplate_method_aaFactory()
                    .GetByPrimaryKey(new CTemplate_method_aaKeys(
                        Convert.ToInt32(gvCalibStd.GetFocusedRowCellValue(gcCal_Idtemplate_method))));

                tbValidity_calib.Value = Convert.ToInt32(currentTemplate_method_aa.Validity_calib);
                tbRexp2.Value = Convert.ToDecimal(currentTemplate_method_aa.Rexp2);
                cbMedium.EditValue = currentTemplate_method_aa.Medium;
                tbMediumConc.Value = Convert.ToDecimal(currentTemplate_method_aa.Medium_conc);
                cbModif.EditValue = currentTemplate_method_aa.Modif;
                tbModifConc.Value = Convert.ToDecimal(currentTemplate_method_aa.Modif_conc);

                // --- dibujar curva
                if (rgSelectGraphic.SelectedIndex == 0
                    || rgSelectGraphic.SelectedIndex == 2)
                {
                    lstChartPoint_serie1 =
                        (from t in LstAllCalibs
                         where t.Idtemplate_method == currentTemplate_method_aa.Idtemplate_method
                         select new ChartPoint
                         {
                             X = Convert.ToDecimal(t.Concentration),
                             Y = Convert.ToDecimal(t.Absorbance_previus)
                         }).ToList();
                }

                // --- dibujar curva
                if (rgSelectGraphic.SelectedIndex == 1
                    || rgSelectGraphic.SelectedIndex == 2)
                {
                    lstChartPoint_serie2 =
                        (from t in LstAllCalibs
                         where t.Idtemplate_method == currentTemplate_method_aa.Idtemplate_method
                         select new ChartPoint
                         {
                             X = Convert.ToDecimal(t.Concentration),
                             Y = Convert.ToDecimal(t.Absorbance)
                         }).ToList();
                }

                InitXYDiagram();
            }
        }

        void GrabarStdFocused(int rowindex)
        {
            
        }

        public void Clear()
        {
            gcCalibStd.DataSource = null;
            chartCalib.Series[0].Points.Clear();
            chartCalib.Series[1].Points.Clear();
            tbValidity_calib.Value = 0;
            cbMedium.EditValue = null;
            cbModif.EditValue = null;
            tbRexp2.Value = 0;
            tbMediumConc.Value = 0;
            tbModifConc.Value = 0;
        }

        #endregion

        #region events

        private void gvCalibStd_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            PlotChart();
        }

        private void gvCalibStd_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {            
            if (e.Column == gcCal_Absorbance)
            {                
                // --- mostrar grafica
                PlotChart();
            }
        }

        private void rgSelectGraphic_SelectedIndexChanged(object sender, EventArgs e)
        {
            PlotChart();
        }

        public bool Son_datos_correctos()        
        {
            for (int i = 0; i < gvCalibStd.RowCount; i++)
            {                
                if (Convert.ToDecimal(gvCalibStd.GetRowCellValue(i, gcCal_Absorbance)) == 0)
                {
                    ComunForm.Send_message("Módulo de Soluciones", TypeMsg.error, "No se ingreso el estandar de calibración.");
                    return false;
                }
            }
            if (tbRexp2.BackColor == Color.Red)
            {
                ComunForm.Send_message("Módulo de Soluciones", TypeMsg.error, "Error en correlación.");
                return false;
            }
            return true;
        }

        public void SaveCalibs(CSolution_interm pSolution_interm)
        {
            solution_interm = pSolution_interm;

            Date_init = Convert.ToDateTime(solution_interm.Date_begin);
            Date_end = Convert.ToDateTime(solution_interm.Date_end);

            UserSign = Comun.GetUser();
            DateSign = Comun.GetDate();
            
            //poner como historico la actual grupo de calibraciones            
            CSet_calibs oSet_calib = oModCalibStd.GetSet_CalibBy(solution_interm.Idsolution_interm, idtemplate_method);

            if (oSet_calib != null)
            {
                oSet_calib.Last_calib = false;
                new CSet_calibsFactory().Update(oSet_calib);
            }

            // --- guardar la nueva calibración
            oSet_calib = new CSet_calibs();
            oSet_calib.Idset_calibs = 0;
            oSet_calib.Idtemplate_method = idtemplate_method;            
            oSet_calib.Idsolution_interm = solution_interm.Idsolution_interm;
            oSet_calib.Datesign = Comun.GetDate();
            oSet_calib.Usersign = Comun.GetUser();
            oSet_calib.Last_calib = true;
                        
            bool result_set_calib = new CSet_calibsFactory().Insert(oSet_calib);

            // --- guardar estandares de calibración
            if (result_set_calib)
            {
                for (int i = 0; i < gvCalibStd.RowCount; i++)
                {
                    CSet_calibsFactory faSet_calibs = new CSet_calibsFactory();

                    CCustomCalibStd customCalibStd = gvCalibStd.GetRow(i) as CCustomCalibStd;

                    // --- guardar calibración
                    CCalib_stdFactory faCalib_std = new CCalib_stdFactory();
                    CCalib_std oCalib_std = new CCalib_std();

                    oCalib_std = new CCalib_std();
                    oCalib_std.Idcalib = customCalibStd.Idcalib;
                    oCalib_std.Absorbance = customCalibStd.Absorbance;
                    oCalib_std.Concentration = customCalibStd.Concentration;
                    oCalib_std.Idset_calibs = oSet_calib.Idset_calibs;
                    oCalib_std.Idsolution_interm = solution_interm.Idsolution_interm;
                    oCalib_std.Idtemplate_method = idtemplate_method;

                    bool result_std = false;
                    if (!(result_std = faCalib_std.Update(oCalib_std)))
                        result_std = faCalib_std.Insert(oCalib_std);

                    if (result_std)
                        gvCalibStd.SetRowCellValue(i, gcCal_Idcalib_std, oCalib_std.Idcalib_std);                    
                }
            }
        }

        #endregion        

    }

    #region Clases

    public class ChartPoint
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
    }

    public class CCurve
    {
        public decimal a { get; set; }
        public decimal b { get; set; }
        public decimal R { get; set; }
    }

    #endregion

    #region Regresion lineal

    public class RegressionLineal
    {
        public List<double> LstX = new List<double>();
        public List<double> LstY = new List<double>();

        public void AddPoint(double x, double y)
        {
            LstX.Add(x);
            LstY.Add(y);
        }

        double SumX()
        {
            double sumx = 0;
            foreach (double item in LstX)
            {
                sumx = sumx + item;
            }
            return sumx;
        }

        double SumY()
        {
            double sumy = 0;
            foreach (double item in LstY)
            {
                sumy = sumy + item;
            }
            return sumy;
        }

        double SumXY()
        {
            double sumxy = 0;
            for (int i = 0; i < LstX.Count; i++)
            {
                sumxy = sumxy + LstX[i] * LstY[i];
            }
            return sumxy;
        }

        double SumXX()
        {
            double sumxx = 0;
            foreach (double item in LstX)
            {
                sumxx = sumxx + item * item;
            }
            return sumxx;
        }

        double SumYY()
        {
            double sumyy = 0;
            foreach (double item in LstY)
            {
                sumyy = sumyy + item * item;
            }
            return sumyy;
        }

        public double a()
        {
            int n = LstX.Count;
            return (n * SumXY() - SumX() * SumY()) / (n * SumXX() - Math.Pow(SumX(), 2));
        }

        public double b()
        {
            int n = LstX.Count;
            return (SumY() * SumXX() - SumX() * SumXY()) / (n * SumXX() - Math.Pow(SumX(), 2));
        }

        public double r()
        {
            int n = LstX.Count;
            return (n * SumXY() - SumX() * SumY()) / Math.Sqrt((n * SumXX() - Math.Pow(SumX(), 2)) * (n * SumYY() - Math.Pow(SumY(), 2)));
        }

        public string getEcuation()
        {
            string ecuation = "y = {0}x + {1}";
            return string.Format(ecuation, Math.Round(a(), 4), Math.Round(b(), 4));
        }

        public string getR()
        {
            string strr = "r = {0}";
            return string.Format(strr, Math.Round(r(), 4));
        }
    }

    #endregion 
}
