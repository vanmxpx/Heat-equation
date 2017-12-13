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
        double ThermalConductivity = 46;
        double HeatCapacity = 460;
        double Density = 7800;

        Func<double, double> initCond;

        public MainWindow()
        {
            InitializeComponent();
            _model = new MainViewModel();
            DataContext = _model;

            initCond = (x) => 500 + 250 * Math.Sin(Math.PI * x);


        }

        void Implicit(int N, double T, double alpha, double beta)
        {
            //init bound conditional coef
            double k = ThermalConductivity / (HeatCapacity * Density);
            // step by x 
            double h = 1f / N;
            //step by t
            double tau = T * h;
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
                u0[i] = initCond(i);
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

            //a = 1;
            //b = 2 + h * h / (k * tau);
            //c = 1;

            while (timing < T && z < l - 1 )
            {
                timing += tau;
                z++;          //current layer


                //A1 и B1 
                Ai[0] = -b / a;
                Bi[0] = (c * u0[0] - b * alpha * timing) / a;

                //Ai и Bi 
                for (int i = 2; i < N - 2; i++)
                {
                    Ai[i + 1] = -b / (b * Ai[i] + a);
                    Bi[i + 1] = (c * u0[i + 1] - b * Bi[i]) / (b * Ai[i] + a);
                }

                //An-1 и Bn-1 
                Ai[N - 2] = 0;
                Bi[N - 2] = (c * u0[N - 2] - b * beta * timing) / (b * Ai[N - 2] + a);

                //Прогонка 
                u[0] = alpha * timing;
                u[N - 2] = Bi[N - 2];
                u[N-1] = beta * timing;
                impl[z][0] = alpha * timing;
                impl[z][N - 2] = Bi[N - 2];
                impl[z][N-1] = beta * timing;

                for (int i = N - 3; i > 0; i--)
                {
                    u[i] = Ai[i+1] * u[i+1] + Bi[i+1];
                    impl[z][i] = u[i];
                }

                LineSeries curr = new LineSeries();

                for (double i = 0, x = 0 ; i < N; i++, x += h)
                {
                    curr.Points.Add(new DataPoint(x, impl[z][(int)i]));
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
            int N = 100;
            //insert конечное Time
            double T = 10;
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
