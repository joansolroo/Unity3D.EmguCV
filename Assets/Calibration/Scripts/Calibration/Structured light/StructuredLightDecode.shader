Shader "Hidden/StructuredLightDecode"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Previous("Previous", 2D) = "white" {}
		_GridX("Grid count X", Float) = 1
		_GridY("Grid count Y ", Float) = 1
		_Border("Border", Float) = 0.01
		_Flip("Flip", Int) = 0
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
			
			sampler2D _MainTex;
			sampler2D _Previous;
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col0 = tex2D(_MainTex, i.uv);
				fixed4 col1 = tex2D(_Previous, i.uv);

				return fixed4(col0.x,col1.x,0,1);
			}
			ENDCG
		}
	}
}
