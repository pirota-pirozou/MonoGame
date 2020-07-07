// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Microsoft.Xna.Framework.Content.Pipeline.Graphics
{
    internal class SwitchTextureProfile : TextureProfile
    {
        public override bool Supports(TargetPlatform platform)
        {
            return platform == TargetPlatform.Switch;
        }

        public override void Requirements(ContentProcessorContext context, TextureProcessorOutputFormat format, out bool requiresPowerOfTwo, out bool requiresSquare)
        {
            var color = format == TextureProcessorOutputFormat.Color || format == TextureProcessorOutputFormat.Color16Bit;
            var compressed = format == TextureProcessorOutputFormat.Compressed || format == TextureProcessorOutputFormat.DxtCompressed;

            // Look for crap we don't support!
            if (!color && !compressed)
                throw new NotSupportedException("We do not support '" + format + "' on the Nintendo Switch platform!");

            // Switch supports non-PoT and non-Square DXT compressed texture data.
            requiresPowerOfTwo = false;
            requiresSquare = false;

        }

        protected override void PlatformCompressTexture(ContentProcessorContext context, TextureContent content, TextureProcessorOutputFormat format, bool isSpriteFont)
        {
            // For now fonts are always uncompressed.
            if (isSpriteFont)
                return;

            throw new NotImplementedException();
        }
    }
}
