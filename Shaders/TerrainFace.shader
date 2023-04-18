Shader "Custom/TerrainFace"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
		_BorderColor ("Border Color", Color) = (1, 1, 1, 1)
		_MaskTex("Mask Tex", 2D) = "white" {}
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent"}        

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade


		sampler2D _MaskTex;
        sampler2D _MainTex;
		float4 _BorderColor;
		float4 _BaseColor;

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c;
			fixed4 m = tex2D(_MaskTex, IN.uv_MainTex);

			if (m.a > 0.1 && m.a < 0.9)
			{
				c = _BorderColor;
			}
			else
			{
				c = tex2D(_MainTex, IN.uv_MainTex) * _BaseColor;
				c.a = m.a;//
			}

            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
