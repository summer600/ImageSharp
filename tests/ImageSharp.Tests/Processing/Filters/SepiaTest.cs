// Copyright (c) Six Labors and contributors.
// Licensed under the GNU Affero General Public License, Version 3.

using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Filters;
using Xunit;

namespace SixLabors.ImageSharp.Tests.Processing.Filters
{
    public class SepiaTest : BaseImageOperationsExtensionTest
    {
        [Fact]
        public void Sepia_amount_SepiaProcessorDefaultsSet()
        {
            this.operations.Sepia();
            this.Verify<SepiaProcessor>();
        }

        [Fact]
        public void Sepia_amount_rect_SepiaProcessorDefaultsSet()
        {
            this.operations.Sepia(this.rect);
            this.Verify<SepiaProcessor>(this.rect);
        }
    }
}
