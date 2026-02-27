Shader "UI/StencilWriter"
{
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Pass
        {
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }
}