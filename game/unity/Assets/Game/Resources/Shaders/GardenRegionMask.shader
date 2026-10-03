Shader "NewAster/GardenRegionMask"
{
    Properties { _MainTex("Source",2D)="white"{} _RegionMask("Front region",2D)="black"{} _Front("Front",Float)=0 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _RegionMask;
            float _Front;
            fixed4 frag(v2f_img input):SV_Target
            {
                fixed4 source=tex2D(_MainTex,input.uv);
                fixed mask=tex2D(_RegionMask,input.uv).a;
                source.a*=lerp(1-mask,mask,_Front);
                return source;
            }
            ENDCG
        }
    }
}
