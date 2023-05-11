 Shader "Unlit/heatmap" {
      Properties {
          _TileX ("Tile X", Float) = 1.0
          _TileY ("Tile Y", Float) = 1.0
          _OffsetX ("Offset X", Float ) = 0.0
          _OffsetY ("Offset Y", Float ) = 0.0
      }
  
      SubShader {
          Pass {
              CGPROGRAM
  
              #pragma vertex vert
              #pragma fragment frag
              #include "UnityCG.cginc"
 
              float _TileX;
              float _TileY;
              float _OffsetX;
              float _OffsetY;
 
              struct v2f{
                  float4 pos: SV_POSITION;
                  fixed2 uv: TEXCOORD0;
              };
  
              v2f vert (appdata_base v) {
                  v2f vOutput;
                  vOutput.pos = UnityObjectToClipPos(v.vertex);
                  vOutput.uv = v.texcoord;
 
                  return vOutput;
              }
  
              fixed4 frag (v2f vOutput) : SV_Target {
                  fixed2 uvs = float2(vOutput.uv.x * _TileX, vOutput.uv.y * _TileY);
                  uvs += float2(_OffsetX, _OffsetY);
                //   fixed f = uvs[0];
                  
                  fixed4 texcol = fixed4 (uvs[0], uvs[1], 0.0, 1.0);
                  return texcol;
              }
  
              ENDCG
          }
      }
  }