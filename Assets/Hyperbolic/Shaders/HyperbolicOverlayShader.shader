Shader "Custom/HyperbolicOverlayShader" {
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
      "Queue" = "Transparent+100"
      "HyperRenderType" = "Overlay"
      "LightMode" = "ForwardBase"
    }
    Pass{
      Cull Back
      Lighting Off
      ZWrite Off
      ZTest Always
      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #include "HyperCore.cginc"
      ENDCG
    }
  }
  CustomEditor "HyperbolicEditor"
}
