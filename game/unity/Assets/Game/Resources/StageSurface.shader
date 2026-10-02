Shader "newASTER/StageSurface"
{
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission", Color) = (0,0,0,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; float4 color : COLOR; };
            struct Output { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float4 color : COLOR; };
            fixed4 _Color;
            fixed4 _EmissionColor;
            Output vert(Input v) {
                Output o; o.position=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal); o.color=v.color; return o;
            }
            fixed4 frag(Output i) : SV_Target {
                float light=.35+.65*saturate(dot(normalize(i.normal),normalize(float3(-.4,1,-.5))));
                return fixed4(_Color.rgb*i.color.rgb*light+_EmissionColor.rgb*.35,1);
            }
            ENDCG
        }
    }
}
