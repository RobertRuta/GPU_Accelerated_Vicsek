Shader "Instanced/InstancedParticleShader" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _ColorIntensity ("Color Intensity", Range(2, 10)) = 1
        _ParticleSize ("Particle Size", Range(10, 1000)) = 1
    }
    SubShader {

        Pass {

            Tags {"LightMode"="ForwardBase"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float _ColorIntensity;
            float _ParticleSize;
            bool _EnableHeading;

            struct Particle
            {
                float4 position;
                float4 velocity;
            };


        #if SHADER_TARGET >= 45
            StructuredBuffer<Particle> particleBuffer;
        #endif

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float3 ambient : TEXCOORD1;
                float3 diffuse : TEXCOORD2;
                float3 color : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                float4 position = particleBuffer[instanceID].position;
                float4 velocity = particleBuffer[instanceID].velocity;
            #else
                float4 position = 0;
                float4 velocity = 0;
            #endif
                float3 direction = normalize(velocity);
                float angle_z = atan2(direction.y, direction.x);
                float angle_y = acos(direction.z);

                // Construct rotation matrices
                float3x3 rotationMatrixY = float3x3(
                    cos(angle_y), 0, sin(angle_y),
                    0, 1, 0,
                    -sin(angle_y), 0, cos(angle_y)
                );

                float3x3 rotationMatrixZ = float3x3(
                    cos(angle_z), -sin(angle_z), 0,
                    sin(angle_z), cos(angle_z), 0,
                    0, 0, 1
                );

                float3x3 rotationMatrixZY = mul(rotationMatrixZ, rotationMatrixY);

                float3 localPosition = v.vertex.xyz * _ParticleSize;
                if (_EnableHeading)
                    localPosition = mul(rotationMatrixZY, localPosition); // Rotating to looking in direction of motion

                float3 worldPosition = position.xyz + localPosition;
                float3 worldNormal = v.normal;

                float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
                float3 diffuse = (ndotl * _LightColor0.rgb);
                float3 velocity_color = normalize(particleBuffer[instanceID].velocity.xyz)*2 - float3(1, 1, 1);
                float3 color = normalize(velocity_color) * _ColorIntensity;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                o.uv_MainTex = v.texcoord;
                o.ambient = ambient;
                o.diffuse = diffuse;
                o.color = color;
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                float3 lighting = i.diffuse * shadow + i.ambient;
                fixed4 output = fixed4(albedo.rgb * i.color * lighting, albedo.w);
                UNITY_APPLY_FOG(i.fogCoord, output);
                return output;
            }

            ENDCG
        }
    }
}