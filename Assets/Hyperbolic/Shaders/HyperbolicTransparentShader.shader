Shader "Custom/HyperbolicTransparentShader" {
  Properties{
    _MainTex("Texture", 2D) = "white" {}
    _AOTex("AO Map", 2D) = "white" {}
    _Enable("Enable Warp", Float) = 1
    _Color("Colorize", Color) = (1.0,1.0,1.0,1.0)
    _Ambient("Ambient", Float) = 0.6
    _BoundaryAO("Boundary AO", Float) = 0.9
    _SuppressAO("Suppress AO", Float) = 0.0
    _Fog("Fog", Float) = 0.0
    _KleinV("Klein Vertex", Float) = 0.4858682718
    _CamHeight("Cam Height", Float) = 0.0
  }
  SubShader{
    Tags{
      "Queue" = "Transparent"
      "HyperRenderType" = "Transparent"
      "LightMode" = "ForwardBase"
    }
    Pass{
      Cull Back
      ZTest LEqual
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha, One Zero
    
      CGPROGRAM
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
