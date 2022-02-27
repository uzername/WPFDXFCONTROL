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
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace WpfDXFViewer
{    
    /// <summary>
    /// Interaction logic for UserControlWPFDXFviewer.xaml
    /// </summary>
    public partial class UserControlDXFviewer : UserControl
    {
        public static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        private DxfBoundingBox currentBBox;
        private DxfFile dxfFile = null;
        /// <summary>
        /// returns bounding box of DXF file: [minX,minY, maxX,maxY]
        /// </summary>
        /// <returns></returns>
        public List<Double> getActiveBoundBoxValues()
        {
            List<Double> retV = new List<double>();
            retV.Add(currentBBox.MinimumPoint.X);
            retV.Add(currentBBox.MinimumPoint.Y);
            retV.Add(currentBBox.MaximumPoint.X);
            retV.Add(currentBBox.MaximumPoint.Y);
            return retV;
        }


        public UserControlDXFviewer()
        {
            InitializeComponent();
        }
        /// <summary>
        /// returns bounding box of currently drawn figurine: [minX,minY, maxX,maxY]
        /// </summary>
        public List<Double> getGeometricalBoundsOfGraphicalEntities()
        {
            List<Double> currentBBox = new List<double>(new double[] { Double.NaN, Double.NaN, Double.NaN, Double.NaN });
            foreach (var item in this.renderBaseDXF.Children)
            {
                if (item is Shape) {
                    Rect axisAlignedBBox = (item as Shape).RenderedGeometry.Bounds;
                    if (Double.IsNaN( currentBBox[0] ) || (axisAlignedBBox.Left < currentBBox[0]) )
                    {
                        currentBBox[0] = axisAlignedBBox.Left;
                    }
                    if (Double.IsNaN(currentBBox[1]) || (axisAlignedBBox.Bottom < currentBBox[1]))
                    {
                        currentBBox[1] = axisAlignedBBox.Bottom;
                    }
                    if (Double.IsNaN(currentBBox[2]) || (axisAlignedBBox.Right > currentBBox[2]))
                    {
                        currentBBox[2] = axisAlignedBBox.Right;
                    }
                    if (Double.IsNaN(currentBBox[3]) || (axisAlignedBBox.Top > currentBBox[3]))
                    {
                        currentBBox[3] = axisAlignedBBox.Top;
                    }
                }
            }
            return currentBBox;
        }
        /// <summary>
        /// NOT WORKING
        /// </summary>
        public void fitGraphicalEntitiesToView()
        {
            List<double> allEntitiesBounds = getGeometricalBoundsOfGraphicalEntities();
            double W = Math.Abs(allEntitiesBounds[0] - allEntitiesBounds[2]);
            double H = Math.Abs(allEntitiesBounds[1] - allEntitiesBounds[3]);
            double scaleX = this.ActualWidth / W;
            double scaleY = this.ActualHeight / H;
            double usedscale = (W < H) ? scaleX : scaleY;
            foreach (var item in this.renderBaseDXF.Children)
            {
                if (item is Shape)
                {
                    (item as Shape).RenderTransform = new ScaleTransform(usedscale, usedscale, (allEntitiesBounds[0] + allEntitiesBounds[2]) / 2, (allEntitiesBounds[1] + allEntitiesBounds[3]) / 2);
                }
            }
        }
        public void processDxfFile(String inFilePath)
        {
            this.renderBaseDXF.Children.Clear();
            dxfFile = DxfFile.Load(inFilePath);
            currentBBox = dxfFile.GetBoundingBox();
            List<Double> boundBox = getActiveBoundBoxValues();
            double minX = boundBox[0];
            double minY = boundBox[1];

            double maxX = boundBox[2];
            double maxY = boundBox[3];

            double scaleX = this.ActualWidth / Math.Abs(minX - maxX);
            double scaleY = this.ActualHeight / Math.Abs(minY - maxY);
            double usedScale = scaleX < scaleY ? scaleX : scaleY;


            foreach (DxfEntity entity in dxfFile.Entities)
            {
                DxfColor entityColor = entity.Color;

                switch (entity.EntityType)
                {
                    case DxfEntityType.Line:
                        {
                            DxfLine lineDxf = (DxfLine)entity;
                            Line lineGraphic = new Line();
                            lineGraphic.X1 = lineDxf.P1.X-minX;
                            lineGraphic.Y1 = lineDxf.P1.Y-minY;
                            lineGraphic.X2 = lineDxf.P2.X-minX;
                            lineGraphic.Y2 = lineDxf.P2.Y-minY;
                            lineGraphic.Stroke = Brushes.Black;
                            this.renderBaseDXF.Children.Add(lineGraphic);
                            break;
                        }
                    case DxfEntityType.Arc:
                        {
                            DxfArc arcDxf = (DxfArc)entity;
                            // arc in dxf is counterclockwise
                            Arc arcGraphic = new Arc();
                            double correctedXCenter = arcDxf.Center.X - minX;
                            double correctedYCenter = arcDxf.Center.Y - minY;
                            // ayyy lmao that's a meme but it works. I have no idea why it worked, but it... uhh, it will backfire at some case
                            arcGraphic.StartAngle = UserControlDXFviewer.ConvertToRadians( (arcDxf.EndAngle) );
                            arcGraphic.EndAngle = UserControlDXFviewer.ConvertToRadians( (arcDxf.StartAngle ) );
                            arcGraphic.Radius = arcDxf.Radius;
                            arcGraphic.Center = new Point(correctedXCenter, correctedYCenter);
                            
                            arcGraphic.Stroke = Brushes.Black;
                            this.renderBaseDXF.Children.Add(arcGraphic);

                            break;
                        }
                }
            }
            
        }
    }
}
