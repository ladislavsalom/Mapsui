using Mapsui.Geometries;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mapsui.Providers
{
    public interface IFeatureCustomHitTest
    {
        bool IsHit(Point worldPosition, IFeature feature, double resolution);
    }
}
