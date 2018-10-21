Shader "Custom/Compute" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		Cull off

		CGPROGRAM
#pragma surface surf Standard fullforwardshadows vertex:vert
#pragma multi_compile_instancing
#pragma instancing_options procedural:setup

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		struct LineData
		{
			float3 pos0;
			float3 pos1;
			float3 pos2;
			float3 pos3;
		};

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		StructuredBuffer<float3> PositionBuffer;
		StructuredBuffer<int> VisibleBuffer;
		StructuredBuffer<LineData> LineDataBuffer;
#endif

		void setup() {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			float3 verts = PositionBuffer[unity_InstanceID];
			float x = verts.x;
			float y = verts.y;
			float z = verts.z;
			unity_ObjectToWorld._14_24_34_44 = float4(x, y, z, 1);
#endif
		}

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void vert(inout appdata_full v) {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			LineData ldata = LineDataBuffer[unity_InstanceID];
			if (v.texcoord1.x == 0.0) {
				v.vertex.xyz = ldata.pos0;
			} else if (v.texcoord1.x == 1.0) {
				v.vertex.xyz = ldata.pos1;
			} else if (v.texcoord1.x == 2.0) {
				v.vertex.xyz = ldata.pos2;
			} else if (v.texcoord1.x == 3.0) {
				v.vertex.xyz = ldata.pos3;
			}
#endif
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			if (VisibleBuffer[unity_InstanceID] == 0) {
				o.Albedo = float4(1.0,0.0,0.0,1.0);
				discard;
			}
			else
			{
				o.Albedo = c.rgb;
			}
#endif
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}