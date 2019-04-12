// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'
// Upgrade NOTE: replaced '_ProjectorClip' with 'unity_ProjectorClip'

Shader "Projector/Reproject" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_Cookie("Cookie", 2D) = "" {}
		_FalloffTex("FallOff", 2D) = "" {}
	}

		Subshader{
			Tags {"Queue" = "Transparent"}
			Pass {
				ZWrite Off
				ColorMask RGB
				Blend One DstAlpha // Traditional transparency
				Offset -1, -1

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				struct v2f {
					float4 uvShadow : TEXCOORD0;
					float4 uvFalloff : TEXCOORD1;
					UNITY_FOG_COORDS(2)
					float4 pos : SV_POSITION;
				};
				
				uniform float4x4 custom_Projector;
				float4x4 unity_Projector;
				float4x4 unity_ProjectorClip;

				v2f vert(float4 vertex : POSITION)
				{
					v2f o;
					o.pos = UnityObjectToClipPos(vertex);

					o.uvShadow = mul(custom_Projector, mul(unity_ObjectToWorld,(vertex)));
					o.uvShadow.xy = ((o.uvShadow.xy+ o.uvShadow.w)/2);
					o.uvFalloff = mul(unity_ProjectorClip, vertex);
					UNITY_TRANSFER_FOG(o,o.pos);
					return o;
				}

				fixed4 _Color;
				sampler2D _Cookie;
				sampler2D _FalloffTex;

				fixed4 frag(v2f i) : SV_Target
				{
					float4 uv = UNITY_PROJ_COORD(i.uvShadow);

					uv /= uv.w;
					fixed4 texS;
					if (uv.x >= 0 && uv.x <= 1 && uv.y >=0 && uv.y <= 1) {
						texS = tex2Dproj(_Cookie, uv);
						texS.rgb *= _Color.rgb;
						texS.a = 1.0 - texS.a;
					}
					else {
						texS = fixed4(0, 0, 0, 0);
					}

					fixed4 texF = tex2Dproj(_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
					fixed4 res = texS * texF.a;

					UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(0,0,0,0));
					return res;
				}
				ENDCG
			}
	}
}
