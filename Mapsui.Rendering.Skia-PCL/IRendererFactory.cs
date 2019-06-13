using Mapsui.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mapsui.Rendering.Skia
{
    public interface IRendererFactory
    {
        void OnRender(IFeature feature, SkiaSharp.SKCanvas canvas, Mapsui.IViewport viewport, long currentIteration);
    }
}
