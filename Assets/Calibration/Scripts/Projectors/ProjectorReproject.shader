// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'
// Upgrade NOTE: replaced '_ProjectorClip' with 'unity_ProjectorClip'

Shader "Projector/Reproject" {
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_Cookie("Cookie", 2D) = "" {}
		_Depth("Depth", 2D) = "" {}
		_FalloffTex("FallOff", 2D) = "" {}
		_DepthBias("Depth Bias", Float) = 1
		_Intensity("Intensity", Float) = 1
	}

		Subshader{
			Tags {"Queue" = "Transparent"}
			Pass {
				ZWrite Off
				ColorMask RGB
				Blend DstColor DstAlpha
				//Blend One DstAlpha // Traditional transparency
				Offset -1, -1

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fog
				#include "UnityCG.cginc"

				struct v2f {
					float4 uvProjector : TEXCOORD0;
					//float4 uvFalloff : TEXCOORD1;
					UNITY_FOG_COORDS(2)
					float4 pos : SV_POSITION;
				};
				
				uniform float4x4 custom_Projector;
				//float4x4 unity_Projector;
				float4x4 unity_ProjectorClip;

				float _DepthBias;
				float _Intensity;
				v2f vert(float4 vertex : POSITION)
				{
					v2f o;
					// computes world position of the vertex
					o.pos = UnityObjectToClipPos(vertex);
					// maps from world space to the projector's space, including perspective
					o.uvProjector = mul(custom_Projector, mul(unity_ObjectToWorld,(vertex))); 
					//this maps from [-w,w] to [0,w] (not yet divided by perspective)
					o.uvProjector.xyz = ((o.uvProjector.xyz+ o.uvProjector.w)/2);
					
					// this is not used anymore, because the clip coordinates are taken from the projectors MVP matrix
					//o.uvFalloff = mul(unity_ProjectorClip, vertex);

					UNITY_TRANSFER_FOG(o,o.pos);
					return o;
				}

				fixed4 _Color;
				sampler2D _Cookie;
				sampler2D _FalloffTex;
				sampler2D _Depth;
				fixed4 frag(v2f i) : SV_Target
				{
					float4 uv = UNITY_PROJ_COORD(i.uvProjector);
					float maxDepth  = 1-tex2Dproj(_Depth, uv).x;
					uv /= uv.w;
					float currentDepth = (uv.z);
					fixed4 texS;

					if (uv.x >= 0 && uv.x <= 1 && uv.y >=0 && uv.y <= 1
						&& currentDepth- _DepthBias <= maxDepth
						)
					{
						////texS.rgb = currentDepth * _DepthBias;
						////texS.a = 0.5;
						texS = tex2Dproj(_Cookie, uv);
						texS.rgb *= _Color.rgb;
						texS.rgb *= _Intensity;
						texS.a = 1.0 - texS.a;
					}
					else {
						texS = fixed4(0, 0, 0, 0);
					}

					//fixed4 texF = tex2Dproj(_FalloffTex, UNITY_PROJ_COORD(i.uvFalloff));
					fixed4 res = texS/* * texF.a*/;

					UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(0,0,0,0));
					return res;
				}
				ENDCG
			}
	}
}
