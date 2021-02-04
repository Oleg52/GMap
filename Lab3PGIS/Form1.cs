using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Npgsql;
using System;
using System.IO;
using SharpKml.Engine;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;


using System.Windows.Forms;

namespace Lab3PGIS
{
    public partial class Form1 : Form
    {
        NpgsqlConnection connection = null;
        private List<PointLatLng> points;
        private PointLatLng curr_center;
        public Form1()
        {
            InitializeComponent();
            points = new List<PointLatLng>();
            map.DragButton = MouseButtons.Left;
            map.Position = new PointLatLng(0, 0);
            map.MapProvider = GMapProviders.OpenStreetMap;
            map.MinZoom = -10;
            map.MaxZoom = 100;
            map.Zoom = 10;
            //Convert1();
        }

        private void textBox2_Click(object sender, EventArgs e)
        {

           /* var route = OpenStreetMapProvider.Instance.GetRoute(
                new PointLatLng(Convert.ToDouble(textBox1.Text), Convert.ToDouble(textBox2.Text)),
                new PointLatLng(Convert.ToDouble(textBox3.Text), Convert.ToDouble(textBox4.Text)),
                false, false, 14);
            GMapRoute r = new GMapRoute(route.Points, "My Route");
            r.Stroke.Width = 5;
            r.Stroke.Color = Color.Red;
            GMapOverlay routes = new GMapOverlay("routes");
            routes.Routes.Add(r);
            map.Overlays.Add(routes);*/


        }

        private void processQuery(PointLatLng point, bool type)
        {
            NumberFormatInfo pr = new NumberFormatInfo();
            pr.NumberDecimalSeparator = ".";
            connection = new NpgsqlConnection("Server=127.0.0.1;User Id=postgres; " +
             "Password=postgres;Database=Postgis2;");
            connection.Open();
            String cmd = "SELECT gid " +
              "FROM object_squares " +
              "where st_contains(geom, ST_GeomFromText('POINT(" + point.Lng.ToString(pr) + " " + point.Lat.ToString(pr) + ")', 4326))";
            Console.WriteLine(cmd);
            NpgsqlCommand com_gid = new NpgsqlCommand(cmd, connection);
            NpgsqlDataReader gid_rd = com_gid.ExecuteReader();
            gid_rd.Read();
            String obj_id = gid_rd[0].ToString();
            gid_rd.Close();
            NpgsqlCommand com_div = new NpgsqlCommand("SELECT divide_object("+ obj_id + ",10)", connection);
            com_div.ExecuteNonQuery();
            NpgsqlCommand obj_cent = new NpgsqlCommand("SELECT st_astext(ST_Centroid(geom)) FROM object_squares WHERE gid = " + obj_id, connection);
            NpgsqlDataReader dr1 = obj_cent.ExecuteReader();
            dr1.Read();
            String p_coord = dr1[0].ToString();
            String[] p_coordArr = p_coord.Substring(6, p_coord.Length - 7).Split(' ');
            curr_center = new PointLatLng((Convert.ToDouble(p_coordArr[1], pr)), (Convert.ToDouble(p_coordArr[0], pr)));
            obj_cent.Dispose();
            dr1.Close();
            NpgsqlCommand command = null;
            if (type)
            {
                command = new NpgsqlCommand("SELECT st_astext((dp).geom)" +
                    "FROM(SELECT 1 As edge_id, ST_DumpPoints(geom) AS dp from tmp_divided) As foo; ", connection);
            }
            else
            {
                command = new NpgsqlCommand("SELECT st_astext((dp).geom) " +
                   "FROM(SELECT ST_DumpPoints(geom) AS dp from object_squares where gid = " + obj_id.ToString() + ") As foo", connection);
            }
            NpgsqlDataReader dr = command.ExecuteReader();
            // Output rows
            while (dr.Read())
            {
                try
                {
                    String pnt = dr[0].ToString();
                    String[] pArr = pnt.Substring(6, pnt.Length - 7).Split(' ');
                    //Console.WriteLine(point);
                    points.Add(new PointLatLng((Convert.ToDouble(pArr[1], pr)) , (Convert.ToDouble(pArr[0], pr)) ));
                }
                catch (FormatException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            map.Overlays.Clear();
            map.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            points.Clear();
            points.Add(map.Overlays[0].Markers[0].Position);
            points.Add(map.Overlays[1].Markers[0].Position);
            var p = new GMapPolygon(points, "Line")
            {
                Stroke = new Pen(Color.Red,4),
                Fill = new SolidBrush(Color.Transparent)
            };
            var ps = new GMapOverlay("polygons");
            ps.Polygons.Add(p);
            map.Overlays.Add(ps);
            map.Zoom--;
            map.Zoom++;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            points.Clear();
            var obj_id1 = map.Overlays[0].Markers[0].Position;
            var obj_id2 = map.Overlays[1].Markers[0].Position;
            processQuery(obj_id1, true);
            var p = new GMapPolygon(points, "My area")
            {
                Stroke = new Pen(Color.Black, 2),
                Fill = new SolidBrush(Color.Transparent)
            };
            points.Clear();
            processQuery(obj_id2, true);
            var p1 = new GMapPolygon(points, "My area")
            {
                Stroke = new Pen(Color.Black, 2),
                Fill = new SolidBrush(Color.Transparent)
            };
            var ps = new GMapOverlay("polygons");
            ps.Polygons.Add(p);
            ps.Polygons.Add(p1);
            map.Overlays.Add(ps);
            map.Zoom--;
            map.Zoom++;
        }

        private void map_MouseClick(object sender, MouseEventArgs e)
        {
            points.Clear();
            if (e.Button == MouseButtons.Right) {
                processQuery(map.FromLocalToLatLng(e.X, e.Y),false);
                var p = new GMapPolygon(points, "My area")
                {
                    Stroke = new Pen(Color.Black, 2),
                    Fill = new SolidBrush(Color.Transparent)
                };
                var marker = new GMarkerGoogle(curr_center, GMarkerGoogleType.green);
                map.Position = curr_center;
                var ps = new GMapOverlay("polygons");
                ps.Polygons.Add(p);
                ps.Markers.Add(marker);
                map.Overlays.Add(ps);
                map.Zoom--;
                map.Zoom++;
            }
        }
    }
    }

