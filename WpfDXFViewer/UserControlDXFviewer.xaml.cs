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
        /// CALL THIS AFTER you have parsed DXF file!
        /// </summary>
        internal void renderCurrentlyProcessedFile(bool isMirrorring)
        {
            if (dxfFile.Entities.Count == 0)
            {
                // parse dxf file first
                return;
            }
            // calculation of appropriate scale. TODO - move to subroutine and include rotation
            List<Double> boundBox = getActiveBoundBoxValues();
            double minX = boundBox[0];
            double minY = boundBox[1];
            double maxX = boundBox[2];
            double maxY = boundBox[3];
            double W = Math.Abs(minX - maxX);
            double H = Math.Abs(minY - maxY);
            double scaleX = this.ActualWidth / W;
            double scaleY = this.ActualHeight / H;
            double usedScale = W > H ? scaleX : scaleY;
            double usedScaleW = usedScale; double usedScaleH = usedScale;
            double usedCenterX = W / 2;
            double usedCenterY = H / 2;
            if (isMirrorring)
            {
                usedScaleW *= -1;
            }
                    double graphPlaneCenterX = this.renderBaseDXF.ActualWidth / 2;
            double graphPlaneCenterY = this.renderBaseDXF.ActualHeight / 2;
            
            // first - rotate, then - scale, after it - translate
            // mirroring may be after rotation or translation. But... no dedicated mirror transform in WPF?
            // potentially mirroring may be achieved together with scaling, by setting a negative sign
            // Mirroring should not affect bound box, it is performed by center of figure
            ScaleTransform scaleOperation = new ScaleTransform(usedScaleW, usedScaleH, usedCenterX, usedCenterY);
            TranslateTransform translocateOperation = new TranslateTransform(graphPlaneCenterX-usedCenterX,graphPlaneCenterY-usedCenterY);
            
            TransformGroup groupOperation = new TransformGroup();
            groupOperation.Children.Add(scaleOperation);
            groupOperation.Children.Add(translocateOperation);

            this.renderBaseDXF.Children.Clear();
            foreach (DxfEntity entity in dxfFile.Entities)
            {
                DxfColor entityColor = entity.Color;

                switch (entity.EntityType)
                {
                    case DxfEntityType.Line:
                        {
                            DxfLine lineDxf = (DxfLine)entity;
                            Line lineGraphic = new Line();
                            lineGraphic.X1 = lineDxf.P1.X - minX;
                            lineGraphic.Y1 = lineDxf.P1.Y - minY;
                            lineGraphic.X2 = lineDxf.P2.X - minX;
                            lineGraphic.Y2 = lineDxf.P2.Y - minY;
                            lineGraphic.Stroke = Brushes.Black;
                            lineGraphic.StrokeThickness = 1 / usedScale;
                            lineGraphic.RenderTransform = groupOperation;
                            
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
                            arcGraphic.StartAngle = UserControlDXFviewer.ConvertToRadians((arcDxf.EndAngle));
                            arcGraphic.EndAngle = UserControlDXFviewer.ConvertToRadians((arcDxf.StartAngle));
                            arcGraphic.Radius = arcDxf.Radius;
                            arcGraphic.Center = new Point(correctedXCenter, correctedYCenter);
                            arcGraphic.Stroke = Brushes.Black;
                            arcGraphic.StrokeThickness = 1 / usedScale;
                            arcGraphic.RenderTransform = groupOperation;
                            this.renderBaseDXF.Children.Add(arcGraphic);

                            break;
                        }
                }
            }
        }
        /// <summary>
        /// returns bounding box of DXF file: [minX,minY, maxX,maxY]
        /// with rotation applied
        /// </summary>
        /// <param name="inAngle">rotation angle in degrees</param>
        /// <returns></returns>
        public List<Double> getActiveBoundBoxValuesWithRotation(double inAngle)
        {
            List<Double> boundBox = getActiveBoundBoxValues();
            double assumedRotationCenterX = (boundBox[0] + boundBox[2]) / 2;
            double assumedRotationCenterY = (boundBox[1] + boundBox[3]) / 2;
            if ((inAngle == 0)||(inAngle==360)) {
                return boundBox;
            } else {
                Matrix rotationMatrix = new Matrix();
                rotationMatrix.SetIdentity();
                rotationMatrix.RotateAt(inAngle, assumedRotationCenterX, assumedRotationCenterY);
                boundBox[0] = Double.NaN; boundBox[1] = Double.NaN;
                boundBox[2] = Double.NaN; boundBox[3] = Double.NaN;
                foreach (var itemEntity in dxfFile.Entities)
                {
                    switch (itemEntity.EntityType)
                    {
                        case DxfEntityType.Line:
                            {
                                // calculate bound box for rotated line
                                Point P1Line = new Point((itemEntity as DxfLine).P1.X, (itemEntity as DxfLine).P1.Y);
                                Point P2Line = new Point((itemEntity as DxfLine).P2.X, (itemEntity as DxfLine).P2.Y);
                                Point P1LineRotated = rotationMatrix.Transform(P1Line);
                                Point P2LineRotated = rotationMatrix.Transform(P2Line);
                                if (Double.IsNaN(boundBox[0]) && Double.IsNaN(boundBox[1]) && Double.IsNaN(boundBox[2]) && Double.IsNaN(boundBox[3]))
                                {
                                    if (P1LineRotated.X < P2LineRotated.X)
                                    {
                                        boundBox[0] = P1LineRotated.X;
                                        boundBox[2] = P2LineRotated.X;
                                    }
                                    else
                                    {
                                        boundBox[2] = P1LineRotated.X;
                                        boundBox[0] = P2LineRotated.X;
                                    }
                                    if (P1LineRotated.Y < P2LineRotated.Y)
                                    {
                                        boundBox[0] = P1LineRotated.Y;
                                        boundBox[2] = P2LineRotated.Y;
                                    }
                                    else
                                    {
                                        boundBox[2] = P1LineRotated.Y;
                                        boundBox[0] = P2LineRotated.Y;
                                    }
                                } else {
                                    if (P1LineRotated.X < boundBox[0])  {
                                        boundBox[0] = P1LineRotated.X;
                                    }
                                    if (P1LineRotated.X>boundBox[2])
                                    {
                                        boundBox[2] = P1LineRotated.X;
                                    }
                                    if (P1LineRotated.Y < boundBox[1])
                                    {
                                        boundBox[1] = P1LineRotated.Y;
                                    }
                                    if (P1LineRotated.Y > boundBox[3])
                                    {
                                        boundBox[3] = P1LineRotated.Y;
                                    }
                                    // ===============================
                                    if (P2LineRotated.X < boundBox[0])
                                    {
                                        boundBox[0] = P2LineRotated.X;
                                    }
                                    if (P2LineRotated.X > boundBox[2])
                                    {
                                        boundBox[2] = P2LineRotated.X;
                                    }
                                    if (P2LineRotated.Y < boundBox[1])
                                    {
                                        boundBox[1] = P2LineRotated.Y;
                                    }
                                    if (P2LineRotated.Y > boundBox[3])
                                    {
                                        boundBox[3] = P2LineRotated.Y;
                                    }

                                }
                                break;
                            }
                        case DxfEntityType.Arc:
                            {
                                double centerX = (itemEntity as DxfArc).Center.X;
                                double centerY = (itemEntity as DxfArc).Center.Y;
                                double radiusArc = (itemEntity as DxfArc).Radius;
                                // angle(s) of arc is kept during rotation, center may move, together with start and end points
                                // regarding angles. They are measured relatively to horizontal direction, so they may be ... 
                                // new angle = old angle+rotation angle
                                // I checked this in QCAD, it should work. Geometrically it makes sense
                                double newStartAngle = ((itemEntity as DxfArc).StartAngle + inAngle) % 360;
                                double endStartAngle = ((itemEntity as DxfArc).EndAngle + inAngle) % 360;
                                

                                break; 
                            }
                    }
                }
                return boundBox;
            }

        }

        /// <summary>
        /// returns bounding box of DXF file: [minX,minY, maxX,maxY]
        /// just a bound box of dxf file, no rotation
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
        /// DOES NOT WORK, returns infinity
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
        /// NOT WORKING, returns INFINITY
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
            dxfFile = DxfFile.Load(inFilePath);
            currentBBox = dxfFile.GetBoundingBox();
            
            
        }
    }
}
