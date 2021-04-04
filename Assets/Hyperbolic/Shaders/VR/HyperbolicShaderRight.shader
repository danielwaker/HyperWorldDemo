Shader "Custom/HyperbolicShaderRight" {
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

  //Opaque Shader
  SubShader{
    Tags{
      "Queue" = "Geometry"
      "HyperRenderType" = "Opaque"
      "LightMode" = "ForwardBase"
    }
    Pass{
      Cull Back
      ZTest LEqual
    
      CGPROGRAM
      #pragma multi_compile __ BOUNDARY_BLEND
      #pragma multi_compile __ CAFE_LIGHT PORTAL WATER WAVY PLASMA
      #pragma vertex vert
      #pragma fragment frag
      #define HYPERBOLIC 1
      #define VR_CAM 1
      #include "../HyperCore.cginc"
      ENDCG
    }
  }

  //Transparent Shader
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
      #define VR_CAM 1
      #include "../HyperCore.cginc"
      ENDCG
    }
  }

  //Overlay Shader
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
      #define VR_CAM 1
      #include "../HyperCore.cginc"
      ENDCG
    }
  }

  CustomEditor "HyperbolicEditor"
}
