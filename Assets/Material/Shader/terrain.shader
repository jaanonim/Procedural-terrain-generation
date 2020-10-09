Shader "Custom/terrain" {
	Properties {
		//test("texture",2D) = "white"{}
		//testS("Scale",float) = 1


	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		const static int maxCount = 8;
		const static float epsilon = 1E-4;

		float worldScale;

		int Count;
        float3 color[maxCount];
        float startHeight[maxCount];
		float blendStrength[maxCount];
		float colorStrength[maxCount];
		float textureScale[maxCount];

	    float minH;
		float maxH;

		sampler2D test;
		float testS;

		UNITY_DECLARE_TEX2DARRAY(baseTexture);

		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		float inversLerp(float a,float b,float value)
		{
			return saturate((value-a)/(b-a));
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxis, int textureIndex)
		{
			float3 scaledWorldPos = worldPos/scale;

			float3 xProdection = UNITY_SAMPLE_TEX2DARRAY(baseTexture,float3(scaledWorldPos.y,scaledWorldPos.z,textureIndex))*blendAxis.x;
			float3 yProdection = UNITY_SAMPLE_TEX2DARRAY(baseTexture,float3(scaledWorldPos.x,scaledWorldPos.z,textureIndex))*blendAxis.y;
			float3 zProdection = UNITY_SAMPLE_TEX2DARRAY(baseTexture,float3(scaledWorldPos.x,scaledWorldPos.y,textureIndex))*blendAxis.z;
			return xProdection+yProdection+zProdection;
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {

			float hightPercent = inversLerp(minH,maxH,IN.worldPos.y);

			float3 blendAxis = abs(IN.worldNormal);
			blendAxis /= blendAxis.x+blendAxis.y+blendAxis.z;

			for(int i=0;i<Count;i++)
			{
				float drawS = inversLerp(-blendStrength[i]/2 - epsilon,blendStrength[i]/2,hightPercent - startHeight[i]);

				float3 baseColor = color[i] * colorStrength[i];
				float3 textureColor = triplanar(IN.worldPos, textureScale[i] * worldScale, blendAxis,i) * (1-colorStrength[i]);

				o.Albedo = o.Albedo * (1-drawS) + (baseColor+textureColor) * drawS;
			}

			
			//o.Albedo=xProdection+yProdection+zProdection;
			
		}
		ENDCG
	}
	FallBack "Diffuse"
}
