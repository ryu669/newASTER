using System;
using System.Collections.Generic;
namespace NewAster.Core
{
    // Union of even/odd polygons, sampled at pixel centres in top-left coordinates.
    public static class PolygonAlphaMask
    {
        public static byte[] Rasterize(int width,int height,float[][] polygons)
        {
            if(width<1 || height<1)throw new ArgumentOutOfRangeException(nameof(width));
            if(polygons==null)throw new ArgumentNullException(nameof(polygons));
            var pixels=new byte[checked(width*height)];var crossings=new List<double>();
            foreach(var polygon in polygons){
                if(polygon==null || polygon.Length<6 || polygon.Length%2!=0)throw new ArgumentException("Polygon needs at least three XY pairs.");
                foreach(float coordinate in polygon)if(float.IsNaN(coordinate) || float.IsInfinity(coordinate))throw new ArgumentException("Non-finite polygon coordinate.");
                for(int y=0;y<height;y++){
                    double sampleY=1-(y+.5)/height;crossings.Clear();
                    for(int i=0,j=polygon.Length-2;i<polygon.Length;j=i,i+=2){
                        double ax=polygon[i],ay=polygon[i+1],bx=polygon[j],by=polygon[j+1];
                        if((ay>sampleY)!=(by>sampleY))crossings.Add(ax+(sampleY-ay)*(bx-ax)/(by-ay));
                    }
                    crossings.Sort();
                    for(int i=0;i+1<crossings.Count;i+=2){
                        // Include the left edge and exclude the right edge, matching ray casting.
                        int first=(int)Math.Max(0,Math.Min(width,Math.Ceiling(crossings[i]*width-.5)));
                        int end=(int)Math.Max(0,Math.Min(width,Math.Ceiling(crossings[i+1]*width-.5)));
                        for(int x=first;x<end;x++)pixels[y*width+x]=255;
                    }
                }
            }
            return pixels;
        }
    }
}
