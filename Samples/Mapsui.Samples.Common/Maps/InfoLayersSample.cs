using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BruTile;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Samples.Common.Helpers;
using Mapsui.Styles;
using Mapsui.Utilities;

namespace Mapsui.Samples.Common.Maps
{
    public static class InfoLayersSample
    {
        private const string InfoLayerName = "Info Layer";
        private const string HoverLayerName = "Hover Layer";
        private const string PolygonLayerName = "Polygon Layer";
        private const string LineLayerName = "Line Layer";

        public static Map CreateMap()
        {
            var map = new Map();

            map.Layers.Add(OpenStreetMap.CreateTileLayer());
            map.Layers.Add(TPLayer());
            map.Layers.Add(CreateInfoLayer(map.Envelope));
            map.Layers.Add(CreateHoverLayer(map.Envelope));
            map.Layers.Add(CreatePolygonLayer());
            map.Layers.Add(CreateLineLayer());

            map.InfoLayers.Add(map.Layers.First(l => l.Name == InfoLayerName));
            map.InfoLayers.Add(map.Layers.First(l => l.Name == PolygonLayerName));
            map.InfoLayers.Add(map.Layers.First(l => l.Name == LineLayerName));
            map.HoverLayers.Add(map.Layers.First(l => l.Name == HoverLayerName));

            return map;
        }

        private static ILayer TPLayer()
        {
            var tileSourceBase = "http://116.203.124.112:8080/geoserver/gwc/service/tms/1.0.0/places:places_pulsing@EPSG:900913@png";
            var tl = new TileLayer(new HttpTileSource(new GlobalSphericalMercator(),
                    new TileRequest(tileSourceBase + "/{z}/{x}/{y}.png"), "My Map")
                )
            {
                Name = "tpl"
            };
            return tl;
        }

        private static ILayer CreatePolygonLayer()
        {
            var features = new Features { CreatePolygonFeature(), CreateMultiPolygonFeature() };
            var provider = new MemoryProvider(features);

            var layer = new MemoryLayer
            {
                Name = PolygonLayerName,
                DataSource = provider,
                Style = null
            };

            return layer;
        }

        private static ILayer CreateLineLayer()
        {
            return new MemoryLayer
            {
                Name = LineLayerName,
                DataSource = new MemoryProvider(CreateLineFeature()),
                Style = null
            };
        }

        private static Feature CreateMultiPolygonFeature()
        {
            var feature = new Feature
            {
                Geometry = CreateMultiPolygon(),
                ["Name"] = "Multipolygon 1"
            };
            feature.Styles.Add(new VectorStyle { Fill = new Brush(Color.Gray), Outline = new Pen(Color.Black) });
            return feature;
        }

        private static Feature CreatePolygonFeature()
        {
            var feature = new Feature
            {
                Geometry = CreatePolygon(),
                ["Name"] = "Polygon 1"
            };
            feature.Styles.Add(new VectorStyle());
            return feature;
        }

        private static Feature CreateLineFeature()
        {
            return new Feature
            {
                Geometry = CreateLine(),
                ["Name"] = "Line 1",
                Styles = new List<IStyle> { new VectorStyle{ Line = new Pen(Color.Violet, 6)}}
            };
        }

        private static MultiPolygon CreateMultiPolygon()
        {
            return new MultiPolygon
            {
                Polygons = new List<Polygon>
                {
                    new Polygon(new LinearRing(new[]
                    {
                        new Point(4000000, 3000000),
                        new Point(4000000, 2000000),
                        new Point(3000000, 2000000),
                        new Point(3000000, 3000000),
                        new Point(4000000, 3000000)
                    })),

                    new Polygon(new LinearRing(new[]
                    {
                        new Point(4000000, 5000000),
                        new Point(4000000, 4000000),
                        new Point(3000000, 4000000),
                        new Point(3000000, 5000000),
                        new Point(4000000, 5000000)
                    }))
                }
            };
        }

        private static Polygon CreatePolygon()
        {
            return new Polygon(new LinearRing(new[]
            {
                new Point(1000000, 1000000),
                new Point(1000000, -1000000),
                new Point(-1000000, -1000000),
                new Point(-1000000, 1000000),
                new Point(1000000, 1000000)
            }));
        }

        private static LineString CreateLine()
        {
            var offsetX = -2000000;
            var offsetY = -2000000;
            var stepSize = -2000000;

            return new LineString(new[]
            {
                new Point(offsetX + stepSize,      offsetY + stepSize),
                new Point(offsetX + stepSize * 2,  offsetY + stepSize),
                new Point(offsetX + stepSize * 2,  offsetY + stepSize * 2),
                new Point(offsetX + stepSize * 3,  offsetY + stepSize * 2),
                new Point(offsetX + stepSize * 3,  offsetY + stepSize * 3)
            });
        }

        private static ILayer CreateInfoLayer(BoundingBox envelope)
        {
            return new Layer(InfoLayerName)
            {
                DataSource = RandomPointHelper.CreateProviderWithRandomPoints(envelope, 25, 7),
                Style = CreateSymbolStyle()
            };
        }

        private static ILayer CreateHoverLayer(BoundingBox envelope)
        {
            return new Layer(HoverLayerName)
            {
                DataSource = RandomPointHelper.CreateProviderWithRandomPoints(envelope, 25, 8),
                Style = CreateHoverSymbolStyle()
            };
        }

        private static SymbolStyle CreateHoverSymbolStyle()
        {
            return new SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Brush(new Color(251, 236, 215)),
                Outline = { Color = Color.Gray, Width = 1 }
            };
        }

        private static SymbolStyle CreateSymbolStyle()
        {
            return new SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Brush(new Color(213, 234, 194)),
                Outline = { Color = Color.Gray, Width = 1 }
            };
        }
    }

    public class TileRequest : IRequest
    {
        private readonly string _urlFormatter;

        public TileRequest(string urlFormatter)
        {
            _urlFormatter = urlFormatter;
        }

        public Uri GetUri(TileInfo info)
        {
            var y = System.Math.Pow(2, Convert.ToInt32(info.Index.Level)) - info.Index.Row - 1;

            var stringBuilder = new StringBuilder(_urlFormatter);
            stringBuilder.Replace("{x}", info.Index.Col.ToString(CultureInfo.InvariantCulture));
            stringBuilder.Replace("{y}", y.ToString(CultureInfo.InvariantCulture));
            stringBuilder.Replace("{z}", info.Index.Level);

            return new Uri(stringBuilder.ToString());
        }
    }
}