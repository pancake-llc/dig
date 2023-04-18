Shader "Unlit/Brush"
{
    Properties
    {
        [NoScaleOffset]_MainTex ("Texture", 2D) = "white" {}
		_OutlineStrength ("Outline Strength", Range(0.0, 0.5)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

		Cull Off
		ZWrite Off
		ZTest Off
	
        Pass
        {
			Blend SrcAlpha DstAlpha
			BlendOp RevSub

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
			float4 _ScaleOffset;
			float _OutlineStrength;

            v2f vert (appdata v)
            {
				v2f o;
				o.vertex = v.vertex;//
				o.vertex.z = 1;
#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = float2((o.vertex.x - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
					1 - (o.vertex.y - _ScaleOffset.y) / _ScaleOffset.w * 2);
#else 
				o.vertex.xy = float2((o.vertex.x - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
					(o.vertex.y - _ScaleOffset.y) / _ScaleOffset.w * 2 - 1);
#endif
				
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {                
                return float4(1, 1, 1, 1);
            }
            ENDCG
        }

		Pass
		{
			Blend SrcAlpha DstAlpha
			BlendOp Min

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal :  NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _ScaleOffset;
			float _OutlineStrength;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.vertex.z = 1;
#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = float2((o.vertex.x + v.normal.x * _OutlineStrength - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
					1 - (o.vertex.y + v.normal.y * _OutlineStrength - _ScaleOffset.y) / _ScaleOffset.w * 2);
#else 
				o.vertex.xy = float2((o.vertex.x + v.normal.x * _OutlineStrength - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
					(o.vertex.y + v.normal.y * _OutlineStrength - _ScaleOffset.y) / _ScaleOffset.w * 2 - 1);
#endif

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return float4(1, 1, 1, 0.5);
			}
			ENDCG
		}

		Pass
		{
			Blend SrcAlpha DstAlpha
			BlendOp Add

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _ScaleOffset;
			float _OutlineStrength;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.vertex.z = 1;
#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = float2((o.vertex.x - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
					1 - (o.vertex.y - _ScaleOffset.y) / _ScaleOffset.w * 2);
#else 
				o.vertex.xy = float2((o.vertex.x - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
					(o.vertex.y - _ScaleOffset.y) / _ScaleOffset.w * 2 - 1);
#endif

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return float4(1, 1, 1, 1);
			}
			ENDCG
		}

		Pass
		{
			Blend SrcAlpha DstAlpha
			BlendOp Max

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal :  NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _ScaleOffset;
			float _OutlineStrength;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.vertex.z = 1;
#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = float2((o.vertex.x + v.normal.x * _OutlineStrength - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
					1 - (o.vertex.y + v.normal.y * _OutlineStrength - _ScaleOffset.y) / _ScaleOffset.w * 2);
#else 
				o.vertex.xy = float2((o.vertex.x + v.normal.x * _OutlineStrength - _ScaleOffset.x) / _ScaleOffset.z * 2 - 1,
									 (o.vertex.y + v.normal.y * _OutlineStrength - _ScaleOffset.y) / _ScaleOffset.w * 2 - 1);
#endif

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return float4(1, 1, 1, 0.5);
			}
			ENDCG
		}
    }
}
