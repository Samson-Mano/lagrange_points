using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Lagrange_points
{
    class lagrange_pt_store
    {
        // main parameters
        // parmaters governing lagrange points
        static double mass_ratio;
        static double mass_distance;

        double canvas_width;
        double canvas_height;
        static PointF orgin_pt;
        static PointF[] Bndry_pts = new PointF[4];
        RectangleF bounding_canvas;

        // Mesh parameters
        public global_mesh g_mesh;
        public static int mesh_width = 8;

        public lagrange_pt_store(double inpt_m_ratio, double inpt_m_dist)
        {
            // Constructor
            // Update from user input
            mass_ratio = inpt_m_ratio;
            mass_distance = inpt_m_dist;

            // create global mesh
            int screen_width_bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / (2 * mesh_width);
            int screen_height_bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / (2 * mesh_width);

            g_mesh = new global_mesh(screen_width_bounds, screen_height_bounds);

            // Solve the Lagrange point
            // http://orca.phys.uvic.ca/~tatum/classmechs.html

            g_mesh.solve_lagrange(mass_ratio, inpt_m_dist);
        }

        public void paint_all(ref Graphics gr0, double main_pic_width, double main_pic_height)
        {
            // main parameters
            canvas_width = main_pic_width;
            canvas_height = main_pic_height;

            // Set orgin points (Assuming the graphics is translate transformed to mid point of the canvas)
            orgin_pt = new PointF(0, 0);

            // Set Boundary points
            int x_margin = 20;
            int y_margin = 20;
            Bndry_pts = new PointF[4] { new PointF { X = -1*(float)((canvas_width*0.5)- x_margin), Y = -1*(float)((canvas_height*0.5)-y_margin) },
                                        new PointF { X = (float)((canvas_width*0.5)- x_margin), Y = -1*(float)((canvas_height*0.5)-y_margin) },
                                        new PointF { X = (float)((canvas_width*0.5)- x_margin), Y = (float)((canvas_height*0.5)-y_margin) },
                                        new PointF { X = -1*(float)((canvas_width*0.5)- x_margin), Y = (float)((canvas_height*0.5)-y_margin) } };

            // Bounding rectangle
            bounding_canvas = new RectangleF(new PointF
            {
                X = -1 * (float)((canvas_width * 0.5) - x_margin),
                Y = -1 * (float)((canvas_height * 0.5) - y_margin)
            },
                new SizeF { Width = (float)(canvas_width - 2 * x_margin), Height = (float)(canvas_height - 2 * y_margin) });


            // Paint mesh
            g_mesh.paint_mesh(ref gr0, ref bounding_canvas);

            // Paint background
            paint_background(ref gr0);

        }


        public class global_mesh
        {
            List<rhombus_mesh> all_mesh = new List<rhombus_mesh>();
            public static RectangleF bound_box;

            static double mass_ratio;
            static double mass_distance;

            public static double g_max_w, g_min_w;

            public global_mesh(int x_grid_size, int y_grid_size)
            {
                all_mesh = new List<rhombus_mesh>();


                int i_x, i_y;
                double alternate_control = 0;

                for (i_y = 0; i_y < y_grid_size; i_y++)
                {
                    for (i_x = 0; i_x < x_grid_size; i_x++)
                    {
                        // + ive x direction
                        all_mesh.Add(new rhombus_mesh((i_x * mesh_width) + (mesh_width * alternate_control), i_y * mesh_width, mesh_width));
                        // - ive x direction
                        all_mesh.Add(new rhombus_mesh(((-1 - i_x) * mesh_width) + (mesh_width * alternate_control), i_y * mesh_width, mesh_width));
                    }

                    alternate_control = alternate_control == 0.0 ? 0.5 : 0.0;

                    for (i_x = 0; i_x < x_grid_size; i_x++)
                    {
                        // + ive y direction
                        all_mesh.Add(new rhombus_mesh((i_x * mesh_width) + (mesh_width * alternate_control), (-1 - i_y) * mesh_width, mesh_width));
                        // - ive y direction
                        all_mesh.Add(new rhombus_mesh(((-1 - i_x) * mesh_width) + (mesh_width * alternate_control), (-1 - i_y) * mesh_width, mesh_width));
                    }
                }
            }

            public void solve_lagrange(double i_mass_ratio, double i_scale_dist)
            {
                mass_ratio = i_mass_ratio;
                mass_distance = i_scale_dist;

                for (int i = 0; i < all_mesh.Count; i++)
                {
                    all_mesh[i].solve_mesh();
                }

                update_maximum_vals();
            }

            public void update_maximum_vals()
            {
                // reset the contour lines
                int i, j;
                for (i = 0; i < all_mesh.Count; i++)
                {
                    all_mesh[i].reset_contour_lines();
                }

                // Find the max min
                g_max_w = double.MinValue;
                foreach (rhombus_mesh r_m in all_mesh)
                {
                    if (r_m.upper_tri_inside_bounding_box() == true)
                    {
                        g_max_w = Math.Max(r_m.max_Wu, g_max_w);
                    }
                    if (r_m.lower_tri_inside_bounding_box() == true)
                    {
                        g_max_w = Math.Max(r_m.max_Wl, g_max_w);
                    }
                }

                // Find z-val at the edge of the boundary
                g_min_w = v_potential_1(Bndry_pts[0].X, orgin_pt.Y);

                int n_cntr_level = 100;

                double z_interval = ((g_max_w - g_min_w) / n_cntr_level);

                for (i = 1; i < n_cntr_level; i++)
                {
                    double z_val = g_min_w + (i * z_interval);

                    for (j = 0; j < all_mesh.Count; j++)
                    {
                        // Check whether z_val lies inside the upper triangle
                        if (z_val < all_mesh[j].max_Wu && z_val > all_mesh[j].min_Wu)
                        {
                            // Add the z_val contour lines
                            all_mesh[j].add_upper_triangle_contour_line(z_val, g_max_w, g_min_w);
                        }

                        // Check whether z_val lies inside the lowerer triangle
                        if (z_val < all_mesh[j].max_Wl && z_val > all_mesh[j].min_Wl)
                        {
                            // Add the z_val contour lines
                            all_mesh[j].add_lower_triangle_contour_lines(z_val, g_max_w, g_min_w);
                        }
                    }
                }

                // Set Contour colors
                for (i = 0; i < all_mesh.Count; i++)
                {
                    all_mesh[i].set_contour_heatmap();
                }
            }

            public void paint_mesh(ref Graphics gr0, ref RectangleF canvas_bounding_box)
            {
                Graphics gr1 = gr0;
                bound_box = canvas_bounding_box;
                foreach (rhombus_mesh r_m in all_mesh)
                {
                    r_m.paint_triagnle_mesh(ref gr1);
                }
            }


            public static double v_potential_1(double x, double y)
            {
                x = x / mass_distance;
                y = y / mass_distance;

                double factor1 = (1 - mass_ratio) / Math.Sqrt(Math.Pow((x - mass_ratio), 2.0f) + Math.Pow(y, 2.0f));
                double factor2 = mass_ratio / Math.Sqrt(Math.Pow((x + 1 - mass_ratio), 2.0f) + Math.Pow(y, 2.0f));
                double factor3 = 0.5 * (Math.Pow(x, 2) + Math.Pow(y, 2));

                return (-factor1 - factor2 - factor3);

            }

            public class rhombus_mesh
            {

                //          4
                //        /   \
                //       /     \
                //      1-------3
                //       \     /
                //        \   /
                //          2 

                PointF pt1;
                PointF pt2;
                PointF pt3;
                PointF pt4;
                // PointF top_midpt; // 1 - 3 - 4
                //  PointF bot_midpt; // 1 - 2 - 3
                double w1, w2, w3, w4;
                public double max_Wu, min_Wu, max_Wl, min_Wl;

                List<triangle_contour_line_store> u_tri_clines = new List<triangle_contour_line_store>();
                List<triangle_contour_line_store> l_tri_clines = new List<triangle_contour_line_store>();


                // Color for hear map
                //GraphicsPath path = new GraphicsPath();

                GraphicsPath u_tri_path;
                PathGradientBrush utri_pthGrBrush;// = new PathGradientBrush(path);

                GraphicsPath l_tri_path;
                PathGradientBrush ltri_pthGrBrush; //= new PathGradientBrush(path);

                public class triangle_contour_line_store
                {
                    PointF spt;
                    PointF ept;
                    Color cline_color;

                    public triangle_contour_line_store(PointF pt1, PointF pt2, PointF pt3, double w1, double w2, double w3, double z_val, Color z_color)
                    {
                        cline_color = z_color;

                        // Find the range of z_vals
                        if (((w1 - z_val) * (w2 - z_val)) < 0)
                        {
                            if (((w2 - z_val) * (w3 - z_val)) < 0)
                            {
                                // 1 & 3 with 2 as common point
                                // Find start point
                                spt = contour_linear_interpolation(w2, w1, z_val, pt2, pt1);
                                // Find end point
                                ept = contour_linear_interpolation(w2, w3, z_val, pt2, pt3);
                            }
                            else if (((w1 - z_val) * (w3 - z_val)) < 0)
                            {
                                // 2 & 3 with 1 as common point
                                // Find start point
                                spt = contour_linear_interpolation(w1, w2, z_val, pt1, pt2);
                                // Find end point
                                ept = contour_linear_interpolation(w1, w3, z_val, pt1, pt3);
                            }
                        }
                        else if (((w2 - z_val) * (w3 - z_val)) < 0)
                        {
                            if (((w1 - z_val) * (w3 - z_val)) < 0)
                            {
                                // 1 & 2 with 3 as common point
                                // Find start point
                                spt = contour_linear_interpolation(w3, w1, z_val, pt3, pt1);
                                // Find end point
                                ept = contour_linear_interpolation(w3, w2, z_val, pt3, pt2);
                            }
                        }
                    }

                    public void paint_contour_line(ref Graphics gr0)
                    {
                        gr0.DrawLine(new Pen(cline_color, 2), spt, ept);
                    }

                    private PointF contour_linear_interpolation(double w1, double w2, double z_val, PointF pt1, PointF pt2)
                    {
                        double z_slope;
                        double ptx, pty;

                        if ((w1 - z_val) > 0)
                        {
                            z_slope = (w1 - z_val) / (w1 - w2);

                            ptx = (pt1.X * (1 - z_slope)) + (pt2.X * z_slope);
                            pty = (pt1.Y * (1 - z_slope)) + (pt2.Y * z_slope);
                        }
                        else
                        {
                            z_slope = (w2 - z_val) / (w2 - w1);

                            ptx = (pt2.X * (1 - z_slope)) + (pt1.X * z_slope);
                            pty = (pt2.Y * (1 - z_slope)) + (pt1.Y * z_slope);
                        }

                        return new PointF((float)ptx, (float)pty);
                    }
                }

                public void reset_contour_lines()
                {
                    // reset the contour lines
                    u_tri_clines = new List<triangle_contour_line_store>();
                    l_tri_clines = new List<triangle_contour_line_store>();
                }

                public void set_contour_heatmap()
                {
                    // Set upper triangle heat map colors
                    if (upper_tri_inside_bounding_box() == true)
                    {
                        // Paint top triangle mesh
                        // gr0.DrawLines(new Pen(Color.Black, 1), new PointF[] { pt1, pt3, pt4, pt1 });
                        double z_lvl1, z_lvl2, z_lvl3;
                        z_lvl1 = ((w1 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w1 - g_min_w) / (g_max_w - g_min_w));
                        z_lvl2 = ((w3 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w3 - g_min_w) / (g_max_w - g_min_w));
                        z_lvl3 = ((w4 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w4 - g_min_w) / (g_max_w - g_min_w));

                        Color z_color1, z_color2, z_color3;
                        z_color1 = HSLToRGB(120, (1 - z_lvl1) * 240, 1, 0.35);
                        z_color2 = HSLToRGB(120, (1 - z_lvl2) * 240, 1, 0.35);
                        z_color3 = HSLToRGB(120, (1 - z_lvl3) * 240, 1, 0.35);

                        u_tri_path = new GraphicsPath();
                        u_tri_path.AddLines(new PointF[] { pt1, pt3, pt4 });
                        utri_pthGrBrush = new PathGradientBrush(u_tri_path);

                        // Contour heat map
                        int R0 = ((z_color1.R + z_color2.R + z_color3.R) / 3);
                        int G0 = ((z_color1.G + z_color2.G + z_color3.G) / 3);
                        int B0 = ((z_color1.B + z_color2.B + z_color3.B) / 3);

                        utri_pthGrBrush.CenterColor = Color.FromArgb(120, R0, G0, B0);
                        utri_pthGrBrush.SurroundColors = new Color[] { z_color1, z_color2, z_color3 };
                    }


                    // Set lower triangle heat map colors
                    if (lower_tri_inside_bounding_box() == true)
                    {
                        // Paint bot triangle mesh
                        // gr0.DrawLines(new Pen(Color.Black, 1), new PointF[] { pt1, pt2, pt3, pt1 });
                        double z_lvl1, z_lvl2, z_lvl3;
                        z_lvl1 = ((w1 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w1 - g_min_w) / (g_max_w - g_min_w));
                        z_lvl2 = ((w2 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w2 - g_min_w) / (g_max_w - g_min_w));
                        z_lvl3 = ((w3 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w3 - g_min_w) / (g_max_w - g_min_w));

                        Color z_color1, z_color2, z_color3;
                        z_color1 = HSLToRGB(120, (1 - z_lvl1) * 240, 1, 0.35);
                        z_color2 = HSLToRGB(120, (1 - z_lvl2) * 240, 1, 0.35);
                        z_color3 = HSLToRGB(120, (1 - z_lvl3) * 240, 1, 0.35);

                        l_tri_path = new GraphicsPath();
                        l_tri_path.AddLines(new PointF[] { pt1, pt2, pt3 });
                        ltri_pthGrBrush = new PathGradientBrush(l_tri_path);
                        // Contour heat map
                        int R0 = (int)(((z_color1.R) + (z_color2.R) + (z_color3.R)) / 3);
                        int G0 = (int)(((z_color1.G) + (z_color2.G) + (z_color3.G)) / 3);
                        int B0 = (int)(((z_color1.B) + (z_color2.B) + (z_color3.B)) / 3);

                        ltri_pthGrBrush.CenterColor = Color.FromArgb(120, R0, G0, B0);
                        ltri_pthGrBrush.SurroundColors = new Color[] { z_color1, z_color2, z_color3 };
                    }
                }

                public void add_upper_triangle_contour_line(double z_val, double g_max_w, double g_min_w)
                {
                    // Add upper triangle contour lines
                    double z_lvl;
                    z_lvl = (z_val - g_min_w) / (g_max_w - g_min_w);

                    Color z_color;
                    z_color = HSLToRGB(120, (1 - z_lvl) * 240, 1, 0.35);

                    u_tri_clines.Add(new triangle_contour_line_store(pt1, pt3, pt4, w1, w3, w4, z_val, z_color));
                }

                public void add_lower_triangle_contour_lines(double z_val, double g_max_w, double g_min_w)
                {
                    // add lower triangle contour lines
                    double z_lvl;
                    z_lvl = (z_val - g_min_w) / (g_max_w - g_min_w);

                    Color z_color;
                    z_color = HSLToRGB(120, (1 - z_lvl) * 240, 1, 0.35);

                    l_tri_clines.Add(new triangle_contour_line_store(pt1, pt2, pt3, w1, w2, w3, z_val, z_color));
                }

                public rhombus_mesh(double x, double y, double m_size)
                {
                    pt1 = new PointF((float)x, (float)y);
                    pt2 = new PointF((float)(x + (m_size * 0.5)), (float)(y - m_size));
                    pt3 = new PointF((float)(x + m_size), (float)y);
                    pt4 = new PointF((float)(x + (m_size * 0.5)), (float)(y + m_size));
                }

                public void solve_mesh()
                {
                    // Lagrange potential
                    w1 = v_potential_1(pt1.X, pt1.Y);
                    w2 = v_potential_1(pt2.X, pt2.Y);
                    w3 = v_potential_1(pt3.X, pt3.Y);
                    w4 = v_potential_1(pt4.X, pt4.Y);

                    // Max & min of this mesh upper triangle
                    max_Wu = Math.Max(Math.Max(w1, w3), w4);
                    min_Wu = Math.Min(Math.Min(w1, w3), w4);

                    // Max & min of this mesh lower triangle
                    max_Wl = Math.Max(Math.Max(w1, w2), w3);
                    min_Wl = Math.Min(Math.Min(w1, w2), w3);
                }

                public bool upper_tri_inside_bounding_box()
                {
                    return (bound_box.Contains(pt1) == true ||
                        bound_box.Contains(pt3) == true ||
                        bound_box.Contains(pt4) == true);
                }

                public bool lower_tri_inside_bounding_box()
                {
                    return (bound_box.Contains(pt1) == true ||
                        bound_box.Contains(pt2) == true ||
                        bound_box.Contains(pt3) == true);
                }


                public void paint_triagnle_mesh(ref Graphics gr0)
                {
                    // check whether mesh lies inside bounding box
                    if (upper_tri_inside_bounding_box() == true && utri_pthGrBrush !=null)
                    {
                        // Paint top triangle mesh
                        // gr0.DrawLines(new Pen(Color.Black, 1), new PointF[] { pt1, pt3, pt4, pt1 });

                        gr0.SmoothingMode = SmoothingMode.Default;
                        gr0.FillPolygon(utri_pthGrBrush, new PointF[] { pt1, pt3, pt4 });
                        gr0.SmoothingMode = SmoothingMode.AntiAlias;

                        // Paint contour lines
                        foreach (triangle_contour_line_store cline in u_tri_clines)
                        {
                            cline.paint_contour_line(ref gr0);
                        }
                    }

                    if (lower_tri_inside_bounding_box() == true && ltri_pthGrBrush !=null)
                    {
                        // Paint bot triangle mesh
                        // gr0.DrawLines(new Pen(Color.Black, 1), new PointF[] { pt1, pt2, pt3, pt1 });

                        gr0.SmoothingMode = SmoothingMode.Default;
                        gr0.FillPolygon(ltri_pthGrBrush, new PointF[] { pt1, pt2, pt3 });
                        gr0.SmoothingMode = SmoothingMode.AntiAlias;
                        // Paint contour lines
                        foreach (triangle_contour_line_store cline in l_tri_clines)
                        {
                            cline.paint_contour_line(ref gr0);
                        }

                    }
                }

                private void paint_upper_triangle_mesh(ref Graphics gr0)
                {
                    // check whether mesh lies inside bounding box
                    if (upper_tri_inside_bounding_box() == true)
                    {
                        // Paint top triangle mesh
                        // gr0.DrawLines(new Pen(Color.Black, 1), new PointF[] { pt1, pt3, pt4, pt1 });

                        if (max_Wu > g_min_w)
                        {
                            // fill contour color triangle mesh
                            double z_lvl1, z_lvl2, z_lvl3;
                            z_lvl1 = ((w1 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w1 - g_min_w) / (g_max_w - g_min_w));
                            z_lvl2 = ((w3 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w3 - g_min_w) / (g_max_w - g_min_w));
                            z_lvl3 = ((w4 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w4 - g_min_w) / (g_max_w - g_min_w));

                            Color z_color1, z_color2, z_color3;
                            z_color1 = HSLToRGB(120, (1 - z_lvl1) * 240, 1, 0.35);
                            z_color2 = HSLToRGB(120, (1 - z_lvl2) * 240, 1, 0.35);
                            z_color3 = HSLToRGB(120, (1 - z_lvl3) * 240, 1, 0.35);

                            gr0.SmoothingMode = SmoothingMode.Default;
                            using (GraphicsPath path = new GraphicsPath())
                            {
                                path.AddLines(new PointF[] { pt1, pt3, pt4 });

                                using (PathGradientBrush pthGrBrush = new PathGradientBrush(path))
                                {
                                    // Contour heat map
                                    int R0 = ((z_color1.R + z_color2.R + z_color3.R) / 3);
                                    int G0 = ((z_color1.G + z_color2.G + z_color3.G) / 3);
                                    int B0 = ((z_color1.B + z_color2.B + z_color3.B) / 3);

                                    pthGrBrush.CenterColor = Color.FromArgb(120, R0, G0, B0);
                                    pthGrBrush.SurroundColors = new Color[] { z_color1, z_color2, z_color3 };

                                    gr0.FillPolygon(pthGrBrush, new PointF[] { pt1, pt3, pt4 });
                                }
                            }
                            gr0.SmoothingMode = SmoothingMode.AntiAlias;
                            // Paint contour lines
                            foreach (triangle_contour_line_store cline in u_tri_clines)
                            {
                                cline.paint_contour_line(ref gr0);
                            }
                        }
                        else
                        {
                            // Paint blanks
                            gr0.SmoothingMode = SmoothingMode.Default;
                            gr0.FillPolygon(new SolidBrush(Color.FromArgb(145, 135, 219)), new PointF[] { pt1, pt3, pt4 });
                            gr0.SmoothingMode = SmoothingMode.AntiAlias;
                        }
                    }
                }

                private void paint_lower_triangle_mesh(ref Graphics gr0)
                {
                    if (lower_tri_inside_bounding_box() == true)
                    {
                        // Paint bot triangle mesh
                        // gr0.DrawLines(new Pen(Color.Black, 1), new PointF[] { pt1, pt2, pt3, pt1 });

                        if (max_Wl > g_min_w)
                        {
                            // fill contour color triangle mesh
                            double z_lvl1, z_lvl2, z_lvl3;
                            z_lvl1 = ((w1 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w1 - g_min_w) / (g_max_w - g_min_w));
                            z_lvl2 = ((w2 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w2 - g_min_w) / (g_max_w - g_min_w));
                            z_lvl3 = ((w3 - g_min_w) / (g_max_w - g_min_w)) < 0 ? 0 : ((w3 - g_min_w) / (g_max_w - g_min_w));

                            Color z_color1, z_color2, z_color3;
                            z_color1 = HSLToRGB(120, (1 - z_lvl1) * 240, 1, 0.35);
                            z_color2 = HSLToRGB(120, (1 - z_lvl2) * 240, 1, 0.35);
                            z_color3 = HSLToRGB(120, (1 - z_lvl3) * 240, 1, 0.35);

                            gr0.SmoothingMode = SmoothingMode.Default;
                            using (GraphicsPath path = new GraphicsPath())
                            {
                                path.AddLines(new PointF[] { pt1, pt2, pt3 });

                                using (PathGradientBrush pthGrBrush = new PathGradientBrush(path))
                                {
                                    // Contour heat map
                                    int R0 = (int)(((z_color1.R) + (z_color2.R) + (z_color3.R)) / 3);
                                    int G0 = (int)(((z_color1.G) + (z_color2.G) + (z_color3.G)) / 3);
                                    int B0 = (int)(((z_color1.B) + (z_color2.B) + (z_color3.B)) / 3);

                                    pthGrBrush.CenterColor = Color.FromArgb(120, R0, G0, B0);
                                    pthGrBrush.SurroundColors = new Color[] { z_color1, z_color2, z_color3 };

                                    gr0.FillPolygon(pthGrBrush, new PointF[] { pt1, pt2, pt3 });
                                }
                            }
                            gr0.SmoothingMode = SmoothingMode.AntiAlias;
                            // Paint contour lines
                            foreach (triangle_contour_line_store cline in l_tri_clines)
                            {
                                cline.paint_contour_line(ref gr0);
                            }
                        }
                        else
                        {
                            // Paint blanks
                            gr0.SmoothingMode = SmoothingMode.Default;
                            Color clr1 = Color.FromArgb(145, 135, 219);
                            gr0.FillPolygon(new SolidBrush(Color.FromArgb(145, 135, 219)), new PointF[] { pt1, pt2, pt3 });
                            gr0.SmoothingMode = SmoothingMode.AntiAlias;
                        }
                    }
                }
            }
        }

        private void paint_background(ref Graphics gr0)
        {
            // paint orgin
            gr0.DrawLine(new Pen(Brushes.Black, 1), rotate_point(orgin_pt.X, orgin_pt.Y, 6, 0, 0), rotate_point(orgin_pt.X, orgin_pt.Y, -6, 0, 0));
            gr0.DrawLine(new Pen(Brushes.Black, 1), rotate_point(orgin_pt.X, orgin_pt.Y, 0, 6, 0), rotate_point(orgin_pt.X, orgin_pt.Y, 0, -6, 0));

            // Draw mass Location
            double mass_dist1 = mass_distance * (1 - mass_ratio);
            double mass_dist2 = mass_distance * mass_ratio;

            gr0.DrawLine(new Pen(Brushes.Black, 1), rotate_point(orgin_pt.X - mass_dist1, orgin_pt.Y, 6, 0, 45), rotate_point(orgin_pt.X - mass_dist1, orgin_pt.Y, -6, 0, 45));
            gr0.DrawLine(new Pen(Brushes.Black, 1), rotate_point(orgin_pt.X - mass_dist1, orgin_pt.Y, 0, 6, 45), rotate_point(orgin_pt.X - mass_dist1, orgin_pt.Y, 0, -6, 45));

            gr0.DrawLine(new Pen(Brushes.Black, 1), rotate_point(orgin_pt.X + mass_dist2, orgin_pt.Y, 6, 0, 45), rotate_point(orgin_pt.X + mass_dist2, orgin_pt.Y, -6, 0, 45));
            gr0.DrawLine(new Pen(Brushes.Black, 1), rotate_point(orgin_pt.X + mass_dist2, orgin_pt.Y, 0, 6, 45), rotate_point(orgin_pt.X + mass_dist2, orgin_pt.Y, 0, -6, 45));

            // Draw Lagrange points
            //double L1_x = orgin_pt.X  + (mass_distance * (1 - Math.Pow((mass_ratio / 3.0f), (1.0f / 3.0f))));
            //paint_lagrange_points(ref gr0, "L1", L1_x, orgin_pt.Y);

            //double L2_x = orgin_pt.X  + (mass_distance * (1 + Math.Pow((mass_ratio / 3.0f), (1.0f / 3.0f))));
            //paint_lagrange_points(ref gr0, "L2", L2_x, orgin_pt.Y);

            //double L3_x = orgin_pt.X  - (mass_distance * (1 + ((5.0f * mass_ratio) / 12.0f)));
            //paint_lagrange_points(ref gr0, "L3", L3_x, orgin_pt.Y);


            // Lagrange points L4 & L5
            double L4_x = orgin_pt.X - ((mass_distance / 2.0f) * (1 - (2 * mass_ratio)));
            double L4_y = orgin_pt.Y - ((Math.Sqrt(3) / 2.0f) * mass_distance);
            paint_lagrange_points(ref gr0, "L4", L4_x, L4_y);

            double L5_y = orgin_pt.Y + ((Math.Sqrt(3) / 2.0f) * mass_distance);
            paint_lagrange_points(ref gr0, "L5", L4_x, L5_y);

            // Paint white background outside canvas
            RectangleF main_pic_rectangle = new RectangleF((float)(-1 * canvas_width * 0.5), (float)(-1 * canvas_height * 0.5), (float)canvas_width, (float)canvas_height);

            gr0.FillRectangle(Brushes.White, new RectangleF(main_pic_rectangle.X, main_pic_rectangle.Y, main_pic_rectangle.Width, (float)((main_pic_rectangle.Height - bounding_canvas.Height) * 0.5)));
            gr0.FillRectangle(Brushes.White, new RectangleF(main_pic_rectangle.X, main_pic_rectangle.Y, (float)((main_pic_rectangle.Width - bounding_canvas.Width) * 0.5), main_pic_rectangle.Height));

            gr0.FillRectangle(Brushes.White, new RectangleF(bounding_canvas.X, bounding_canvas.Y + bounding_canvas.Height, bounding_canvas.Width, (float)((main_pic_rectangle.Height - bounding_canvas.Height) * 0.5)));
            gr0.FillRectangle(Brushes.White, new RectangleF(bounding_canvas.X + bounding_canvas.Width, bounding_canvas.Y, (float)((main_pic_rectangle.Width - bounding_canvas.Width) * 0.5), main_pic_rectangle.Height));

            // paint border
            gr0.DrawLine(new Pen(Brushes.Black, 1), Bndry_pts[0], Bndry_pts[1]);
            gr0.DrawLine(new Pen(Brushes.Black, 1), Bndry_pts[1], Bndry_pts[2]);
            gr0.DrawLine(new Pen(Brushes.Black, 1), Bndry_pts[2], Bndry_pts[3]);
            gr0.DrawLine(new Pen(Brushes.Black, 1), Bndry_pts[3], Bndry_pts[0]);
        }

        private void paint_lagrange_points(ref Graphics gr0, string Lpt, double x, double y)
        {
            gr0.FillEllipse(Brushes.Red, (float)(x - 3), (float)(y - 3), 6, 6);

            // Rotate text to pain in correct orientation
            GraphicsState transState = gr0.Save();
            gr0.MultiplyTransform(new Matrix(1, 0, 0, -1, 0, 0));
            gr0.DrawString(Lpt, new Font("Verdana", 12), Brushes.Red, (float)(x + 3), (float)(y - 3));

            gr0.Restore(transState);
        }

        // Supporting functions
        public static PointF rotate_point(double about_x, double about_y, double x1, double y1, int rotation_angle)
        {
            // Rotation angle to radian
            double rotation_rad = rotation_angle * (Math.PI / 180);

            // Rotate about orgin
            double rot_x, rot_y;

            rot_x = (x1 * Math.Cos(rotation_rad)) - (y1 * Math.Sin(rotation_rad));
            rot_y = (x1 * Math.Sin(rotation_rad)) + (y1 * Math.Cos(rotation_rad));

            // Translate to about point
            return new PointF((float)(about_x + rot_x), (float)(about_y + rot_y));
        }

        #region "HSL to RGB Fundamental code -Not by Me"
        //---- The below code is from https://www.programmingalgorithms.com/algorithm/hsl-to-rgb?lang=VB.Net
        //0    : blue   (hsl(240, 100%, 50%))
        //0.25 : cyan   (hsl(180, 100%, 50%))
        //0.5  : green  (hsl(120, 100%, 50%))
        //0.75 : yellow (hsl(60, 100%, 50%))
        //1    : red    (hsl(0, 100%, 50%))
        public static Color HSLToRGB(int alpha_i, double hsl_H, double hsl_S, double hsl_L)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;


            if (hsl_S == 0)
            {
                r = g = b = (byte)(hsl_L * 255);
            }
            else
            {
                double v1, v2;
                double hue = hsl_H / 360;

                v2 = (hsl_L < 0.5) ? (hsl_L * (1 + hsl_S)) : ((hsl_L + hsl_S) - (hsl_L * hsl_S));
                v1 = 2 * hsl_L - v2;

                r = (byte)(255 * HueToRGB(v1, v2, hue + (1.0f / 3)));
                g = (byte)(255 * HueToRGB(v1, v2, hue));
                b = (byte)(255 * HueToRGB(v1, v2, hue - (1.0f / 3)));
            }

            return Color.FromArgb(alpha_i, r, g, b);
        }


        private static double HueToRGB(double v1, double v2, double vH)
        {
            if (vH < 0)
                vH += 1;

            if (vH > 1)
                vH -= 1;

            if ((6 * vH) < 1)
                return (v1 + (v2 - v1) * 6 * vH);

            if ((2 * vH) < 1)
                return v2;

            if ((3 * vH) < 2)
                return (v1 + (v2 - v1) * ((2.0f / 3) - vH) * 6);

            return v1;
        }
        #endregion

    }
}
