// Unity Noita-Style Pixel Physics Engine
// SandRenderer.shader
Shader "PixelPhysics/SandRenderer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct PixelData
            {
                uint material;
                uint age;
                uint flags;
                uint padding;
            };

            StructuredBuffer<PixelData> _PixelBuffer;
            sampler2D _MainTex;
            int _Width;
            int _Height;

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

            fixed4 GetMaterialColor(uint mat)
            {
                switch (mat)
                {
                    case 0: return fixed4(0.043, 0.058, 0.09, 1.0); // Air
                    case 1: return fixed4(0.89, 0.725, 0.423, 1.0);  // Sand
                    case 2: return fixed4(0.23, 0.51, 0.965, 1.0);   // Water
                    case 3: return fixed4(0.39, 0.41, 0.45, 1.0);    // Gunpowder
                    case 4: return fixed4(0.97, 0.45, 0.08, 1.0);    // Fire
                    case 5: return fixed4(0.29, 0.87, 0.50, 1.0);    // Acid
                    case 6: return fixed4(0.93, 0.26, 0.26, 1.0);    // Lava
                    case 7: return fixed4(0.63, 0.38, 0.02, 1.0);    // Oil
                    case 8: return fixed4(0.47, 0.29, 0.17, 1.0);    // Wood
                    case 9: return fixed4(0.58, 0.64, 0.72, 1.0);    // Stone
                    default: return fixed4(0.0, 0.0, 0.0, 1.0);
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                int px = clamp((int)(i.uv.x * _Width), 0, _Width - 1);
                int py = clamp((int)((1.0 - i.uv.y) * _Height), 0, _Height - 1);
                int idx = py * _Width + px;
                int totalPixels = _Width * _Height;
                if (idx >= 0 && idx < totalPixels)
                {
                    PixelData p = _PixelBuffer[idx];
                    if (p.material != 0) return GetMaterialColor(p.material);
                }

                fixed4 tex = tex2D(_MainTex, i.uv);
                if (tex.a > 0.01) return tex;

                return fixed4(0.043, 0.058, 0.09, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
