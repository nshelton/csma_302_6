Shader "Hidden/debug"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _velocityScale(" velocity scale", float) = 1
        _divergenceScale("divergence scale", float) = 1
        _pressureScale("pressure scale", float) = 1
        _concentrationScale("concentration scale", float) = 1
        
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _velocityScale;
            float _divergenceScale;
            float _pressureScale;
            float _concentrationScale;

            sampler2D _MainTex;

            sampler2D _divergence;
            sampler2D _fluid; //velocity, concentration
            sampler2D _pressure;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col =  (float4) 0;

                // velocity
                if ( i.uv.x < 0.5 && i.uv.y < 0.5) {
                    col.xy = _velocityScale * abs(tex2D(_fluid, i.uv * 2).xy);
                }
                if ( i.uv.x > 0.5 && i.uv.y < 0.5) {
                    col = _divergenceScale * tex2D(_divergence, frac(i.uv * 2));
                }
                if ( i.uv.x > 0.5 && i.uv.y > 0.5) {
                    col.r = _pressureScale * tex2D(_pressure, frac(i.uv * 2));
                    col.b = -_pressureScale * tex2D(_pressure, frac(i.uv * 2));
                }
                // concentration
                if ( i.uv.x < 0.5 && i.uv.y > 0.5) {
                    col = _concentrationScale * (float4) tex2D(_fluid, frac(i.uv * 2)).z;
                }

                col.rgb = col;
                return col;
            }
            ENDCG
        }
    }
}
