using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<LineSeries> _lines = new List<LineSeries>();
        MainViewModel _model;
        int ThermalConductivity = 46;
        int HeatCapacity = 460;
        int Density = 7800;

        Func<double, double, double> initCond;

        public MainWindow()
        {
            InitializeComponent();
            _model = new MainViewModel();
            DataContext = _model;

            initCond = (x, p) => 500 + 250 * Math.Sin(Math.PI * x * p);


        }

        void Implicit(int N, double T, double alpha, double beta)
        {
            _lines.Clear();
            //init bound conditional coef
            double k = ThermalConductivity / (HeatCapacity * Density);
            // step by x 
            double h = 1.0 / N;
            //step by T
            double tau = h * h / 2;
            //layers
            int l = (int)(T / tau);
            double[][] impl = new double[l][]; 
            for(int i = 0; i < l; i++)
            {
                impl[i] = new double[N];
            }

            double timing = 0, t = 0;
            double[] u0 = new double[N], u = new double[N]; //[N]
            double a, b, c; //коэффициенты в системе 
            double[] Ai = new double[N], Bi = new double[N]; //[N] //массивы прогоночных коэффициентов 
            int z = 0; //счетчик слоя 

            //начальные условия 
            for (int i = 0; i < N; i++)
            {
                u0[i] = initCond(i, k);
                //u0[i] = sin(pi * k * i * h);
                // cout <<u0[i] << ",  "; 
            }

            //переопределяем значения на границах нулевого слоя 
            u0[0] = 0; // u=a*t 
            u0[N-1] = 0; // u=b*t 

            //коэффициенты системы 
            a = (-1d) * ((2d / (h * h) + 1d / tau));
            b = 1d / (h * h);
            c = (-1d) / tau;

            while (timing < T && z < l - 1 )
            {
                timing = timing + tau;
                z++;                //current layer



                //A1 и B1 
                Ai[0] = -b / a;
                Bi[0] = (c * u0[1] - b * alpha * timing) / a;

                //Ai и Bi 
                for (int i = 0; i < N - 1; i++)
                {
                    Ai[i + 1] = -b / (b * Ai[i] + a);
                    Bi[i + 1] = (c * u0[i + 1] - b * Bi[i]) / (b * Ai[i] + a);
                }

                //An-1 и Bn-1 
                Ai[N - 1] = 0;
                Bi[N - 1] = (c * u0[N - 1] - b * beta * timing) / (b * Ai[N - 2] + a);

                //Прогонка 
                u[0] = alpha * timing;
                u[N - 2] = Bi[N - 2];
                u[N-1] = beta * timing;
                impl[z][0] = alpha * timing;
                impl[z][N - 2] = Bi[N - 2];
                impl[z][N-1] = beta * timing;

                for (int i = 1; i < N - 1; i++)
                {
                    u[N - i - 1] = Ai[N - i - 1] * u[N - i] + Bi[N - i - 1];
                    impl[z][N - i - 1] = u[N - i - 1];
                }

                LineSeries curr = new LineSeries();
                for (int i = 0; i < N; i++)
                {
                    curr.Points.Add(new DataPoint(i, impl[z][i]));
                }
                _lines.Add(curr);
                //Запоминаем этот слой 
                for (int i = 0; i < N; i++)
                {
                    u0[i] = u[i];
                }
            }
            lblTimes.Content =_lines.Count;
            _model.Model.Series.Add(_lines[0]);
            _model.Model.InvalidatePlot(true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //insert разбиение X
            int N = 10;
            //insert конечное Time
            double T = 100;
            //insert left bound conditional coef
            double alpha = 500;
            //insert rigt bound conditional coef
            double beta = 500;

            Implicit(N, T, alpha, beta);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _model.Model.Series.Clear();
            _model.Model.Series.Add(_lines[Convert.ToInt32(textBoxLine.Text) - 1]);
            _model.Model.InvalidatePlot(true);
        }

        bool stop;
        BackgroundWorker worker;
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (stop)
            {
                stop = false;
            }
            else
            {
                stop = true;
                worker = new BackgroundWorker();
                worker.DoWork += worker_DoWork;
                worker.WorkerReportsProgress = true;
                worker.ProgressChanged += UpdateGui;
                worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                worker.RunWorkerAsync();
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (!stop)
                    break;
                Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {

                    _model.Model.Series.Clear();
                    _model.Model.Series.Add(_lines[i]);
                    textBoxLine.Text = i.ToString();
                    worker.ReportProgress(0);
                

                }));
                Thread.Sleep(300);
            }
        }

        private void worker_RunWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            stop = false;
        }

        private void UpdateGui(object sender,
                                       EventArgs e)
        {
            _model.Model.InvalidatePlot(true);
        }

    }
}
