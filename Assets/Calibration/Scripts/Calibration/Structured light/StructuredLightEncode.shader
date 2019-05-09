Shader "StructuredLight/Encode"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			float _Border;
			float _GridX;
			float _GridY;
			int _Flip;
			fixed4 frag(v2f i) : SV_Target
			{
				if (i.uv.x <= _Border || i.uv.x >= 1- _Border || i.uv.y <= _Border || i.uv.y >= 1- _Border)
				{
					return float4(0, 0, 0, 0);
				}
				else
				{
					float2 uvScaled;
					uvScaled.x = (i.uv.x*(_GridX-1))%1;
					uvScaled.y = (i.uv.y*(_GridY-1))%1;

					
					bool c =  uvScaled.x <0.5 && uvScaled.y <0.5;
					if(_Flip == 1) c = !c;

					return float4(c, c, c, c);
					
				}

			}
			ENDCG
		}
	}
}
