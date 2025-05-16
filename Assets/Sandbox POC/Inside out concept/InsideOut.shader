Shader "Custom/InsideOutDot"
{
    Properties
    {
        _FrontColor ("Front Color", Color) = (0,0,0,1)
        _BackColor  ("Back Color",  Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off       // render both sides of every triangle
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _FrontColor;
            fixed4 _BackColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos         = UnityObjectToClipPos(v.vertex);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // compute view direction
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                // dot > 0 means normal points toward camera → front face
                bool isFront = dot(i.worldNormal, viewDir) > 0;
                // pick color
                return isFront ? _FrontColor  : _BackColor;
            }
            ENDCG
        }
    }
}
