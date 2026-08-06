Shader "UI/DiagonalSplitMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Boundary ("Boundary", Float) = 0.5
        _Aspect ("Aspect", Float) = 1.77778
        _Angle ("Angle", Float) = 15
        _RectMinX ("Panel Left", Float) = 0
        _RectWidth ("Panel Width", Float) = 1
        _Side ("Side", Float) = -1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

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
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Boundary;
            float _Aspect;
            float _Angle;
            float _RectMinX;
            float _RectWidth;
            float _Side;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // The aspect correction keeps the seam angle consistent on every resolution.
                float lineX = _Boundary + (0.5 - input.uv.y) * tan(radians(_Angle)) / _Aspect;
                float screenX = _RectMinX + input.uv.x * _RectWidth;
                float signedDistance = (screenX - lineX) * _Side;
                clip(signedDistance);
                return tex2D(_MainTex, input.uv) * input.color;
            }
            ENDCG
        }
    }
}
