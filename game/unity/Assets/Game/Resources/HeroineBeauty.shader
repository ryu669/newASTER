Shader "newASTER/HeroineBeauty"
{
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        _ShadowColor ("Warm shadow", Color) = (.88,.76,.80,1)
        _Gloss ("Highlight", Range(0,1)) = .06
        _Face ("Soft face lighting", Range(0,1)) = 0
        _Outline ("Outline width", Float) = .0003
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _Outline;
            fixed4 _Color;
            float4 vert(appdata_base v) : SV_POSITION {
                float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                world+=normalize(UnityObjectToWorldNormal(v.normal))*_Outline;
                return UnityWorldToClipPos(world);
            }
            fixed4 frag() : SV_Target { clip(_Outline-.000001); return fixed4(_Color.rgb*fixed3(.43,.32,.37),1); }
            ENDCG
        }
        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; };
            struct Output { float4 position:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float4 color:COLOR; };
            fixed4 _Color,_EmissionColor,_ShadowColor;
            float _Gloss,_Face;
            Output vert(Input v) {
                Output o; o.position=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.color=v.color; return o;
            }
            fixed4 frag(Output i):SV_Target {
                float3 n=normalize(i.normal), v=normalize(_WorldSpaceCameraPos-i.world);
                float3 light=normalize(float3(.45,.8,-.5));
                float shade=smoothstep(-.35,.55,dot(n,light));
                float3 tone=lerp(_ShadowColor.rgb,float3(1,1,1),lerp(shade,.85,_Face*.45));
                float spec=pow(saturate(dot(n,normalize(light+v))),42)*_Gloss;
                float rim=pow(1-saturate(dot(n,v)),4)*.025;
                return fixed4(_Color.rgb*i.color.rgb*tone+spec+rim+_EmissionColor.rgb*.25,1);
            }
            ENDCG
        }
    }
}
