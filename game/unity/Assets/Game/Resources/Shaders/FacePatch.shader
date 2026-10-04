Shader "Hidden/NewAster/FacePatch"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _SourceRegion;
            fixed4 frag(v2f_img i) : SV_Target {
                float2 uv=_SourceRegion.xy+i.uv*_SourceRegion.zw;
                fixed4 color=tex2D(_MainTex,uv);
                float2 centered=(i.uv-.5)*2;
                float mask=1-smoothstep(.72,1,dot(centered,centered));
                color.a*=mask;
                return color;
            }
            ENDCG
        }
    }
}
