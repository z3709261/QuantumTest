Shader "Unlit/UISliderScroll"
{
     Properties
    {
        _MainTex("Base Layer(RGB)", 2D) = "white" {}    // 纹理    
		_ScrollX("Base layer Scroll X Speed",Float) = 1.0   // 滚动X速度
		_ScrollY("Base layer Scroll Y Speed",Float) = 1.0   // 滚动Y速度
                                                       
        _Mutiplier("Layer Mutiplier", Float) = 1         //整体亮度
    }
        SubShader
    {
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
		Blend  SrcAlpha OneMinusSrcAlpha 
        Pass
    {
            Tags { "LightMode" = "UniversalForward" }
        CGPROGRAM
#pragma vertex vert
#pragma fragment frag


#include "UnityCG.cginc"

        struct a2v
    {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    sampler2D _MainTex;
    float4 _MainTex_ST; 
    float _ScrollX; 
    float _ScrollY; 

    v2f vert(a2v v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);

        o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex) + frac(float2 (-_ScrollX, _ScrollY) * _Time.x); 
		//o.uv = v.texcoord;
        
        return o;
    }

    fixed4 frag(v2f i) : SV_Target
    {

        fixed4 c = tex2D(_MainTex, i.uv.xy); 
		//fixed4 c = tex2D(_MainTex,i.uv - _Time.x*fixed2(11,0)* 2);
		
		return c;
    }
        ENDCG
    }
    }
        FallBack "VertexLit" 
}
