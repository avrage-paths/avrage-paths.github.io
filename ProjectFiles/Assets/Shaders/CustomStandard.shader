Shader "Custom/CustomStandard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ImpossibleSpace("ImpossibleSpace", Range(0,1)) = 0.0
        _ImpossiblePieces("ImpossiblePieces", Range(0,100)) = 1.0
        _Opacity("Opacity", Range(0,1)) = 1.0
        _TextureScale ("Texture Scale", float) = 1
        _TriplanarBlendSharpness ("Blend Sharpness", float) = 1
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        
        // Physically based Standard lighting model, and enable shadows on all light types
        // #pragma surface surf Standard fullforwardshadows
        #pragma surface surf Lambert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        float _TextureScale;
        float _TriplanarBlendSharpness;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o)
        {
            half2 xUV = IN.worldPos.zy / _TextureScale;
            half2 yUV = IN.worldPos.xz / _TextureScale;
            half2 zUV = IN.worldPos.xy / _TextureScale;

            half3 xDiff = tex2D(_MainTex, xUV);
            half3 yDiff = tex2D(_MainTex, yUV);
            half3 zDiff = tex2D(_MainTex, zUV);

            half3 blendWeights = pow(abs(IN.worldNormal), _TriplanarBlendSharpness);
            blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);

            // Needed for tinted color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = (xDiff * blendWeights.x + yDiff * blendWeights.y + zDiff * blendWeights.z) * _Color;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}



