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
				// 将UV坐标转换为网格坐标（0-8范围）
				// UV坐标范围是0-1，网格坐标范围是0-8（9x9网格，索引0-8）
				// 对于9x9网格，需要乘以8（gridSize - 1）
				float2 gridSizeMinusOne = _GridSize - float2(1.0, 1.0);
				float2 gridPos = IN.texcoord * gridSizeMinusOne;
				
				// 检查是否在攻击范围内（通过纹理采样）
				// 使用点过滤模式，确保精确对应到网格格子
				float attackRangeValue = tex2D(_AttackRangeTex, IN.texcoord).r;
				bool inAttackRange = attackRangeValue > 0.5;
				
				// 计算到最近钻头格子的距离
				// 搜索半径限制在revealRadius + fadeDistance范围内，优化性能
				float searchRadius = ceil(_RevealRadius + _FadeDistance);
				float minDistance = 999.0; // 初始化为大值
				
				// 遍历附近格子，找到最近的钻头格子
				// 对于9x9网格，从任意点到最远点的距离约为5.66，所以-8到8的范围足够
				for (int dx = -8; dx <= 8; dx++)
				{
					for (int dy = -8; dy <= 8; dy++)
					{
						// 检查是否在搜索半径内
						float distFromCenter = length(float2(dx, dy));
						if (distFromCenter > searchRadius)
						{
							continue;
						}
						
						// 计算邻居格子的网格坐标
						float2 neighborGridPos = gridPos + float2(dx, dy);
						
						// 检查是否在网格范围内
						if (neighborGridPos.x >= 0.0 && neighborGridPos.x < _GridSize.x &&
							neighborGridPos.y >= 0.0 && neighborGridPos.y < _GridSize.y)
						{
							// 转换为UV坐标并采样攻击范围纹理
							float2 neighborUV = neighborGridPos / gridSizeMinusOne;
							// 确保UV在有效范围内
							neighborUV = clamp(neighborUV, 0.0, 1.0);
							float neighborAttackValue = tex2D(_AttackRangeTex, neighborUV).r;
							
							// 如果这是钻头格子，计算距离
							if (neighborAttackValue > 0.5)
							{
								// 计算到该钻头格子的欧几里得距离
								float dist = length(gridPos - neighborGridPos);
								minDistance = min(minDistance, dist);
							}
						}
					}
				}
				
				// 如果没有找到任何钻头格子（minDistance仍为大值），使用最大距离
				if (minDistance > 100.0)
				{
					minDistance = _RevealRadius + _FadeDistance + 1.0;
				}
				
				// 使用最近距离来计算迷雾alpha值
				float distance = minDistance;
				float fogAlpha = 0.0;
				
				if (inAttackRange)
				{
					// 在攻击范围内，完全无迷雾
					fogAlpha = 0.0;
				}
				else if (distance <= _RevealRadius)
				{
					// 在revealRadius内，完全无迷雾
					fogAlpha = 0.0;
				}
				else if (distance >= _RevealRadius + _FadeDistance)
				{
					// 超过revealRadius + fadeDistance，完全迷雾
					fogAlpha = _MaxFogAlpha;
				}
				else
				{
					// 在渐变区域内，线性插值
					float normalizedDistance = (distance - _RevealRadius) / _FadeDistance;
					fogAlpha = lerp(0.0, _MaxFogAlpha, normalizedDistance);
				}
				
				// 组合颜色：使用迷雾颜色，alpha由计算得出
				fixed4 color = _FogColor;
				color.a = fogAlpha;
				
				// 应用UI ClipRect
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
