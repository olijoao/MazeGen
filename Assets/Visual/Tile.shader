Shader "Unlit/Tile"{
    Properties{
        _MainTex("Albedo (RGB)", 2D) = "white" {}   //unused, needs to run in webgl?
        _Tile("tile",                   Int)    = 0
        _highlighted("highlight",       Int)    = 0
        _uiElement("uiElement",         Int)    = 0
		_stone_pos("stone pos",         Vector) = (0,0,0,0)
        _switch_pos("switch pos",       Vector) = (0,0,0,0) 
        _switch_radius("switch radius", Float)  = 0 
    }

    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            int			_Tile;
            int			_highlighted	= 0;
            int			_uiElement		= 0;
			float4		_stone_pos      = 0;
            float4      _switch_pos     = 0;
            float       _switch_radius  = 0;


            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //math
            #define M_PI        3.14159265359
            #define M_HALF_PI   1.57079632679

            //defines copy pasted from C#
            #define offset_Content      3
            #define offset_ContentInfo  6

            #define section_Ground      7
            #define section_Content     (7  << offset_Content)
            #define section_ContentInfo (3  << offset_ContentInfo)

            #define GROUND_NONE         0
            #define GROUND_ICE          1
            #define GROUND_BRIDGE       2
            #define GROUND_RED          3

            #define Content_None         0 << offset_Content
            #define Content_Switch       1 << offset_Content
            #define Content_Stone        2 << offset_Content
            #define Content_Shift        3 << offset_Content

            #define getGround(tile)         (tile & section_Ground)
            #define getContent(tile)        (tile & section_Content)
            #define getContentInfo(tile)    ((tile & section_ContentInfo)>>offset_ContentInfo)


			float2 rotate(float2 p, float angle){
				float s = sin(angle);
				float c = cos(angle);

				return float2(	p.x * c - p.y * s, 
								p.x * s + p.y * c);
			}


            float sdRoundedBox(float2 p, float2 b, float4 r) {
                r.xy = (p.x > 0.0) ? r.xy : r.zw;
                r.x = (p.y > 0.0) ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }



            float getContentMask(float2 uv, uint tile) {
                switch (getContent(tile)) {
                case Content_None:
                    break;

                case Content_Switch:
                    float distance = length(uv - 0.5);
              
                    if (distance < 0.2)
                        return clamp(0,1,1.0-(distance-0.19)/0.01);
                    break;

                case Content_Stone:
                    if (abs(uv.x + _stone_pos.x - 0.5) < 0.3 && abs(uv.y + _stone_pos.y - 0.5) < 0.3)
                        return 1;
                    break;

                case Content_Shift:
                    float2 pos = -2 * (uv - 0.5f);
                    pos = rotate(pos, M_HALF_PI * (1 + getContentInfo(tile)));
                    pos *= 0.5f;
                    pos.x -= 0.2;

                    float xArrow = pos.x + abs(pos.y) * .75f;

                    if (xArrow<0 && xArrow > -0.2 && abs(pos.y) < 0.2)
                        return 1;
                    break;

                default:
                    return 0;
                }

                return 0;
            }




            fixed4 getTileColor(v2f i){
                float    content     = getContentMask(i.uv, _Tile);

                //ground
                float4 ground_white = fixed4(.83, .89, .84, 1);
                float4 ground_black = fixed4(.1,.1,.1,1)  ;

                //swtich animation
                if (_switch_radius > 0.0f) {
                    float radius = _switch_radius; 
                    radius = sqrt(sqrt(sqrt(radius)));
                    float2 dist = abs(i.uv.xy -0.5f + _switch_pos.xy);

                    // wipe transition
                    if ( dist.x < radius * 1.5f &&  dist.y < radius * 1.5f) 
                        content = 1.0f - content;

                    // border
                    if ( dist.x > 1.45f && dist.x < 1.5f ||  dist.y > 1.45f && dist.y < 1.5f)
                        return fixed4(0.5f, 0.5f, 0.5f, 1);
                    
                }

                switch (getGround(_Tile)) {
                    case GROUND_NONE:       return lerp(ground_black, ground_white, content);
                    case GROUND_ICE:        return lerp(ground_white, ground_black, content);
                    case GROUND_RED:        return fixed4(1, 0, 0, 1);
                    case GROUND_BRIDGE:
                        int2 pos = (int2)(i.vertex.xy/3);                      
                        if ((pos.x+pos.y)%2==0)
                            return ground_white;
                        return ground_black;
                    default:                return fixed4(1, 0, 1, 1);
                }

                return fixed4(1, 0, 1, 1);
            }



            fixed4 frag(v2f i) : SV_Target{
                fixed4 tileColor = getTileColor(i);
                
                if(_uiElement != 0){
                    if (_highlighted != 0) {
                        if (abs(i.uv.x - 0.5f) > 0.45f || abs(i.uv.y - 0.5f) > 0.45f)
                            return fixed4(.043, .631, 711, 1);
                    }else
                        return tileColor * 0.5;
                }
                
                return tileColor;     
            }
            ENDCG
        }
    }
}
