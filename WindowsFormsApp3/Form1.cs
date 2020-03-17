using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpGL;
namespace WindowsFormsApp3
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            DrawTimer.Interval = 1000;
        }

        private void openGLControl_OpenGLDraw(object sender, SharpGL.RenderEventArgs args)
        {
            OpenGL gl = openGLControl.OpenGL;
            // Clear the color and depth buffer.
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            DrawAllShape(gl);
            gl.Flush();
        }

        private void openGLControl_OpenGLInitialized(object sender, EventArgs e)
        {
            OpenGL gl = openGLControl.OpenGL;
            // Set the clear color.
            gl.ClearColor(0, 0, 0, 0);
            // Set the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            // Load the identity.
            gl.LoadIdentity();

        }

        private void openGLControl_Resized(object sender, EventArgs e)
        {
            OpenGL gl = openGLControl.OpenGL;
            // Set the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            // Load the identity.
            gl.LoadIdentity();
            // Create a perspective transformation.
            gl.Viewport(0, 0, openGLControl.Width, openGLControl.Height);
            gl.Ortho2D(0, openGLControl.Width, 0, openGLControl.Height);
        }

        private class State
        {
            public string Choose;
            public int UnitOfLineWidth;
            public bool IsMouseDown;
            public int DrawTime;
            public HashSet<Point> PixelsOfChosenControlPoints;
            //public bool IsChoose;
            public Shape ChosenShape;
            public Point BeginPoint;
            public Point EndPoint;
            public State()
            {
                Choose = "DrawLine";
                UnitOfLineWidth = 1;
                IsMouseDown = false;
                DrawTime = 0;
                //IsChoose = false;
                PixelsOfChosenControlPoints = new HashSet<Point>();
            }
        }
        private class Properties
        {
            public Point PointBegin;
            public Point PointEnd;
            public int LineWidth;
            public Color ColorLine;
            public Color ColorFill;
            public Properties()
            {
                PointBegin = new Point();
                PointEnd = new Point();
                LineWidth = 1;
                ColorLine = Color.White;
                ColorFill = Color.Red;
            }
            public Properties(Point pb, Point pe, int lw, Color cl, Color cf)
            {
                PointBegin = new Point(pb.X, pb.Y);
                PointEnd = new Point(pe.X, pe.Y);
                LineWidth = lw;
                ColorLine = Color.FromArgb(cl.A, cl.R, cl.G, cl.B);
                ColorFill = Color.FromArgb(cf.A, cf.R, cf.G, cf.B);
            }
            public Properties DeepCopy()
            {
                Properties newProperties = new Properties(this.PointBegin, this.PointEnd, this.LineWidth, this.ColorLine, this.ColorFill);
                return newProperties;
            }
        }
        private class properties_fill
        {
            Color fill = Color.White;
        }
        private class Shape
        {
            public string Kind;
            public Properties Properties;
            public HashSet<Point> PixelsInLine;
            public bool IsFill;
            public HashSet<Point> PixelsInArea;
            public bool IsChosen;
            public List<Point> ListOfControlPoints;
            public List<Point> ListOfBetweenPoints;
            public bool IsDone;
            public Shape()
            {
                Kind = "Line";
                Properties = new Properties();
                PixelsInLine = new HashSet<Point>();
                IsFill = false;
                PixelsInArea = new HashSet<Point>();
                IsChosen = false;
                ListOfControlPoints = new List<Point>();

                IsDone = false;
                ListOfBetweenPoints = new List<Point>();
            }
        }
        class AffineTransform
        {
            public double[,] _matrixTransform = new double[3, 3];
            public double Ox, Oy;
            public AffineTransform()
            {
                Ox = 0;
                Oy = 0;
                _matrixTransform[0, 0] = 1;
                _matrixTransform[1, 1] = 1;
                _matrixTransform[2, 2] = 1;
            }
            public void Translate(double dx, double dy)
            {
                double[,] newMatrix = new double[3, 3];
                newMatrix[0, 0] = 1;
                newMatrix[1, 1] = 1;
                newMatrix[2, 2] = 1;
                newMatrix[0, 2] = dx;
                newMatrix[1, 2] = dy;
                Array.Copy(MulMatrix(_matrixTransform, 3, newMatrix, 3, 3), _matrixTransform, 9);
            }
            double[,] MulMatrix(double[,] firstMatrix, int h, double[,] secondMatrix, int w, int subw)
            {
                double[,] result = new double[h, w];
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        for (int k = 0; k < subw; k++)
                        {
                            result[i, j] += firstMatrix[i, k] * secondMatrix[k, j];
                        }
                    }
                }
                return result;
            }
            public void Rotate(double angle)
            {
                double[,] newMatrix = new double[3, 3];
                newMatrix[0, 0] = Math.Cos(angle * Math.PI / 180.0);

                newMatrix[1, 1] = Math.Cos(angle * Math.PI / 180.0);
                newMatrix[2, 2] = 1;
                newMatrix[0, 1] = -Math.Sin(angle * Math.PI / 180.0);
                newMatrix[1, 0] = Math.Sin(angle * Math.PI / 180.0);
                Array.Copy(MulMatrix(_matrixTransform, 3, newMatrix, 3, 3), _matrixTransform, 9);
            }
            public void Scale(double sx, double sy)
            {
                double[,] newMatrix = new double[3, 3];
                newMatrix[0, 0] = sx;
                newMatrix[1, 1] = sy;
                newMatrix[2, 2] = 1;
                Array.Copy(MulMatrix(_matrixTransform, 3, newMatrix, 3, 3), _matrixTransform, 9);
            }
            public void TransformPoint(ref double x, ref double y)
            {
                double[,] newMatrix = new double[3, 1];
                newMatrix[0, 0] = x - Ox;
                newMatrix[1, 0] = y - Oy;
                newMatrix[2, 0] = 1;
                Array.Copy(MulMatrix(_matrixTransform, 3, newMatrix, 1, 3), newMatrix, 3);
                x = newMatrix[0, 0] + Ox;
                y = newMatrix[1, 0] + Oy;
            }
            public void ReservedTransformPoint(ref double x, ref double y)
            {
                double[,] newMatrix = new double[3, 1];
                newMatrix[0, 0] = x - Ox;
                newMatrix[1, 0] = y - Oy;
                newMatrix[2, 0] = 1;
                Array.Copy(MulMatrix(InverseMatrix(_matrixTransform), 3, newMatrix, 1, 3), newMatrix, 3);
                x = newMatrix[0, 0] + Ox;
                y = newMatrix[1, 0] + Oy;
            }
            public void TransformList(string ShapeKind, List<Point> lp)
            {
                int minx = lp.OrderBy(n => n.X).First().X,
                    maxx = lp.OrderBy(n => n.X).Last().X,
                    miny = lp.OrderBy(n => n.Y).First().Y,
                    maxy = lp.OrderBy(n => n.Y).Last().Y;
                switch (ShapeKind)
                {
                    case "Triangle":
                        Ox = (minx + maxx) * 1.0 / 2;
                        Oy = maxy - (maxy - miny) * 1.0 / 3;
                        break;
                    case "Pentagon":
                        Ox = (minx + maxx) * 1.0 / 2;
                        Oy = maxy - (maxy - miny) * 1.0 / Math.Sqrt(5);
                        break;
                    default:
                        Ox = (minx + maxx) * 1.0 / 2;
                        Oy = (miny + maxy) * 1.0 / 2;
                        break;
                }
                double x, y;
                for (int i = 0; i < lp.Count; i++)
                {
                    x = lp[i].X;
                    y = lp[i].Y;
                    TransformPoint(ref x, ref y);
                    lp[i] = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
                }
            }
            public HashSet<Point> TransformPixels(string ShapeKind, HashSet<Point> points)
            {
                HashSet<Point> result = new HashSet<Point>();
                int minx = points.OrderBy(x => x.X).First().X,
                    maxx = points.OrderBy(x => x.X).Last().X,
                    miny = points.OrderBy(x => x.Y).First().Y,
                    maxy = points.OrderBy(x => x.Y).Last().Y;
                switch (ShapeKind)
                {
                    case "Triangle":
                        Ox = (minx + maxx) * 1.0 / 2;
                        Oy = maxy- (maxy - miny) * 1.0 / 3;
                        break;
                    case "Pentagon":
                        Ox = (minx + maxx) * 1.0 / 2;
                        Oy = maxy- (maxy - miny)*1.0/Math.Sqrt(5);
                        break;
                    default:
                        Ox = (minx + maxx) * 1.0 / 2;
                        Oy = (miny + maxy) * 1.0 / 2;
                        break;
                }
                double mindx = minx, mindy = miny, maxdx = maxx, maxdy = maxy, px, py;
                px = mindx; py = mindy;
                TransformPoint(ref px, ref py);
                minx = Convert.ToInt32(px);
                maxx = Convert.ToInt32(px);
                miny = Convert.ToInt32(py);
                maxy = Convert.ToInt32(py);
                px = mindx; py = maxdy;
                TransformPoint(ref px, ref py);
                minx = (Convert.ToInt32(px) > minx) ? minx : Convert.ToInt32(px);
                maxx = (Convert.ToInt32(px) > maxx) ? Convert.ToInt32(px) : maxx;
                miny = (Convert.ToInt32(py) > miny) ? miny : Convert.ToInt32(py);
                maxy = (Convert.ToInt32(py) > maxy) ? Convert.ToInt32(py) : maxy;
                px = maxdx; py = mindy;
                TransformPoint(ref px, ref py);
                minx = (Convert.ToInt32(px) > minx) ? minx : Convert.ToInt32(px);
                maxx = (Convert.ToInt32(px) > maxx) ? Convert.ToInt32(px) : maxx;
                miny = (Convert.ToInt32(py) > miny) ? miny : Convert.ToInt32(py);
                maxy = (Convert.ToInt32(py) > maxy) ? Convert.ToInt32(py) : maxy;
                px = maxdx; py = maxdy;
                TransformPoint(ref px, ref py);
                minx = (Convert.ToInt32(px) > minx) ? minx : Convert.ToInt32(px);
                maxx = (Convert.ToInt32(px) > maxx) ? Convert.ToInt32(px) : maxx;
                miny = (Convert.ToInt32(py) > miny) ? miny : Convert.ToInt32(py);
                maxy = (Convert.ToInt32(py) > maxy) ? Convert.ToInt32(py) : maxy;
                int ox = minx, oy = miny,
                    h = maxy - miny + 1, w = maxx - minx + 1;
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        double dx = ox + j, dy = oy + i;
                        ReservedTransformPoint(ref dx, ref dy);
                        int x = Convert.ToInt32(dx), y = Convert.ToInt32(dy);
                        if (points.Contains(new Point(x, y)))
                        {
                            result.Add(new Point(ox + j, oy + i));
                        }
                    }
                }
                return result;
            }
            double[,] InverseMatrix(double[,] matrix)
            {
                double det = matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[2, 1] * matrix[1, 2]) -
            matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0]) +
            matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]);

                double invdet = 1 / det;

                double[,] minv = new double[3, 3];
                minv[0, 0] = (matrix[1, 1] * matrix[2, 2] - matrix[2, 1] * matrix[1, 2]) * invdet;
                minv[0, 1] = (matrix[0, 2] * matrix[2, 1] - matrix[0, 1] * matrix[2, 2]) * invdet;
                minv[0, 2] = (matrix[0, 1] * matrix[1, 2] - matrix[0, 2] * matrix[1, 1]) * invdet;
                minv[1, 0] = (matrix[1, 2] * matrix[2, 0] - matrix[1, 0] * matrix[2, 2]) * invdet;
                minv[1, 1] = (matrix[0, 0] * matrix[2, 2] - matrix[0, 2] * matrix[2, 0]) * invdet;
                minv[1, 2] = (matrix[1, 0] * matrix[0, 2] - matrix[0, 0] * matrix[1, 2]) * invdet;
                minv[2, 0] = (matrix[1, 0] * matrix[2, 1] - matrix[2, 0] * matrix[1, 1]) * invdet;
                minv[2, 1] = (matrix[2, 0] * matrix[0, 1] - matrix[0, 0] * matrix[2, 1]) * invdet;
                minv[2, 2] = (matrix[0, 0] * matrix[1, 1] - matrix[1, 0] * matrix[0, 1]) * invdet;

                return minv;
            }
        }

        State CurrentState = new State();
        List<Shape> ListOfInstances = new List<Shape>();
        Properties CurrentCustom = new Properties();

        private void DrawAllShape(OpenGL gl)
        {
            foreach (Shape AnInstance in ListOfInstances)
            {
                gl.Color(AnInstance.Properties.ColorLine.R, AnInstance.Properties.ColorLine.G, AnInstance.Properties.ColorLine.B, AnInstance.Properties.ColorLine.A);
                gl.Begin(OpenGL.GL_POINTS);
                foreach (Point A in AnInstance.PixelsInLine)
                {
                    gl.Vertex(A.X, gl.RenderContextProvider.Height - A.Y);
                }
                gl.End();

                gl.Color(AnInstance.Properties.ColorFill.R, AnInstance.Properties.ColorFill.G, AnInstance.Properties.ColorFill.B, AnInstance.Properties.ColorFill.A);
                gl.Begin(OpenGL.GL_POINTS);
                foreach (Point A in AnInstance.PixelsInArea)
                {
                    gl.Vertex(A.X, gl.RenderContextProvider.Height - A.Y);
                }
                gl.End();
            }
            if (CurrentState.PixelsOfChosenControlPoints.Count!=0)
            {
                gl.Color(0.2f, 0.5f, 1.0f, 1.0f);
                gl.Begin(OpenGL.GL_POINTS);
                foreach (Point A in CurrentState.PixelsOfChosenControlPoints)
                {
                    gl.Vertex(A.X, gl.RenderContextProvider.Height - A.Y);
                }
                gl.End();
            }
        }
        private void openGLControl_MouseDown(object sender, MouseEventArgs e)
        {
            CurrentState.IsMouseDown = true;
            if (e.Button == MouseButtons.Left)
            {
                switch (CurrentState.Choose)
                {
                    case "DrawLine":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Start();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newLine = new Shape();
                        newLine.Kind = "Line";
                        newLine.Properties = (Properties)CurrentCustom.DeepCopy();
                        newLine.ListOfControlPoints.Add(e.Location);
                        newLine.ListOfControlPoints.Add(e.Location);
                        ListOfInstances.Add(newLine);
                        break;
                    case "DrawEllipse":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Start();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newEllipse = new Shape();
                        newEllipse.Kind = "Ellipse";
                        newEllipse.Properties = (Properties)CurrentCustom.DeepCopy();
                        newEllipse.ListOfControlPoints.Add(e.Location);
                        newEllipse.ListOfControlPoints.Add(e.Location);
                        newEllipse.ListOfControlPoints.Add(e.Location);
                        newEllipse.ListOfControlPoints.Add(e.Location);
                        ListOfInstances.Add(newEllipse);
                        break;
                    case "DrawCircle":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimer.Start();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newCircle = new Shape();
                        newCircle.Kind = "Circle";
                        newCircle.Properties = (Properties)CurrentCustom.DeepCopy();
                        newCircle.ListOfControlPoints.Add(e.Location);
                        newCircle.ListOfControlPoints.Add(e.Location);
                        newCircle.ListOfControlPoints.Add(e.Location);
                        newCircle.ListOfControlPoints.Add(e.Location);
                        ListOfInstances.Add(newCircle);
                        break;
                    case "DrawEquilTriangle":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimer.Start();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newEquilTriangle = new Shape();
                        newEquilTriangle.Kind = "EquilTriangle";
                        newEquilTriangle.Properties = (Properties)CurrentCustom.DeepCopy();
                        ListOfInstances.Add(newEquilTriangle);
                        break;
                    case "DrawRectangle":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Start();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newRectangle = new Shape();
                        newRectangle.Kind = "Rectangle";
                        newRectangle.Properties = (Properties)CurrentCustom.DeepCopy();
                        ListOfInstances.Add(newRectangle);
                        break;
                    case "DrawPentagon":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Start();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newPentagon = new Shape();
                        newPentagon.Kind = "Pentagon";
                        newPentagon.Properties = (Properties)CurrentCustom.DeepCopy();
                        ListOfInstances.Add(newPentagon);
                        break;
                    case "DrawHexagon":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Start();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newHexagon = new Shape();
                        newHexagon.Kind = "Hexagon";
                        newHexagon.Properties = (Properties)CurrentCustom.DeepCopy();
                        ListOfInstances.Add(newHexagon);
                        break;
                    case "DrawPolygon":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        if (ListOfInstances.Count > 0)
                        {
                            if (ListOfInstances[ListOfInstances.Count - 1].Kind == "Polygon")
                            {
                                if ((ListOfInstances[ListOfInstances.Count - 1]).IsDone == false)
                                {
                                    ListOfInstances[ListOfInstances.Count - 1].ListOfControlPoints.Add(new Point(
                                        e.Location.X,
                                        e.Location.Y));
                                    ListOfInstances[ListOfInstances.Count - 1].PixelsInLine = GeneratePolygon(
                                        ListOfInstances[ListOfInstances.Count - 1].ListOfControlPoints,
                                        ListOfInstances[ListOfInstances.Count - 1].Properties.LineWidth
                                        );
                                    break;
                                }
                            }
                        }
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Start();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        Shape newPolygon = new Shape();
                        newPolygon.Kind = "Polygon";
                        newPolygon.Properties = (Properties)CurrentCustom.DeepCopy();
                        newPolygon.ListOfControlPoints.Add(new Point(
                            e.Location.X,
                            e.Location.Y));
                        newPolygon.ListOfControlPoints.Add(new Point(
                            e.Location.X,
                            e.Location.Y));
                        ListOfInstances.Add(newPolygon);
                        break;
                    case "ShowControlPoint":
                        CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Stop();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        CurrentState.ChosenShape = Show_ControlPoint_Shape(CurrentCustom.PointBegin.X, CurrentCustom.PointBegin.Y);
                        //CurrentState.BeginPoint = e.Location;
                        //CurrentState.EndPoint = e.Location;
                        break;
                    case "Resize":
                        //CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0s";
                        DrawTimer.Stop();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        CurrentState.ChosenShape = Show_ControlPoint_Shape(CurrentCustom.PointBegin.X, CurrentCustom.PointBegin.Y);
                        CurrentState.BeginPoint = Find_Nearest_Point(CurrentState.ChosenShape.ListOfControlPoints, CurrentCustom.PointBegin);
                        //CurrentState.BeginPoint = e.Location;
                        CurrentState.EndPoint = e.Location;
                        break;
                    case "FloodFill":
                        //CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0 s";
                        DrawTimer.Stop();
                        floodFill(e.Location.X, e.Location.Y);
                        break;
                    case "Move":
                        //CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
                        DrawTimeLabel.Text = "Draw Time: 0s";
                        DrawTimer.Stop();
                        CurrentCustom.PointBegin = e.Location;
                        CurrentCustom.PointEnd = e.Location;
                        CurrentState.ChosenShape = Show_ControlPoint_Shape(CurrentCustom.PointBegin.X, CurrentCustom.PointBegin.Y);
                        CurrentState.BeginPoint = Find_Nearest_Point(CurrentState.ChosenShape.ListOfControlPoints, CurrentCustom.PointBegin);
                        //CurrentState.BeginPoint = e.Location;
                        CurrentState.EndPoint = e.Location;
                        break;
                    default:
                        break;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                switch (CurrentState.Choose)
                {
                    case "DrawPolygon":
                        if (ListOfInstances[ListOfInstances.Count - 1].Kind == "Polygon" && ListOfInstances[ListOfInstances.Count - 1].IsDone == false)
                        {
                            ListOfInstances[ListOfInstances.Count - 1].IsDone = true;
                            DrawTimer.Stop();
                            CurrentState.DrawTime = 0;
                        }
                        break;
                }
            }
        }

        private void openGLControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (CurrentState.IsMouseDown && e.Button == MouseButtons.Left)
            {
                int index = ListOfInstances.Count - 1;
                switch (CurrentState.Choose)
                {
                    case "DrawLine":
                        ListOfInstances[ListOfInstances.Count - 1].Properties.PointEnd = e.Location;
                        ListOfInstances[index].ListOfControlPoints[1] = e.Location;
                        ListOfInstances[index].PixelsInLine = GenerateLine(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "DrawEllipse":
                        ListOfInstances[index].Properties.PointEnd = e.Location;
                        ListOfInstances[index].ListOfControlPoints[1] = new Point(ListOfInstances[index].Properties.PointBegin.X, ListOfInstances[index].Properties.PointEnd.Y);
                        ListOfInstances[index].ListOfControlPoints[2] = new Point(ListOfInstances[index].Properties.PointEnd.X, ListOfInstances[index].Properties.PointEnd.Y);
                        ListOfInstances[index].ListOfControlPoints[3] = new Point(ListOfInstances[index].Properties.PointEnd.X, ListOfInstances[index].Properties.PointBegin.Y);
                        ListOfInstances[index].PixelsInLine = GenerateEllipse(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "DrawCircle":
                        ListOfInstances[index].Properties.PointEnd = e.Location;

                        ListOfInstances[index].ListOfControlPoints = GenerateControlPointCircle(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y
                            );
                        /*ListOfInstances[index].ListOfControlPoints[1] = new Point(ListOfInstances[index].Properties.PointBegin.X, ListOfInstances[index].Properties.PointEnd.Y);
                        ListOfInstances[index].ListOfControlPoints[2] = new Point(ListOfInstances[index].Properties.PointEnd.X, ListOfInstances[index].Properties.PointEnd.Y);
                        ListOfInstances[index].ListOfControlPoints[3] = new Point(ListOfInstances[index].Properties.PointEnd.X, ListOfInstances[index].Properties.PointBegin.Y);*/
                        ListOfInstances[index].PixelsInLine = GenerateCircle(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "DrawEquilTriangle":
                        ListOfInstances[index].Properties.PointEnd = e.Location;
                        ListOfInstances[index].ListOfControlPoints = BuildEquilateralTriangle(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y
                            );
                        ListOfInstances[index].PixelsInLine = GeneratePolygon(
                            ListOfInstances[index].ListOfControlPoints,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "DrawRectangle":
                        ListOfInstances[index].Properties.PointEnd = e.Location;
                        ListOfInstances[index].ListOfControlPoints = BuildRectangle(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y
                            );
                        ListOfInstances[index].PixelsInLine = GeneratePolygon(
                            ListOfInstances[index].ListOfControlPoints,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "DrawPentagon":
                        ListOfInstances[index].Properties.PointEnd = e.Location;
                        ListOfInstances[index].ListOfControlPoints = BuildPentagon(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y
                            );
                        ListOfInstances[index].PixelsInLine = GeneratePolygon(
                            ListOfInstances[index].ListOfControlPoints,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "DrawHexagon":
                        ListOfInstances[index].Properties.PointEnd = e.Location;
                        ListOfInstances[index].ListOfControlPoints = BuildHexagon(
                            ListOfInstances[index].Properties.PointBegin.X,
                            ListOfInstances[index].Properties.PointBegin.Y,
                            ListOfInstances[index].Properties.PointEnd.X,
                            ListOfInstances[index].Properties.PointEnd.Y
                            );
                        ListOfInstances[index].PixelsInLine = GeneratePolygon(
                            ListOfInstances[index].ListOfControlPoints,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "DrawPolygon":
                        ListOfInstances[index].ListOfControlPoints[
                            ListOfInstances[index].ListOfControlPoints.Count - 1
                            ] = e.Location;
                        ListOfInstances[index].PixelsInLine = GeneratePolygon(
                            ListOfInstances[ListOfInstances.Count - 1].ListOfControlPoints,
                            ListOfInstances[index].Properties.LineWidth
                            );
                        break;
                    case "Resize":
                        //Point nearest = Find_Nearest_Point(CurrentState.ChosenShape.ListOfControlPoints, CurrentCustom.PointBegin);

                        Resize_Shape(ref CurrentState.ChosenShape, CurrentState.BeginPoint, CurrentCustom.PointEnd);
                        CurrentState.ChosenShape.Properties.PointEnd = e.Location;
                        CurrentState.ChosenShape.ListOfControlPoints[1] = e.Location;
                        CurrentState.ChosenShape.PixelsInLine = GenerateLine(
                            CurrentState.ChosenShape.ListOfControlPoints[0].X,
                            CurrentState.ChosenShape.ListOfControlPoints[0].Y,
                            CurrentState.ChosenShape.ListOfControlPoints[1].X,
                            CurrentState.ChosenShape.ListOfControlPoints[1].Y,
                            CurrentState.ChosenShape.Properties.LineWidth
                            );
                        UpdatePixelsOfControlPoints();
                        break;
                    case "Move":
                        CurrentState.BeginPoint = CurrentState.EndPoint;
                        CurrentState.EndPoint = e.Location;
                        AffineTransform affineM = new AffineTransform();
                        affineM.Translate(CurrentState.EndPoint.X - CurrentState.BeginPoint.X, CurrentState.EndPoint.Y - CurrentState.BeginPoint.Y);
                        affineM.TransformList(CurrentState.ChosenShape.Kind, CurrentState.ChosenShape.ListOfControlPoints);
                        switch (CurrentState.ChosenShape.Kind)
                        {
                            case "Line":
                            case "Circle":
                            case "Ellipse":
                                CurrentState.ChosenShape.PixelsInLine = affineM.TransformPixels(CurrentState.ChosenShape.Kind, CurrentState.ChosenShape.PixelsInLine);
                                break;
                            default:
                                CurrentState.ChosenShape.PixelsInLine=GeneratePolygon(CurrentState.ChosenShape.ListOfControlPoints, CurrentState.ChosenShape.Properties.LineWidth);
                                break;
                        }
                        if (CurrentState.ChosenShape.PixelsInArea.Count() != 0)
                            CurrentState.ChosenShape.PixelsInArea = affineM.TransformPixels(CurrentState.ChosenShape.Kind, CurrentState.ChosenShape.PixelsInArea);
                        UpdatePixelsOfControlPoints();

                        break;
                }
            }
        }   
        private void openGLControl_MouseUp(object sender, MouseEventArgs e)
        {
            CurrentState.IsMouseDown = false;
            if (CurrentState.Choose != "DrawPolygon")
            {
                DrawTimer.Stop();
                CurrentState.DrawTime = 0;
            }
        }

        private void DrawLineButton_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawLine";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void DrawCircleButton_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawCircle";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void DrawEllipseButton_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawEllipse";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void DrawRectangleButton_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawRectangle";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void DrawEquilateralTriangle_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawEquilTriangle";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void DrawPentagonButton_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawPentagon";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void DrawHexagonButton_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawHexagon";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void DrawPolygonButton_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "DrawPolygon";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void Show_ControlPoint(object sender, EventArgs e)
        {
            CurrentState.Choose = "ShowControlPoint";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }
        private void Resize_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "Resize";
            DrawTimeLabel.Text = "Draw Time: 0s";
            DrawTimer.Stop();
        }
        private void Move_Button_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "Move";
            DrawTimeLabel.Text = "Draw Time: 0s";
            DrawTimer.Stop();
        }
        private void FloodFill_Button_Click(object sender, EventArgs e)
        {
            CurrentState.Choose = "FloodFill";
            DrawTimeLabel.Text = "Draw Time: 0 s";
            DrawTimer.Stop();
        }

        private void SetLineColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                CurrentCustom.ColorLine = colorDialog1.Color;
            }
        }
        private void SetFillColorButton_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                CurrentCustom.ColorFill = colorDialog1.Color;
            }
        }
        private void ClearScreen_Click(object sender, EventArgs e)
        {
            CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
            ListOfInstances.Clear();
            CurrentState.DrawTime = 0;
            DrawTimer.Stop();
            DrawTimeLabel.Text = "Draw Time: 0 s";
        }

        private void DrawTimer_Tick(object sender, EventArgs e)
        {
            CurrentState.DrawTime += 1;
            DrawTimeLabel.Text = "Draw Time: " + CurrentState.DrawTime.ToString() + " s";
            DrawTimer.Start();
        }
        private void SetLineWidthUI_ValueChanged(object sender, EventArgs e)
        {
            CurrentCustom.LineWidth = Convert.ToInt32(decimal.ToSingle(SetLineWidthUI.Value)) * CurrentState.UnitOfLineWidth;
        }

        private HashSet<Point> putpixels(int x, int y, int widthLine)
        {
            HashSet<Point> newList = new HashSet<Point>();
            for (int i = 0; i < widthLine; i++)
                for (int j = 0; j < widthLine; j++)
                {
                    newList.Add(new Point(x + i, y + j));
                }
            return newList;
        }
        private HashSet<Point> GenerateLineLow(int x1, int y1, int x2, int y2, int widthLine)
        {
            HashSet<Point> result = new HashSet<Point>();
            int dx, dy, yi, D;
            int x, y;
            dx = x2 - x1;
            dy = y2 - y1;
            yi = 1;
            if (dy < 0)
            {
                yi = -1;
                dy = -dy;
            }
            D = 2 * dy - dx;
            y = y1;
            for (x = x1; x <= x2; x++)
            {
                result.UnionWith(putpixels(x, y, widthLine));
                if (D > 0)
                {
                    y += yi;
                    D -= 2 * dx;
                }
                D += 2 * dy;
            }
            return result;
        }
        private HashSet<Point> GenerateLineHigh(int x1, int y1, int x2, int y2, int widthLine)
        {
            HashSet<Point> result = new HashSet<Point>();
            int dx, dy, xi, D;
            int x, y;
            dx = x2 - x1;
            dy = y2 - y1;
            xi = 1;
            if (dx < 0)
            {
                xi = -1;
                dx = -dx;
            }
            D = 2 * dx - dy;
            x = x1;
            for (y = y1; y <= y2; y++)
            {
                result.UnionWith(putpixels(x, y, widthLine));
                if (D > 0)
                {
                    x += xi;
                    D -= 2 * dy;
                }
                D += 2 * dx;
            }
            return result;
        }
        private HashSet<Point> GenerateLine(int x1, int y1, int x2, int y2, int widthLine)
        {
            HashSet<Point> result = new HashSet<Point>();

            if (Math.Abs(y2 - y1) < Math.Abs(x2 - x1))
            {
                if (x1 > x2)
                    result.UnionWith(GenerateLineLow(x2, y2, x1, y1, widthLine));
                else
                    result.UnionWith(GenerateLineLow(x1, y1, x2, y2, widthLine));
            }
            else
            {
                if (y1 > y2)
                    result.UnionWith(GenerateLineHigh(x2, y2, x1, y1, widthLine));
                else
                    result.UnionWith(GenerateLineHigh(x1, y1, x2, y2, widthLine));
            }
            return result;
        }
        
        private HashSet<Point> GenerateEllipse(int x1, int y1, int x2, int y2, int widthLine)
        {
            HashSet<Point> result = new HashSet<Point>();
            int Ry = Math.Abs(y1 - y2) / 2;
            int Rx = Math.Abs(x1 - x2) / 2;
            int Rx2 = Rx * Rx;
            int Ry2 = Ry * Ry;
            int twoRx2 = 2 * Rx2;
            int twoRy2 = 2 * Ry2;
            int p;
            int x = 0;
            int y = Ry;
            int a = (x1 + x2) / 2;
            int b = (y1 + y2) / 2;
            int px = 0;
            int py = twoRx2 * y;
            result.UnionWith(ellipsePlotPoints(a, b, x, y, widthLine));

            p = Convert.ToInt32(Ry2 - (Rx2 * Ry) + (0.25 + Rx2));
            while (px < py)
            {
                x++;
                px += twoRy2;
                if (p < 0)
                {
                    p += Ry2 + px;
                }
                else
                {
                    y--;
                    py -= twoRx2;
                    p += Ry2 + px - py;
                }
                result.UnionWith(ellipsePlotPoints(a, b, x, y, widthLine));
            }
            p = Convert.ToInt32(Ry2 * (x + 0.5) * (x + 0.5) + Rx2 * (y - 1) * (y - 1) - Rx2 * Ry2);
            while (y > 0)
            {
                y--;
                py -= twoRx2;
                if (p > 0) p += Rx2 - py;
                else
                {
                    x++;
                    px += twoRy2;
                    p += Rx2 - py + px;
                }
                result.UnionWith(ellipsePlotPoints(a, b, x, y, widthLine));
            }
            return result;
        }
        private HashSet<Point> ellipsePlotPoints(int x1, int y1, int x2, int y2, int widthLine)
        {
            HashSet<Point> result = new HashSet<Point>();
            result.UnionWith(putpixels(x1 + x2, y1 + y2, widthLine));

            result.UnionWith(putpixels(x1 - x2, y1 + y2, widthLine));

            result.UnionWith(putpixels(x1 + x2, y1 - y2, widthLine));

            result.UnionWith(putpixels(x1 - x2, y1 - y2, widthLine));
            return result;
        }
        
        private List<Point> BuildRectangle(int x1, int y1, int x2, int y2)
        {
            List<Point> result = new List<Point>();
            result.Add(new Point(x1, y1));
            result.Add(new Point(x1, y2));
            result.Add(new Point(x2, y2));
            result.Add(new Point(x2, y1));
            return result;
        }

        private List<Point> BuildPentagon(int x1, int y1, int x2, int y2)
        {
            List<Point> result = new List<Point>();
            int bx, by, cx, cy;

            cx = x2 - x1;
            cy = y2 - y1;
            if (cx < 0)
            {
                cx = -cx;
                if (Math.Abs(cy) * 1.0 / cx < Math.Sqrt(5 + 2 * Math.Sqrt(5)) / (1 + Math.Sqrt(5)))
                {
                    double tempA = Math.Abs(cy) * 2 / (Math.Sqrt(5 + 2 * Math.Sqrt(5)));
                    bx = x1 - Convert.ToInt32(tempA * ((1 + Math.Sqrt(5)) / 2));
                }
                else
                {
                    bx = x2;
                }
            }
            else
            {
                bx = x1;
            }
            if (cy < 0)
            {
                cy = -cy;
                by = y1;
            }
            else
            {
                if (cy * 1.0 / Math.Abs(cx) > Math.Sqrt(5 + 2 * Math.Sqrt(5)) / (1 + Math.Sqrt(5)))
                {
                    double tempA = Math.Abs(cx) * 2 / (1 + Math.Sqrt(5));
                    by = y1 + Convert.ToInt32(tempA * ((Math.Sqrt(5 + 2 * Math.Sqrt(5))) / 2));
                }
                else
                {
                    by = y2;
                }
            }

            double a = 0;
            if (cy * 1.0 / cx < Math.Sqrt(5 + 2 * Math.Sqrt(5)) / (1 + Math.Sqrt(5)))
            {
                a = cy * 2 / (Math.Sqrt(5 + 2 * Math.Sqrt(5)));
            }
            else
            {
                a = cx * 2 / (1 + Math.Sqrt(5));
            }

            result.Add(new Point(
                    bx + 0,
                    Convert.ToInt32(by - a * Math.Sqrt((5 + Math.Sqrt(5)) / 8))
                    ));
            result.Add(new Point(
                    Convert.ToInt32(bx + a * (Math.Sqrt(5) - 1) / 4),
                    by - 0
                    ));
            result.Add(new Point(
                    Convert.ToInt32(bx + a * ((3 + Math.Sqrt(5)) / 4)),
                    by - 0
                    ));
            result.Add(new Point(
                    Convert.ToInt32(bx + a * ((1 + Math.Sqrt(5)) / 2)),
                    Convert.ToInt32(by - a * (Math.Sqrt((5 + Math.Sqrt(5)) / 8)))
                    ));
            result.Add(new Point(
                    Convert.ToInt32(bx + a * ((1 + Math.Sqrt(5)) / 4)),
                    Convert.ToInt32(by - a * ((Math.Sqrt(5 + 2 * Math.Sqrt(5))) / 2))
                    ));
            return result;
        }
        private List<Point> BuildHexagon(int x1, int y1, int x2, int y2)
        {
            //Mindset là vẽ đường tròn xong lấy 2 điểm đối xứng qua tâm,
            //Tiếp tục vẽ đường tròn từ 2 điểm đấy rồi ta sẽ có cắt đường tròn đầu tiên 4 điểm
            //Sau đó nối 6 điểm với nhau ta được lục giác đều.
            //Tính xấp xỉ gần đúng khoảng cách các điểm
            //Ref: https://stackoverflow.com/questions/58117115/hexagon-packing-algorithm
            List<Point> result = new List<Point>();
            int x = (x1 + x2) / 2;
            int y = (y1 + y2) / 2;
            int R = Math.Abs((x1 - x2) / 2);
            List<int> coorx = new List<int>();
            List<int> coory = new List<int>();

            for (int i = 0; i < 6; i++)
            {
                int tmpx = Convert.ToInt32(x + R * (float)Math.Cos(i * 60 * Math.PI / 180f));
                int tmpy = Convert.ToInt32(y + R * (float)Math.Sin(i * 60 * Math.PI / 180f));
                coorx.Add(tmpx);
                coory.Add(tmpy);
            }
            for (int i = 0; i < 6; i++)
            {
                result.Add(new Point(coorx[i], coory[i]));
            }
            return result;
        }
 
            private HashSet<Point> GeneratePolygon(List<Point> lp, int lineWidth)
        {
            HashSet<Point> result = new HashSet<Point>();
            for (int i = 0; i < lp.Count - 1; i++)
            {
                result.UnionWith(GenerateLine(lp[i].X, lp[i].Y, lp[i + 1].X, lp[i + 1].Y, lineWidth));
            }
            if (lp.Count > 0)
            {
                result.UnionWith(GenerateLine(lp[0].X, lp[0].Y, lp[lp.Count-1].X, lp[lp.Count-1].Y, lineWidth));
            }
            return result;
        }
        
        private HashSet<Point> put8Pixels(int a, int b, int x, int y, int widthLine)
        {
            HashSet<Point> result = new HashSet<Point>();
            result.UnionWith(putpixels(x + a, y + b, widthLine));

            result.UnionWith(putpixels(x + a, b - y, widthLine));

            result.UnionWith(putpixels(a - x, y + b, widthLine));

            result.UnionWith(putpixels(a - x, b - y, widthLine));

            result.UnionWith(putpixels(y + a, x + b, widthLine));

            result.UnionWith(putpixels(a - y, x + b, widthLine));

            result.UnionWith(putpixels(a + y, b - x, widthLine));

            result.UnionWith(putpixels(a - y, b - x, widthLine));
            return result;
        }
        private List<Point> GenerateControlPointCircle(int x1, int y1, int x2, int y2)
        {
            List<Point> result = new List<Point>();
            double R;
            int deltaX = x2 - x1;
            int deltaY = y2 - y1;

            if (Math.Abs(deltaX) < Math.Abs(deltaY))
            {
                R = Math.Abs(deltaX) / 2;
            }
            else
                R = Math.Abs(deltaY) / 2;

            int x, y, p, a, b;

            if (deltaY < 0)
            {
                if (deltaX < 0)
                {
                    a = Convert.ToInt32(x1 - R);
                    b = Convert.ToInt32(y1 - R);
                }
                else
                {
                    a = Convert.ToInt32(x1 + R);
                    b = Convert.ToInt32(y1 - R);
                }
            }
            else
            {
                if (deltaX < 0)
                {
                    a = Convert.ToInt32(x1 - R);
                    b = Convert.ToInt32(y1 + R);
                }
                else
                {
                    a = Convert.ToInt32(x1 + R);
                    b = Convert.ToInt32(y1 + R);
                }
            }
            int r = Convert.ToInt32(R);
            result.Add(new Point(a - r, b - r));
            result.Add(new Point(a + r, b - r));
            result.Add(new Point(a + r, b + r));
            result.Add(new Point(a - r, b + r));
            return result;
        }
        private HashSet<Point> GenerateCircle(int x1, int y1, int x2, int y2, int widthLine)
        {
            HashSet<Point> result = new HashSet<Point>();

            double R;
            int deltaX = x2 - x1;
            int deltaY = y2 - y1;

            if (Math.Abs(deltaX) < Math.Abs(deltaY))
            {
                R = Math.Abs(deltaX) / 2;
            }
            else
                R = Math.Abs(deltaY) / 2;

            int x, y, p, a, b;

            if (deltaY < 0)
            {
                if (deltaX < 0)
                {
                    a = Convert.ToInt32(x1 - R);
                    b = Convert.ToInt32(y1 - R);
                }
                else
                {
                    a = Convert.ToInt32(x1 + R);
                    b = Convert.ToInt32(y1 - R);
                }
            }
            else
            {
                if (deltaX < 0)
                {
                    a = Convert.ToInt32(x1 - R);
                    b = Convert.ToInt32(y1 + R);
                }
                else
                {
                    a = Convert.ToInt32(x1 + R);
                    b = Convert.ToInt32(y1 + R);
                }
            }
            x = 0;
            y = Convert.ToInt32(R);
            result.UnionWith(put8Pixels(a, b, x, y, widthLine));
            p = Convert.ToInt32(5 / 4 - R);
            while (x < y)
            {
                if (p < 0)
                    p += 2 * x + 3;
                else
                {
                    p += 2 * (x - y) + 5;
                    y--;
                }
                x++;
                result.UnionWith(put8Pixels(a, b, x, y, widthLine));
            }
            return result;
        }

        private List<Point> BuildEquilateralTriangle(int x1, int y1, int x2, int y2)
        {
            int a;
            Point vertex1 = new Point(), vertex2 = new Point(), vertex3 = new Point();
            List<Point> result = new List<Point>();

            int deltaX = x2 - x1;
            int deltaY = y2 - y1;

            if (Math.Abs(deltaX) < Math.Abs(deltaY) / Math.Sin(Math.PI / 3))
                a = Math.Abs(deltaX);
            else
                a = Math.Abs(Convert.ToInt32(deltaY / Math.Sin(Math.PI / 3)));

            if (deltaY > 0)
            {
                if (deltaX > 0)
                {
                    vertex1 = new Point(x1 + a / 2, y1);
                    vertex2 = new Point(x1, Convert.ToInt32(y1 + a * Math.Sin(Math.PI / 3)));
                    vertex3 = new Point(vertex2.X + a, vertex2.Y);
                }
                if (deltaX < 0)
                {
                    vertex1 = new Point(x1 - a / 2, y1);
                    vertex2 = new Point(x1, Convert.ToInt32(y1 + a * Math.Sin(Math.PI / 3)));
                    vertex3 = new Point(vertex2.X - a, vertex2.Y);
                }
            }
            else
            {
                if (deltaX > 0)
                {
                    vertex1 = new Point(x1, y1);
                    vertex2 = new Point(x1 + a, y1);
                    vertex3 = new Point(x1 + a / 2, Convert.ToInt32(y1 - a * Math.Sin(Math.PI / 3)));
                }
                if (deltaX < 0)
                {
                    vertex1 = new Point(x1, y1);
                    vertex2 = new Point(x1 - a, y1);
                    vertex3 = new Point(x1 - a / 2, Convert.ToInt32(y1 - a * Math.Sin(Math.PI / 3)));
                }
            }

            result.Add(new Point(vertex1.X, vertex1.Y));
            result.Add(new Point(vertex2.X, vertex2.Y));
            result.Add(new Point(vertex3.X, vertex3.Y));
            return result;
        }
        private Shape Show_ControlPoint_Shape(int x, int y)
        {
            Point local = new Point(x, y);
            Shape NearestShape = Find_Min_Distance(local, ListOfInstances);
            if (NearestShape != null)
            {
                CurrentState.ChosenShape = NearestShape;
                UpdatePixelsOfControlPoints();
            }
            return NearestShape;
        }
        private void UpdatePixelsOfControlPoints()
        {
            CurrentState.PixelsOfChosenControlPoints = new HashSet<Point>();
            HashSet<Point> drawPoints = new HashSet<Point>();
            foreach (Point index in CurrentState.ChosenShape.ListOfControlPoints)
            {
                drawPoints = putpixels(index.X - 5, index.Y - 5, 10);
                CurrentState.PixelsOfChosenControlPoints.UnionWith(drawPoints);
            }
        }
        private Shape Find_Min_Distance(Point curr, List<Shape> ListShapes)
        {
            double minDistance = 0f;
            Point PointMinDistance = new Point();
            //Find point has min distance to click point
            foreach (Shape index in ListShapes)
            {
                foreach (Point point in index.PixelsInLine)
                {
                    //CalDistance
                    double distance = CalDistance(curr, point);
                    if (distance < minDistance || minDistance == 0f)
                    {
                        minDistance = distance;
                        PointMinDistance = point;
                    }
                }
            }
            //
            foreach (Shape index in ListShapes)
            {
                bool alReadyExist = index.PixelsInLine.Contains(PointMinDistance);
                if (alReadyExist)
                {
                    return index;
                }
                else continue;
            }
            return null;
        }
        private double CalDistance(Point p0, Point p1)
        {
            //Tinh khoang cach 2 diem (khoang cach Euclid)
            double x = Math.Pow((p0.X - p1.X), 2);
            double y = Math.Pow((p0.Y - p1.Y), 2);
            return Math.Sqrt(x + y);
        }
        private byte checkPointOnEdge(Point p, HashSet<Point> shape)
        {
            foreach (Point temp in shape)
            {
                if (p.X == temp.X && p.Y == temp.Y)
                    return 1;
            }
            return 0;
        }
        private byte checkPointInShape(Point p, HashSet<Point> shape)//hàm kiểm tra điểm nằm trong hình chữ nhật bao hình.
        {
            const int MAX_VALUE = 1000000;
            const int MIN_VALUE = 0;
            int xmax = MIN_VALUE, ymax = MIN_VALUE;
            int xmin = MAX_VALUE, ymin = MAX_VALUE;

            foreach (Point temp in shape)
            {
                if (temp.X > xmax)
                    xmax = temp.X;
                if (temp.X < xmin)
                    xmin = temp.X;
                if (temp.Y > ymax)
                    ymax = temp.Y;
                if (temp.Y < ymin)
                    ymin = temp.Y;
            }

            if (p.X >= xmin && p.X <= xmax && p.Y >= ymin && p.Y <= ymax)
                return 1;
            return 0;
        }
        private void floodFill(int x, int y)
        {
            List<Point> pixels = new List<Point>();
            Shape temp_s = new Shape();
            OpenGL gl = openGLControl.OpenGL;

            int[,] checkFill = new int[gl.RenderContextProvider.Width + 1, gl.RenderContextProvider.Height + 1];
            for (int i = 0; i <= gl.RenderContextProvider.Width; i++)
            {
                for (int j = 0; j <= gl.RenderContextProvider.Height; j++)
                {
                    checkFill[i, j] = 0;
                }
            }

            int index = 0;
            foreach (Shape s in ListOfInstances)
            {
                if (checkPointInShape(new Point(x, y), s.PixelsInLine) == 1)
                {
                    temp_s = s;
                    temp_s.Properties.ColorFill = CurrentCustom.ColorFill;
                    break;
                }
                index++;
            }

            Point temp = new Point(x, y);
            pixels.Add(temp);
            while (pixels.Count() > 0)
            {
                Point a = pixels[0];
                pixels.RemoveAt(0);

                if ((a.X <= gl.RenderContextProvider.Width && a.X >= 0 && a.Y <= gl.RenderContextProvider.Height && a.Y >= 0)
                    && checkPointOnEdge(a, temp_s.PixelsInLine) == 0)
                {
                    if (checkFill[a.X, a.Y] == 0)
                    {
                        temp_s.PixelsInArea.Add(a);
                        checkFill[a.X, a.Y] = 1;
                        pixels.Add(new Point(a.X + 1, a.Y));
                        pixels.Add(new Point(a.X, a.Y + 1));
                        pixels.Add(new Point(a.X - 1, a.Y));
                        pixels.Add(new Point(a.X, a.Y - 1));
                    }
                }
            }
            ListOfInstances[index] = temp_s;
        }
        private void Resize_Shape(ref Shape chosen, Point nearest, Point move)
        {
            switch (chosen.Kind)
            {
                case "Line":
                    Point anchor = new Point();
                    foreach (Point index in chosen.ListOfControlPoints)
                    {
                        if (index != nearest)
                        {
                            anchor = index;
                            break;
                        }
                    }
                    double lineDistance = Cal_Resize(anchor, nearest);
                    double valueDistance = Cal_Resize(anchor, nearest);
                    double percentResize = valueDistance / lineDistance;
                    chosen.ListOfBetweenPoints.Clear();
                    chosen.ListOfControlPoints.Clear();
                    chosen.ListOfControlPoints.Add(anchor);
                    Point controlPoint = new Point((int)((double)nearest.X * percentResize), (int)((double)nearest.Y * percentResize));
                    chosen.ListOfControlPoints.Add(controlPoint);
                    break;
            }
        }
        private Point Find_Nearest_Point(List<Point> controlPoints, Point begin)
        {
            //Tinh khoang cach tu diem click tren man hinh toi tap cac control points cua hinh
            Point result = new Point();
            double minDistance = 0f;
            foreach(Point index in controlPoints)
            {
                double distance = CalDistance(index, begin);
                if(distance < minDistance || minDistance == 0f)
                {
                    result = index;
                    minDistance = distance;
                }
            }
            return result;
        }
        private double Cal_Resize(Point anchor, Point move)
        {
            
            double x = Math.Pow((anchor.X - move.X), 2);
            double y = Math.Pow((anchor.Y - move.Y), 2);
            return Math.Sqrt(x + y);
        }
    }
}