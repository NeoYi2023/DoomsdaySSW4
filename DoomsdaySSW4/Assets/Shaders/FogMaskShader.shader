Shader "UI/FogMask"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		// 迷雾参数
		_FogColor ("Fog Color", Color) = (0,0,0,1)
		_DrillCenter ("Drill Center", Vector) = (0.5,0.5,0,0) // 归一化到0-1范围
		_RevealRadius ("Reveal Radius", Float) = 2.0
		_FadeDistance ("Fade Distance", Float) = 3.0
		_MaxFogAlpha ("Max Fog Alpha", Range(0,1)) = 1.0
		_GridSize ("Grid Size", Vector) = (9,9,0,0) // 网格大小
		_AttackRangeTex ("Attack Range Texture", 2D) = "white" {} // 攻击范围掩码纹理（可选）
		
		// 六边形布局（由 FogMaskView 在 SetHexLayoutSource 时设置）
		_UseHexLayout ("Use Hex Layout", Float) = 0
		_HexCenterTex ("Hex Center Tex", 2D) = "black" {}
		_FogRectMin ("Fog Rect Min", Vector) = (0,0,0,0)
		_FogRectSize ("Fog Rect Size", Vector) = (1,1,0,0)
		
		// UI必需参数
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		
		_CullMode ("Cull Mode", Float) = 0
		_ColorMask ("Color Mask", Float) = 15
		_ClipRect ("Clip Rect", vector) = (-32767, -32767, 32767, 32767)

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull [_CullMode]
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			sampler2D _MainTex;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float4 _MainTex_ST;
			
			// 迷雾参数
			fixed4 _FogColor;
			float2 _DrillCenter;
			float _RevealRadius;
			float _FadeDistance;
			float _MaxFogAlpha;
			float2 _GridSize;
			sampler2D _AttackRangeTex;
			float4 _AttackRangeTex_ST;
			
			float _UseHexLayout;
			sampler2D _HexCenterTex;
			float4 _HexCenterTex_ST;
			float2 _FogRectMin;
			float2 _FogRectSize;

			// odd-r 六边形网格：(col, row) -> cube (q, r, s)
			void OffsetToCube(int col, int row, out int oq, out int or_, out int os)
			{
				oq = col - (row - (row & 1)) / 2;
				or_ = row;
				os = -oq - or_;
			}
			
			float HexDistance(int c1, int r1, int c2, int r2)
			{
				int q1, r1c, s1, q2, r2c, s2;
				OffsetToCube(c1, r1, q1, r1c, s1);
				OffsetToCube(c2, r2, q2, r2c, s2);
				return (abs(q2 - q1) + abs(r2c - r1c) + abs(s2 - s1)) * 0.5;
			}

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				
				OUT.color = v.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				float fogAlpha = 0.0;
				bool inAttackRange = false;
				float distance = _RevealRadius + _FadeDistance + 1.0;
				
				if (_UseHexLayout > 0.5)
				{
					// ——— 六边形分支：片段 local 位置 -> 最近六边形 (row,col) -> 六边形距离 ———
					float2 localPos = _FogRectMin + IN.texcoord * _FogRectSize;
					int gridW = (int)_GridSize.x;
					int gridH = (int)_GridSize.y;
					
					// 找最近六边形中心；跳过无有效 RectTransform 的格子（alpha=0 标记为无效）
					float minDistSq = 1e10;
					int myCol = -1;
					int myRow = -1;
					for (int r = 0; r < gridH; r++)
					{
						for (int c = 0; c < gridW; c++)
						{
							float2 uv = float2((float)c + 0.5, (float)r + 0.5) / float2((float)gridW, (float)gridH);
							float4 norm = tex2D(_HexCenterTex, uv);
							if (norm.a < 0.5) continue;
							float2 localCenter = _FogRectMin + float2(norm.r, norm.g) * _FogRectSize;
							float dSq = dot(localPos - localCenter, localPos - localCenter);
							if (dSq < minDistSq)
							{
								minDistSq = dSq;
								myCol = c;
								myRow = r;
							}
						}
					}
					
					if (myCol >= 0)
					{
						float2 myUV = float2((float)myCol + 0.5, (float)myRow + 0.5) / float2((float)gridW, (float)gridH);
						float attackVal = tex2D(_AttackRangeTex, myUV).r;
						inAttackRange = attackVal > 0.5;
						
						// 到最近"揭示格"的六边形距离
						float searchRadius = (int)ceil(_RevealRadius + _FadeDistance);
						float minHexDist = 999.0;
						for (int dr = -8; dr <= 8; dr++)
						{
							for (int dc = -8; dc <= 8; dc++)
							{
								int nc = myCol + dc;
								int nr = myRow + dr;
								if (nc < 0 || nc >= gridW || nr < 0 || nr >= gridH) continue;
								float hd = HexDistance(myCol, myRow, nc, nr);
								if (hd > searchRadius) continue;
								float2 nUV = float2((float)nc + 0.5, (float)nr + 0.5) / float2((float)gridW, (float)gridH);
								if (tex2D(_AttackRangeTex, nUV).r > 0.5)
									minHexDist = min(minHexDist, hd);
							}
						}
						if (minHexDist > 100.0)
							minHexDist = _RevealRadius + _FadeDistance + 1.0;
						distance = minHexDist;
					}
				}
				else
				{
					// ——— 方形分支（原有逻辑） ———
					float2 gridSizeMinusOne = _GridSize - float2(1.0, 1.0);
					float2 gridPos = IN.texcoord * gridSizeMinusOne;
					float attackRangeValue = tex2D(_AttackRangeTex, IN.texcoord).r;
					inAttackRange = attackRangeValue > 0.5;
					
					float searchRadius = ceil(_RevealRadius + _FadeDistance);
					float minDistance = 999.0;
					for (int dx = -8; dx <= 8; dx++)
					{
						for (int dy = -8; dy <= 8; dy++)
						{
							float distFromCenter = length(float2(dx, dy));
							if (distFromCenter > searchRadius) continue;
							float2 neighborGridPos = gridPos + float2(dx, dy);
							if (neighborGridPos.x >= 0.0 && neighborGridPos.x < _GridSize.x &&
								neighborGridPos.y >= 0.0 && neighborGridPos.y < _GridSize.y)
							{
								float2 neighborUV = neighborGridPos / gridSizeMinusOne;
								neighborUV = clamp(neighborUV, 0.0, 1.0);
								if (tex2D(_AttackRangeTex, neighborUV).r > 0.5)
								{
									float dist = length(gridPos - neighborGridPos);
									minDistance = min(minDistance, dist);
								}
							}
						}
					}
					if (minDistance > 100.0)
						minDistance = _RevealRadius + _FadeDistance + 1.0;
					distance = minDistance;
				}
				
				if (inAttackRange)
					fogAlpha = 0.0;
				else if (distance <= _RevealRadius)
					fogAlpha = 0.0;
				else if (distance >= _RevealRadius + _FadeDistance)
					fogAlpha = _MaxFogAlpha;
				else
					fogAlpha = lerp(0.0, _MaxFogAlpha, (distance - _RevealRadius) / _FadeDistance);
				
				fixed4 color = _FogColor;
				color.a = fogAlpha;
				
				#ifdef UNITY_UI_CLIP_RECT
					color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif
				#ifdef UNITY_UI_ALPHACLIP
					clip (color.a - 0.001);
				#endif
				return color;
			}
			ENDCG
		}
	}
}
