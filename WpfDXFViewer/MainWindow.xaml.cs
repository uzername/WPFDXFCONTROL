using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace WpfDXFViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RenderDXF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (String.IsNullOrEmpty(this.PathToDXF.Text) == false)
                {
                    this.DXFrenderPlane.processDxfFile(this.PathToDXF.Text);
                    this.DXFrenderPlane.renderCurrentlyProcessedFile();
                    List<Double> boundValues = this.DXFrenderPlane.getActiveBoundBoxValues();
                    LowerCoordBoundBox.Content = boundValues[0].ToString("0.####") + ";" + boundValues[1].ToString("0.####");
                    UpperCoordBoundBox.Content = boundValues[2].ToString("0.####") + ";" + boundValues[3].ToString("0.####");
                    
                }
            } catch (Exception e2)
            {
                MessageBox.Show(e2.Message);
            }
        }
    }
}
