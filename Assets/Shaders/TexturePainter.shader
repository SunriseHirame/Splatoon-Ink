Shader "TNTC/TexturePainter"{   


    SubShader{
        Cull Off ZWrite Off ZTest Off

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
			sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float3 _PainterPosition;
            float3 _SurfaceDirection;
            
            float _Radius;
            float _Hardness;
            float _Strength;
            float4 _PainterColor;
            float _PrepareUV;
            
            struct appdata{
                float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
                float3 normal : NORMAL;
            };

            struct v2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            float mask(float3 position, float3 center, float radius, float hardness){
                float m = distance(center, position);
                return 1 - smoothstep(radius * hardness, radius, m);    
            }

            v2f vert (appdata v){
                v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos.w = dot (_SurfaceDirection, mul(unity_ObjectToWorld, v.normal));
                o.uv = v.uv;
				float4 uv = float4(0, 0, 0, 1);
                uv.xy = float2(1, _ProjectionParams.x) * (v.uv.xy * float2( 2, 2) - float2(1, 1));
				o.vertex = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{   
                if(_PrepareUV > 0 ){
                    return float4(0, 0, 1, 1);
                }
                
                float4 col = tex2D(_MainTex, i.uv);
                float nDot = i.worldPos.w;
                if (nDot < 0.1)
                {
                    return col;
                }

                const float f = mask(i.worldPos, _PainterPosition, _Radius, _Hardness);
                const float edge = f * _Strength;

                const float t = clamp (edge * pow (nDot, 8), 0, 1);
                return lerp(col, _PainterColor, edge);
            }
            ENDCG
        }
    }
}