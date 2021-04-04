#include "UnityCG.cginc"
sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _AOTex;
float _Enable;
float4 _Color;
float _Ambient;
float _BoundaryAO;
float _SuppressAO;
float _Fog;
float4x4 _HyperRot;
#define _HyperPos _HyperRot._m03_m13_m23
float4x4 _HyperTileRot;
#define _HyperTilePos _HyperTileRot._m03_m13_m23
float4x4 _HyperMapRot;
#define _HyperMapPos _HyperMapRot._m03_m13_m23
float _Proj;
float _KleinV;
float _CamHeight;
float _DebugColor;
float _TanKHeight;

struct vin {
  float4 vertex : POSITION;
  float3 normal : NORMAL;
  float4 texcoord : TEXCOORD0;
  float4 texcoord1 : TEXCOORD1;
  fixed4 color : COLOR;
  //UNITY_VERTEX_INPUT_INSTANCE_ID //TODO: GPU instancing
};

struct v2f {
  float4 pos : SV_POSITION;
  float2 uv : TEXCOORD0;
  centroid float2 uv_ao : TEXCOORD1;
  float w_dot : TEXCOORD2;
  float3 n : NORMAL;
  float3 v_abs : COLOR;
};

#ifdef HYPERBOLIC
#define K -1
#define tan_k tanh
#elif defined(EUCLIDEAN)
#define K 0
#define tan_k(x) x
#else
#define K 1
#define tan_k tan
#endif

//For H2xE and S2xE
float2 mobius_add(float2 z, float2 b) {
  float bb = K * dot(b, b);
  float zz = K * dot(z, z);
  float zb = 1 - 2 * K * dot(z, b);
  return (z*(1 + bb) + b * (zb - zz)) / (zb + bb * zz);
}

//For H3 and S3
float3 mobius_add(float3 z, float3 b) {
  float bb = K * dot(b, b);
  float zz = K * dot(z, z);
  float zb = 1 - 2 * K * dot(z, b);
  return (z*(1 + bb) + b * (zb - zz)) / (zb + bb * zz);
}

//GLSL style mod
float mod(float x, float y) {
  return x - y * floor(x / y);
}

//Gaussian sine
float gsin(float x, float b) {
  x = mod(x, 1.0) - 0.5;
  return exp(-b*b*x*x)*(0.2 + b*0.2*0.56418958);
}

float4 portal(float2 p, float sharp) {
  p = abs(p*2.0 - 1.0);
  float t = -mod(_Time.z * 0.2, 1.0);
  float2 q = log(p*2.0);
  float B = 50.0 * sharp + 1.0;
  float a0 = clamp(1.0 + B * (0.25 - max(p.x, p.y)), 0.0, 1.0);
  a0 *= a0;
  float a1 = gsin(max(q.x, q.y)*3.0 + t, 20.0*sharp);
  float a2 = gsin(max(q.x, q.y)*1.0 + t, 6.0*sharp);
  float a3 = gsin(max(q.x, q.y)*0.5 + t, 3.0*sharp);
  float a4 = gsin(max(q.x, q.y)*0.3 + t, 2.0*sharp);
  float a = a0;
  float b = a0;
  a += a1;
  a += 0.9*a2;
  b += 0.75*a3;
  b += 0.6*a4;
  b = min((a + b)*0.5, 1.0);
  a = min(a, 1.0);
  return float4(a, b, min(a + b, 1.0), 1.0);
}

float randNoise(float2 st) {
  return frac(sin(dot(st.xy,
    float2(12.9898, 78.233)))
    * 43758.5453123);
}

v2f vert(vin v) {
  //Get standard unit point
  v2f o;
  float4 w_pos = mul(unity_ObjectToWorld, v.vertex);
  float3 n = UnityObjectToWorldNormal(v.normal);

  //Compute an edge gradient
  o.v_abs = abs(w_pos.xyz);

  float w_dot = 0.0;
  if (_Enable > 0.0) {
    //Convert from Unit coordinates to Klein coordinates
    w_pos.xyz = w_pos.xyz * _KleinV;

    //Modify height of water effect (hard-coded)
#if WATER
    float3 g_pos = w_pos.xyz / (sqrt(1.0 + K * dot(w_pos.xyz, w_pos.xyz)) + 1.0);
    g_pos = mobius_add(g_pos, _HyperTilePos);
    g_pos = mul(_HyperTileRot, g_pos);
    float xi = g_pos.x * 2000.0;
    float yi = g_pos.z * 200.0;
    float dy = cos(xi - _Time.z)*0.01;
    float dx = sin(xi - _Time.z)*0.06;
    float dz = sin(yi + _Time.y)*0.12;
    dy += (dot(g_pos, g_pos) - 0.97)*6.0;
    w_pos.y += dy;
    n = normalize(float3(dx, 1.0, dz));
    o.v_abs.y = w_pos.y;
#elif WAVY
    float x_t = sin(w_pos.x*10.0) * 190.0;
    float y_t = sin(w_pos.y) * 200.0;
    float z_t = cos(w_pos.z*7.0) * 150.0;
    float3 dwave = float3(y_t - _Time.y*3.0, x_t + z_t + _Time.y*2.5, y_t + _Time.y*2.1);
    w_pos.x += sin(dwave.x)*clamp(-w_pos.y*0.01, 0.0, 0.005);
    w_pos.y += sin(dwave.y)*0.002;
    w_pos.z += sin(dwave.z)*clamp(-w_pos.y*0.01, 0.0, 0.005);
    dwave = float3(x_t + z_t + _Time.y*2.5, 0.0f, x_t - z_t + _Time.y*1.5);
    n = normalize(n + cos(dwave)*0.5);
#elif PLASMA
    float plasma_offset = randNoise(w_pos.xz + _Time.xy);
    w_pos.xyz += n * plasma_offset * 0.005f;
#endif

    //Convert from Klein coordinates to Poincaré coordinates
#if IS_2D_MAP
    w_pos.xyz /= sqrt(1.0 + K * dot(w_pos.xz, w_pos.xz)) + 1.0;
#else
    if (_TanKHeight > 0.0) {
      w_pos.y = tan_k(w_pos.y) * sqrt(1.0 + K * dot(w_pos.xz, w_pos.xz));
    }
    w_pos.xyz /= sqrt(1.0 + K * dot(w_pos.xyz, w_pos.xyz)) + 1.0;
#endif

#if IS_2D_MAP
    //Apply 2D hyper-rotation to the coordinates
    w_pos.xz = mobius_add(w_pos.xz, _HyperMapPos.xz);
    w_pos.xyz = mul(_HyperMapRot, w_pos.xyz);
    w_dot = dot(w_pos.xz, w_pos.xz);

    //Projection depends on map projection
    w_pos.xyz /= max(1.0 + _Proj * w_dot, 0.0001);
    w_pos.y *= 1 + K * w_dot;
#else
    //Apply 3D hyper-rotation to the coordinates
    w_pos.xyz = mobius_add(w_pos.xyz, _HyperPos);
    w_pos.xyz = mul(_HyperRot, w_pos.xyz);
    w_pos.xyz = mobius_add(w_pos.xyz, float3(0.0, -_CamHeight, 0.0));
#ifdef VR_CAM
    float ipd = tan_k(VR_CAM * 0.004);
    float3 eye_delta = mul(float3(ipd, 0, 0), UNITY_MATRIX_V).xyz;
    w_pos.xyz = mobius_add(w_pos.xyz, eye_delta);
#endif
    w_dot = dot(w_pos.xyz, w_pos.xyz);

    //Invert coordinates to render the far hemisphere
#if TOP_HEMISPHERE
    w_pos.xyz /= w_dot;
#endif

    //Project to Beltrami-Klein coordinates when using H3
#ifdef HYPERBOLIC
    w_pos.w *= 1.0 + w_dot;
#endif
#endif
  }

  o.w_dot = w_dot;
  o.pos = mul(UNITY_MATRIX_VP, w_pos);
#if WATER
  o.n = n;
#elif PLASMA
  o.n = mul(UNITY_MATRIX_V, mul(_HyperRot, n));
#elif PORTAL
  o.n = mul(UNITY_MATRIX_V, w_pos.xyz);
#else
  o.n = mul(_HyperTileRot, n);
#endif
  o.uv = v.texcoord1.xy*_MainTex_ST.xy + _MainTex_ST.zw;
  o.uv_ao = v.texcoord.xy;
  return o;
}

//Depth buffer trick for hypersphere
#if TOP_HEMISPHERE
void frag(v2f i, out fixed4 color : SV_Target, out float depth : SV_Depth) {
  depth = 0.5 - 0.5*LinearEyeDepth(i.pos.w);
#elif BOTTOM_HEMISPHERE
void frag(v2f i, out fixed4 color : SV_Target, out float depth : SV_Depth) {
  depth = 0.5 + 0.5*LinearEyeDepth(i.pos.w);
#else
void frag(v2f i, out fixed4 color : SV_Target) {
#endif

  //NOTE: Even though w_dot is strictly a positive value, MSAA may extrapolate it
  //to a negative number, so the max function is used here to clamp it.
  float w_dot = max(i.w_dot, 0.0);

  //Fog effect
#if IS_2D_MAP
#ifndef HYPERBOLIC
  if (w_dot * max(abs(_Proj), 0.01) > 1.0) discard;
#endif
#else
  float fog = _Fog * w_dot / (2.0 + K * w_dot);
#ifndef HYPERBOLIC
  fog *= fog;
#endif
  fog *= fog;
#endif

  //Ambient occlusion
  float ao = tex2D(_AOTex, i.uv_ao).r;
  float a1 = clamp(min(5.0*(max(i.v_abs.x, i.v_abs.z) - 0.8), 1.0 - i.v_abs.y*5.0), 0.0, 1.0);
#if BOUNDARY_BLEND
  ao = (1.0 - a1)*ao + a1 * _BoundaryAO;
#endif

  //Cafe lighting
#if CAFE_LIGHT
  float absDist = 100.0 * (abs(1.0 - i.v_abs.x) + abs(1.0 - i.v_abs.z));
  ao *= absDist / (absDist + 1.0);
#endif

  //Final color mixing
  ao = 1.0 - (1.0 - ao)*(1.0 - ao);
#if PORTAL
  float sharp = 0.5; //min(-normalize(i.n).z, 1.0 - length(i.n));
  float4 col = portal(i.uv, sharp);
#else
  float4 col = tex2D(_MainTex, i.uv);
  float diffuse = 0.5 + 0.5*dot(i.n, _WorldSpaceLightPos0.xyz);
  if (_DebugColor > 0.5) {
    col.rgb = (col.r *0.299 + col.g*0.587 + col.b*0.114);
  }
  if (_DebugColor > 1.5) {
    col.rgb = 1.0;
  }
  if (_DebugColor > 2.5) {
    diffuse = 1.0f;
  }
  col.rgb *= ao * (_SuppressAO + _Ambient + (1.0 - _Ambient)*diffuse);
#endif
#if WATER
  float wash = min(i.v_abs.y*200.0, 1.0);
  color = float4(col.rgb*_Color.rgb*wash + (1.0 - wash), 1.0);
#elif PLASMA
  color = _Color;
  color.a *= 1.0 - abs(i.n.z);
#else
  //Colorize the object
  color = col * _Color;

#ifndef IS_2D_MAP
  //Blend in the fog
  color.rgb = color.rgb*(1.0 - fog) + fog*1.0;
#endif
#endif
}
