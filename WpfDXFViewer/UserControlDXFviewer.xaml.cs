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
        internal List<double> renderCurrentlyProcessedFile(bool isMirrorring, double rotationAngleDegrees)
        {
            List<Double> boundBox = new List<double>(new double[] { 0, 0, 0, 0 });
            if ((dxfFile==null)||(dxfFile.Entities.Count == 0))
            {
                // parse dxf file first
                return boundBox;
            }
            // calculation of appropriate scale. TODO - move to subroutine and include rotation
            
            boundBox = getActiveBoundBoxValuesWithRotation(rotationAngleDegrees);
            double minX = boundBox[0];
            double minY = boundBox[1];
            double maxX = boundBox[2];
            double maxY = boundBox[3];
            double W = Math.Abs(minX - maxX);
            double H = Math.Abs(minY - maxY);
            double scaleX = this.renderBaseDXF.ActualWidth / W;
            double scaleY = this.renderBaseDXF.ActualHeight / H;
            double usedScale = scaleX < scaleY ? scaleX : scaleY;
            double usedScaleW = usedScale; double usedScaleH = usedScale;
            // center on the figure with original scale
            double usedCenterX = (maxX-minX) / 2;
            double usedCenterY = (maxY-minY) / 2;
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
            TransformGroup groupOperation = new TransformGroup();
            TranslateTransform translocateOperation2 = new TranslateTransform(-minX * usedScaleW, -minY * usedScaleH);
            ScaleTransform scaleOperation = new ScaleTransform(usedScaleW, usedScaleH, usedCenterX, usedCenterY);
            TranslateTransform translocateOperation = new TranslateTransform(graphPlaneCenterX-usedCenterX,graphPlaneCenterY-usedCenterY);
            
            groupOperation.Children.Add(scaleOperation);
            groupOperation.Children.Add(translocateOperation2);
            if (rotationAngleDegrees % 360 != 0)
            {
                RotateTransform rotateOperation = new RotateTransform(rotationAngleDegrees, usedCenterX, usedCenterY);
                groupOperation.Children.Add(rotateOperation);
            }
            groupOperation.Children.Add(translocateOperation);
            
            this.renderBaseDXF.Children.Clear();
            /// ====== render bound box
            TransformGroup groupOperation1 = new TransformGroup();
            groupOperation1.Children.Add(scaleOperation);
            groupOperation1.Children.Add(translocateOperation);
            Line lGraphic1 = new Line();
            lGraphic1.X1 = 0; lGraphic1.Y1 = 0;
            lGraphic1.X2 = 0; lGraphic1.Y2 = maxY-minY;
            lGraphic1.Stroke = Brushes.Red;
            lGraphic1.StrokeThickness = 1 / usedScale;
            lGraphic1.RenderTransform = groupOperation1;
            Line lGraphic2 = new Line();
            lGraphic2.X1 = maxX-minX; lGraphic2.Y1 = 0;
            lGraphic2.X2 = maxX-minX; lGraphic2.Y2 = maxY-minY;
            lGraphic2.Stroke = Brushes.Green;
            lGraphic2.StrokeThickness = 1 / usedScale;
            lGraphic2.RenderTransform = groupOperation1;
            Line lGraphic3 = new Line();
            lGraphic3.X1 = 0; lGraphic3.Y1 = 0;
            lGraphic3.X2 = maxX- minX; lGraphic3.Y2 = 0;
            lGraphic3.Stroke = Brushes.Blue;
            lGraphic3.StrokeThickness = 1 / usedScale;
            lGraphic3.RenderTransform = groupOperation1;
            Line lGraphic4 = new Line();
            lGraphic4.X1 = 0; lGraphic4.Y1 = maxY-minY;
            lGraphic4.X2 = maxX- minX; lGraphic4.Y2 = maxY-minY;            
            lGraphic4.Stroke = Brushes.Cyan;
            lGraphic4.StrokeThickness = 1 / usedScale;
            lGraphic4.RenderTransform = groupOperation1;

            Line lcntr1 = new Line();
            lcntr1.X1 = (usedCenterX - 2); lcntr1.X2 = (usedCenterX + 2) ;
            lcntr1.Y1 = usedCenterY ; lcntr1.Y2 = usedCenterY ;
            lcntr1.Stroke = Brushes.DarkGreen;
            lcntr1.StrokeThickness = 1 / usedScale;
            lcntr1.RenderTransform = groupOperation1;

            Line lcntr2 = new Line();
            lcntr2.X1 = (usedCenterX ) ; lcntr2.X2 = (usedCenterX) ;
            lcntr2.Y1 = usedCenterY-2 ; lcntr2.Y2 = usedCenterY +2;
            lcntr2.Stroke = Brushes.DarkGreen;
            lcntr2.StrokeThickness = 1 / usedScale;
            lcntr2.RenderTransform = groupOperation1;

            this.renderBaseDXF.Children.Add(lcntr1);
            this.renderBaseDXF.Children.Add(lcntr2);
            this.renderBaseDXF.Children.Add(lGraphic1);
            this.renderBaseDXF.Children.Add(lGraphic2);
            this.renderBaseDXF.Children.Add(lGraphic3);
            this.renderBaseDXF.Children.Add(lGraphic4);
            /// ======

            foreach (DxfEntity entity in dxfFile.Entities)
            {
                DxfColor entityColor = entity.Color;

                switch (entity.EntityType)
                {
                    case DxfEntityType.Line:
                        {
                            DxfLine lineDxf = (DxfLine)entity;
                            Line lineGraphic = new Line();
                            
                            lineGraphic.X1 = lineDxf.P1.X;
                            lineGraphic.Y1 = lineDxf.P1.Y;
                            lineGraphic.X2 = lineDxf.P2.X;
                            lineGraphic.Y2 = lineDxf.P2.Y;
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
                            double correctedXCenter = arcDxf.Center.X;
                            double correctedYCenter = arcDxf.Center.Y;
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
            return boundBox;
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
                // rotation matrix is counter clockwise?
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
                                        boundBox[1] = P1LineRotated.Y;
                                        boundBox[3] = P2LineRotated.Y;
                                    }
                                    else
                                    {
                                        boundBox[3] = P1LineRotated.Y;
                                        boundBox[1] = P2LineRotated.Y;
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
                                double findNearestStraightAngle(double inAngle2)
                                {
                                    double retVal = 0;
                                    if ((inAngle2 >=0 ) && (inAngle2 < 90))         {
                                        retVal = 90;
                                    } else if ((inAngle2 >=90) && (inAngle2<180))   {
                                        retVal = 180;
                                    } else if ((inAngle2>=180)&&(inAngle2<270))     {
                                        retVal = 270;
                                    } else if ((inAngle2 >= 270) && (inAngle2 < 360))  {
                                        retVal = 360;
                                    } else if ((inAngle2 >= 360) && (inAngle2 < 450))  {
                                        retVal = 450;
                                    } else if ((inAngle2 >= 450) && (inAngle2 < 540))  {
                                        retVal = 540;
                                    } else if ((inAngle2 >= 540) && (inAngle2 < 630))  {
                                        retVal = 630;
                                    } else if ((inAngle2 >= 630) && (inAngle2 < 720))  {
                                        retVal = 720;
                                    }
                                        return retVal;
                                }
                                double centerX = (itemEntity as DxfArc).Center.X;
                                double centerY = (itemEntity as DxfArc).Center.Y;
                                // rotate center of Arc
                                Point centerNew = rotationMatrix.Transform(new Point(centerX, centerY));
                                double radiusArc = (itemEntity as DxfArc).Radius;
                                // angle(s) of arc is kept during rotation, center may move, together with start and end points
                                // regarding angles. They are measured relatively to horizontal direction, so they may be ... 
                                // new angle = old angle+rotation angle
                                // I checked this in QCAD, it should work. Geometrically it makes sense
                                // ALSO. in DXF arc is rotated counterclockwise
                                double newStartAngle = ((itemEntity as DxfArc).StartAngle + inAngle) % 360;
                                double newEndAngle = ((itemEntity as DxfArc).EndAngle + inAngle) % 360;
                                if (newEndAngle < newStartAngle)
                                {
                                    // arc may be intersecting zero horizontal
                                    newEndAngle += 360;
                                }
                                List<Point> valuablePoints = new List<Point>();
                                Point startPoint = new Point();
                                startPoint.X = centerNew.X + Math.Cos(ConvertToRadians(newStartAngle)) * radiusArc;
                                startPoint.Y = centerNew.Y + Math.Sin(ConvertToRadians(newStartAngle)) * radiusArc;
                                valuablePoints.Add(startPoint);
                                double iteratorAngle = findNearestStraightAngle(newStartAngle);
                                while (iteratorAngle < newEndAngle)
                                {
                                    Point valuablePoint = new Point();
                                    valuablePoint.X = centerNew.X + Math.Cos(ConvertToRadians(iteratorAngle)) * radiusArc;
                                    valuablePoint.Y = centerNew.Y + Math.Sin(ConvertToRadians(iteratorAngle)) * radiusArc;
                                    valuablePoints.Add(valuablePoint);
                                    iteratorAngle += 90;
                                }
                                Point endPoint = new Point();
                                endPoint.X = centerNew.X + Math.Cos(ConvertToRadians(newEndAngle)) * radiusArc;
                                endPoint.Y = centerNew.Y + Math.Sin(ConvertToRadians(newEndAngle)) * radiusArc;
                                valuablePoints.Add(endPoint);
                                // now, let's get the ACTUAL bound box of transformed arc
                                List<Double> currentBBoxArc = new List<double>(new double[] { Double.NaN, Double.NaN, Double.NaN, Double.NaN });
                                foreach (var valuablePointArc in valuablePoints)
                                {
                                    if (Double.IsNaN(currentBBoxArc[0]) || valuablePointArc.X < currentBBoxArc[0])   {
                                        currentBBoxArc[0] = valuablePointArc.X;
                                    } 
                                    if (Double.IsNaN(currentBBoxArc[1]) || valuablePointArc.Y < currentBBoxArc[1])   {
                                        currentBBoxArc[1] = valuablePointArc.Y;
                                    }
                                    if (Double.IsNaN(currentBBoxArc[2]) || valuablePointArc.X > currentBBoxArc[2])   {
                                        currentBBoxArc[2] = valuablePointArc.X;
                                    }
                                    if (Double.IsNaN(currentBBoxArc[3]) || valuablePointArc.Y > currentBBoxArc[3])
                                    {
                                        currentBBoxArc[3] = valuablePointArc.Y;
                                    }
                                }
                                // now, merge arc bbox with general bbox
                                if (Double.IsNaN(boundBox[0]) && Double.IsNaN(boundBox[1]) && Double.IsNaN(boundBox[2]) && Double.IsNaN(boundBox[3]))
                                { //arc was first
                                    boundBox[0] = currentBBoxArc[0];
                                    boundBox[1] = currentBBoxArc[1];
                                    boundBox[2] = currentBBoxArc[2];
                                    boundBox[3] = currentBBoxArc[3];
                                } else
                                {
                                   if (boundBox[0] > currentBBoxArc[0])  {
                                        boundBox[0] = currentBBoxArc[0];
                                   }
                                    if (boundBox[1] > currentBBoxArc[1])
                                    {
                                        boundBox[1] = currentBBoxArc[1];
                                    }
                                    if (boundBox[2] < currentBBoxArc[2])
                                    {
                                        boundBox[2] = currentBBoxArc[2];
                                    }
                                    if (boundBox[3] < currentBBoxArc[3])
                                    {
                                        boundBox[3] = currentBBoxArc[3];
                                    }
                                }
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
